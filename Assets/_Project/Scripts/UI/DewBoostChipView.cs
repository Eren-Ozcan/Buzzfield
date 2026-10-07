using System;
using Buzzfield.Core;
using Buzzfield.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// Small turquoise chip under the settings gear while a Dewdrops honey boost runs: the
    /// multiplier and the time left. A tap opens the shop's Boosts tab. Hidden otherwise.
    /// </summary>
    public sealed class DewBoostChipView : MonoBehaviour
    {
        [SerializeField] private GameObject chip;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private readonly BoostCountdown countdown = new BoostCountdown(Strings.BoostChipFormat);
        private BoostManager boosts;
        private Tweener tweener;
        private UiFeedbackSettings feedback;
        private Action open;

        public bool IsShown => chip.activeSelf;

        public void Init(BoostManager boostManager, Tweener tweenRunner, UiFeedbackSettings feedbackSettings, Action openBoosts)
        {
            boosts = boostManager;
            tweener = tweenRunner;
            feedback = feedbackSettings;
            open = openBoosts;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
            chip.SetActive(false);
        }

        private void LateUpdate()
        {
            if (boosts == null)
                return;
            DewBoostQueue line = boosts.DewBoosts;
            bool show = line.Count > 0;
            if (show != chip.activeSelf)
            {
                chip.SetActive(show);
                countdown.Reset();
                if (show)
                    tweener.PopIn(chip.transform, feedback.BadgePop, feedback.BadgePopDuration);
            }
            if (show)
                countdown.Show(label, line.Multiplier, line.SecondsLeft);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick() => open?.Invoke();
    }
}
