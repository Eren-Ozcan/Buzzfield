using System;
using Buzzfield.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// The current goal under the top bar: what to do and how far along it is. Once done it
    /// turns green, breathes and shows the honey it pays; a tap claims it. Hidden when the
    /// chain is finished. The text is rebuilt only when the shown numbers change.
    /// </summary>
    public sealed class GoalView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text icon;
        [SerializeField] private Color workingColor = Color.white;
        [SerializeField] private Color doneColor = Color.green;
        [SerializeField] private string workingGlyph;
        [SerializeField] private string doneGlyph;
        [Tooltip("Dewdrops the goal pays, on the right end of the pill; hidden for goals without any.")]
        [SerializeField] private GameObject dewBadge;
        [SerializeField] private TMP_Text dewText;
        [Tooltip("Scale swing of the breathing while the goal waits to be claimed.")]
        [SerializeField, Min(0f)] private float pulseAmount = 0.05f;
        [SerializeField, Min(0.1f)] private float pulsePeriod = 1.1f;

        private Func<bool> claim;
        private bool hasGoal;
        private bool done;
        private GoalKind shownKind;
        private long shownCurrent = -1;
        private long shownTarget = -1;
        private BigNumber shownReward;
        private int shownDew = -1;

        public bool IsVisible => root.activeSelf;

        /// <summary>The goal is done and waits for its tap.</summary>
        public bool IsClaimable => hasGoal && done;

        /// <summary>Screen position of the pill, where the claimed honey flies from.</summary>
        public RectTransform Pill => (RectTransform)button.transform;

        /// <param name="tryClaim">Claims the goal; true when it went through.</param>
        public void Init(Func<bool> tryClaim)
        {
            claim = tryClaim;
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }

        public void ShowNone()
        {
            hasGoal = false;
            done = false;
            if (root.activeSelf)
                root.SetActive(false);
        }

        /// <summary>Shows a goal in progress, or done with its honey <paramref name="reward"/>; <paramref name="dew"/> shows on its badge.</summary>
        public void Show(GoalKind kind, long current, long target, bool isDone, BigNumber reward, int dew)
        {
            if (!root.activeSelf)
                root.SetActive(true);
            if (dew != shownDew)
            {
                shownDew = dew;
                dewBadge.SetActive(dew > 0);
                if (dew > 0)
                    dewText.text = string.Format(Strings.DewGainFormat, dew);
            }
            bool changed = !hasGoal || kind != shownKind || target != shownTarget || isDone != done
                || (isDone ? reward != shownReward : current != shownCurrent);
            hasGoal = true;
            if (!changed)
                return;
            shownKind = kind;
            shownCurrent = current;
            shownTarget = target;
            shownReward = reward;
            done = isDone;

            background.color = isDone ? doneColor : workingColor;
            icon.text = isDone ? doneGlyph : workingGlyph;
            label.text = isDone
                ? string.Format(Strings.GoalClaimFormat, NumberFormat.Abbreviate(reward))
                : string.Format(Strings.GoalProgressFormat, Describe(kind, target), current, target);
            if (!isDone)
                root.transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (!IsClaimable)
                return;
            float wave = Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / pulsePeriod));
            // The holder breathes; the button itself keeps its press feel.
            root.transform.localScale = Vector3.one * (1f + pulseAmount * wave);
        }

        private void HandleClick()
        {
            if (!IsClaimable || claim == null)
                return;
            claim();
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(HandleClick);
        }

        /// <summary>The goal in words, for example "Add 3 bees".</summary>
        public static string Describe(GoalKind kind, long target)
        {
            switch (kind)
            {
                case GoalKind.ShakeFlowers: return string.Format(Strings.GoalShakeFlowers, target);
                case GoalKind.OwnBees: return string.Format(Strings.GoalOwnBees, target);
                case GoalKind.SpeedLevel: return string.Format(Strings.GoalSpeedLevel, target);
                case GoalKind.HoneyValueLevel: return string.Format(Strings.GoalHoneyValueLevel, target);
                case GoalKind.BloomFlowers: return string.Format(Strings.GoalBloomFlowers, target);
                case GoalKind.EvolveBees: return string.Format(target == 1 ? Strings.GoalEvolveOnce : Strings.GoalEvolveBees, target);
                case GoalKind.GardenBloomPercent: return string.Format(Strings.GoalGardenBloom, target);
                case GoalKind.MoveQueen: return string.Format(target == 1 ? Strings.GoalMoveQueenOnce : Strings.GoalMoveQueen, target);
                case GoalKind.AbilityLevels: return string.Format(Strings.GoalAbilityLevels, target);
                case GoalKind.CompleteGardens: return string.Format(target == 1 ? Strings.GoalCompleteGardenOnce : Strings.GoalCompleteGardens, target);
                default: return kind.ToString();
            }
        }
    }
}
