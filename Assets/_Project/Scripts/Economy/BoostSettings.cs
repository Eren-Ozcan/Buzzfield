using UnityEngine;

namespace Buzzfield.Economy
{
    /// <summary>Tap boost and the rewarded honey boost.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Boost Settings", fileName = "BoostSettings")]
    public sealed class BoostSettings : ScriptableObject
    {
        [Header("Tap boost")]
        [SerializeField, Min(1f)] private float tapSpeedMultiplier = 2f;
        [SerializeField, Min(0f)] private float tapDurationSeconds = 5f;
        [Tooltip("Seconds from activation until the next tap boost is allowed.")]
        [SerializeField, Min(0f)] private float tapCooldownSeconds = 15f;

        [Header("Rewarded 2x honey")]
        [SerializeField, Min(1f)] private float rewardedHoneyMultiplier = 2f;
        [SerializeField, Min(0f)] private float rewardedDurationSeconds = 300f;

        public float TapSpeedMultiplier => tapSpeedMultiplier;
        public float TapDurationSeconds => tapDurationSeconds;
        public float TapCooldownSeconds => tapCooldownSeconds;
        public float RewardedHoneyMultiplier => rewardedHoneyMultiplier;
        public float RewardedDurationSeconds => rewardedDurationSeconds;
    }
}
