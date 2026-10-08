using Buzzfield.Ads;
using Buzzfield.Audio;
using Buzzfield.Core;
using Buzzfield.Platform;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// In-app purchases: grants what each product gives (Dewdrops, honey sized in seconds of the
    /// offline rate) and applies the entitlements (permanent honey multiplier, interstitials off).
    /// Every grant saves the game, so a kill right after a purchase never loses it. Entitlements
    /// survive the Queen move.
    /// </summary>
    public sealed partial class GameManager
    {
        private void InitStore()
        {
            // Unity IAP on a phone, the mock in the editor.
            store = new StoreManager(PlatformServices.CreateStoreService(storeCatalog), storeCatalog);
            store.OnProductGranted += HandleProductGranted;
            shopPanel.Init(store, dew, tweener, feedbackSettings, () => ads.PrivacyOptionsRequired, ShowPrivacyOptions,
                () => stats.playSeconds >= storeCatalog.BadgeAfterPlaySeconds);
            ApplyEntitlements();
        }

        private void DisposeStore()
        {
            if (store == null)
                return;
            store.OnProductGranted -= HandleProductGranted;
            store.Dispose();
        }

        private void RestoreStore(Entitlements saved)
        {
            store.Restore(saved);
            ApplyEntitlements();
        }

        private void ApplyEntitlements()
        {
            Entitlements owned = store.Entitlements;
            economy.PurchasedMultiplier = owned.permanentHoney2x ? storeCatalog.PermanentHoneyMultiplier : 1;
            ads.ForcedAdsRemoved = owned.removeAds;
        }

        private void ShowPrivacyOptions() => ads.ShowPrivacyOptions(null);

        private void HandleProductGranted(StoreProduct product)
        {
            var centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (product.dewGrant > 0)
                EarnDew(product.dewGrant, product.id, centre);
            if (product.honeySecondsGrant > 0)
            {
                // Sized by the garden's income, so the same pack is worth the same in every garden.
                economy.Grant(RewardMath.IncomeSeconds(TheoreticalHoneyPerSecond(), product.honeySecondsGrant, storeCatalog.MinimumHoneyGrant));
                uiEffects.FlyHoney(centre, 8);
            }
            ApplyEntitlements();
            sound.Play(Sfx.PurchaseSuccess);
            Pulse(true);
            SaveNow();
        }
    }
}
