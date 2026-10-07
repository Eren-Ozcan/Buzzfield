using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>App-level settings that belong to no single system.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Game Settings", fileName = "GameSettings")]
    public sealed class GameSettings : ScriptableObject
    {
        [SerializeField, Min(30)] private int targetFrameRate = 60;
        [SerializeField, Min(5f)] private float autosaveIntervalSeconds = 30f;
        [SerializeField] private string saveFileName = "buzzfield_save.json";
        [Tooltip("Ask a time server for UTC, so offline earnings survive device clock changes and reboots.")]
        [SerializeField] private bool fetchTrustedTime = true;
        [Tooltip("Opened by Privacy Policy in the settings panel (the studio page the store listing links).")]
        [SerializeField] private string privacyPolicyUrl = "https://yilkgames.com/privacy-policy/";

        [Header("First return gift")]
        [Tooltip("The first Welcome back ever also gives this many seconds of the bees' honey per second.")]
        [SerializeField, Min(0f)] private float welcomeGiftSeconds = 120f;
        [SerializeField, Min(0f)] private float welcomeGiftMinimum = 100f;

        public int TargetFrameRate => targetFrameRate;
        public float AutosaveIntervalSeconds => autosaveIntervalSeconds;
        public string SaveFileName => saveFileName;
        public bool FetchTrustedTime => fetchTrustedTime;
        public string PrivacyPolicyUrl => privacyPolicyUrl;
        public float WelcomeGiftSeconds => welcomeGiftSeconds;
        public float WelcomeGiftMinimum => welcomeGiftMinimum;
    }
}
