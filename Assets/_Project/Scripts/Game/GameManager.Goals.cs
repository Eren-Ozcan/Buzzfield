using Buzzfield.Audio;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Platform;
using Buzzfield.UI;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// The goal chain under the top bar. The current goal is checked a few times a second; once
    /// done, its honey reward (seconds of the bees' honey per second) is fixed until it is claimed,
    /// so the number on the pill does not drift. A claim pays the honey and the goal's Dewdrops,
    /// which fly to their counters. After the authored chain the endless goals take turns.
    /// </summary>
    public sealed partial class GameManager
    {
        private const float GoalCheckSeconds = 0.25f;

        [Header("Goals")]
        [SerializeField] private GoalSettings goalSettings;
        [SerializeField] private GoalView goalView;

        private GoalManager goals;
        private float goalTimer;
        private BigNumber goalReward;
        private bool goalRewardSet;

        public GoalManager Goals => goals;

        /// <summary>Needs the economy, upgrades, bees, bloom, prestige and Queen.</summary>
        private void InitGoals()
        {
            goals = new GoalManager(goalSettings);
            goalView.Init(TryClaimGoal);
        }

        /// <summary>What the goals count right now.</summary>
        public GoalProgress ReadGoalProgress() => new GoalProgress
        {
            FlowersShaken = stats.flowersShaken,
            Bees = beeManager.Count,
            SpeedLevel = upgrades.SpeedLevel,
            HoneyValueLevel = upgrades.HoneyValueLevel,
            FlowersBloomed = stats.flowersBloomed,
            BeesEvolved = stats.beesEvolved,
            BloomPercent = bloom.Percent,
            QueenMoves = prestige.MovesMade,
            AbilityLevels = TotalAbilityLevels(),
            GardensCompleted = stats.gardensCompleted,
        };

        /// <summary>Honey the current goal pays once claimed; zero while it is not done.</summary>
        public BigNumber ClaimableGoalReward => goals.IsDone(ReadGoalProgress()) ? CurrentGoalReward() : BigNumber.Zero;

        /// <summary>Dewdrops the current goal pays when claimed.</summary>
        public int GoalDewReward => goals.DewReward;

        /// <summary>Claims the current goal when it is done: pays its honey and moves on. False otherwise.</summary>
        public bool TryClaimGoal()
        {
            GoalProgress progress = ReadGoalProgress();
            if (!goals.IsDone(progress))
                return false;
            BigNumber reward = CurrentGoalReward();
            int dewReward = goals.DewReward;
            int claimed = goals.Index;
            if (!goals.TryClaim(progress))
                return false;
            goalRewardSet = false;
            economy.Grant(reward);
            sound.Play(Sfx.GoalClaim);
            Pulse(true);
            uiEffects.FlyHoney(goalView.Pill, 6);
            EarnDew(dewReward, "goal", RectTransformUtility.WorldToScreenPoint(null, goalView.Pill.position));
            FirebaseServices.Log("goal_claim", ("goal", claimed + 1), ("play_minutes", (long)(stats.playSeconds / 60)));
            RefreshGoal();
            SaveNow();
            return true;
        }

        private void TickGoals(float unscaledDeltaTime)
        {
            goalTimer -= unscaledDeltaTime;
            if (goalTimer > 0f)
                return;
            goalTimer = GoalCheckSeconds;
            RefreshGoal();
        }

        private void RefreshGoal()
        {
            if (!goals.HasGoal)
            {
                goalView.ShowNone();
                return;
            }
            GoalProgress progress = ReadGoalProgress();
            GoalStep step = goals.Current;
            long current = goals.Progress(progress);
            bool done = current >= step.target;
            if (!done)
                goalRewardSet = false;
            goalView.Show(step.kind, current < step.target ? current : step.target, step.target, done,
                done ? CurrentGoalReward() : BigNumber.Zero, step.dewReward);
        }

        /// <summary>The done goal's reward, fixed the first time it is read.</summary>
        private BigNumber CurrentGoalReward()
        {
            if (!goalRewardSet)
            {
                goalReward = goals.Reward(TheoreticalHoneyPerSecond());
                goalRewardSet = true;
            }
            return goalReward;
        }

        private int TotalAbilityLevels()
        {
            int total = 0;
            for (int i = 0; i < queen.AbilityCount; i++)
                total += queen.AbilityLevel(i);
            return total;
        }
    }
}
