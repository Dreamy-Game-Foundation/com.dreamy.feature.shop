using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Dreamy.DataConfig;
using Dreamy.Economy;
using Dreamy.Feedback;
using Dreamy.Shop;
using Dreamy.UI;

namespace Dreamy.Feature.Shop.Integration
{
    /// <summary>Production composition entry point for the supplied Shop view.</summary>
    public static class ShopFeatureInstaller
    {
        public static void RegisterConfig(IDataConfigService config) => ShopInstaller.RegisterConfig(config);

        public static IShopService Install(PanelPresenterFactory factory, ShopCatalogConfig catalog,
            IResourceWallet wallet, IShopPurchaseGateway purchases = null, IResourceBalanceProvider balances = null,
            FeedbackHost feedbackHost = null,
            Func<ShopPurchaseResult, CancellationToken, UniTask> recoverResources = null)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            return Install(factory, ShopInstaller.Install(catalog, wallet, purchases, balances), feedbackHost, recoverResources);
        }

        public static IShopService Install(PanelPresenterFactory factory, IShopService service, FeedbackHost feedbackHost = null,
            Func<ShopPurchaseResult, CancellationToken, UniTask> recoverResources = null)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (service == null) throw new ArgumentNullException(nameof(service));
            factory.Register<ShopPanel>(view =>
            {
                view.BindFeedbackHost(feedbackHost);
                return new ShopPresenter(service, view, view.PresentPurchaseAsync, recoverResources);
            });
            return service;
        }
    }
}
