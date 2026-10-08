using System;
using Dreamy.DataConfig;
using Dreamy.Economy;
using Dreamy.Shop;
using Dreamy.UI;

namespace Dreamy.Feature.Shop.Integration
{
    /// <summary>Production composition entry point for the supplied Shop view.</summary>
    public static class ShopFeatureInstaller
    {
        public static void RegisterConfig(IDataConfigService config) => ShopInstaller.RegisterConfig(config);

        public static IShopService Install(PanelPresenterFactory factory, ShopCatalogConfig catalog,
            IResourceWallet wallet, IShopPurchaseGateway purchases = null, IResourceBalanceProvider balances = null)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            return Install(factory, ShopInstaller.Install(catalog, wallet, purchases, balances));
        }

        public static IShopService Install(PanelPresenterFactory factory, IShopService service)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (service == null) throw new ArgumentNullException(nameof(service));
            factory.Register<ShopPanel>(view => new ShopPresenter(service, view));
            return service;
        }
    }
}
