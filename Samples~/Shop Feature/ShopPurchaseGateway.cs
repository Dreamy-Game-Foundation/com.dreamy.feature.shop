using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Shop;

namespace Dreamy.Feature.Shop.Integration
{
    /// <summary>Adapter to the host's real billing/receipt-validation flow.</summary>
    public sealed class ShopPurchaseGateway : IShopPurchaseGateway
    {
        private readonly Func<ShopGatewayPurchaseRequest, CancellationToken, UniTask<ShopGatewayPurchaseResult>> purchase;

        public ShopPurchaseGateway(
            Func<ShopGatewayPurchaseRequest, CancellationToken, UniTask<ShopGatewayPurchaseResult>> purchase)
        {
            this.purchase = purchase ?? throw new ArgumentNullException(nameof(purchase));
        }

        public UniTask<ShopGatewayPurchaseResult> PurchaseAsync(
            ShopGatewayPurchaseRequest request, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.FromResult(ShopGatewayPurchaseResult.Cancelled());
            return purchase(request, cancellationToken);
        }
    }
}
