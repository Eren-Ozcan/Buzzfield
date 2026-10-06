namespace Buzzfield.Core
{
    /// <summary>
    /// Honey gifts sized in seconds of income (goals, the butterfly, the gift on the first
    /// return), so they stay worth the same share of the economy from the first garden to the last.
    /// </summary>
    public static class RewardMath
    {
        /// <summary>
        /// <paramref name="seconds"/> of <paramref name="honeyPerSecond"/>, never below
        /// <paramref name="minimum"/> (a new garden earns almost nothing yet). Negative inputs read as zero.
        /// </summary>
        public static BigNumber IncomeSeconds(BigNumber honeyPerSecond, double seconds, double minimum)
        {
            BigNumber rate = honeyPerSecond.IsNegative ? BigNumber.Zero : honeyPerSecond;
            BigNumber amount = rate * BigNumber.FromDouble(seconds > 0 ? seconds : 0);
            return BigNumber.Max(amount, BigNumber.FromDouble(minimum > 0 ? minimum : 0));
        }
    }
}
