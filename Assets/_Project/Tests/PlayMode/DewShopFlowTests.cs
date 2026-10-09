using System.Collections;
using System.Reflection;
using Buzzfield.Ads;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Game;
using Buzzfield.Save;
using Buzzfield.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// The shop's Dewdrops side against the real Main scene (docs/DEWDROPS.md, stage B): Dewdrop
    /// packs through the mock store, the Boosts tab (lock, time skips, honey boosts and their
    /// line, the second tap on big spends), the boost chip, and boosts across reloads and absences.
    /// </summary>
    public class DewShopFlowTests
    {
        const float MockDelay = 0.05f;
        const float Timeout = 5f;
        const string AccountKey = "Buzzfield.Tests.PlayMode.DewShop.Owned";

        static readonly FieldInfo MockDelayField =
            typeof(StoreCatalog).GetField("mockDelaySeconds", BindingFlags.Instance | BindingFlags.NonPublic);

        GameManager game;
        ShopPanelView shop;
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
            if (catalog != null)
                MockDelayField.SetValue(catalog, savedDelay);
            MockStoreService.ClearAccount();
            MockStoreService.AccountKeyOverride = null;
            yield return null;
        }

        IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            // One frame so Start (offline evaluation, the first save, the store) has run.
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            shop = Object.FindAnyObjectByType<ShopPanelView>();
            Assert.That(game, Is.Not.Null);
            if (catalog == null)
            {
                catalog = game.Store.Catalog;
                savedDelay = (float)MockDelayField.GetValue(catalog);
            }
            MockDelayField.SetValue(catalog, MockDelay);
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
            yield return null;
            TestShots.SaveIfRequested($"dewshop_{name}_1080x1920.png", 1080, 1920);
            TestShots.SaveIfRequested($"dewshop_{name}_1440x1920.png", 1440, 1920);
        }

        int OfferIndex(string id)
        {
            for (int i = 0; i < game.DewSettings.Offers.Count; i++)
            {
                if (game.DewSettings.Offers[i].id == id)
                    return i;
            }
            Assert.Fail($"No offer {id}.");
            return -1;
        }

        DewOfferRowView Row(int offerIndex)
        {
            foreach (DewOfferRowView row in shop.BoostsTab.Rows)
            {
                if (row.OfferIndex == offerIndex)
                    return row;
            }
            Assert.Fail($"No row for offer {offerIndex}.");
            return null;
        }

        /// <summary>Completes the first garden, which opens the Boosts tab; its Dewdrops reward is spent so balances start at zero.</summary>
        IEnumerator Unlock()
        {
            game.DebugCompleteGarden();
            yield return null;
            Assert.That(game.BoostsUnlocked);
            Assert.That(game.TrySpendDew(game.Dew.Balance, "test"));
        }

        [UnityTest]
        public IEnumerator DewPack_AddsDewdropsEveryTime_AndSurvivesAReload()
        {
            shop.Open();
            Assert.That(shop.Tab, Is.EqualTo(ShopTab.Dewdrops));
            Assert.That(GameObject.Find("dew_80"), Is.Not.Null, "The pack has a row.");
            yield return Shot("dewdrops_tab");

            yield return Buy("dew_80");
            yield return Buy("dew_80");
            Assert.That(game.Dew.Balance, Is.EqualTo(160));
            Assert.That(game.Store.GetState("dew_80"), Is.EqualTo(ShopItemState.Buyable), "Packs never run out.");

            yield return LoadMain();
            Assert.That(game.Dew.Balance, Is.EqualTo(160), "Every grant saves.");
        }

        [UnityTest]
        public IEnumerator StarterPack_GivesDewdropsAndHoneyFromTheRate()
        {
            catalog.TryGet(ProductIds.StarterPack, out StoreProduct pack);
            double expected = RewardMath.IncomeSeconds(game.TheoreticalHoneyPerSecond(), pack.honeySecondsGrant, catalog.MinimumHoneyGrant).ToDouble();
            double honeyBefore = game.Economy.Honey.ToDouble();

            yield return Buy(ProductIds.StarterPack);

            Assert.That(game.Dew.Balance, Is.EqualTo(pack.dewGrant));
            // Bees keep depositing while the purchase runs.
            Assert.That(game.Economy.Honey.ToDouble() - honeyBefore, Is.EqualTo(expected).Within(expected * 0.05 + 50));
            Assert.That(game.Prestige.RoyalJelly.IsZero, "Royal Jelly is never sold.");
        }

        [UnityTest]
        public IEnumerator BoostsTab_IsLockedUntilTheFirstGardenIsComplete()
        {
            game.EarnDew(500, "test", Vector2.zero);
            shop.OpenBoosts();
            yield return null;
            Assert.That(shop.Tab, Is.EqualTo(ShopTab.Boosts));
            Assert.That(GameObject.Find("LockedNote"), Is.Not.Null, "The note shows while locked.");
            Assert.That(game.TryBuyDewOffer(0), Is.False);
            Assert.That(game.Dew.Balance, Is.EqualTo(500));
            yield return Shot("boosts_locked");

            yield return Unlock();
            game.EarnDew(500, "test", Vector2.zero);
            yield return null;
            Assert.That(GameObject.Find("LockedNote"), Is.Null);
            Assert.That(Row(0).ShownState, Is.EqualTo(DewOfferRowView.State.Buyable));
        }

        [UnityTest]
        public IEnumerator TimeSkip_PaysHoneyNow_OutsideTheRun()
        {
            yield return Unlock();
            game.EarnDew(100, "test", Vector2.zero);
            int index = OfferIndex("time_skip_1h");
            DewOffer offer = game.DewSettings.Offers[index];
            double expected = game.TimeSkipHoney(index).ToDouble();
            double honeyBefore = game.Economy.Honey.ToDouble();
            double runBefore = game.Economy.RunHoneyEarned.ToDouble();

            Assert.That(game.TryBuyDewOffer(index));

            Assert.That(game.Dew.Balance, Is.EqualTo(100 - offer.cost));
            Assert.That(game.Economy.Honey.ToDouble() - honeyBefore, Is.EqualTo(expected).Within(expected * 1e-6));
            Assert.That(game.Economy.RunHoneyEarned.ToDouble(), Is.EqualTo(runBefore), "A time skip never feeds Royal Jelly.");
            Assert.That(expected, Is.GreaterThanOrEqualTo(game.TheoreticalHoneyPerSecond().ToDouble() * offer.seconds * 0.999));
        }

        [UnityTest]
        public IEnumerator HoneyBoosts_Stack_AndTheWeakerWaitsInLine()
        {
            yield return Unlock();
            game.EarnDew(500, "test", Vector2.zero);
            var chip = Object.FindAnyObjectByType<DewBoostChipView>();
            Assert.That(chip.IsShown, Is.False);

            Assert.That(game.TryBuyDewOffer(OfferIndex("honey_2x_1h")));
            yield return null;
            Assert.That(game.Economy.BoostMultiplier, Is.EqualTo(2));
            Assert.That(chip.IsShown);

            Assert.That(game.TryBuyDewOffer(OfferIndex("honey_3x_30m")));
            yield return null;
            Assert.That(game.Economy.BoostMultiplier, Is.EqualTo(3), "The stronger boost runs first.");
            Assert.That(game.Boosts.DewBoosts.Count, Is.EqualTo(2));
            Assert.That(game.Boosts.DewBoosts.Entries[1].secondsLeft, Is.EqualTo(3600).Within(2), "The 2x waits with its time.");

            // A rewarded ad boost multiplies on top.
            game.Boosts.StartRewardedHoney(GameClock.DeviceUtc);
            yield return null;
            Assert.That(game.Economy.BoostMultiplier, Is.EqualTo(3 * game.Boosts.RewardedHoneyMultiplier));

            shop.OpenBoosts();
            yield return null;
            Assert.That(GameObject.Find("QueueText").GetComponent<TMPro.TMP_Text>().text, Does.Contain("x2"));
            yield return Shot("boosts_running");

            // The chip opens the Boosts tab.
            shop.Close();
            chip.GetComponentInChildren<Button>(true).onClick.Invoke();
            Assert.That(shop.IsOpen);
            Assert.That(shop.Tab, Is.EqualTo(ShopTab.Boosts));
        }

        [UnityTest]
        public IEnumerator BigSpend_NeedsASecondTap_SmallOneDoesNot()
        {
            yield return Unlock();
            game.EarnDew(150, "test", Vector2.zero);
            shop.OpenBoosts();
            yield return null;

            int big = OfferIndex("time_skip_4h");
            Assert.That(game.DewSettings.Offers[big].cost, Is.GreaterThan(game.DewSettings.ConfirmAbove));
            Row(big).BuyButton.onClick.Invoke();
            yield return null;
            Assert.That(game.Dew.Balance, Is.EqualTo(150), "The first tap only asks.");
            Assert.That(Row(big).ShownState, Is.EqualTo(DewOfferRowView.State.Confirming));
            yield return Shot("boosts_confirm");
            Row(big).BuyButton.onClick.Invoke();
            yield return null;
            Assert.That(game.Dew.Balance, Is.EqualTo(150 - game.DewSettings.Offers[big].cost));

            int small = OfferIndex("time_skip_1h");
            Assert.That(game.DewSettings.Offers[small].cost, Is.LessThanOrEqualTo(game.DewSettings.ConfirmAbove));
            long before = game.Dew.Balance;
            Row(small).BuyButton.onClick.Invoke();
            yield return null;
            Assert.That(game.Dew.Balance, Is.EqualTo(before - game.DewSettings.Offers[small].cost), "One tap is enough.");
        }

        [UnityTest]
        public IEnumerator Confirm_RunsOut_AndShortBalanceDisablesTheRow()
        {
            yield return Unlock();
            game.EarnDew(120, "test", Vector2.zero);
            shop.OpenBoosts();
            yield return null;
            int big = OfferIndex("time_skip_4h");
            Row(big).BuyButton.onClick.Invoke();
            Assert.That(shop.BoostsTab.ConfirmingIndex, Is.EqualTo(big));
            yield return new WaitForSecondsRealtime(game.DewSettings.ConfirmSeconds + 0.2f);
            Assert.That(shop.BoostsTab.ConfirmingIndex, Is.EqualTo(-1));
            Assert.That(Row(big).ShownState, Is.EqualTo(DewOfferRowView.State.Buyable));

            int dearest = OfferIndex("time_skip_8h");
            Assert.That(Row(dearest).ShownState, Is.EqualTo(DewOfferRowView.State.TooExpensive));
            Assert.That(Row(dearest).BuyButton.interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator HoneyBoost_SurvivesAReload_AndPaysForTheTimeAway()
        {
            yield return Unlock();
            game.EarnDew(100, "test", Vector2.zero);
            Assert.That(game.TryBuyDewOffer(OfferIndex("honey_2x_1h")));
            game.SaveNow();

            // Left an hour ago with the boost just bought: the whole absence was boosted.
            SaveManager files = TestSave.Files();
            SaveData data = files.Load();
            data.welcomeGiftGiven = true;
            data.clock.lastSeenUtc -= 3600;
            data.clock.lastDeviceUtc -= 3600;
            data.clock.lastMonotonicSeconds -= 3600;
            data.dewBoostAnchorUtc -= 3600;
            data.offlineRate = BigNumberData.From(2);
            Assert.That(files.Save(data));
            double honey = game.Economy.Honey.ToDouble();

            yield return LoadMain();

            float efficiency = ((OfflineSettings)typeof(GameManager).GetField("offlineSettings", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(game)).Efficiency;
            Assert.That(game.LastOffline.Status, Is.EqualTo(OfflineStatus.Credited));
            Assert.That(game.LastOffline.Amount.ToDouble(), Is.EqualTo(2 * 3600 * efficiency * 2).Within(2 * 20 * efficiency * 2 + 1),
                "Each boosted second pays double.");
            Assert.That(game.Boosts.DewBoosts.Count, Is.Zero, "The hour away used the boost up.");
            Assert.That(game.Economy.Honey.ToDouble(), Is.GreaterThan(honey));
        }

        [UnityTest]
        public IEnumerator HoneyBoost_KeepsRunningAcrossAReload()
        {
            yield return Unlock();
            game.EarnDew(100, "test", Vector2.zero);
            Assert.That(game.TryBuyDewOffer(OfferIndex("honey_3x_30m")));
            yield return LoadMain();
            yield return null;
            Assert.That(game.Boosts.DewBoosts.Count, Is.EqualTo(1));
            Assert.That(game.Boosts.DewBoosts.SecondsLeft, Is.InRange(1790, 1800));
            Assert.That(game.Economy.BoostMultiplier, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator ShopBadge_WaitsForTheFirstMinutesOfPlay()
        {
            GameObject badge = GameObject.Find("ShopButton").transform.Find("NewBadge").gameObject;
            yield return null;
            Assert.That(badge.activeSelf, Is.False, "Nothing points at the shop in the first minutes.");
            game.Stats.playSeconds = catalog.BadgeAfterPlaySeconds + 1;
            yield return null;
            yield return null;
            Assert.That(badge.activeSelf);
        }
    }
}
