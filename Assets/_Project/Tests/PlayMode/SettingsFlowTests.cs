using System.Collections;
using System.Reflection;
using Buzzfield.Audio;
using Buzzfield.Core;
using Buzzfield.Game;
using Buzzfield.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>The settings panel against the real Main scene: switches, saving, the back button and "Delete my data".</summary>
    public class SettingsFlowTests
    {
        GameManager game;
        SettingsPanelView settings;

        [UnitySetUp]
        public IEnumerator LoadFresh()
        {
            TestSave.Clear();
            yield return LoadMain();
        }

        IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            settings = Object.FindAnyObjectByType<SettingsPanelView>();
            Assert.That(game, Is.Not.Null);
            Assert.That(settings, Is.Not.Null);
        }

        static Button ButtonNamed(string name)
        {
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button.name == name)
                    return button;
            }
            Assert.Fail($"No button named {name}.");
            return null;
        }

        static T Hidden<T>(object target, string field) =>
            (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        [UnityTest]
        public IEnumerator Gear_OpensThePanel_AndBackClosesIt()
        {
            Assert.That(settings.IsOpen, Is.False);
            ButtonNamed("SettingsButton").onClick.Invoke();
            Assert.That(settings.IsOpen);
            var back = Object.FindAnyObjectByType<BackButtonHandler>();
            Assert.That(back.IsModalOpen);

            back.HandleBack();
            Assert.That(settings.IsOpen, Is.False);
            Assert.That(back.IsQuitDialogOpen, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Switches_ChangeTheGame_AndSurviveAReload()
        {
            settings.Open();
            ButtonNamed("Music").onClick.Invoke();
            ButtonNamed("Sound").onClick.Invoke();
            ButtonNamed("Vibration").onClick.Invoke();

            Assert.That(game.Settings.musicOn, Is.False);
            Assert.That(game.Settings.soundOn, Is.False);
            Assert.That(game.Settings.vibration, Is.EqualTo(VibrationMode.Important));
            Assert.That(game.Sound.MusicOn, Is.False);
            Assert.That(game.Sound.EffectsOn, Is.False);
            Assert.That(Hidden<TMPro.TMP_Text>(settings, "musicLabel").text, Is.EqualTo(Strings.MusicOff));

            yield return LoadMain();
            Assert.That(game.Settings.musicOn, Is.False);
            Assert.That(game.Settings.soundOn, Is.False);
            Assert.That(game.Settings.vibration, Is.EqualTo(VibrationMode.Important));
            Assert.That(game.Sound.EffectsOn, Is.False);
        }

        [UnityTest]
        public IEnumerator Vibration_CyclesThroughItsThreeModes()
        {
            settings.Open();
            Button vibration = ButtonNamed("Vibration");
            Assert.That(game.Settings.vibration, Is.EqualTo(VibrationMode.All));
            vibration.onClick.Invoke();
            Assert.That(game.Settings.vibration, Is.EqualTo(VibrationMode.Important));
            vibration.onClick.Invoke();
            Assert.That(game.Settings.vibration, Is.EqualTo(VibrationMode.Off));
            vibration.onClick.Invoke();
            Assert.That(game.Settings.vibration, Is.EqualTo(VibrationMode.All));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SoundOff_SilencesButtonTaps()
        {
            Button addBee = ButtonNamed("AddBeeButton");
            game.Economy.Grant(game.Upgrades.AddBeeCost);
            yield return null;
            var feedback = addBee.GetComponent<ButtonFeedback>();
            var pointer = new PointerEventData(EventSystem.current);

            int before = game.Sound.PlayedCount(Sfx.UiTap);
            feedback.OnPointerDown(pointer);
            feedback.OnPointerUp(pointer);
            Assert.That(game.Sound.PlayedCount(Sfx.UiTap), Is.EqualTo(before + 1));

            settings.Open();
            ButtonNamed("Sound").onClick.Invoke();
            settings.Close();
            yield return new WaitForSecondsRealtime(0.15f);
            feedback.OnPointerDown(pointer);
            feedback.OnPointerUp(pointer);
            Assert.That(game.Sound.PlayedCount(Sfx.UiTap), Is.EqualTo(before + 1), "No tap sound with the sound off.");
        }

        [UnityTest]
        public IEnumerator DeleteMyData_NeedsASecondTap_ThenStartsANewGame()
        {
            game.Economy.Grant(BigNumber.FromDouble(500));
            game.Economy.Grant(game.Upgrades.AddBeeCost);
            Assert.That(game.Upgrades.TryAddBee());
            game.SaveNow();

            settings.Open();
            Button delete = ButtonNamed("DeleteButton");
            delete.onClick.Invoke();
            Assert.That(settings.IsConfirmingDelete);
            Assert.That(ButtonNamed("KeepButton").gameObject.activeSelf);

            // Back cancels the pending delete first, and keeps the panel open.
            Object.FindAnyObjectByType<BackButtonHandler>().HandleBack();
            Assert.That(settings.IsConfirmingDelete, Is.False);
            Assert.That(settings.IsOpen);

            delete.onClick.Invoke();
            delete.onClick.Invoke();
            // The scene reloads as a new game.
            yield return null;
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
            Assert.That(game.Bees.Count, Is.EqualTo(game.StartingWorkers));
            Assert.That(game.Economy.Honey.ToDouble(), Is.LessThan(500));
            Assert.That(game.Upgrades.BeesBought, Is.Zero);
        }
    }
}
