using System;
using System.Collections.Generic;

namespace Buzzfield.Core
{
    /// <summary>Store product ids; they must match the ids in the store catalog and the store consoles.</summary>
    public static class ProductIds
    {
        public const string RemoveAds = "remove_ads";
        public const string PermanentHoney2x = "permanent_2x_honey";
        public const string StarterPack = "starter_pack";
    }

    /// <summary>
    /// What the player bought. Saved with the game and kept through every Queen move.
    /// The starter pack is a consumable offered once, so it is tracked here as well. Other
    /// consumables (Dewdrop packs) can be bought again and again, so they are not tracked;
    /// instead the store orders already handed out are remembered, so an order the store
    /// delivers twice (a kill between grant and confirm) is only granted once.
    /// </summary>
    [Serializable]
    public sealed class Entitlements
    {
        /// <summary>How many order ids are remembered; the store redelivers within minutes, not hundreds of orders.</summary>
        public const int OrderMemory = 32;

        public bool removeAds;
        public bool permanentHoney2x;
        public bool starterPackBought;

        /// <summary>Ids of the latest store orders already granted, oldest first.</summary>
        public List<string> grantedOrders = new List<string>();

        /// <summary>The product is owned once at most (non-consumables and the starter pack).</summary>
        public static bool Tracks(string productId) =>
            productId == ProductIds.RemoveAds || productId == ProductIds.PermanentHoney2x || productId == ProductIds.StarterPack;

        /// <summary>The product was bought (for the starter pack: the one-time offer is used).</summary>
        public bool Owns(string productId)
        {
            switch (productId)
            {
                case ProductIds.RemoveAds: return removeAds;
                case ProductIds.PermanentHoney2x: return permanentHoney2x;
                case ProductIds.StarterPack: return starterPackBought;
                default: return false;
            }
        }

        /// <summary>Records a purchase; returns true when it was not owned before.</summary>
        public bool Grant(string productId)
        {
            if (Owns(productId))
                return false;
            switch (productId)
            {
                case ProductIds.RemoveAds: removeAds = true; return true;
                case ProductIds.PermanentHoney2x: permanentHoney2x = true; return true;
                case ProductIds.StarterPack: starterPackBought = true; return true;
                default: return false;
            }
        }

        /// <summary>The store order <paramref name="orderId"/> was already granted. Orders without an id never match.</summary>
        public bool HasGrantedOrder(string orderId) =>
            !string.IsNullOrEmpty(orderId) && grantedOrders != null && grantedOrders.Contains(orderId);

        /// <summary>Remembers a granted order, forgetting the oldest beyond <see cref="OrderMemory"/>.</summary>
        public void RecordOrder(string orderId)
        {
            if (string.IsNullOrEmpty(orderId) || HasGrantedOrder(orderId))
                return;
            grantedOrders ??= new List<string>();
            grantedOrders.Add(orderId);
            if (grantedOrders.Count > OrderMemory)
                grantedOrders.RemoveRange(0, grantedOrders.Count - OrderMemory);
        }

        public void CopyFrom(Entitlements other)
        {
            if (ReferenceEquals(other, this))
                return;
            removeAds = other != null && other.removeAds;
            permanentHoney2x = other != null && other.permanentHoney2x;
            starterPackBought = other != null && other.starterPackBought;
            grantedOrders ??= new List<string>();
            grantedOrders.Clear();
            if (other?.grantedOrders != null)
                grantedOrders.AddRange(other.grantedOrders);
        }
    }
}
