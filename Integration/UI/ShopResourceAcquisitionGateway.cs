using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Economy;
using Dreamy.Economy.UI;

namespace Dreamy.Shop.UI
{
    public sealed class ShopResourceAcquisitionGateway : IResourceAcquisitionGateway
    {
        private readonly ShopCatalogConfig catalog;
        private readonly ShopResourceDestinationMap destinations;
        private readonly IShopResourceNavigator navigator;
        private readonly IResourceBalanceProvider balances;
        private bool busy;

        public ShopResourceAcquisitionGateway(ShopCatalogConfig catalog, ShopResourceDestinationMap destinations, IShopResourceNavigator navigator, IResourceBalanceProvider balances = null)
        {
            this.balances = balances;
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.destinations = destinations ?? throw new ArgumentNullException(nameof(destinations));
            this.navigator = navigator ?? throw new ArgumentNullException(nameof(navigator));
        }

        public ResourceAcquisitionAvailability GetAvailability(ResourceId resourceId) =>
            destinations.TryGet(resourceId, out _) && FindOffer(resourceId) != null
                ? ResourceAcquisitionAvailability.Available : ResourceAcquisitionAvailability.Unavailable;

        public async UniTask<ResourceAcquisitionResult> OpenAsync(ResourceAcquisitionRequest request, CancellationToken token)
        {
            if (token.IsCancellationRequested) return ResourceAcquisitionResult.Cancelled;
            if (!request.IsValid) return ResourceAcquisitionResult.Failed;
            if (busy) return ResourceAcquisitionResult.Busy;
            var offer = FindOffer(request.TargetResourceId);
            if (offer == null || !destinations.TryGet(request.TargetResourceId, out string destination))
                return ResourceAcquisitionResult.NoMatchingDestination;
            busy = true;
            try
            {
                var result = await navigator.OpenAsync(destination, offer.Id, request, token);
                return token.IsCancellationRequested ? ResourceAcquisitionResult.Cancelled : result;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { return ResourceAcquisitionResult.Cancelled; }
            catch (Exception) { return ResourceAcquisitionResult.Failed; }
            finally { busy = false; }
        }

        private ShopOfferConfig FindOffer(ResourceId resource)
        {
            foreach (var offer in catalog.Offers)
            {
                if (offer.PurchaseOnce && (balances == null || balances.GetBalance(offer.OwnershipResourceId) > 0)) continue;
                // Avoid suggesting a conversion that itself consumes the missing resource.
                if (offer.PurchaseKind == ShopPurchaseKind.VirtualCurrency && offer.Cost.ResourceId == resource) continue;
                foreach (var reward in offer.Rewards)
                    if (reward.Resource.ResourceId == resource) return offer;
            }
            return null;
        }
    }
}
