using System;

namespace Buzzfield.Core
{
    /// <summary>The daily login streak as the save keeps it. Day numbers come from <see cref="LoginStreak.DayNumber"/>; 0 means "not yet".</summary>
    [Serializable]
    public struct DailyStreak
    {
        /// <summary>The first day the game knew the date (trusted time); the streak opens some days after it.</summary>
        public long firstDay;
        /// <summary>The day of the last claim.</summary>
        public long lastClaimDay;
        /// <summary>Days claimed in a row, up to and including <see cref="lastClaimDay"/>.</summary>
        public int streak;
    }

    /// <summary>
    /// The daily login streak (docs/DEWDROPS.md section 6): one claim per local day, the day turning
    /// at a fixed local hour. Claims on back-to-back days grow the streak and walk a reward cycle;
    /// a missed day starts it over at day 1. Days only ever come from trusted time, and a day
    /// earlier than the last claim (clock or time zone moved back) claims nothing.
    /// </summary>
    public static class LoginStreak
    {
        public const double SecondsPerDay = 86400.0;

        /// <summary>
        /// Local day number of <paramref name="utcSeconds"/> (unix seconds): days since 1970 in the
        /// player's time zone, turning at <paramref name="dayStartHour"/> local time instead of midnight.
        /// </summary>
        public static long DayNumber(double utcSeconds, double utcOffsetSeconds, double dayStartHour) =>
            (long)Math.Floor((utcSeconds + utcOffsetSeconds - dayStartHour * 3600.0) / SecondsPerDay);

        /// <summary>Records the first known day once; later days leave it alone.</summary>
        public static DailyStreak NoteDay(DailyStreak data, long today)
        {
            if (data.firstDay <= 0 && today > 0)
                data.firstDay = today;
            return data;
        }

        /// <summary>The streak is open: <paramref name="unlockDay"/> days have passed since the first known day (1 = the second day).</summary>
        public static bool IsUnlocked(DailyStreak data, long today, int unlockDay) =>
            data.firstDay > 0 && today - data.firstDay >= unlockDay;

        /// <summary>Today's reward is still to claim.</summary>
        public static bool CanClaim(DailyStreak data, long today) => today > 0 && (data.lastClaimDay <= 0 || today > data.lastClaimDay);

        /// <summary>A day passed without a claim since the last one: the next claim starts over.</summary>
        public static bool Missed(DailyStreak data, long today) => data.lastClaimDay > 0 && today > data.lastClaimDay + 1;

        /// <summary>The streak a claim today makes: one more after yesterday's claim, otherwise 1.</summary>
        public static int StreakIfClaimed(DailyStreak data, long today) =>
            data.lastClaimDay > 0 && today == data.lastClaimDay + 1 && data.streak > 0 ? data.streak + 1 : 1;

        /// <summary>Place of streak day <paramref name="streak"/> (1-based) in a reward cycle of <paramref name="cycleLength"/>; 0-based.</summary>
        public static int CycleIndex(int streak, int cycleLength)
        {
            if (cycleLength <= 0 || streak <= 0)
                return 0;
            return (streak - 1) % cycleLength;
        }

        /// <summary>
        /// The cycle day today stands on (0-based): the one a claim would pay, or the one already
        /// claimed today. Days before it in the cycle are done.
        /// </summary>
        public static int TodayIndex(DailyStreak data, long today, int cycleLength) =>
            CycleIndex(CanClaim(data, today) ? StreakIfClaimed(data, today) : Math.Max(1, data.streak), cycleLength);

        /// <summary>Claims today; returns the data unchanged when there is nothing to claim.</summary>
        public static DailyStreak Claim(DailyStreak data, long today)
        {
            if (!CanClaim(data, today))
                return data;
            data.streak = StreakIfClaimed(data, today);
            data.lastClaimDay = today;
            return data;
        }
    }
}
