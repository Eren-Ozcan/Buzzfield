using System;
using System.Collections.Generic;
using Buzzfield.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>What the daily panel shows; the game reads it from the streak and the date.</summary>
    public struct DailyPanelState
    {
        /// <summary>Trusted time has given today's date; without it nothing can be claimed.</summary>
        public bool DateKnown;
        /// <summary>The cycle day today stands on (0-based): the one to claim or the one claimed today.</summary>
        public int TodayIndex;
        public bool CanClaim;
        /// <summary>A day was missed, so the streak starts over with this claim.</summary>
        public bool Missed;
        /// <summary>The streak is not open yet (the first day): nothing is claimed or claimable.</summary>
        public bool Locked;
    }

    /// <summary>
    /// The daily login streak (docs/DEWDROPS.md section 6): a modal panel with one tile per day of
    /// the reward cycle (done days ticked, today lit, the last day big) and a Claim button, plus
    /// the small calendar badge under the top bar that stays while today's Dewdrops wait. The
    /// game opens the panel by itself once a session; the badge opens it again.
    /// </summary>
    public sealed class DailyPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button claimButton;
        [SerializeField] private TMP_Text claimLabel;
        [SerializeField] private TMP_Text statusText;

        [Header("Days")]
        [SerializeField] private Image[] dayBackgrounds;
        [SerializeField] private TMP_Text[] dayLabels;
        [SerializeField] private TMP_Text[] dayAmounts;
        [Tooltip("Tick over a day already claimed in this cycle.")]
        [SerializeField] private GameObject[] dayChecks;
        [SerializeField] private Color doneColor = Color.gray;
        [SerializeField] private Color todayColor = Color.cyan;
        [SerializeField] private Color laterColor = Color.white;

        [Header("Badge")]
        [Tooltip("Calendar button under the top bar, shown while today's Dewdrops wait.")]
        [SerializeField] private GameObject badge;
        [SerializeField] private Button badgeButton;

        private IReadOnlyList<int> rewards;
        private Func<bool> claim;
        private Action open;
        private Tweener tweener;
        private UiFeedbackSettings feedback;

        public bool IsOpen => panel.activeSelf;

        public bool IsBadgeShown => badge.activeSelf;

        /// <summary>The Claim button, where the Dewdrops fly from.</summary>
        public RectTransform ClaimButton => (RectTransform)claimButton.transform;

        /// <param name="dayRewards">Dewdrops per day of the cycle, first day first.</param>
        /// <param name="tryClaim">Claims today; true when it went through.</param>
        /// <param name="openPanel">The badge was tapped: the game reads the state and opens the panel.</param>
        public void Init(IReadOnlyList<int> dayRewards, Func<bool> tryClaim, Action openPanel, Tweener tweenRunner, UiFeedbackSettings feedbackSettings)
        {
            rewards = dayRewards;
            claim = tryClaim;
            open = openPanel;
            tweener = tweenRunner;
            feedback = feedbackSettings;
            AddListener(closeButton, Close);
            AddListener(claimButton, HandleClaim);
            AddListener(badgeButton, HandleBadge);
            for (int i = 0; i < dayLabels.Length; i++)
            {
                bool used = i < rewards.Count;
                dayBackgrounds[i].gameObject.SetActive(used);
                if (!used)
                    continue;
                dayLabels[i].text = string.Format(Strings.DailyDayFormat, i + 1);
                dayAmounts[i].text = string.Format(Strings.DewGainFormat, rewards[i]);
            }
            panel.SetActive(false);
            badge.SetActive(false);
        }

        /// <summary>Opens the panel on <paramref name="state"/>.</summary>
        public void Open(DailyPanelState state)
        {
            Show(state);
            panel.SetActive(true);
        }

        public void Close() => panel.SetActive(false);

        /// <summary>Shows or hides the calendar badge; it pops in when it appears.</summary>
        public void SetBadge(bool show)
        {
            if (badge.activeSelf == show)
                return;
            badge.SetActive(show);
            if (show)
                tweener.PopIn(badge.transform, feedback.BadgePop, feedback.BadgePopDuration);
        }

        /// <summary>Lays out the tiles, the Claim button and the line above it.</summary>
        public void Show(DailyPanelState state)
        {
            int count = Mathf.Min(rewards.Count, dayBackgrounds.Length);
            int today = Mathf.Clamp(state.TodayIndex, 0, Mathf.Max(0, count - 1));
            for (int i = 0; i < count; i++)
            {
                bool done = !state.Locked && (i < today || (i == today && !state.CanClaim && state.DateKnown));
                dayBackgrounds[i].color = done ? doneColor : i == today ? todayColor : laterColor;
                dayChecks[i].SetActive(done);
            }

            // Without the date, or before the streak opens, the button still names the reward but waits.
            bool claimedToday = state.DateKnown && !state.CanClaim && !state.Locked;
            claimButton.interactable = state.DateKnown && state.CanClaim;
            claimLabel.text = claimedToday ? Strings.DailyClaimed : string.Format(Strings.DailyClaimFormat, count > 0 ? rewards[today] : 0);
            if (!state.DateKnown)
                statusText.text = Strings.DailyWaitingForTime;
            else if (claimedToday)
                statusText.text = Strings.DailyComeBack;
            else
                statusText.text = state.Missed ? Strings.DailyMissed : Strings.DailyHint;
        }

        private void HandleClaim() => claim?.Invoke();

        private void HandleBadge() => open?.Invoke();

        private void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
            if (claimButton != null)
                claimButton.onClick.RemoveListener(HandleClaim);
            if (badgeButton != null)
                badgeButton.onClick.RemoveListener(HandleBadge);
        }
    }
}
