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
    /// at once; then a short "double it" offer plays a rewarded ad on the player's tap. Once the
    /// daily streak is open, every few butterflies one carries a Dewdrops pouch instead of honey
    /// (a few a day, only while a rewarded ad is ready): caught, it offers to open the pouch with
    /// a rewarded ad, or the player leaves it. The offer is the only limit on these ads: one per
    /// butterfly, gone after a few seconds (docs/DEWDROPS.md section 8).
    /// </summary>
    public sealed partial class GameManager
    {
        [Header("Visitors")]
        [SerializeField] private VisitorSettings visitorSettings;
        [SerializeField] private GiftOfferView giftOffer;

        private VisitorManager visitors;

        /// <summary>Honey the open "double it" offer adds; zero when there is no offer.</summary>
        private BigNumber giftAdExtra;

        private DewPouch pouch;
        private System.Random pouchRandom;

        public VisitorManager Visitors => visitors;

        /// <summary>Needs the camera (pollen), economy and ads.</summary>
        private void InitVisitors()
        {
            visitors = new VisitorManager(visitorSettings, worldRoot, System.Environment.TickCount, NextButterflyCarriesPouch);
            pouchRandom = new System.Random(System.Environment.TickCount ^ 0x5EED);
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

        private bool CanWatchGiftAd() => giftOffer.IsShowing && (giftOffer.IsPouch || !giftAdExtra.IsZero) && ads.CanShowRewarded();

        /// <summary>
        /// Plays the rewarded ad of the open butterfly offer: it doubles the last gift, or opens the
        /// pouch. False when there is no offer or no ad.
        /// </summary>
        public bool TryWatchGiftAd() =>
            CanWatchGiftAd() && ads.TryShowRewarded(giftOffer.IsPouch ? (System.Action<bool>)HandlePouchAdDone : HandleGiftAdDone);

        /// <summary>Asked as each butterfly sets off: every few carry the pouch, once the streak is open and an ad is ready.</summary>
        private bool NextButterflyCarriesPouch()
        {
            bool known = TryGetToday(out long today);
            bool allowed = known && DailyUnlocked(today) && ads.CanShowRewarded();
            return pouch.NextCarries(known ? today : 0, allowed, visitorSettings.PouchEvery, visitorSettings.PouchesPerDay);
        }

        private void HandleVisitorAppeared() => sound.Play(Sfx.ButterflyAppear);

        private void HandleVisitorCaught(Vector3 position, bool hasPouch)
        {
            if (hasPouch)
            {
                CatchPouch();
                return;
            }
            BigNumber gift = VisitorGift();
            economy.Grant(gift);
            Vector2 screen = ScreenPoint(position);
            uiEffects.FloatHoney(screen, gift);
            uiEffects.FlyHoney(screen, 5);
            sound.Play(Sfx.ButterflyCatch);
            Pulse(true);
            FirebaseServices.Log("visitor_catch", ("play_minutes", (long)(stats.playSeconds / 60)), ("pouch", 0L));

            giftAdExtra = gift * BigNumber.FromDouble(visitorSettings.AdMultiplier - 1);
            if (!giftAdExtra.IsZero && ads.CanShowRewarded())
                giftOffer.Show(giftAdExtra, visitorSettings.OfferSeconds);
            else
                giftAdExtra = BigNumber.Zero;
        }

        /// <summary>The pouch only opens with an ad: no honey, just the offer (when an ad is still ready).</summary>
        private void CatchPouch()
        {
            giftAdExtra = BigNumber.Zero;
            sound.Play(Sfx.ButterflyCatch);
            Pulse(true);
            FirebaseServices.Log("visitor_catch", ("play_minutes", (long)(stats.playSeconds / 60)), ("pouch", 1L));
            if (ads.CanShowRewarded())
                giftOffer.ShowPouch(visitorSettings.PouchOfferSeconds);
        }

        private void HandlePouchAdDone(bool rewarded)
        {
            if (rewarded)
            {
                int amount = DewPouch.Roll(pouchRandom.NextDouble(), visitorSettings.PouchMinDew, visitorSettings.PouchMaxDew);
                EarnDew(amount, "butterfly_pouch", RectTransformUtility.WorldToScreenPoint(null, giftOffer.Button.position));
                sound.Play(Sfx.Reward);
                FirebaseServices.Log("ad_rewarded", ("placement", "butterfly_pouch"));
            }
            giftOffer.Finish();
            SaveNow();
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
