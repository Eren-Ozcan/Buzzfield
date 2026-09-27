using System;

namespace Buzzfield.Core
{
    /// <summary>
    /// Tap boost timer: a tap on the world speeds all bees up for a while; the cooldown
    /// runs from the moment of the tap. Times are game seconds passed in by the caller.
    /// </summary>
    public sealed class TapBoost
    {
        readonly double duration;
        readonly double cooldown;
        readonly double multiplier;

        public TapBoost(double durationSeconds, double cooldownSeconds, double speedMultiplier)
        {
            duration = Math.Max(0, durationSeconds);
            // A cooldown shorter than the boost would allow stacking taps.
            cooldown = Math.Max(duration, cooldownSeconds);
            multiplier = Math.Max(1, speedMultiplier);
            Reset();
        }

        public double ActiveUntil { get; private set; }
        public double ReadyAt { get; private set; }

        public bool IsActive(double now) => now < ActiveUntil;
        public bool IsReady(double now) => now >= ReadyAt;

        public bool TryActivate(double now)
        {
            if (!IsReady(now))
                return false;
            ActiveUntil = now + duration;
            ReadyAt = now + cooldown;
            return true;
        }

        public double SpeedMultiplier(double now) => IsActive(now) ? multiplier : 1;

        /// <summary>Boost time left, 1 right after the tap down to 0.</summary>
        public double ActiveFraction(double now) =>
            duration > 0 && IsActive(now) ? (ActiveUntil - now) / duration : 0;

        /// <summary>Cooldown progress, 0 right after the tap up to 1 when ready again.</summary>
        public double CooldownFraction(double now)
        {
            if (IsReady(now) || cooldown <= 0)
                return 1;
            return 1 - (ReadyAt - now) / cooldown;
        }

        /// <summary>Ends any boost and cooldown (Queen move).</summary>
        public void Reset()
        {
            ActiveUntil = double.NegativeInfinity;
            ReadyAt = double.NegativeInfinity;
        }
    }
}
