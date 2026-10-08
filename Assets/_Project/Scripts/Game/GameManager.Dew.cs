using Buzzfield.Audio;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Platform;
using Buzzfield.UI;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// Dewdrops, the premium currency (docs/DEWDROPS.md). Every Dewdrop goes through
    /// <see cref="EarnDew"/> or <see cref="TrySpendDew"/>, which log where it came from and went;
    /// earning plays its sound and flies crystals into the counter. The shop's Boosts tab spends
    /// them on time skips and honey boosts (<see cref="TryBuyDewOffer"/>).
    /// </summary>
    public sealed partial class GameManager
    {
        [Header("Dewdrops")]
        [SerializeField] private DewSettings dewSettings;
        [SerializeField] private DewBoostChipView dewBoostChip;

        private readonly DewManager dew = new DewManager();

        public DewManager Dew => dew;

        public DewSettings DewSettings => dewSettings;

        /// <summary>The Boosts tab sells: the first garden is complete (or the Queen has moved).</summary>
        public bool BoostsUnlocked => stats.gardensCompleted >= dewSettings.BoostsUnlockGardens || prestige.MovesMade > 0;

        /// <summary>Needs the HUD, the effects layer, the shop and the boosts.</summary>
        private void InitDew()
        {
            hud.InitDew(dew);
            shopPanel.BoostsTab.Init(dewSettings, dew, boosts, () => BoostsUnlocked, TimeSkipHoney, TryBuyDewOffer, shopPanel.SetStatus);
            dewBoostChip.Init(boosts, tweener, feedbackSettings, shopPanel.OpenBoosts);
        }

        /// <summary>Adds Dewdrops from <paramref name="source"/> (analytics name); crystals fly from <paramref name="from"/>.</summary>
        public void EarnDew(long amount, string source, Vector2 from)
        {
            if (amount <= 0)
                return;
            dew.Earn(amount);
            uiEffects.FlyDew(from, (int)Mathf.Clamp(amount / 2, 3, 8));
            sound.Play(Sfx.DewGain);
            FirebaseServices.Log("dew_earn", ("source", source), ("amount", amount), ("balance", dew.Balance));
        }

        /// <summary>
        /// Spends Dewdrops on <paramref name="item"/> (analytics name); false when the balance is short.
        /// Does not save: the caller hands out what was bought first, then saves both together.
        /// </summary>
        public bool TrySpendDew(long cost, string item)
        {
            if (!dew.TrySpend(cost))
                return false;
            FirebaseServices.Log("dew_spend", ("item", item), ("amount", cost), ("balance", dew.Balance));
            return true;
        }

        /// <summary>Honey the time skip at <paramref name="offerIndex"/> pays right now (in full, no efficiency cut).</summary>
        public BigNumber TimeSkipHoney(int offerIndex)
        {
            if (offerIndex < 0 || offerIndex >= dewSettings.Offers.Count)
                return BigNumber.Zero;
            return RewardMath.IncomeSeconds(TheoreticalHoneyPerSecond(), dewSettings.Offers[offerIndex].seconds, dewSettings.TimeSkipMinimumHoney);
        }

        /// <summary>
        /// Buys the Boosts tab offer at <paramref name="offerIndex"/>: a time skip pays its honey now
        /// (outside the run total, so it never feeds Royal Jelly), a honey boost joins the line.
        /// False when the tab is locked or the balance is short.
        /// </summary>
        public bool TryBuyDewOffer(int offerIndex)
        {
            if (!BoostsUnlocked || offerIndex < 0 || offerIndex >= dewSettings.Offers.Count)
                return false;
            DewOffer offer = dewSettings.Offers[offerIndex];
            BigNumber honey = offer.kind == DewOfferKind.TimeSkip ? TimeSkipHoney(offerIndex) : BigNumber.Zero;
            if (!TrySpendDew(offer.cost, offer.id))
                return false;
            if (offer.kind == DewOfferKind.TimeSkip)
            {
                economy.Grant(honey);
                uiEffects.FlyHoney(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), 8);
                sound.Play(Sfx.HoneyCollect);
            }
            else
            {
                boosts.AddDewBoost(offer.multiplier, offer.seconds, GameClock.DeviceUtc);
                sound.Play(Sfx.PurchaseSuccess);
            }
            Pulse(true);
            SaveNow();
            return true;
        }

        /// <summary>The garden bloomed to 100%: its Dewdrops fly from the middle of the screen, where the banner is.</summary>
        private void GrantGardenDew() =>
            EarnDew(dewSettings.GardenCompleteReward, "garden_complete", new Vector2(Screen.width * 0.5f, Screen.height * 0.6f));
    }
}
