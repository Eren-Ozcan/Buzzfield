using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>Pollen shake: swiping across flowers for honey and bloom, its pacing and its look.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Pollen Settings", fileName = "PollenSettings")]
    public sealed class PollenSettings : ScriptableObject
    {
        [Header("Swipe")]
        [Tooltip("World radius around a flower that a swipe has to pass through to shake it.")]
        [SerializeField, Min(0.05f)] private float hitRadius = 1f;
        [Tooltip("Height of the plane swipes are projected onto, about where the flower heads are.")]
        [SerializeField] private float hitPlaneHeight = 0.9f;

        [Header("Reward")]
        [Tooltip("Seconds before a shaken flower has pollen again.")]
        [SerializeField, Min(0.1f)] private float cooldownSeconds = 10f;
        [Tooltip("Seconds of the bees' honey per second that one sweep over every flower pays. " +
                 "A player sweeping as often as the cooldown allows earns this / cooldown on top of the bees.")]
        [SerializeField, Min(0f)] private float incomeSeconds = 2.5f;
        [Tooltip("Nectar one shake is worth at least, so swiping pays before the bees earn much.")]
        [SerializeField, Min(0f)] private float minimumNectar = 0.5f;
        [Tooltip("Share of a shake's nectar that also grows the flower's bloom. Shakes reach every " +
                 "flower, also the far ones bees rarely visit, so a full share would rush the bloom gate.")]
        [SerializeField, Range(0f, 1f)] private float bloomShare = 0.4f;

        [Header("Feel")]
        [SerializeField, Min(0f)] private float wiggleDegrees = 12f;
        [SerializeField, Min(0f)] private float wiggleCycles = 2.5f;
        [SerializeField, Min(0.05f)] private float wiggleDuration = 0.55f;
        [Tooltip("Looping system with no emission of its own; every shake emits a puff into it.")]
        [SerializeField] private ParticleSystem puffPrefab;
        [SerializeField, Min(1)] private int puffParticles = 12;

        [Header("Ready cue")]
        [Tooltip("Looping system with no emission of its own; flowers with pollen emit drifting motes into it.")]
        [SerializeField] private ParticleSystem motePrefab;
        [Tooltip("Average seconds between two motes of one flower that has pollen.")]
        [SerializeField, Min(0.1f)] private float moteInterval = 1.4f;

        [Header("Hint")]
        [Tooltip("The swipe hint shows until the player has shaken this many flowers.")]
        [SerializeField, Min(0)] private int hintUntilShakes = 3;

        public float HitRadius => hitRadius;
        public float HitPlaneHeight => hitPlaneHeight;
        public float CooldownSeconds => cooldownSeconds;
        public float IncomeSeconds => incomeSeconds;
        public float MinimumNectar => minimumNectar;
        public float BloomShare => bloomShare;
        public float WiggleDegrees => wiggleDegrees;
        public float WiggleCycles => wiggleCycles;
        public float WiggleDuration => wiggleDuration;
        public ParticleSystem PuffPrefab => puffPrefab;
        public int PuffParticles => puffParticles;
        public ParticleSystem MotePrefab => motePrefab;
        public float MoteInterval => moteInterval;
        public int HintUntilShakes => hintUntilShakes;
    }
}
