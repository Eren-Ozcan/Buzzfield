using System;
using System.Collections.Generic;
using Buzzfield.Core;
using UnityEngine;

namespace Buzzfield.Economy
{
    /// <summary>One step of the goal chain.</summary>
    [Serializable]
    public struct GoalStep
    {
        public GoalKind kind;
        [Min(1)] public long target;
        [Tooltip("Reward: this many seconds of the bees' honey per second (offline rate).")]
        [Min(0f)] public float rewardSeconds;
        [Tooltip("Dewdrops the goal pays on top of the honey.")]
        [Min(0)] public int dewReward;
    }

    /// <summary>
    /// The goal chain under the top bar: one goal at a time, each paying honey and Dewdrops when
    /// claimed. The early goals teach the game (swipe, add bees, evolve, bloom), the later ones lead
    /// to the Queen. After the last goal the endless goals take turns; each counts from the moment
    /// it started, so they only use lifetime counts (shakes, blooms, evolves, gardens, Queen moves).
    /// With no endless goals the pill hides once the chain is done.
    /// </summary>
    [CreateAssetMenu(menuName = "Buzzfield/Goal Settings", fileName = "GoalSettings")]
    public sealed class GoalSettings : ScriptableObject
    {
        [SerializeField] private List<GoalStep> goals = new List<GoalStep>();
        [Tooltip("Repeat in turn after the chain; each counts from the moment it started.")]
        [SerializeField] private List<GoalStep> endlessGoals = new List<GoalStep>();
        [Tooltip("Honey a goal pays at least, so the first goals are worth claiming before the bees earn much.")]
        [SerializeField, Min(0f)] private float minimumReward = 20f;

        public IReadOnlyList<GoalStep> Goals => goals;
        public IReadOnlyList<GoalStep> EndlessGoals => endlessGoals;
        public float MinimumReward => minimumReward;
    }
}
