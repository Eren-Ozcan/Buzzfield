using Buzzfield.Ads;
using Buzzfield.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// Shop button in the top bar and the shop panel behind it: one row per product, a
    /// restore button and a status line for the last purchase or restore. Store events only
    /// mark it dirty; the rows are refreshed at most once per frame while the panel is open.
    /// </summary>
    public sealed class ShopPanelView : MonoBehaviour
    {
        [Header("Top bar")]
        [SerializeField] private Button openButton;

        [Header("Panel")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private ShopItemView[] items;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button restoreButton;

        private StoreManager store;
        private bool dirty;

        public bool IsOpen => panel.activeSelf;

        public void Init(StoreManager storeManager)
        {
            Unsubscribe();
            store = storeManager;
            store.OnChanged += MarkDirty;
            store.OnPurchaseFinished += HandlePurchaseFinished;
            store.OnRestoreFinished += HandleRestoreFinished;

            AddListener(openButton, Open);
            AddListener(closeButton, Close);
            AddListener(restoreButton, Restore);

            StoreCatalog catalog = store.Catalog;
            for (int i = 0; i < items.Length; i++)
            {
                ShopItemView item = items[i];
                item.Init(Title(item.ProductId), Body(item.ProductId, catalog), Buy);
            }
            panel.SetActive(false);
            MarkDirty();
        }

        public void Open()
        {
            panel.SetActive(true);
            statusText.text = store.IsReady ? string.Empty : Strings.StoreConnecting;
            Refresh();
        }

        public void Close() => panel.SetActive(false);

        private void LateUpdate()
        {
            if (store == null || !dirty || !panel.activeSelf)
                return;
            Refresh();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            RemoveListener(openButton, Open);
            RemoveListener(closeButton, Close);
            RemoveListener(restoreButton, Restore);
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

        private void HandlePurchaseFinished(PurchaseResult result)
        {
            switch (result.Status)
            {
                case PurchaseStatus.Success: statusText.text = Strings.PurchaseThanks; break;
                case PurchaseStatus.Cancelled: statusText.text = Strings.PurchaseCancelled; break;
                default: statusText.text = Strings.PurchaseFailed; break;
            }
            MarkDirty();
        }

        private void HandleRestoreFinished(int granted)
        {
            statusText.text = granted > 0 ? Strings.RestoreDone : Strings.RestoreNothing;
            MarkDirty();
        }

        private void MarkDirty() => dirty = true;

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
