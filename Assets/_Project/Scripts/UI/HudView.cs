using Buzzfield.Core;
using Buzzfield.Economy;
using TMPro;
using UnityEngine;

namespace Buzzfield.UI
{
    /// <summary>
    /// Top bar readout: honey balance and measured honey per second. The balance text
    /// changes only on OnHoneyChanged; the rate is re-read on a slow timer because it
    /// also decays while nothing is deposited.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text honeyText;
        [SerializeField] private TMP_Text rateText;

        private EconomyManager economy;
        private float refreshInterval;
        private float refreshTimer;
        private BigNumber shownRate = BigNumber.Zero;

        public void Init(EconomyManager economyManager, EconomySettings settings)
        {
            Unsubscribe();
            economy = economyManager;
            refreshInterval = settings.RateRefreshSeconds;
            economy.OnHoneyChanged += ShowHoney;
            ShowHoney(economy.Honey);
            ShowRate(BigNumber.Zero);
        }

        private void Update()
        {
            if (economy == null)
                return;
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
                return;
            refreshTimer = refreshInterval;
            BigNumber rate = economy.HoneyPerSecond(Time.timeAsDouble);
            if (rate != shownRate)
                ShowRate(rate);
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void ShowHoney(BigNumber honey)
        {
            honeyText.text = NumberFormat.Abbreviate(honey);
        }

        private void ShowRate(BigNumber rate)
        {
            shownRate = rate;
            rateText.text = NumberFormat.PerSecond(rate);
        }

        private void Unsubscribe()
        {
            if (economy != null)
                economy.OnHoneyChanged -= ShowHoney;
        }
    }
}
