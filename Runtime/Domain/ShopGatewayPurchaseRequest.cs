using System;

namespace Dreamy.Shop
{
    public readonly struct ShopGatewayPurchaseRequest
    {
        public ShopGatewayPurchaseRequest(string offerId, string storeProductId)
        {
            if (string.IsNullOrWhiteSpace(offerId))
                throw new ArgumentException("Offer ID cannot be empty.", nameof(offerId));
            if (string.IsNullOrWhiteSpace(storeProductId))
                throw new ArgumentException("Store product ID cannot be empty.", nameof(storeProductId));

            OfferId = offerId;
            StoreProductId = storeProductId;
        }

        public string OfferId { get; }
        public string StoreProductId { get; }
    }

    public readonly struct ShopGatewayPurchaseResult
    {
        private ShopGatewayPurchaseResult(ShopGatewayPurchaseStatus status, string transactionId)
        {
            Status = status;
            TransactionId = transactionId;
        }

        public ShopGatewayPurchaseStatus Status { get; }
        public string TransactionId { get; }
        public bool IsSuccess => Status == ShopGatewayPurchaseStatus.Purchased;

        public static ShopGatewayPurchaseResult Purchased(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentException("Transaction ID cannot be empty.", nameof(transactionId));
            return new ShopGatewayPurchaseResult(ShopGatewayPurchaseStatus.Purchased, transactionId);
        }

        public static ShopGatewayPurchaseResult Cancelled() =>
            new ShopGatewayPurchaseResult(ShopGatewayPurchaseStatus.Cancelled, null);

        public static ShopGatewayPurchaseResult Failed() =>
            new ShopGatewayPurchaseResult(ShopGatewayPurchaseStatus.Failed, null);
    }

    public enum ShopGatewayPurchaseStatus
    {
        Purchased,
        Cancelled,
        Failed
    }
}
