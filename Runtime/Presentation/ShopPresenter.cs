using System;
using System.Threading;
using Dreamy.UI;
using Cysharp.Threading.Tasks;

namespace Dreamy.Shop
{
    public sealed class ShopPresenter : IPanelPresenter
    {
        private readonly IShopService service;
        private readonly IShopView view;
        private readonly Func<ShopPurchaseResult, UniTask> presentPurchase;
        private readonly Func<ShopPurchaseResult, CancellationToken, UniTask> recoverResources;
        private CancellationTokenSource lifetime;
        private IDisposable activePresentation;
        private bool isBound;
        private bool isPurchasing;
        private int generation;
        private int purchaseVersion;

        public ShopPresenter(IShopService service, IShopView view, Func<ShopPurchaseResult, UniTask> presentPurchase = null,
            Func<ShopPurchaseResult, CancellationToken, UniTask> recoverResources = null)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.presentPurchase = presentPurchase;
            this.recoverResources = recoverResources;
        }

        public void Show()
        {
            Bind();
            view.Render(service.GetState());
            view.SetPurchaseInteractable(!isPurchasing);
        }

        public void Dispose()
        {
            if (!isBound) return;
            isBound = false;
            isPurchasing = false;
            generation++;
            view.PurchaseRequested -= Purchase;
            view.CloseRequested -= Close;
            var cancellation = lifetime;
            lifetime = null;
            try { cancellation?.Cancel(); }
            finally
            {
                try { ReleasePresentation(activePresentation); }
                finally { cancellation?.Dispose(); }
            }
        }

        private void Bind()
        {
            if (isBound) return;
            generation++;
            lifetime = new CancellationTokenSource();
            view.PurchaseRequested += Purchase;
            view.CloseRequested += Close;
            isBound = true;
        }

        private void Purchase(string offerId)
        {
            if (!isPurchasing) PurchaseAsync(offerId).Forget();
        }

        private async UniTaskVoid PurchaseAsync(string offerId)
        {
            int purchaseGeneration = generation;
            int operation = ++purchaseVersion;
            CancellationToken token = lifetime.Token;
            isPurchasing = true;
            IDisposable presentation = null;
            try
            {
                view.SetPurchaseInteractable(false);
                if (view is IShopPurchasePresentation presentationView)
                    activePresentation = presentation = presentationView.BeginPurchasePresentation(offerId, token);
                token.ThrowIfCancellationRequested();
                ShopPurchaseResult result = await service.PurchaseAsync(offerId, token);
                if (!isBound || generation != purchaseGeneration) return;
                if (result.IsSuccess && presentPurchase != null)
                    await presentPurchase(result);
                if (result.Status == ShopPurchaseStatus.InsufficientResources && recoverResources != null)
                {
                    // A failed purchase must not hold balances during resource acquisition.
                    ReleasePresentation(presentation);
                    // The modal blocks input until navigation; acquisition may reuse this same Shop.
                    isPurchasing = false;
                    view.SetPurchaseInteractable(true);
                    await recoverResources(result, token);
                }
                if (!isBound || generation != purchaseGeneration || operation != purchaseVersion) return;
                view.ShowPurchaseResult(result);
                view.Render(service.GetState());
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            finally
            {
                ReleasePresentation(presentation);
                if (isBound && generation == purchaseGeneration && operation == purchaseVersion)
                {
                    isPurchasing = false;
                    view.SetPurchaseInteractable(true);
                }
            }
        }

        private void ReleasePresentation(IDisposable presentation)
        {
            if (!ReferenceEquals(activePresentation, presentation)) return;
            activePresentation = null;
            presentation?.Dispose();
        }

        private void Close()
        {
            Dispose();
            view.Close();
        }
    }
}
