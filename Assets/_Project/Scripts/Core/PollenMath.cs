using System;

namespace Buzzfield.Core
{
    /// <summary>
    /// Pollen shake: a swipe across a flower shakes pollen out of it for honey and bloom.
    /// Hit testing of one swipe stroke and the nectar one shake is worth.
    /// </summary>
    public static class PollenMath
    {
        /// <summary>
        /// True when the stroke from (ax, az) to (bx, bz) passes within <paramref name="radius"/>
        /// of the point (px, pz). A zero-length stroke is a tap at its start.
        /// </summary>
        public static bool StrokeHits(double ax, double az, double bx, double bz, double px, double pz, double radius)
        {
            double dx = bx - ax;
            double dz = bz - az;
            double lengthSquared = dx * dx + dz * dz;
            double t = lengthSquared > 0 ? ((px - ax) * dx + (pz - az) * dz) / lengthSquared : 0;
            t = Math.Max(0, Math.Min(1, t));
            double ex = ax + t * dx - px;
            double ez = az + t * dz - pz;
            return ex * ex + ez * ez <= radius * radius;
        }

        /// <summary>
        /// Nectar one shake is worth. One sweep over every active flower pays
        /// <paramref name="incomeSeconds"/> of the bees' honey per second; split over the flowers
        /// by value, that is the same nectar on each flower, so a richer flower pays more honey.
        /// Never below <paramref name="minimumNectar"/>, so shaking pays before the bees have earned.
        /// </summary>
        /// <param name="beeHoneyPerSecond">Measured honey per second from bee deposits.</param>
        /// <param name="activeValue">Sum of the active flowers' honey per nectar unit.</param>
        /// <param name="honeyMultiplier">Every multiplier on honey per nectar unit right now.</param>
        public static double ShakeNectar(BigNumber beeHoneyPerSecond, double incomeSeconds, double activeValue,
            BigNumber honeyMultiplier, double minimumNectar)
        {
            double floor = Math.Max(0, minimumNectar);
            if (incomeSeconds <= 0 || activeValue <= 0 || beeHoneyPerSecond.IsZero || beeHoneyPerSecond.IsNegative
                || honeyMultiplier.IsZero || honeyMultiplier.IsNegative)
                return floor;
            double nectar = (beeHoneyPerSecond / honeyMultiplier).ToDouble() * incomeSeconds / activeValue;
            return double.IsNaN(nectar) ? floor : Math.Max(floor, nectar);
        }
    }
}
