using System;
using System.Collections.Generic;

namespace Dreamy.Shop
{
    public sealed class ShopViewState
    {
        public ShopViewState(IReadOnlyList<ShopOfferViewState> offers)
        {
            Offers = offers ?? throw new ArgumentNullException(nameof(offers));
        }

        public IReadOnlyList<ShopOfferViewState> Offers { get; }
    }

    public readonly struct ShopOfferViewState
    {
        public ShopOfferViewState(ShopOfferConfig offer, bool isOwned = false)
        {
            Offer = offer ?? throw new ArgumentNullException(nameof(offer));
            IsOwned = isOwned;
        }

        public ShopOfferConfig Offer { get; }

        public bool IsOwned { get; }
        public bool CanPurchase => !IsOwned;

        public string PurchaseLabel => IsOwned ? "Owned" : Offer.PurchaseKind == ShopPurchaseKind.Iap
            ? string.IsNullOrWhiteSpace(Offer.DisplayPrice) ? Offer.StoreProductId : Offer.DisplayPrice
            : $"{Offer.Cost.Amount} {Offer.Cost.ResourceId}";
    }
}
