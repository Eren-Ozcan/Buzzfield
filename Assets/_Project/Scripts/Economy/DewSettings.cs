using System;
using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Economy
{
    public enum DewOfferKind
    {
        /// <summary>Honey now: seconds of the current offline rate, in full.</summary>
        TimeSkip,
        /// <summary>A timed honey multiplier that waits in line behind stronger ones.</summary>
        HoneyBoost,
    }

    /// <summary>One thing the shop's Boosts tab sells for Dewdrops.</summary>
    [Serializable]
    public struct DewOffer
    {
        [Tooltip("Analytics name (dew_spend item).")]
        public string id;
        public DewOfferKind kind;
        [Tooltip("Time skip: seconds of honey paid. Honey boost: how long it runs.")]
        [Min(1f)] public float seconds;
        [Tooltip("Honey boost only.")]
        [Min(1f)] public float multiplier;
        [Min(1)] public int cost;
    }

    /// <summary>
    /// Dewdrops, the premium currency: what the game gives away (finished gardens, the daily
    /// streak) and what the shop's Boosts tab sells (docs/DEWDROPS.md 4.1, 6). Goal rewards live in
    /// <see cref="GoalSettings"/>, the butterfly's pouch in VisitorSettings.
    /// </summary>
    [CreateAssetMenu(menuName = "Buzzfield/Dew Settings", fileName = "DewSettings")]
    public sealed class DewSettings : ScriptableObject
    {
        [Tooltip("Dewdrops for every garden bloomed to 100%.")]
        [SerializeField, Min(0)] private int gardenCompleteReward = 10;

        [Header("Boosts tab")]
        [SerializeField] private List<DewOffer> offers = new List<DewOffer>
        {
            new DewOffer { id = "time_skip_1h", kind = DewOfferKind.TimeSkip, seconds = 3600, multiplier = 1, cost = 30 },
            new DewOffer { id = "time_skip_4h", kind = DewOfferKind.TimeSkip, seconds = 14400, multiplier = 1, cost = 100 },
            new DewOffer { id = "time_skip_8h", kind = DewOfferKind.TimeSkip, seconds = 28800, multiplier = 1, cost = 180 },
            new DewOffer { id = "honey_2x_1h", kind = DewOfferKind.HoneyBoost, seconds = 3600, multiplier = 2, cost = 40 },
            new DewOffer { id = "honey_3x_30m", kind = DewOfferKind.HoneyBoost, seconds = 1800, multiplier = 3, cost = 50 },
        };
        [Tooltip("Gardens to complete before the Boosts tab opens.")]
        [SerializeField, Min(0)] private int boostsUnlockGardens = 1;
        [Tooltip("Spends above this many Dewdrops need a second tap.")]
        [SerializeField, Min(0)] private int confirmAbove = 50;
        [Tooltip("Least honey a time skip pays, for a garden that earns almost nothing yet.")]
        [SerializeField, Min(0f)] private double timeSkipMinimumHoney = 500;
        [Tooltip("How long a second tap still confirms a spend.")]
        [SerializeField, Min(0.5f)] private float confirmSeconds = 3f;

        [Header("Daily streak")]
        [Tooltip("Dewdrops for each day of the streak; after the last day it starts again from the first.")]
        [SerializeField] private List<int> dailyRewards = new List<int> { 10, 10, 15, 15, 20, 20, 50 };
        [Tooltip("Local hour the day turns, for the streak and the butterfly's pouches.")]
        [SerializeField, Range(0f, 23f)] private float dayStartHour = 4f;
        [Tooltip("Days after the first one before the streak and the pouch open (1 = the second day).")]
        [SerializeField, Min(0)] private int dailyUnlockDay = 1;
        [Tooltip("After a launch or a return, the panel opens by itself if the date is known within this many seconds.")]
        [SerializeField, Min(0f)] private float dailyAutoOpenSeconds = 20f;

        public int GardenCompleteReward => gardenCompleteReward;
        public IReadOnlyList<DewOffer> Offers => offers;
        public int BoostsUnlockGardens => boostsUnlockGardens;
        public int ConfirmAbove => confirmAbove;
        public double TimeSkipMinimumHoney => timeSkipMinimumHoney;
        public float ConfirmSeconds => confirmSeconds;
        public IReadOnlyList<int> DailyRewards => dailyRewards;
        public float DayStartHour => dayStartHour;
        public int DailyUnlockDay => dailyUnlockDay;
        public float DailyAutoOpenSeconds => dailyAutoOpenSeconds;

        /// <summary>Dewdrops for the 0-based day <paramref name="index"/> of the cycle.</summary>
        public int DailyReward(int index) =>
            dailyRewards.Count == 0 ? 0 : dailyRewards[Mathf.Clamp(index, 0, dailyRewards.Count - 1)];
    }
}
