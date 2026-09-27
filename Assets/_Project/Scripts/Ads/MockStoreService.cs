using System;
using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Ads
{
    /// <summary>
    /// Editor and development stand-in for the store. Purchases finish after
    /// <see cref="StoreCatalog.MockDelaySeconds"/> with <see cref="StoreCatalog.MockPurchaseResult"/>
    /// (read live, so it can be switched in play mode). Owned non-consumables are kept in
    /// PlayerPrefs, apart from the save, like a store account: deleting the save and
    /// restoring brings them back.
    /// </summary>
    public sealed class MockStoreService : IStoreService, ITickableService
    {
        public const string DefaultAccountKey = "Buzzfield.MockStore.Owned";
        private const char Separator = ',';

        private readonly StoreCatalog settings;
        private readonly string accountKey;
        private readonly List<StoreProductInfo> products = new List<StoreProductInfo>();
        private readonly List<string> owned = new List<string>();
        private float purchaseLeft = -1f;
        private string pendingId;
        private Action<PurchaseResult> pendingCallback;

        public MockStoreService(StoreCatalog settings, string accountKey = DefaultAccountKey)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.accountKey = accountKey;
        }

        public bool IsInitialized { get; private set; }
        public bool IsPurchasing => purchaseLeft >= 0f;

        // The mock has no purchases outside the game's own calls.
#pragma warning disable 0067
        public event Action<PurchaseResult> DeferredPurchaseCompleted;
#pragma warning restore 0067

        /// <summary>Forgets every mock-owned product (tests, and resetting a dev device).</summary>
        public static void ClearAccount(string accountKey = DefaultAccountKey) => PlayerPrefs.DeleteKey(accountKey);

        public void Initialize(IReadOnlyList<StoreProduct> catalog, Action<bool> onDone)
        {
            products.Clear();
            if (catalog != null)
            {
                for (int i = 0; i < catalog.Count; i++)
                    products.Add(new StoreProductInfo(catalog[i], catalog[i].fallbackPrice, true));
            }
            LoadAccount();
            IsInitialized = true;
            onDone?.Invoke(true);
        }

        public IReadOnlyList<StoreProductInfo> GetProducts() => products;

        public void Purchase(string productId, Action<PurchaseResult> onComplete)
        {
            if (!IsInitialized)
            {
                onComplete?.Invoke(new PurchaseResult(productId, PurchaseStatus.Failed, "Store not initialized."));
                return;
            }
            if (IsPurchasing)
            {
                onComplete?.Invoke(new PurchaseResult(productId, PurchaseStatus.Failed, "Another purchase is running."));
                return;
            }
            if (!TryFind(productId, out _))
            {
                onComplete?.Invoke(new PurchaseResult(productId, PurchaseStatus.Failed, "Unknown product."));
                return;
            }
            pendingId = productId;
            pendingCallback = onComplete;
            purchaseLeft = settings.MockDelaySeconds;
        }

        public void RestorePurchases(Action<IReadOnlyList<string>> onDone)
        {
            LoadAccount();
            onDone?.Invoke(owned.ToArray());
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (purchaseLeft < 0f)
                return;
            purchaseLeft -= unscaledDeltaTime;
            if (purchaseLeft >= 0f)
                return;

            string id = pendingId;
            Action<PurchaseResult> callback = pendingCallback;
            pendingId = null;
            pendingCallback = null;
            PurchaseResult result = settings.MockPurchaseResult switch
            {
                StoreCatalog.MockResult.Cancel => new PurchaseResult(id, PurchaseStatus.Cancelled),
                StoreCatalog.MockResult.Fail => new PurchaseResult(id, PurchaseStatus.Failed, "Mock failure."),
                _ => new PurchaseResult(id, PurchaseStatus.Success),
            };
            if (result.IsSuccess && TryFind(id, out StoreProductInfo product)
                && product.Definition.type == StoreProductType.NonConsumable && !owned.Contains(id))
            {
                owned.Add(id);
                SaveAccount();
            }
            callback?.Invoke(result);
        }

        private bool TryFind(string id, out StoreProductInfo product)
        {
            for (int i = 0; i < products.Count; i++)
            {
                if (products[i].Id == id)
                {
                    product = products[i];
                    return true;
                }
            }
            product = default;
            return false;
        }

        private void LoadAccount()
        {
            owned.Clear();
            string stored = PlayerPrefs.GetString(accountKey, string.Empty);
            if (stored.Length == 0)
                return;
            foreach (string id in stored.Split(Separator))
            {
                if (id.Length > 0 && !owned.Contains(id))
                    owned.Add(id);
            }
        }

        private void SaveAccount()
        {
            PlayerPrefs.SetString(accountKey, string.Join(Separator.ToString(), owned));
            PlayerPrefs.Save();
        }
    }
}
