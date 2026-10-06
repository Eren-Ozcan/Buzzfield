namespace Buzzfield.Core
{
    /// <summary>
    /// What a goal counts. Lifetime totals (shakes, blooms, evolves, gardens, Queen moves) keep
    /// counting through the Queen move; run values (bees, upgrade levels, bloom) are read as they
    /// are now, so a goal like "own 8 bees" can be reached again in a later garden.
    /// </summary>
    public enum GoalKind
    {
        ShakeFlowers = 0,
        OwnBees = 1,
        SpeedLevel = 2,
        HoneyValueLevel = 3,
        BloomFlowers = 4,
        EvolveBees = 5,
        GardenBloomPercent = 6,
        MoveQueen = 7,
        AbilityLevels = 8,
        CompleteGardens = 9,
    }

    /// <summary>The numbers goals read, filled in by the game whenever it checks the current goal.</summary>
    public struct GoalProgress
    {
        public long FlowersShaken;
        public int Bees;
        public int SpeedLevel;
        public int HoneyValueLevel;
        public long FlowersBloomed;
        public long BeesEvolved;
        public int BloomPercent;
        public int QueenMoves;
        public int AbilityLevels;
        public long GardensCompleted;
    }

    public static class GoalMath
    {
        /// <summary>The value <paramref name="kind"/> counts in <paramref name="progress"/>.</summary>
        public static long Current(GoalKind kind, in GoalProgress progress)
        {
            switch (kind)
            {
                case GoalKind.ShakeFlowers: return progress.FlowersShaken;
                case GoalKind.OwnBees: return progress.Bees;
                case GoalKind.SpeedLevel: return progress.SpeedLevel;
                case GoalKind.HoneyValueLevel: return progress.HoneyValueLevel;
                case GoalKind.BloomFlowers: return progress.FlowersBloomed;
                case GoalKind.EvolveBees: return progress.BeesEvolved;
                case GoalKind.GardenBloomPercent: return progress.BloomPercent;
                case GoalKind.MoveQueen: return progress.QueenMoves;
                case GoalKind.AbilityLevels: return progress.AbilityLevels;
                case GoalKind.CompleteGardens: return progress.GardensCompleted;
                default: return 0;
            }
        }

        /// <summary>Progress shown on the goal: the current value, never above the target.</summary>
        public static long Shown(GoalKind kind, long target, in GoalProgress progress)
        {
            long current = Current(kind, progress);
            return current < 0 ? 0 : current > target ? target : current;
        }

        public static bool IsDone(GoalKind kind, long target, in GoalProgress progress) => Current(kind, progress) >= target;

        /// <summary>
        /// Progress of a goal that counts from <paramref name="baseline"/> (the value when it started,
        /// for the endless goals); never below zero, also when a save holds a baseline above the count.
        /// </summary>
        public static long Since(GoalKind kind, long baseline, in GoalProgress progress)
        {
            long done = Current(kind, progress) - baseline;
            return done < 0 ? 0 : done;
        }
    }
}
