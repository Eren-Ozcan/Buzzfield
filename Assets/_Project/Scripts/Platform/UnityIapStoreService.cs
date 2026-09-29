using System;
using System.Collections.Generic;
using Buzzfield.Ads;
using UnityEngine;
using UnityEngine.Purchasing;
using GameStore = Buzzfield.Ads.IStoreService;

namespace Buzzfield.Platform
{
    /// <summary>
    /// Unity IAP 5 (<see cref="StoreController"/>) behind the game's store interface. Every
    /// order the store hands over is granted first and confirmed after, so a kill between
    /// the two only means the store delivers it again (grants are idempotent through
    /// <see cref="Buzzfield.Core.Entitlements"/>). Owned non-consumables the store reports
    /// at startup are granted too, which restores them after a reinstall.
    /// </summary>
    public sealed class UnityIapStoreService : GameStore
    {
        private readonly List<StoreProductInfo> products = new List<StoreProductInfo>();
        private readonly List<string> ownedIds = new List<string>();
        private readonly List<ProductDefinition> definitions = new List<ProductDefinition>();
        private IReadOnlyList<StoreProduct> catalog;
        private StoreController controller;
        private Action<bool> initDone;
        private string purchasingId;
        private Action<PurchaseResult> purchaseDone;
        private Action<IReadOnlyList<string>> restoreDone;

        public bool IsInitialized { get; private set; }
        public bool IsPurchasing => purchasingId != null;

        public event Action<PurchaseResult> DeferredPurchaseCompleted;

        public async void Initialize(IReadOnlyList<StoreProduct> productCatalog, Action<bool> onDone)
        {
            catalog = productCatalog ?? Array.Empty<StoreProduct>();
            initDone = onDone;
            controller = UnityIAPServices.StoreController();
            controller.OnProductsFetched += HandleProductsFetched;
            controller.OnProductsFetchFailed += HandleProductsFetchFailed;
            controller.OnPurchasePending += HandlePurchasePending;
            controller.OnPurchaseFailed += HandlePurchaseFailed;
            controller.OnPurchaseDeferred += HandlePurchaseDeferred;
            controller.OnPurchasesFetched += HandlePurchasesFetched;
            controller.OnPurchasesFetchFailed += HandlePurchasesFetchFailed;
            controller.OnStoreConnected += HandleStoreConnected;
            controller.OnStoreDisconnected += HandleStoreDisconnected;
            try
            {
                await controller.Connect();
            }
            catch (Exception e)
            {
                Debug.LogWarning("Store connection failed: " + e.Message);
                FinishInit(false);
                return;
            }

            definitions.Clear();
            for (int i = 0; i < catalog.Count; i++)
            {
                ProductType type = catalog[i].type == StoreProductType.Consumable ? ProductType.Consumable : ProductType.NonConsumable;
                definitions.Add(new ProductDefinition(catalog[i].id, type));
            }
            controller.FetchProducts(definitions);
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
            Product product = controller.GetProductById(productId);
            if (product == null || !product.availableToPurchase)
            {
                onComplete?.Invoke(new PurchaseResult(productId, PurchaseStatus.Failed, "Product unavailable."));
                return;
            }
            purchasingId = productId;
            purchaseDone = onComplete;
            controller.PurchaseProduct(product);
        }

        public void RestorePurchases(Action<IReadOnlyList<string>> onDone)
        {
            restoreDone = onDone;
#if UNITY_IOS
            controller.RestoreTransactions((_, _) => controller.FetchPurchases());
#else
            controller.FetchPurchases();
#endif
        }

        /// <summary>Billing came back after a drop: fetch the products again if they never loaded.</summary>
        private void HandleStoreConnected()
        {
            if (definitions.Count > 0 && !products.Exists(p => p.Available))
                controller.FetchProducts(definitions);
        }

        private void HandleStoreDisconnected(StoreConnectionFailureDescription failure) =>
            Debug.LogWarning("Store disconnected: " + failure.Message);

