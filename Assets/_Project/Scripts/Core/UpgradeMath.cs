using System;

namespace Buzzfield.Core
{
    /// <summary>Cost and effect curves of the levelled upgrades (design doc section 6).</summary>
    public static class UpgradeMath
    {
        /// <summary>cost = baseCost * multiplier^level.</summary>
        public static BigNumber Cost(double baseCost, double multiplier, int level)
        {
            if (level < 0)
                throw new ArgumentOutOfRangeException(nameof(level));
            return BigNumber.FromDouble(baseCost) * BigNumber.Pow(multiplier, level);
        }

        /// <summary>
        /// Multiplicative bonus: every level multiplies by (1 + perLevel), so +10% at level 3
        /// is 1.1^3 = 1.331. Used for flight speed, where the value stays small.
        /// </summary>
        public static double Compound(double perLevel, int level)
        {
            if (level < 0)
                throw new ArgumentOutOfRangeException(nameof(level));
            return Math.Pow(1 + perLevel, level);
        }

        /// <summary>Same curve as <see cref="Compound"/> for multipliers with no level cap (honey value).</summary>
        public static BigNumber CompoundBig(double perLevel, int level)
        {
            if (level < 0)
                throw new ArgumentOutOfRangeException(nameof(level));
            return BigNumber.Pow(1 + perLevel, level);
        }

        /// <summary>True when a level cap exists and has been reached. 0 = no cap.</summary>
        public static bool IsMaxed(int level, int maxLevel) => maxLevel > 0 && level >= maxLevel;
    }
}
