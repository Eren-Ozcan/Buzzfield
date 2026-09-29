using UnityEngine;

namespace Buzzfield.Ads
{
    /// <summary>
    /// Live ad unit ids and ad pacing. The AdMob app id lives in the plugin's own settings
    /// (Assets/GoogleMobileAds/Resources). Development builds and empty ids use Google's
    /// public test units, so a playtest never touches a live ad.
    /// </summary>
    [CreateAssetMenu(menuName = "Buzzfield/Ad Settings", fileName = "AdSettings")]
    public sealed class AdSettings : ScriptableObject
    {
        public const string TestAndroidRewarded = "ca-app-pub-3940256099942544/5224354917";
        public const string TestIosRewarded = "ca-app-pub-3940256099942544/1712485313";
        public const string TestAndroidInterstitial = "ca-app-pub-3940256099942544/1033173712";
        public const string TestIosInterstitial = "ca-app-pub-3940256099942544/4411468910";

        [Header("Live units (release builds)")]
        [SerializeField] private string androidRewardedUnitId = "";
        [SerializeField] private string iosRewardedUnitId = "";
        [SerializeField] private string androidInterstitialUnitId = "";
        [SerializeField] private string iosInterstitialUnitId = "";

        [Header("Interstitials (studio ad policy)")]
        [Tooltip("Seconds after any full-screen ad (rewarded included) before an interstitial may play.")]
        [SerializeField, Min(0f)] private float interstitialCooldownSeconds = 240f;
        [Tooltip("Total play time before the first interstitial.")]
        [SerializeField, Min(0f)] private float interstitialMinPlaySeconds = 1200f;
        [Tooltip("Queen moves before the first interstitial; the first garden is never interrupted.")]
        [SerializeField, Min(0)] private int interstitialMinQueenMoves = 1;
        [Tooltip("The screen must stay calm (no panel, celebration or ad) this long after a break.")]
        [SerializeField, Min(0f)] private float breakSettleSeconds = 1f;

        [Tooltip("Seconds to wait before loading again after a failed load.")]
        [SerializeField, Min(1f)] private float loadRetrySeconds = 30f;

        [Header("Mock service (editor)")]
        [SerializeField, Min(0f)] private float mockDelaySeconds = 1.5f;
        [SerializeField] private bool mockSimulateFailure;

        /// <summary>The rewarded unit for this platform; the test unit when <paramref name="test"/> or no live id.</summary>
        public string RewardedUnitId(bool test) =>
#if UNITY_IOS
            Pick(iosRewardedUnitId, TestIosRewarded, test);
#else
            Pick(androidRewardedUnitId, TestAndroidRewarded, test);
#endif

        /// <summary>The interstitial unit for this platform; the test unit when <paramref name="test"/> or no live id.</summary>
        public string InterstitialUnitId(bool test) =>
#if UNITY_IOS
            Pick(iosInterstitialUnitId, TestIosInterstitial, test);
#else
            Pick(androidInterstitialUnitId, TestAndroidInterstitial, test);
#endif

        public float InterstitialCooldownSeconds => interstitialCooldownSeconds;
        public float InterstitialMinPlaySeconds => interstitialMinPlaySeconds;
        public int InterstitialMinQueenMoves => interstitialMinQueenMoves;
        public float BreakSettleSeconds => breakSettleSeconds;
        public float LoadRetrySeconds => loadRetrySeconds;
        public float MockDelaySeconds => mockDelaySeconds;
        public bool MockSimulateFailure => mockSimulateFailure;

        private static string Pick(string live, string testUnit, bool test) =>
            test || string.IsNullOrEmpty(live) ? testUnit : live;
    }
}
