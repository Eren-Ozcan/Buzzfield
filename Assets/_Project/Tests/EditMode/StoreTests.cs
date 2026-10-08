using System.Collections.Generic;
using Buzzfield.Ads;
using Buzzfield.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Buzzfield.Tests.EditMode
{
    public class EntitlementsTests
    {
        [Test]
        public void NewEntitlements_OwnNothing()
        {
            var owned = new Entitlements();
            Assert.That(owned.Owns(ProductIds.RemoveAds), Is.False);
            Assert.That(owned.Owns(ProductIds.PermanentHoney2x), Is.False);
            Assert.That(owned.Owns(ProductIds.StarterPack), Is.False);
        }

        [Test]
        public void Grant_OnlyTheFirstTimeCounts()
        {
            var owned = new Entitlements();
            Assert.That(owned.Grant(ProductIds.PermanentHoney2x));
            Assert.That(owned.permanentHoney2x);
            Assert.That(owned.Grant(ProductIds.PermanentHoney2x), Is.False);
            Assert.That(owned.removeAds, Is.False);
        }

        [Test]
        public void UnknownProduct_IsNeverOwned()
        {
            var owned = new Entitlements();
            Assert.That(owned.Grant("gold_bar"), Is.False);
            Assert.That(owned.Owns("gold_bar"), Is.False);
        }

        [Test]
        public void CopyFrom_CopiesEveryFlag_AndNullClears()
        {
            var source = new Entitlements { removeAds = true, starterPackBought = true };
            var target = new Entitlements { permanentHoney2x = true };
            target.CopyFrom(source);
            Assert.That(target.removeAds && target.starterPackBought && !target.permanentHoney2x);
            target.CopyFrom(null);
            Assert.That(target.removeAds || target.starterPackBought, Is.False);
        }

        [Test]
        public void OrderMemory_KeepsTheLatestOrders()
        {
            var owned = new Entitlements();
            for (int i = 0; i < Entitlements.OrderMemory + 5; i++)
                owned.RecordOrder("order" + i);
            Assert.That(owned.grantedOrders.Count, Is.EqualTo(Entitlements.OrderMemory));
            Assert.That(owned.HasGrantedOrder("order0"), Is.False, "The oldest are forgotten.");
            Assert.That(owned.HasGrantedOrder("order" + (Entitlements.OrderMemory + 4)));
            owned.RecordOrder(null);
            owned.RecordOrder(string.Empty);
            Assert.That(owned.HasGrantedOrder(null), Is.False);
        }

        [Test]
        public void CopyFrom_CopiesOrders_AndIgnoresItself()
        {
            var source = new Entitlements();
            source.RecordOrder("GPA.1");
            var target = new Entitlements();
            target.RecordOrder("GPA.9");
            target.CopyFrom(source);
            Assert.That(target.HasGrantedOrder("GPA.1"));
            Assert.That(target.HasGrantedOrder("GPA.9"), Is.False);
            target.CopyFrom(target);
            Assert.That(target.HasGrantedOrder("GPA.1"));
        }

        [Test]
        public void JsonRoundTrip_KeepsEntitlements()
        {
            var data = new SaveData();
            data.entitlements.removeAds = true;
            data.entitlements.starterPackBought = true;
            SaveData read = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            Assert.That(read.entitlements.removeAds);
            Assert.That(read.entitlements.starterPackBought);
            Assert.That(read.entitlements.permanentHoney2x, Is.False);
        }
    }

    public class StoreManagerTests
    {
        private const string AccountKey = "Buzzfield.Tests.StoreManager.Owned";
        private const string DewPack = "dew_500";
        private StoreCatalog catalog;
        private MockStoreService service;
        private StoreManager store;
        private readonly List<string> granted = new List<string>();

        [SetUp]
        public void SetUp()
        {
            MockStoreService.ClearAccount(AccountKey);
            catalog = ScriptableObject.CreateInstance<StoreCatalog>();
            var so = new SerializedObject(catalog);
            SerializedProperty list = so.FindProperty("products");
            list.arraySize = 4;
            SetProduct(list.GetArrayElementAtIndex(0), ProductIds.RemoveAds, StoreProductType.NonConsumable, 0);
            SetProduct(list.GetArrayElementAtIndex(1), ProductIds.PermanentHoney2x, StoreProductType.NonConsumable, 0);
            SetProduct(list.GetArrayElementAtIndex(2), ProductIds.StarterPack, StoreProductType.Consumable, 250);
            SetProduct(list.GetArrayElementAtIndex(3), DewPack, StoreProductType.Consumable, 500);
            so.FindProperty("mockDelaySeconds").floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();

            service = new MockStoreService(catalog, AccountKey);
            store = new StoreManager(service, catalog);
            granted.Clear();
            store.OnProductGranted += p => granted.Add(p.id);
        }

        [TearDown]
        public void TearDown()
        {
            store.Dispose();
            MockStoreService.ClearAccount(AccountKey);
            Object.DestroyImmediate(catalog);
        }

        private static void SetProduct(SerializedProperty product, string id, StoreProductType type, int dewdrops)
        {
            product.FindPropertyRelative("id").stringValue = id;
            product.FindPropertyRelative("type").enumValueIndex = (int)type;
            product.FindPropertyRelative("fallbackPrice").stringValue = "$0.99";
            product.FindPropertyRelative("dewGrant").intValue = dewdrops;
        }

        private void SetResult(StoreCatalog.MockResult result)
        {
            var so = new SerializedObject(catalog);
            so.FindProperty("mockResult").enumValueIndex = (int)result;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private void Buy(string id)
        {
            Assert.That(store.TryPurchase(id));
            Assert.That(store.GetState(id), Is.EqualTo(ShopItemState.Busy));
            store.Tick(1.1f);
        }

        [Test]
        public void BeforeStart_EverythingUnavailable()
        {
            Assert.That(store.IsReady, Is.False);
            Assert.That(store.GetState(ProductIds.RemoveAds), Is.EqualTo(ShopItemState.Unavailable));
            Assert.That(store.TryPurchase(ProductIds.RemoveAds), Is.False);
            Assert.That(store.PriceText(ProductIds.RemoveAds), Is.EqualTo("$0.99"), "falls back to the catalog price");
        }

        [Test]
        public void UnknownProduct_IsHidden()
        {
            store.Start();
            Assert.That(store.GetState("gold_bar"), Is.EqualTo(ShopItemState.Hidden));
            Assert.That(store.TryPurchase("gold_bar"), Is.False);
        }

        [Test]
        public void Purchase_GrantsOnce_AndShowsOwned()
        {
            store.Start();
            Assert.That(store.GetState(ProductIds.PermanentHoney2x), Is.EqualTo(ShopItemState.Buyable));
            PurchaseResult? finished = null;
            store.OnPurchaseFinished += r => finished = r;

            Buy(ProductIds.PermanentHoney2x);

            Assert.That(finished.HasValue && finished.Value.IsSuccess);
            Assert.That(granted, Is.EqualTo(new[] { ProductIds.PermanentHoney2x }));
            Assert.That(store.Entitlements.permanentHoney2x);
            Assert.That(store.GetState(ProductIds.PermanentHoney2x), Is.EqualTo(ShopItemState.Owned));
            Assert.That(store.TryPurchase(ProductIds.PermanentHoney2x), Is.False);
        }

        [Test]
        public void WhilePurchasing_OtherProductsAreBusy()
        {
            store.Start();
            Assert.That(store.TryPurchase(ProductIds.RemoveAds));
            Assert.That(store.GetState(ProductIds.StarterPack), Is.EqualTo(ShopItemState.Busy));
            Assert.That(store.TryPurchase(ProductIds.StarterPack), Is.False);
        }

        [TestCase(StoreCatalog.MockResult.Fail)]
        [TestCase(StoreCatalog.MockResult.Cancel)]
        public void UnsuccessfulPurchase_GrantsNothing(StoreCatalog.MockResult mock)
        {
            store.Start();
            SetResult(mock);
            Buy(ProductIds.RemoveAds);
            Assert.That(granted, Is.Empty);
            Assert.That(store.Entitlements.removeAds, Is.False);
            Assert.That(store.GetState(ProductIds.RemoveAds), Is.EqualTo(ShopItemState.Buyable));
        }

        [Test]
        public void StarterPack_IsHiddenAfterItsOnePurchase()
        {
            store.Start();
            Buy(ProductIds.StarterPack);
            Assert.That(granted, Is.EqualTo(new[] { ProductIds.StarterPack }));
            Assert.That(store.GetState(ProductIds.StarterPack), Is.EqualTo(ShopItemState.Hidden));
            Assert.That(store.TryPurchase(ProductIds.StarterPack), Is.False);
        }

        [Test]
        public void Restore_GrantsMissingNonConsumablesOnly()
        {
            store.Start();
            Buy(ProductIds.RemoveAds);
            Buy(ProductIds.StarterPack);

            // A fresh install: same store account, empty save.
            var freshService = new MockStoreService(catalog, AccountKey);
            var fresh = new StoreManager(freshService, catalog);
            var freshGrants = new List<string>();
            int restored = -1;
            fresh.OnProductGranted += p => freshGrants.Add(p.id);
            fresh.OnRestoreFinished += n => restored = n;
            fresh.Start();
            fresh.RestorePurchases();

            Assert.That(restored, Is.EqualTo(1));
            Assert.That(freshGrants, Is.EqualTo(new[] { ProductIds.RemoveAds }));
            Assert.That(fresh.Entitlements.removeAds);
            Assert.That(fresh.Entitlements.starterPackBought, Is.False);

            // A second restore finds nothing new.
            fresh.RestorePurchases();
            Assert.That(restored, Is.EqualTo(0));
            fresh.Dispose();
        }

        [Test]
        public void RestoreBeforeStart_ReportsNothing()
        {
            int restored = -1;
            store.OnRestoreFinished += n => restored = n;
            store.RestorePurchases();
            Assert.That(restored, Is.EqualTo(0));
        }

        [Test]
        public void SavedEntitlements_ShowOwnedWithoutGranting()
        {
            store.Restore(new Entitlements { removeAds = true, starterPackBought = true });
            store.Start();
            Assert.That(store.GetState(ProductIds.RemoveAds), Is.EqualTo(ShopItemState.Owned));
            Assert.That(store.GetState(ProductIds.StarterPack), Is.EqualTo(ShopItemState.Hidden));
            Assert.That(granted, Is.Empty);
        }
        [Test]
        public void DewPack_CanBeBoughtAgainAndAgain()
        {
            store.Start();
            Buy(DewPack);
            Buy(DewPack);
            Assert.That(granted, Is.EqualTo(new[] { DewPack, DewPack }));
            Assert.That(store.GetState(DewPack), Is.EqualTo(ShopItemState.Buyable));
            Assert.That(store.Entitlements.grantedOrders.Count, Is.EqualTo(2), "Each mock order has its own id.");
        }

        [Test]
        public void SameOrderDeliveredTwice_IsGrantedOnce()
        {
            var fake = new FakeStoreService();
            var manager = new StoreManager(fake, catalog);
            var grants = new List<string>();
            manager.OnProductGranted += p => grants.Add(p.id);

            fake.Deliver(new PurchaseResult(DewPack, PurchaseStatus.Success, orderId: "GPA.1"));
            fake.Deliver(new PurchaseResult(DewPack, PurchaseStatus.Success, orderId: "GPA.1"));
            fake.Deliver(new PurchaseResult(DewPack, PurchaseStatus.Success, orderId: "GPA.2"));

            Assert.That(grants, Is.EqualTo(new[] { DewPack, DewPack }));
            manager.Dispose();
        }

        [Test]
        public void GrantedOrders_SurviveASaveAndStillBlockARedelivery()
        {
            var fake = new FakeStoreService();
            var manager = new StoreManager(fake, catalog);
            fake.Deliver(new PurchaseResult(DewPack, PurchaseStatus.Success, orderId: "GPA.7"));
            var data = new SaveData();
            data.entitlements.CopyFrom(manager.Entitlements);
            SaveData read = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
            manager.Dispose();

            // The app was killed before the store heard the order was done: it comes again.
            var again = new FakeStoreService();
            var restarted = new StoreManager(again, catalog);
            restarted.Restore(read.entitlements);
            int grants = 0;
            restarted.OnProductGranted += _ => grants++;
            again.Deliver(new PurchaseResult(DewPack, PurchaseStatus.Success, orderId: "GPA.7"));
            Assert.That(grants, Is.Zero);
            restarted.Dispose();
        }

        [Test]
        public void OrderWithoutId_IsGranted()
        {
            var fake = new FakeStoreService();
            var manager = new StoreManager(fake, catalog);
            int grants = 0;
            manager.OnProductGranted += _ => grants++;
            fake.Deliver(new PurchaseResult(DewPack, PurchaseStatus.Success));
            fake.Deliver(new PurchaseResult(DewPack, PurchaseStatus.Success));
            Assert.That(grants, Is.EqualTo(2));
            manager.Dispose();
        }

        /// <summary>A store that only hands over orders on its own, like redeliveries after a restart.</summary>
        private sealed class FakeStoreService : IStoreService
        {
            public bool IsInitialized => true;
            public bool IsPurchasing => false;
            public event System.Action<PurchaseResult> DeferredPurchaseCompleted;
            public void Initialize(IReadOnlyList<StoreProduct> catalog, System.Action<bool> onDone) => onDone?.Invoke(true);
            public IReadOnlyList<StoreProductInfo> GetProducts() => System.Array.Empty<StoreProductInfo>();
            public void Purchase(string productId, System.Action<PurchaseResult> onComplete) =>
                onComplete?.Invoke(new PurchaseResult(productId, PurchaseStatus.Failed));
            public void RestorePurchases(System.Action<IReadOnlyList<string>> onDone) => onDone?.Invoke(System.Array.Empty<string>());
            public void Deliver(PurchaseResult result) => DeferredPurchaseCompleted?.Invoke(result);
        }
    }
}
