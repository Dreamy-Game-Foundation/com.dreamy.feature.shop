using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Economy;

namespace Dreamy.Shop.UI
{
    public interface IShopResourceNavigator
    {
        // Open/reuse, wait for layout, focus and complete when the player returns.
        // Respect cancellation before every late UI change; do not purchase here.
        UniTask<ResourceAcquisitionResult> OpenAsync(string destinationId, string offerId,
            ResourceAcquisitionRequest request, CancellationToken token);
    }
}
