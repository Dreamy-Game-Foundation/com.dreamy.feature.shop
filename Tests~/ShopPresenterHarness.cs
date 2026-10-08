using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Shop;
using Dreamy.UI;
using Dreamy.Feature.Shop.Integration;

internal static class ShopPresenterHarness
{
    private sealed class Service : IShopService
    {
        public int Calls;
        public UniTaskCompletionSource<ShopPurchaseResult> Pending;
        public ShopViewState GetState() => new(Array.Empty<ShopOfferViewState>());
        public ShopPurchaseResult Purchase(string id) => ShopPurchaseResult.OfferNotFound();
        public UniTask<ShopPurchaseResult> PurchaseAsync(string id, CancellationToken token = default)
        {
            Calls++;
            return Pending?.Task ?? UniTask.FromResult(ShopPurchaseResult.OfferNotFound());
        }
    }
    private sealed class View : IShopView
    {
        public event Action<string> PurchaseRequested;
        public event Action CloseRequested;
        public int Renders, Results, Writes, Closes;
        public bool Interactable;
        public void Render(ShopViewState state) => Renders++;
        public void SetPurchaseInteractable(bool value) { Writes++; Interactable = value; }
        public void ShowPurchaseResult(ShopPurchaseResult result) => Results++;
        public void Close() => Closes++;
        public void Buy() => PurchaseRequested?.Invoke("offer");
        public void RequestClose() => CloseRequested?.Invoke();
    }
    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); Console.WriteLine("PASS Shop " + message); }

    public static void Main()
    {
        int backendCalls = 0;
        using var tokenSource = new CancellationTokenSource();
        var gateway = new ShopPurchaseGateway((request, token) => {
            Check(request.OfferId == "offer" && request.StoreProductId == "product" && token == tokenSource.Token,
                "billing adapter forwards request and cancellation token");
            backendCalls++;
            return UniTask.FromResult(ShopGatewayPurchaseResult.Purchased("verified-store-transaction"));
        });
        var result = gateway.PurchaseAsync(new ShopGatewayPurchaseRequest("offer", "product"), tokenSource.Token)
            .GetAwaiter().GetResult();
        Check(result.TransactionId == "verified-store-transaction", "billing adapter preserves backend transaction ID");
        tokenSource.Cancel();
        result = gateway.PurchaseAsync(new ShopGatewayPurchaseRequest("offer", "product"), tokenSource.Token)
            .GetAwaiter().GetResult();
        Check(backendCalls == 1 && result.Status == ShopGatewayPurchaseStatus.Cancelled,
            "cancelled billing never calls backend or creates a transaction");
        bool nullRejected = false;
        try { new ShopPurchaseGateway(null); } catch (ArgumentNullException) { nullRejected = true; }
        Check(nullRejected, "billing adapter requires a real host callback");
        var service = new Service();
        var view = new View();
        var factory = new PanelPresenterFactory();
        int created = 0;
        factory.Register<View>(target => { created++; return new ShopPresenter(service, target); });
        var host = new PanelPresenterHost(factory, view);
        host.Show(); host.Show(); view.Buy();
        Check(created == 1 && service.Calls == 1, "factory binds one presenter per opening");
        Check(view.Results == 1 && view.Interactable, "completed purchase renders and unlocks buttons");
        host.Dispose(); host.Dispose(); view.Buy();
        Check(service.Calls == 1, "closed view has no purchase subscription");
        host.Show(); view.Buy();
        Check(created == 2 && service.Calls == 2, "cached reopen creates one fresh presenter");
        view.RequestClose(); view.Buy();
        Check(view.Closes == 1 && service.Calls == 2, "close intent unbinds before closing view");
        host.Dispose();

        service.Pending = new UniTaskCompletionSource<ShopPurchaseResult>();
        host.Show(); view.Buy(); view.Buy();
        Check(service.Calls == 3 && !view.Interactable, "pending purchase blocks repeated clicks");
        host.Dispose();
        int writes = view.Writes, renders = view.Renders, results = view.Results;
        service.Pending.TrySetResult(ShopPurchaseResult.OfferNotFound());
        Check(view.Writes == writes && view.Renders == renders && view.Results == results,
            "late purchase completion never updates a disposed view");
        service.Pending = new UniTaskCompletionSource<ShopPurchaseResult>();
        var reused = new ShopPresenter(service, view);
        reused.Show(); view.Buy(); reused.Dispose(); reused.Show();
        writes = view.Writes; renders = view.Renders; results = view.Results;
        service.Pending.TrySetResult(ShopPurchaseResult.OfferNotFound());
        Check(view.Writes == writes && view.Renders == renders && view.Results == results,
            "old purchase cannot update a reopened presenter generation");
        service.Pending = null;
        view.Buy();
        Check(service.Calls == 5 && view.Interactable, "reopened presenter can purchase again");
        reused.Dispose();

        var fixtureType = Assembly.GetExecutingAssembly().GetType("Dreamy.Shop.Tests.ShopModelTests");
        var fixture = Activator.CreateInstance(fixtureType);
        foreach (var method in fixtureType.GetMethods())
        {
            if (!Array.Exists(method.GetCustomAttributes(false), a => a.GetType().Name == "TestAttribute")) continue;
            method.Invoke(fixture, null);
            Console.WriteLine("PASS Shop model " + method.Name);
        }
    }
}
