using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Dreamy.Economy;
using NUnit.Framework;

namespace Dreamy.Shop.Tests
{
    public sealed class ShopModelTests
    {
        [Test]
        public void Purchase_DelegatesSingleAtomicExchange()
        {
            ShopOfferConfig offer = CreateOffer();
            ShopCatalogConfig catalog = CreateCatalog(offer);
            RecordingWallet wallet = new();
            ShopModel model = new(catalog, wallet);

            ShopPurchaseResult result = model.Purchase("starter-pack");

            Assert.That(result.Status, Is.EqualTo(ShopPurchaseStatus.Purchased));
            Assert.That(wallet.LastRequest.Cost.Amount, Is.EqualTo(25));
            Assert.That(wallet.LastRequest.Rewards.Count, Is.EqualTo(1));
            Assert.That(wallet.LastRequest.Rewards[0].Amount, Is.EqualTo(1));
        }

        [Test]
        public void Purchase_UnknownOffer_DoesNotExchange()
        {
            ShopModel model = new(CreateCatalog(CreateOffer()), new RecordingWallet());

            ShopPurchaseResult result = model.Purchase("missing");

            Assert.That(result.Status, Is.EqualTo(ShopPurchaseStatus.OfferNotFound));
        }

        [Test]
        public void PurchaseAsync_IapOffer_GrantsEachRewardUsingTheStoreTransaction()
        {
            ShopOfferConfig offer = CreateIapOffer();
            RecordingWallet wallet = new();
            ShopModel model = new(CreateCatalog(offer), wallet, new SuccessfulPurchaseGateway());

            ShopPurchaseResult result = model.PurchaseAsync("iap-starter-pack").GetAwaiter().GetResult();

            Assert.That(result.Status, Is.EqualTo(ShopPurchaseStatus.Purchased));
            Assert.That(wallet.GrantRequests.Count, Is.EqualTo(2));
            Assert.That(wallet.GrantRequests[0].TransactionId, Is.EqualTo("main-shop:iap:iap-starter-pack:store-tx:0"));
            Assert.That(wallet.GrantRequests[1].Resource.Amount, Is.EqualTo(60));
        }

        [Test]
        public void PurchaseOnce_RejectsRepurchaseAndReadsOwnershipOnNewModel()
        {
            var catalog = CreateCatalog(CreateOnceOffer());
            var wallet = new RecordingWallet();
            var gateway = new SuccessfulPurchaseGateway();
            var model = new ShopModel(catalog, wallet, gateway);
            Assert.That(model.PurchaseAsync("iap-starter-pack").GetAwaiter().GetResult().IsSuccess, Is.True);
            Assert.That(model.PurchaseAsync("iap-starter-pack").GetAwaiter().GetResult().Status, Is.EqualTo(ShopPurchaseStatus.AlreadyOwned));
            Assert.That(model.Purchase("iap-starter-pack").Status, Is.EqualTo(ShopPurchaseStatus.AlreadyOwned));
            Assert.That(gateway.CallCount, Is.EqualTo(1));
            Assert.That(wallet.GrantRequests.Count, Is.EqualTo(1));
            var reopened = new ShopModel(catalog, wallet, gateway);
            Assert.That(reopened.GetState().Offers[0].IsOwned, Is.True);
            Assert.That(reopened.GetState().Offers[0].CanPurchase, Is.False);
            Assert.That(reopened.GetState().Offers[0].PurchaseLabel, Is.EqualTo("Owned"));
        }

        [Test]
        public void Consumable_AllowsRepeatedPurchases()
        {
            var gateway = new SuccessfulPurchaseGateway();
            var model = new ShopModel(CreateCatalog(CreateIapOffer()), new RecordingWallet(), gateway);
            Assert.That(model.PurchaseAsync("iap-starter-pack").GetAwaiter().GetResult().IsSuccess, Is.True);
            Assert.That(model.PurchaseAsync("iap-starter-pack").GetAwaiter().GetResult().IsSuccess, Is.True);
            Assert.That(gateway.CallCount, Is.EqualTo(2));
            Assert.That(model.GetState().Offers[0].IsOwned, Is.False);
        }

        [Test]
        public void PendingPurchase_BlocksDuplicateAndAllowsRetryAfterCancellation()
        {
            var gateway = new DeferredGateway();
            var model = new ShopModel(CreateCatalog(CreateOnceOffer()), new RecordingWallet(), gateway);
            var first = model.PurchaseAsync("iap-starter-pack");
            Assert.That(model.PurchaseAsync("iap-starter-pack").GetAwaiter().GetResult().Status, Is.EqualTo(ShopPurchaseStatus.PurchaseInProgress));
            Assert.That(gateway.CallCount, Is.EqualTo(1));
            gateway.Complete(ShopGatewayPurchaseResult.Cancelled());
            Assert.That(first.GetAwaiter().GetResult().Status, Is.EqualTo(ShopPurchaseStatus.PurchaseCancelled));
            var retry = model.PurchaseAsync("iap-starter-pack");
            gateway.Complete(ShopGatewayPurchaseResult.Purchased("retry-tx"));
            Assert.That(retry.GetAwaiter().GetResult().IsSuccess, Is.True);
            Assert.That(gateway.CallCount, Is.EqualTo(2));
        }

