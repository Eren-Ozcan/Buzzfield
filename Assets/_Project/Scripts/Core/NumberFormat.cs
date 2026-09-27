using System;
using System.Globalization;

namespace Buzzfield.Core
{
    /// <summary>
    /// Display notation for honey and costs: whole numbers below 1000, then K, M, B, T,
    /// then two-letter suffixes aa, ab, ... zz. At most 3 significant digits, trailing
    /// zeros trimmed (1.2K, 34.5M, 999B, 1aa). Values are rounded down, so a displayed
    /// amount is never more than the real one. Never scientific notation.
    /// </summary>
    public static class NumberFormat
    {
        const int SignificantDigits = 3;
        const int NamedTiers = 5; // "", K, M, B, T
        const int LetterCount = 26;

        static readonly string[] Named = { "", "K", "M", "B", "T" };

        /// <summary>Highest suffix tier: T plus every two-letter pair up to zz.</summary>
        public static readonly int MaxTier = NamedTiers - 1 + LetterCount * LetterCount;

        public static string Abbreviate(BigNumber value)
        {
            if (value.IsNegative)
                return "-" + Abbreviate(-value);
            if (value < 1000)
                return Math.Floor(value.ToDouble() + 1e-9).ToString("0", CultureInfo.InvariantCulture);

            int tier = (int)Math.Min(value.Exponent / 3, MaxTier);
            double scaled = (value / BigNumber.Create(1, tier * 3L)).ToDouble();
            double rounded = RoundSignificant(scaled);

            int decimals = DecimalsFor(rounded);
            string digits = rounded.ToString("F" + decimals, CultureInfo.InvariantCulture);
            if (decimals > 0)
                digits = digits.TrimEnd('0').TrimEnd('.');
            return digits + Suffix(tier);
        }

        /// <summary>
        /// Income per second. Early rates sit below 1 honey/s, so values under 10 keep one
        /// decimal (0.4/s, 2.5/s) instead of rounding to whole units.
        /// </summary>
        public static string PerSecond(BigNumber value)
        {
            if (!value.IsNegative && value < 10)
            {
                double rounded = Math.Floor(value.ToDouble() * 10 + 1e-9) / 10;
                return rounded.ToString("0.#", CultureInfo.InvariantCulture) + "/s";
            }
            return Abbreviate(value) + "/s";
        }

        /// <summary>Suffix for a power-of-1000 tier: 0 = none, 1 = K ... 4 = T, 5 = aa, 6 = ab.</summary>
        public static string Suffix(int tier)
        {
            if (tier < 0 || tier > MaxTier)
                throw new ArgumentOutOfRangeException(nameof(tier));
            if (tier < NamedTiers)
                return Named[tier];
            int index = tier - NamedTiers;
            char first = (char)('a' + index / LetterCount);
            char second = (char)('a' + index % LetterCount);
            return new string(new[] { first, second });
        }

        static double RoundSignificant(double value)
        {
            double factor = Math.Pow(10, DecimalsFor(value));
            // The epsilon absorbs binary error such as 1.2 * 100 = 119.99999999999999.
            return Math.Floor(value * factor + 1e-9) / factor;
        }

        static int DecimalsFor(double value)
        {
            // Past the last suffix the integer part can exceed 3 digits; it is kept whole.
            int integerDigits = value < 1 ? 1 : (int)Math.Floor(Math.Log10(value)) + 1;
            return Math.Max(0, SignificantDigits - integerDigits);
        }
    }
}
