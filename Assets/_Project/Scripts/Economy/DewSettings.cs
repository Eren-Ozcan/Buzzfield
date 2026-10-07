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
    /// Dewdrops, the premium currency: what the game gives away and what the shop's Boosts tab
    /// sells (docs/DEWDROPS.md 4.1). Goal rewards live in <see cref="GoalSettings"/>.
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

        public int GardenCompleteReward => gardenCompleteReward;
        public IReadOnlyList<DewOffer> Offers => offers;
        public int BoostsUnlockGardens => boostsUnlockGardens;
        public int ConfirmAbove => confirmAbove;
        public double TimeSkipMinimumHoney => timeSkipMinimumHoney;
        public float ConfirmSeconds => confirmSeconds;
    }
}
