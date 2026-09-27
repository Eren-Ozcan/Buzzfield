using UnityEngine;

namespace Buzzfield.Economy
{
    /// <summary>Starting currency and how the honey per second readout is measured.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Economy Settings", fileName = "EconomySettings")]
    public sealed class EconomySettings : ScriptableObject
    {
        [SerializeField] private double startingHoney;
        [Tooltip("Seconds of real deposits averaged for the honey/sec readout.")]
        [SerializeField, Min(1f)] private float honeyRateWindowSeconds = 30f;
        [Tooltip("Time buckets in the window; more buckets = smoother readout.")]
        [SerializeField, Min(1)] private int honeyRateBuckets = 30;
        [Tooltip("Seconds between honey/sec text refreshes.")]
        [SerializeField, Min(0.1f)] private float rateRefreshSeconds = 0.5f;

        public double StartingHoney => startingHoney;
        public float HoneyRateWindowSeconds => honeyRateWindowSeconds;
        public int HoneyRateBuckets => honeyRateBuckets;
        public float RateRefreshSeconds => rateRefreshSeconds;
    }
}
