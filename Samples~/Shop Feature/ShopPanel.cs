using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Dreamy.Shop;
using Dreamy.Economy.UI;
using Dreamy.Feedback;
using Dreamy.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;

namespace Dreamy.Feature.Shop.Integration
{
    public sealed class ShopPanel : UIPanel, IShopView, IShopPurchasePresentation
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Transform offerContainer;
        [SerializeField] private ShopOfferItem offerItemPrefab;
        [SerializeField] private SpriteAtlas iconAtlas;
        [SerializeField] private Sprite fallbackIcon;
        [Header("Confirmed purchase feedback")]
        [SerializeField] private ResourceUIHolder[] rewardHolders = Array.Empty<ResourceUIHolder>();
        [SerializeField] private FeedbackPlayer rewardFeedback;
        private Transform rewardSource;
        private int presentationVersion;
        private ShopViewState currentState;
        private PurchasePresentation activePresentation;

        private sealed class PurchasePresentation : IDisposable
        {
            private ShopPanel owner;
            private readonly CancellationTokenSource cancellation;
            public readonly CancellationToken Token;
            public readonly List<IDisposable> Holds = new();
            public FeedbackHandle Flight;

            public PurchasePresentation(ShopPanel owner, CancellationToken token)
            {
                this.owner = owner;
                cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
                Token = cancellation.Token;
            }

            public void Dispose()
            {
                if (owner == null) return;
                ShopPanel panel = owner;
                owner = null;
                if (ReferenceEquals(panel.activePresentation, this)) panel.activePresentation = null;
                try { cancellation.Cancel(); }
                finally
                {
                    try { Flight.Stop(); }
                    finally
                    {
                        try { foreach (var hold in Holds) hold.Dispose(); }
                        finally { Holds.Clear(); cancellation.Dispose(); }
                    }
                }
            }
        }
        private readonly List<ShopOfferItem> items = new();
        // GetSprite creates clones. This panel owns and destroys them.
        private readonly Dictionary<string, Sprite> icons = new(StringComparer.Ordinal);

        public override bool CanBack => true;
        public event Action<string> PurchaseRequested;
        public event Action CloseRequested;
        public event Action Destroyed;

        public void BindFeedbackHost(FeedbackHost host)
        {
            if (rewardFeedback != null && host != null) rewardFeedback.BindHost(host);
        }

        public IDisposable BeginPurchasePresentation(string offerId, CancellationToken cancellation)
        {
            CancelPresentation();
            var presentation = new PurchasePresentation(this, cancellation);
            activePresentation = presentation;
            try
            {
                ShopOfferConfig offer = null;
                if (currentState != null)
                    foreach (var entry in currentState.Offers)
                        if (entry.Offer.Id == offerId) { offer = entry.Offer; break; }
                if (rewardFeedback != null && offer != null)
                {
                    var selected = new HashSet<ResourceUIHolder>();
                    foreach (var reward in offer.Rewards)
                        foreach (var holder in rewardHolders)
                            if (holder != null && holder.isActiveAndEnabled &&
                                holder.ResourceId == reward.Resource.ResourceId && selected.Add(holder))
                                presentation.Holds.Add(holder.DeferRefresh());
                }
                return presentation;
            }
            catch { presentation.Dispose(); throw; }
        }

        public async UniTask PresentPurchaseAsync(ShopPurchaseResult result)
        {
            if (!result.IsSuccess || result.Offer == null) return;
            var presentation = activePresentation;
            int version = presentationVersion;
            try
            {
                if (result.Offer.PurchaseKind == ShopPurchaseKind.VirtualCurrency)
                    foreach (var holder in rewardHolders)
                        if (holder != null && holder.ResourceId == result.Offer.Cost.ResourceId)
                            holder.RefreshBalance();
                if (rewardFeedback == null) return;
                statusText.text = "Collecting reward...";
                foreach (var reward in result.Offer.Rewards)
                {
                    foreach (var holder in rewardHolders)
                    {
                        if (holder == null || holder.ResourceId != reward.Resource.ResourceId) continue;
                        if (presentation != null) presentation.Token.ThrowIfCancellationRequested();
                        var handle = rewardFeedback.PlayBetween(rewardSource, holder.IconTarget, reward.Resource.Amount, holder.Icon);
                        if (presentation != null) presentation.Flight = handle;
                        await handle.WaitAsync(presentation != null ? presentation.Token : default);
                        if (version != presentationVersion || !isActiveAndEnabled || handle.Status != FeedbackStatus.Completed) return;
                        holder.RefreshBalance(); // Any outstanding hold still takes precedence.
                    }
                }
            }
            catch (OperationCanceledException) when (presentation != null && presentation.Token.IsCancellationRequested) { }
            finally { presentation?.Dispose(); }
        }

