using System;

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
    /// The starter pack is a consumable offered once, so it is tracked here as well.
    /// </summary>
    [Serializable]
    public sealed class Entitlements
    {
        public bool removeAds;
        public bool permanentHoney2x;
        public bool starterPackBought;

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

        public void CopyFrom(Entitlements other)
        {
            removeAds = other != null && other.removeAds;
            permanentHoney2x = other != null && other.permanentHoney2x;
            starterPackBought = other != null && other.starterPackBought;
        }
    }
}
