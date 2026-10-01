using System.Collections;
using Buzzfield.Core;
using Buzzfield.Economy;
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
    /// <summary>Pollen shake against the real Main scene: swipes, cooldown, rewards and the hint.</summary>
    public class PollenFlowTests
    {
        GameManager game;

        [UnitySetUp]
        public IEnumerator LoadFresh()
        {
            TestSave.Clear();
            yield return LoadMain();
        }

        [UnityTearDown]
        public IEnumerator ResetTime()
        {
            Time.timeScale = 1f;
            yield return null;
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

        static int ActiveCount(GameManager game)
        {
            int active = 0;
            foreach (Flower flower in game.Flowers.Flowers)
            {
                if (flower.IsActive)
                    active++;
            }
            return active;
        }

        [UnityTest]
        public IEnumerator SwipeAcrossFlower_ShakesItForHoneyAndBloom()
        {
            Flower flower = FirstWithPollen(game);
            BigNumber honeyBefore = game.Economy.Honey;
            float bloomBefore = flower.Bloom;

            // A short horizontal swipe through the flower head, as the EventSystem would send it.
            Vector2 center = Camera.main.WorldToScreenPoint(flower.NectarPoint);
            var catcher = Object.FindAnyObjectByType<SwipeCatcher>();
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 0, position = center + new Vector2(-40f, 0f) };
            catcher.OnPointerDown(pointer);
            pointer.position = center + new Vector2(40f, 0f);
            catcher.OnDrag(pointer);
            catcher.OnPointerUp(pointer);
            yield return null;

            Assert.That(flower.HasPollen, Is.False);
            Assert.That(flower.PollenCooldown, Is.GreaterThan(0f));
            Assert.That(game.Economy.Honey, Is.GreaterThan(honeyBefore));
            Assert.That(flower.Bloom, Is.GreaterThan(bloomBefore));
            Assert.That(game.Stats.flowersShaken, Is.GreaterThanOrEqualTo(1));
        }

        [UnityTest]
        public IEnumerator SwipeOverEmptyGround_ShakesNothing()
        {
            var catcher = Object.FindAnyObjectByType<SwipeCatcher>();
            // A tap on the hive, which stands clear of every flower.
            Vector2 hive = Camera.main.WorldToScreenPoint(Object.FindAnyObjectByType<HiveView>().transform.position);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 0, position = hive };
            catcher.OnPointerDown(pointer);
            catcher.OnPointerUp(pointer);
            yield return null;
            Assert.That(game.Stats.flowersShaken, Is.Zero);
        }

        [UnityTest]
        public IEnumerator ShakenFlowers_WaitForTheCooldown()
        {
            int active = ActiveCount(game);
            Assert.That(game.ShakeAllFlowers(), Is.EqualTo(active));
            Assert.That(game.ShakeAllFlowers(), Is.Zero, "Every flower is cooling down.");

            Time.timeScale = 10f;
            float longest = 0f;
            foreach (Flower flower in game.Flowers.Flowers)
                longest = Mathf.Max(longest, flower.PollenCooldown);
            double ready = Time.timeAsDouble + longest + 0.5;
            while (Time.timeAsDouble < ready)
                yield return null;
            Time.timeScale = 1f;

            Assert.That(game.ShakeAllFlowers(), Is.EqualTo(ActiveCount(game)));
        }

        [UnityTest]
        public IEnumerator Shake_PaysTheFloorBeforeTheBeesEarn()
        {
            // Fresh game: no deposit yet, so every shake pays the minimum nectar at the flower's value.
            Assert.That(game.Economy.HoneyPerSecond(Time.timeAsDouble).IsZero);
            double nectar = game.ShakeNectar();
            Assert.That(nectar, Is.GreaterThan(0));

            Flower flower = FirstWithPollen(game);
            BigNumber before = game.Economy.Honey;
            int shaken = game.ShakeAllFlowers();
            Assert.That(shaken, Is.GreaterThan(0));
            BigNumber expected = HoneyFormula.Honey(nectar, flower.Value, game.Economy.HoneyValueMultiplier, 1) * shaken;
            Assert.That((game.Economy.Honey - before).ToDouble(), Is.EqualTo(expected.ToDouble()).Within(1e-6),
                "Garden 1 starts on Daisies only, so every shake pays the same.");
            Assert.That(game.Economy.HoneyPerSecond(Time.timeAsDouble).IsZero, "Shakes do not count as bee income.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SwipeHint_StaysHiddenAfterReload()
        {
            var hint = Object.FindAnyObjectByType<SwipeHintView>();
            Assert.That(hint.IsVisible);
            game.ShakeAllFlowers();
            Assert.That(hint.IsVisible, Is.False);
            game.SaveNow();

            yield return LoadMain();
            Assert.That(Object.FindAnyObjectByType<SwipeHintView>().IsVisible, Is.False);
            Assert.That(game.Stats.flowersShaken, Is.GreaterThanOrEqualTo(3));
        }
    }

    /// <summary>Harvested honey against the economy alone, no scene.</summary>
    public class HarvestTests
    {
        [Test]
        public void Harvest_CountsForTheRunButNotForHoneyPerSecond()
        {
            var settings = ScriptableObject.CreateInstance<EconomySettings>();
            var economy = new EconomyManager(settings, 0);
            BigNumber start = economy.Honey;

            BigNumber honey = economy.Harvest(10, 2);
            Assert.That(honey.ToDouble(), Is.EqualTo(20).Within(1e-9));
            Assert.That((economy.Honey - start).ToDouble(), Is.EqualTo(20).Within(1e-9));
            Assert.That(economy.RunHoneyEarned.ToDouble(), Is.EqualTo(20).Within(1e-9));
            Assert.That(economy.LifetimeHoneyEarned.ToDouble(), Is.EqualTo(20).Within(1e-9));
            Assert.That(economy.HoneyPerSecond(1).IsZero);

            economy.Deposit(10, 2, 0.5);
            Assert.That(economy.HoneyPerSecond(1).IsZero, Is.False);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void Harvest_UsesEveryMultiplier()
        {
            var settings = ScriptableObject.CreateInstance<EconomySettings>();
            var economy = new EconomyManager(settings, 0)
            {
                HoneyValueMultiplier = BigNumber.FromDouble(1.5),
                QueenMultiplier = 2,
                PurchasedMultiplier = 2,
                BoostMultiplier = 2,
            };
            Assert.That(economy.TotalMultiplier.ToDouble(), Is.EqualTo(12).Within(1e-9));
            Assert.That(economy.Harvest(1, 3).ToDouble(), Is.EqualTo(36).Within(1e-9));
            Object.DestroyImmediate(settings);
        }
    }
}
