using System;
using Buzzfield.Ads;
using Buzzfield.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// Shop button in the top bar and the shop panel behind it: one row per product, a
    /// restore button, a status line for the last purchase or restore, and a Privacy button
    /// that reopens the ad consent choices where the law asks for one (EEA, UK). Store events only
    /// mark it dirty; the rows are refreshed at most once per frame while the panel is open.
    /// A badge on the shop button pops in once the store is ready with something to buy, and
    /// goes away for the rest of the session when the shop is opened.
    /// </summary>
    public sealed class ShopPanelView : MonoBehaviour
    {
        [Header("Top bar")]
        [SerializeField] private Button openButton;
        [Tooltip("Shown on the shop button until the shop is opened this session.")]
        [SerializeField] private GameObject newBadge;

        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private ShopItemView[] items;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button restoreButton;
        [SerializeField] private Button privacyButton;

        private StoreManager store;
        private Tweener tweener;
        private UiFeedbackSettings feedback;
        private bool dirty;
        private bool badgeDirty;
        private bool openedThisSession;
        private Func<bool> privacyRequired;
        private Action showPrivacy;

        public bool IsOpen => panel.activeSelf;

        /// <param name="privacyOptionsRequired">True where the Privacy button must be offered.</param>
        /// <param name="showPrivacyOptions">Reopens the consent choices.</param>
        public void Init(StoreManager storeManager, Tweener tweenRunner, UiFeedbackSettings feedbackSettings,
            Func<bool> privacyOptionsRequired, Action showPrivacyOptions)
        {
            Unsubscribe();
            store = storeManager;
            tweener = tweenRunner;
            feedback = feedbackSettings;
            privacyRequired = privacyOptionsRequired ?? (() => false);
            showPrivacy = showPrivacyOptions;
            store.OnChanged += MarkDirty;
            store.OnPurchaseFinished += HandlePurchaseFinished;
            store.OnRestoreFinished += HandleRestoreFinished;

            AddListener(openButton, Open);
            AddListener(closeButton, Close);
            AddListener(restoreButton, Restore);
            AddListener(privacyButton, ShowPrivacy);

            StoreCatalog catalog = store.Catalog;
            for (int i = 0; i < items.Length; i++)
            {
                ShopItemView item = items[i];
                item.Init(Title(item.ProductId), Body(item.ProductId, catalog), Buy);
            }
            panel.SetActive(false);
            newBadge.SetActive(false);
            MarkDirty();
        }

        public void Open()
        {
            openedThisSession = true;
            newBadge.SetActive(false);
            panel.SetActive(true);
            statusText.text = store.IsReady ? string.Empty : Strings.StoreConnecting;
            Refresh();
        }

        public void Close() => panel.SetActive(false);

        private void LateUpdate()
        {
            if (store == null)
                return;
            if (badgeDirty)
                RefreshBadge();
            if (dirty && panel.activeSelf)
                Refresh();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            RemoveListener(openButton, Open);
            RemoveListener(closeButton, Close);
            RemoveListener(restoreButton, Restore);
            RemoveListener(privacyButton, ShowPrivacy);
        }

        private void Refresh()
        {
            dirty = false;
            for (int i = 0; i < items.Length; i++)
            {
                string id = items[i].ProductId;
                items[i].Show(store.GetState(id), store.PriceText(id));
            }
            restoreButton.interactable = store.IsReady && !store.IsPurchasing;
            privacyButton.gameObject.SetActive(privacyRequired());
            if (store.IsReady && statusText.text == Strings.StoreConnecting)
                statusText.text = string.Empty;
        }

        private void Buy(string productId)
        {
            if (store.TryPurchase(productId))
                statusText.text = string.Empty;
        }

        private void Restore()
        {
            restoreButton.interactable = false;
            store.RestorePurchases();
        }

        private void ShowPrivacy() => showPrivacy?.Invoke();

        private void HandlePurchaseFinished(PurchaseResult result)
        {
            switch (result.Status)
            {
                case PurchaseStatus.Success: statusText.text = Strings.PurchaseThanks; break;
                case PurchaseStatus.Cancelled: statusText.text = Strings.PurchaseCancelled; break;
                case PurchaseStatus.Pending: statusText.text = Strings.PurchasePending; break;
                default: statusText.text = Strings.PurchaseFailed; break;
            }
            MarkDirty();
        }

        private void HandleRestoreFinished(int granted)
        {
            statusText.text = granted > 0 ? Strings.RestoreDone : Strings.RestoreNothing;
            MarkDirty();
        }

        private void MarkDirty()
        {
            dirty = true;
            badgeDirty = true;
        }

        private void RefreshBadge()
        {
            badgeDirty = false;
            bool show = !openedThisSession && store.IsReady && AnyBuyable();
            if (show == newBadge.activeSelf)
                return;
            newBadge.SetActive(show);
            if (show)
                tweener.PopIn(newBadge.transform, feedback.BadgePop, feedback.BadgePopDuration);
        }

        private bool AnyBuyable()
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (store.GetState(items[i].ProductId) == ShopItemState.Buyable)
                    return true;
            }
            return false;
        }

        private static string Title(string productId)
        {
            switch (productId)
            {
                case ProductIds.RemoveAds: return Strings.ProductRemoveAds;
                case ProductIds.PermanentHoney2x: return Strings.ProductHoney2x;
                case ProductIds.StarterPack: return Strings.ProductStarterPack;
                default: return productId;
            }
        }

        private static string Body(string productId, StoreCatalog catalog)
        {
            switch (productId)
            {
                case ProductIds.RemoveAds:
                    return Strings.ProductRemoveAdsBody;
                case ProductIds.PermanentHoney2x:
                    return string.Format(Strings.ProductHoney2xBodyFormat, catalog.PermanentHoneyMultiplier.ToString("0.#"));
                case ProductIds.StarterPack:
                    catalog.TryGet(productId, out StoreProduct pack);
                    return string.Format(Strings.ProductStarterPackBodyFormat,
                        NumberFormat.Abbreviate(BigNumber.FromDouble(pack.honeyGrant)), pack.jellyGrant);
                default:
                    return string.Empty;
            }
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.RemoveListener(action);
        }

        private void Unsubscribe()
        {
            if (store == null)
                return;
            store.OnChanged -= MarkDirty;
            store.OnPurchaseFinished -= HandlePurchaseFinished;
            store.OnRestoreFinished -= HandleRestoreFinished;
        }
    }
}
