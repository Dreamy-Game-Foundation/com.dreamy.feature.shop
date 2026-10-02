using Dreamy.Core;
using Dreamy.Shop;
using UnityEngine;

namespace Dreamy.Feature.Shop.Integration
{
    public sealed class ShopDemo : MonoBehaviour
    {
        [SerializeField] private ShopPanel panel;
        private ShopPresenter presenter;

        private void Awake() => presenter = new ShopPresenter(ServiceLocator.Get<IShopService>(), panel);
        private void OnEnable() => presenter.Show();
        private void OnDestroy() => presenter?.Dispose();
    }
}
