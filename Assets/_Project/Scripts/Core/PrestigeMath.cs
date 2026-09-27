using System;

namespace Buzzfield.Core
{
    /// <summary>
    /// "Move the Queen" rules (design doc section 3b): the unlock gate, the Royal Jelly
    /// reward and how gardens scale once the authored ones run out.
    /// </summary>
    public static class PrestigeMath
    {
        /// <summary>Slack for float bloom fractions such as 6 / 10 against a 0.6 threshold.</summary>
        private const float BloomEpsilon = 1e-5f;

        /// <summary>Largest value below which a <see cref="BigNumber"/> is floored exactly as a double.</summary>
        private const double ExactFloorLimit = 1e15;

        /// <summary>True once the garden bloom reaches the unlock threshold.</summary>
        public static bool IsUnlocked(float bloom, float unlockBloom) => bloom + BloomEpsilon >= unlockBloom;

        /// <summary>
        /// Extra Royal Jelly share for blooming past the unlock threshold: 0 at the threshold,
        /// growing linearly to <paramref name="bonusAtFull"/> at 100%, plus
        /// <paramref name="completeBonus"/> when every slot has bloomed.
        /// </summary>
        public static double BloomBonus(float bloom, float unlockBloom, float bonusAtFull, float completeBonus)
        {
            if (!IsUnlocked(bloom, unlockBloom))
                return 0;
            if (bloom + BloomEpsilon >= 1f)
                return bonusAtFull + completeBonus;
            float span = 1f - unlockBloom;
            if (span <= 0f)
                return 0;
            double share = Math.Min(1f, Math.Max(0f, (bloom - unlockBloom) / span));
            return share * bonusAtFull;
        }

        /// <summary>
        /// Royal Jelly for a move: floor(scale * sqrt(runHoney / jellyBase)) * (1 + bloomBonus),
        /// floored again so the currency stays a whole number.
        /// </summary>
        public static BigNumber Jelly(BigNumber runHoney, double jellyBase, double jellyScale, double bloomBonus)
        {
            if (runHoney.IsZero || runHoney.IsNegative || jellyBase <= 0 || jellyScale <= 0)
                return BigNumber.Zero;
            BigNumber raw = Floor(BigNumber.Pow(runHoney / jellyBase, 0.5) * jellyScale);
            return Floor(raw * (1 + Math.Max(0, bloomBonus)));
        }

        /// <summary>
        /// Value and cost factor of garden <paramref name="gardenIndex"/>: 1 for authored gardens,
        /// then (1 + loopBonus)^loops once the last authored garden repeats.
        /// </summary>
        public static double LoopMultiplier(int gardenIndex, int authoredCount, double loopBonus)
        {
            if (authoredCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(authoredCount));
            int loops = Math.Max(0, gardenIndex - (authoredCount - 1));
            return Math.Pow(1 + Math.Max(0, loopBonus), loops);
        }

        /// <summary>Index into the authored garden list: the last one repeats forever.</summary>
        public static int ConfigIndex(int gardenIndex, int authoredCount)
        {
            if (authoredCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(authoredCount));
            return Math.Min(Math.Max(0, gardenIndex), authoredCount - 1);
        }

        /// <summary>
        /// Rounds down, forgiving float drift just under a whole number (4.9999999 is 5).
        /// Past 1e15 a double has no fractional part left to drop.
        /// </summary>
        public static BigNumber Floor(BigNumber value)
        {
            if (value.IsZero || value >= ExactFloorLimit || value <= -ExactFloorLimit)
                return value;
            return BigNumber.FromDouble(Math.Floor(value.ToDouble() + 1e-9));
        }
    }
}
