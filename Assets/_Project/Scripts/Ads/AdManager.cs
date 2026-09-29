using System;
using Buzzfield.Core;

namespace Buzzfield.Ads
{
    /// <summary>
    /// The game's single door to ads (studio ad policy). Runs consent before the first ad
    /// request and keeps a rewarded ad loaded, retrying after failures. Rewarded ads show
    /// whenever the player asks and no other panel is open. Interstitials only follow a
    /// natural break the game marked (a garden completed, a Queen move), once the screen has
    /// been calm for a moment, and only when <see cref="AdPacing.InterstitialBlock"/> allows:
    /// never for a paying or a new player, never inside the shared cooldown. Every ad that
    /// opens stamps <see cref="LastFullScreenAdUtc"/>, which the save keeps.
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
        private float rewardedRetryLeft = -1f;
        private float interstitialRetryLeft = -1f;
        private float settledFor;
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
            service.RewardedLoadFailed += HandleRewardedLoadFailed;
            service.InterstitialLoadFailed += HandleInterstitialLoadFailed;
        }

        /// <summary>UTC of the last full-screen ad; shared by every full-screen placement.</summary>
        public double LastFullScreenAdUtc { get; private set; }

        public bool IsShowing => service.IsShowing;

        /// <summary>The player bought remove_ads: no interstitials. Rewarded ads ignore it and keep working.</summary>
        public bool ForcedAdsRemoved { get; set; }

        /// <summary>Consent is done and the SDK is up.</summary>
        public bool IsReady => consent.CanRequestAds && service.IsInitialized;

        /// <summary>The player needs a way back to the consent choices (EEA and UK).</summary>
        public bool PrivacyOptionsRequired => consent.PrivacyOptionsRequired;

        /// <summary>The natural break waiting for the screen to settle; null when there is none.</summary>
        public string PendingBreak { get; private set; }

        /// <summary>An interstitial closed; the game saves so the stamp survives a kill.</summary>
        public event Action InterstitialClosed;

        /// <summary>A natural break passed without an interstitial: the trigger and why.</summary>
        public event Action<string, string> BreakSkipped;

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

        /// <summary>Reopens the consent form (the Privacy button).</summary>
        public void ShowPrivacyOptions(Action onDone) => consent.ShowPrivacyOptions(onDone);

        /// <summary>
        /// A rewarded ad could be shown now. <paramref name="fromOpenPanel"/> is for the ad
        /// button on the top panel itself (the Welcome back offer): that panel does not
        /// block its own ad, while any other open panel does. The shared cooldown does not
        /// apply: the player asked for this ad.
        /// </summary>
        public bool CanShowRewarded(bool fromOpenPanel = false) =>
            IsReady && service.IsRewardedReady && !service.IsShowing
            && (fromOpenPanel || !isModalOpen());

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

        /// <summary>Seconds left in the interstitial cooldown that the last full-screen ad started.</summary>
        public double InterstitialCooldownLeft() =>
            AdPacing.SecondsUntilAllowed(utcNow(), LastFullScreenAdUtc, settings.InterstitialCooldownSeconds);

        /// <summary>What would keep an interstitial from playing right now.</summary>
        public ForcedAdBlock InterstitialBlock(double playSeconds, int queenMoves) =>
            AdPacing.InterstitialBlock(utcNow(), LastFullScreenAdUtc, settings.InterstitialCooldownSeconds,
                ForcedAdsRemoved, playSeconds, settings.InterstitialMinPlaySeconds, queenMoves,
                settings.InterstitialMinQueenMoves);

        /// <summary>
        /// The player just finished something (<paramref name="trigger"/>, e.g. "garden_complete").
        /// Its celebration plays first; the interstitial waits until the screen is calm.
        /// </summary>
        public void MarkNaturalBreak(string trigger) => PendingBreak = trigger;

        /// <summary>Drops a pending break, e.g. when the app goes to the background.</summary>
        public void ClearPendingBreak() => PendingBreak = null;

        public void Tick(float unscaledDeltaTime)
        {
            tickable?.Tick(unscaledDeltaTime);
            if (TickRetry(ref rewardedRetryLeft, unscaledDeltaTime))
                service.LoadRewarded();
            TickRetry(ref interstitialRetryLeft, unscaledDeltaTime);
        }

        /// <summary>
        /// Keeps an interstitial loaded for players who can get one, and plays it for a
        /// pending break once the screen has been calm for <see cref="AdSettings.BreakSettleSeconds"/>.
        /// <paramref name="screenCalm"/> is false while any panel, celebration or purchase is up.
        /// </summary>
        public void TickInterstitial(float unscaledDeltaTime, bool screenCalm, double playSeconds, int queenMoves)
        {
            ForcedAdBlock block = InterstitialBlock(playSeconds, queenMoves);
            // New and paying players never even request one.
            if (IsReady && interstitialRetryLeft < 0f && !service.IsInterstitialReady
                && (block == ForcedAdBlock.None || block == ForcedAdBlock.Cooldown))
                service.LoadInterstitial();

            settledFor = screenCalm && !service.IsShowing && !isModalOpen() ? settledFor + unscaledDeltaTime : 0f;
            if (PendingBreak == null || settledFor < settings.BreakSettleSeconds)
                return;

            string trigger = PendingBreak;
            PendingBreak = null;
            string reason = block != ForcedAdBlock.None ? block.ToString()
                : !IsReady || !service.IsInterstitialReady ? "NotReady" : null;
            if (reason != null)
            {
                BreakSkipped?.Invoke(trigger, reason);
                return;
            }
            service.ShowInterstitial(HandleInterstitialClosed);
        }

        public void Dispose()
        {
            service.Opened -= HandleOpened;
            service.RewardedLoadFailed -= HandleRewardedLoadFailed;
            service.InterstitialLoadFailed -= HandleInterstitialLoadFailed;
        }

        /// <summary>Counts a retry wait down; true once when it runs out.</summary>
        private static bool TickRetry(ref float left, float deltaTime)
        {
            if (left < 0f)
                return false;
            left -= deltaTime;
            return left < 0f;
        }

        private void HandleConsentDone()
        {
            // Without consent (or where ads are not allowed) the game simply offers no ads.
            if (!consent.CanRequestAds)
                return;
            service.Initialize(service.LoadRewarded);
        }

        private void HandleOpened() => LastFullScreenAdUtc = utcNow();

        private void HandleInterstitialClosed() => InterstitialClosed?.Invoke();

        private void HandleRewardedLoadFailed(string _) => rewardedRetryLeft = settings.LoadRetrySeconds;

        private void HandleInterstitialLoadFailed(string _) => interstitialRetryLeft = settings.LoadRetrySeconds;
    }
}