        private void HandleProductsFetched(List<Product> _)
        {
            RefreshProducts();
            // Redelivers unfinished orders and reports owned non-consumables.
            controller.FetchPurchases();
            FinishInit(true);
        }

        private void HandleProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogWarning("Store products failed to load: " + failure.FailureReason);
            RefreshProducts();
            FinishInit(products.Exists(p => p.Available));
        }

        private void RefreshProducts()
        {
            products.Clear();
            for (int i = 0; i < catalog.Count; i++)
            {
                Product product = controller.GetProductById(catalog[i].id);
                bool available = product != null && product.availableToPurchase;
                string price = available ? product.metadata.localizedPriceString : null;
                products.Add(new StoreProductInfo(catalog[i], price, available));
            }
            IsInitialized = true;
        }

        private void FinishInit(bool ok)
        {
            Action<bool> callback = initDone;
            initDone = null;
            callback?.Invoke(ok);
        }

        private void HandlePurchasePending(PendingOrder order)
        {
            // Grant (and let the game save) before the store hears the order is done.
            Complete(ProductIdOf(order), PurchaseStatus.Success, null);
            controller.ConfirmPurchase(order);
        }

        private void HandlePurchaseFailed(FailedOrder order)
        {
            PurchaseStatus status = order.FailureReason == PurchaseFailureReason.UserCancelled
                ? PurchaseStatus.Cancelled : PurchaseStatus.Failed;
            Complete(ProductIdOf(order), status, order.Details);
        }

        /// <summary>Paid with a slow method (cash, parental approval): it arrives later as a pending order.</summary>
        private void HandlePurchaseDeferred(DeferredOrder order) =>
            Complete(ProductIdOf(order), PurchaseStatus.Pending, null);

        private void HandlePurchasesFetched(Orders orders)
        {
            ownedIds.Clear();
            IReadOnlyList<ConfirmedOrder> confirmed = orders.ConfirmedOrders;
            for (int i = 0; i < confirmed.Count; i++)
            {
                string id = ProductIdOf(confirmed[i]);
                if (id != null && !ownedIds.Contains(id) && IsNonConsumable(id))
                    ownedIds.Add(id);
            }

            Action<IReadOnlyList<string>> callback = restoreDone;
            restoreDone = null;
            if (callback != null)
            {
                callback(ownedIds.ToArray());
                return;
            }
            // Startup: owned products the save does not know (a reinstall) come back on their own.
            for (int i = 0; i < ownedIds.Count; i++)
                DeferredPurchaseCompleted?.Invoke(new PurchaseResult(ownedIds[i], PurchaseStatus.Success));
        }

        private void HandlePurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            Debug.LogWarning("Store purchases failed to load: " + failure.Message);
            Action<IReadOnlyList<string>> callback = restoreDone;
            restoreDone = null;
            callback?.Invoke(Array.Empty<string>());
        }

        /// <summary>Ends the running purchase when it is this product; anything else arrived on its own.</summary>
        private void Complete(string productId, PurchaseStatus status, string error)
        {
            var result = new PurchaseResult(productId, status, error);
            if (productId != null && productId == purchasingId)
            {
                Action<PurchaseResult> callback = purchaseDone;
                purchasingId = null;
                purchaseDone = null;
                callback?.Invoke(result);
                return;
            }
            if (result.IsSuccess)
                DeferredPurchaseCompleted?.Invoke(result);
        }

        private bool IsNonConsumable(string id)
        {
            for (int i = 0; i < catalog.Count; i++)
            {
                if (catalog[i].id == id)
                    return catalog[i].type == StoreProductType.NonConsumable;
            }
            return false;
        }

        private static string ProductIdOf(Order order)
        {
            IReadOnlyList<CartItem> items = order?.CartOrdered?.Items();
            return items != null && items.Count > 0 ? items[0].Product?.definition.id : null;
        }
    }
}
