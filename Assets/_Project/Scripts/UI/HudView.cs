using Buzzfield.Core;
using Buzzfield.Economy;
using TMPro;
using UnityEngine;

namespace Buzzfield.UI
{
    /// <summary>
    /// Top bar readout: honey balance, measured honey per second and garden bloom, written without
    /// allocating (<see cref="NumberLabel"/>, TextMeshPro SetText). The balance text
    /// is rebuilt at most once per frame after OnHoneyChanged; the rate is re-read on a
    /// slow timer because it also decays while nothing is deposited. The balance pops when it
    /// grows, at most once per <see cref="UiFeedbackSettings.HoneyPopInterval"/>.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text honeyText;
        [SerializeField] private TMP_Text rateText;
        [SerializeField] private TMP_Text bloomText;
        [Tooltip("Stretched from the left; its right anchor follows the bloom fraction.")]
        [SerializeField] private RectTransform bloomFill;

        private EconomyManager economy;
        private NumberLabel honeyLabel;
        private NumberLabel rateLabel;
        private Tweener tweener;
        private UiFeedbackSettings feedback;
        private BigNumber shownHoney = BigNumber.Zero;
        private float popCooldown;
        private float refreshInterval;
        private float refreshTimer;
        private BigNumber shownRate = BigNumber.Zero;
        private bool honeyDirty;
        private int shownBloomPercent = -1;

        public void Init(EconomyManager economyManager, EconomySettings settings, Tweener tweenRunner, UiFeedbackSettings feedbackSettings)
        {
            Unsubscribe();
            economy = economyManager;
            tweener = tweenRunner;
            feedback = feedbackSettings;
            shownHoney = economy.Honey;
            honeyLabel ??= new NumberLabel(honeyText);
            rateLabel ??= new NumberLabel(rateText);
            if (bloomText != null && shownBloomPercent < 0)
                NumberLabel.Reserve(bloomText, Strings.BloomFormat.Length + 3);
            refreshInterval = settings.RateRefreshSeconds;
            economy.OnHoneyChanged += MarkHoneyDirty;
            honeyDirty = true;
            ShowRate(BigNumber.Zero);
        }

        private void Update()
        {
            if (economy == null)
                return;
            popCooldown -= Time.unscaledDeltaTime;
            // Many deposits can land in one frame; the text is rebuilt once.
            if (honeyDirty)
            {
                honeyDirty = false;
                BigNumber honey = economy.Honey;
                honeyLabel.ShowAbbreviated(honey);
                if (honey > shownHoney && popCooldown <= 0f)
                {
                    popCooldown = feedback.HoneyPopInterval;
                    tweener.Pop(honeyText.transform, feedback.HoneyPop, feedback.HoneyPopDuration);
                }
                shownHoney = honey;
            }
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

        /// <summary>Called when a flower blooms; text is rebuilt only when the whole percent changes.</summary>
        public void ShowBloom(int percent, float fraction)
        {
            if (bloomFill != null)
                bloomFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), bloomFill.anchorMax.y);
            if (percent == shownBloomPercent || bloomText == null)
                return;
            shownBloomPercent = percent;
            bloomText.SetText(Strings.BloomFormat, percent);
        }

        private void MarkHoneyDirty(BigNumber _) => honeyDirty = true;

        private void ShowRate(BigNumber rate)
        {
            shownRate = rate;
            rateLabel.ShowPerSecond(rate);
        }

        private void Unsubscribe()
        {
            if (economy != null)
                economy.OnHoneyChanged -= MarkHoneyDirty;
        }
    }
}
