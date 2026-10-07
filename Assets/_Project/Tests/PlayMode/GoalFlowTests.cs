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
    /// <summary>The goal chain against the real Main scene: progress on the pill, the claim and its reward, saving.</summary>
    public class GoalFlowTests
    {
        GameManager game;
        GoalView view;

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
            view = Object.FindAnyObjectByType<GoalView>();
            Assert.That(game, Is.Not.Null);
            Assert.That(view, Is.Not.Null);
        }

        /// <summary>Waits past one goal check (they run a few times a second).</summary>
        static IEnumerator NextCheck() => new WaitForSecondsRealtime(0.35f);

        /// <summary>Shakes every flower with pollen until the shake count reaches <paramref name="count"/>.</summary>
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
        public IEnumerator NewGame_ShowsTheFirstGoal_InProgress()
        {
            yield return NextCheck();
            Assert.That(game.Goals.Index, Is.Zero);
            Assert.That(game.Goals.Current.kind, Is.EqualTo(GoalKind.ShakeFlowers));
            Assert.That(view.IsVisible);
            Assert.That(view.IsClaimable, Is.False);
            Assert.That(game.TryClaimGoal(), Is.False, "Not done yet.");
            Assert.That(game.Goals.Index, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DoneGoal_ClaimPaysHoney_AndMovesOn()
        {
            yield return ShakeUntil(game.Goals.Current.target);
            yield return NextCheck();
            Assert.That(view.IsClaimable);

            double reward = game.ClaimableGoalReward.ToDouble();
            Assert.That(reward, Is.GreaterThanOrEqualTo(Minimum()));
            double before = game.Economy.Honey.ToDouble();
            int claimSounds = game.Sound.PlayedCount(Sfx.GoalClaim);
            view.Pill.GetComponent<Button>().onClick.Invoke();

            Assert.That(game.Goals.Index, Is.EqualTo(1));
            // Bees may deposit in the same frame; the claim itself pays exactly the shown reward.
            Assert.That(game.Economy.Honey.ToDouble() - before, Is.EqualTo(reward).Within(reward * 1e-6 + 0.5));
            Assert.That(game.Sound.PlayedCount(Sfx.GoalClaim), Is.EqualTo(claimSounds + 1));
            Assert.That(Object.FindAnyObjectByType<UiEffectsView>().FlyingDrops, Is.GreaterThan(0));
            Assert.That(view.IsClaimable, Is.False);

            yield return LoadMain();
            Assert.That(game.Goals.Index, Is.EqualTo(1), "The claimed goal stays claimed.");
        }

        [UnityTest]
        public IEnumerator SecondGoal_CountsBees()
        {
            yield return ShakeUntil(game.Goals.Current.target);
            Assert.That(game.TryClaimGoal());
            Assert.That(game.Goals.Current.kind, Is.EqualTo(GoalKind.OwnBees));

            while (game.Bees.Count < game.Goals.Current.target)
            {
                game.Economy.Grant(game.Upgrades.AddBeeCost);
                Assert.That(game.Upgrades.TryAddBee());
            }
            yield return NextCheck();
            Assert.That(view.IsClaimable);
            Assert.That(game.TryClaimGoal());
            Assert.That(game.Goals.Index, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator AfterTheChain_EndlessGoalsCountFromTheirStart()
        {
            GoalSettings settings = Resources.FindObjectsOfTypeAll<GoalSettings>()[0];
            Assert.That(settings.EndlessGoals.Count, Is.GreaterThan(0));
            Assert.That(settings.EndlessGoals[0].kind, Is.EqualTo(GoalKind.ShakeFlowers));

            // Claim the last authored goal from a save one short of the end, after some shaking.
            yield return ShakeUntil(40);
            game.SaveNow();
            var files = TestSave.Files();
            SaveData data = files.Load();
            data.goalIndex = settings.Goals.Count - 1;
            data.movesMade = 99;
            Assert.That(files.Save(data));
            yield return LoadMain();
            Assert.That(game.Goals.IsEndless, Is.False);
            Assert.That(game.TryClaimGoal(), "The last authored goal (Queen moves) is done.");

            // The first endless goal starts now: the earlier shakes do not count.
            Assert.That(game.Goals.IsEndless);
            Assert.That(game.Goals.Baseline, Is.EqualTo(game.Stats.flowersShaken));
            Assert.That(game.Goals.Progress(game.ReadGoalProgress()), Is.Zero);
            yield return NextCheck();
            Assert.That(view.IsVisible);
            Assert.That(view.IsClaimable, Is.False);

            long target = game.Goals.Current.target;
            yield return ShakeUntil(game.Goals.Baseline + target);
            Assert.That(game.TryClaimGoal());
            Assert.That(game.Goals.Current.kind, Is.EqualTo(settings.EndlessGoals[1].kind), "The endless goals take turns.");

            // Index and baseline survive a reload.
            int index = game.Goals.Index;
            long baseline = game.Goals.Baseline;
            game.SaveNow();
            yield return LoadMain();
            Assert.That(game.Goals.Index, Is.EqualTo(index));
            Assert.That(game.Goals.Baseline, Is.EqualTo(baseline));
        }

        static double Minimum() => Resources.FindObjectsOfTypeAll<GoalSettings>()[0].MinimumReward;
    }
}
