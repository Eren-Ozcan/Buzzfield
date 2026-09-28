using System;
using System.Collections.Generic;

namespace Buzzfield.Core
{
    /// <summary>
    /// Queen level and Queen abilities (design doc section 3b): every Royal Jelly ever earned
    /// is Queen XP, each level adds a permanent honey bonus, and abilities are bought with
    /// Royal Jelly on their own cost curves.
    /// </summary>
    public static class QueenMath
    {
        /// <summary>Slack for curve values such as 99.99999 against 100 jelly.</summary>
        private const double JellyEpsilon = 1e-6;

        /// <summary>
        /// Total jelly needed for levels 1..<paramref name="maxLevel"/>, read from
        /// <paramref name="jellyForLevel"/>. Values are forced to rise, so a badly authored
        /// curve can never make a level cheaper than the one before it.
        /// </summary>
        public static double[] Thresholds(Func<int, double> jellyForLevel, int maxLevel)
        {
            if (jellyForLevel == null)
                throw new ArgumentNullException(nameof(jellyForLevel));
            var thresholds = new double[Math.Max(0, maxLevel)];
            double previous = 0;
            for (int i = 0; i < thresholds.Length; i++)
            {
                double value = jellyForLevel(i + 1);
                if (double.IsNaN(value) || value < previous)
                    value = previous;
                thresholds[i] = value;
                previous = value;
            }
            return thresholds;
        }

        /// <summary>Levels reached with <paramref name="lifetimeJelly"/>: 0 until the first threshold.</summary>
        public static int Level(BigNumber lifetimeJelly, IReadOnlyList<double> thresholds)
        {
            if (thresholds == null || lifetimeJelly.IsZero || lifetimeJelly.IsNegative)
                return 0;
            // Anything past a double's range is past every threshold.
            double jelly = lifetimeJelly.Exponent > 300 ? double.MaxValue : lifetimeJelly.ToDouble();
            int level = 0;
            while (level < thresholds.Count && jelly + JellyEpsilon >= thresholds[level])
                level++;
            return level;
        }

        /// <summary>Lifetime jelly for the next level, or -1 at the top level.</summary>
        public static double NextThreshold(int level, IReadOnlyList<double> thresholds)
        {
            if (thresholds == null || level < 0 || level >= thresholds.Count)
                return -1;
            return thresholds[level];
        }

        /// <summary>Permanent honey multiplier: 1 + level * bonusPerLevel (0.1 = +10% per level).</summary>
        public static double HoneyMultiplier(int level, double bonusPerLevel) =>
            1 + Math.Max(0, level) * Math.Max(0, bonusPerLevel);

        /// <summary>Summed effect of an ability: perLevel * level. Abilities add, they do not compound.</summary>
        public static double AbilityEffect(double perLevel, int level) => perLevel * Math.Max(0, level);

        /// <summary>A saved ability level, within 0..<paramref name="maxLevel"/>.</summary>
        public static int ClampAbilityLevel(int level, int maxLevel) => Math.Min(Math.Max(0, level), Math.Max(0, maxLevel));

        /// <summary>Factor from a percent bonus: 10 is 1.1.</summary>
        public static double PercentFactor(double percent) => 1 + Math.Max(0, percent) / 100.0;
    }
}
