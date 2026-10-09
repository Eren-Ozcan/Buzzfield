using System;
using Buzzfield.Core;
using Buzzfield.Platform;
using Buzzfield.UI;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// The daily login streak (docs/DEWDROPS.md section 6). Days come from trusted time only, in
    /// the player's time zone, turning at <see cref="Economy.DewSettings.DayStartHour"/>; the first
    /// day the game knows the date is noted, and the streak opens on a later day. After a launch
    /// or a return the panel opens by itself once the date is known and nothing else is on screen
    /// (after Welcome back), at most once a day; while today's Dewdrops wait, the calendar badge
    /// under the top bar opens it again.
    /// </summary>
    public sealed partial class GameManager
    {
        /// <summary>How often the date is read again while playing, so the badge turns up when the day turns.</summary>
        private const float DailyCheckSeconds = 30f;

        [Header("Daily streak")]
        [SerializeField] private DailyPanelView dailyPanel;

        private DailyStreak daily;
        private float dailyCheckTimer;
        /// <summary>A launch or a return armed the panel to open by itself once the date is known.</summary>
        private bool dailyAutoOpenArmed;
        /// <summary>Seconds the armed panel still waits for the time server.</summary>
        private float dailyAutoOpenWait;
        /// <summary>The day the panel last opened by itself; it does so once a day.</summary>
        private long dailyAutoOpenedDay;
        /// <summary>Days the play test tools moved the date ahead; always 0 otherwise.</summary>
        private long dayShift;

        public DailyStreak Daily => daily;

        public bool IsDailyOpen => dailyPanel.IsOpen;

        /// <summary>Needs the effects layer, the sound and the clock.</summary>
        private void InitDaily()
        {
            dailyPanel.Init(dewSettings.DailyRewards, TryClaimDaily, OpenDaily, tweener, feedbackSettings);
        }

        /// <summary>Today's day number from trusted time; false until the time server has answered.</summary>
        public bool TryGetToday(out long today)
        {
            double? utc = clock.TrustedUtcAt(GameClock.MonotonicSeconds);
            if (!utc.HasValue)
            {
                today = 0;
                return false;
            }
            DateTime moment = DateTimeOffset.FromUnixTimeSeconds((long)utc.Value).UtcDateTime;
            double offset = TimeZoneInfo.Local.GetUtcOffset(moment).TotalSeconds;
            today = LoginStreak.DayNumber(utc.Value, offset, dewSettings.DayStartHour) + dayShift;
            return true;
        }

        /// <summary>The streak (and the butterfly's pouch) is open on <paramref name="today"/>.</summary>
        private bool DailyUnlocked(long today) => LoginStreak.IsUnlocked(daily, today, dewSettings.DailyUnlockDay);

        private bool DailyClaimable(long today) => DailyUnlocked(today) && LoginStreak.CanClaim(daily, today);

        /// <summary>Today's streak reward is open and not claimed yet.</summary>
        public bool IsDailyClaimable => TryGetToday(out long today) && DailyClaimable(today);

        /// <summary>Opens the panel on today's state (the badge, or by itself after a launch).</summary>
        public void OpenDaily() => dailyPanel.Open(ReadDailyState());

        /// <summary>Claims today's Dewdrops: the panel closes and they fly to the counter. False when there is nothing to claim.</summary>
        public bool TryClaimDaily()
        {
            if (!TryGetToday(out long today) || !DailyClaimable(today))
                return false;
            int streak = LoginStreak.StreakIfClaimed(daily, today);
            int index = LoginStreak.CycleIndex(streak, dewSettings.DailyRewards.Count);
            int reward = dewSettings.DailyReward(index);
            daily = LoginStreak.Claim(daily, today);
            Vector2 from = RectTransformUtility.WorldToScreenPoint(null, dailyPanel.ClaimButton.position);
            // Closed first: the crystals fly over the garden, not behind the panel.
            dailyPanel.Close();
            EarnDew(reward, "login_streak", from);
            Pulse(true);
            FirebaseServices.Log("login_streak", ("day", (long)(index + 1)), ("streak", (long)daily.streak));
            RefreshDaily(today);
            SaveNow();
            return true;
        }

        /// <summary>A launch or a real return: the panel may open by itself once the date is known.</summary>
        private void ArmDailyAutoOpen()
        {
            dailyAutoOpenArmed = true;
            dailyAutoOpenWait = dewSettings.DailyAutoOpenSeconds;
        }

        /// <summary>The date may be known now: notes the first day and shows or hides the badge.</summary>
        private void NoteToday()
        {
            if (!TryGetToday(out long today))
                return;
            daily = LoginStreak.NoteDay(daily, today);
            RefreshDaily(today);
        }

        private void TickDaily(float unscaledDeltaTime)
        {
            if (dailyAutoOpenArmed)
            {
                if (!clock.HasTrustedTime)
                {
                    // A slow time server must not open the panel in the middle of play.
                    dailyAutoOpenWait -= unscaledDeltaTime;
                    if (dailyAutoOpenWait <= 0f)
                        dailyAutoOpenArmed = false;
                }
                else if (IsGardenCalm())
                {
                    dailyAutoOpenArmed = false;
                    if (TryGetToday(out long today) && DailyClaimable(today) && today != dailyAutoOpenedDay)
                    {
                        dailyAutoOpenedDay = today;
                        OpenDaily();
                    }
                }
            }

            dailyCheckTimer -= unscaledDeltaTime;
            if (dailyCheckTimer > 0f)
                return;
            dailyCheckTimer = DailyCheckSeconds;
            NoteToday();
        }

        private void RefreshDaily(long today)
        {
            dailyPanel.SetBadge(DailyClaimable(today));
            if (dailyPanel.IsOpen)
                dailyPanel.Show(ReadDailyState());
        }

        private DailyPanelState ReadDailyState()
        {
            int cycle = dewSettings.DailyRewards.Count;
            if (!TryGetToday(out long today))
            {
                // The day the next claim would be, as far as the save knows.
                return new DailyPanelState { DateKnown = false, TodayIndex = LoginStreak.CycleIndex(daily.streak + 1, cycle), CanClaim = true };
            }
            return new DailyPanelState
            {
                DateKnown = true,
                TodayIndex = LoginStreak.TodayIndex(daily, today, cycle),
                CanClaim = DailyClaimable(today),
                Missed = LoginStreak.Missed(daily, today),
                Locked = !DailyUnlocked(today),
            };
        }
    }
}
