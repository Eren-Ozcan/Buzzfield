using UnityEngine;

namespace Buzzfield.Economy
{
    /// <summary>What a Queen ability changes. New abilities reuse these effects via data.</summary>
    public enum QueenEffect
    {
        StartingWorkers,
        FlightSpeedPercent,
        OfflineCapHours,
        BloomSpeedPercent,
    }

    /// <summary>Ids of the authored abilities; saves and display names key on these.</summary>
    public static class QueenAbilityIds
    {
        public const string RoyalBrood = "RoyalBrood";
        public const string RoyalWings = "RoyalWings";
        public const string SweetMemory = "SweetMemory";
        public const string PollenTouch = "PollenTouch";
    }

    /// <summary>One permanent Queen ability bought with Royal Jelly.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Queen Ability", fileName = "QueenAbility")]
    public sealed class QueenAbility : ScriptableObject
    {
        [SerializeField] private string id = "ability";
        [SerializeField] private QueenEffect effect;
        [Tooltip("Effect gained per level, in the unit of the effect.")]
        [SerializeField] private float effectPerLevel = 1f;
        [SerializeField, Min(1)] private int maxLevel = 5;
        [Tooltip("Royal Jelly cost = baseCost * costMultiplier^level.")]
        [SerializeField] private double baseCost = 1;
        [SerializeField, Min(1f)] private float costMultiplier = 2f;

        public string Id => id;
        public QueenEffect Effect => effect;
        public float EffectPerLevel => effectPerLevel;
        public int MaxLevel => maxLevel;
        public double BaseCost => baseCost;
        public float CostMultiplier => costMultiplier;
    }
}
