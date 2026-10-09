using System;
using Buzzfield.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// A short-lived button after a caught butterfly: "double it" plays a rewarded ad that pays
    /// the honey gift again, "open the pouch" plays one that opens the Dewdrops pouch (its X leaves
    /// the pouch). It shrinks a timer bar and goes away by itself; it only ever starts an ad when
    /// the player taps it (studio ad policy).
    /// </summary>
    public sealed class GiftOfferView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [Tooltip("Stretched from the left; its right anchor is the time left.")]
        [SerializeField] private RectTransform timerFill;
        [Tooltip("Leaves the pouch; shown on the pouch offer only.")]
        [SerializeField] private Button closeButton;

        private Func<bool> canWatch;
        private Func<bool> watch;
        private float duration;
        private float left;
        private bool pending;

        public bool IsShowing => root.activeSelf;

        /// <summary>The offer on screen opens the Dewdrops pouch (not the "double it" one).</summary>
        public bool IsPouch { get; private set; }

        /// <summary>The offer button, where the doubled honey flies from.</summary>
        public RectTransform Button => (RectTransform)button.transform;

        /// <param name="canWatchAd">An ad could be shown now.</param>
        /// <param name="watchAd">Starts the ad; false when none could be shown.</param>
        public void Init(Func<bool> canWatchAd, Func<bool> watchAd)
        {
            canWatch = canWatchAd;
            watch = watchAd;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Leave);
                closeButton.onClick.AddListener(Leave);
            }
            root.SetActive(false);
        }

        /// <summary>Offers <paramref name="extra"/> honey for an ad, for <paramref name="seconds"/>.</summary>
        public void Show(BigNumber extra, float seconds) =>
            Open(string.Format(Strings.GiftDoubleFormat, NumberFormat.Abbreviate(extra)), seconds, false);

        /// <summary>Offers to open the Dewdrops pouch with an ad, for <paramref name="seconds"/>; the X leaves it.</summary>
        public void ShowPouch(float seconds) => Open(Strings.PouchOffer, seconds, true);

        /// <summary>The player leaves the pouch (its X); an ad already starting is not stopped.</summary>
        public void Leave()
        {
            if (!pending)
                Hide();
        }

        /// <summary>The ad closed: the offer is used up either way.</summary>
        public void Finish()
        {
            pending = false;
            Hide();
        }

        public void Hide()
        {
            if (root.activeSelf)
                root.SetActive(false);
        }

        private void Update()
        {
            if (!root.activeSelf || pending)
                return;
            left -= Time.unscaledDeltaTime;
            if (left <= 0f)
            {
                Hide();
                return;
            }
            SetFill(left / duration);
            bool interactable = canWatch == null || canWatch();
            if (button.interactable != interactable)
                button.interactable = interactable;
        }

        private void Open(string text, float seconds, bool pouch)
        {
            label.text = text;
            IsPouch = pouch;
            if (closeButton != null)
                closeButton.gameObject.SetActive(pouch);
            duration = Mathf.Max(0.1f, seconds);
            left = duration;
            pending = false;
            root.SetActive(true);
            SetFill(1f);
        }

        private void SetFill(float fraction)
        {
            if (timerFill != null)
                timerFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), timerFill.anchorMax.y);
        }

        private void HandleClick()
        {
            if (pending || watch == null || !watch())
                return;
            pending = true;
            button.interactable = false;
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Leave);
        }
    }
}
