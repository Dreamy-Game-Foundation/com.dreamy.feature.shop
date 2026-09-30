using System.Threading;
using Cysharp.Threading.Tasks;

namespace Dreamy.Shop
{
    public interface IShopPurchaseGateway
    {
        UniTask<ShopGatewayPurchaseResult> PurchaseAsync(
            ShopGatewayPurchaseRequest request,
            CancellationToken cancellationToken = default);
    }
}
