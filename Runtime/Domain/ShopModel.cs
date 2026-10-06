using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.Economy;

namespace Dreamy.Shop
{
    public sealed class ShopModel : IShopService
    {
        private readonly ShopCatalogConfig catalog;
        private readonly IResourceWallet wallet;
        private readonly IShopPurchaseGateway purchaseGateway;
        private readonly IResourceBalanceProvider balances;
        private readonly HashSet<string> pendingPurchases = new(StringComparer.Ordinal);

        public ShopModel(ShopCatalogConfig catalog, IResourceWallet wallet, IShopPurchaseGateway purchaseGateway = null, IResourceBalanceProvider balances = null)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.purchaseGateway = purchaseGateway;
            this.balances = balances ?? wallet as IResourceBalanceProvider;
            foreach (ShopOfferConfig offer in catalog.Offers)
                if (offer.PurchaseOnce && this.balances == null)
                    throw new ArgumentException("Purchase-once offers require a resource balance provider.", nameof(balances));
        }

        public ShopViewState GetState()
        {
            List<ShopOfferViewState> offers = new(catalog.Offers.Count);
            foreach (ShopOfferConfig offer in catalog.Offers)
            {
                offers.Add(new ShopOfferViewState(offer, IsOwned(offer)));
            }

            return new ShopViewState(offers);
        }

        public ShopPurchaseResult Purchase(string offerId)
        {
            ShopOfferConfig offer = FindOffer(offerId);
            if (offer == null)
            {
                return ShopPurchaseResult.OfferNotFound();
            }

            if (IsOwned(offer)) return ShopPurchaseResult.AlreadyOwned(offer);
            if (pendingPurchases.Contains(offer.Id)) return ShopPurchaseResult.PurchaseInProgress(offer);

            if (offer.PurchaseKind == ShopPurchaseKind.Iap)
            {
                return ShopPurchaseResult.IapGatewayUnavailable(offer);
            }

            return PurchaseVirtualCurrencyOffer(offer);
        }

        public async UniTask<ShopPurchaseResult> PurchaseAsync(string offerId, CancellationToken cancellationToken = default)
        {
            ShopOfferConfig offer = FindOffer(offerId);
            if (offer == null)
            {
                return ShopPurchaseResult.OfferNotFound();
            }

            if (IsOwned(offer)) return ShopPurchaseResult.AlreadyOwned(offer);
            if (pendingPurchases.Contains(offer.Id)) return ShopPurchaseResult.PurchaseInProgress(offer);

            if (offer.PurchaseKind == ShopPurchaseKind.VirtualCurrency)
            {
                return PurchaseVirtualCurrencyOffer(offer);
            }

            if (purchaseGateway == null)
            {
                return ShopPurchaseResult.IapGatewayUnavailable(offer);
            }

            pendingPurchases.Add(offer.Id);
            try
            {
                ShopGatewayPurchaseResult gatewayResult = await purchaseGateway.PurchaseAsync(
                    new ShopGatewayPurchaseRequest(offer.Id, offer.StoreProductId), cancellationToken);
                if (gatewayResult.Status == ShopGatewayPurchaseStatus.Cancelled)
                {
                    return ShopPurchaseResult.PurchaseCancelled(offer);
                }

                if (!gatewayResult.IsSuccess)
                {
                    return ShopPurchaseResult.PurchaseFailed(offer);
                }

                for (int index = 0; index < offer.Rewards.Count; index++)
                {
                    ResourceGrantRequest request = new(
                        $"{catalog.CatalogId}:iap:{offer.Id}:{gatewayResult.TransactionId}:{index}",
                        offer.Rewards[index].Resource);
                    if (!wallet.TryGrant(request))
                    {
                        return ShopPurchaseResult.RewardGrantFailed(offer);
                    }
                }

                return ShopPurchaseResult.Purchased(offer);
            }
            finally
            {
                pendingPurchases.Remove(offer.Id);
            }
        }

        private bool IsOwned(ShopOfferConfig offer) =>
            offer.PurchaseOnce && balances.GetBalance(offer.OwnershipResourceId) > 0;

        private ShopPurchaseResult PurchaseVirtualCurrencyOffer(ShopOfferConfig offer)
        {
            List<ResourceAmount> rewards = new(offer.Rewards.Count);
            foreach (ShopResourceConfig reward in offer.Rewards)
            {
                rewards.Add(reward.Resource);
            }

            ResourceExchangeRequest request = new(
                $"{catalog.CatalogId}:{offer.Id}:{Guid.NewGuid():N}",
                offer.Cost,
                rewards);
            return wallet.TryExchange(request)
                ? ShopPurchaseResult.Purchased(offer)
                : ShopPurchaseResult.ExchangeFailed(offer);
        }

        private ShopOfferConfig FindOffer(string offerId)
        {
            if (string.IsNullOrWhiteSpace(offerId))
            {
                return null;
            }

            foreach (ShopOfferConfig offer in catalog.Offers)
            {
                if (string.Equals(offer.Id, offerId, StringComparison.Ordinal))
                {
                    return offer;
                }
            }

            return null;
        }
    }
}
