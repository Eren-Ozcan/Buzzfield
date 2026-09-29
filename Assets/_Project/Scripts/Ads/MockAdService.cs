using System;

namespace Buzzfield.Ads
{
    /// <summary>
    /// Editor stand-in for the ad SDK. Loads and shows each take
    /// <see cref="AdSettings.MockDelaySeconds"/>; with <see cref="AdSettings.MockSimulateFailure"/>
    /// on, a rewarded ad closes without a reward. Settings are read live, so the toggle works in play mode.
    /// </summary>
    public sealed class MockAdService : IAdService, ITickableService
    {
        private readonly AdSettings settings;
        private float rewardedLoadLeft = -1f;
        private float interstitialLoadLeft = -1f;
        private float showLeft = -1f;
        private Action<bool> pendingRewarded;
        private Action pendingInterstitial;

        public MockAdService(AdSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool IsInitialized { get; private set; }
        public bool IsRewardedReady { get; private set; }
        public bool IsInterstitialReady { get; private set; }
        public bool IsShowing => showLeft >= 0f;
        public bool IsLoading => rewardedLoadLeft >= 0f;
        public bool IsInterstitialLoading => interstitialLoadLeft >= 0f;

        public event Action Opened;
        public event Action<string> RewardedLoadFailed;
        public event Action<string> InterstitialLoadFailed;

        public void Initialize(Action onDone)
        {
            IsInitialized = true;
            onDone?.Invoke();
        }

        public void LoadRewarded()
        {
            if (!IsInitialized)
            {
                RewardedLoadFailed?.Invoke("Not initialized.");
                return;
            }
            if (IsRewardedReady || IsLoading)
                return;
            rewardedLoadLeft = settings.MockDelaySeconds;
        }

        public void LoadInterstitial()
        {
            if (!IsInitialized)
            {
                InterstitialLoadFailed?.Invoke("Not initialized.");
                return;
            }
            if (IsInterstitialReady || IsInterstitialLoading)
                return;
            interstitialLoadLeft = settings.MockDelaySeconds;
        }

        public void ShowRewarded(Action<bool> onComplete)
        {
            if (!IsRewardedReady || IsShowing)
            {
                onComplete?.Invoke(false);
                return;
            }
            IsRewardedReady = false;
            pendingRewarded = onComplete;
            showLeft = settings.MockDelaySeconds;
            Opened?.Invoke();
        }

        public void ShowInterstitial(Action onClosed)
        {
            if (!IsInterstitialReady || IsShowing)
            {
                onClosed?.Invoke();
                return;
            }
            IsInterstitialReady = false;
            pendingInterstitial = onClosed;
            showLeft = settings.MockDelaySeconds;
            Opened?.Invoke();
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (rewardedLoadLeft >= 0f)
            {
                rewardedLoadLeft -= unscaledDeltaTime;
                if (rewardedLoadLeft < 0f)
                    IsRewardedReady = true;
            }

            if (interstitialLoadLeft >= 0f)
            {
                interstitialLoadLeft -= unscaledDeltaTime;
                if (interstitialLoadLeft < 0f)
                    IsInterstitialReady = true;
            }

            if (showLeft >= 0f)
            {
                showLeft -= unscaledDeltaTime;
                if (showLeft < 0f)
                    Close();
            }
        }

        private void Close()
        {
            Action<bool> rewarded = pendingRewarded;
            Action interstitial = pendingInterstitial;
            pendingRewarded = null;
            pendingInterstitial = null;
            rewarded?.Invoke(!settings.MockSimulateFailure);
            interstitial?.Invoke();
        }
    }
}
