using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Economy;
using Dreamy.Shop.UI;

namespace Dreamy.Feature.Shop.Integration
{
    // Scroll-list example. A tabbed game implements the same navigator and resolves destinationId to its tab.
    public sealed class ShopPanelResourceNavigator : IShopResourceNavigator
    {
        private readonly Func<CancellationToken, UniTask<ShopPanel>> openReadyPanel;
        public ShopPanelResourceNavigator(Func<CancellationToken, UniTask<ShopPanel>> openReadyPanel) =>
            this.openReadyPanel = openReadyPanel ?? throw new ArgumentNullException(nameof(openReadyPanel));

        public async UniTask<ResourceAcquisitionResult> OpenAsync(string destinationId, string offerId,
            ResourceAcquisitionRequest request, CancellationToken token)
        {
            var panel = await openReadyPanel(token);
            token.ThrowIfCancellationRequested();
            if (panel == null) return ResourceAcquisitionResult.Failed;
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token, panel.GetCancellationTokenOnDestroy());
            var returned = new UniTaskCompletionSource();
            void Closed() => returned.TrySetResult();
            panel.OnPostHide += Closed;
            panel.Destroyed += Closed;
            try
            {
                if (!await panel.FocusOfferAsync(offerId, lifetime.Token)) return ResourceAcquisitionResult.NoMatchingDestination;
                await returned.Task.AttachExternalCancellation(lifetime.Token);
                return ResourceAcquisitionResult.Returned;
            }
            finally { panel.OnPostHide -= Closed; panel.Destroyed -= Closed; }
        }
    }
}
