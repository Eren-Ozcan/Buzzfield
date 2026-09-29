using System;
using System.Collections.Generic;
using Buzzfield.Ads;
using Buzzfield.Core;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace Buzzfield.Platform
{
    /// <summary>
    /// Google's User Messaging Platform (studio ad policy rule 8): the consent form runs
    /// before any ad request; when consent cannot be had the game plays on without ads.
    /// The GDPR message itself is set up in the AdMob console (Privacy and messaging).
    /// </summary>
    public sealed class UmpConsentService : IConsentService
    {
        private const string DebugEeaKey = "Buzzfield.DebugEea";

        /// <summary>Hashed ids of test phones (from UMP's logcat hint); only they can fake the EEA.</summary>
        private static readonly List<string> TestDeviceHashes = new List<string> { "C6072BA852407A04D43B8DDE46C30DAD" };

        public bool CanRequestAds { get; private set; }

        public bool PrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public event Action<TcfConsent> ConsentChanged;

        /// <summary>
        /// Development builds only: the next launch acts as if the phone were in the EEA, so
        /// the consent form shows. Changing it forgets the stored answer.
        /// </summary>
        public static bool DebugEea
        {
            get => Debug.isDebugBuild && PlayerPrefs.GetInt(DebugEeaKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(DebugEeaKey, value ? 1 : 0);
                PlayerPrefs.Save();
                ConsentInformation.Reset();
            }
        }

        public void RequestConsent(Action onDone)
        {
            // The queue behind ExecuteInUpdate; MobileAds.Initialize would create it too late for UMP.
            MobileAdsEventExecutor.Initialize();
            var request = new ConsentRequestParameters { TagForUnderAgeOfConsent = false };
            if (DebugEea)
            {
                request.ConsentDebugSettings = new ConsentDebugSettings
                {
                    DebugGeography = DebugGeography.EEA,
                    TestDeviceHashedIds = TestDeviceHashes,
                };
            }
            // UMP answers on its own thread; everything after it runs on Unity's.
            ConsentInformation.Update(request, updateError => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (updateError != null)
                    Debug.LogWarning("Consent info update failed: " + updateError.Message);
                ConsentForm.LoadAndShowConsentFormIfRequired(formError => MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (formError != null)
                        Debug.LogWarning("Consent form failed: " + formError.Message);
                    CanRequestAds = ConsentInformation.CanRequestAds();
                    ConsentChanged?.Invoke(ReadTcfConsent());
                    onDone?.Invoke();
                }));
            }));
        }

        public void ShowPrivacyOptions(Action onDone)
        {
            ConsentForm.ShowPrivacyOptionsForm(error => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (error != null)
                    Debug.LogWarning("Privacy options failed: " + error.Message);
                CanRequestAds = ConsentInformation.CanRequestAds();
                ConsentChanged?.Invoke(ReadTcfConsent());
                onDone?.Invoke();
            }));
        }

        /// <summary>The IAB TCF strings UMP wrote to the default SharedPreferences.</summary>
        private static TcfConsent ReadTcfConsent()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var manager = new AndroidJavaClass("android.preference.PreferenceManager"))
                using (AndroidJavaObject prefs = manager.CallStatic<AndroidJavaObject>("getDefaultSharedPreferences", activity))
                {
                    int gdprApplies = prefs.Call<int>("getInt", "IABTCF_gdprApplies", -1);
                    string purposes = prefs.Call<string>("getString", "IABTCF_PurposeConsents", "");
                    return TcfConsent.From(gdprApplies, purposes);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Reading TCF consent failed: " + e.Message);
                return TcfConsent.From(1, null);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            return TcfConsent.From(PlayerPrefs.GetInt("IABTCF_gdprApplies", -1), PlayerPrefs.GetString("IABTCF_PurposeConsents", ""));
#else
            return TcfConsent.Granted;
#endif
        }
    }
}
