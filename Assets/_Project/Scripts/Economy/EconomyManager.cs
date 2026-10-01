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

        /// <summary>Permanent honey per nectar multiplier from the Honey Value upgrade; resets with the run.</summary>
        public BigNumber HoneyValueMultiplier { get; set; } = BigNumber.One;

        /// <summary>Permanent multiplier from the Queen level; survives the Queen move.</summary>
        public double QueenMultiplier { get; set; } = 1;

        /// <summary>Every permanent multiplier on top of <see cref="HoneyValueMultiplier"/> (Queen level, store).</summary>
        public double PermanentMultiplier => QueenMultiplier * PurchasedMultiplier;

        /// <summary>Product of the timed boosts active right now.</summary>
        public double BoostMultiplier { get; set; } = 1;

        /// <summary>Permanent multiplier bought in the store; stacks with every other one and survives the Queen move.</summary>
        public double PurchasedMultiplier { get; set; } = 1;

        /// <summary>Every multiplier on honey per nectar unit right now: upgrade, permanent and timed boosts.</summary>
        public BigNumber TotalMultiplier => HoneyValueMultiplier * (BoostMultiplier * PermanentMultiplier);

        public event Action<BigNumber> OnHoneyChanged;

        /// <summary>Turns nectar the bees deposited into honey and returns the amount added.</summary>
        public BigNumber Deposit(double nectar, double flowerValue, double now)
        {
            BigNumber honey = HoneyFormula.Honey(nectar, flowerValue, HoneyValueMultiplier, BoostMultiplier * PermanentMultiplier);
            if (honey.IsZero)
                return honey;
            honeyRate.Add(now, honey);
            Earn(honey);
            return honey;
        }

        /// <summary>
        /// Turns nectar shaken out of a flower into honey and returns the amount added. Counts
        /// toward the run and lifetime totals like a deposit, but not toward honey per second:
        /// that rate measures the bees and sizes the shake, so it must not feed on itself.
        /// </summary>
        public BigNumber Harvest(double nectar, double flowerValue)
        {
            BigNumber honey = HoneyFormula.Honey(nectar, flowerValue, HoneyValueMultiplier, BoostMultiplier * PermanentMultiplier);
            if (!honey.IsZero)
                Earn(honey);
            return honey;
        }

        private void Earn(BigNumber honey)
        {
            RunHoneyEarned += honey;
            LifetimeHoneyEarned += honey;
            Honey += honey;
            OnHoneyChanged?.Invoke(Honey);
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
