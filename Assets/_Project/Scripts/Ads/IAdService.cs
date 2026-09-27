using System;

namespace Buzzfield.Ads
{
    /// <summary>
    /// What the game needs from a rewarded ad SDK. Shaped after Google Mobile Ads so the
    /// AdMob implementation maps call for call: <see cref="Initialize"/> is
    /// <c>MobileAds.Initialize</c>, <see cref="LoadRewarded"/> is <c>RewardedAd.Load</c>
    /// and <see cref="ShowRewarded"/> is <c>RewardedAd.Show</c> plus its reward and
    /// closed callbacks. Callbacks must arrive on the main thread (for AdMob, set
    /// <c>MobileAds.RaiseAdEventsOnUnityMainThread</c>).
    /// </summary>
    public interface IAdService
    {
        bool IsInitialized { get; }

        /// <summary>A rewarded ad is loaded and can be shown now.</summary>
        bool IsRewardedReady { get; }

        /// <summary>True from <see cref="ShowRewarded"/> until the ad closes.</summary>
        bool IsShowing { get; }

        /// <summary>Raised when the ad takes the screen; the caller stamps the shared ad time here.</summary>
        event Action Opened;

        /// <summary>Raised when a rewarded ad finishes loading.</summary>
        event Action RewardedLoaded;

        /// <summary>Raised with the SDK message when a load fails; the caller decides when to retry.</summary>
        event Action<string> RewardedLoadFailed;

        void Initialize(Action onDone);

        /// <summary>Starts loading a rewarded ad; does nothing while one is loaded or loading.</summary>
        void LoadRewarded();

        /// <summary>
        /// Shows the loaded rewarded ad. The callback runs once, after the ad closes, with
        /// true when the reward was earned; right away with false when no ad is loaded.
        /// </summary>
        void ShowRewarded(Action<bool> onComplete);
    }
}
