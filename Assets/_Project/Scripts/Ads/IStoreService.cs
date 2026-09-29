using System;
using System.Collections.Generic;

namespace Buzzfield.Ads
{
    public enum PurchaseStatus
    {
        Success,
        Cancelled,
        Failed,
        /// <summary>Paid with a slow method; the product arrives later as a deferred purchase.</summary>
        Pending,
    }

    /// <summary>Outcome of one purchase.</summary>
    public readonly struct PurchaseResult
    {
        public PurchaseResult(string productId, PurchaseStatus status, string error = null)
        {
            ProductId = productId;
            Status = status;
            Error = error;
        }

        public string ProductId { get; }
        public PurchaseStatus Status { get; }

        /// <summary>Store message for a failure; null otherwise.</summary>
        public string Error { get; }

        public bool IsSuccess => Status == PurchaseStatus.Success;
    }

    /// <summary>A product as the store reports it: the catalog entry plus the localized price.</summary>
    public readonly struct StoreProductInfo
    {
        public StoreProductInfo(StoreProduct definition, string priceText, bool available)
        {
            Definition = definition;
            PriceText = priceText;
            Available = available;
        }

        public StoreProduct Definition { get; }
        public string Id => Definition.id;

        /// <summary>Localized price from the store, or the catalog's fallback price.</summary>
        public string PriceText { get; }

        /// <summary>The store knows the product and it can be bought.</summary>
        public bool Available { get; }
    }

    /// <summary>
    /// What the game needs from an in-app purchase SDK. Shaped after Unity IAP so it maps
    /// call for call: <see cref="Initialize"/> is store initialization with the catalog,
    /// <see cref="GetProducts"/> reads the fetched products, <see cref="Purchase"/> is
    /// <c>InitiatePurchase</c> completed by <c>ProcessPurchase</c> or the purchase-failed
    /// callback, and <see cref="RestorePurchases"/> is the store's restore call. Callbacks
    /// arrive on the main thread.
    /// </summary>
    public interface IStoreService
    {
        bool IsInitialized { get; }

        /// <summary>A purchase is running; the store takes one at a time.</summary>
        bool IsPurchasing { get; }

        /// <summary>
        /// Raised for a purchase the game did not start in this session, such as one that
        /// finished after the app was closed or a pending payment that cleared. Grant it like
        /// a <see cref="Purchase"/> result.
        /// </summary>
        event Action<PurchaseResult> DeferredPurchaseCompleted;

        /// <summary>Connects to the store and fetches <paramref name="catalog"/>; the callback reports success.</summary>
        void Initialize(IReadOnlyList<StoreProduct> catalog, Action<bool> onDone);

        /// <summary>The catalog products; empty before <see cref="Initialize"/> succeeds.</summary>
        IReadOnlyList<StoreProductInfo> GetProducts();

        /// <summary>Buys one product. The callback runs once with the result.</summary>
        void Purchase(string productId, Action<PurchaseResult> onComplete);

        /// <summary>Asks the store for owned non-consumables; the callback gets their ids.</summary>
        void RestorePurchases(Action<IReadOnlyList<string>> onDone);
    }
}
