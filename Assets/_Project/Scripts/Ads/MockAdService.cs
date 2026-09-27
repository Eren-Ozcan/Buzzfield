using System;

namespace Buzzfield.Ads
{
    /// <summary>
    /// Editor and development stand-in for the ad SDK. Loads and shows each take
    /// <see cref="AdSettings.MockDelaySeconds"/>; with <see cref="AdSettings.MockSimulateFailure"/>
    /// on, the ad closes without a reward. Settings are read live, so the toggle works in play mode.
    /// </summary>
    public sealed class MockAdService : IAdService, ITickableService
    {
        private readonly AdSettings settings;
        private float loadLeft = -1f;
        private float showLeft = -1f;
        private Action<bool> pendingShow;

        public MockAdService(AdSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public bool IsInitialized { get; private set; }
        public bool IsRewardedReady { get; private set; }
        public bool IsShowing => showLeft >= 0f;
        public bool IsLoading => loadLeft >= 0f;

        public event Action Opened;
        public event Action RewardedLoaded;
        public event Action<string> RewardedLoadFailed;

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
            if (IsRewardedReady || IsLoading || IsShowing)
                return;
            loadLeft = settings.MockDelaySeconds;
        }

        public void ShowRewarded(Action<bool> onComplete)
        {
            if (!IsRewardedReady || IsShowing)
            {
                onComplete?.Invoke(false);
                return;
            }
            IsRewardedReady = false;
            pendingShow = onComplete;
            showLeft = settings.MockDelaySeconds;
            Opened?.Invoke();
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (loadLeft >= 0f)
            {
                loadLeft -= unscaledDeltaTime;
                if (loadLeft < 0f)
                {
                    IsRewardedReady = true;
                    RewardedLoaded?.Invoke();
                }
            }

            if (showLeft >= 0f)
            {
                showLeft -= unscaledDeltaTime;
                if (showLeft < 0f)
                {
                    Action<bool> callback = pendingShow;
                    pendingShow = null;
                    callback?.Invoke(!settings.MockSimulateFailure);
                }
            }
        }
    }
}
