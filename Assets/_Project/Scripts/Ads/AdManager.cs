using System;
using Buzzfield.Core;

namespace Buzzfield.Ads
{
    /// <summary>
    /// The game's single door to ads. Runs consent before the first ad request, keeps a
    /// rewarded ad loaded (retrying after failures), and only shows one when no modal is
    /// open and the shared full-screen gap (<see cref="AdPacing"/>) has passed. Every ad
    /// that opens stamps <see cref="LastFullScreenAdUtc"/>, which the save keeps.
    /// GameManager ticks it once per frame.
    /// </summary>
    public sealed class AdManager
    {
        private readonly IAdService service;
        private readonly IConsentService consent;
        private readonly AdSettings settings;
        private readonly Func<bool> isModalOpen;
        private readonly Func<double> utcNow;
        private readonly ITickableService tickable;
        private float retryLeft = -1f;
        private bool started;

        public AdManager(IAdService service, IConsentService consent, AdSettings settings,
            Func<bool> isModalOpen, Func<double> utcNow)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.consent = consent ?? throw new ArgumentNullException(nameof(consent));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.isModalOpen = isModalOpen ?? (() => false);
            this.utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
            tickable = service as ITickableService;
            service.Opened += HandleOpened;
            service.RewardedLoadFailed += HandleLoadFailed;
        }

        /// <summary>UTC of the last full-screen ad; shared by every full-screen placement.</summary>
        public double LastFullScreenAdUtc { get; private set; }

        public bool IsShowing => service.IsShowing;

        /// <summary>Consent is done and the SDK is up.</summary>
        public bool IsReady => consent.CanRequestAds && service.IsInitialized;

        /// <summary>Loads the saved stamp.</summary>
        public void Restore(double lastFullScreenAdUtc) => LastFullScreenAdUtc = Math.Max(0, lastFullScreenAdUtc);

        /// <summary>Consent first, then SDK init, then the first rewarded load. Safe to call once per session.</summary>
        public void Start()
        {
            if (started)
                return;
            started = true;
            consent.RequestConsent(HandleConsentDone);
        }

        /// <summary>
        /// A rewarded ad could be shown now. <paramref name="fromOpenPanel"/> is for the ad
        /// button on the top panel itself (the Welcome back offer): that panel does not
        /// block its own ad, while any other open panel does.
        /// </summary>
        public bool CanShowRewarded(bool fromOpenPanel = false) =>
            IsReady && service.IsRewardedReady && !service.IsShowing
            && (fromOpenPanel || !isModalOpen())
            && SecondsUntilAllowed() <= 0;

        /// <summary>Seconds left in the shared full-screen gap.</summary>
        public double SecondsUntilAllowed() =>
            AdPacing.SecondsUntilAllowed(utcNow(), LastFullScreenAdUtc, settings.MinSecondsBetweenFullScreenAds);

        /// <summary>
        /// Shows a rewarded ad when <see cref="CanShowRewarded"/> allows it. Returns false
        /// (and never calls back) when it does not; otherwise <paramref name="onComplete"/>
        /// runs once with true when the reward was earned.
        /// </summary>
        public bool TryShowRewarded(Action<bool> onComplete, bool fromOpenPanel = false)
        {
            if (!CanShowRewarded(fromOpenPanel))
                return false;
            service.ShowRewarded(rewarded =>
            {
                // The next ad starts loading as soon as this one is gone.
                service.LoadRewarded();
                onComplete?.Invoke(rewarded);
            });
            return true;
        }

        public void Tick(float unscaledDeltaTime)
        {
            tickable?.Tick(unscaledDeltaTime);
            if (retryLeft < 0f)
                return;
            retryLeft -= unscaledDeltaTime;
            if (retryLeft < 0f)
                service.LoadRewarded();
        }

        public void Dispose()
        {
            service.Opened -= HandleOpened;
            service.RewardedLoadFailed -= HandleLoadFailed;
        }

        private void HandleConsentDone()
        {
            // Without consent (or where ads are not allowed) the game simply offers no ads.
            if (!consent.CanRequestAds)
                return;
            service.Initialize(service.LoadRewarded);
        }

        private void HandleOpened() => LastFullScreenAdUtc = utcNow();

        private void HandleLoadFailed(string _) => retryLeft = settings.LoadRetrySeconds;
    }
}
