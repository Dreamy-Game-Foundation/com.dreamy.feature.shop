using System;
using System.Collections.Generic;
using Dreamy.Shop;
using Dreamy.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamy.Feature.Shop.Integration
{
    public sealed class ShopPanel : UIPanel, IShopView
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Transform offerContainer;
        [SerializeField] private ShopOfferItem offerItemPrefab;
        private readonly List<ShopOfferItem> items = new();

        public override bool CanBack => true;
        public event Action<string> PurchaseRequested;
        public event Action CloseRequested;
        public event Action Destroyed;

        private void OnEnable() => closeButton.onClick.AddListener(RequestClose);
        protected override void OnDisable()
        {
            closeButton.onClick.RemoveListener(RequestClose);
            base.OnDisable();
        }

        public void Render(ShopViewState state)
        {
            EnsureItems(state.Offers.Count);
            for (int index = 0; index < state.Offers.Count; index++) items[index].Render(state.Offers[index]);
        }

        public void ShowPurchaseResult(ShopPurchaseResult result) =>
            statusText.text = result.IsSuccess ? "Purchase complete" : result.Status.ToString();

        public void SetPurchaseInteractable(bool interactable)
        {
            foreach (ShopOfferItem item in items)
            {
                item.SetPurchaseInteractable(interactable);
            }
        }

        public void Close() => Hide();

        protected override void OnDestroy()
        {
            foreach (ShopOfferItem item in items) if (item != null) Destroy(item.gameObject);
            Destroyed?.Invoke();
            base.OnDestroy();
        }

        private void EnsureItems(int count)
        {
            if (offerItemPrefab == null || offerContainer == null) throw new InvalidOperationException("Assign an offer item prefab and container.");
            bool addedItems = false;
            while (items.Count < count)
            {
                ShopOfferItem item = Instantiate(offerItemPrefab, offerContainer);
                item.EnsureTweenDelaySlot();
                item.PurchaseRequested += RequestPurchase;
                items.Add(item);
                addedItems = true;
            }

            if (addedItems)
            {
                GetComponentInChildren<TweenDelayControl>(true)?.ApplyStaggerDelays();
            }
        }

        private void RequestPurchase(string offerId) => PurchaseRequested?.Invoke(offerId);
        private void RequestClose() => CloseRequested?.Invoke();
    }
}
