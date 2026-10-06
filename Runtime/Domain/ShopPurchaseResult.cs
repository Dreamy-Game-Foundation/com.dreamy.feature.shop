namespace Dreamy.Shop
{
    public readonly struct ShopPurchaseResult
    {
        private ShopPurchaseResult(ShopPurchaseStatus status, ShopOfferConfig offer)
        {
            Status = status;
            Offer = offer;
        }

        public ShopPurchaseStatus Status { get; }
        public ShopOfferConfig Offer { get; }
        public bool IsSuccess => Status == ShopPurchaseStatus.Purchased;

        public static ShopPurchaseResult AlreadyOwned(ShopOfferConfig offer) => new(ShopPurchaseStatus.AlreadyOwned, offer);
        public static ShopPurchaseResult PurchaseInProgress(ShopOfferConfig offer) => new(ShopPurchaseStatus.PurchaseInProgress, offer);
        public static ShopPurchaseResult Purchased(ShopOfferConfig offer) => new(ShopPurchaseStatus.Purchased, offer);
        public static ShopPurchaseResult OfferNotFound() => new(ShopPurchaseStatus.OfferNotFound, null);
        public static ShopPurchaseResult ExchangeFailed(ShopOfferConfig offer) => new(ShopPurchaseStatus.ExchangeFailed, offer);
        public static ShopPurchaseResult IapGatewayUnavailable(ShopOfferConfig offer) => new(ShopPurchaseStatus.IapGatewayUnavailable, offer);
        public static ShopPurchaseResult PurchaseCancelled(ShopOfferConfig offer) => new(ShopPurchaseStatus.PurchaseCancelled, offer);
        public static ShopPurchaseResult PurchaseFailed(ShopOfferConfig offer) => new(ShopPurchaseStatus.PurchaseFailed, offer);
        public static ShopPurchaseResult RewardGrantFailed(ShopOfferConfig offer) => new(ShopPurchaseStatus.RewardGrantFailed, offer);
    }

    public enum ShopPurchaseStatus
    {
        Purchased,
        OfferNotFound,
        ExchangeFailed,
        IapGatewayUnavailable,
        PurchaseCancelled,
        PurchaseFailed,
        RewardGrantFailed,
        AlreadyOwned,
        PurchaseInProgress
    }
}
