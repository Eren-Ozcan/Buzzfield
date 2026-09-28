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

        /// <summary>Buffer size that fits any label written by <see cref="Write"/> or <see cref="WritePerSecond"/> in play.</summary>
        public const int MaxLength = 32;

        /// <summary>Largest scaled digit count written without a string; only values past the last suffix get there.</summary>
        const double MaxExactDigits = 1e15;

        public static string Abbreviate(BigNumber value)
        {
            var buffer = new char[MaxLength];
            return new string(buffer, 0, Write(value, buffer));
        }

        /// <summary>
        /// Income per second. Early rates sit below 1 honey/s, so values under 10 keep one
        /// decimal (0.4/s, 2.5/s) instead of rounding to whole units.
        /// </summary>
        public static string PerSecond(BigNumber value)
        {
            var buffer = new char[MaxLength];
            return new string(buffer, 0, WritePerSecond(value, buffer));
        }

        /// <summary>
        /// Writes <see cref="Abbreviate"/> into <paramref name="buffer"/> without allocating and
        /// returns the length. Labels that change every frame use this with a reused buffer.
        /// </summary>
        public static int Write(BigNumber value, char[] buffer)
        {
            int length = 0;
            if (value.IsNegative)
            {
                buffer[length++] = '-';
                value = -value;
            }
            if (value < 1000)
                return WriteInteger((long)Math.Floor(value.ToDouble() + 1e-9), buffer, length);

            int tier = (int)Math.Min(value.Exponent / 3, MaxTier);
            double scaled = (value / BigNumber.Create(1, tier * 3L)).ToDouble();
            int decimals = DecimalsFor(scaled);
            // The epsilon absorbs binary error such as 1.2 * 100 = 119.99999999999999.
            double digits = Math.Floor(scaled * Pow10(decimals) + 1e-9);
            if (digits >= MaxExactDigits)
                length = CopyString(digits.ToString("F0", CultureInfo.InvariantCulture), buffer, length);
            else
                length = WriteFixed((long)digits, decimals, buffer, length);
            return WriteSuffix(tier, buffer, length);
        }

        /// <summary>Writes <see cref="PerSecond"/> into <paramref name="buffer"/> without allocating and returns the length.</summary>
        public static int WritePerSecond(BigNumber value, char[] buffer)
        {
            int length;
            if (!value.IsNegative && value < 10)
                length = WriteFixed((long)Math.Floor(value.ToDouble() * 10 + 1e-9), 1, buffer, 0);
            else
                length = Write(value, buffer);
            buffer[length++] = '/';
            buffer[length++] = 's';
            return length;
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

        /// <summary><paramref name="scaledDigits"/> / 10^<paramref name="decimals"/> with trailing fraction zeros trimmed.</summary>
        static int WriteFixed(long scaledDigits, int decimals, char[] buffer, int length)
        {
            long unit = (long)Pow10(decimals);
            length = WriteInteger(scaledDigits / unit, buffer, length);
            long fraction = scaledDigits % unit;
            if (fraction == 0)
                return length;
            int places = decimals;
            while (fraction % 10 == 0)
            {
                fraction /= 10;
                places--;
            }
            buffer[length++] = '.';
            for (int i = places - 1; i >= 0; i--)
            {
                buffer[length + i] = (char)('0' + fraction % 10);
                fraction /= 10;
            }
            return length + places;
        }

        static int WriteInteger(long value, char[] buffer, int length)
        {
            int count = 1;
            for (long rest = value / 10; rest > 0; rest /= 10)
                count++;
            for (int i = count - 1; i >= 0; i--)
            {
                buffer[length + i] = (char)('0' + value % 10);
                value /= 10;
            }
            return length + count;
        }

        static int WriteSuffix(int tier, char[] buffer, int length)
        {
            if (tier < NamedTiers)
                return CopyString(Named[tier], buffer, length);
            int index = tier - NamedTiers;
            buffer[length++] = (char)('a' + index / LetterCount);
            buffer[length++] = (char)('a' + index % LetterCount);
            return length;
        }

        static int CopyString(string text, char[] buffer, int length)
        {
            int count = Math.Min(text.Length, buffer.Length - length);
            text.CopyTo(0, buffer, length, count);
            return length + count;
        }

        static double Pow10(int exponent)
        {
            double result = 1;
            for (int i = 0; i < exponent; i++)
                result *= 10;
            return result;
        }

        static int DecimalsFor(double value)
        {
            // Past the last suffix the integer part can exceed 3 digits; it is kept whole.
            int integerDigits = value < 1 ? 1 : (int)Math.Floor(Math.Log10(value)) + 1;
            return Math.Max(0, SignificantDigits - integerDigits);
        }
    }
}
