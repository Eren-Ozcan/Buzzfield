using System;

namespace Buzzfield.Core
{
    /// <summary>
    /// Spacing between full-screen ads. Every full-screen ad stamps one shared "last ad"
    /// time (UTC seconds, saved), and the next one waits until the gap has passed.
    /// </summary>
    public static class AdPacing
    {
        /// <summary>
        /// Seconds until the next full-screen ad is allowed; 0 when it is allowed now.
        /// A stamp more than one gap in the future (the device clock went back) is ignored,
        /// so a clock change never locks ads for longer than one gap.
        /// </summary>
        public static double SecondsUntilAllowed(double nowUtc, double lastAdUtc, double minGapSeconds)
        {
            if (minGapSeconds <= 0 || lastAdUtc <= 0)
                return 0;
            if (lastAdUtc > nowUtc + minGapSeconds)
                return 0;
            return Math.Max(0, lastAdUtc + minGapSeconds - nowUtc);
        }

        public static bool IsAllowed(double nowUtc, double lastAdUtc, double minGapSeconds) =>
            SecondsUntilAllowed(nowUtc, lastAdUtc, minGapSeconds) <= 0;
    }
}
