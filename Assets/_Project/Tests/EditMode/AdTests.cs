using System;
using Buzzfield.Ads;
using Buzzfield.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Buzzfield.Tests.EditMode
{
    public class AdPacingTests
    {
        [Test]
        public void NoStamp_Allowed()
        {
            Assert.That(AdPacing.IsAllowed(1000, 0, 30));
        }

        [Test]
        public void WaitsForTheGap()
        {
            Assert.That(AdPacing.SecondsUntilAllowed(1010, 1000, 30), Is.EqualTo(20));
            Assert.That(AdPacing.IsAllowed(1029.9, 1000, 30), Is.False);
            Assert.That(AdPacing.IsAllowed(1030, 1000, 30));
        }

        [Test]
        public void ClockMovedBack_NeverLocksLongerThanOneGap()
        {
            // Stamp slightly ahead: wait out the rest of the gap from the stamp.
            Assert.That(AdPacing.SecondsUntilAllowed(990, 1000, 30), Is.EqualTo(40));
            // Stamp far ahead: ignored.
            Assert.That(AdPacing.IsAllowed(500, 1000, 30));
        }

        [Test]
        public void ZeroGap_AlwaysAllowed()
        {
            Assert.That(AdPacing.IsAllowed(1000, 1000, 0));
        }

        private static ForcedAdBlock Block(double now = 10_000, double lastAd = 0, bool removed = false,
            double playSeconds = 2000, int moves = 1) =>
            AdPacing.InterstitialBlock(now, lastAd, 240, removed, playSeconds, 1200, moves, 1);

        [Test]
        public void Interstitial_AllowedForAnEstablishedPlayer()
        {
            Assert.That(Block(), Is.EqualTo(ForcedAdBlock.None));
        }

        [Test]
        public void Interstitial_RemoveAdsWinsOverEverything()
        {
            Assert.That(Block(removed: true, playSeconds: 0, moves: 0), Is.EqualTo(ForcedAdBlock.Removed));
        }

        [Test]
        public void Interstitial_NewPlayersAreLeftAlone()
        {
            Assert.That(Block(playSeconds: 1199), Is.EqualTo(ForcedAdBlock.TooEarly));
            Assert.That(Block(moves: 0), Is.EqualTo(ForcedAdBlock.TooEarly));
        }

        [Test]
        public void Interstitial_WaitsForTheSharedCooldown()
        {
            Assert.That(Block(now: 10_000, lastAd: 10_000 - 100), Is.EqualTo(ForcedAdBlock.Cooldown));
            Assert.That(Block(now: 10_000, lastAd: 10_000 - 240), Is.EqualTo(ForcedAdBlock.None));
        }
    }

    public class TcfConsentTests
    {
        [Test]
        public void OutsideTheEea_AllGranted()
        {
            TcfConsent c = TcfConsent.From(0, null);
            Assert.That(c.AnalyticsStorage && c.AdStorage && c.AdUserData && c.AdPersonalization);
        }

        [Test]
        public void InsideTheEea_ReadsThePurposes()
        {
            // Purposes 1 and 7 given, 3 and 4 refused.
            TcfConsent c = TcfConsent.From(1, "1100001");
            Assert.That(c.AnalyticsStorage && c.AdStorage && c.AdUserData);
            Assert.That(c.AdPersonalization, Is.False);
        }

        [Test]
        public void InsideTheEea_NoAnswerMeansDenied()
        {
            TcfConsent c = TcfConsent.From(1, "");
            Assert.That(c.AnalyticsStorage || c.AdStorage || c.AdUserData || c.AdPersonalization, Is.False);
        }
    }

    public class AdManagerTests
    {
        private AdSettings settings;
        private MockAdService service;
        private DeferredConsent consent;
        private bool modalOpen;
        private double now;
        private AdManager ads;

        private sealed class DeferredConsent : IConsentService
        {
            private Action pending;
            public bool CanRequestAds { get; private set; }
            public bool Asked => pending != null;
            public bool PrivacyOptionsRequired => false;

#pragma warning disable 0067
            public event Action<TcfConsent> ConsentChanged;
#pragma warning restore 0067

            public void RequestConsent(Action onDone) => pending = onDone;

            public void ShowPrivacyOptions(Action onDone) => onDone?.Invoke();

            public void Grant()
            {
                CanRequestAds = true;
                pending?.Invoke();
            }
        }

        [SetUp]
        public void SetUp()
        {
            settings = ScriptableObject.CreateInstance<AdSettings>();
            Configure(delay: 1f, failure: false);
            service = new MockAdService(settings);
            consent = new DeferredConsent();
            modalOpen = false;
            now = 10_000;
            ads = new AdManager(service, consent, settings, () => modalOpen, () => now);
        }

        [TearDown]
        public void TearDown()
        {
            ads.Dispose();
            UnityEngine.Object.DestroyImmediate(settings);
        }

        private void Configure(float delay, bool failure)
        {
            var so = new SerializedObject(settings);
            so.FindProperty("mockDelaySeconds").floatValue = delay;
            so.FindProperty("mockSimulateFailure").boolValue = failure;
            so.FindProperty("interstitialCooldownSeconds").floatValue = 240f;
            so.FindProperty("interstitialMinPlaySeconds").floatValue = 1200f;
            so.FindProperty("interstitialMinQueenMoves").intValue = 1;
            so.FindProperty("breakSettleSeconds").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Consent, init and the first load.</summary>
        private void StartAndLoad()
        {
            ads.Start();
            consent.Grant();
            ads.Tick(1.1f);
        }

        /// <summary>One frame of an established player's session.</summary>
        private void Frame(float dt, bool calm = true, double playSeconds = 2000, int moves = 1)
        {
            ads.TickInterstitial(dt, calm, playSeconds, moves);
            ads.Tick(dt);
        }

        [Test]
        public void ConsentRunsBeforeTheSdkStarts()
        {
            ads.Start();
            Assert.That(consent.Asked);
            Assert.That(service.IsInitialized, Is.False);
            ads.Tick(5f);
            Assert.That(ads.CanShowRewarded(), Is.False);

            consent.Grant();
            Assert.That(service.IsInitialized);
            Assert.That(service.IsLoading);
        }

        [Test]
        public void LoadsAfterTheDelay_ThenShowsAndRewards()
        {
            StartAndLoad();
            Assert.That(ads.CanShowRewarded());

            bool? rewarded = null;
            Assert.That(ads.TryShowRewarded(r => rewarded = r));
            Assert.That(ads.IsShowing);
            Assert.That(rewarded, Is.Null);
            ads.Tick(1.1f);
            Assert.That(rewarded, Is.True);
            Assert.That(ads.IsShowing, Is.False);
        }

        [Test]
        public void SimulatedFailure_ClosesWithoutReward()
        {
            Configure(delay: 1f, failure: true);
            StartAndLoad();
            bool? rewarded = null;
            ads.TryShowRewarded(r => rewarded = r);
            ads.Tick(1.1f);
            Assert.That(rewarded, Is.False);
        }

        [Test]
        public void OpenModal_BlocksAdsExceptItsOwnButton()
        {
            StartAndLoad();
            modalOpen = true;
            Assert.That(ads.CanShowRewarded(), Is.False);
            Assert.That(ads.TryShowRewarded(_ => Assert.Fail("must not call back")), Is.False);
            Assert.That(ads.CanShowRewarded(fromOpenPanel: true));
        }

        [Test]
        public void RewardedAds_StampTheSharedTime_ButNeverWaitForIt()
        {
            StartAndLoad();
            ads.TryShowRewarded(null);
            Assert.That(ads.LastFullScreenAdUtc, Is.EqualTo(now));
            ads.Tick(1.1f); // closes and starts the next load
            ads.Tick(1.1f); // next ad loaded
            Assert.That(ads.CanShowRewarded(), "the player asked for it");
            Assert.That(ads.InterstitialCooldownLeft(), Is.EqualTo(240));
        }

        [Test]
        public void RestoredStamp_IsHonouredByInterstitials()
        {
            ads.Restore(now - 10);
            StartAndLoad();
            Assert.That(ads.InterstitialCooldownLeft(), Is.EqualTo(230));
            Assert.That(ads.InterstitialBlock(2000, 1), Is.EqualTo(ForcedAdBlock.Cooldown));
        }

        [Test]
        public void NaturalBreak_PlaysOnceTheScreenHasBeenCalm()
        {
            StartAndLoad();
            Frame(0.1f);
            Frame(1.1f); // interstitial loaded
            Assert.That(service.IsInterstitialReady);

            bool closed = false;
            ads.InterstitialClosed += () => closed = true;
            ads.MarkNaturalBreak("garden_complete");
            Frame(0.5f, calm: false); // the celebration is still on screen
            Frame(0.5f);
            Assert.That(ads.IsShowing, Is.False, "must settle for a full second first");
            Frame(0.6f);
            Assert.That(ads.IsShowing);
            Assert.That(ads.PendingBreak, Is.Null);
            Assert.That(ads.LastFullScreenAdUtc, Is.EqualTo(now));

            Frame(1.1f);
            Assert.That(closed);
        }

        [TestCase(true, 2000, 1, "Removed")]
        [TestCase(false, 100, 1, "TooEarly")]
        [TestCase(false, 2000, 0, "TooEarly")]
        public void NaturalBreak_SkippedForPayingOrNewPlayers(bool removed, double playSeconds, int moves, string reason)
        {
            ads.ForcedAdsRemoved = removed;
            StartAndLoad();
            string skipped = null;
            ads.BreakSkipped += (_, why) => skipped = why;
            ads.MarkNaturalBreak("queen_move");
            Frame(1.1f, playSeconds: playSeconds, moves: moves);
            Frame(1.1f, playSeconds: playSeconds, moves: moves);
            Assert.That(ads.IsShowing, Is.False);
            Assert.That(service.IsInterstitialLoading || service.IsInterstitialReady, Is.False, "never even requested");
            Assert.That(skipped, Is.EqualTo(reason));
        }

        [Test]
        public void NaturalBreak_SkippedInsideTheCooldown()
        {
            StartAndLoad();
            ads.TryShowRewarded(null);
            ads.Tick(1.1f);
            string skipped = null;
            ads.BreakSkipped += (_, why) => skipped = why;
            ads.MarkNaturalBreak("queen_move");
            Frame(1.1f);
            Frame(1.1f);
            Assert.That(ads.IsShowing, Is.False);
            Assert.That(skipped, Is.EqualTo("Cooldown"));
        }

        [Test]
        public void ClearedBreak_NeverPlays()
        {
            StartAndLoad();
            Frame(1.1f);
            ads.MarkNaturalBreak("garden_complete");
            ads.ClearPendingBreak();
            Frame(1.1f);
            Frame(1.1f);
            Assert.That(ads.IsShowing, Is.False);
        }
    }

    public class MockStoreServiceTests
    {
        private const string AccountKey = "Buzzfield.Tests.MockStore.Owned";
        private StoreCatalog catalog;
        private MockStoreService store;

        [SetUp]
        public void SetUp()
        {
            MockStoreService.ClearAccount(AccountKey);
            catalog = ScriptableObject.CreateInstance<StoreCatalog>();
            var so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("products");
            list.arraySize = 2;
            SetProduct(list.GetArrayElementAtIndex(0), "remove_ads", StoreProductType.NonConsumable);
            SetProduct(list.GetArrayElementAtIndex(1), "starter_pack", StoreProductType.Consumable);
            so.FindProperty("mockDelaySeconds").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
            store = new MockStoreService(catalog, AccountKey);
        }

        [TearDown]
        public void TearDown()
        {
            MockStoreService.ClearAccount(AccountKey);
            UnityEngine.Object.DestroyImmediate(catalog);
        }

        private static void SetProduct(SerializedProperty product, string id, StoreProductType type)
        {
            product.FindPropertyRelative("id").stringValue = id;
            product.FindPropertyRelative("type").enumValueIndex = (int)type;
            product.FindPropertyRelative("fallbackPrice").stringValue = "$1.99";
        }

        private void SetResult(StoreCatalog.MockResult result)
        {
            var so = new SerializedObject(catalog);
            so.FindProperty("mockResult").enumValueIndex = (int)result;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private PurchaseResult? Buy(string id)
        {
            PurchaseResult? result = null;
            store.Purchase(id, r => result = r);
            Assert.That(result, Is.Null, "completes only after the delay");
            store.Tick(1.1f);
            return result;
        }

        [Test]
        public void BeforeInitialize_PurchaseFails()
        {
            PurchaseResult? result = null;
            store.Purchase("remove_ads", r => result = r);
            Assert.That(result.HasValue && result.Value.Status == PurchaseStatus.Failed);
            Assert.That(store.GetProducts(), Is.Empty);
        }

        [Test]
        public void Initialize_ListsCatalogWithFallbackPrices()
        {
            store.Initialize(catalog.Products, ok => Assert.That(ok));
            Assert.That(store.GetProducts().Count, Is.EqualTo(2));
            Assert.That(store.GetProducts()[0].Id, Is.EqualTo("remove_ads"));
            Assert.That(store.GetProducts()[0].PriceText, Is.EqualTo("$1.99"));
        }

        [TestCase(StoreCatalog.MockResult.Success, PurchaseStatus.Success)]
        [TestCase(StoreCatalog.MockResult.Cancel, PurchaseStatus.Cancelled)]
        [TestCase(StoreCatalog.MockResult.Fail, PurchaseStatus.Failed)]
        public void Purchase_ReportsTheMockResult(StoreCatalog.MockResult mock, PurchaseStatus expected)
        {
            store.Initialize(catalog.Products, null);
            SetResult(mock);
            PurchaseResult? result = Buy("remove_ads");
            Assert.That(result.HasValue);
            Assert.That(result.Value.ProductId, Is.EqualTo("remove_ads"));
            Assert.That(result.Value.Status, Is.EqualTo(expected));
        }

        [Test]
        public void UnknownProduct_Fails()
        {
            store.Initialize(catalog.Products, null);
            PurchaseResult? result = null;
            store.Purchase("gold_bar", r => result = r);
            Assert.That(result.HasValue && result.Value.Status == PurchaseStatus.Failed);
        }

        [Test]
        public void OnePurchaseAtATime()
        {
            store.Initialize(catalog.Products, null);
            store.Purchase("remove_ads", null);
            PurchaseResult? second = null;
            store.Purchase("starter_pack", r => second = r);
            Assert.That(second.HasValue && second.Value.Status == PurchaseStatus.Failed);
        }

        [Test]
        public void Restore_ReturnsOwnedNonConsumablesOnly_AcrossInstances()
        {
            store.Initialize(catalog.Products, null);
            Buy("remove_ads");
            Buy("starter_pack");

            var fresh = new MockStoreService(catalog, AccountKey);
            fresh.Initialize(catalog.Products, null);
            string[] restored = null;
            fresh.RestorePurchases(ids => restored = new System.Collections.Generic.List<string>(ids).ToArray());
            Assert.That(restored, Is.EqualTo(new[] { "remove_ads" }));
        }

        [Test]
        public void FailedPurchase_OwnsNothing()
        {
            store.Initialize(catalog.Products, null);
            SetResult(StoreCatalog.MockResult.Fail);
            Buy("remove_ads");
            int count = -1;
            store.RestorePurchases(ids => count = ids.Count);
            Assert.That(count, Is.EqualTo(0));
        }
    }
}
