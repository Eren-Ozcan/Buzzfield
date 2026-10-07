using Buzzfield.Audio;
using Buzzfield.Core;
using Buzzfield.Flowers;
using Buzzfield.Platform;
using Buzzfield.UI;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// The butterfly: it crosses the garden now and then (never for a new player, behind a panel,
    /// an ad or the Garden Complete celebration). A tap or swipe through it pays a gift of honey
    /// at once; then a short "double it" offer plays a rewarded ad on the player's tap. The
    /// offer is the only limit on that ad: one per butterfly, gone after a few seconds.
    /// </summary>
    public sealed partial class GameManager
    {
        [Header("Visitors")]
        [SerializeField] private VisitorSettings visitorSettings;
        [SerializeField] private GiftOfferView giftOffer;

        private VisitorManager visitors;

        /// <summary>Honey the open "double it" offer adds; zero when there is no offer.</summary>
        private BigNumber giftAdExtra;

        public VisitorManager Visitors => visitors;

        /// <summary>Needs the camera (pollen), economy and ads.</summary>
        private void InitVisitors()
        {
            visitors = new VisitorManager(visitorSettings, worldRoot, System.Environment.TickCount);
            visitors.OnAppeared += HandleVisitorAppeared;
            visitors.OnCaught += HandleVisitorCaught;
            giftOffer.Init(CanWatchGiftAd, TryWatchGiftAd);
        }

        private void DisposeVisitors()
        {
            if (visitors == null)
                return;
            visitors.OnAppeared -= HandleVisitorAppeared;
            visitors.OnCaught -= HandleVisitorCaught;
        }

        /// <summary>The butterfly may come, and keeps its time away from panels, ads and celebrations.</summary>
        private bool IsGardenCalm() => !IsModalOpen() && !gardenComplete.IsShowing && !ads.IsShowing && !store.IsPurchasing;

        private void TickVisitors(float deltaTime) =>
            visitors.Tick(deltaTime, IsGardenCalm(), stats.playSeconds, garden.Bounds);

        /// <summary>Gift for a caught butterfly right now: seconds of the bees' honey per second.</summary>
        public BigNumber VisitorGift() =>
            RewardMath.IncomeSeconds(TheoreticalHoneyPerSecond(), visitorSettings.RewardSeconds, visitorSettings.MinimumReward);

        private bool CanWatchGiftAd() => giftOffer.IsShowing && !giftAdExtra.IsZero && ads.CanShowRewarded();

        /// <summary>Plays the rewarded ad that doubles the last butterfly gift. False when there is no offer or no ad.</summary>
        public bool TryWatchGiftAd() => CanWatchGiftAd() && ads.TryShowRewarded(HandleGiftAdDone);

        private void HandleVisitorAppeared() => sound.Play(Sfx.ButterflyAppear);

        private void HandleVisitorCaught(Vector3 position)
        {
            BigNumber gift = VisitorGift();
            economy.Grant(gift);
            Vector2 screen = ScreenPoint(position);
            uiEffects.FloatHoney(screen, gift);
            uiEffects.FlyHoney(screen, 5);
            sound.Play(Sfx.ButterflyCatch);
            Pulse(true);
            FirebaseServices.Log("visitor_catch", ("play_minutes", (long)(stats.playSeconds / 60)));

            giftAdExtra = gift * BigNumber.FromDouble(visitorSettings.AdMultiplier - 1);
            if (!giftAdExtra.IsZero && ads.CanShowRewarded())
                giftOffer.Show(giftAdExtra, visitorSettings.OfferSeconds);
            else
                giftAdExtra = BigNumber.Zero;
        }

        private void HandleGiftAdDone(bool rewarded)
        {
            BigNumber extra = giftAdExtra;
            giftAdExtra = BigNumber.Zero;
            if (rewarded && !extra.IsZero)
            {
                economy.Grant(extra);
                uiEffects.FlyHoney(giftOffer.Button, 6);
                sound.Play(Sfx.Reward);
                FirebaseServices.Log("ad_rewarded", ("placement", "butterfly_gift"));
            }
            giftOffer.Finish();
            SaveNow();
        }
    }
}
