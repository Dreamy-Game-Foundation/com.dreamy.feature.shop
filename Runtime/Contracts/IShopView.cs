using System;

namespace Dreamy.Shop
{
    public interface IShopView
    {
        event Action<string> PurchaseRequested;
        event Action CloseRequested;

        void Render(ShopViewState state);
        void SetPurchaseInteractable(bool interactable);
        void ShowPurchaseResult(ShopPurchaseResult result);
        void Close();
    }
}
