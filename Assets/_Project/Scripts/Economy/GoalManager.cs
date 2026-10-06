using System;
using Buzzfield.Core;

namespace Buzzfield.Economy
{
    /// <summary>
    /// Walks the goal chain: which goal is current, how far along it is, whether it is done and
    /// what it pays. After the authored chain the endless goals take turns, each counting from
    /// <see cref="Baseline"/>, the count when it started. Plain class; the game fills a
    /// <see cref="GoalProgress"/> from its managers and pays the rewards. Index and baseline
    /// survive the Queen move.
    /// </summary>
    public sealed class GoalManager
    {
        private readonly GoalSettings settings;

        public GoalManager(GoalSettings settings)
        {
            this.settings = settings;
        }

        /// <summary>Goals claimed so far, endless ones included; also the index of the current goal.</summary>
        public int Index { get; private set; }

        /// <summary>Count the current endless goal started from; zero for the authored chain.</summary>
        public long Baseline { get; private set; }

        /// <summary>Goals in the authored chain.</summary>
        public int Count => settings.Goals.Count;

        /// <summary>The authored chain is done and the endless goals are taking turns.</summary>
        public bool IsEndless => Index >= Count;

        /// <summary>False only when the chain is done and there are no endless goals.</summary>
        public bool HasGoal => Index < Count || settings.EndlessGoals.Count > 0;

        public GoalStep Current => IsEndless
            ? settings.EndlessGoals[(Index - Count) % settings.EndlessGoals.Count]
            : settings.Goals[Index];

        /// <summary>Raised when the current goal changes (claimed or restored).</summary>
        public event Action OnChanged;

        public void Restore(int index, long baseline)
        {
            Index = Math.Max(0, index);
            if (Index > Count && settings.EndlessGoals.Count == 0)
                Index = Count;
            Baseline = IsEndless ? Math.Max(0, baseline) : 0;
            OnChanged?.Invoke();
        }

        /// <summary>How far along the current goal is: counted since it started for an endless goal.</summary>
        public long Progress(in GoalProgress progress)
        {
            if (!HasGoal)
                return 0;
            GoalStep step = Current;
            return IsEndless ? GoalMath.Since(step.kind, Baseline, progress) : GoalMath.Current(step.kind, progress);
        }

        public bool IsDone(in GoalProgress progress) => HasGoal && Progress(progress) >= Current.target;

        /// <summary>Honey the current goal pays at <paramref name="honeyPerSecond"/>; zero when there is no goal.</summary>
        public BigNumber Reward(BigNumber honeyPerSecond) =>
            HasGoal ? RewardMath.IncomeSeconds(honeyPerSecond, Current.rewardSeconds, settings.MinimumReward) : BigNumber.Zero;

        /// <summary>Dewdrops the current goal pays; zero when there is no goal.</summary>
        public int DewReward => HasGoal ? Current.dewReward : 0;

        /// <summary>
        /// Moves on to the next goal when the current one is done; an endless goal that comes next
        /// starts counting now. The caller pays the rewards it read before.
        /// </summary>
        public bool TryClaim(in GoalProgress progress)
        {
            if (!IsDone(progress))
                return false;
            Index++;
            Baseline = IsEndless && HasGoal ? GoalMath.Current(Current.kind, progress) : 0;
            OnChanged?.Invoke();
            return true;
        }
    }
}
