using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>
    /// The butterfly that now and then crosses the garden: a tap or a swipe through it gives a
    /// gift of honey, which a rewarded ad can double. Missed, it is gone. It never comes to a
    /// new player, behind a panel or during the Garden Complete celebration.
    /// </summary>
    [CreateAssetMenu(menuName = "Buzzfield/Visitor Settings", fileName = "VisitorSettings")]
    public sealed class VisitorSettings : ScriptableObject
    {
        [SerializeField] private ButterflyView prefab;
        [Tooltip("Sparkles where the butterfly is caught.")]
        [SerializeField] private ParticleSystem catchBurstPrefab;

        [Header("Pacing")]
        [Tooltip("Play time before the first butterfly ever comes: the first minutes stay about the bees and the swipe.")]
        [SerializeField, Min(0f)] private float firstAfterPlaySeconds = 300f;
        [Tooltip("Calm play seconds between two butterflies, picked at random between min and max.")]
        [SerializeField, Min(1f)] private float minIntervalSeconds = 110f;
        [SerializeField, Min(1f)] private float maxIntervalSeconds = 160f;

        [Header("Flight")]
        [Tooltip("Seconds to cross the garden from one side to the other.")]
        [SerializeField, Min(1f)] private float crossSeconds = 13f;
        [Tooltip("World units outside the garden where the flight starts and ends.")]
        [SerializeField, Min(0f)] private float edgeMargin = 2.5f;
        [SerializeField, Min(0f)] private float flightHeight = 1.9f;
        [Tooltip("Side-to-side swing of the path in world units, and its waves per crossing.")]
        [SerializeField, Min(0f)] private float swing = 1.2f;
        [SerializeField, Min(0f)] private float swingWaves = 2.5f;
        [SerializeField, Min(0f)] private float bob = 0.25f;
        [Tooltip("Wing beats per second.")]
        [SerializeField, Min(0.1f)] private float flapRate = 3.2f;
        [SerializeField, Range(0f, 89f)] private float flapDegrees = 55f;
        [SerializeField, Min(0.1f)] private float scale = 1.5f;

        [Header("Catch")]
        [Tooltip("World radius around the butterfly that a tap or swipe has to pass through.")]
        [SerializeField, Min(0.1f)] private float hitRadius = 1.2f;
        [Tooltip("Gift: this many seconds of the bees' honey per second, never below the minimum.")]
        [SerializeField, Min(0f)] private float rewardSeconds = 20f;
        [SerializeField, Min(0f)] private float minimumReward = 25f;
        [Tooltip("The rewarded ad multiplies the gift by this (2 = the ad adds the gift once more).")]
        [SerializeField, Min(1f)] private float adMultiplier = 2f;
        [Tooltip("Seconds the \"double it\" offer stays on screen after a catch.")]
        [SerializeField, Min(1f)] private float offerSeconds = 8f;

        public ButterflyView Prefab => prefab;
        public ParticleSystem CatchBurstPrefab => catchBurstPrefab;
        public float FirstAfterPlaySeconds => firstAfterPlaySeconds;
        public float MinIntervalSeconds => minIntervalSeconds;
        public float MaxIntervalSeconds => maxIntervalSeconds;
        public float CrossSeconds => crossSeconds;
        public float EdgeMargin => edgeMargin;
        public float FlightHeight => flightHeight;
        public float Swing => swing;
        public float SwingWaves => swingWaves;
        public float Bob => bob;
        public float FlapRate => flapRate;
        public float FlapDegrees => flapDegrees;
        public float Scale => scale;
        public float HitRadius => hitRadius;
        public float RewardSeconds => rewardSeconds;
        public float MinimumReward => minimumReward;
        public float AdMultiplier => adMultiplier;
        public float OfferSeconds => offerSeconds;
    }
}
