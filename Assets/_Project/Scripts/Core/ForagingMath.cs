using System;

namespace Buzzfield.Core
{
    /// <summary>Which flowers are worth a bee's trip (design doc section 4, targeting).</summary>
    public static class ForagingMath
    {
        /// <summary>
        /// Nectar a bee waits for before it flies to a flower: <paramref name="loadFraction"/> of a
        /// full load, where a full load is the bee's capacity or a full flower if that is smaller.
        /// Never below one unit.
        /// </summary>
        public static double MinLoad(double capacity, double maxNectar, double loadFraction)
        {
            double fullLoad = Math.Min(Math.Max(0, capacity), Math.Max(0, maxNectar));
            return Math.Max(1, Math.Max(0, loadFraction) * fullLoad);
        }

        /// <summary>
        /// True when the flower still holds a worthwhile load for one more bee after the
        /// <paramref name="assignedBees"/> already flying to it each take theirs. Without this,
        /// every bee piles onto the flowers nearest the hive and carries home a unit or two,
        /// so bigger bees earn no more than small ones.
        /// </summary>
        public static bool OffersLoad(double nectar, double maxNectar, int assignedBees, double capacity, double loadFraction) =>
            nectar >= MinLoad(capacity, maxNectar, loadFraction) * (Math.Max(0, assignedBees) + 1);
    }
}
