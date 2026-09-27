using System;
using Buzzfield.Bees;
using Buzzfield.Core;
using Buzzfield.Economy;

namespace Buzzfield.Upgrades
{
    /// <summary>
    /// The four bottom-bar upgrades: levels, costs, spending and applying the effects.
    /// Plain class; the game passes in the systems it acts on.
    /// </summary>
    public sealed class UpgradeManager
    {
        private readonly UpgradeDefinition addBee;
        private readonly UpgradeDefinition speed;
        private readonly UpgradeDefinition honeyValue;
        private readonly BeeSettings beeSettings;
        private readonly EconomyManager economy;
        private readonly BeeManager bees;

        public UpgradeManager(UpgradeDefinition addBee, UpgradeDefinition speed, UpgradeDefinition honeyValue,
            BeeSettings beeSettings, EconomyManager economy, BeeManager bees)
        {
            this.addBee = addBee;
            this.speed = speed;
            this.honeyValue = honeyValue;
            this.beeSettings = beeSettings;
            this.economy = economy;
            this.bees = bees;
            ApplyEffects();
        }

        /// <summary>Bees bought with Add Bee this run; drives its cost, not the current bee count.</summary>
        public int BeesBought { get; private set; }
        public int SpeedLevel { get; private set; }
        public int HoneyValueLevel { get; private set; }

        /// <summary>Flight speed factor from the Speed upgrade.</summary>
        public float SpeedMultiplier { get; private set; } = 1f;

        /// <summary>Raised after any purchase or reset.</summary>
        public event Action OnUpgradesChanged;

        // ---- Add Bee ----

        public BigNumber AddBeeCost => UpgradeMath.Cost(addBee.BaseCost, addBee.CostMultiplier, BeesBought);
        public bool AddBeeAvailable => !bees.IsAtCap;
        public bool CanAddBee => AddBeeAvailable && economy.CanAfford(AddBeeCost);

        public bool TryAddBee()
        {
            if (!AddBeeAvailable || !economy.TrySpend(AddBeeCost))
                return false;
            bees.Spawn(0);
            BeesBought++;
            OnUpgradesChanged?.Invoke();
            return true;
        }

        // ---- Speed ----

        public BigNumber SpeedCost => UpgradeMath.Cost(speed.BaseCost, speed.CostMultiplier, SpeedLevel);
        public bool SpeedMaxed => UpgradeMath.IsMaxed(SpeedLevel, speed.MaxLevel);
        public bool CanBuySpeed => !SpeedMaxed && economy.CanAfford(SpeedCost);

        public bool TryBuySpeed()
        {
            if (SpeedMaxed || !economy.TrySpend(SpeedCost))
                return false;
            SpeedLevel++;
            ApplyEffects();
            OnUpgradesChanged?.Invoke();
            return true;
        }

        // ---- Honey Value ----

        public BigNumber HoneyValueCost => UpgradeMath.Cost(honeyValue.BaseCost, honeyValue.CostMultiplier, HoneyValueLevel);
        public bool HoneyValueMaxed => UpgradeMath.IsMaxed(HoneyValueLevel, honeyValue.MaxLevel);
        public bool CanBuyHoneyValue => !HoneyValueMaxed && economy.CanAfford(HoneyValueCost);

        public bool TryBuyHoneyValue()
        {
            if (HoneyValueMaxed || !economy.TrySpend(HoneyValueCost))
                return false;
            HoneyValueLevel++;
            ApplyEffects();
            OnUpgradesChanged?.Invoke();
            return true;
        }

        // ---- Evolve ----

        /// <summary>Tier that the next Evolve merges from, or -1 when no tier has three bees below the top.</summary>
        public int EvolveSourceTier => EvolveRules.SourceTier(bees.TierCounts);

        /// <summary>Cost of the next Evolve; depends on the target tier. Zero when none is possible.</summary>
        public BigNumber EvolveCost
        {
            get
            {
                int source = EvolveSourceTier;
                return source < 0 ? BigNumber.Zero : BigNumber.FromDouble(beeSettings.Tiers[source + 1].EvolveCost);
            }
        }

        public bool EvolveAvailable => EvolveSourceTier >= 0;
        public bool CanEvolve => EvolveAvailable && economy.CanAfford(EvolveCost);

        public bool TryEvolve()
        {
            int source = EvolveSourceTier;
            if (source < 0)
                return false;
            BigNumber cost = EvolveCost;
            if (!economy.CanAfford(cost))
                return false;
            if (!bees.TryEvolve(source))
                return false;
            economy.TrySpend(cost);
            OnUpgradesChanged?.Invoke();
            return true;
        }

        /// <summary>Back to level 0 on every upgrade (Queen move).</summary>
        public void ResetLevels()
        {
            BeesBought = 0;
            SpeedLevel = 0;
            HoneyValueLevel = 0;
            ApplyEffects();
            OnUpgradesChanged?.Invoke();
        }

        private void ApplyEffects()
        {
            SpeedMultiplier = (float)UpgradeMath.Compound(speed.EffectPerLevel, SpeedLevel);
            bees.SpeedMultiplier = SpeedMultiplier;
            economy.HoneyValueMultiplier = UpgradeMath.CompoundBig(honeyValue.EffectPerLevel, HoneyValueLevel);
        }
    }
}
