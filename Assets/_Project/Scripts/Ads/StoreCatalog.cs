using System;
using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Ads
{
    public enum StoreProductType
    {
        Consumable,
        NonConsumable,
    }

    /// <summary>One in-app product. Price text is a placeholder until the store returns localized prices.</summary>
    [Serializable]
    public struct StoreProduct
    {
        public string id;
        public StoreProductType type;
        public string fallbackPrice;
        [Tooltip("Dewdrops granted on purchase (Dewdrop packs, starter pack).")]
        public int dewGrant;
        [Tooltip("Honey granted on purchase, in seconds of the current offline rate (starter pack).")]
        public float honeySecondsGrant;
    }

    /// <summary>In-app products and mock store behaviour.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Store Catalog", fileName = "StoreCatalog")]
    public sealed class StoreCatalog : ScriptableObject
    {
        public enum MockResult
        {
            Success,
            Fail,
            Cancel,
        }

        [SerializeField] private List<StoreProduct> products = new List<StoreProduct>();
        [Tooltip("Honey multiplier of permanent_2x_honey; stacks with the rewarded boost.")]
        [SerializeField, Min(1f)] private float permanentHoneyMultiplier = 2f;
        [Tooltip("Least honey a honeySecondsGrant pays, for a garden that earns almost nothing yet.")]
        [SerializeField, Min(0f)] private double minimumHoneyGrant = 500;
        [Tooltip("Play time before the shop button may show its badge: nothing points at the shop in the first minutes.")]
        [SerializeField, Min(0f)] private float badgeAfterPlaySeconds = 300f;

        [Header("Mock service (editor and dev builds)")]
        [SerializeField, Min(0f)] private float mockDelaySeconds = 1f;
        [SerializeField] private MockResult mockResult;

        public IReadOnlyList<StoreProduct> Products => products;
        public float PermanentHoneyMultiplier => permanentHoneyMultiplier;
        public double MinimumHoneyGrant => minimumHoneyGrant;
        public float BadgeAfterPlaySeconds => badgeAfterPlaySeconds;
        public float MockDelaySeconds => mockDelaySeconds;
        public MockResult MockPurchaseResult => mockResult;

        public bool TryGet(string id, out StoreProduct product)
        {
            for (int i = 0; i < products.Count; i++)
            {
                if (products[i].id == id)
                {
                    product = products[i];
                    return true;
                }
            }
            product = default;
            return false;
        }
    }
}
