using System.Collections;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Flowers;
using Buzzfield.Game;
using Buzzfield.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>Queen level bonus and Queen abilities: costs, effects, the Queen move, saving and the panel rows.</summary>
    public class QueenAbilityTests
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

        int IndexOf(QueenEffect effect)
        {
            for (int i = 0; i < game.Queen.AbilityCount; i++)
            {
                if (game.Queen.Abilities[i].Effect == effect)
                    return i;
            }
            Assert.Fail($"No ability with {effect}.");
            return -1;
        }

        [Test]
        public void NewGame_HasQueenLevelZeroAndNoBonus()
        {
            Assert.That(game.Queen.Level, Is.Zero);
            Assert.That(game.Queen.HoneyMultiplier, Is.EqualTo(1));
            Assert.That(game.Economy.QueenMultiplier, Is.EqualTo(1));
            Assert.That(game.Queen.AbilityCount, Is.EqualTo(4));
        }

        [Test]
        public void EarnedJelly_RaisesTheLevelAndHoneyPerNectar()
        {
            double before = game.Economy.Deposit(10, 1, Time.timeAsDouble).ToDouble();
            game.Prestige.GrantJelly(BigNumber.FromDouble(10));

            Assert.That(game.Queen.Level, Is.GreaterThanOrEqualTo(3), "10 lifetime jelly reaches Lv 3 on the default curve.");
            Assert.That(game.Economy.QueenMultiplier, Is.EqualTo(game.Queen.HoneyMultiplier));
            double after = game.Economy.Deposit(10, 1, Time.timeAsDouble).ToDouble();
            Assert.That(after, Is.EqualTo(before * game.Queen.HoneyMultiplier).Within(1e-6));
        }

        [Test]
        public void BuyingAnAbility_SpendsJellyButKeepsTheLevel()
        {
            game.Prestige.GrantJelly(BigNumber.FromDouble(10));
            int level = game.Queen.Level;
            int wings = IndexOf(QueenEffect.FlightSpeedPercent);
            BigNumber cost = game.Queen.AbilityCost(wings);

            Assert.That(game.Queen.TryBuy(wings));
            Assert.That(game.Queen.AbilityLevel(wings), Is.EqualTo(1));
            Assert.That(game.Prestige.RoyalJelly, Is.EqualTo(BigNumber.FromDouble(10) - cost));
            Assert.That(game.Prestige.LifetimeJelly.ToDouble(), Is.EqualTo(10), "Spending is not un-earning.");
            Assert.That(game.Queen.Level, Is.EqualTo(level));
            Assert.That(game.Queen.AbilityCost(wings), Is.GreaterThan(cost), "The next level costs more.");
        }

        [Test]
        public void BuyingWithoutJelly_IsRefused()
        {
            int wings = IndexOf(QueenEffect.FlightSpeedPercent);
            Assert.That(game.Queen.CanBuy(wings), Is.False);
            Assert.That(game.Queen.TryBuy(wings), Is.False);
            Assert.That(game.Queen.AbilityLevel(wings), Is.Zero);
        }

        [Test]
        public void AbilityStopsAtItsMaxLevel()
        {
            int pollen = IndexOf(QueenEffect.BloomPerVisitPercent);
            game.Prestige.GrantJelly(BigNumber.Create(1, 9));
            int max = game.Queen.Abilities[pollen].MaxLevel;
            for (int i = 0; i < max; i++)
                Assert.That(game.Queen.TryBuy(pollen), $"Level {i + 1} failed.");
            Assert.That(game.Queen.IsMaxed(pollen));
            Assert.That(game.Queen.TryBuy(pollen), Is.False);
            Assert.That(game.Queen.AbilityLevel(pollen), Is.EqualTo(max));
        }

        [Test]
        public void Abilities_ApplyTheirEffects()
        {
            game.Prestige.GrantJelly(BigNumber.Create(1, 6));
            float speedBefore = game.Bees.SpeedMultiplier;
            double capBefore = game.OfflineCapHours;

            QueenAbility wings = game.Queen.Abilities[IndexOf(QueenEffect.FlightSpeedPercent)];
            Assert.That(game.Queen.TryBuy(IndexOf(QueenEffect.FlightSpeedPercent)));
            Assert.That(game.Bees.SpeedMultiplier, Is.EqualTo(speedBefore * (1 + wings.EffectPerLevel / 100f)).Within(1e-5));

            QueenAbility pollen = game.Queen.Abilities[IndexOf(QueenEffect.BloomPerVisitPercent)];
            Assert.That(game.Queen.TryBuy(IndexOf(QueenEffect.BloomPerVisitPercent)));
            Assert.That(game.Bloom.BloomPerVisitMultiplier, Is.EqualTo(1 + pollen.EffectPerLevel / 100f).Within(1e-5));

            QueenAbility memory = game.Queen.Abilities[IndexOf(QueenEffect.OfflineCapHours)];
            Assert.That(game.Queen.TryBuy(IndexOf(QueenEffect.OfflineCapHours)));
            Assert.That(game.OfflineCapHours, Is.EqualTo(capBefore + memory.EffectPerLevel).Within(1e-5));

            int bees = game.Bees.Count;
            Assert.That(game.Queen.TryBuy(IndexOf(QueenEffect.StartingWorkers)));
            Assert.That(game.Bees.Count, Is.EqualTo(bees + 1), "Royal Brood adds its Worker right away.");
        }

        [Test]
        public void SpeedUpgrade_StacksWithRoyalWings()
        {
            game.Prestige.GrantJelly(BigNumber.FromDouble(100));
            Assert.That(game.Queen.TryBuy(IndexOf(QueenEffect.FlightSpeedPercent)));
            game.Economy.Grant(BigNumber.FromDouble(1000));
            Assert.That(game.Upgrades.TryBuySpeed());
            Assert.That(game.Bees.SpeedMultiplier,
                Is.EqualTo(game.Upgrades.SpeedMultiplier * game.Upgrades.QueenSpeedMultiplier).Within(1e-5));
            Assert.That(game.Upgrades.QueenSpeedMultiplier, Is.GreaterThan(1f));
        }

        [UnityTest]
        public IEnumerator QueenMove_KeepsAbilitiesAndLevel_AndBroodStartsTheGarden()
        {
            game.Prestige.GrantJelly(BigNumber.FromDouble(100));
            int brood = IndexOf(QueenEffect.StartingWorkers);
            int wings = IndexOf(QueenEffect.FlightSpeedPercent);
            Assert.That(game.Queen.TryBuy(brood));
            Assert.That(game.Queen.TryBuy(wings));
            int level = game.Queen.Level;
            float queenSpeed = game.Upgrades.QueenSpeedMultiplier;

            BloomTo(game.Prestige.MoveUnlockBloom);
            game.Economy.Grant(game.Prestige.MoveCost);
            Assert.That(game.TryMoveQueen());
            yield return null;

            Assert.That(game.Queen.AbilityLevel(brood), Is.EqualTo(1));
            Assert.That(game.Queen.AbilityLevel(wings), Is.EqualTo(1));
            Assert.That(game.Queen.Level, Is.GreaterThanOrEqualTo(level));
            Assert.That(game.Upgrades.QueenSpeedMultiplier, Is.EqualTo(queenSpeed));
            Assert.That(game.Bees.SpeedMultiplier, Is.EqualTo(queenSpeed).Within(1e-5), "Speed upgrade reset, Royal Wings kept.");
            Assert.That(game.Bees.Count, Is.EqualTo(game.StartingWorkers));
            Assert.That(game.StartingWorkers, Is.EqualTo(2), "One base Worker plus one from Royal Brood.");
        }

        [UnityTest]
        public IEnumerator SaveAndReload_KeepsAbilityLevels()
        {
            game.Prestige.GrantJelly(BigNumber.FromDouble(100));
            int pollen = IndexOf(QueenEffect.BloomPerVisitPercent);
            Assert.That(game.Queen.TryBuy(pollen));
            Assert.That(game.Queen.TryBuy(pollen));
            float bloomFactor = game.Bloom.BloomPerVisitMultiplier;
            int level = game.Queen.Level;
            BigNumber jelly = game.Prestige.RoyalJelly;
            game.SaveNow();

            yield return LoadMain();

            Assert.That(game.Queen.AbilityLevel(pollen), Is.EqualTo(2));
            Assert.That(game.Queen.Level, Is.EqualTo(level));
            Assert.That(game.Prestige.RoyalJelly, Is.EqualTo(jelly));
            Assert.That(game.Bloom.BloomPerVisitMultiplier, Is.EqualTo(bloomFactor).Within(1e-6));
            Assert.That(game.Economy.QueenMultiplier, Is.EqualTo(game.Queen.HoneyMultiplier));
        }

        [UnityTest]
        public IEnumerator Panel_HasOneRowPerAbility_AndRowsBuy()
        {
            var panel = Object.FindAnyObjectByType<QueenPanelView>();
            Assert.That(panel.AbilityRows.Count, Is.EqualTo(game.Queen.AbilityCount));

            panel.Open();
            yield return null;
            QueenAbilityRowView row = panel.AbilityRows[0];
            Assert.That(row.CanBuy, Is.False, "No jelly yet.");
            row.Press();
            Assert.That(game.Queen.AbilityLevel(row.AbilityIndex), Is.Zero);

            game.Prestige.GrantJelly(BigNumber.FromDouble(100));
            yield return new WaitForSeconds(0.3f);
            Assert.That(row.CanBuy);
            row.Press();
            Assert.That(game.Queen.AbilityLevel(row.AbilityIndex), Is.EqualTo(1));
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
    }
}
