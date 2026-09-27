using UnityEngine;

namespace Buzzfield.Economy
{
    /// <summary>Offline earnings rules.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Offline Settings", fileName = "OfflineSettings")]
    public sealed class OfflineSettings : ScriptableObject
    {
        [Tooltip("Share of the theoretical honey/sec earned while away.")]
        [SerializeField, Range(0f, 1f)] private float efficiency = 0.5f;
        [Tooltip("Longest absence that pays, before Queen abilities.")]
        [SerializeField, Min(0f)] private float capHours = 2f;
        [Tooltip("Shorter absences show no Welcome back panel.")]
        [SerializeField, Min(0f)] private float minAwaySeconds = 60f;
        [Tooltip("Multiplier of the rewarded ad offer on the Welcome back panel.")]
        [SerializeField, Min(1f)] private float rewardedMultiplier = 3f;

        public float Efficiency => efficiency;
        public float CapHours => capHours;
        public float MinAwaySeconds => minAwaySeconds;
        public float RewardedMultiplier => rewardedMultiplier;
    }
}
