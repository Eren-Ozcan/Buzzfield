namespace Buzzfield.Core
{
    /// <summary>
    /// Every user-visible text in one place, so localization can replace this class later.
    /// English only for now.
    /// </summary>
    public static class Strings
    {
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

        public const string QueenLevelFormat = "Queen Lv {0}  x{1} honey";
        public const string QueenNextLevelFormat = "{0} / {1} jelly earned to Lv {2} (x{3})";
        public const string QueenMaxLevel = "Top Queen level reached";
        public const string AbilitiesTitle = "Queen Abilities";
        public const string AbilityLevelFormat = "Lv {0}/{1}";
        public const string AbilityCostFormat = "{0}\nJelly";

        public const string AbilityRoyalBrood = "Royal Brood";
        public const string AbilityRoyalWings = "Royal Wings";
        public const string AbilitySweetMemory = "Sweet Memory";
        public const string AbilityPollenTouch = "Pollen Touch";

        /// <summary>What one level of an ability does, by effect; {0} is the amount per level.</summary>
        public const string EffectStartingWorkersFormat = "+{0} Worker at the start of every garden";
        public const string EffectFlightSpeedFormat = "+{0}% bee flight speed";
        public const string EffectOfflineCapFormat = "+{0}h offline earning time";
        public const string EffectBloomSpeedFormat = "+{0}% flower bloom speed";
        public const string PerLevelSuffix = " per level";
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

        public const string SwipeHint = "Swipe across the flowers\nto shake out pollen!";

        public const string ShopButton = "Shop";
        public const string ShopTitle = "Shop";
        public const string ProductRemoveAds = "Remove Ads";
        public const string ProductRemoveAdsBody = "No forced ads.\nRewarded ads stay available.";
        public const string ProductHoney2x = "Double Honey";
        public const string ProductHoney2xBodyFormat = "x{0} honey forever.\nStacks with ad boosts.";
        public const string ProductStarterPack = "Starter Pack";
        public const string ProductStarterPackBodyFormat = "+{0} Dewdrops and {1} of honey.\nOne time only.";
        public const string ProductDewPackFormat = "{0} Dewdrops";
        public const string ProductDewPackBody = "For honey boosts\nand time skips.";
        public const string ShopTabDewdrops = "Dewdrops";
        public const string ShopTabBoosts = "Boosts";
        public const string BoostsLocked = "Complete your first garden\nto unlock boosts.";
        public const string TimeSkipFormat = "Time Skip {0}";
        public const string TimeSkipBodyFormat = "{0} of honey right now:\n+{1}";
        public const string HoneyBoostFormat = "x{0} Honey {1}";
        public const string HoneyBoostBody = "Runs even while you are away.\nStacks with every other boost.";
        public const string BoostNone = "No boost running.";
        /// <summary>The running Dewdrops boost; {1} is a duration format with its own placeholders.</summary>
        public const string BoostRunningFormat = "x{0} honey: {1} left";
        public const string BoostQueuedFormat = "Next in line: x{0} for {1}";
        public const string BoostQueuedMoreFormat = "Next in line: x{0} for {1} (+{2} more)";
        public const string BoostChipFormat = "x{0} {1}";
        public const string ConfirmSpend = "Tap again";
        public const string NotEnoughDew = "Not enough Dewdrops.";
        public const string BoostBoughtFormat = "x{0} honey for {1}!";
        public const string TimeSkipDoneFormat = "+{0} honey!";
        public const string Owned = "Owned";
        public const string StoreBusy = "...";
        public const string StoreUnavailable = "Unavailable";
        public const string StoreConnecting = "Connecting to the store...";
        public const string PurchaseThanks = "Thank you!";
        public const string PurchaseCancelled = "Purchase cancelled.";
        public const string PurchaseFailed = "Purchase failed. Please try again.";
        public const string PurchasePending = "Payment pending. It arrives once it clears.";
        public const string RestorePurchases = "Restore Purchases";
        public const string RestoreDone = "Purchases restored.";
        public const string RestoreNothing = "Nothing to restore.";
        public const string PrivacyButton = "Privacy";

        public const string SettingsTitle = "Settings";
        public const string MusicOn = "Music: On";
        public const string MusicOff = "Music: Off";
        public const string SoundOn = "Sound: On";
        public const string SoundOff = "Sound: Off";
        public const string VibrationAll = "Vibration: On";
        public const string VibrationImportant = "Vibration: Big moments";
        public const string VibrationOff = "Vibration: Off";
        public const string PrivacyChoices = "Ad privacy choices";
        public const string PrivacyPolicy = "Privacy Policy";
        public const string DeleteData = "Delete my data";
        public const string DeleteDataConfirm = "Tap again: your garden and progress\nare deleted. This cannot be undone.";
        public const string KeepProgress = "Keep my progress";
        public const string VersionFormat = "Buzzfield {0}";

        /// <summary>A goal in progress: what to do, then how far along it is.</summary>
        public const string GoalProgressFormat = "{0}  {1}/{2}";
        public const string GoalClaimFormat = "Goal done! Tap for +{0}";
        public const string GoalShakeFlowers = "Shake {0} flowers";
        public const string GoalOwnBees = "Have {0} bees";
        public const string GoalSpeedLevel = "Speed to Lv {0}";
        public const string GoalHoneyValueLevel = "Honey Value to Lv {0}";
        public const string GoalBloomFlowers = "Bloom {0} flowers";
        public const string GoalEvolveOnce = "Evolve a bee";
        public const string GoalEvolveBees = "Evolve {0} times";
        public const string GoalGardenBloom = "Bloom the garden to {0}%";
        public const string GoalMoveQueenOnce = "Move the Queen";
        public const string GoalMoveQueen = "Move the Queen {0} times";
        public const string GoalAbilityLevels = "Buy {0} Queen ability levels";
        public const string GoalCompleteGardenOnce = "Complete a garden";
        public const string GoalCompleteGardens = "Complete {0} gardens";

        public const string GiftDoubleFormat = "Watch ad\n+{0} more";
        public const string PouchOffer = "Watch ad\nOpen the pouch";

        public const string DailyTitle = "Daily Dewdrops";
        public const string DailyDayFormat = "Day {0}";
        public const string DailyClaimFormat = "Claim +{0}";
        public const string DailyClaimed = "Claimed";
        public const string DailyHint = "Come back every day for more.\nA missed day starts over.";
        public const string DailyComeBack = "See you tomorrow!";
        public const string DailyMissed = "You missed a day:\nthe streak starts over.";
        public const string DailyWaitingForTime = "Waiting for the time server...";

        public const string HiveTitle = "Hive";
        public const string HiveHint = "Rare bees fly in every garden\nand stay when the Queen moves.";
        public const string RareBumblebee = "Bumblebee";
        public const string RareBlueBandedBee = "Blue Banded Bee";
        public const string RareMasonBee = "Mason Bee";
        public const string RareOrchidBee = "Orchid Bee";
        public const string RareLeafcutterBee = "Leafcutter Bee";
        public const string RareCarpenterBee = "Carpenter Bee";
        public const string RareBeeFree = "Free";
        /// <summary>The species that joins for free, waiting for its tap; {0} is its name in lower case.</summary>
        public const string RareBeeFreeFormat = "A {0} wants to move in!";
        public const string RareBeeLockedFormat = "Reach Garden {0}";
        public const string RareBeeJoinedFormat = "The {0} moved in!";
        public const string RareBeeLevelUpFormat = "{0} reached Lv {1}!";
        /// <summary>What the next level brings; {0} is the bonus it adds, such as "+3%".</summary>
        public const string RareBeeNextFormat = "Next level: {0}";
        public const string PercentUpFormat = "+{0}%";
        public const string PercentDownFormat = "-{0}%";

        /// <summary>What a rare bee's levels give, by effect; {0} is the amount.</summary>
        public const string RareEffectCarryFormat = "+{0}% nectar carried by every bee";
        public const string RareEffectShakeFormat = "+{0}% honey from pollen shakes";
        public const string RareEffectOfflineFormat = "+{0}% offline earning rate";
        /// <summary>Names the Orchid Bee's flowers (RareBee.flowers in the data).</summary>
        public const string RareEffectFlowerFormat = "+{0}% honey from Orchids and Lavender";
        public const string RareEffectDiscountFormat = "-{0}% Add Bee and Evolve cost";
        public const string RareEffectJellyFormat = "+{0}% Royal Jelly from Queen moves";

        /// <summary>Dewdrops a reward pays, next to the crystal icon.</summary>
        public const string DewGainFormat = "+{0}";
        public const string WelcomeGiftFormat = "Your bees found a honey jar: +{0}";

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
