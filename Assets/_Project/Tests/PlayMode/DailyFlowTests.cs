using System.Collections;
using System.Reflection;
using Buzzfield.Ads;
using Buzzfield.Audio;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Flowers;
using Buzzfield.Game;
using Buzzfield.Save;
using Buzzfield.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// The daily login streak and the butterfly's Dewdrops pouch against the real Main scene
    /// (docs/DEWDROPS.md sections 6 and 8). The device clock stands in for the time server, and
    /// the play test tools move the date.
    /// </summary>
    public class DailyFlowTests
    {
        const float MockDelay = 0.05f;
        const float Timeout = 5f;

        static readonly FieldInfo MockDelayField =
            typeof(AdSettings).GetField("mockDelaySeconds", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo FetchField =
            typeof(GameSettings).GetField("fetchTrustedTime", BindingFlags.Instance | BindingFlags.NonPublic);

        GameManager game;
        DailyPanelView panel;
        AdSettings adSettings;
        GameSettings gameSettings;
        float savedDelay;

        [UnitySetUp]
        public IEnumerator LoadFresh()
        {
            TestSave.Clear();
            yield return LoadMain(fetchTime: true);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            if (adSettings != null)
                MockDelayField.SetValue(adSettings, savedDelay);
            if (gameSettings != null)
                FetchField.SetValue(gameSettings, true);
            yield return null;
        }

        /// <param name="fetchTime">False keeps the time server out, so the date stays unknown until the test trusts the device clock.</param>
        IEnumerator LoadMain(bool fetchTime)
        {
            if (gameSettings != null)
                FetchField.SetValue(gameSettings, fetchTime);
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            panel = Object.FindAnyObjectByType<DailyPanelView>();
            Assert.That(game, Is.Not.Null);
            Assert.That(panel, Is.Not.Null);
            gameSettings = Hidden<GameSettings>(game, "gameSettings");
            if (adSettings == null)
            {
                adSettings = Hidden<AdSettings>(game, "adSettings");
                savedDelay = (float)MockDelayField.GetValue(adSettings);
            }
            MockDelayField.SetValue(adSettings, MockDelay);
        }

        static T Hidden<T>(object target, string field) =>
            (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        /// <summary>The device clock gives today's date; the first day is noted.</summary>
        long Today()
        {
            game.DebugTrustDeviceClock();
            Assert.That(game.TryGetToday(out long today));
            return today;
        }

        IEnumerator WaitFor(System.Func<bool> condition, string message)
        {
            float end = Time.realtimeSinceStartup + Timeout;
            while (!condition() && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.That(condition(), message);
        }

        static IEnumerator Shot(string name)
        {
            yield return null;
            TestShots.SaveIfRequested($"daily_{name}_1080x1920.png", 1080, 1920);
            TestShots.SaveIfRequested($"daily_{name}_1440x1920.png", 1440, 1920);
        }

        // ---- Daily streak ----

        [UnityTest]
        public IEnumerator FirstDay_NotesTheDate_ButOffersNothingYet()
        {
            long today = Today();
            Assert.That(game.Daily.firstDay, Is.EqualTo(today));
            Assert.That(game.IsDailyClaimable, Is.False);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(panel.IsBadgeShown, Is.False);
            Assert.That(game.TryClaimDaily(), Is.False);

            // Opened by hand (play test tools), the locked panel shows nothing as claimed.
            game.OpenDaily();
            Assert.That(Hidden<Button>(panel, "claimButton").interactable, Is.False);
            Assert.That(Hidden<GameObject[]>(panel, "dayChecks")[0].activeSelf, Is.False);
            Assert.That(Hidden<TMP_Text>(panel, "statusText").text, Is.EqualTo(Strings.DailyHint));
            panel.Close();
        }

        [UnityTest]
        public IEnumerator SecondDay_PanelOpensByItself_AndTheClaimPays()
        {
            Today();
            game.DebugShiftDays(1);
            Assert.That(game.IsDailyClaimable);
            yield return WaitFor(() => panel.IsOpen, "The panel opens by itself on the second day.");
            Assert.That(panel.IsBadgeShown);
            yield return Shot("panel_day1");

            long before = game.Dew.Balance;
            int sounds = game.Sound.PlayedCount(Sfx.DewGain);
            Assert.That(game.TryClaimDaily());
            Assert.That(game.Dew.Balance - before, Is.EqualTo(game.DewSettings.DailyReward(0)));
            Assert.That(game.Sound.PlayedCount(Sfx.DewGain), Is.EqualTo(sounds + 1));
            Assert.That(panel.IsOpen, Is.False, "The panel closes so the crystals fly over the garden.");
            Assert.That(panel.IsBadgeShown, Is.False);
            Assert.That(game.Daily.streak, Is.EqualTo(1));
            Assert.That(game.TryClaimDaily(), Is.False, "One claim a day.");
            Assert.That(TestSave.Files().Load().daily.streak, Is.EqualTo(1), "The claim is saved at once.");
        }

        /// <summary>The butterfly's offer counts down on screen; the panel waits for it instead of covering it.</summary>
        [UnityTest]
        public IEnumerator NewDay_PanelWaitsForTheButterflyOffer()
        {
            Today();
            var offer = Object.FindAnyObjectByType<GiftOfferView>();
            offer.Show(BigNumber.FromDouble(1000), 2f);
            game.DebugShiftDays(1);
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(offer.IsShowing, "the offer is still counting down");
            Assert.That(panel.IsOpen, Is.False, "the panel does not cover the offer");

            offer.Hide();
            yield return WaitFor(() => panel.IsOpen, "The panel opens once the offer is gone.");
        }

        [UnityTest]
        public IEnumerator ClosedWithoutClaiming_TheBadgeStays_AndOpensItAgain()
        {
            Today();
            game.DebugShiftDays(1);
            yield return WaitFor(() => panel.IsOpen, "The panel opens by itself.");
            Object.FindAnyObjectByType<BackButtonHandler>().HandleBack();
            Assert.That(panel.IsOpen, Is.False, "Back closes the panel.");
            yield return null;
            Assert.That(panel.IsBadgeShown);
            yield return Shot("badge");

            Hidden<Button>(panel, "badgeButton").onClick.Invoke();
            Assert.That(panel.IsOpen);
            Hidden<Button>(panel, "claimButton").onClick.Invoke();
            Assert.That(game.Daily.streak, Is.EqualTo(1));
            Assert.That(panel.IsBadgeShown, Is.False);
        }

        [UnityTest]
        public IEnumerator BackToBackDays_WalkTheRewards_AndAMissedDayStartsOver()
        {
            Today();
            DewSettings settings = game.DewSettings;
            for (int day = 0; day < 3; day++)
            {
                game.DebugShiftDays(1);
                long before = game.Dew.Balance;
                Assert.That(game.TryClaimDaily());
                Assert.That(game.Dew.Balance - before, Is.EqualTo(settings.DailyReward(day)), $"Day {day + 1}");
            }
            Assert.That(game.Daily.streak, Is.EqualTo(3));

            game.DebugShiftDays(2);
            game.OpenDaily();
            yield return null;
            Assert.That(Hidden<TMP_Text>(panel, "statusText").text, Is.EqualTo(Strings.DailyMissed));
            yield return Shot("missed");
            long start = game.Dew.Balance;
            Assert.That(game.TryClaimDaily());
            Assert.That(game.Daily.streak, Is.EqualTo(1));
            Assert.That(game.Dew.Balance - start, Is.EqualTo(settings.DailyReward(0)));
        }

        [UnityTest]
        public IEnumerator SeventhDay_PaysTheBigReward_ThenTheCycleStartsAgain()
        {
            Today();
            DewSettings settings = game.DewSettings;
            for (int day = 0; day < 6; day++)
            {
                game.DebugShiftDays(1);
                Assert.That(game.TryClaimDaily());
            }
            game.DebugShiftDays(1);
            game.OpenDaily();
            yield return Shot("panel_day7");
            long before = game.Dew.Balance;
            Assert.That(game.TryClaimDaily());
            Assert.That(game.Dew.Balance - before, Is.EqualTo(settings.DailyReward(6)));
            game.DebugShiftDays(1);
            before = game.Dew.Balance;
            Assert.That(game.TryClaimDaily());
            Assert.That(game.Dew.Balance - before, Is.EqualTo(settings.DailyReward(0)));
            Assert.That(game.Daily.streak, Is.EqualTo(8));
        }

        [UnityTest]
        public IEnumerator NextLaunch_OpensThePanel_AfterTheTimeServerAnswers()
        {
            long today = Today();
            game.SaveNow();
            SaveManager files = TestSave.Files();
            SaveData data = files.Load();
            data.daily.firstDay = today - 1;
            files.Save(data);

            yield return LoadMain(fetchTime: false);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(panel.IsOpen, Is.False, "No date yet: nothing opens.");
            Assert.That(game.TryClaimDaily(), Is.False);

            game.DebugTrustDeviceClock();
            yield return WaitFor(() => panel.IsOpen, "The time server answered: the panel opens.");
        }

        [UnityTest]
        public IEnumerator WithoutTheDate_TheClaimWaits()
        {
            yield return LoadMain(fetchTime: false);
            game.OpenDaily();
            yield return null;
            Assert.That(Hidden<Button>(panel, "claimButton").interactable, Is.False);
            Assert.That(Hidden<TMP_Text>(panel, "statusText").text, Is.EqualTo(Strings.DailyWaitingForTime));
            Assert.That(game.TryClaimDaily(), Is.False);
            panel.Close();
        }

        [UnityTest]
        public IEnumerator Streak_SurvivesAReload()
        {
            long today = Today();
            game.DebugShiftDays(1);
            Assert.That(game.TryClaimDaily());

            yield return LoadMain(fetchTime: false);
            Assert.That(game.Daily.streak, Is.EqualTo(1));
            Assert.That(game.Daily.lastClaimDay, Is.EqualTo(today + 1));
            Assert.That(game.Daily.firstDay, Is.EqualTo(today));
            game.DebugTrustDeviceClock();
            Assert.That(game.IsDailyClaimable, Is.False, "The claim was a day ahead of the real date: nothing more today.");
        }

        // ---- Butterfly pouch ----

        IEnumerator WaitForAd() => WaitFor(() => game.Ads.CanShowRewarded(), "No rewarded ad became available.");

        void TapButterfly()
        {
            Vector2 screen = Camera.main.WorldToScreenPoint(game.Visitors.Position);
            var catcher = Object.FindAnyObjectByType<SwipeCatcher>();
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 0, position = screen };
            catcher.OnPointerDown(pointer);
            catcher.OnPointerUp(pointer);
        }

        [UnityTest]
        public IEnumerator PouchButterfly_OpensOnlyWithAnAd()
        {
            yield return WaitForAd();
            Assert.That(game.DebugLaunchButterfly(true));
            Assert.That(game.Visitors.HasPouch);
            yield return new WaitForSeconds(3f);
            yield return Shot("pouch_flying");

            double honey = game.Economy.Honey.ToDouble();
            long dew = game.Dew.Balance;
            TapButterfly();
            Assert.That(game.Visitors.IsFlying, Is.False);
            Assert.That(game.Sound.PlayedCount(Sfx.ButterflyCatch), Is.EqualTo(1));
            Assert.That(game.Economy.Honey.ToDouble(), Is.LessThanOrEqualTo(honey + 1), "A pouch pays no honey.");
            var offer = Object.FindAnyObjectByType<GiftOfferView>();
            Assert.That(offer.IsShowing);
            Assert.That(offer.IsPouch);
            yield return Shot("pouch_offer");

            Assert.That(game.TryWatchGiftAd());
            yield return WaitFor(() => !offer.IsShowing, "The ad closes the offer.");
            VisitorSettings settings = Hidden<VisitorSettings>(game, "visitorSettings");
            Assert.That(game.Dew.Balance - dew, Is.InRange(settings.PouchMinDew, settings.PouchMaxDew));
            Assert.That(game.TryWatchGiftAd(), Is.False, "One ad per pouch.");
        }

        [UnityTest]
        public IEnumerator LeftPouch_IsGone()
        {
            yield return WaitForAd();
            Assert.That(game.DebugLaunchButterfly(true));
            yield return new WaitForSeconds(3f);
            long dew = game.Dew.Balance;
            TapButterfly();
            var offer = Object.FindAnyObjectByType<GiftOfferView>();
            Assert.That(offer.IsShowing && offer.IsPouch);
            Hidden<Button>(offer, "closeButton").onClick.Invoke();
            Assert.That(offer.IsShowing, Is.False);
            Assert.That(game.TryWatchGiftAd(), Is.False);
            Assert.That(game.Dew.Balance, Is.EqualTo(dew));
        }

        [UnityTest]
        public IEnumerator EveryFourthButterfly_CarriesThePouch_OnceTheStreakIsOpen()
        {
            yield return WaitForAd();
            Today();
            for (int i = 0; i < 4; i++)
            {
                Assert.That(game.DebugLaunchButterfly());
                Assert.That(game.Visitors.HasPouch, Is.False, "Not on the first day.");
                game.Visitors.Cancel();
            }

            game.DebugShiftDays(1);
            // The panel opens by itself on the new day; no butterfly comes while it is open.
            yield return WaitFor(() => panel.IsOpen, "The new day opens the panel.");
            panel.Close();
            int carried = 0;
            for (int i = 0; i < 8; i++)
            {
                Assert.That(game.DebugLaunchButterfly());
                if (game.Visitors.HasPouch)
                    carried++;
                game.Visitors.Cancel();
            }
            Assert.That(carried, Is.EqualTo(2));
            game.SaveNow();
            Assert.That(TestSave.Files().Load().pouch.today, Is.EqualTo(2), "The tally is saved with the game.");
        }
    }
}
