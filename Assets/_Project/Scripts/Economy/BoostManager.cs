using System;

namespace Buzzfield.Economy
{
    /// <summary>
    /// The rewarded 2x honey boost. It runs on UTC so it keeps going while the game is
    /// closed; GameManager ticks it and it sets the economy's boost multiplier.
    /// </summary>
    public sealed class BoostManager
    {
        private readonly BoostSettings settings;
        private readonly EconomyManager economy;

        public BoostManager(BoostSettings settings, EconomyManager economy)
        {
            this.settings = settings;
            this.economy = economy;
        }

        /// <summary>UTC end of the rewarded 2x honey boost; in the past when inactive.</summary>
        public double RewardedHoneyEndUtc { get; private set; }

        public float RewardedHoneyMultiplier => settings.RewardedHoneyMultiplier;
        public float RewardedDurationSeconds => settings.RewardedDurationSeconds;

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
    }
}
