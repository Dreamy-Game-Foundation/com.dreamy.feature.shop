using System;
using Dreamy.Shop.Recovery.Tests;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Dreamy.Economy;
using Dreamy.Economy.UI;
using Dreamy.Shop;
using Dreamy.Shop.UI;
using Newtonsoft.Json;
using NUnit.Framework;

public sealed class RecoveryFlowTests
{
    private static readonly ResourceId Coin = new("currency.coin");
    private static readonly ResourceId Gem = new("currency.gem");
    private static ResourceSpendRequest Request() => new("action", new[] { new ResourceAmount(Coin, 10) }, "upgrade");
    private sealed class Prompt : IInsufficientResourcesPresentation
    {
        public int Calls;
        public UniTask<ResourceRecoveryStatus> ShowAsync(IReadOnlyList<ResourceAmount> costs, string context, CancellationToken token)
        { Calls++; return UniTask.FromResult(ResourceRecoveryStatus.Returned); }
    }
    private sealed class View : INotEnoughResourcesView
    {
        public event Action<ResourceId> AcquireRequested;
        public event Action CancelRequested;
        public int Opens, Closes, Renders;
        public bool Opening;
        public IReadOnlyList<ResourceDeficit> Deficits;
        public void Open() => Opens++;
        public void Close() => Closes++;
        public void Render(IReadOnlyList<ResourceDeficit> deficits, Func<ResourceId, ResourceAcquisitionAvailability> availability,
            bool opening, ResourceAcquisitionResult? last)
        { Deficits = deficits; Opening = opening; Renders++; }
        public void Acquire(ResourceId id) => AcquireRequested?.Invoke(id);
        public void Cancel() => CancelRequested?.Invoke();
    }
    private sealed class Gateway : IResourceAcquisitionGateway
    {
        public int Calls;
        public ResourceAcquisitionRequest Request;
        public UniTaskCompletionSource<ResourceAcquisitionResult> Pending;
        public ResourceAcquisitionAvailability GetAvailability(ResourceId id) => ResourceAcquisitionAvailability.Available;
        public UniTask<ResourceAcquisitionResult> OpenAsync(ResourceAcquisitionRequest request, CancellationToken token)
        {
            Calls++; Request = request;
            return Pending == null ? UniTask.FromResult(ResourceAcquisitionResult.Returned) : Pending.Task.AttachExternalCancellation(token);
        }
    }
    [Test]
    public async Task Flow_PolicyAndUnsupported_DoNotLoseOriginalTransaction()
    {
        var wallet = new DatasaveResourceWallet(new RecoveryStore());
        var prompt = new Prompt(); var flow = new ResourceActionFlow(wallet, prompt);
        Assert.That((await flow.ExecuteAsync(Request())).Recovery, Is.EqualTo(ResourceRecoveryStatus.None));
        Assert.That(prompt.Calls, Is.Zero);
        var result = await flow.ExecuteAsync(Request(), InsufficientResourcePresentation.Prompt);
        Assert.That(result.Transaction.Status, Is.EqualTo(ResourceTransactionStatus.InsufficientResources));
        Assert.That(result.Recovery, Is.EqualTo(ResourceRecoveryStatus.Returned));
        Assert.That(prompt.Calls, Is.EqualTo(1));
        result = await new ResourceActionFlow(wallet).ExecuteAsync(Request(), InsufficientResourcePresentation.Prompt);
        Assert.That(result.Recovery, Is.EqualTo(ResourceRecoveryStatus.PresentationUnavailable));
        result = await new ResourceActionFlow(null, prompt).ExecuteAsync(Request(), InsufficientResourcePresentation.Prompt);
        Assert.That(result.Transaction.Status, Is.EqualTo(ResourceTransactionStatus.Unsupported));
        Assert.That(prompt.Calls, Is.EqualTo(1));
        result = await flow.ExecuteAsync(default(ResourceSpendRequest), InsufficientResourcePresentation.Prompt);
        Assert.That(result.Transaction.Status, Is.EqualTo(ResourceTransactionStatus.InvalidRequest));
        Assert.That(prompt.Calls, Is.EqualTo(1));
    }
    [Test]
    public async Task Popup_RefreshesAllDeficits_BlocksConcurrentPrompt_AndDoesNotAutoSpend()
    {
        var wallet = new DatasaveResourceWallet(new RecoveryStore());
        var view = new View(); var gateway = new Gateway();
        using var presenter = new NotEnoughResourcesPresenter(wallet, view, new ResourceAcquisitionCoordinator(gateway));
        var task = presenter.ShowAsync(new[] { new ResourceAmount(Coin, 4), new ResourceAmount(Coin, 6), new ResourceAmount(Gem, 2) }, "upgrade", default);
        Assert.That(view.Deficits.Count, Is.EqualTo(2));
        Assert.That(await presenter.ShowAsync(Request().Costs, "other", default), Is.EqualTo(ResourceRecoveryStatus.Busy));
        view.Acquire(Coin);
        Assert.That(gateway.Calls, Is.EqualTo(1));
        Assert.That(gateway.Request.MissingAmount, Is.EqualTo(10));
        Assert.That(gateway.Request.Deficits.Count, Is.EqualTo(2));
        wallet.TryGrant(new ResourceGrantRequest("earn", new ResourceAmount(Coin, 10)));
        Assert.That(view.Deficits.Count, Is.EqualTo(1));
        view.Acquire(Coin); // No longer missing; must not navigate.
        Assert.That(gateway.Calls, Is.EqualTo(1));
        wallet.TryGrant(new ResourceGrantRequest("earn-gem", new ResourceAmount(Gem, 2)));
        Assert.That(view.Deficits.Count, Is.Zero);
        view.Cancel();
        Assert.That(await task, Is.EqualTo(ResourceRecoveryStatus.Returned));
        Assert.That(wallet.GetBalance(Coin), Is.EqualTo(10));
        Assert.That(view.Closes, Is.EqualTo(1));
        int renders = view.Renders;
        wallet.TryGrant(new ResourceGrantRequest("later", new ResourceAmount(Coin, 1)));
        view.Acquire(Coin);
        Assert.That(view.Renders, Is.EqualTo(renders));
        Assert.That(gateway.Calls, Is.EqualTo(1));
    }
    [Test]
    public async Task OwnerCancellation_ClosesPopupAndAcquisition_WithoutLateRender()
    {
        var wallet = new DatasaveResourceWallet(new RecoveryStore());
        var view = new View(); var gateway = new Gateway { Pending = new UniTaskCompletionSource<ResourceAcquisitionResult>() };
        using var presenter = new NotEnoughResourcesPresenter(wallet, view, new ResourceAcquisitionCoordinator(gateway));
        using var owner = new CancellationTokenSource();
        var task = presenter.ShowAsync(Request().Costs, "owner", owner.Token);
        view.Acquire(Coin); view.Acquire(Coin);
        Assert.That(gateway.Calls, Is.EqualTo(1));
        Assert.That(view.Opening, Is.True);
        owner.Cancel();
        Assert.That(await task, Is.EqualTo(ResourceRecoveryStatus.Cancelled));
        int renders = view.Renders;
        gateway.Pending.TrySetResult(ResourceAcquisitionResult.Returned);
        Assert.That(view.Renders, Is.EqualTo(renders));
        Assert.That(view.Closes, Is.EqualTo(1));
    }
    [Test]
    public async Task DirectAcquisition_SupportsCancellationBusyAndMissingGateway()
    {
        var gateway = new Gateway { Pending = new UniTaskCompletionSource<ResourceAcquisitionResult>() };
        var coordinator = new ResourceAcquisitionCoordinator(gateway);
        using var owner = new CancellationTokenSource();
        var task = coordinator.OpenForResourceAsync(Coin, 10, "direct", owner.Token);
        Assert.That(await coordinator.OpenForResourceAsync(Gem, 1), Is.EqualTo(ResourceAcquisitionResult.Busy));
        owner.Cancel();
        Assert.That(await task, Is.EqualTo(ResourceAcquisitionResult.Cancelled));
        Assert.That(await new ResourceAcquisitionCoordinator(null).OpenForResourceAsync(Coin, 1), Is.EqualTo(ResourceAcquisitionResult.Unsupported));
    }
    private sealed class RecoveryShopService : IShopService
    {
        public int Purchases;
        public bool ShouldSucceed;
        public ShopViewState GetState() => new(Array.Empty<ShopOfferViewState>());
        public ShopPurchaseResult Purchase(string id)
        {
            Purchases++;
            if (ShouldSucceed) return ShopPurchaseResult.Purchased(null);
            return ShopPurchaseResult.InsufficientResources(null, new ResourceTransactionResult(
                ResourceTransactionStatus.InsufficientResources, "shop", new[] { new ResourceDeficit(Coin, 10, 0) }));
        }
        public UniTask<ShopPurchaseResult> PurchaseAsync(string id, CancellationToken token = default) => UniTask.FromResult(Purchase(id));
    }
    private sealed class RecoveryShopView : IShopView
    {
        public event Action<string> PurchaseRequested;
        public event Action CloseRequested;
        public int Results;
        public void Render(ShopViewState state) { }
        public void SetPurchaseInteractable(bool value) { }
        public void ShowPurchaseResult(ShopPurchaseResult result) => Results++;
        public void Close() { }
        public void Buy(string id = "offer") => PurchaseRequested?.Invoke(id);
    }
    [Test]
    public void ShopPresenter_RecoveryIsOptionalAndCancelledWithView_WithoutSuccessFeedbackOrRetry()
    {
        var service = new RecoveryShopService();
        var view = new RecoveryShopView();
        var pending = new UniTaskCompletionSource();
        CancellationToken owner = default;
        int recovery = 0, feedback = 0;
        using var presenter = new ShopPresenter(service, view,
            _ => { feedback++; return UniTask.CompletedTask; },
            (result, token) => { recovery++; owner = token; return pending.Task.AttachExternalCancellation(token); });
        presenter.Show(); view.Buy();
        Assert.That(recovery, Is.EqualTo(1));
        Assert.That(service.Purchases, Is.EqualTo(1));
        Assert.That(feedback, Is.Zero);
        presenter.Dispose();
        Assert.That(owner.IsCancellationRequested, Is.True);
        pending.TrySetResult();
        Assert.That(view.Results, Is.Zero);
        Assert.That(service.Purchases, Is.EqualTo(1));
    }

