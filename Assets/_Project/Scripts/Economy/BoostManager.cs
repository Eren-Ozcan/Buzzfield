using System;
using Buzzfield.Core;

namespace Buzzfield.Economy
{
    /// <summary>
    /// The timed honey boosts: the rewarded 2x from an ad and the line of boosts bought with
    /// Dewdrops (<see cref="DewBoostQueue"/>). Both run on UTC so they keep going while the
    /// game is closed; GameManager ticks it and it sets the economy's boost multiplier to
    /// their product, so they stack with each other and with the bought 2x.
    /// </summary>
    public sealed class BoostManager
    {
        private readonly BoostSettings settings;
        private readonly EconomyManager economy;
        private readonly DewBoostQueue dewBoosts = new DewBoostQueue();

        public BoostManager(BoostSettings settings, EconomyManager economy)
        {
            this.settings = settings;
            this.economy = economy;
        }

        /// <summary>UTC end of the rewarded 2x honey boost; in the past when inactive.</summary>
        public double RewardedHoneyEndUtc { get; private set; }

        public float RewardedHoneyMultiplier => settings.RewardedHoneyMultiplier;
        public float RewardedDurationSeconds => settings.RewardedDurationSeconds;

        /// <summary>The Dewdrops boosts, strongest first; the first one runs.</summary>
        public DewBoostQueue DewBoosts => dewBoosts;

        /// <summary>A Dewdrops boost was bought or ran out.</summary>
        public event Action OnDewBoostsChanged;

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

        /// <summary>Adds a bought honey boost; it runs now if it is the strongest, else it waits in line.</summary>
        public void AddDewBoost(float multiplier, double seconds, double utcNow)
        {
            dewBoosts.Add(multiplier, seconds, utcNow);
            Apply(utcNow);
            OnDewBoostsChanged?.Invoke();
        }

        /// <summary>Loads the saved line as it was when saved; the next tick uses up the time since.</summary>
        public void RestoreDewBoosts(DewBoostEntry[] saved, double anchorUtc)
        {
            dewBoosts.Restore(saved, anchorUtc);
            OnDewBoostsChanged?.Invoke();
        }

        /// <summary>Updates the honey multiplier; call once per frame.</summary>
        public void Tick(double utcNow)
        {
            int before = dewBoosts.Count;
            dewBoosts.Advance(utcNow);
            Apply(utcNow);
            if (dewBoosts.Count != before)
                OnDewBoostsChanged?.Invoke();
        }

        private void Apply(double utcNow)
        {
            double rewarded = IsRewardedHoneyActive(utcNow) ? settings.RewardedHoneyMultiplier : 1;
            double multiplier = rewarded * dewBoosts.Multiplier;
            if (economy.BoostMultiplier != multiplier)
                economy.BoostMultiplier = multiplier;
        }
    }
}
