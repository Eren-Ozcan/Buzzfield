namespace Buzzfield.Core
{
    /// <summary>
    /// Every user-visible text in one place, so localization can replace this class later.
    /// English only for now.
    /// </summary>
    public static class Strings
    {
        public const string HoneyLabel = "Honey";
        public const string PerSecondSuffix = " honey";

        public const string TierWorker = "Worker";
        public const string TierForager = "Forager";
        public const string TierGolden = "Golden";

        public const string FlowerDaisy = "Daisy";
        public const string FlowerLavender = "Lavender";
        public const string FlowerOrchid = "Orchid";

        public const string UpgradeAddBee = "Add Bee";
        public const string UpgradeSpeed = "Speed";
        public const string UpgradeEvolve = "Evolve";
        public const string UpgradeHoneyValue = "Honey Value";

        public const string LevelFormat = "Lv {0}";
        public const string BeeCountFormat = "{0}/{1} bees";
        public const string Max = "MAX";
        public const string EvolveNeedsBees = "Need 3";
        public const string TierCountFormat = "{0}{1}";
        public const string TierCountSeparator = "  ";

        /// <summary>One-letter tier tags for the Evolve button, lowest tier first.</summary>
        public static readonly string[] TierShort = { "W", "F", "G" };
    }
}