    [Test]
    public void ShopPresenter_RecoveryAllowsAnotherOffer_AndOlderFailureCannotOverwriteNewPurchase()
    {
        var service = new RecoveryShopService();
        var view = new RecoveryShopView();
        var pending = new UniTaskCompletionSource();
        int recoveries = 0, feedback = 0;
        using var presenter = new ShopPresenter(service, view,
            _ => { feedback++; return UniTask.CompletedTask; },
            (_, token) => { recoveries++; return pending.Task.AttachExternalCancellation(token); });
        presenter.Show();
        view.Buy("gold-pack");
        Assert.That(recoveries, Is.EqualTo(1));
        Assert.That(service.Purchases, Is.EqualTo(1));
        service.ShouldSucceed = true;
        view.Buy("gem-pack");
        Assert.That(service.Purchases, Is.EqualTo(2));
        Assert.That(feedback, Is.EqualTo(1));
        Assert.That(view.Results, Is.EqualTo(1));
        pending.TrySetResult();
        Assert.That(view.Results, Is.EqualTo(1));
        Assert.That(recoveries, Is.EqualTo(1));
    }

    [Test]
    public void ShopPresenter_EveryInsufficientPurchaseRequestsRecovery_EvenForSameOfferDuringAcquisition()
    {
        var service = new RecoveryShopService();
        var view = new RecoveryShopView();
        var requests = new System.Collections.Generic.List<UniTaskCompletionSource>();
        using var presenter = new ShopPresenter(service, view, recoverResources: (_, token) =>
        {
            var completion = new UniTaskCompletionSource();
            requests.Add(completion);
            return completion.Task.AttachExternalCancellation(token);
        });
        presenter.Show();
        view.Buy("gold-pack");
        view.Buy("gold-pack"); // The popup may be hidden while acquisition reuses this Shop.
        Assert.That(service.Purchases, Is.EqualTo(2));
        Assert.That(requests.Count, Is.EqualTo(2));
        requests[0].TrySetResult();
        Assert.That(view.Results, Is.Zero, "Old recovery must not overwrite the latest request.");
        requests[1].TrySetResult();
        Assert.That(view.Results, Is.EqualTo(1));
        for (int i = 0; i < 3; i++)
        {
            view.Buy("gold-pack");
            Assert.That(requests.Count, Is.EqualTo(3 + i));
            requests[2 + i].TrySetResult();
        }
        Assert.That(service.Purchases, Is.EqualTo(5));
        Assert.That(view.Results, Is.EqualTo(4));
    }

