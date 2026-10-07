using System.Collections;
using Buzzfield.Audio;
using Buzzfield.Core;
using Buzzfield.Flowers;
using Buzzfield.Game;
using Buzzfield.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// How the game answers the player, against the real Main scene: the swipe trail, rising
    /// "+honey" numbers, the hint's hand, sounds and vibration for purchases and panels, and the
    /// gift on the first return.
    /// </summary>
    public class FeelFlowTests
    {
        GameManager game;

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
            Assert.That(game, Is.Not.Null);
        }

        static Flower FirstWithPollen(GameManager game)
        {
            foreach (Flower flower in game.Flowers.Flowers)
            {
                if (flower.HasPollen)
                    return flower;
            }
            Assert.Fail("No flower has pollen.");
            return null;
        }

        [UnityTest]
        public IEnumerator Swipe_DrawsATrail_ThatStopsWhenTheFingerLifts()
        {
            var trail = Object.FindAnyObjectByType<TrailRenderer>();
            Assert.That(trail, Is.Not.Null, "The swipe trail is spawned with the game.");
            Assert.That(trail.emitting, Is.False);

            Flower flower = FirstWithPollen(game);
            Vector2 center = Camera.main.WorldToScreenPoint(flower.NectarPoint);
            var catcher = Object.FindAnyObjectByType<SwipeCatcher>();
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 0, position = center + new Vector2(-60f, 0f) };
            catcher.OnPointerDown(pointer);
            Assert.That(trail.emitting);
            pointer.position = center + new Vector2(60f, 0f);
            catcher.OnDrag(pointer);
            yield return null;
            Assert.That(trail.emitting);

            catcher.OnPointerUp(pointer);
            Assert.That(trail.emitting, Is.False);
        }

        [UnityTest]
        public IEnumerator Shake_RaisesAHoneyNumber_AndAClimbingSound()
        {
            var effects = Object.FindAnyObjectByType<UiEffectsView>();
            int sounds = game.Sound.PlayedCount(Sfx.PollenShake);
            Assert.That(game.ShakeAllFlowers(), Is.GreaterThan(0));
            Assert.That(effects.FloatingTexts, Is.GreaterThan(0));
            Assert.That(game.Sound.PlayedCount(Sfx.PollenShake), Is.GreaterThan(sounds));

            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(effects.FloatingTexts, Is.Zero, "The numbers fade away.");
        }

        [UnityTest]
        public IEnumerator SwipeHint_HandSweepsAcrossTheGarden()
        {
            var hint = Object.FindAnyObjectByType<SwipeHintView>();
            Assert.That(hint.IsVisible);
            Assert.That(hint.IsHandSweeping);
            Vector2 first = hint.HandPosition;
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Vector2.Distance(first, hint.HandPosition), Is.GreaterThan(10f), "The hand moves.");

            // Shaking a few flowers hides the hint and its hand for good.
            while (hint.IsVisible)
            {
                game.Pollen.Refill();
                game.ShakeAllFlowers();
                yield return null;
            }
            Assert.That(hint.IsHandSweeping, Is.False);
        }

        [UnityTest]
        public IEnumerator Purchase_PlaysItsSound_AndVibrates()
        {
            int spawns = game.Sound.PlayedCount(Sfx.BeeSpawn);
            int buys = game.Sound.PlayedCount(Sfx.UpgradeBuy);
            int pulses = game.PulsesSent;

            game.Economy.Grant(game.Upgrades.AddBeeCost);
            Assert.That(game.Upgrades.TryAddBee());
            Assert.That(game.Sound.PlayedCount(Sfx.BeeSpawn), Is.EqualTo(spawns + 1));

            yield return new WaitForSecondsRealtime(0.2f);
            game.Economy.Grant(game.Upgrades.SpeedCost);
            Assert.That(game.Upgrades.TryBuySpeed());
            Assert.That(game.Sound.PlayedCount(Sfx.UpgradeBuy), Is.EqualTo(buys + 1));
            Assert.That(game.PulsesSent, Is.EqualTo(pulses + 2));
        }

        [UnityTest]
        public IEnumerator VibrationOff_SendsNoPulse()
        {
            game.Settings.vibration = VibrationMode.Off;
            int pulses = game.PulsesSent;
            game.Economy.Grant(game.Upgrades.AddBeeCost);
            Assert.That(game.Upgrades.TryAddBee());
            Assert.That(game.PulsesSent, Is.EqualTo(pulses));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Panels_SoundWhenTheyOpenAndClose()
        {
            int opens = game.Sound.PlayedCount(Sfx.UiOpen);
            int closes = game.Sound.PlayedCount(Sfx.UiClose);
            var shop = Object.FindAnyObjectByType<ShopPanelView>();
            shop.Open();
            yield return null;
            Assert.That(game.Sound.PlayedCount(Sfx.UiOpen), Is.EqualTo(opens + 1));
            yield return new WaitForSecondsRealtime(0.3f);
            shop.Close();
            yield return null;
            Assert.That(game.Sound.PlayedCount(Sfx.UiClose), Is.EqualTo(closes + 1));
        }

        [UnityTest]
        public IEnumerator FirstReturn_BringsAGiftOnce()
        {
            game.SaveNow();
            ShiftSavedClock(-3600);
            yield return LoadMain();

            var welcome = Object.FindAnyObjectByType<WelcomeBackView>();
            Assert.That(game.IsWelcomeBackOpen);
            Assert.That(welcome.IsGiftShown);
            Assert.That(game.Economy.Honey.ToDouble() - game.LastOffline.Amount.ToDouble(), Is.GreaterThanOrEqualTo(Hidden().WelcomeGiftMinimum - 1));

            int collects = game.Sound.PlayedCount(Sfx.HoneyCollect);
            welcome.CollectButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(game.IsWelcomeBackOpen, Is.False);
            Assert.That(game.Sound.PlayedCount(Sfx.HoneyCollect), Is.EqualTo(collects + 1));

            // The next return pays offline honey only.
            game.SaveNow();
            ShiftSavedClock(-3600);
            yield return LoadMain();
            welcome = Object.FindAnyObjectByType<WelcomeBackView>();
            Assert.That(game.IsWelcomeBackOpen);
            Assert.That(welcome.IsGiftShown, Is.False);
        }

        GameSettings Hidden() =>
            (GameSettings)typeof(GameManager).GetField("gameSettings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(game);

        static void ShiftSavedClock(double seconds)
        {
            var files = TestSave.Files();
            SaveData data = files.Load();
            Assert.That(data, Is.Not.Null);
            data.clock.lastSeenUtc += seconds;
            data.clock.lastDeviceUtc += seconds;
            data.clock.lastMonotonicSeconds += seconds;
            Assert.That(files.Save(data));
        }
    }
}
