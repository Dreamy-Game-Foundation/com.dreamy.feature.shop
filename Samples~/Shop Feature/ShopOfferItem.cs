using System;
using Dreamy.Shop;
using Dreamy.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamy.Feature.Shop.Integration
{
    public sealed class ShopOfferItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private Button purchaseButton;
        [SerializeField] private Image iconImage;
        private string offerId;
        private bool canPurchase = true;
        private bool purchaseEnabled = true;

        public event Action<string> PurchaseRequested;

        private void OnEnable() => purchaseButton.onClick.AddListener(RequestPurchase);
        private void OnDisable() => purchaseButton.onClick.RemoveListener(RequestPurchase);

        public void Render(ShopOfferViewState state)
        {
            ShopOfferConfig offer = state.Offer;
            offerId = offer.Id;
            canPurchase = state.CanPurchase;
            purchaseButton.interactable = purchaseEnabled && canPurchase;
            titleText.text = offer.TitleKey;
            costText.text = state.PurchaseLabel;
            rewardText.text = offer.RewardText;
        }

        public void SetPurchaseInteractable(bool interactable)
        {
            purchaseEnabled = interactable;
            purchaseButton.interactable = purchaseEnabled && canPurchase;
        }

        public void SetIcon(Sprite sprite)
        {
            if (iconImage == null) return;
            iconImage.sprite = sprite;
            iconImage.preserveAspect = true;
            iconImage.enabled = sprite != null;
        }

        /// <summary>Ensures dynamically created offers participate in their parent staggered tween sequence.</summary>
        public void EnsureTweenDelaySlot()
        {
            if (GetComponent<TweenDelayByIndex>() == null) gameObject.AddComponent<TweenDelayByIndex>();
        }

        private void RequestPurchase()
        {
            if (purchaseEnabled && canPurchase) PurchaseRequested?.Invoke(offerId);
        }
    }
}
