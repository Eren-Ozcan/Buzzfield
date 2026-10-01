using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using Buzzfield.Core;
using Buzzfield.Flowers;
using Buzzfield.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// Balance pass: a bot plays the real Main scene from a new game through the three
    /// authored gardens and two loops of the last one, and logs when each milestone of the
    /// design doc's starting targets is reached. Game time runs in fixed steps as fast as the
    /// machine allows (frame rate uncapped), so a 90-minute session takes a few minutes.
    /// Explicit: run it on its own with -testFilter BalanceReportTests; set BZ_BALANCE_OUT to
    /// a file to save the report.
    /// </summary>
    /// <remarks>
    /// The bot is an attentive active player without ads or purchases: it shakes every
    /// flower the moment it has pollen again, evolves when it can, otherwise buys the
    /// cheapest affordable upgrade, spends Royal Jelly on the cheapest ability, and moves
    /// the Queen once the garden is complete or no flower has grown for <see cref="StallSeconds"/> after the gate
    /// opened. From then on it saves its honey for the move.
    /// </remarks>
    [Explicit("Long balance run; start it on its own.")]
    public class BalanceReportTests
    {
        const float StepSeconds = 0.1f;
        const float DecisionSeconds = 0.5f;
        const double StallSeconds = 120;
        const double MaxGardenSeconds = 3600;
        const double StatusSeconds = 300;
        const int MovesToPlay = 5;

        GameManager game;
        string outPath;
        int frameRate;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            TestSave.Clear();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
            frameRate = Application.targetFrameRate;
            Application.targetFrameRate = -1;
        }

        [UnityTearDown]
        public IEnumerator RestoreTime()
        {
            Time.captureDeltaTime = 0f;
            Application.targetFrameRate = frameRate;
            yield return null;
        }

        [UnityTest, Timeout(3_600_000)]
        public IEnumerator PlayThreeGardensAndTwoLoops()
        {
            outPath = Environment.GetEnvironmentVariable("BZ_BALANCE_OUT");
            var report = new StringBuilder();
            report.AppendLine("Buzzfield balance run (bot, no ads, no purchases)");
            report.AppendLine("time      event");
            Flush(report);

            Time.captureDeltaTime = StepSeconds;
            double start = Time.timeAsDouble;
            double gardenStart = 0;
            double nextStatus = StatusSeconds;
            double lastBloomChange = 0;
            float lastProgress = 0f;
            bool firstBee = false, firstEvolve = false, firstGolden = false, unlockedLogged = false, completeLogged = false;
            float decisionTimer = 0f;
            int moves = 0;

            while (moves < MovesToPlay)
            {
                yield return null;
                double now = Time.timeAsDouble - start;
                if (now >= nextStatus)
                {
                    nextStatus += StatusSeconds;
                    Status(report, now);
                }
                if (now - gardenStart >= MaxGardenSeconds)
                {
                    Status(report, now);
                    Log(report, now, $"garden {game.Prestige.GardenIndex + 1}: STUCK after {Clock(now - gardenStart)}");
                    Assert.Fail($"Stuck in garden {game.Prestige.GardenIndex + 1}.\n{report}");
                }

                float progress = BloomProgress();
                if (progress > lastProgress + 1e-4f)
                {
                    lastProgress = progress;
                    lastBloomChange = now;
                }
                if (!unlockedLogged && game.Prestige.IsUnlocked(game.Bloom.Fraction))
                {
                    unlockedLogged = true;
                    Log(report, now, $"garden {game.Prestige.GardenIndex + 1}: move unlocked ({game.Bloom.Percent}% bloom) after {Clock(now - gardenStart)}");
                }
                if (!completeLogged && game.Bloom.IsComplete)
                {
                    completeLogged = true;
                    Log(report, now, $"garden {game.Prestige.GardenIndex + 1}: complete after {Clock(now - gardenStart)}");
                }

                game.ShakeAllFlowers();
                decisionTimer -= StepSeconds;
                if (decisionTimer > 0f)
                    continue;
                decisionTimer = DecisionSeconds;

                bool canMove = game.Prestige.CanMove(game.Bloom.Fraction, game.Economy.Honey);
                if (canMove && (game.Bloom.IsComplete || now - lastBloomChange > StallSeconds))
                {
                    BigNumber jelly = game.Prestige.PreviewJelly(game.Economy.RunHoneyEarned, game.Bloom.Fraction);
                    int bees = game.Bees.Count;
                    int percent = game.Bloom.Percent;
                    Assert.That(game.TryMoveQueen());
                    moves++;
                    Log(report, now, $"garden {game.Prestige.GardenIndex}: MOVE after {Clock(now - gardenStart)} at {percent}% bloom, " +
                        $"{bees} bees, +{NumberFormat.Abbreviate(jelly)} jelly, Queen Lv {game.Queen.Level} (x{game.Queen.HoneyMultiplier.ToString("0.##", CultureInfo.InvariantCulture)})");
                    SpendJelly(report, now);
                    gardenStart = now;
                    lastBloomChange = now;
                    lastProgress = 0f;
                    unlockedLogged = completeLogged = false;
                    continue;
                }

                if (game.Upgrades.CanEvolve)
                {
                    int target = game.Upgrades.EvolveSourceTier + 1;
                    game.Upgrades.TryEvolve();
                    if (!firstEvolve)
                    {
                        firstEvolve = true;
                        Log(report, now, "first Evolve");
                    }
                    if (!firstGolden && target == 2)
                    {
                        firstGolden = true;
                        Log(report, now, "first Golden bee");
                    }
                    continue;
                }

                // Save up for the move once the gate is open and the garden is done or has stalled.
                if (game.Prestige.IsUnlocked(game.Bloom.Fraction) && (game.Bloom.IsComplete || now - lastBloomChange > StallSeconds))
                    continue;

                if (BuyCheapest() && !firstBee && game.Upgrades.BeesBought > 0)
                {
                    firstBee = true;
                    Log(report, now, "first Add Bee");
                }
            }

            Log(report, Time.timeAsDouble - start, $"done: {game.Stats.gardensCompleted} gardens completed, " +
                $"{NumberFormat.Abbreviate(game.Economy.LifetimeHoneyEarned)} lifetime honey, " +
                $"{NumberFormat.Abbreviate(game.Prestige.LifetimeJelly)} lifetime jelly");

            Debug.Log(report.ToString());
        }

        /// <summary>
        /// Logs where the garden stands: flowers by bloom state (untouched, growing, bloomed),
        /// how many can take a bee right now, bees and honey.
        /// </summary>
        void Status(StringBuilder report, double now)
        {
            int active = 0, untouched = 0, growing = 0, bloomed = 0, available = 0;
            var flowers = game.Flowers.Flowers;
            for (int i = 0; i < flowers.Count; i++)
            {
                Flower flower = flowers[i];
                if (flower.IsBloomed) bloomed++;
                if (!flower.IsActive) continue;
                active++;
                if (flower.IsAvailable) available++;
                if (flower.Bloom <= 0f) untouched++;
                else if (!flower.IsBloomed) growing++;
            }
            Log(report, now, $"  status g{game.Prestige.GardenIndex + 1}: {game.Bloom.Percent}% bloom ({bloomed}/{flowers.Count}), " +
                $"active {active} (untouched {untouched}, growing {growing}, available {available}), " +
                $"{game.Bees.Count} bees, honey {NumberFormat.Abbreviate(game.Economy.Honey)}");
        }

        /// <summary>Sum of every flower's bloom progress: grows while any flower is still growing.</summary>
        float BloomProgress()
        {
            float sum = 0f;
            var flowers = game.Flowers.Flowers;
            for (int i = 0; i < flowers.Count; i++)
                sum += flowers[i].Bloom;
            return sum;
        }

        /// <summary>Buys the cheapest affordable bottom-bar upgrade; true when something was bought.</summary>
        bool BuyCheapest()
        {
            int choice = -1;
            BigNumber best = BigNumber.Zero;
            Consider(0, game.Upgrades.CanAddBee, game.Upgrades.AddBeeCost, ref choice, ref best);
            Consider(1, game.Upgrades.CanBuySpeed, game.Upgrades.SpeedCost, ref choice, ref best);
            Consider(2, game.Upgrades.CanBuyHoneyValue, game.Upgrades.HoneyValueCost, ref choice, ref best);
            switch (choice)
            {
                case 0: return game.Upgrades.TryAddBee();
                case 1: return game.Upgrades.TryBuySpeed();
                case 2: return game.Upgrades.TryBuyHoneyValue();
                default: return false;
            }
        }

        static void Consider(int id, bool can, BigNumber cost, ref int choice, ref BigNumber best)
        {
            if (!can || (choice >= 0 && cost >= best))
                return;
            choice = id;
            best = cost;
        }

        /// <summary>Spends Royal Jelly on the cheapest ability until nothing is affordable.</summary>
        void SpendJelly(StringBuilder report, double now)
        {
            while (true)
            {
                int choice = -1;
                BigNumber best = BigNumber.Zero;
                for (int i = 0; i < game.Queen.AbilityCount; i++)
                    Consider(i, game.Queen.CanBuy(i), game.Queen.AbilityCost(i), ref choice, ref best);
                if (choice < 0 || !game.Queen.TryBuy(choice))
                    return;
                Log(report, now, $"  bought {game.Queen.Abilities[choice].Id} Lv {game.Queen.AbilityLevel(choice)} for {NumberFormat.Abbreviate(best)} jelly");
            }
        }

        void Log(StringBuilder report, double seconds, string message)
        {
            report.Append(Clock(seconds).PadRight(10)).AppendLine(message);
            Flush(report);
        }

        /// <summary>Rewrites the report file after every line so a stuck or killed run still leaves it.</summary>
        void Flush(StringBuilder report)
        {
            if (!string.IsNullOrEmpty(outPath))
                File.WriteAllText(outPath, report.ToString());
        }

        static string Clock(double seconds) => $"{(int)(seconds / 60)}:{(int)(seconds % 60):00}";
    }
}
