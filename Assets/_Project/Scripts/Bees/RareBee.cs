using System.Collections.Generic;
using Buzzfield.Flowers;
using UnityEngine;

namespace Buzzfield.Bees
{
    /// <summary>What a rare bee's levels improve. New species reuse these effects via data.</summary>
    public enum RareBeeEffect
    {
        /// <summary>Nectar every bee carries per trip, in percent.</summary>
        CarryCapacityPercent,
        /// <summary>Honey from pollen shakes, in percent.</summary>
        ShakeHoneyPercent,
        /// <summary>Offline earnings efficiency, in percentage points.</summary>
        OfflineEfficiencyPoints,
        /// <summary>Honey from the species' <see cref="RareBee.Flowers"/>, in percent.</summary>
        FlowerHoneyPercent,
        /// <summary>Honey cost of Add Bee and Evolve, in percent off.</summary>
        UpgradeDiscountPercent,
        /// <summary>Royal Jelly from a Queen move, in percent.</summary>
        RoyalJellyPercent,
    }

    /// <summary>Ids of the authored species; saves and display names key on these.</summary>
    public static class RareBeeIds
    {
        public const string Bumblebee = "Bumblebee";
        public const string BlueBandedBee = "BlueBandedBee";
        public const string MasonBee = "MasonBee";
        public const string OrchidBee = "OrchidBee";
        public const string LeafcutterBee = "LeafcutterBee";
        public const string CarpenterBee = "CarpenterBee";
    }

    /// <summary>
    /// One rare bee species (docs/DEWDROPS.md 4.2): it joins for Dewdrops once the player reaches
    /// its garden, flies in the garden as one bee of its own and gives a permanent bonus that grows
    /// with its level. Kept through every Queen move.
    /// </summary>
    [CreateAssetMenu(menuName = "Buzzfield/Rare Bee", fileName = "RareBee")]
    public sealed class RareBee : ScriptableObject
    {
        [SerializeField] private string id = "rare_bee";
        [SerializeField] private RareBeeEffect effect;
        [Tooltip("Bonus per level, in the unit of the effect.")]
        [SerializeField, Min(0f)] private float effectPerLevel = 3f;
        [SerializeField, Min(1)] private int maxLevel = 10;
        [Tooltip("Garden number (1 = the first) the player must reach before the species can join.")]
        [SerializeField, Min(1)] private int unlockGarden = 2;
        [Tooltip("Dewdrops to join; 0 = free.")]
        [SerializeField, Min(0)] private int unlockCost;
        [Tooltip("FlowerHoneyPercent only: the flower types it boosts. Strings.RareEffectFlowerFormat names them.")]
        [SerializeField] private List<FlowerType> flowers = new List<FlowerType>();
        [Tooltip("Flight stats and prefab of the one bee the species adds to the garden. Not one of BeeSettings' tiers.")]
        [SerializeField] private BeeTier flight;

        public string Id => id;
        public RareBeeEffect Effect => effect;
        public float EffectPerLevel => effectPerLevel;
        public int MaxLevel => maxLevel;
        public int UnlockGarden => unlockGarden;
        public int UnlockCost => unlockCost;
        public IReadOnlyList<FlowerType> Flowers => flowers;
        public BeeTier Flight => flight;
    }
}
