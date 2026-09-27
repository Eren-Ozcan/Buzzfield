using UnityEngine;

namespace Buzzfield.Ads
{
    /// <summary>Ad unit ids and pacing. Defaults are the public AdMob test ids.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Ad Settings", fileName = "AdSettings")]
    public sealed class AdSettings : ScriptableObject
    {
        [SerializeField] private string androidRewardedUnitId = "ca-app-pub-3940256099942544/5224354917";
        [SerializeField] private string iosRewardedUnitId = "ca-app-pub-3940256099942544/1712485313";
        [Tooltip("Minimum seconds between any two full-screen ads.")]
        [SerializeField, Min(0f)] private float minSecondsBetweenFullScreenAds = 30f;

        [Header("Mock service (editor and dev builds)")]
        [SerializeField, Min(0f)] private float mockDelaySeconds = 1.5f;
        [SerializeField] private bool mockSimulateFailure;

        public string AndroidRewardedUnitId => androidRewardedUnitId;
        public string IosRewardedUnitId => iosRewardedUnitId;
        public float MinSecondsBetweenFullScreenAds => minSecondsBetweenFullScreenAds;
        public float MockDelaySeconds => mockDelaySeconds;
        public bool MockSimulateFailure => mockSimulateFailure;
    }
}
