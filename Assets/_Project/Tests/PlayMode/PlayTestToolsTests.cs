using System.Collections;
using Buzzfield.Core;
using Buzzfield.Flowers;
using Buzzfield.Game;
using Buzzfield.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>The play test shortcuts on GameManager (Buzzfield > Play Test Tools).</summary>
    public class PlayTestToolsTests
    {
        GameManager game;

        [UnitySetUp]
        public IEnumerator LoadFresh()
        {
            TestSave.Clear();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator GrantHoney_AddsToTheBalance()
        {
            BigNumber before = game.Economy.Honey;
            game.DebugGrantHoney(BigNumber.FromDouble(1000));
            Assert.That((game.Economy.Honey - before).ToDouble(), Is.EqualTo(1000).Within(1e-9));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CompleteGarden_BloomsEverySlotAndOpensTheMove()
        {
            Assert.That(game.DebugBloomNext());
            Assert.That(game.Bloom.BloomedCount, Is.EqualTo(1));

            game.DebugCompleteGarden();
            Assert.That(game.Bloom.IsComplete);
            Assert.That(game.Bloom.BloomedCount, Is.EqualTo(game.Bloom.TotalSlots));
            Assert.That(game.DebugBloomNext(), Is.False, "Nothing left to bloom.");

            game.DebugGrantHoney(BigNumber.FromDouble(1e6));
            Assert.That(game.Prestige.CanMove(game.Bloom.Fraction, game.Economy.Honey));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RefillPollen_And_ResetHint()
        {
            var hint = Object.FindAnyObjectByType<SwipeHintView>();
            game.ShakeAllFlowers();
            Assert.That(hint.IsVisible, Is.False);
            Assert.That(game.ShakeAllFlowers(), Is.Zero);

            game.DebugRefillPollen();
            foreach (Flower flower in game.Flowers.Flowers)
                Assert.That(flower.PollenCooldown, Is.Zero);
            Assert.That(game.ShakeAllFlowers(), Is.GreaterThan(0));

            game.DebugResetSwipeHint();
            Assert.That(hint.IsVisible);
            Assert.That(game.Stats.flowersShaken, Is.Zero);
            yield return null;
        }
    }
}
