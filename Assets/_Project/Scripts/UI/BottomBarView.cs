using System.Collections.Generic;
using System.Text;
using Buzzfield.Bees;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Upgrades;
using UnityEngine;

namespace Buzzfield.UI
{
    /// <summary>
    /// The four upgrade buttons. Events only mark the bar dirty; it redraws at most once per
    /// frame. Honey changes only toggle affordability; texts are rebuilt when levels or bee
    /// counts change, which is rare.
    /// </summary>
    public sealed class BottomBarView : MonoBehaviour
    {
        [SerializeField] private UpgradeButtonView addBeeButton;
        [SerializeField] private UpgradeButtonView speedButton;
        [SerializeField] private UpgradeButtonView evolveButton;
        [SerializeField] private UpgradeButtonView honeyValueButton;

        private readonly StringBuilder builder = new StringBuilder(32);
        private UpgradeManager upgrades;
        private EconomyManager economy;
        private BeeManager bees;
        private BeeSettings beeSettings;
        private bool textsDirty;
        private bool stateDirty;

        public void Init(UpgradeManager upgradeManager, EconomyManager economyManager, BeeManager beeManager, BeeSettings settings)
        {
            Unsubscribe();
            upgrades = upgradeManager;
            economy = economyManager;
            bees = beeManager;
            beeSettings = settings;

            addBeeButton.Init(Strings.UpgradeAddBee, () => upgrades.TryAddBee());
            speedButton.Init(Strings.UpgradeSpeed, () => upgrades.TryBuySpeed());
            evolveButton.Init(Strings.UpgradeEvolve, () => upgrades.TryEvolve());
            honeyValueButton.Init(Strings.UpgradeHoneyValue, () => upgrades.TryBuyHoneyValue());

            economy.OnHoneyChanged += HandleHoneyChanged;
            upgrades.OnUpgradesChanged += MarkTextsDirty;
            bees.OnBeesChanged += MarkTextsDirty;
            MarkTextsDirty();
        }

        private void LateUpdate()
        {
            if (upgrades == null)
                return;
            if (textsDirty)
                RefreshTexts();
            if (stateDirty)
                RefreshState();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void HandleHoneyChanged(BigNumber _) => stateDirty = true;

        private void MarkTextsDirty()
        {
            textsDirty = true;
            stateDirty = true;
        }

        private void RefreshTexts()
        {
            textsDirty = false;

            addBeeButton.SetTexts(
                string.Format(Strings.BeeCountFormat, bees.Count, beeSettings.MaxBees),
                bees.IsAtCap ? Strings.Max : NumberFormat.Abbreviate(upgrades.AddBeeCost));

            speedButton.SetTexts(
                string.Format(Strings.LevelFormat, upgrades.SpeedLevel),
                upgrades.SpeedMaxed ? Strings.Max : NumberFormat.Abbreviate(upgrades.SpeedCost));

            evolveButton.SetTexts(
                TierCounts(),
                upgrades.EvolveAvailable ? NumberFormat.Abbreviate(upgrades.EvolveCost) : Strings.EvolveNeedsBees);

            honeyValueButton.SetTexts(
                string.Format(Strings.LevelFormat, upgrades.HoneyValueLevel),
                upgrades.HoneyValueMaxed ? Strings.Max : NumberFormat.Abbreviate(upgrades.HoneyValueCost));
        }

        private void RefreshState()
        {
            stateDirty = false;
            addBeeButton.SetInteractable(upgrades.CanAddBee);
            speedButton.SetInteractable(upgrades.CanBuySpeed);
            evolveButton.SetInteractable(upgrades.CanEvolve);
            honeyValueButton.SetInteractable(upgrades.CanBuyHoneyValue);
        }

        private string TierCounts()
        {
            builder.Clear();
            IReadOnlyList<int> counts = bees.TierCounts;
            for (int i = 0; i < counts.Count; i++)
            {
                if (i > 0)
                    builder.Append(Strings.TierCountSeparator);
                string tag = i < Strings.TierShort.Length ? Strings.TierShort[i] : (i + 1).ToString();
                builder.AppendFormat(Strings.TierCountFormat, tag, counts[i]);
            }
            return builder.ToString();
        }

        private void Unsubscribe()
        {
            if (economy != null)
                economy.OnHoneyChanged -= HandleHoneyChanged;
            if (upgrades != null)
                upgrades.OnUpgradesChanged -= MarkTextsDirty;
            if (bees != null)
                bees.OnBeesChanged -= MarkTextsDirty;
        }
    }
}
