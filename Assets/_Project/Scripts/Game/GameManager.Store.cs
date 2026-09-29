using Buzzfield.Ads;
using Buzzfield.Core;
using Buzzfield.Platform;

namespace Buzzfield.Game
{
    /// <summary>
    /// In-app purchases: grants what each product gives and applies the entitlements
    /// (permanent honey multiplier, interstitials off). Every grant saves the game, so a kill
    /// right after a purchase never loses it. Entitlements survive the Queen move.
    /// </summary>
    public sealed partial class GameManager
    {
        private void InitStore()
        {
            // Unity IAP on a phone, the mock in the editor.
            store = new StoreManager(PlatformServices.CreateStoreService(storeCatalog), storeCatalog);
            store.OnProductGranted += HandleProductGranted;
            shopPanel.Init(store, tweener, feedbackSettings, () => ads.PrivacyOptionsRequired, ShowPrivacyOptions);
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
            if (product.honeyGrant > 0)
                economy.Grant(BigNumber.FromDouble(product.honeyGrant));
            if (product.jellyGrant > 0)
                prestige.GrantJelly(BigNumber.FromDouble(product.jellyGrant));
            ApplyEntitlements();
            SaveNow();
        }
    }
}
