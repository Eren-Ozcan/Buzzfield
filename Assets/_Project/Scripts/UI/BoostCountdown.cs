using System;
using Buzzfield.Core;
using TMPro;

namespace Buzzfield.UI
{
    /// <summary>
    /// Text for a running boost from a template where {0} is the multiplier and {1} the time
    /// left ("x{0} honey: {1} left"). The template is filled once per multiplier with each
    /// duration format, so the countdown itself goes through TextMeshPro SetText and allocates nothing.
    /// </summary>
    public sealed class BoostCountdown
    {
        private static readonly string[] DurationFormats =
        {
            Strings.HoursMinutesFormat, Strings.HoursFormat, Strings.MinutesSecondsFormat, Strings.MinutesFormat, Strings.SecondsFormat,
        };

        private readonly string template;
        private readonly string[] formats = new string[DurationFormats.Length];
        private double shownMultiplier = -1;
        private long shownSeconds = -1;

        public BoostCountdown(string template)
        {
            this.template = template;
        }

        /// <summary>The next <see cref="Show"/> writes the text even if nothing changed.</summary>
        public void Reset() => shownSeconds = -1;

        /// <summary>Writes the label when the multiplier or the whole second changed; rounds up so it never reads 0s while running.</summary>
        public void Show(TMP_Text text, double multiplier, double secondsLeft)
        {
            long seconds = (long)Math.Ceiling(Math.Max(0, secondsLeft));
            if (multiplier == shownMultiplier && seconds == shownSeconds)
                return;
            if (multiplier != shownMultiplier)
            {
                shownMultiplier = multiplier;
                string number = multiplier.ToString("0.#");
                for (int i = 0; i < DurationFormats.Length; i++)
                    formats[i] = string.Format(template, number, DurationFormats[i]);
            }
            shownSeconds = seconds;
            string duration = TimeFormat.Split(seconds, out long first, out long second);
            text.SetText(FormatFor(duration), first, second);
        }

        private string FormatFor(string durationFormat)
        {
            for (int i = 0; i < DurationFormats.Length; i++)
            {
                if (ReferenceEquals(DurationFormats[i], durationFormat))
                    return formats[i];
            }
            return durationFormat;
        }
    }
}
