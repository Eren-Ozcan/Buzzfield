using Buzzfield.Core;
using Buzzfield.Platform;
using Buzzfield.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Buzzfield.Game
{
    /// <summary>
    /// The settings panel: music, sound and vibration switches (saved with the game), the ad
    /// privacy choices, restore purchases, the privacy policy and "Delete my data", which wipes
    /// the save and the analytics data on the device and starts a new game.
    /// </summary>
    public sealed partial class GameManager
    {
        [Header("Settings")]
        [SerializeField] private SettingsPanelView settingsPanel;

        /// <summary>Set once the data is deleted: nothing may write a save on the way out.</summary>
        private bool dataDeleted;

        /// <summary>Needs the store and ads.</summary>
        private void InitSettings()
        {
            settingsPanel.Init(playerSettings, HandleSettingsChanged, store, () => ads.PrivacyOptionsRequired,
                ShowPrivacyOptions, OpenPrivacyPolicy, DeleteAllData, Application.version);
        }

        private void HandleSettingsChanged()
        {
            ApplySettings();
            SaveNow();
        }

        private void OpenPrivacyPolicy() => Application.OpenURL(gameSettings.PrivacyPolicyUrl);

        /// <summary>
        /// Deletes the save (and its backup) and the analytics data on the device, then reloads
        /// the scene as a new game. Purchases come back through Restore Purchases.
        /// </summary>
        public void DeleteAllData()
        {
            dataDeleted = true;
            saveManager.Delete();
            FirebaseServices.ResetAnalyticsData();
            SceneManager.LoadScene(gameObject.scene.name);
        }

        private void RestoreSettings(PlayerSettings saved)
        {
            playerSettings.musicOn = saved.musicOn;
            playerSettings.soundOn = saved.soundOn;
            playerSettings.vibration = saved.vibration;
            ApplySettings();
        }
    }
}
