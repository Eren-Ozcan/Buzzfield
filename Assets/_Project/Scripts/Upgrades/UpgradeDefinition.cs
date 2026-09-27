using UnityEngine;

namespace Buzzfield.Upgrades
{
    public enum UpgradeKind
    {
        AddBee,
        Speed,
        HoneyValue,
    }

    /// <summary>
    /// A levelled upgrade on the bottom bar. Cost = baseCost * costMultiplier^level.
    /// Evolve is not levelled; its cost lives on the target bee tier.
    /// </summary>
    [CreateAssetMenu(menuName = "Buzzfield/Upgrade", fileName = "Upgrade")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [SerializeField] private UpgradeKind kind;
        [SerializeField] private double baseCost = 10;
        [SerializeField, Min(1f)] private float costMultiplier = 1.15f;
        [Tooltip("Effect per level: +fraction for Speed and Honey Value (0.1 = +10%), bees for Add Bee.")]
        [SerializeField] private float effectPerLevel = 0.1f;
        [Tooltip("0 = no limit.")]
        [SerializeField, Min(0)] private int maxLevel;

        public UpgradeKind Kind => kind;
        public double BaseCost => baseCost;
        public float CostMultiplier => costMultiplier;
        public float EffectPerLevel => effectPerLevel;
        public int MaxLevel => maxLevel;
    }
}
