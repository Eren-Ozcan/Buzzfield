using System;

namespace Buzzfield.Core
{
    /// <summary>Short durations for the UI: "2h 5m", "12m 30s", "45s".</summary>
    public static class TimeFormat
    {
        /// <summary>Two largest units, the smaller one left out when it is zero. Rounds down.</summary>
        public static string Duration(double seconds)
        {
            if (double.IsNaN(seconds) || seconds < 0)
                seconds = 0;
            long total = double.IsInfinity(seconds) ? long.MaxValue / 2 : (long)Math.Floor(seconds);
            long hours = total / 3600;
            long minutes = total % 3600 / 60;
            long secs = total % 60;

            if (hours > 0)
                return minutes > 0
                    ? string.Format(Strings.HoursMinutesFormat, hours, minutes)
                    : string.Format(Strings.HoursFormat, hours);
            if (minutes > 0)
                return secs > 0
                    ? string.Format(Strings.MinutesSecondsFormat, minutes, secs)
                    : string.Format(Strings.MinutesFormat, minutes);
            return string.Format(Strings.SecondsFormat, secs);
        }
    }
}
