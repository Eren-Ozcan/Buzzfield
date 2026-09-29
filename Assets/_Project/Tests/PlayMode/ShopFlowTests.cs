using System.Collections;
using System.Reflection;
using Buzzfield.Ads;
using Buzzfield.Core;
using Buzzfield.Flowers;
using Buzzfield.Game;
using Buzzfield.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// The shop through the mock store against the real Main scene: the panel, each
    /// product's grant, entitlements across restarts and Queen moves, and restoring
    /// purchases on a fresh save.
    /// </summary>
    public class ShopFlowTests
    {
        const float MockDelay = 0.05f;
        const float Timeout = 5f;
        const string AccountKey = "Buzzfield.Tests.PlayMode.MockStore.Owned";

        static readonly FieldInfo MockDelayField =
            typeof(StoreCatalog).GetField("mockDelaySeconds", BindingFlags.Instance | BindingFlags.NonPublic);

        GameManager game;
        StoreCatalog catalog;
        float savedDelay;

        [UnitySetUp]
        public IEnumerator LoadFresh()
        {
            TestSave.Clear();
            MockStoreService.AccountKeyOverride = AccountKey;
            MockStoreService.ClearAccount();
            yield return LoadMain();
        }

        [UnityTearDown]
        public IEnumerator RestoreSettings()
        {
            // The catalog asset is shared with the editor; leave it as it was.
            if (catalog != null)
                MockDelayField.SetValue(catalog, savedDelay);
            MockStoreService.ClearAccount();
            MockStoreService.AccountKeyOverride = null;
            yield return null;
        }

        IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
            if (catalog == null)
            {
                catalog = game.Store.Catalog;
                savedDelay = (float)MockDelayField.GetValue(catalog);
            }
            MockDelayField.SetValue(catalog, MockDelay);
            Assert.That(game.Store.IsReady, "The store connects in Start.");
        }

        IEnumerator Buy(string productId)
        {
            Assert.That(game.Store.TryPurchase(productId), $"{productId} is not buyable.");
            float end = Time.realtimeSinceStartup + Timeout;
            while (game.Store.IsPurchasing && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.That(game.Store.IsPurchasing, Is.False);
            yield return null;
        }

        static IEnumerator Shot(string name)
        {
            // Let text and layout settle before the render.
            yield return null;
            TestShots.SaveIfRequested($"shop_{name}_1080x1920.png", 1080, 1920);
            TestShots.SaveIfRequested($"shop_{name}_1440x1920.png", 1440, 1920);
        }

        [UnityTest]
        public IEnumerator ShopButton_OpensPanel_BackCloses()
        {
            var shop = Object.FindAnyObjectByType<ShopPanelView>();
            var back = Object.FindAnyObjectByType<BackButtonHandler>();
            Button shopButton = GameObject.Find("ShopButton").GetComponent<Button>();
            Assert.That(shop.IsOpen, Is.False);

            shopButton.onClick.Invoke();
            yield return null;
            Assert.That(shop.IsOpen);
            Assert.That(back.IsModalOpen, "The shop blocks ads like any panel.");
            foreach (ShopItemView item in shop.GetComponentsInChildren<ShopItemView>())
                Assert.That(item.ShownState, Is.EqualTo(ShopItemState.Buyable), item.ProductId);
            yield return Shot("open");

            back.HandleBack();
            Assert.That(shop.IsOpen, Is.False);
            Assert.That(back.IsQuitDialogOpen, Is.False, "Back closes the shop before asking to quit.");
        }

        [UnityTest]
        public IEnumerator DoubleHoney_DoublesDepositsAndOfflineRate_AndSurvivesRestart()
        {
            BigNumber rateBefore = game.TheoreticalHoneyPerSecond();
            yield return Buy(ProductIds.PermanentHoney2x);

            Assert.That(game.Economy.PurchasedMultiplier, Is.EqualTo(2));
            Assert.That(game.Economy.Deposit(100, 1, Time.timeAsDouble).ToDouble(), Is.EqualTo(200).Within(1e-6));
            Assert.That(game.TheoreticalHoneyPerSecond().ToDouble(), Is.EqualTo(rateBefore.ToDouble() * 2).Within(1e-6));

            yield return LoadMain();
            Assert.That(game.Store.Entitlements.permanentHoney2x);
            Assert.That(game.Economy.PurchasedMultiplier, Is.EqualTo(2));
            Assert.That(game.Store.GetState(ProductIds.PermanentHoney2x), Is.EqualTo(ShopItemState.Owned));
        }

        [UnityTest]
        public IEnumerator StarterPack_GrantsHoneyAndJellyOnce_ThenHides()
        {
            catalog.TryGet(ProductIds.StarterPack, out StoreProduct pack);
            BigNumber honeyBefore = game.Economy.Honey;
            var shop = Object.FindAnyObjectByType<ShopPanelView>();
            shop.Open();

            yield return Buy(ProductIds.StarterPack);

            Assert.That((game.Economy.Honey - honeyBefore).ToDouble(), Is.EqualTo(pack.honeyGrant).Within(1e-6));
            Assert.That(game.Prestige.RoyalJelly.ToDouble(), Is.EqualTo(pack.jellyGrant));
            Assert.That(game.Economy.RunHoneyEarned.IsZero, "Bought honey does not feed the Royal Jelly formula.");
            Assert.That(game.Store.GetState(ProductIds.StarterPack), Is.EqualTo(ShopItemState.Hidden));
            Assert.That(GameObject.Find(ProductIds.StarterPack), Is.Null, "The used offer's row is hidden.");
            Assert.That(game.Store.TryPurchase(ProductIds.StarterPack), Is.False);
            yield return Shot("after_starter");

            yield return LoadMain();
            Assert.That(game.Store.Entitlements.starterPackBought);
            Assert.That(game.Prestige.RoyalJelly.ToDouble(), Is.EqualTo(pack.jellyGrant));
        }

        [UnityTest]
        public IEnumerator Entitlements_SurviveTheQueenMove()
        {
            yield return Buy(ProductIds.RemoveAds);
            yield return Buy(ProductIds.PermanentHoney2x);

            BloomToMoveThreshold();
            game.Economy.Grant(BigNumber.Create(1, 12));
            Assert.That(game.TryMoveQueen());
            yield return null;

            Assert.That(game.Store.Entitlements.removeAds && game.Store.Entitlements.permanentHoney2x);
            Assert.That(game.Economy.PurchasedMultiplier, Is.EqualTo(2));
            Assert.That(game.Ads.ForcedAdsRemoved);
        }

        [UnityTest]
        public IEnumerator Restore_OnAFreshSave_BringsBackNonConsumables()
        {
            yield return Buy(ProductIds.RemoveAds);
            yield return Buy(ProductIds.StarterPack);

            // New install: the save is gone, the store account keeps the purchases.
            TestSave.Clear();
            yield return LoadMain();
            Assert.That(game.Store.Entitlements.removeAds, Is.False);
            Assert.That(game.Ads.ForcedAdsRemoved, Is.False);

            game.Store.RestorePurchases();
            yield return null;
            Assert.That(game.Store.Entitlements.removeAds);
            Assert.That(game.Ads.ForcedAdsRemoved);
            Assert.That(game.Store.Entitlements.starterPackBought, Is.False, "Consumables are not restored.");
            Assert.That(game.Prestige.RoyalJelly.IsZero);

            yield return LoadMain();
            Assert.That(game.Store.Entitlements.removeAds, "The restore was saved.");
        }

        void BloomToMoveThreshold()
        {
            float target = game.Prestige.MoveUnlockBloom;
            for (int pass = 0; pass < 64 && !PrestigeMath.IsUnlocked(game.Bloom.Fraction, target); pass++)
            {
                for (int i = 0; i < game.Flowers.Flowers.Count && !PrestigeMath.IsUnlocked(game.Bloom.Fraction, target); i++)
                {
                    Flower flower = game.Flowers.Flowers[i];
                    TestBloom.UntilBloomed(game, flower);
                }
            }
            Assert.That(PrestigeMath.IsUnlocked(game.Bloom.Fraction, target), "Could not bloom to the move threshold.");
        }
    }
}
