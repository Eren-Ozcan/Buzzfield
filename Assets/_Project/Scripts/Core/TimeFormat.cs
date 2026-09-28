using System;

namespace Buzzfield.Core
{
    /// <summary>Short durations for the UI: "2h 5m", "12m 30s", "45s".</summary>
    public static class TimeFormat
    {
        /// <summary>Two largest units, the smaller one left out when it is zero. Rounds down.</summary>
        public static string Duration(double seconds)
        {
            string format = Split(seconds, out long first, out long second);
            return string.Format(format, first, second);
        }

        /// <summary>
        /// The pieces of <see cref="Duration"/> without building the string: returns its format
        /// ({0} = first unit, {1} = second unit if any) and the two values. Labels that tick every
        /// second fill the format through TextMeshPro instead of allocating.
        /// </summary>
        public static string Split(double seconds, out long first, out long second)
        {
            if (double.IsNaN(seconds) || seconds < 0)
                seconds = 0;
            long total = double.IsInfinity(seconds) ? long.MaxValue / 2 : (long)Math.Floor(seconds);
            long hours = total / 3600;
            long minutes = total % 3600 / 60;
            long secs = total % 60;

            if (hours > 0)
            {
                first = hours;
                second = minutes;
                return minutes > 0 ? Strings.HoursMinutesFormat : Strings.HoursFormat;
            }
            if (minutes > 0)
            {
                first = minutes;
                second = secs;
                return secs > 0 ? Strings.MinutesSecondsFormat : Strings.MinutesFormat;
            }
            first = secs;
            second = 0;
            return Strings.SecondsFormat;
        }
    }
}
