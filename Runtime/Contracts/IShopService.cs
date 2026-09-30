using System.Threading;
using Cysharp.Threading.Tasks;

namespace Dreamy.Shop
{
    public interface IShopService
    {
        ShopViewState GetState();
        ShopPurchaseResult Purchase(string offerId);
        UniTask<ShopPurchaseResult> PurchaseAsync(string offerId, CancellationToken cancellationToken = default);
    }
}
