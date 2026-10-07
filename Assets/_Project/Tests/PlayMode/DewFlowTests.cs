using System.Collections;
using Buzzfield.Audio;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Game;
using Buzzfield.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>Dewdrops against the real Main scene: earning from goals and finished gardens, the counter, spending and saving.</summary>
    public class DewFlowTests
    {
        GameManager game;
        HudView hud;

        [UnitySetUp]
        public IEnumerator LoadFresh()
        {
            TestSave.Clear();
            yield return LoadMain();
        }

        IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            hud = Object.FindAnyObjectByType<HudView>();
            Assert.That(game, Is.Not.Null);
        }

        IEnumerator ShakeUntil(long count)
        {
            while (game.Stats.flowersShaken < count)
            {
                game.Pollen.Refill();
                game.ShakeAllFlowers();
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator NewGame_HasNoDewdrops_AndHidesTheCounter()
        {
            Assert.That(game.Dew.Balance, Is.Zero);
            yield return null;
            Assert.That(hud.IsDewShown, Is.False);
        }

        [UnityTest]
        public IEnumerator FirstGoal_PaysDewdrops_AndShowsTheCounter()
        {
            int dew = game.GoalDewReward;
            Assert.That(dew, Is.GreaterThan(0), "The first goal pays Dewdrops.");
            yield return ShakeUntil(game.Goals.Current.target);
            int sounds = game.Sound.PlayedCount(Sfx.DewGain);
            Assert.That(game.TryClaimGoal());

            Assert.That(game.Dew.Balance, Is.EqualTo(dew));
            Assert.That(game.Dew.TotalEarned, Is.EqualTo(dew));
            Assert.That(game.Sound.PlayedCount(Sfx.DewGain), Is.EqualTo(sounds + 1));
            Assert.That(Object.FindAnyObjectByType<UiEffectsView>().FlyingDew, Is.GreaterThan(0));
            yield return null;
            Assert.That(hud.IsDewShown);
        }

        [UnityTest]
        public IEnumerator CompletedGarden_PaysDewdrops()
        {
            DewSettings settings = Resources.FindObjectsOfTypeAll<DewSettings>()[0];
            game.DebugCompleteGarden();
            yield return null;
            Assert.That(game.Bloom.IsComplete);
            Assert.That(game.Dew.Balance, Is.EqualTo(settings.GardenCompleteReward));
        }

        [UnityTest]
        public IEnumerator Dewdrops_SurviveAReloadAndAQueenMove()
        {
            game.EarnDew(37, "test", Vector2.zero);
            game.SaveNow();
            yield return LoadMain();
            Assert.That(game.Dew.Balance, Is.EqualTo(37));
            Assert.That(game.Dew.TotalEarned, Is.EqualTo(37));

            game.DebugCompleteGarden();
            game.Economy.Grant(BigNumber.FromDouble(1e9));
            yield return null;
            long before = game.Dew.Balance;
            Assert.That(game.TryMoveQueen());
            Assert.That(game.Dew.Balance, Is.EqualTo(before), "The Queen move keeps Dewdrops.");
        }

        [UnityTest]
        public IEnumerator Spending_NeedsTheBalance()
        {
            game.EarnDew(30, "test", Vector2.zero);
            Assert.That(game.TrySpendDew(31, "test"), Is.False);
            Assert.That(game.Dew.Balance, Is.EqualTo(30));
            Assert.That(game.TrySpendDew(30, "test"));
            Assert.That(game.Dew.Balance, Is.Zero);
            Assert.That(game.Dew.TotalEarned, Is.EqualTo(30), "Spending does not lower what was earned.");
            yield return null;
            Assert.That(hud.IsDewShown, "The counter stays once a Dewdrop was earned.");
        }

        [UnityTest]
        public IEnumerator DewCounter_OpensTheShop()
        {
            game.EarnDew(5, "test", Vector2.zero);
            yield return null;
            var shop = Object.FindAnyObjectByType<ShopPanelView>();
            GameObject.Find("DewCounter").GetComponent<Button>().onClick.Invoke();
            Assert.That(shop.IsOpen);
        }

        [UnityTest]
        public IEnumerator GoalPill_ShowsTheDewdropsItPays()
        {
            yield return new WaitForSecondsRealtime(0.35f);
            GameObject badge = GameObject.Find("DewBadge");
            Assert.That(badge, Is.Not.Null);
            Assert.That(badge.activeInHierarchy);
            Assert.That(badge.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo(string.Format(Strings.DewGainFormat, game.GoalDewReward)));
        }
    }
}
