using System;
using Cysharp.Threading.Tasks;

namespace Dreamy.Shop
{
    public sealed class ShopPresenter : IDisposable
    {
        private readonly IShopService service;
        private readonly IShopView view;
        private bool isBound;

        public ShopPresenter(IShopService service, IShopView view)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public void Show()
        {
            Bind();
            view.Render(service.GetState());
        }

        public void Dispose()
        {
            if (!isBound) return;
            view.PurchaseRequested -= Purchase;
            view.CloseRequested -= Close;
            isBound = false;
        }

        private void Bind()
        {
            if (isBound) return;
            view.PurchaseRequested += Purchase;
            view.CloseRequested += Close;
            isBound = true;
        }

        private void Purchase(string offerId)
        {
            PurchaseAsync(offerId).Forget();
        }

        private async UniTaskVoid PurchaseAsync(string offerId)
        {
            view.SetPurchaseInteractable(false);
            try
            {
                ShopPurchaseResult result = await service.PurchaseAsync(offerId);
                view.ShowPurchaseResult(result);
                view.Render(service.GetState());
            }
            finally
            {
                view.SetPurchaseInteractable(true);
            }
        }

        private void Close()
        {
            Dispose();
            view.Close();
        }
    }
}
