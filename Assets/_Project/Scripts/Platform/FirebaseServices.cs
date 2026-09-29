using System.Collections.Generic;
using Buzzfield.Core;
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
using UnityEngine;

namespace Buzzfield.Platform
{
    /// <summary>
    /// Firebase Analytics and Crashlytics. Starts in the background; events logged before
    /// that are queued. Off in the editor, so play mode and the tests never touch the
    /// network. Ad revenue arrives as the ad_impression event Analytics logs by itself once
    /// AdMob is linked to Firebase, so the game never logs its own ad_impression.
    /// </summary>
    public static class FirebaseServices
    {
        private const int MaxQueued = 64;

        private static readonly Queue<KeyValuePair<string, Parameter[]>> Queued = new Queue<KeyValuePair<string, Parameter[]>>();
        private static bool started;
        private static bool ready;
        private static TcfConsent? pendingConsent;

        private static bool Enabled => !Application.isEditor;

        public static void Start()
        {
            if (!Enabled || started)
                return;
            started = true;
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.Result != DependencyStatus.Available)
                {
                    Debug.LogWarning("Firebase unavailable: " + (task.IsFaulted ? task.Exception?.Message : task.Result.ToString()));
                    return;
                }
                try
                {
                    // Both settings persist on the device, so they are written every launch.
                    FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    Crashlytics.IsCrashlyticsCollectionEnabled = true;
                    Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                    FirebaseAnalytics.SetUserProperty("build", Debug.isDebugBuild ? "dev" : "release");
                }
                catch (System.Exception e)
                {
                    // A build without google-services.json has no default app; play on without it.
                    Debug.LogWarning("Firebase failed to start: " + e.Message);
                    Queued.Clear();
                    return;
                }
                ready = true;
                ApplyConsent();
                while (Queued.Count > 0)
                {
                    KeyValuePair<string, Parameter[]> e = Queued.Dequeue();
                    FirebaseAnalytics.LogEvent(e.Key, e.Value);
                }
            });
        }

        /// <summary>Consent Mode v2 from the consent form; applied as soon as Firebase is up.</summary>
        public static void SetConsent(TcfConsent consent)
        {
            pendingConsent = consent;
            if (ready)
                ApplyConsent();
        }

        /// <summary>Logs one analytics event; values are long, double or string.</summary>
        public static void Log(string name, params (string key, object value)[] parameters)
        {
            if (!Enabled)
                return;
            var list = new Parameter[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                list[i] = ToParameter(parameters[i].key, parameters[i].value);
            if (ready)
                FirebaseAnalytics.LogEvent(name, list);
            else if (Queued.Count < MaxQueued)
                Queued.Enqueue(new KeyValuePair<string, Parameter[]>(name, list));
        }

        private static void ApplyConsent()
        {
            if (!pendingConsent.HasValue)
                return;
            TcfConsent c = pendingConsent.Value;
            FirebaseAnalytics.SetConsent(new Dictionary<ConsentType, ConsentStatus>
            {
                { ConsentType.AnalyticsStorage, c.AnalyticsStorage ? ConsentStatus.Granted : ConsentStatus.Denied },
                { ConsentType.AdStorage, c.AdStorage ? ConsentStatus.Granted : ConsentStatus.Denied },
                { ConsentType.AdUserData, c.AdUserData ? ConsentStatus.Granted : ConsentStatus.Denied },
                { ConsentType.AdPersonalization, c.AdPersonalization ? ConsentStatus.Granted : ConsentStatus.Denied },
            });
        }

        private static Parameter ToParameter(string key, object value)
        {
            switch (value)
            {
                case bool b: return new Parameter(key, b ? 1L : 0L);
                case int i: return new Parameter(key, (long)i);
                case long l: return new Parameter(key, l);
                case float f: return new Parameter(key, (double)f);
                case double d: return new Parameter(key, d);
                default: return new Parameter(key, value?.ToString() ?? string.Empty);
            }
        }
    }
}
