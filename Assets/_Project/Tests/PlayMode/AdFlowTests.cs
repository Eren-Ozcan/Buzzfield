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

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// Rewarded ads through the mock service against the real Main scene: the honey boost
    /// button, the Welcome back multiplier, modal blocking and the saved ad stamp.
    /// </summary>
    public class AdFlowTests
    {
        const float MockDelay = 0.05f;
        const float Timeout = 5f;

        static readonly FieldInfo MockDelayField =
            typeof(AdSettings).GetField("mockDelaySeconds", BindingFlags.Instance | BindingFlags.NonPublic);

        static readonly FieldInfo FailureField =
            typeof(AdSettings).GetField("mockSimulateFailure", BindingFlags.Instance | BindingFlags.NonPublic);

        GameManager game;
        AdSettings adSettings;
        float savedDelay;

        [UnitySetUp]
        public IEnumerator LoadFresh()
        {
            TestSave.Clear();
            yield return LoadMain();
        }

        [UnityTearDown]
        public IEnumerator RestoreSettings()
        {
            // The settings asset is shared with the editor; leave it as it was.
            if (adSettings != null)
            {
                MockDelayField.SetValue(adSettings, savedDelay);
                FailureField.SetValue(adSettings, false);
            }
            yield return null;
        }

        IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
            if (adSettings == null)
            {
                adSettings = Hidden<AdSettings>(game, "adSettings");
                savedDelay = (float)MockDelayField.GetValue(adSettings);
            }
            MockDelayField.SetValue(adSettings, MockDelay);
        }

        static T Hidden<T>(object target, string field) =>
            (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        /// <summary>Waits (real time) until an ad could be shown, from the open panel when asked.</summary>
        IEnumerator WaitForAd(bool fromOpenPanel = false)
        {
            float end = Time.realtimeSinceStartup + Timeout;
            while (!game.Ads.CanShowRewarded(fromOpenPanel) && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.That(game.Ads.CanShowRewarded(fromOpenPanel), "No rewarded ad became available.");
        }

        IEnumerator WaitForAdClosed()
        {
            float end = Time.realtimeSinceStartup + Timeout;
            while (game.Ads.IsShowing && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.That(game.Ads.IsShowing, Is.False);
        }

        [UnityTest]
        public IEnumerator BoostAd_StartsTheBoostAndBlocksTheNextAd()
        {
            yield return WaitForAd();
            Assert.That(game.TryWatchBoostAd());
            Assert.That(game.Ads.IsShowing);
            yield return WaitForAdClosed();

            double now = GameClock.DeviceUtc;
            Assert.That(game.Boosts.IsRewardedHoneyActive(now));
            Assert.That(game.Boosts.RewardedHoneySecondsLeft(now), Is.EqualTo(game.Boosts.RewardedDurationSeconds).Within(2));
            Assert.That(game.Ads.LastFullScreenAdUtc, Is.EqualTo(now).Within(2));

            // The boost runs, and the shared gap blocks every other ad.
            Assert.That(game.TryWatchBoostAd(), Is.False);
            Assert.That(game.Ads.SecondsUntilAllowed(), Is.GreaterThan(0));

            // Both the boost and the ad stamp survive a restart.
            yield return LoadMain();
            Assert.That(game.Boosts.IsRewardedHoneyActive(GameClock.DeviceUtc));
            Assert.That(game.Ads.LastFullScreenAdUtc, Is.EqualTo(now).Within(2));
            Assert.That(game.Ads.SecondsUntilAllowed(), Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator BoostAd_NotShownWhileAPanelIsOpen()
        {
            yield return WaitForAd();
            var back = Object.FindAnyObjectByType<BackButtonHandler>();
            back.HandleBack();
            Assert.That(back.IsQuitDialogOpen);

            Assert.That(game.TryWatchBoostAd(), Is.False);
            Assert.That(game.Ads.IsShowing, Is.False);

            back.CloseQuitDialog();
            Assert.That(game.TryWatchBoostAd());
            yield return WaitForAdClosed();
        }

        [UnityTest]
        public IEnumerator BoostAd_WithoutRewardGivesNoBoost()
        {
            yield return WaitForAd();
            // Read when the ad closes, so it stays on until then; the teardown turns it off.
            FailureField.SetValue(adSettings, true);
            Assert.That(game.TryWatchBoostAd());
            yield return WaitForAdClosed();

            Assert.That(game.Boosts.IsRewardedHoneyActive(GameClock.DeviceUtc), Is.False);
            Assert.That(game.Ads.LastFullScreenAdUtc, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator WelcomeBackAd_MultipliesOfflineHoneyOnce()
        {
            game.SaveNow();
            SaveManager files = TestSave.Files();
            SaveData data = files.Load();
            data.clock.lastSeenUtc -= 3600;
            data.clock.lastDeviceUtc -= 3600;
            data.clock.lastMonotonicSeconds -= 3600;
            data.offlineRate = BigNumberData.From(2);
            Assert.That(files.Save(data));

            yield return LoadMain();
            var welcome = Object.FindAnyObjectByType<WelcomeBackView>();
            Assert.That(game.IsWelcomeBackOpen);
            Assert.That(welcome.IsAdOffered);
            double offline = game.LastOffline.Amount.ToDouble();
            Assert.That(offline, Is.GreaterThan(0));

            // The Welcome back panel does not block its own ad, but blocks the boost ad.
            yield return WaitForAd(fromOpenPanel: true);
            Assert.That(game.TryWatchBoostAd(), Is.False);
            double before = game.Stats.offlineHoney.ToBigNumber().ToDouble();
            Assert.That(game.TryWatchOfflineAd());
            yield return WaitForAdClosed();

            float multiplier = Hidden<OfflineSettings>(game, "offlineSettings").RewardedMultiplier;
            Assert.That(game.Stats.offlineHoney.ToBigNumber().ToDouble() - before, Is.EqualTo(offline * (multiplier - 1)).Within(offline * 1e-6));
            Assert.That(welcome.IsAdOffered, Is.False);
            Assert.That(game.TryWatchOfflineAd(), Is.False);
            Assert.That(game.IsWelcomeBackOpen);
        }
    }
}
