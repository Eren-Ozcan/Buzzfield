using System;
using System.Globalization;
using System.Text;

namespace Buzzfield.Core
{
    /// <summary>
    /// Turns the store's localized price into text the game's font can draw. Store prices carry
    /// no-break and thin spaces, direction marks and currency signs (₺, ₹, ₽, zł) the font does
    /// not have, which would show as boxes; such a price keeps its number and names the currency
    /// by its ISO code instead ("₺29,99" becomes "29,99 TRY").
    /// </summary>
    public static class PriceLabel
    {
        /// <param name="price">The store's localized price string.</param>
        /// <param name="currencyCode">ISO 4217 code of the price, or null when the store gave none.</param>
        /// <param name="amount">The price as a number, used when even its digits cannot be drawn.</param>
        /// <param name="canDraw">Whether the font has a character.</param>
        public static string Drawable(string price, string currencyCode, decimal amount, Func<char, bool> canDraw)
        {
            if (string.IsNullOrEmpty(price))
                return price;

            string text = Clean(price);
            if (AllDrawable(text, canDraw))
                return text;

            bool hasCode = !string.IsNullOrEmpty(currencyCode) && AllDrawable(currencyCode, canDraw);
            string number = NumberSpan(text);
            if (number.Length > 0 && AllDrawable(number, canDraw))
                return hasCode ? number + " " + currencyCode : number;

            // Digits of another script (Arabic-Indic, Devanagari): write the amount ourselves.
            string invariant = amount == decimal.Truncate(amount)
                ? amount.ToString("#,0", CultureInfo.InvariantCulture)
                : amount.ToString("#,0.00", CultureInfo.InvariantCulture);
            return hasCode ? invariant + " " + currencyCode : invariant;
        }

        /// <summary>Every kind of space becomes a plain one, runs of them one; invisible format marks go.</summary>
        static string Clean(string price)
        {
            var builder = new StringBuilder(price.Length);
            bool space = false;
            foreach (char c in price)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Format)
                    continue;
                if (char.IsWhiteSpace(c))
                {
                    space = builder.Length > 0;
                    continue;
                }
                if (space)
                    builder.Append(' ');
                space = false;
                builder.Append(c);
            }
            return builder.ToString();
        }

        /// <summary>From the first digit to the last: "29,99" of "29,99 zł", "1 234,56" of "1 234,56 ₽".</summary>
        static string NumberSpan(string text)
        {
            int first = -1;
            int last = -1;
            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsDigit(text[i]))
                    continue;
                if (first < 0)
                    first = i;
                last = i;
            }
            return first < 0 ? string.Empty : text.Substring(first, last - first + 1);
        }

        static bool AllDrawable(string text, Func<char, bool> canDraw)
        {
            foreach (char c in text)
            {
                if (!canDraw(c))
                    return false;
            }
            return true;
        }
    }
}
