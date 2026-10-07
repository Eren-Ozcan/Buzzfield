using Buzzfield.Audio;
using Buzzfield.Economy;
using Buzzfield.Platform;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// Dewdrops, the premium currency (docs/DEWDROPS.md). Every Dewdrop goes through
    /// <see cref="EarnDew"/> or <see cref="TrySpendDew"/>, which log where it came from and went;
    /// earning plays its sound and flies crystals into the counter.
    /// </summary>
    public sealed partial class GameManager
    {
        [Header("Dewdrops")]
        [SerializeField] private DewSettings dewSettings;

        private readonly DewManager dew = new DewManager();

        public DewManager Dew => dew;

        /// <summary>Needs the HUD and the effects layer.</summary>
        private void InitDew() => hud.InitDew(dew);

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

        /// <summary>Spends Dewdrops on <paramref name="item"/> (analytics name); false when the balance is short.</summary>
        public bool TrySpendDew(long cost, string item)
        {
            if (!dew.TrySpend(cost))
                return false;
            FirebaseServices.Log("dew_spend", ("item", item), ("amount", cost), ("balance", dew.Balance));
            SaveNow();
            return true;
        }

        /// <summary>The garden bloomed to 100%: its Dewdrops fly from the middle of the screen, where the banner is.</summary>
        private void GrantGardenDew() =>
            EarnDew(dewSettings.GardenCompleteReward, "garden_complete", new Vector2(Screen.width * 0.5f, Screen.height * 0.6f));
    }
}
