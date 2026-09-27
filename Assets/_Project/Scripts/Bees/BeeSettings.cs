using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Bees
{
    /// <summary>Global bee behaviour: tier order, cap and flight feel.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Bee Settings", fileName = "BeeSettings")]
    public sealed class BeeSettings : ScriptableObject
    {
        [Tooltip("Tiers from lowest to highest. Evolve merges into the next entry.")]
        [SerializeField] private List<BeeTier> tiers = new List<BeeTier>();
        [SerializeField, Min(1)] private int maxBees = 150;
        [SerializeField, Min(0)] private int startingBees = 1;
        [Tooltip("Instances created per tier at startup so early spawns do not instantiate.")]
        [SerializeField, Min(0)] private int poolPrewarm = 8;
        [SerializeField, Min(0f)] private float depositDuration = 0.25f;
        [Tooltip("Seconds between retries while no flower is available.")]
        [SerializeField, Min(0.05f)] private float retryInterval = 0.5f;
        [Tooltip("Radius around the hive entrance where idle bees hover.")]
        [SerializeField, Min(0f)] private float hoverRadius = 0.8f;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.06f;
        [SerializeField, Min(0f)] private float bobFrequency = 7f;
        [Tooltip("Degrees per second the bee turns to face its flight direction.")]
        [SerializeField, Min(1f)] private float turnSpeed = 720f;

        [Header("Evolve")]
        [Tooltip("Flight speed factor while the three bees gather at the merge point.")]
        [SerializeField, Min(0.1f)] private float mergeGatherSpeed = 1.5f;
        [Tooltip("Seconds of the flash before the new bee appears.")]
        [SerializeField, Min(0.05f)] private float mergeFlashDuration = 0.4f;
        [Tooltip("Pooled effect shown at the merge point; scaled up and down during the flash.")]
        [SerializeField] private GameObject mergeFlashPrefab;
        [SerializeField, Min(0.1f)] private float mergeFlashScale = 1.2f;

        public IReadOnlyList<BeeTier> Tiers => tiers;
        public int MaxBees => maxBees;
        public int StartingBees => startingBees;
        public int PoolPrewarm => poolPrewarm;
        public float DepositDuration => depositDuration;
        public float RetryInterval => retryInterval;
        public float HoverRadius => hoverRadius;
        public float BobAmplitude => bobAmplitude;
        public float BobFrequency => bobFrequency;
        public float TurnSpeed => turnSpeed;
        public float MergeGatherSpeed => mergeGatherSpeed;
        public float MergeFlashDuration => mergeFlashDuration;
        public GameObject MergeFlashPrefab => mergeFlashPrefab;
        public float MergeFlashScale => mergeFlashScale;
    }
}
