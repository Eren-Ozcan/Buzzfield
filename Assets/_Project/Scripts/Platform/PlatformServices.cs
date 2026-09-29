using Buzzfield.Ads;
using UnityEngine;

namespace Buzzfield.Platform
{
    /// <summary>
    /// Picks the real SDKs on an Android or iOS player and the mocks everywhere else (the
    /// editor, play mode tests, desktop players), so tests never reach a live service.
    /// </summary>
    public static class PlatformServices
    {
        /// <summary>Tests running in a device player set this to keep the mocks.</summary>
        public static bool ForceMocks { get; set; }

        public static bool UseDeviceServices =>
            !ForceMocks && (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.IPhonePlayer);

        public static IAdService CreateAdService(AdSettings settings) =>
            UseDeviceServices ? new AdMobAdService(settings) : new MockAdService(settings);

        public static IConsentService CreateConsentService() =>
            UseDeviceServices ? new UmpConsentService() : new StubConsentService();

        public static IStoreService CreateStoreService(StoreCatalog catalog) =>
            UseDeviceServices ? new UnityIapStoreService() : new MockStoreService(catalog);
    }
}
