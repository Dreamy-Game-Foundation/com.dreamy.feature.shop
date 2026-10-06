using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Shop;

namespace Dreamy.Feature.Shop.Integration
{
    public sealed class SimulatedShopPurchaseGateway : IShopPurchaseGateway
    {
        public UniTask<ShopGatewayPurchaseResult> PurchaseAsync(
            ShopGatewayPurchaseRequest request,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.FromResult(ShopGatewayPurchaseResult.Cancelled());

            return UniTask.FromResult(ShopGatewayPurchaseResult.Purchased(Guid.NewGuid().ToString("N")));
        }
    }
}
