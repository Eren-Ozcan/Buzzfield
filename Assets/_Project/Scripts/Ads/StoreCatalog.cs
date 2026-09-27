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
        [Tooltip("Honey granted on purchase (starter pack).")]
        public double honeyGrant;
        [Tooltip("Royal Jelly granted on purchase (starter pack).")]
        public int jellyGrant;
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

        [Header("Mock service (editor and dev builds)")]
        [SerializeField, Min(0f)] private float mockDelaySeconds = 1f;
        [SerializeField] private MockResult mockResult;

        public IReadOnlyList<StoreProduct> Products => products;
        public float MockDelaySeconds => mockDelaySeconds;
        public MockResult MockPurchaseResult => mockResult;
    }
}
