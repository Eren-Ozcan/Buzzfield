using System;
using Buzzfield.Ads;
using Buzzfield.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.UI
{
    /// <summary>
    /// The settings panel behind the gear: music, sound and vibration switches, the ad privacy
    /// choices where the law asks for them, restore purchases, the privacy policy and
    /// "Delete my data", which needs a second tap to go through. Switches change the
    /// <see cref="PlayerSettings"/> in place and report it; the game applies and saves them.
    /// </summary>
    public sealed class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private Button openButton;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button closeButton;

        [Header("Switches")]
        [SerializeField] private Button musicButton;
        [SerializeField] private TMP_Text musicLabel;
        [SerializeField] private TMP_Text musicIcon;
        [SerializeField] private Button soundButton;
        [SerializeField] private TMP_Text soundLabel;
        [SerializeField] private TMP_Text soundIcon;
        [SerializeField] private Button vibrationButton;
        [SerializeField] private TMP_Text vibrationLabel;
        [SerializeField] private string musicOnGlyph;
        [SerializeField] private string musicOffGlyph;
        [SerializeField] private string soundOnGlyph;
        [SerializeField] private string soundOffGlyph;

        [Header("Privacy and purchases")]
        [SerializeField] private Button privacyChoicesButton;
        [SerializeField] private Button restoreButton;
        [SerializeField] private Button policyButton;
        [SerializeField] private TMP_Text statusText;

        [Header("Delete my data")]
        [SerializeField] private Button deleteButton;
        [SerializeField] private TMP_Text deleteLabel;
        [SerializeField] private Button keepButton;
        [SerializeField] private TMP_Text versionText;

        private PlayerSettings settings;
        private StoreManager store;
        private Action changed;
        private Func<bool> privacyRequired;
        private Action showPrivacy;
        private Action openPolicy;
        private Action deleteData;
        private bool confirming;

        public bool IsOpen => panel.activeSelf;

        /// <summary>The first tap on "Delete my data" happened; the next one deletes.</summary>
        public bool IsConfirmingDelete => confirming;

        /// <param name="playerSettings">Changed in place by the switches.</param>
        /// <param name="onChanged">A switch changed <paramref name="playerSettings"/>.</param>
        /// <param name="privacyOptionsRequired">True where the ad privacy choices must be reachable.</param>
        public void Init(PlayerSettings playerSettings, Action onChanged, StoreManager storeManager,
            Func<bool> privacyOptionsRequired, Action showPrivacyOptions, Action openPrivacyPolicy, Action deleteMyData, string version)
        {
            Unsubscribe();
            settings = playerSettings;
            changed = onChanged;
            store = storeManager;
            privacyRequired = privacyOptionsRequired ?? (() => false);
            showPrivacy = showPrivacyOptions;
            openPolicy = openPrivacyPolicy;
            deleteData = deleteMyData;
            store.OnRestoreFinished += HandleRestoreFinished;

            AddListener(openButton, Open);
            AddListener(closeButton, Close);
            AddListener(musicButton, ToggleMusic);
            AddListener(soundButton, ToggleSound);
            AddListener(vibrationButton, CycleVibration);
            AddListener(privacyChoicesButton, ShowPrivacy);
            AddListener(restoreButton, Restore);
            AddListener(policyButton, OpenPolicy);
            AddListener(deleteButton, Delete);
            AddListener(keepButton, CancelDelete);

            versionText.text = string.Format(Strings.VersionFormat, version);
            panel.SetActive(false);
        }

        public void Open()
        {
            SetConfirming(false);
            statusText.text = string.Empty;
            panel.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            SetConfirming(false);
            panel.SetActive(false);
        }

        /// <summary>Back button: a pending delete is cancelled first, then the panel closes.</summary>
        public void CancelDelete() => SetConfirming(false);

        private void Refresh()
        {
            musicLabel.text = settings.musicOn ? Strings.MusicOn : Strings.MusicOff;
            musicIcon.text = settings.musicOn ? musicOnGlyph : musicOffGlyph;
            soundLabel.text = settings.soundOn ? Strings.SoundOn : Strings.SoundOff;
            soundIcon.text = settings.soundOn ? soundOnGlyph : soundOffGlyph;
            vibrationLabel.text = VibrationText(settings.vibration);
            privacyChoicesButton.gameObject.SetActive(privacyRequired());
            restoreButton.interactable = store.IsReady && !store.IsPurchasing;
        }

        private void ToggleMusic()
        {
            settings.musicOn = !settings.musicOn;
            Changed();
        }

        private void ToggleSound()
        {
            settings.soundOn = !settings.soundOn;
            Changed();
        }

        private void CycleVibration()
        {
            switch (settings.vibration)
            {
                case VibrationMode.All: settings.vibration = VibrationMode.Important; break;
                case VibrationMode.Important: settings.vibration = VibrationMode.Off; break;
                default: settings.vibration = VibrationMode.All; break;
            }
            Changed();
        }

        private void Changed()
        {
            changed?.Invoke();
            Refresh();
        }

        private void ShowPrivacy() => showPrivacy?.Invoke();

        private void OpenPolicy() => openPolicy?.Invoke();

        private void Restore()
        {
            restoreButton.interactable = false;
            statusText.text = Strings.StoreBusy;
            store.RestorePurchases();
        }

        private void HandleRestoreFinished(int granted)
        {
            statusText.text = granted > 0 ? Strings.RestoreDone : Strings.RestoreNothing;
            if (panel.activeSelf)
                Refresh();
        }

        private void Delete()
        {
            if (!confirming)
            {
                SetConfirming(true);
                return;
            }
            SetConfirming(false);
            panel.SetActive(false);
            deleteData?.Invoke();
        }

        private void SetConfirming(bool value)
        {
            confirming = value;
            deleteLabel.text = value ? Strings.DeleteDataConfirm : Strings.DeleteData;
            keepButton.gameObject.SetActive(value);
        }

        private static string VibrationText(VibrationMode mode)
        {
            switch (mode)
            {
                case VibrationMode.All: return Strings.VibrationAll;
                case VibrationMode.Important: return Strings.VibrationImportant;
                default: return Strings.VibrationOff;
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
            RemoveListener(openButton, Open);
            RemoveListener(closeButton, Close);
            RemoveListener(musicButton, ToggleMusic);
            RemoveListener(soundButton, ToggleSound);
            RemoveListener(vibrationButton, CycleVibration);
            RemoveListener(privacyChoicesButton, ShowPrivacy);
            RemoveListener(restoreButton, Restore);
            RemoveListener(policyButton, OpenPolicy);
            RemoveListener(deleteButton, Delete);
            RemoveListener(keepButton, CancelDelete);
        }

        private void Unsubscribe()
        {
            if (store != null)
                store.OnRestoreFinished -= HandleRestoreFinished;
        }

        private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
        {
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
                button.onClick.RemoveListener(action);
        }
    }
}
