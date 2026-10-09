using System;

namespace Buzzfield.Core
{
    /// <summary>
    /// The butterfly's Dewdrops pouch (docs/DEWDROPS.md section 8): every few butterflies one
    /// carries a pouch instead of honey, a few a day at most. The save keeps this tally. A
    /// butterfly that may not carry one (the pouch is locked, no ad is ready, the day is unknown)
    /// passes the pouch on to the next one.
    /// </summary>
    [Serializable]
    public struct DewPouch
    {
        /// <summary>The day <see cref="today"/> counts for (<see cref="LoginStreak.DayNumber"/>).</summary>
        public long day;
        /// <summary>Pouches carried on <see cref="day"/>.</summary>
        public int today;
        /// <summary>Butterflies since the last pouch.</summary>
        public int since;

        /// <summary>
        /// Counts a butterfly that sets off on day <paramref name="currentDay"/> (0 = unknown) and
        /// says whether it carries a pouch: every <paramref name="every"/>th one, while
        /// <paramref name="allowed"/> and fewer than <paramref name="perDay"/> went out that day.
        /// A new day starts the count over; an earlier day (clock moved back) keeps it.
        /// </summary>
        public bool NextCarries(long currentDay, bool allowed, int every, int perDay)
        {
            if (currentDay > day)
            {
                day = currentDay;
                today = 0;
            }
            if (since < int.MaxValue)
                since++;
            if (!allowed || currentDay <= 0 || since < Math.Max(1, every) || today >= perDay)
                return false;
            since = 0;
            today++;
            return true;
        }

        /// <summary>Pouches still to come on <paramref name="currentDay"/>.</summary>
        public int LeftToday(long currentDay, int perDay) => Math.Max(0, perDay - (currentDay > day ? 0 : today));

        /// <summary>Dewdrops in an opened pouch: <paramref name="min"/> to <paramref name="max"/>, each as likely; <paramref name="random01"/> in [0, 1).</summary>
        public static int Roll(double random01, int min, int max)
        {
            if (max < min)
                max = min;
            double t = random01 < 0 ? 0 : random01 >= 1 ? 0.999999999 : random01;
            return min + (int)Math.Floor(t * (max - min + 1));
        }
    }
}
