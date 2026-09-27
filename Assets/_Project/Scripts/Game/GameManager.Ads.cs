using Buzzfield.Ads;
using Buzzfield.Core;

namespace Buzzfield.Game
{
    /// <summary>
    /// Rewarded ad placements: the "x2 honey for 5 minutes" button and the Welcome back
    /// multiplier. Every ad that closes saves the game, so the boost end time and the
    /// shared full-screen ad stamp survive a kill right after the ad.
    /// </summary>
    public sealed partial class GameManager
    {
        /// <summary>Offline honey the open Welcome back offer multiplies; zero when there is no offer.</summary>
        private BigNumber offlineAdBase;

        private void InitAds()
        {
            // The mock stands in until the AdMob and UMP SDKs are added.
            ads = new AdManager(new MockAdService(adSettings), new StubConsentService(), adSettings,
                IsModalOpen, UtcNow);
            rewardedBoostView.Init(boosts, UtcNow, CanWatchBoostAd, TryWatchBoostAd);
            welcomeBack.Init(offlineSettings.RewardedMultiplier, CanWatchOfflineAd, TryWatchOfflineAd);
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

        private void HandleBoostAdDone(bool rewarded)
        {
            if (rewarded)
                boosts.StartRewardedHoney(UtcNow());
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
            }
            else
            {
                welcomeBack.AdFinished(false, amount);
            }
            SaveNow();
        }

        private bool IsModalOpen() => backButton.IsModalOpen;

        private static double UtcNow() => GameClock.DeviceUtc;
    }
}
