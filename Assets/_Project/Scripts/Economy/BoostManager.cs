using System;
using Buzzfield.Core;

namespace Buzzfield.Economy
{
    /// <summary>
    /// Timed boosts: the tap boost (bee speed, game time) and the rewarded 2x honey boost
    /// (honey, UTC so it runs on while the game is closed). GameManager ticks it and applies
    /// <see cref="SpeedMultiplier"/> to the bees.
    /// </summary>
    public sealed class BoostManager
    {
        private readonly BoostSettings settings;
        private readonly EconomyManager economy;

        public BoostManager(BoostSettings settings, EconomyManager economy)
        {
            this.settings = settings;
            this.economy = economy;
            Tap = new TapBoost(settings.TapDurationSeconds, settings.TapCooldownSeconds, settings.TapSpeedMultiplier);
        }

        public TapBoost Tap { get; }

        /// <summary>UTC end of the rewarded 2x honey boost; in the past when inactive.</summary>
        public double RewardedHoneyEndUtc { get; private set; }

        public float TapSpeedMultiplier => settings.TapSpeedMultiplier;
        public float RewardedHoneyMultiplier => settings.RewardedHoneyMultiplier;
        public float RewardedDurationSeconds => settings.RewardedDurationSeconds;

        /// <summary>Raised when a tap starts the boost.</summary>
        public event Action OnTapBoostStarted;

        public bool TryTapBoost(double gameTime)
        {
            if (!Tap.TryActivate(gameTime))
                return false;
            OnTapBoostStarted?.Invoke();
            return true;
        }

        public float SpeedMultiplier(double gameTime) => (float)Tap.SpeedMultiplier(gameTime);

        public bool IsRewardedHoneyActive(double utcNow) => utcNow < RewardedHoneyEndUtc;

        /// <summary>Seconds left on the rewarded honey boost; 0 when inactive.</summary>
        public double RewardedHoneySecondsLeft(double utcNow) => Math.Max(0, RewardedHoneyEndUtc - utcNow);

        /// <summary>Starts (or extends to) a full rewarded honey boost from now.</summary>
        public void StartRewardedHoney(double utcNow)
        {
            RewardedHoneyEndUtc = Math.Max(RewardedHoneyEndUtc, utcNow + settings.RewardedDurationSeconds);
        }

        /// <summary>Loads the saved end time; a value too far ahead (clock change) is cut to one full boost.</summary>
        public void RestoreRewardedHoney(double endUtc, double utcNow)
        {
            RewardedHoneyEndUtc = Math.Min(endUtc, utcNow + settings.RewardedDurationSeconds);
        }

        /// <summary>Updates the honey multiplier; call once per frame.</summary>
        public void Tick(double utcNow)
        {
            double multiplier = IsRewardedHoneyActive(utcNow) ? settings.RewardedHoneyMultiplier : 1;
            if (economy.BoostMultiplier != multiplier)
                economy.BoostMultiplier = multiplier;
        }

        /// <summary>Queen move: the tap boost ends; the rewarded boost keeps running.</summary>
        public void ResetTapBoost() => Tap.Reset();
    }
}
