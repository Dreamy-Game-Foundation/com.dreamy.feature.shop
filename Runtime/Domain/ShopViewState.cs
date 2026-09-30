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
        public ShopOfferViewState(ShopOfferConfig offer)
        {
            Offer = offer ?? throw new ArgumentNullException(nameof(offer));
        }

        public ShopOfferConfig Offer { get; }

        public string PurchaseLabel => Offer.PurchaseKind == ShopPurchaseKind.Iap
            ? string.IsNullOrWhiteSpace(Offer.DisplayPrice) ? Offer.StoreProductId : Offer.DisplayPrice
            : $"{Offer.Cost.Amount} {Offer.Cost.ResourceId}";
    }
}
