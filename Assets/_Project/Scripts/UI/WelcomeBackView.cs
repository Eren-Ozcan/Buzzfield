using System;
using Buzzfield.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// "Welcome back" panel: time away and the honey earned meanwhile. The honey is already
    /// credited when the panel opens, so closing the app with it open loses nothing.
    /// Each opening offers one rewarded ad that multiplies the offline honey. The first return
    /// also shows a one-time gift from the bees, credited with the offline honey.
    /// </summary>
    public sealed class WelcomeBackView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text awayText;
        [SerializeField] private TMP_Text honeyText;
        [SerializeField] private TMP_Text capText;
        [SerializeField] private Button collectButton;
        [SerializeField] private Button adButton;
        [SerializeField] private TMP_Text adLabel;
        [Tooltip("Row for the one-time gift; hidden when there is none.")]
        [SerializeField] private GameObject giftRow;
        [SerializeField] private TMP_Text giftText;

        private Func<bool> canWatchAd;
        private Func<bool> watchAd;
        private bool adOffered;
        private bool adPending;

        public bool IsOpen => panel.activeSelf;

        /// <summary>The ad offer is on screen and not used yet.</summary>
        public bool IsAdOffered => adOffered;

        /// <summary>The gift row shows on the open panel.</summary>
        public bool IsGiftShown => giftRow != null && giftRow.activeSelf;

        /// <summary>The Collect button, where the honey flies from.</summary>
        public RectTransform CollectButton => (RectTransform)collectButton.transform;

        /// <summary>The player pressed Collect (the panel then closes).</summary>
        public event Action OnCollected;

        private void Awake()
        {
            collectButton.onClick.AddListener(Collect);
            adButton.onClick.AddListener(HandleAdClick);
            panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (collectButton != null)
                collectButton.onClick.RemoveListener(Collect);
            if (adButton != null)
                adButton.onClick.RemoveListener(HandleAdClick);
        }

        /// <param name="watch">Starts the ad; returns false when none could be shown.</param>
        public void Init(float adMultiplier, Func<bool> canWatch, Func<bool> watch)
        {
            canWatchAd = canWatch;
            watchAd = watch;
            adLabel.text = string.Format(Strings.OfflineAdOfferFormat, adMultiplier.ToString("0.#"));
        }

        /// <param name="gift">The one-time gift from the bees; zero hides its row.</param>
        public void Show(double awaySeconds, BigNumber honey, bool capReached, double capSeconds, BigNumber gift = default)
        {
            if (giftRow != null)
            {
                bool hasGift = !gift.IsZero;
                giftRow.SetActive(hasGift);
                if (hasGift)
                    giftText.text = string.Format(Strings.WelcomeGiftFormat, NumberFormat.Abbreviate(gift));
            }
            awayText.text = string.Format(Strings.AwayFormat, TimeFormat.Duration(awaySeconds));
            ShowHoney(honey);
            capText.gameObject.SetActive(capReached);
            if (capReached)
                capText.text = string.Format(Strings.OfflineCapFormat, TimeFormat.Duration(capSeconds));
            adOffered = watchAd != null;
            adPending = false;
            adButton.gameObject.SetActive(adOffered);
            panel.SetActive(true);
        }

        /// <summary>The ad closed. With a reward the offer is used up and the new total shows.</summary>
        public void AdFinished(bool rewarded, BigNumber totalHoney)
        {
            adPending = false;
            if (!rewarded)
                return;
            adOffered = false;
            adButton.gameObject.SetActive(false);
            ShowHoney(totalHoney);
        }

        public void Close() => panel.SetActive(false);

        private void Collect()
        {
            OnCollected?.Invoke();
            Close();
        }

        private void Update()
        {
            if (!adOffered || !panel.activeSelf)
                return;
            bool interactable = !adPending && canWatchAd();
            if (adButton.interactable != interactable)
                adButton.interactable = interactable;
        }

        private void ShowHoney(BigNumber honey) =>
            honeyText.text = string.Format(Strings.OfflineHoneyFormat, NumberFormat.Abbreviate(honey));

        private void HandleAdClick()
        {
            if (!adOffered || adPending || !watchAd())
                return;
            adPending = true;
            adButton.interactable = false;
        }
    }
}
