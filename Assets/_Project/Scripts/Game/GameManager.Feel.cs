using Buzzfield.Audio;
using Buzzfield.Core;
using Buzzfield.Flowers;
using Buzzfield.Platform;
using Buzzfield.UI;
using Buzzfield.Upgrades;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// How the game sounds and feels: the sound manager (effects, music, ambience by bloom,
    /// swarm by bee count), the phone's vibration, the "+honey" numbers and flying honey, and the
    /// interface cues every button sends. Everything follows the player's settings.
    /// </summary>
    public sealed partial class GameManager : IUiCues
    {
        [Header("Feel")]
        [SerializeField] private SoundLibrary soundLibrary;
        [SerializeField] private UiEffectsView uiEffects;

        private SoundManager sound;
        private IHapticService haptics;
        private double lastTickPulse = double.MinValue;
        private bool modalWasOpen;

        /// <summary>The player's settings; the save holds the same object.</summary>
        private readonly PlayerSettings playerSettings = new PlayerSettings();

        public SoundManager Sound => sound;
        public PlayerSettings Settings => playerSettings;

        /// <summary>Vibration pulses asked for that the setting let through (light, important).</summary>
        public int PulsesSent { get; private set; }

        /// <summary>Needs the bee, bloom, upgrade and economy managers.</summary>
        private void InitFeel()
        {
            sound = new SoundManager(soundLibrary, transform);
            haptics = PlatformServices.CreateHaptics();
            uiEffects.Init(tweener, feedbackSettings);
            upgrades.OnPurchased += HandleUpgradePurchased;
            bloom.OnSproutActivated += HandleSproutActivated;
            welcomeBack.OnCollected += HandleWelcomeBackCollected;
            ApplySettings();
        }

        private void DisposeFeel()
        {
            if (upgrades != null)
                upgrades.OnPurchased -= HandleUpgradePurchased;
            if (bloom != null)
                bloom.OnSproutActivated -= HandleSproutActivated;
            if (welcomeBack != null)
                welcomeBack.OnCollected -= HandleWelcomeBackCollected;
            sound?.Dispose();
        }

        /// <summary>Applies the music and sound switches.</summary>
        private void ApplySettings()
        {
            sound.MusicOn = playerSettings.musicOn;
            sound.EffectsOn = playerSettings.soundOn;
        }

        /// <summary>Panels opening and closing, the loops following the garden, silence under ads.</summary>
        private void TickFeel(float unscaledDeltaTime)
        {
            bool modal = IsModalOpen();
            if (modal != modalWasOpen)
            {
                modalWasOpen = modal;
                sound.Play(modal ? Sfx.UiOpen : Sfx.UiClose);
            }
            sound.SetBloom(bloom.Fraction);
            sound.SetBees(beeManager.Count);
            sound.SetPaused(ads.IsShowing);
            sound.Tick(unscaledDeltaTime);
        }

        /// <summary>One vibration pulse, if the setting allows it; light ticks close together merge.</summary>
        private void Pulse(bool important)
        {
            if (!HapticRules.Plays(playerSettings.vibration, important))
                return;
            if (!important)
            {
                double now = Time.unscaledTimeAsDouble;
                if (now - lastTickPulse < HapticRules.MinTickGapSeconds)
                    return;
                lastTickPulse = now;
            }
            PulsesSent++;
            haptics.Pulse(important);
        }

        void IUiCues.Tap() => sound.Play(Sfx.UiTap);

        void IUiCues.Refused()
        {
            sound.Play(Sfx.UiRefused);
            Pulse(false);
        }

        private Vector2 ScreenPoint(Vector3 world) => worldCamera.WorldToScreenPoint(world);

        private void HandleUpgradePurchased(UpgradeCard card)
        {
            // Evolve sounds when the merge lands (HandleBeeEvolved), not when it is paid for.
            if (card == UpgradeCard.AddBee)
                sound.Play(Sfx.BeeSpawn);
            else if (card != UpgradeCard.Evolve)
                sound.Play(Sfx.UpgradeBuy);
            uiEffects.Sparkle(bottomBar.CardOf(card));
            Pulse(false);
        }

        private void HandleSproutActivated(Flower _) => sound.Play(Sfx.SproutPop);

        private void HandleWelcomeBackCollected()
        {
            sound.Play(Sfx.HoneyCollect);
            uiEffects.FlyHoney(welcomeBack.CollectButton, 8);
        }
    }
}
