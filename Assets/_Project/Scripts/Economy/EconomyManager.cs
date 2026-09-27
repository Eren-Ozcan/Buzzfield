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
        private readonly EconomySettings settings;
        private readonly RollingRate honeyRate;

        public EconomyManager(EconomySettings settings, double now)
        {
            this.settings = settings;
            Honey = settings.StartingHoney;
            honeyRate = new RollingRate(settings.HoneyRateWindowSeconds, settings.HoneyRateBuckets, now);
        }

        public BigNumber Honey { get; private set; }

        /// <summary>Honey earned since the last Queen move; feeds the Royal Jelly formula.</summary>
        public BigNumber RunHoneyEarned { get; private set; }

        /// <summary>Honey earned from deposits over all runs; survives the Queen move.</summary>
        public BigNumber LifetimeHoneyEarned { get; private set; }

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
            LifetimeHoneyEarned += honey;
            Honey += honey;
            OnHoneyChanged?.Invoke(Honey);
            return honey;
        }

        public bool CanAfford(BigNumber cost) => Honey >= cost;

        /// <summary>Removes <paramref name="cost"/> if the balance covers it.</summary>
        public bool TrySpend(BigNumber cost)
        {
            if (cost.IsNegative || Honey < cost)
                return false;
            Honey -= cost;
            OnHoneyChanged?.Invoke(Honey);
            return true;
        }

        /// <summary>Adds honey that did not come from a deposit (offline earnings, store grants).
        /// Not counted in honey/sec or the run total.</summary>
        public void Grant(BigNumber amount)
        {
            if (amount.IsZero || amount.IsNegative)
                return;
            Honey += amount;
            OnHoneyChanged?.Invoke(Honey);
        }

        public BigNumber HoneyPerSecond(double now) => honeyRate.PerSecond(now);

        /// <summary>Loads saved balances. Negative values (a damaged save) read as zero.</summary>
        public void Restore(BigNumber honey, BigNumber runHoneyEarned, BigNumber lifetimeHoneyEarned)
        {
            Honey = honey.IsNegative ? BigNumber.Zero : honey;
            RunHoneyEarned = runHoneyEarned.IsNegative ? BigNumber.Zero : runHoneyEarned;
            LifetimeHoneyEarned = BigNumber.Max(lifetimeHoneyEarned, RunHoneyEarned);
            OnHoneyChanged?.Invoke(Honey);
        }

        /// <summary>Queen move: back to the starting honey with an empty run total and rate.
        /// Multipliers are owned by their systems and reset there.</summary>
        public void ResetRun(double now)
        {
            Honey = settings.StartingHoney;
            RunHoneyEarned = BigNumber.Zero;
            honeyRate.Reset(now);
            OnHoneyChanged?.Invoke(Honey);
        }
    }
}
