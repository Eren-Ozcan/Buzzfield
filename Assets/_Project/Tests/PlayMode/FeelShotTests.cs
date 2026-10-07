using System.Collections;
using Buzzfield.Game;
using Buzzfield.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// Walks the feel screens (the hint's hand, rising numbers, the goal pill, the butterfly and
    /// its offer, the settings panel, the first-return gift) and checks they show; with
    /// BZ_SHOT_DIR set it also saves a shot of each.
    /// </summary>
    public class FeelShotTests
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
            yield return null;
            TestShots.SaveIfRequested($"feel_{name}_1080x1920.png", 1080, 1920);
            TestShots.SaveIfRequested($"feel_{name}_1440x1920.png", 1440, 1920);
        }

        [UnityTest]
        public IEnumerator NewPlayer_HandHint_AndGoal()
        {
            yield return new WaitForSecondsRealtime(0.55f);
            Assert.That(Object.FindAnyObjectByType<SwipeHintView>().IsHandSweeping);
            Assert.That(Object.FindAnyObjectByType<GoalView>().IsVisible);
            yield return Shot("hand_hint");
        }

        [UnityTest]
        public IEnumerator Shake_Numbers_AndAGoalToClaim()
        {
            while (game.Stats.flowersShaken < game.Goals.Current.target)
            {
                game.Pollen.Refill();
                game.ShakeAllFlowers();
                yield return null;
            }
            game.Pollen.Refill();
            game.ShakeAllFlowers();
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Object.FindAnyObjectByType<GoalView>().IsClaimable);
            yield return Shot("shake_goal_done");
        }

        [UnityTest]
        public IEnumerator GoalClaimed_DewdropsCounter()
        {
            while (game.Stats.flowersShaken < game.Goals.Current.target)
            {
                game.Pollen.Refill();
                game.ShakeAllFlowers();
                yield return null;
            }
            Assert.That(game.TryClaimGoal());
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(Object.FindAnyObjectByType<HudView>().IsDewShown);
            yield return Shot("dew_flying");
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("dew_counter");
        }

        [UnityTest]
        public IEnumerator Butterfly_InTheGarden_ThenItsOffer()
        {
            Assert.That(game.DebugLaunchButterfly());
            yield return new WaitForSeconds(5f);
            Assert.That(game.Visitors.IsFlying);
            yield return Shot("butterfly");
        }

        [UnityTest]
        public IEnumerator SettingsPanel_Open()
        {
            Object.FindAnyObjectByType<SettingsPanelView>().Open();
            yield return Shot("settings");
        }

        [UnityTest]
        public IEnumerator FirstReturn_Gift()
        {
            game.SaveNow();
            var files = TestSave.Files();
            Buzzfield.Core.SaveData data = files.Load();
            data.clock.lastSeenUtc -= 3600;
            data.clock.lastDeviceUtc -= 3600;
            data.clock.lastMonotonicSeconds -= 3600;
            Assert.That(files.Save(data));
            yield return LoadMain();
            Assert.That(Object.FindAnyObjectByType<WelcomeBackView>().IsGiftShown);
            yield return Shot("welcome_gift");
        }
    }
}
