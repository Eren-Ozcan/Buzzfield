using UnityEngine;

namespace Buzzfield.Economy
{
    /// <summary>The rewarded honey boost.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Boost Settings", fileName = "BoostSettings")]
    public sealed class BoostSettings : ScriptableObject
    {
        [Header("Rewarded 2x honey")]
        [SerializeField, Min(1f)] private float rewardedHoneyMultiplier = 2f;
        [SerializeField, Min(0f)] private float rewardedDurationSeconds = 300f;

        public float RewardedHoneyMultiplier => rewardedHoneyMultiplier;
        public float RewardedDurationSeconds => rewardedDurationSeconds;
    }
}
