using System;

namespace Buzzfield.Ads
{
    /// <summary>
    /// What the game needs from an ad SDK: rewarded and interstitial ads. Shaped after Google
    /// Mobile Ads so the AdMob implementation maps call for call: <see cref="Initialize"/> is
    /// <c>MobileAds.Initialize</c>, the loads are <c>RewardedAd.Load</c> and
    /// <c>InterstitialAd.Load</c>, the shows are their <c>Show</c> plus the reward and closed
    /// callbacks. Callbacks must arrive on the main thread (for AdMob, set
    /// <c>MobileAds.RaiseAdEventsOnUnityMainThread</c>).
    /// </summary>
    public interface IAdService
    {
        bool IsInitialized { get; }

        /// <summary>A rewarded ad is loaded and can be shown now.</summary>
        bool IsRewardedReady { get; }

        /// <summary>An interstitial is loaded and can be shown now.</summary>
        bool IsInterstitialReady { get; }

        /// <summary>True from a show call until the ad closes.</summary>
        bool IsShowing { get; }

        /// <summary>Raised when any ad takes the screen; the caller stamps the shared ad time here.</summary>
        event Action Opened;

        /// <summary>Raised with the SDK message when a rewarded load fails; the caller decides when to retry.</summary>
        event Action<string> RewardedLoadFailed;

        /// <summary>Raised with the SDK message when an interstitial load fails; the caller decides when to retry.</summary>
        event Action<string> InterstitialLoadFailed;

        void Initialize(Action onDone);

        /// <summary>Starts loading a rewarded ad; does nothing while one is loaded or loading.</summary>
        void LoadRewarded();

        /// <summary>Starts loading an interstitial; does nothing while one is loaded or loading.</summary>
        void LoadInterstitial();

        /// <summary>
        /// Shows the loaded rewarded ad. The callback runs once, after the ad closes, with
        /// true when the reward was earned; right away with false when no ad is loaded.
        /// </summary>
        void ShowRewarded(Action<bool> onComplete);

        /// <summary>
        /// Shows the loaded interstitial. The callback runs once, after the ad closes or fails
        /// to open; right away when no ad is loaded.
        /// </summary>
        void ShowInterstitial(Action onClosed);
    }
}
