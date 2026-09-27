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
    /// <summary>"Move the Queen" gate, reward, reset/keep rules and the next garden, against the real Main scene.</summary>
    public class QueenMoveTests
    {
        GameManager game;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            TestSave.Clear();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
        }

        [Test]
        public void Move_LockedBelowBloomThreshold_EvenWithHoney()
        {
            game.Economy.Grant(BigNumber.Create(1, 30));
            Assert.That(game.Bloom.Fraction, Is.LessThan(game.Prestige.MoveUnlockBloom));
            Assert.That(game.TryMoveQueen(), Is.False);
            Assert.That(game.Prestige.GardenIndex, Is.Zero);
        }

        [Test]
        public void Move_LockedWithoutHoney_EvenWhenBloomed()
        {
            BloomTo(game.Prestige.MoveUnlockBloom);
            Assert.That(game.Economy.Honey, Is.LessThan(game.Prestige.MoveCost));
            Assert.That(game.TryMoveQueen(), Is.False);
            Assert.That(game.Prestige.GardenIndex, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Move_ResetsRunAndKeepsJelly()
        {
            BloomTo(game.Prestige.MoveUnlockBloom);
            EarnRunHoney(100_000);
            game.Economy.Grant(BigNumber.Create(1, 9));
            Assert.That(game.Upgrades.TryAddBee(), Is.True);
            Assert.That(game.Upgrades.TryBuySpeed(), Is.True);
            BigNumber lifetimeBefore = game.Economy.LifetimeHoneyEarned;

            BigNumber preview = game.Prestige.PreviewJelly(game.Economy.RunHoneyEarned, game.Bloom.Fraction);
            Assert.That(preview.ToDouble(), Is.EqualTo(10), "floor(sqrt(100K / 1K)) = 10 at the threshold.");
            GardenConfig firstGarden = game.Prestige.CurrentGarden;

            Assert.That(game.TryMoveQueen(), Is.True);
            yield return null;

            Assert.That(game.Prestige.RoyalJelly, Is.EqualTo(preview));
            Assert.That(game.Prestige.LifetimeJelly, Is.EqualTo(preview));
            Assert.That(game.Prestige.GardenIndex, Is.EqualTo(1));
            Assert.That(game.Prestige.CurrentGarden, Is.Not.SameAs(firstGarden));

            Assert.That(game.Economy.Honey.IsZero);
            Assert.That(game.Economy.RunHoneyEarned.IsZero);
            Assert.That(game.Economy.LifetimeHoneyEarned, Is.EqualTo(lifetimeBefore), "Lifetime stats survive the move.");
            Assert.That(game.Upgrades.BeesBought, Is.Zero);
            Assert.That(game.Upgrades.SpeedLevel, Is.Zero);
            Assert.That(game.Bees.Count, Is.EqualTo(1));
            Assert.That(game.Bees.CountOfTier(0), Is.EqualTo(1));

            Assert.That(game.Bloom.BloomedCount, Is.Zero);
            Assert.That(game.Bloom.TotalSlots, Is.EqualTo(game.Prestige.CurrentGarden.Slots.Count));
            foreach (Flower flower in game.Flowers.Flowers)
                Assert.That(flower.Value, Is.EqualTo(flower.Type.NectarValue * game.Prestige.CurrentGarden.GardenValueMultiplier).Within(1e-4));

            // Only one garden is left in the world once the old one is destroyed.
            Assert.That(Object.FindObjectsByType<GroundView>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [Test]
        public void CompleteGarden_AddsFullBloomBonus()
        {
            BloomTo(1f);
            EarnRunHoney(100_000);
            Assert.That(game.Bloom.IsComplete);
            Assert.That(game.Prestige.BloomBonus(game.Bloom.Fraction), Is.EqualTo(1.0).Within(1e-6));
            Assert.That(game.Prestige.PreviewJelly(game.Economy.RunHoneyEarned, game.Bloom.Fraction).ToDouble(), Is.EqualTo(20));
        }

        [UnityTest]
        public IEnumerator PastLastGarden_RepeatsWithLoopBonus()
        {
            for (int move = 0; move < 4; move++)
            {
                BloomTo(game.Prestige.MoveUnlockBloom);
                game.Economy.Grant(game.Prestige.MoveCost);
                Assert.That(game.TryMoveQueen(), Is.True, $"Move {move + 1} failed.");
                yield return null;
            }

            // Three authored gardens: index 4 is the second repeat of the last one.
            Assert.That(game.Prestige.GardenIndex, Is.EqualTo(4));
            Assert.That(game.Prestige.LoopMultiplier, Is.EqualTo(1.5625).Within(1e-9));
            GardenConfig last = game.Prestige.CurrentGarden;
            Assert.That(game.Prestige.MoveCost.ToDouble(), Is.EqualTo(last.MoveHoneyCost * 1.5625).Within(1e-3));
            Flower flower = game.Flowers.Flowers[0];
            Assert.That(flower.Value, Is.EqualTo(flower.Type.NectarValue * last.GardenValueMultiplier * 1.5625).Within(1e-3));
        }

        [UnityTest]
        public IEnumerator Panel_MoveButtonFollowsGateAndConfirmMoves()
        {
            var panel = Object.FindAnyObjectByType<QueenPanelView>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.IsOpen, Is.False);

            panel.Open();
            yield return null;
            Assert.That(panel.IsOpen);
            panel.AskConfirm();
            Assert.That(panel.IsConfirming, Is.False, "Confirm must not open while the move is locked.");

            BloomTo(game.Prestige.MoveUnlockBloom);
            game.Economy.Grant(game.Prestige.MoveCost);
            yield return new WaitForSeconds(0.3f);
            panel.AskConfirm();
            Assert.That(panel.IsConfirming);

            panel.CancelConfirm();
            Assert.That(panel.IsConfirming, Is.False);
            Assert.That(game.Prestige.GardenIndex, Is.Zero);

            panel.AskConfirm();
            panel.ConfirmMove();
            Assert.That(game.Prestige.GardenIndex, Is.EqualTo(1));
            Assert.That(panel.IsOpen, Is.False);
        }

        /// <summary>Blooms active flowers until the garden reaches <paramref name="target"/>.</summary>
        void BloomTo(float target)
        {
            for (int pass = 0; pass < 64 && !PrestigeMath.IsUnlocked(game.Bloom.Fraction, target); pass++)
            {
                for (int i = 0; i < game.Flowers.Flowers.Count && !PrestigeMath.IsUnlocked(game.Bloom.Fraction, target); i++)
                {
                    Flower flower = game.Flowers.Flowers[i];
                    for (int visits = 0; flower.IsActive && !flower.IsBloomed && visits < 1000; visits++)
                        game.Flowers.Collect(flower, 0.001);
                }
            }
            Assert.That(PrestigeMath.IsUnlocked(game.Bloom.Fraction, target), $"Could not bloom to {target:P0}.");
        }

        /// <summary>Deposits honey so it counts toward the run total (Grant does not).</summary>
        void EarnRunHoney(double honey) => game.Economy.Deposit(honey, 1, Time.timeAsDouble);
    }
}