    private sealed class Navigator : IShopResourceNavigator
    {
        public string Destination, Offer;
        public int Calls;
        public UniTask<ResourceAcquisitionResult> OpenAsync(string destination, string offer, ResourceAcquisitionRequest request, CancellationToken token)
        { Destination = destination; Offer = offer; Calls++; return UniTask.FromResult(ResourceAcquisitionResult.Returned); }
    }
    [Test]
    public async Task ShopRouting_UsesRewardResourceAndSemanticDestination_AvoidsCircularCost()
    {
        var catalog = JsonConvert.DeserializeObject<ShopCatalogConfig>(@"{""catalogId"":""shop"",""offers"":[
            {""id"":""loop"",""titleKey"":""loop"",""purchaseKind"":0,""costResourceId"":""currency.coin"",""costAmount"":1,""rewards"":[{""resourceId"":""currency.coin"",""amount"":10}]},
            {""id"":""coins"",""titleKey"":""coins"",""purchaseKind"":0,""costResourceId"":""currency.gem"",""costAmount"":1,""rewards"":[{""resourceId"":""currency.coin"",""amount"":10}]}]}");
        catalog.Initialize("shop");
        var navigator = new Navigator();
        var gateway = new ShopResourceAcquisitionGateway(catalog,
            new ShopResourceDestinationMap(new Dictionary<ResourceId,string> { [Coin] = "currency-section" }), navigator);
        Assert.That(await gateway.OpenAsync(new ResourceAcquisitionRequest(Coin, 10), default), Is.EqualTo(ResourceAcquisitionResult.Returned));
        Assert.That(navigator.Offer, Is.EqualTo("coins"));
        Assert.That(navigator.Destination, Is.EqualTo("currency-section"));
        Assert.That(await gateway.OpenAsync(new ResourceAcquisitionRequest(Gem, 1), default), Is.EqualTo(ResourceAcquisitionResult.NoMatchingDestination));
        Assert.That(navigator.Calls, Is.EqualTo(1));
        var wallet = new DatasaveResourceWallet(new RecoveryStore());
        var result = new ShopModel(catalog, wallet).Purchase("coins");
        Assert.That(result.Status, Is.EqualTo(ShopPurchaseStatus.InsufficientResources));
        Assert.That(result.Deficits[0].ResourceId, Is.EqualTo(Gem));
        Assert.That(result.Deficits[0].Available, Is.Zero);
    }
}
