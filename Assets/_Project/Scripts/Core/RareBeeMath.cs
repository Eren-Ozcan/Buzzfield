using System;
using System.Collections.Generic;

namespace Buzzfield.Core
{
    /// <summary>
    /// Rare bees (docs/DEWDROPS.md 4.2): species bought with Dewdrops that move into the hive for
    /// good. A species is at level 0 until it joins (its unlock price, sometimes free), then every
    /// species climbs the same list of level costs. Each level adds the same bonus; bonuses add up,
    /// they do not compound.
    /// </summary>
    public static class RareBeeMath
    {
        /// <summary>
        /// Dewdrops for the next step of a species at <paramref name="level"/>: the unlock price at
        /// level 0, then entry level - 1 of <paramref name="levelCosts"/> (the last entry repeats);
        /// -1 at the top level.
        /// </summary>
        public static long NextCost(int level, int maxLevel, long unlockCost, IReadOnlyList<int> levelCosts)
        {
            if (level >= maxLevel)
                return -1;
            if (level <= 0)
                return Math.Max(0, unlockCost);
            if (levelCosts == null || levelCosts.Count == 0)
                return -1;
            return Math.Max(0, levelCosts[Math.Min(level - 1, levelCosts.Count - 1)]);
        }

        /// <summary>Dewdrops from level 1 to <paramref name="maxLevel"/>, the unlock price left out.</summary>
        public static long LevelsCost(int maxLevel, IReadOnlyList<int> levelCosts)
        {
            long total = 0;
            for (int level = 1; level < maxLevel; level++)
            {
                long cost = NextCost(level, maxLevel, 0, levelCosts);
                if (cost < 0)
                    break;
                total += cost;
            }
            return total;
        }

        /// <summary>Summed bonus at <paramref name="level"/>: perLevel * level, nothing at level 0.</summary>
        public static double Effect(double perLevel, int level) => perLevel * Math.Max(0, level);

        /// <summary>A saved level, within 0..<paramref name="maxLevel"/>.</summary>
        public static int ClampLevel(int level, int maxLevel) => Math.Min(Math.Max(0, level), Math.Max(0, maxLevel));

        /// <summary>The species can join once the player has reached garden <paramref name="unlockGarden"/> (1 = the first).</summary>
        public static bool IsAvailable(int gardenNumber, int unlockGarden) => gardenNumber >= unlockGarden;

        /// <summary>Cost factor of a discount in percent: 20 is 0.8, and a cost never drops below zero.</summary>
        public static double DiscountFactor(double percent) => Math.Max(0, 1 - Math.Max(0, percent) / 100.0);

        /// <summary>Offline efficiency (0..1) raised by <paramref name="points"/> percentage points, at most 100%.</summary>
        public static double RaisedEfficiency(double efficiency, double points) =>
            Math.Min(1, Math.Max(0, efficiency) + Math.Max(0, points) / 100.0);
    }
}
