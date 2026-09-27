using System;
using Buzzfield.Core;
using Buzzfield.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// "Watch ad: x2 honey 5m" button above the bottom bar. While the boost runs it shows
    /// the time left instead and takes no presses; otherwise it is pressable only when an
    /// ad can be shown right now.
    /// </summary>
    public sealed class RewardedBoostView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color offerColor = new Color(0.45f, 0.8f, 0.95f);
        [SerializeField] private Color activeColor = new Color(0.45f, 0.9f, 0.35f);

        private BoostManager boosts;
        private Func<double> utcNow;
        private Func<bool> canWatchAd;
        private Func<bool> watchAd;
        private string offerText;
        private string multiplierText;
        private long shownSecondsLeft = -1;
        private bool? shownInteractable;

        private void Awake()
        {
            button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        /// <param name="watch">Starts the ad; returns false when none could be shown.</param>
        public void Init(BoostManager boostManager, Func<double> utcClock, Func<bool> canWatch, Func<bool> watch)
        {
            boosts = boostManager;
            utcNow = utcClock;
            canWatchAd = canWatch;
            watchAd = watch;
            multiplierText = boosts.RewardedHoneyMultiplier.ToString("0.#");
            offerText = string.Format(Strings.RewardedBoostOfferFormat, multiplierText,
                TimeFormat.Duration(boosts.RewardedDurationSeconds));
            shownSecondsLeft = -1;
            shownInteractable = null;
        }

        private void Update()
        {
            if (boosts == null)
                return;
            // Rounded up so the label never reads 0s while the boost still runs.
            long secondsLeft = (long)Math.Ceiling(boosts.RewardedHoneySecondsLeft(utcNow()));
            bool interactable = secondsLeft == 0 && canWatchAd();
            if (interactable != shownInteractable)
            {
                shownInteractable = interactable;
                button.interactable = interactable;
            }
            if (secondsLeft == shownSecondsLeft)
                return;
            bool wasActive = shownSecondsLeft > 0;
            shownSecondsLeft = secondsLeft;
            if (secondsLeft > 0)
            {
                label.text = string.Format(Strings.RewardedBoostActiveFormat, multiplierText, TimeFormat.Duration(secondsLeft));
                if (!wasActive)
                    background.color = activeColor;
            }
            else
            {
                label.text = offerText;
                background.color = offerColor;
            }
        }

        private void HandleClick()
        {
            if (watchAd == null || !watchAd())
                return;
            // Blocks a second press until the ad's own state takes over next frame.
            shownInteractable = false;
            button.interactable = false;
        }
    }
}
