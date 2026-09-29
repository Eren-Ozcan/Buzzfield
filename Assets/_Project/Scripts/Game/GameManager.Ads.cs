using Buzzfield.Ads;
using Buzzfield.Core;
using Buzzfield.Platform;

namespace Buzzfield.Game
{
    /// <summary>
    /// Ad placements (studio ad policy). Rewarded: the "x2 honey for 5 minutes" button and
    /// the Welcome back multiplier. Interstitial: after a garden is completed and after a
    /// Queen move, once the celebration is over and no panel is open; never for new or paying
    /// players. Every ad that closes saves the game, so the boost end time and the shared
    /// full-screen ad stamp survive a kill right after the ad. On a phone the services are
    /// AdMob behind Google's consent form, with Firebase told about the consent; the editor
    /// uses the mocks.
    /// </summary>
    public sealed partial class GameManager
    {
        public const string BreakGardenComplete = "garden_complete";
        public const string BreakQueenMove = "queen_move";

        /// <summary>Offline honey the open Welcome back offer multiplies; zero when there is no offer.</summary>
        private BigNumber offlineAdBase;

        private IConsentService consent;

        private void InitAds()
        {
            FirebaseServices.Start();
            consent = PlatformServices.CreateConsentService();
            consent.ConsentChanged += FirebaseServices.SetConsent;
            ads = new AdManager(PlatformServices.CreateAdService(adSettings), consent, adSettings, IsModalOpen, UtcNow);
            ads.InterstitialClosed += HandleInterstitialClosed;
            ads.BreakSkipped += HandleBreakSkipped;
            rewardedBoostView.Init(boosts, UtcNow, CanWatchBoostAd, TryWatchBoostAd);
            welcomeBack.Init(offlineSettings.RewardedMultiplier, CanWatchOfflineAd, TryWatchOfflineAd);
        }

        private void DisposeAds()
        {
            if (consent != null)
                consent.ConsentChanged -= FirebaseServices.SetConsent;
            if (ads == null)
                return;
            ads.InterstitialClosed -= HandleInterstitialClosed;
            ads.BreakSkipped -= HandleBreakSkipped;
            ads.Dispose();
        }

        /// <summary>Shows a rewarded ad for the honey boost. False when no ad can be shown now or the boost already runs.</summary>
        public bool TryWatchBoostAd() => CanWatchBoostAd() && ads.TryShowRewarded(HandleBoostAdDone);

        /// <summary>Shows a rewarded ad for the Welcome back multiplier. False when there is no open offer or no ad.</summary>
        public bool TryWatchOfflineAd() => CanWatchOfflineAd() && ads.TryShowRewarded(HandleOfflineAdDone, fromOpenPanel: true);

        private bool CanWatchBoostAd() => !boosts.IsRewardedHoneyActive(UtcNow()) && ads.CanShowRewarded();

        private bool CanWatchOfflineAd() => welcomeBack.IsAdOffered && !offlineAdBase.IsZero && ads.CanShowRewarded(fromOpenPanel: true);

        private void OfferOfflineAd(BigNumber amount, double awaySeconds, bool capReached, double capSeconds)
        {
            offlineAdBase = amount;
            welcomeBack.Show(awaySeconds, amount, capReached, capSeconds);
        }

        /// <summary>
        /// The pending natural break plays once nothing is on screen: no panel, no garden
        /// celebration, no ad, no purchase in flight (studio ad policy rules 2 to 4).
        /// </summary>
        private void TickInterstitial(float unscaledDeltaTime)
        {
            bool calm = !IsModalOpen() && !gardenComplete.IsShowing && !ads.IsShowing && !store.IsPurchasing;
            ads.TickInterstitial(unscaledDeltaTime, calm, stats.playSeconds, prestige.MovesMade);
        }

        private void HandleBoostAdDone(bool rewarded)
        {
            if (rewarded)
            {
                boosts.StartRewardedHoney(UtcNow());
                FirebaseServices.Log("ad_rewarded", ("placement", "honey_boost"));
            }
            SaveNow();
        }

        private void HandleOfflineAdDone(bool rewarded)
        {
            BigNumber amount = offlineAdBase;
            if (rewarded && !amount.IsZero)
            {
                // The base amount is already credited; the ad adds the rest of the multiple.
                offlineAdBase = BigNumber.Zero;
                double multiplier = offlineSettings.RewardedMultiplier;
                GrantOffline(amount * (multiplier - 1));
                welcomeBack.AdFinished(true, amount * multiplier);
                FirebaseServices.Log("ad_rewarded", ("placement", "welcome_back"));
            }
            else
            {
                welcomeBack.AdFinished(false, amount);
            }
            SaveNow();
        }

        private void HandleInterstitialClosed() => SaveNow();

        private void HandleBreakSkipped(string trigger, string reason) =>
            FirebaseServices.Log("ad_interstitial_skip", ("trigger", trigger), ("reason", reason));

        private bool IsModalOpen() => backButton.IsModalOpen;

        private static double UtcNow() => GameClock.DeviceUtc;
    }
}
