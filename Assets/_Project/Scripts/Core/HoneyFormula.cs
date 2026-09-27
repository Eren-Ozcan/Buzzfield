namespace Buzzfield.Core
{
    /// <summary>Converts deposited nectar into honey (design doc section 2).</summary>
    public static class HoneyFormula
    {
        /// <param name="nectar">Units carried back to the hive.</param>
        /// <param name="flowerValue">Nectar value of the source flower times the garden value multiplier.</param>
        /// <param name="honeyValueMultiplier">Permanent multiplier from upgrades and the Queen.</param>
        /// <param name="boostMultiplier">Product of all timed boosts active right now (1 when none).</param>
        public static BigNumber Honey(double nectar, double flowerValue, BigNumber honeyValueMultiplier, double boostMultiplier)
        {
            if (nectar <= 0 || flowerValue <= 0 || boostMultiplier <= 0)
                return BigNumber.Zero;
            return BigNumber.FromDouble(nectar * flowerValue * boostMultiplier) * honeyValueMultiplier;
        }
    }
}
