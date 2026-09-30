using System;
using Dreamy.Core;
using Dreamy.DataConfig;
using Dreamy.Economy;

namespace Dreamy.Shop
{
    public static class ShopInstaller
    {
        public static void RegisterConfig(IDataConfigService dataConfigService)
        {
            if (dataConfigService == null) throw new ArgumentNullException(nameof(dataConfigService));
            dataConfigService.Register<ShopCatalogConfig>("shopCatalog");
        }

        public static IShopService Install()
        {
            return Install(
                ServiceLocator.Get<IDataConfigService>().GetTable<ShopCatalogConfig>(),
                ServiceLocator.Get<IResourceWallet>(),
                ServiceLocator.TryGet<IShopPurchaseGateway>(out IShopPurchaseGateway purchaseGateway)
                    ? purchaseGateway
                    : null);
        }

        public static IShopService Install(
            ShopCatalogConfig catalog,
            IResourceWallet wallet,
            IShopPurchaseGateway purchaseGateway = null)
        {
            ShopModel service = new(catalog, wallet, purchaseGateway);
            ServiceLocator.Register<IShopService>(service);
            return service;
        }
    }
}
