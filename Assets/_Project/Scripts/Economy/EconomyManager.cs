using System;
using Buzzfield.Core;

namespace Buzzfield.Economy
{
    /// <summary>
    /// Owns the honey balance and the measured honey per second. Plain class: the game
    /// passes in time, so it has no Unity lifecycle of its own.
    /// </summary>
    public sealed class EconomyManager
    {
        private readonly RollingRate honeyRate;

        public EconomyManager(EconomySettings settings, double now)
        {
            Honey = settings.StartingHoney;
            honeyRate = new RollingRate(settings.HoneyRateWindowSeconds, settings.HoneyRateBuckets, now);
        }

        public BigNumber Honey { get; private set; }

        /// <summary>Honey earned since the last Queen move; feeds the Royal Jelly formula.</summary>
        public BigNumber RunHoneyEarned { get; private set; }

        /// <summary>Permanent honey per nectar multiplier (Honey Value upgrade, Queen level).</summary>
        public BigNumber HoneyValueMultiplier { get; set; } = BigNumber.One;

        /// <summary>Product of the timed boosts active right now.</summary>
        public double BoostMultiplier { get; set; } = 1;

        public event Action<BigNumber> OnHoneyChanged;

        /// <summary>Turns deposited nectar into honey and returns the amount added.</summary>
        public BigNumber Deposit(double nectar, double flowerValue, double now)
        {
            BigNumber honey = HoneyFormula.Honey(nectar, flowerValue, HoneyValueMultiplier, BoostMultiplier);
            if (honey.IsZero)
                return honey;
            honeyRate.Add(now, honey);
            RunHoneyEarned += honey;
            Honey += honey;
            OnHoneyChanged?.Invoke(Honey);
            return honey;
        }

        public BigNumber HoneyPerSecond(double now) => honeyRate.PerSecond(now);
    }
}
