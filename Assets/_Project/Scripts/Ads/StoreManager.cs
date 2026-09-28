using System;
using System.Collections.Generic;
using Buzzfield.Core;

namespace Buzzfield.Ads
{
    /// <summary>How the shop shows one product right now.</summary>
    public enum ShopItemState
    {
        /// <summary>Not offered: not in the catalog, or the one-time starter pack is used.</summary>
        Hidden,
        Owned,
        Buyable,
        /// <summary>A purchase is running.</summary>
        Busy,
        /// <summary>The store is not connected or does not know the product.</summary>
        Unavailable,
    }

    /// <summary>
    /// The game's single door to the store. Initializes the service with the catalog, keeps
    /// the <see cref="Entitlements"/> and records every successful purchase, restore or
    /// deferred purchase in them. <see cref="OnProductGranted"/> tells the game to hand out
    /// what the product gives (honey, Royal Jelly, multipliers) and to save.
    /// GameManager ticks it once per frame.
    /// </summary>
    public sealed class StoreManager
    {
        private readonly IStoreService service;
        private readonly StoreCatalog catalog;
        private readonly ITickableService tickable;
        private bool started;

        public StoreManager(IStoreService service, StoreCatalog catalog)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            tickable = service as ITickableService;
            service.DeferredPurchaseCompleted += HandleDeferredPurchase;
        }

        public Entitlements Entitlements { get; } = new Entitlements();

        public StoreCatalog Catalog => catalog;

        public bool IsReady => service.IsInitialized;

        public bool IsPurchasing => service.IsPurchasing;

        /// <summary>Entitlements, the store connection or the running purchase changed.</summary>
        public event Action OnChanged;

        /// <summary>A product was granted: bought, restored or completed later. Raised once per grant.</summary>
        public event Action<StoreProduct> OnProductGranted;

        /// <summary>A purchase the game started has finished, whatever the result.</summary>
        public event Action<PurchaseResult> OnPurchaseFinished;

        /// <summary>A restore has finished; the argument is how many products it newly granted.</summary>
        public event Action<int> OnRestoreFinished;

        /// <summary>Loads the saved entitlements.</summary>
        public void Restore(Entitlements saved)
        {
            Entitlements.CopyFrom(saved);
            OnChanged?.Invoke();
        }

        /// <summary>Connects to the store. Safe to call once per session.</summary>
        public void Start()
        {
            if (started)
                return;
            started = true;
            service.Initialize(catalog.Products, _ => OnChanged?.Invoke());
        }

        public ShopItemState GetState(string productId)
        {
            if (!catalog.TryGet(productId, out StoreProduct product))
                return ShopItemState.Hidden;
            if (Entitlements.Owns(productId))
                return product.type == StoreProductType.Consumable ? ShopItemState.Hidden : ShopItemState.Owned;
            if (!TryGetInfo(productId, out StoreProductInfo info) || !info.Available)
                return ShopItemState.Unavailable;
            return service.IsPurchasing ? ShopItemState.Busy : ShopItemState.Buyable;
        }

        /// <summary>Localized price from the store, else the catalog's fallback price.</summary>
        public string PriceText(string productId)
        {
            if (TryGetInfo(productId, out StoreProductInfo info) && !string.IsNullOrEmpty(info.PriceText))
                return info.PriceText;
            return catalog.TryGet(productId, out StoreProduct product) ? product.fallbackPrice : string.Empty;
        }

        /// <summary>Starts a purchase when the product is buyable; returns false otherwise.</summary>
        public bool TryPurchase(string productId)
        {
            if (GetState(productId) != ShopItemState.Buyable)
                return false;
            service.Purchase(productId, HandlePurchaseResult);
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Asks the store for owned non-consumables and grants the missing ones.</summary>
        public void RestorePurchases()
        {
            if (!service.IsInitialized)
            {
                OnRestoreFinished?.Invoke(0);
                return;
            }
            service.RestorePurchases(HandleRestored);
        }

        public void Tick(float unscaledDeltaTime) => tickable?.Tick(unscaledDeltaTime);

        public void Dispose() => service.DeferredPurchaseCompleted -= HandleDeferredPurchase;

        private bool TryGetInfo(string productId, out StoreProductInfo info)
        {
            IReadOnlyList<StoreProductInfo> products = service.GetProducts();
            for (int i = 0; i < products.Count; i++)
            {
                if (products[i].Id == productId)
                {
                    info = products[i];
                    return true;
                }
            }
            info = default;
            return false;
        }

        private void HandlePurchaseResult(PurchaseResult result)
        {
            if (result.IsSuccess)
                Grant(result.ProductId);
            OnChanged?.Invoke();
            OnPurchaseFinished?.Invoke(result);
        }

        private void HandleDeferredPurchase(PurchaseResult result)
        {
            if (!result.IsSuccess)
                return;
            Grant(result.ProductId);
            OnChanged?.Invoke();
        }

        private void HandleRestored(IReadOnlyList<string> ownedIds)
        {
            int granted = 0;
            if (ownedIds != null)
            {
                for (int i = 0; i < ownedIds.Count; i++)
                {
                    // Consumables are never restored, even if a store reports one.
                    if (catalog.TryGet(ownedIds[i], out StoreProduct product)
                        && product.type == StoreProductType.NonConsumable && Grant(ownedIds[i]))
                        granted++;
                }
            }
            OnChanged?.Invoke();
            OnRestoreFinished?.Invoke(granted);
        }

        /// <summary>Records the product and tells the game to hand it out; false when it was already owned.</summary>
        private bool Grant(string productId)
        {
            if (!catalog.TryGet(productId, out StoreProduct product) || !Entitlements.Grant(productId))
                return false;
            OnProductGranted?.Invoke(product);
            return true;
        }
    }
}