        [Test]
        public void PurchaseOnce_RequiresOwnershipReward()
        {
            var offer = CreateIapOffer();
            Set(offer, "purchaseOnce", true);
            Set(offer, "ownershipResourceId", "entitlement.remove-ads");
            Assert.Throws<Dreamy.DataConfig.DataConfigException>(() => CreateCatalog(offer));
        }

        private static ShopOfferConfig CreateOnceOffer()
        {
            var offer = CreateIapOffer();
            var reward = new ShopResourceConfig();
            Set(reward, "resourceId", "entitlement.remove-ads");
            Set(reward, "amount", 1L);
            Set(offer, "purchaseOnce", true);
            Set(offer, "ownershipResourceId", "entitlement.remove-ads");
            Set(offer, "rewards", new List<ShopResourceConfig> { reward });
            return offer;
        }

        private sealed class DeferredGateway : IShopPurchaseGateway
        {
            private UniTaskCompletionSource<ShopGatewayPurchaseResult> completion;
            public int CallCount { get; private set; }
            public UniTask<ShopGatewayPurchaseResult> PurchaseAsync(ShopGatewayPurchaseRequest request,
                System.Threading.CancellationToken cancellationToken = default)
            {
                CallCount++;
                completion = new UniTaskCompletionSource<ShopGatewayPurchaseResult>();
                return completion.Task;
            }
            public void Complete(ShopGatewayPurchaseResult result) => completion.TrySetResult(result);
        }

        private static ShopCatalogConfig CreateCatalog(ShopOfferConfig offer)
        {
            ShopCatalogConfig catalog = new();
            Set(catalog, "catalogId", "main-shop");
            Set(catalog, "offers", new List<ShopOfferConfig> { offer });
            catalog.Initialize("test");
            return catalog;
        }

        private static ShopOfferConfig CreateOffer()
        {
            ShopResourceConfig reward = new();
            Set(reward, "resourceId", "item.bomb");
            Set(reward, "amount", 1L);

            ShopOfferConfig offer = new();
            Set(offer, "id", "starter-pack");
            Set(offer, "titleKey", "shop.starter-pack");
            Set(offer, "costResourceId", "currency.coin");
            Set(offer, "costAmount", 25L);
            Set(offer, "rewards", new List<ShopResourceConfig> { reward });
            return offer;
        }

        private static ShopOfferConfig CreateIapOffer()
        {
            ShopResourceConfig coins = new();
            Set(coins, "resourceId", "currency.coin");
            Set(coins, "amount", 500L);
            ShopResourceConfig gems = new();
            Set(gems, "resourceId", "currency.gem");
            Set(gems, "amount", 60L);

            ShopOfferConfig offer = new();
            Set(offer, "id", "iap-starter-pack");
            Set(offer, "titleKey", "shop.iap-starter-pack");
            Set(offer, "purchaseKind", ShopPurchaseKind.Iap);
            Set(offer, "storeProductId", "com.dreamy.sample.starter_pack");
            Set(offer, "rewards", new List<ShopResourceConfig> { coins, gems });
            return offer;
        }

        private static void Set<T>(T instance, string fieldName, object value)
        {
            FieldInfo field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(instance, value);
        }

        private sealed class RecordingWallet : IResourceWallet, IResourceBalanceProvider
        {
            private readonly Dictionary<ResourceId, long> balances = new();
            public long GetBalance(ResourceId id) => balances.TryGetValue(id, out long amount) ? amount : 0;
            public ResourceExchangeRequest LastRequest { get; private set; }
            public List<ResourceGrantRequest> GrantRequests { get; } = new();

            public bool TryGrant(ResourceGrantRequest request)
            {
                GrantRequests.Add(request);
                balances[request.Resource.ResourceId] = GetBalance(request.Resource.ResourceId) + request.Resource.Amount;
                return true;
            }

            public bool TryExchange(ResourceExchangeRequest request)
            {
                LastRequest = request;
                return true;
            }
        }

        private sealed class SuccessfulPurchaseGateway : IShopPurchaseGateway
        {
            public int CallCount { get; private set; }
            public UniTask<ShopGatewayPurchaseResult> PurchaseAsync(
                ShopGatewayPurchaseRequest request,
                System.Threading.CancellationToken cancellationToken = default)
            {
                CallCount++;
                return UniTask.FromResult(ShopGatewayPurchaseResult.Purchased("store-tx"));
            }
        }
    }
}
