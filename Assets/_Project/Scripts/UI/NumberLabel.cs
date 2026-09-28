using Buzzfield.Core;
using TMPro;

namespace Buzzfield.UI
{
    /// <summary>
    /// A number readout that allocates nothing: the value is written into a reused buffer and
    /// handed to TextMeshPro only when the characters differ from what is shown.
    /// </summary>
    public sealed class NumberLabel
    {
        private readonly TMP_Text text;
        private readonly char[] shown = new char[NumberFormat.MaxLength];
        private readonly char[] scratch = new char[NumberFormat.MaxLength];
        private int shownLength = -1;

        public NumberLabel(TMP_Text text)
        {
            this.text = text;
            Reserve(text, NumberFormat.MaxLength);
        }

        /// <summary>
        /// Grows the text's internal buffers and mesh to <paramref name="length"/> characters once,
        /// so a label that later gets longer does not allocate mid-game. The caller sets the real text next.
        /// </summary>
        public static void Reserve(TMP_Text text, int length)
        {
            var filler = new char[length];
            for (int i = 0; i < filler.Length; i++)
                filler[i] = '8';
            text.SetCharArray(filler, 0, filler.Length);
            text.ForceMeshUpdate(true, true);
        }

        /// <summary>Shows <see cref="NumberFormat.Abbreviate"/> of the value; false when the text did not change.</summary>
        public bool ShowAbbreviated(BigNumber value) => Apply(NumberFormat.Write(value, scratch));

        /// <summary>Shows <see cref="NumberFormat.PerSecond"/> of the value; false when the text did not change.</summary>
        public bool ShowPerSecond(BigNumber value) => Apply(NumberFormat.WritePerSecond(value, scratch));

        private bool Apply(int length)
        {
            if (length == shownLength && Same(length))
                return false;
            System.Array.Copy(scratch, shown, length);
            shownLength = length;
            text.SetCharArray(shown, 0, length);
            return true;
        }

        private bool Same(int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (scratch[i] != shown[i])
                    return false;
            }
            return true;
        }
    }
}
