using System.Collections;
using System.IO;
using System.Reflection;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Game;
using Buzzfield.Save;
using Buzzfield.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// Save/load, backup fallback, offline earnings, back button and safe area
    /// against the real Main scene.
    /// </summary>
    public class SaveFlowTests
    {
        GameManager game;

        [UnitySetUp]
        public IEnumerator LoadFresh()
        {
            TestSave.Clear();
            yield return LoadMain();
        }

        [UnityTearDown]
        public IEnumerator ResetTime()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            // One frame so Start (offline evaluation and the first save) has run.
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
        }

        /// <summary>Moves the saved clock readings by the given seconds (negative = the player left earlier) and optionally sets the saved rate.</summary>
        static void ShiftSavedClock(double seen, double device, double monotonic, double? rate = null)
        {
            SaveManager files = TestSave.Files();
            SaveData data = files.Load();
            Assert.That(data, Is.Not.Null);
            data.clock.lastSeenUtc += seen;
            data.clock.lastDeviceUtc += device;
            data.clock.lastMonotonicSeconds += monotonic;
            // Boot estimate = device - monotonic; keep it consistent with the shifted readings.
            data.clock.lastBootUtc += device - monotonic;
            if (rate.HasValue)
                data.offlineRate = BigNumberData.From(rate.Value);
            Assert.That(files.Save(data));
        }

        static T Hidden<T>(object target, string field) =>
            (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        [UnityTest]
        public IEnumerator SaveAndReload_RestoresRunAndGarden()
        {
            game.Economy.Grant(BigNumber.FromDouble(5000));
            Assert.That(game.Upgrades.TryAddBee());
            Assert.That(game.Upgrades.TryAddBee());
            Assert.That(game.Upgrades.TryBuySpeed());
            game.Flowers.RestoreSlot(0, true, 0.5f, 2.5);
            game.Stats.flowersBloomed = 3;
            game.SaveNow();
            double honey = game.Economy.Honey.ToDouble();

            yield return LoadMain();

            Assert.That(game.LastLoadSource, Is.EqualTo(SaveSource.Main));
            Assert.That(game.Bees.Count, Is.EqualTo(3));
            Assert.That(game.Upgrades.BeesBought, Is.EqualTo(2));
            Assert.That(game.Upgrades.SpeedLevel, Is.EqualTo(1));
            Assert.That(game.Upgrades.AddBeeCost.ToDouble(), Is.GreaterThan(4), "Add Bee cost continues from the saved count.");
            Assert.That(game.Flowers.Flowers[0].Bloom, Is.EqualTo(0.5f));
            Assert.That(game.Stats.flowersBloomed, Is.EqualTo(3));
            // A fraction of a second passed between the scenes; offline pays at most a little of it.
            Assert.That(game.Economy.Honey.ToDouble(), Is.InRange(honey, honey + 50));
            Assert.That(game.IsWelcomeBackOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator SaveMidEvolve_KeepsTheMergedBee()
        {
            game.Economy.Grant(BigNumber.FromDouble(1_000_000));
            for (int i = 0; i < 3; i++)
                Assert.That(game.Upgrades.TryAddBee());
            Assert.That(game.Upgrades.TryEvolve());
            game.SaveNow();

            SaveData data = TestSave.Files().Load();
            Assert.That(data.beesPerTier, Is.EqualTo(new[] { 1, 1, 0 }));

            yield return LoadMain();
            Assert.That(game.Bees.CountOfTier(0), Is.EqualTo(1));
            Assert.That(game.Bees.CountOfTier(1), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DamagedMain_LoadsBackup()
        {
            game.Economy.Grant(BigNumber.FromDouble(500));
            game.SaveNow();
            game.Economy.Grant(BigNumber.FromDouble(500));
            game.SaveNow();
            SaveManager files = TestSave.Files();
            File.WriteAllText(files.MainPath, "{ half a save");

            yield return LoadMain();

            Assert.That(game.LastLoadSource, Is.EqualTo(SaveSource.Backup));
            Assert.That(game.Economy.Honey.ToDouble(), Is.InRange(500, 550));
        }

        [UnityTest]
        public IEnumerator DamagedMainAndBackup_StartsNewGame()
        {
            game.Economy.Grant(BigNumber.FromDouble(500));
            game.SaveNow();
            SaveManager files = TestSave.Files();
            File.WriteAllText(files.MainPath, "garbage");
            File.WriteAllText(files.BackupPath, "BZ1|nope|{}");

            yield return LoadMain();

            Assert.That(game.LastLoadSource, Is.EqualTo(SaveSource.Unreadable));
            Assert.That(game.Economy.Honey.IsZero);
            Assert.That(game.Bees.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator HourAway_PaysOfflineHoneyAndShowsWelcomeBack()
        {
            game.SaveNow();
            double honey = game.Economy.Honey.ToDouble();
            ShiftSavedClock(-3600, -3600, -3600, rate: 2);

            yield return LoadMain();

            float efficiency = Hidden<OfflineSettings>(game, "offlineSettings").Efficiency;
            Assert.That(game.LastOffline.Status, Is.EqualTo(OfflineStatus.Credited));
            Assert.That(game.LastOffline.ElapsedSeconds, Is.InRange(3600, 3620));
            Assert.That(game.Economy.Honey.ToDouble(), Is.EqualTo(honey + 2 * 3600 * efficiency).Within(2 * 20 * efficiency + 1));
            Assert.That(game.IsWelcomeBackOpen);
            Assert.That(game.Stats.offlineHoney.ToBigNumber().ToDouble(), Is.GreaterThan(0));

            // Collect closes it; a reload right after pays nothing more.
            Object.FindAnyObjectByType<WelcomeBackView>().Close();
            Assert.That(game.IsWelcomeBackOpen, Is.False);
            double collected = game.Economy.Honey.ToDouble();
            yield return LoadMain();
            Assert.That(game.Economy.Honey.ToDouble(), Is.InRange(collected, collected + 5));
        }

        [UnityTest]
        public IEnumerator LongAbsence_IsCapped()
        {
            game.SaveNow();
            double honey = game.Economy.Honey.ToDouble();
            ShiftSavedClock(-48 * 3600, -48 * 3600, -48 * 3600, rate: 1);

            yield return LoadMain();

            OfflineSettings settings = Hidden<OfflineSettings>(game, "offlineSettings");
            Assert.That(game.LastOffline.CapReached);
            Assert.That(game.Economy.Honey.ToDouble() - honey,
                Is.EqualTo(settings.CapHours * 3600 * settings.Efficiency).Within(1e-3));
        }

        [UnityTest]
        public IEnumerator ShortAbsence_PaysWithoutWelcomeBack()
        {
            game.SaveNow();
            ShiftSavedClock(-30, -30, -30, rate: 2);

            yield return LoadMain();

            Assert.That(game.LastOffline.IsPayable);
            Assert.That(game.IsWelcomeBackOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator TimestampInFuture_PaysNothingAndResets()
        {
            game.SaveNow();
            double honey = game.Economy.Honey.ToDouble();
            ShiftSavedClock(3600, 3600, 0, rate: 100);

            yield return LoadMain();

            Assert.That(game.LastOffline.Status, Is.EqualTo(OfflineStatus.RejectedClockBackwards));
            Assert.That(game.Economy.Honey.ToDouble(), Is.EqualTo(honey));
            Assert.That(game.IsWelcomeBackOpen, Is.False);
            Assert.That(TestSave.Files().Load().clock.lastSeenUtc, Is.LessThanOrEqualTo(GameClock.DeviceUtc + 1),
                "The first save after the load stamps the clock again.");
        }

        [UnityTest]
        public IEnumerator DeviceClockMovedForward_WaitsForTrustedTime()
        {
            game.SaveNow();
            double honey = game.Economy.Honey.ToDouble();
            // Wall clock says five hours passed, the monotonic clock says none. That also moves
            // the boot estimate, so it looks like a reboot: the device clock alone never pays.
            ShiftSavedClock(-5 * 3600, -5 * 3600, 0, rate: 100);

            // No time server: its answer can arrive within the first frame and settle the
            // pending return before the assert. GameSettings is an asset; restore it.
            GameSettings settings = Hidden<GameSettings>(game, "gameSettings");
            FieldInfo fetch = typeof(GameSettings).GetField("fetchTrustedTime", BindingFlags.Instance | BindingFlags.NonPublic);
            fetch.SetValue(settings, false);
            try
            {
                yield return LoadMain();

                Assert.That(game.LastOffline.Status, Is.EqualTo(OfflineStatus.PendingTrustedTime));
                Assert.That(game.Economy.Honey.ToDouble(), Is.EqualTo(honey));
                Assert.That(game.IsWelcomeBackOpen, Is.False);
            }
            finally
            {
                fetch.SetValue(settings, true);
            }
        }

        [UnityTest]
        public IEnumerator RewardedBoost_SurvivesReload()
        {
            game.Boosts.StartRewardedHoney(GameClock.DeviceUtc);
            game.SaveNow();

            yield return LoadMain();
            yield return null;

            Assert.That(game.Boosts.IsRewardedHoneyActive(GameClock.DeviceUtc));
            Assert.That(game.Economy.BoostMultiplier, Is.GreaterThan(1));
        }

        [UnityTest]
        public IEnumerator BackButton_ClosesTopPanelThenAsksToQuit()
        {
            var back = Object.FindAnyObjectByType<BackButtonHandler>();
            var queen = Object.FindAnyObjectByType<QueenPanelView>();

            queen.Open();
            back.HandleBack();
            Assert.That(queen.IsOpen, Is.False);
            Assert.That(back.IsQuitDialogOpen, Is.False);

            back.HandleBack();
            Assert.That(back.IsQuitDialogOpen);
            Assert.That(back.IsModalOpen);

            back.HandleBack();
            Assert.That(back.IsQuitDialogOpen, Is.False);
            yield return null;
        }

        [Test]
        public void SafeArea_AnchorsCoverTheSafeRect()
        {
            (Vector2 min, Vector2 max) = SafeArea.ToAnchors(new Rect(0f, 96f, 1080f, 1728f), new Vector2(1080f, 1920f));
            Assert.That(min.x, Is.EqualTo(0f));
            Assert.That(min.y, Is.EqualTo(0.05f).Within(1e-6));
            Assert.That(max.x, Is.EqualTo(1f));
            Assert.That(max.y, Is.EqualTo(0.95f).Within(1e-6));
        }

        [UnityTest]
        public IEnumerator TheoreticalRate_IsCloseToMeasuredRate()
        {
            game.Economy.Grant(BigNumber.FromDouble(1000));
            for (int i = 0; i < 4; i++)
                game.Upgrades.TryAddBee();

            Time.timeScale = 4f;
            float end = Time.time + 60f;
            while (Time.time < end)
                yield return null;

            double theoretical = game.TheoreticalHoneyPerSecond().ToDouble();
            double measured = game.Economy.HoneyPerSecond(Time.timeAsDouble).ToDouble();
            TestContext.WriteLine($"Theoretical {theoretical:F2}/s, measured {measured:F2}/s with {game.Bees.Count} bees.");
            Assert.That(measured, Is.GreaterThan(0));
            Assert.That(theoretical / measured, Is.InRange(0.5, 2.0));
        }
    }
}
