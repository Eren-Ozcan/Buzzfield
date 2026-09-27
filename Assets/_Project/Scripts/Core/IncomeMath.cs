using System;

namespace Buzzfield.Core
{
    /// <summary>
    /// Theoretical honey per second from the current bees, upgrades and garden (not the
    /// measured average). Offline earnings pay a share of this rate.
    /// </summary>
    public static class IncomeMath
    {
        /// <summary>
        /// Nectar one bee brings home per second: one load per round trip, where a trip is
        /// the flight there and back plus collecting and depositing.
        /// </summary>
        public static double BeeNectarPerSecond(double capacity, double speed, double roundTripDistance,
            double collectSeconds, double depositSeconds)
        {
            if (capacity <= 0 || speed <= 0)
                return 0;
            double trip = Math.Max(0, roundTripDistance) / speed + Math.Max(0, collectSeconds) + Math.Max(0, depositSeconds);
            return trip > 0 ? capacity / trip : 0;
        }

        /// <summary>
        /// Honey per second: bees cannot carry more than the flowers regrow, so the nectar
        /// flow is the smaller of the two, times its average value and the honey multiplier.
        /// </summary>
        /// <param name="beeNectarPerSecond">Sum of <see cref="BeeNectarPerSecond"/> over all bees.</param>
        /// <param name="flowerRegenPerSecond">Sum of regrowth over the active flowers.</param>
        /// <param name="averageValue">Honey per nectar unit, averaged over the flowers by regrowth.</param>
        public static BigNumber HoneyPerSecond(double beeNectarPerSecond, double flowerRegenPerSecond,
            double averageValue, BigNumber honeyValueMultiplier)
        {
            double nectar = Math.Min(Math.Max(0, beeNectarPerSecond), Math.Max(0, flowerRegenPerSecond));
            if (nectar <= 0 || averageValue <= 0)
                return BigNumber.Zero;
            return BigNumber.FromDouble(nectar * averageValue) * honeyValueMultiplier;
        }
    }
}