        private void CancelPresentation()
        {
            presentationVersion++;
            activePresentation?.Dispose();
        }

        private void OnPanelHiding()
        {
            CancelPresentation();
            // Hide may be invoked by navigation rather than the close button.
            // Cancel the presenter lifetime now, before waiting for HideTween.
            CloseRequested?.Invoke();
        }

        private void OnEnable()
        {
            closeButton.onClick.AddListener(RequestClose);
            OnPreHide += OnPanelHiding;
        }
        protected override void OnDisable()
        {
            closeButton.onClick.RemoveListener(RequestClose);
            OnPreHide -= OnPanelHiding;
            CancelPresentation();
            base.OnDisable();
        }

        public void Render(ShopViewState state)
        {
            currentState = state;
            EnsureItems(state.Offers.Count);
            for (int index = 0; index < items.Count; index++)
            {
                items[index].gameObject.SetActive(index < state.Offers.Count);
                if (index >= state.Offers.Count) continue;
                items[index].Render(state.Offers[index]);
                items[index].SetIcon(ResolveIcon(state.Offers[index].Offer.IconKey));
            }
        }

        public async UniTask<bool> FocusOfferAsync(string offerId, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);
            token.ThrowIfCancellationRequested();
            Canvas.ForceUpdateCanvases();
            var item = items.Find(candidate => candidate.OfferId == offerId);
            if (item == null) return false;
            var scroll = offerContainer.GetComponentInParent<ScrollRect>();
            if (scroll != null && scroll.content != null && scroll.viewport != null)
            {
                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.content, item.transform);
                float height = scroll.content.rect.height - scroll.viewport.rect.height;
                if (height > 0)
                    scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01(-bounds.center.y / height);
            }
            item.GetComponent<Selectable>()?.Select();
            statusText.text = "Choose a resource pack";
            return true;
        }

        public void ShowPurchaseResult(ShopPurchaseResult result) =>
            statusText.text = result.IsSuccess ? "Purchase complete" : result.Status.ToString();

        public void SetPurchaseInteractable(bool interactable)
        {
            if (interactable)
                foreach (var holder in rewardHolders)
                    if (holder != null) holder.RefreshBalance();
            foreach (ShopOfferItem item in items)
            {
                item.SetPurchaseInteractable(interactable);
            }
        }

        public void Close()
        {
            CancelPresentation();
            Hide().Forget();
        }

        protected override void OnDestroy()
        {
            CancelPresentation();
            foreach (ShopOfferItem item in items)
            {
                if (item == null) continue;
                item.SetIcon(null);
                Destroy(item.gameObject);
            }
            foreach (Sprite sprite in icons.Values) if (sprite != null) Destroy(sprite);
            icons.Clear();
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

        private void RequestPurchase(string offerId)
        {
            rewardSource = items.Find(item => item.OfferId == offerId)?.RewardSource;
            PurchaseRequested?.Invoke(offerId);
        }
        private void RequestClose() => CloseRequested?.Invoke();

        private Sprite ResolveIcon(string key)
        {
            if (iconAtlas == null || string.IsNullOrWhiteSpace(key)) return fallbackIcon;
            if (!icons.TryGetValue(key, out Sprite sprite))
            {
                sprite = iconAtlas.GetSprite(key);
                icons.Add(key, sprite);
                if (sprite == null)
                    Debug.LogWarning($"Shop icon '{key}' was not found in atlas '{iconAtlas.name}'.", this);
            }
            return sprite != null ? sprite : fallbackIcon;
        }
    }
}
