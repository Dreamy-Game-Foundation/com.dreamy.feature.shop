using System;
using Dreamy.UI;
using Cysharp.Threading.Tasks;

namespace Dreamy.Shop
{
    public sealed class ShopPresenter : IPanelPresenter
    {
        private readonly IShopService service;
        private readonly IShopView view;
        private bool isBound;
        private bool isPurchasing;
        private int generation;

        public ShopPresenter(IShopService service, IShopView view)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
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
        }

        private void Bind()
        {
            if (isBound) return;
            generation++;
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
            isPurchasing = true;
            view.SetPurchaseInteractable(false);
            try
            {
                ShopPurchaseResult result = await service.PurchaseAsync(offerId);
                if (!isBound || generation != purchaseGeneration) return;
                view.ShowPurchaseResult(result);
                view.Render(service.GetState());
            }
            finally
            {
                if (isBound && generation == purchaseGeneration)
                {
                    isPurchasing = false;
                    view.SetPurchaseInteractable(true);
                }
            }
        }

        private void Close()
        {
            Dispose();
            view.Close();
        }
    }
}
