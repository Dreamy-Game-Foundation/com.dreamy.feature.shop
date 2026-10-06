using System;
using System.Collections.Generic;
using Dreamy.DataConfig;
using Dreamy.Economy;
using Newtonsoft.Json;

namespace Dreamy.Shop
{
    public sealed class ShopCatalogConfig : ConfigBase
    {
        [JsonProperty("catalogId", Required = Required.Always)]
        private string catalogId;

        [JsonProperty("offers", Required = Required.Always)]
        private List<ShopOfferConfig> offers = new();

        [JsonIgnore]
        public string CatalogId => catalogId;

        [JsonIgnore]
        public IReadOnlyList<ShopOfferConfig> Offers => offers;

        public override void Initialize(string documentName)
        {
            if (string.IsNullOrWhiteSpace(catalogId))
            {
                throw new DataConfigException(documentName, "catalogId cannot be empty.");
            }

            if (offers == null || offers.Count == 0)
            {
                throw new DataConfigException(documentName, "offers must contain at least one item.");
            }

            HashSet<string> ids = new(StringComparer.Ordinal);
            foreach (ShopOfferConfig offer in offers)
            {
                offer?.Validate(documentName, ids);
            }
        }
    }

    [Serializable]
    public sealed class ShopOfferConfig
    {
        [JsonProperty("id", Required = Required.Always)]
        private string id;

        [JsonProperty("titleKey", Required = Required.Always)]
        private string titleKey;

        [JsonProperty("rewardText")]
        private string rewardText;

        [JsonProperty("iconKey")]
        private string iconKey;

        [JsonProperty("purchaseKind", Required = Required.Always)]
        private ShopPurchaseKind purchaseKind;

        [JsonProperty("purchaseOnce")]
        private bool purchaseOnce;

        [JsonProperty("ownershipResourceId")]
        private string ownershipResourceId;

        [JsonProperty("costResourceId")]
        private string costResourceId;

        [JsonProperty("costAmount")]
        private long costAmount;

        [JsonProperty("storeProductId")]
        private string storeProductId;

        [JsonProperty("displayPrice")]
        private string displayPrice;

        [JsonProperty("rewards", Required = Required.Always)]
        private List<ShopResourceConfig> rewards = new();

        [JsonIgnore]
        public string Id => id;

        [JsonIgnore]
        public string TitleKey => titleKey;

        [JsonIgnore]
        public string RewardText => rewardText ?? string.Empty;

        /// <summary>Optional presentation key resolved by the host's icon provider.</summary>
        [JsonIgnore]
        public string IconKey => iconKey;

        [JsonIgnore]
        public ShopPurchaseKind PurchaseKind => purchaseKind;

        [JsonIgnore]
        public bool PurchaseOnce => purchaseOnce;

        [JsonIgnore]
        public ResourceId OwnershipResourceId => new(ownershipResourceId);

        [JsonIgnore]
        public ResourceAmount Cost => new(new ResourceId(costResourceId), costAmount);

        [JsonIgnore]
        public string StoreProductId => storeProductId;

        [JsonIgnore]
        public string DisplayPrice => displayPrice;

        [JsonIgnore]
        public IReadOnlyList<ShopResourceConfig> Rewards => rewards;

        internal void Validate(string documentName, ISet<string> ids)
        {
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
            {
                throw new DataConfigException(documentName, "Each offer requires a unique non-empty id.");
            }

            if (string.IsNullOrWhiteSpace(titleKey) || rewards == null || rewards.Count == 0)
            {
                throw new DataConfigException(documentName, $"Offer '{id}' has invalid data.");
            }

            if (purchaseKind == ShopPurchaseKind.VirtualCurrency &&
                (!ResourceId.TryParse(costResourceId, out _) || costAmount <= 0))
            {
                throw new DataConfigException(documentName, $"Virtual offer '{id}' has an invalid cost.");
            }

            if (purchaseKind == ShopPurchaseKind.Iap &&
                string.IsNullOrWhiteSpace(storeProductId))
            {
                throw new DataConfigException(documentName, $"IAP offer '{id}' requires a storeProductId.");
            }

            foreach (ShopResourceConfig reward in rewards)
            {
                reward?.Validate(documentName, id);
            }

            if (purchaseOnce)
            {
                if (!ResourceId.TryParse(ownershipResourceId, out ResourceId ownershipId))
                    throw new DataConfigException(documentName, $"Offer '{id}' requires a valid ownershipResourceId.");
                int ownershipRewards = 0;
                foreach (ShopResourceConfig reward in rewards)
                    if (reward != null && reward.Resource.ResourceId == ownershipId) ownershipRewards++;
                if (ownershipRewards != 1 || rewards[rewards.Count - 1] == null || rewards[rewards.Count - 1].Resource.ResourceId != ownershipId)
                    throw new DataConfigException(documentName, $"Offer '{id}' must grant its ownership resource exactly once, as the last reward.");
            }
        }
    }

    public enum ShopPurchaseKind
    {
        VirtualCurrency,
        Iap
    }

    [Serializable]
    public sealed class ShopResourceConfig
    {
        [JsonProperty("resourceId", Required = Required.Always)]
        private string resourceId;

        [JsonProperty("amount", Required = Required.Always)]
        private long amount;

        [JsonIgnore]
        public ResourceAmount Resource => new(new ResourceId(resourceId), amount);

        internal void Validate(string documentName, string offerId)
        {
            if (!ResourceId.TryParse(resourceId, out _) || amount <= 0)
            {
                throw new DataConfigException(documentName, $"Offer '{offerId}' has an invalid reward.");
            }
        }
    }
}
