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

            public void RequestConsent(Action onDone) => pending = onDone;

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
            Configure(delay: 1f, failure: false, gap: 30f);
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

        private void Configure(float delay, bool failure, float gap)
        {
            var so = new SerializedObject(settings);
            so.FindProperty("mockDelaySeconds").floatValue = delay;
            so.FindProperty("mockSimulateFailure").boolValue = failure;
            so.FindProperty("minSecondsBetweenFullScreenAds").floatValue = gap;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Consent, init and the first load.</summary>
        private void StartAndLoad()
        {
            ads.Start();
            consent.Grant();
            ads.Tick(1.1f);
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
            Configure(delay: 1f, failure: true, gap: 30f);
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
        public void OpeningStampsTheSharedTime_AndTheGapBlocksTheNextAd()
        {
            StartAndLoad();
            ads.TryShowRewarded(null);
            Assert.That(ads.LastFullScreenAdUtc, Is.EqualTo(now));
            ads.Tick(1.1f); // closes and starts the next load
            ads.Tick(1.1f); // next ad loaded
            Assert.That(service.IsRewardedReady);
            Assert.That(ads.CanShowRewarded(), Is.False);
            Assert.That(ads.SecondsUntilAllowed(), Is.EqualTo(30));

            now += 30;
            Assert.That(ads.CanShowRewarded());
        }

        [Test]
        public void RestoredStamp_IsHonoured()
        {
            ads.Restore(now - 10);
            StartAndLoad();
            Assert.That(ads.CanShowRewarded(), Is.False);
            Assert.That(ads.SecondsUntilAllowed(), Is.EqualTo(20));
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
