using System;
using Buzzfield.Ads;
using GoogleMobileAds.Api;
using UnityEngine;

namespace Buzzfield.Platform
{
    /// <summary>
    /// Google Mobile Ads behind <see cref="IAdService"/>. Development builds always use
    /// Google's test units (<see cref="AdSettings"/>), so a phone playtest never touches a
    /// live ad. The app id is in Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.
    /// </summary>
    public sealed class AdMobAdService : IAdService
    {
        private readonly string rewardedUnit;
        private readonly string interstitialUnit;
        private RewardedAd rewarded;
        private InterstitialAd interstitial;
        private bool rewardedLoading;
        private bool interstitialLoading;
        private bool rewardEarned;
        private Action<bool> rewardedDone;
        private Action interstitialDone;

        public AdMobAdService(AdSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            bool test = Debug.isDebugBuild;
            rewardedUnit = settings.RewardedUnitId(test);
            interstitialUnit = settings.InterstitialUnitId(test);
        }

        public bool IsInitialized { get; private set; }
        public bool IsRewardedReady => rewarded != null && rewarded.CanShowAd();
        public bool IsInterstitialReady => interstitial != null && interstitial.CanShowAd();
        public bool IsShowing => rewardedDone != null || interstitialDone != null;

        public event Action Opened;
        public event Action<string> RewardedLoadFailed;
        public event Action<string> InterstitialLoadFailed;

        private static bool IsOnline => Application.internetReachability != NetworkReachability.NotReachable;

        public void Initialize(Action onDone)
        {
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            // A cartoon idle game for a 13+ audience: no mature ads.
            MobileAds.SetRequestConfiguration(new RequestConfiguration
            {
                MaxAdContentRating = MaxAdContentRating.PG,
                TagForUnderAgeOfConsent = TagForUnderAgeOfConsent.False,
            });
            MobileAds.Initialize(_ =>
            {
                IsInitialized = true;
                onDone?.Invoke();
            });
        }

        public void LoadRewarded()
        {
            if (rewarded != null || rewardedLoading || rewardedDone != null)
                return;
            if (!IsInitialized || !IsOnline)
            {
                RewardedLoadFailed?.Invoke(IsInitialized ? "Offline." : "Not initialized.");
                return;
            }
            rewardedLoading = true;
            RewardedAd.Load(rewardedUnit, new AdRequest(), (ad, error) =>
            {
                rewardedLoading = false;
                if (error != null || ad == null)
                {
                    RewardedLoadFailed?.Invoke(error?.GetMessage() ?? "No fill.");
                    return;
                }
                ad.OnAdFullScreenContentOpened += () => Opened?.Invoke();
                ad.OnAdFullScreenContentClosed += () => FinishRewarded(ad);
                ad.OnAdFullScreenContentFailed += showError =>
                {
                    Debug.LogWarning("Rewarded ad failed to show: " + showError.GetMessage());
                    FinishRewarded(ad);
                };
                rewarded = ad;
            });
        }

        public void LoadInterstitial()
        {
            if (interstitial != null || interstitialLoading || interstitialDone != null)
                return;
            if (!IsInitialized || !IsOnline)
            {
                InterstitialLoadFailed?.Invoke(IsInitialized ? "Offline." : "Not initialized.");
                return;
            }
            interstitialLoading = true;
            InterstitialAd.Load(interstitialUnit, new AdRequest(), (ad, error) =>
            {
                interstitialLoading = false;
                if (error != null || ad == null)
                {
                    InterstitialLoadFailed?.Invoke(error?.GetMessage() ?? "No fill.");
                    return;
                }
                ad.OnAdFullScreenContentOpened += () => Opened?.Invoke();
                ad.OnAdFullScreenContentClosed += () => FinishInterstitial(ad);
                ad.OnAdFullScreenContentFailed += showError =>
                {
                    Debug.LogWarning("Interstitial failed to show: " + showError.GetMessage());
                    FinishInterstitial(ad);
                };
                interstitial = ad;
            });
        }

        public void ShowRewarded(Action<bool> onComplete)
        {
            if (!IsRewardedReady || IsShowing)
            {
                onComplete?.Invoke(false);
                return;
            }
            RewardedAd ad = rewarded;
            rewarded = null;
            rewardEarned = false;
            rewardedDone = onComplete;
            ad.Show(_ => rewardEarned = true);
        }

        public void ShowInterstitial(Action onClosed)
        {
            if (!IsInterstitialReady || IsShowing)
            {
                onClosed?.Invoke();
                return;
            }
            InterstitialAd ad = interstitial;
            interstitial = null;
            interstitialDone = onClosed;
            ad.Show();
        }

        private void FinishRewarded(RewardedAd ad)
        {
            ad.Destroy();
            Action<bool> callback = rewardedDone;
            rewardedDone = null;
            callback?.Invoke(rewardEarned);
        }

        private void FinishInterstitial(InterstitialAd ad)
        {
            ad.Destroy();
            Action callback = interstitialDone;
            interstitialDone = null;
            callback?.Invoke();
        }
    }
}
