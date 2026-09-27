using System.Collections;
using Buzzfield.Core;
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
    /// Walks the Phase 4 screens (tap boost ring, Welcome back, quit dialog, safe area, a
    /// reload) and checks their state; with BZ_SHOT_DIR set it also saves a shot of each.
    /// </summary>
    public class Phase4ShotTests
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
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
        }

        static IEnumerator Shot(string name)
        {
            // Let text and layout settle before the render.
            yield return null;
            TestShots.SaveIfRequested($"p4_{name}_1080x1920.png", 1080, 1920);
            TestShots.SaveIfRequested($"p4_{name}_1440x1920.png", 1440, 1920);
        }

        static TapBoostView Ring => Object.FindAnyObjectByType<TapBoostView>();

        static string RingLabel => Ring.GetComponentInChildren<TMPro.TMP_Text>().text;

        static Image RingFill => Ring.transform.Find("Ring").GetComponent<Image>();

        [UnityTest]
        public IEnumerator TapBoostRing_ReadyActiveCooldown()
        {
            yield return null;
            Assert.That(RingLabel, Is.EqualTo(Strings.TapBoostReady));
            Assert.That(RingFill.fillAmount, Is.EqualTo(1f));
            yield return Shot("tap_ready");

            Object.FindAnyObjectByType<TapCatcher>().OnPointerDown(null);
            Time.timeScale = 1f;
            double halfway = Time.timeAsDouble + 2.5;
            while (Time.timeAsDouble < halfway)
                yield return null;
            Assert.That(RingLabel, Does.StartWith("x"));
            Assert.That(RingFill.fillAmount, Is.InRange(0.3f, 0.7f));
            yield return Shot("tap_active");

            Time.timeScale = 4f;
            double midCooldown = game.Boosts.Tap.ActiveUntil + 5;
            while (Time.timeAsDouble < midCooldown)
                yield return null;
            Time.timeScale = 1f;
            Assert.That(RingLabel, Is.Empty);
            Assert.That(RingFill.fillAmount, Is.InRange(0.3f, 0.95f));
            yield return Shot("tap_cooldown");
        }

        [UnityTest]
        public IEnumerator WelcomeBack_HourAndCapped()
        {
            game.SaveNow();
            Shift(-3600, 2);
            yield return LoadMain();
            Assert.That(game.IsWelcomeBackOpen);
            yield return Shot("welcome_1h");

            game.SaveNow();
            Shift(-30 * 3600, 2);
            yield return LoadMain();
            Assert.That(game.IsWelcomeBackOpen);
            Assert.That(game.LastOffline.CapReached);
            yield return Shot("welcome_capped");

            Object.FindAnyObjectByType<WelcomeBackView>().Close();
            Assert.That(game.IsWelcomeBackOpen, Is.False);
        }

        static void Shift(double seconds, double rate)
        {
            SaveManager files = TestSave.Files();
            SaveData data = files.Load();
            data.clock.lastSeenUtc += seconds;
            data.clock.lastDeviceUtc += seconds;
            data.clock.lastMonotonicSeconds += seconds;
            data.offlineRate = BigNumberData.From(rate);
            Assert.That(files.Save(data));
        }

        [UnityTest]
        public IEnumerator BackButton_QueenPanelThenQuitDialog()
        {
            var back = Object.FindAnyObjectByType<BackButtonHandler>();
            var queen = Object.FindAnyObjectByType<QueenPanelView>();
            queen.Open();
            yield return Shot("queen_open");

            back.HandleBack();
            Assert.That(queen.IsOpen, Is.False);
            yield return Shot("after_back");

            back.HandleBack();
            Assert.That(back.IsQuitDialogOpen);
            yield return Shot("quit_dialog");
            back.HandleBack();
        }

        [UnityTest]
        public IEnumerator SafeArea_SimulatedNotch()
        {
            // Batchmode has no notch; apply the anchors a 1080x1920 phone with a 120 px notch
            // and a 90 px home indicator would get.
            var safeArea = Object.FindAnyObjectByType<SafeArea>();
            safeArea.enabled = false;
            (Vector2 min, Vector2 max) = SafeArea.ToAnchors(new Rect(0f, 90f, 1080f, 1920f - 90f - 120f), new Vector2(1080f, 1920f));
            var rect = (RectTransform)safeArea.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            Assert.That(max.y, Is.EqualTo(1800f / 1920f).Within(1e-5));
            yield return Shot("safe_area_notch");
        }

        [UnityTest]
        public IEnumerator Reload_RestoresBeesUpgradesAndBloom()
        {
            game.Economy.Grant(BigNumber.FromDouble(100_000));
            for (int i = 0; i < 6; i++)
                game.Upgrades.TryAddBee();
            game.Upgrades.TryBuySpeed();
            game.Upgrades.TryBuyHoneyValue();
            var flowers = game.Flowers.Flowers;
            for (int i = 0; i < flowers.Count; i++)
                game.Flowers.RestoreSlot(i, flowers[i].IsActive, flowers[i].IsActive ? 0.6f : 0f, flowers[i].Nectar);
            game.SaveNow();
            yield return LoadMain();

            Assert.That(game.Bees.Count, Is.EqualTo(7));
            Assert.That(game.Upgrades.SpeedLevel, Is.EqualTo(1));
            Assert.That(game.Upgrades.HoneyValueLevel, Is.EqualTo(1));
            Assert.That(game.Flowers.Flowers[0].Bloom, Is.EqualTo(0.6f).Within(1e-6));
            Time.timeScale = 4f;
            float end = Time.time + 8f;
            while (Time.time < end)
                yield return null;
            Time.timeScale = 1f;
            yield return Shot("after_reload");
        }
    }
}
