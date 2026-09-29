using System;

namespace Buzzfield.Core
{
    /// <summary>
    /// Garden bloom rules (design doc section 3): per-flower progress, garden progress and
    /// how strongly a flower greens the ground around it.
    /// </summary>
    public static class BloomMath
    {
        /// <summary>
        /// Flower bloom after bees take <paramref name="nectar"/> units from it: a flower blooms
        /// once <paramref name="nectarToBloom"/> units have been collected (sooner with a
        /// <paramref name="multiplier"/> above 1). Clamped to 1; never decreases.
        /// </summary>
        /// <remarks>
        /// Progress follows the nectar, not the number of visits, so evolving into bees that
        /// carry more per trip never slows the bloom down.
        /// </remarks>
        public static float AddNectar(float bloom, double nectar, float nectarToBloom, float multiplier)
        {
            if (nectar <= 0 || nectarToBloom <= 0f || multiplier <= 0f)
                return bloom;
            return (float)Math.Min(1.0, bloom + nectar * multiplier / nectarToBloom);
        }

        /// <summary>Garden bloom = bloomed slots / all slots, sprout slots included.</summary>
        public static float GardenFraction(int bloomed, int total)
        {
            if (total <= 0)
                return 0f;
            return Math.Min(1f, Math.Max(0, bloomed) / (float)total);
        }

        /// <summary>Whole percent shown in the HUD. Only 100 when every slot has bloomed.</summary>
        public static int DisplayPercent(int bloomed, int total)
        {
            if (total <= 0)
                return 0;
            return Math.Min(100, Math.Max(0, bloomed) * 100 / total);
        }

        /// <summary>
        /// Green amount a flower gives a tile at squared distance <paramref name="distanceSq"/>:
        /// its bloom times a smooth falloff that is 1 at the flower and 0 at <paramref name="radius"/>.
        /// </summary>
        public static float Influence(float bloom, float distanceSq, float radius)
        {
            if (bloom <= 0f || radius <= 0f || distanceSq >= radius * radius)
                return 0f;
            float t = 1f - (float)Math.Sqrt(distanceSq) / radius;
            return bloom * t * t * (3f - 2f * t);
        }
    }
}
