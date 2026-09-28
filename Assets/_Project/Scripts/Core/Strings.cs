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

        public const string BloomFormat = "Bloom {0}%";
        public const string GardenComplete = "Garden Complete!";

        public const string QueenButton = "Queen";
        public const string QueenTitle = "Queen Bee";
        public const string RoyalJellyFormat = "Royal Jelly: {0}";
        public const string GardenNumberFormat = "Garden {0}";
        public const string MoveTheQueen = "Move the Queen";
        public const string MoveNeedsBloomFormat = "Needs {0}% bloom (now {1}%)";
        public const string MoveBloomReadyFormat = "Bloom {0}% - ready";
        public const string MoveCostFormat = "Cost: {0} honey";
        public const string JellyPreviewFormat = "+{0} Royal Jelly";
        public const string BloomBonusFormat = "Bloom bonus +{0}%";
        public const string MoveConfirmTitle = "Move the Queen?";
        public const string MoveConfirmBody = "Your bees and honey will reset.\nYou keep your Royal Jelly and go to Garden {0}.";
        public const string MoveConfirm = "Move";
        public const string Cancel = "Cancel";
        public const string Close = "Close";

        public const string WelcomeBackTitle = "Welcome back!";
        public const string AwayFormat = "You were away for {0}";
        public const string OfflineHoneyFormat = "+{0} honey";
        public const string OfflineCapFormat = "Your bees rest after {0}";
        public const string Collect = "Collect";
        public const string OfflineAdOfferFormat = "Watch ad: x{0}";

        public const string RewardedBoostOfferFormat = "Watch ad\nx{0} honey {1}";
        public const string RewardedBoostActiveFormat = "x{0} honey\n{1}";

        public const string TapBoostReady = "Tap!";
        public const string TapBoostActiveFormat = "x{0}";

        public const string ShopButton = "Shop";
        public const string ShopTitle = "Shop";
        public const string ProductRemoveAds = "Remove Ads";
        public const string ProductRemoveAdsBody = "No forced ads.\nRewarded ads stay available.";
        public const string ProductHoney2x = "Double Honey";
        public const string ProductHoney2xBodyFormat = "x{0} honey forever.\nStacks with ad boosts.";
        public const string ProductStarterPack = "Starter Pack";
        public const string ProductStarterPackBodyFormat = "+{0} honey, +{1} Royal Jelly.\nOne time only.";
        public const string Owned = "Owned";
        public const string StoreBusy = "...";
        public const string StoreUnavailable = "Unavailable";
        public const string StoreConnecting = "Connecting to the store...";
        public const string PurchaseThanks = "Thank you!";
        public const string PurchaseCancelled = "Purchase cancelled.";
        public const string PurchaseFailed = "Purchase failed. Please try again.";
        public const string RestorePurchases = "Restore Purchases";
        public const string RestoreDone = "Purchases restored.";
        public const string RestoreNothing = "Nothing to restore.";

        public const string QuitTitle = "Quit the game?";
        public const string QuitBody = "Your progress is saved.";
        public const string Quit = "Quit";

        public const string HoursMinutesFormat = "{0}h {1}m";
        public const string HoursFormat = "{0}h";
        public const string MinutesSecondsFormat = "{0}m {1}s";
        public const string MinutesFormat = "{0}m";
        public const string SecondsFormat = "{0}s";

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
