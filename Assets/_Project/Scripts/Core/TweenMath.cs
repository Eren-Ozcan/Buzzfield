using System;

namespace Buzzfield.Core
{
    /// <summary>
    /// Curves for the small UI tweens (scale pop, press, wiggle). Every curve takes a
    /// normalized time t, clamps it to 0..1 and lands exactly on its rest value at t = 1.
    /// </summary>
    public static class TweenMath
    {
        public static float Clamp01(float t) => t < 0f ? 0f : t > 1f ? 1f : t;

        public static float EaseOutQuad(float t)
        {
            t = Clamp01(t);
            float u = 1f - t;
            return 1f - u * u;
        }

        /// <summary>
        /// Scale factor of a pop: eases from <paramref name="from"/> back to 1 and adds a bump
        /// of <paramref name="amount"/> that peaks halfway. from = 1, amount = 0.2 goes 1, 1.2, 1.
        /// </summary>
        public static float Pop(float from, float amount, float t)
        {
            t = Clamp01(t);
            if (t >= 1f)
                return 1f;
            return from + (1f - from) * EaseOutQuad(t) + amount * (float)Math.Sin(Math.PI * t);
        }

        /// <summary>Factor that eases from <paramref name="from"/> to <paramref name="to"/> and stays there.</summary>
        public static float Hold(float from, float to, float t) => from + (to - from) * EaseOutQuad(t);

        /// <summary>Side-to-side swing in -1..1 that fades out: <paramref name="cycles"/> full swings over t = 0..1.</summary>
        public static float Wiggle(float t, float cycles)
        {
            t = Clamp01(t);
            if (t >= 1f)
                return 0f;
            return (float)Math.Sin(t * cycles * 2.0 * Math.PI) * (1f - t);
        }
    }
}
