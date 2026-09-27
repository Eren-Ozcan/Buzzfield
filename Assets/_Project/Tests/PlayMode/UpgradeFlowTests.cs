using System.Collections;
using Buzzfield.Bees;
using Buzzfield.Core;
using Buzzfield.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>Bottom-bar upgrades against the real Main scene.</summary>
    public class UpgradeFlowTests
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

        [UnityTearDown]
        public IEnumerator ResetTime()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        [Test]
        public void AddBee_Unaffordable_DoesNothing()
        {
            Assert.That(game.Upgrades.CanAddBee, Is.False);
            Assert.That(game.Upgrades.TryAddBee(), Is.False);
            Assert.That(game.Bees.Count, Is.EqualTo(1));
        }

        [Test]
        public void AddBee_SpendsAndSpawns_CostRises()
        {
            BigNumber first = game.Upgrades.AddBeeCost;
            game.Economy.Grant(first);

            Assert.That(game.Upgrades.TryAddBee(), Is.True);
            Assert.That(game.Bees.Count, Is.EqualTo(2));
            Assert.That(game.Economy.Honey.IsZero);
            Assert.That(game.Upgrades.AddBeeCost, Is.GreaterThan(first));
        }

        [Test]
        public void AddBee_AtCap_IsUnavailable()
        {
            while (game.Bees.Spawn(0) != null) { }
            game.Economy.Grant(BigNumber.Create(1, 30));

            Assert.That(game.Bees.IsAtCap);
            Assert.That(game.Upgrades.AddBeeAvailable, Is.False);
            Assert.That(game.Upgrades.TryAddBee(), Is.False);
        }

        [Test]
        public void Speed_AppliesMultiplicativeBonus()
        {
            game.Economy.Grant(BigNumber.Create(1, 6));
            Assert.That(game.Upgrades.TryBuySpeed(), Is.True);
            Assert.That(game.Upgrades.TryBuySpeed(), Is.True);

            Assert.That(game.Upgrades.SpeedLevel, Is.EqualTo(2));
            Assert.That(game.Bees.SpeedMultiplier, Is.EqualTo(1.21f).Within(1e-5f));
        }

        [Test]
        public void HoneyValue_RaisesMultiplier()
        {
            game.Economy.Grant(BigNumber.Create(1, 6));
            Assert.That(game.Upgrades.TryBuyHoneyValue(), Is.True);
            Assert.That(game.Economy.HoneyValueMultiplier.ToDouble(), Is.EqualTo(1.15).Within(1e-6));
        }

        [Test]
        public void Evolve_NeedsThreeBees()
        {
            game.Economy.Grant(BigNumber.Create(1, 6));
            game.Bees.Spawn(0);
            Assert.That(game.Upgrades.EvolveAvailable, Is.False);
            Assert.That(game.Upgrades.TryEvolve(), Is.False);
        }

        [UnityTest]
        public IEnumerator Evolve_MergesThreeWorkersIntoForager()
        {
            game.Bees.Spawn(0);
            game.Bees.Spawn(0);
            game.Bees.Spawn(0);
            BigNumber cost = game.Upgrades.EvolveCost;
            game.Economy.Grant(cost);

            Bee evolved = null;
            game.Bees.OnBeeEvolved += bee => evolved = bee;
            Assert.That(game.Upgrades.TryEvolve(), Is.True);

            // The three bees leave the counts at once; the fourth Worker stays.
            Assert.That(game.Bees.TierCounts[0], Is.EqualTo(1));
            Assert.That(game.Economy.Honey.ToDouble(), Is.LessThan(1e-6));
            Assert.That(game.Upgrades.EvolveAvailable, Is.False, "Merging bees must not count for another Evolve.");

            Time.timeScale = 4f;
            float end = Time.time + 20f;
            while (evolved == null && Time.time < end)
                yield return null;

            Assert.That(evolved, Is.Not.Null, "Merge never finished.");
            Assert.That(evolved.TierIndex, Is.EqualTo(1));
            Assert.That(game.Bees.Count, Is.EqualTo(2));
            Assert.That(game.Bees.TierCounts[1], Is.EqualTo(1));
            Assert.That(game.Bees.IsMerging, Is.False);
        }

        [UnityTest]
        public IEnumerator Evolve_CreditsCarriedNectar()
        {
            game.Bees.Spawn(0);
            game.Bees.Spawn(0);
            Time.timeScale = 4f;
            // Wait until at least one bee carries nectar back.
            float end = Time.time + 20f;
            bool carrying = false;
            while (!carrying && Time.time < end)
            {
                foreach (Bee bee in game.Bees.Bees)
                    carrying |= bee.Carried > 0;
                if (!carrying)
                    yield return null;
            }
            Assert.That(carrying, "No bee picked up nectar.");

            BigNumber before = game.Economy.RunHoneyEarned;
            game.Economy.Grant(game.Upgrades.EvolveCost);
            Assert.That(game.Upgrades.TryEvolve(), Is.True);
            Assert.That(game.Economy.RunHoneyEarned, Is.GreaterThan(before));
            foreach (Bee bee in game.Bees.Bees)
            {
                if (bee.State == BeeState.Merging)
                    Assert.That(bee.Carried, Is.Zero);
            }
        }
    }
}
