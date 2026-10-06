using System;
using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class GoalTests
    {
        static GoalProgress Sample() => new GoalProgress
        {
            FlowersShaken = 12,
            Bees = 3,
            SpeedLevel = 4,
            HoneyValueLevel = 5,
            FlowersBloomed = 6,
            BeesEvolved = 7,
            BloomPercent = 64,
            QueenMoves = 2,
            AbilityLevels = 9,
            GardensCompleted = 1,
        };

        [TestCase(GoalKind.ShakeFlowers, 12)]
        [TestCase(GoalKind.OwnBees, 3)]
        [TestCase(GoalKind.SpeedLevel, 4)]
        [TestCase(GoalKind.HoneyValueLevel, 5)]
        [TestCase(GoalKind.BloomFlowers, 6)]
        [TestCase(GoalKind.EvolveBees, 7)]
        [TestCase(GoalKind.GardenBloomPercent, 64)]
        [TestCase(GoalKind.MoveQueen, 2)]
        [TestCase(GoalKind.AbilityLevels, 9)]
        [TestCase(GoalKind.CompleteGardens, 1)]
        public void Current_ReadsTheRightCount(GoalKind kind, long expected)
        {
            GoalProgress progress = Sample();
            Assert.That(GoalMath.Current(kind, progress), Is.EqualTo(expected));
        }

        [Test]
        public void EveryKind_IsRead()
        {
            GoalProgress progress = Sample();
            foreach (GoalKind kind in Enum.GetValues(typeof(GoalKind)))
                Assert.That(GoalMath.Current(kind, progress), Is.GreaterThan(0), kind.ToString());
        }

        [Test]
        public void Done_AtTheTarget()
        {
            GoalProgress progress = Sample();
            Assert.That(GoalMath.IsDone(GoalKind.OwnBees, 3, progress), Is.True);
            Assert.That(GoalMath.IsDone(GoalKind.OwnBees, 4, progress), Is.False);
        }

        [Test]
        public void Shown_StopsAtTheTarget()
        {
            GoalProgress progress = Sample();
            Assert.That(GoalMath.Shown(GoalKind.ShakeFlowers, 10, progress), Is.EqualTo(10));
            Assert.That(GoalMath.Shown(GoalKind.ShakeFlowers, 20, progress), Is.EqualTo(12));
        }

        [Test]
        public void Since_CountsFromTheBaseline_NeverBelowZero()
        {
            GoalProgress progress = Sample();
            Assert.That(GoalMath.Since(GoalKind.ShakeFlowers, 10, progress), Is.EqualTo(2));
            Assert.That(GoalMath.Since(GoalKind.ShakeFlowers, 0, progress), Is.EqualTo(12));
            Assert.That(GoalMath.Since(GoalKind.ShakeFlowers, 50, progress), Is.Zero, "A baseline above the count reads as nothing done.");
        }

        [Test]
        public void Reward_IsSecondsOfIncome()
        {
            BigNumber reward = RewardMath.IncomeSeconds(BigNumber.FromDouble(50), 30, 10);
            Assert.That(reward.ToDouble(), Is.EqualTo(1500).Within(1e-6));
        }

        [Test]
        public void Reward_NeverBelowTheMinimum()
        {
            Assert.That(RewardMath.IncomeSeconds(BigNumber.FromDouble(0.1), 30, 25).ToDouble(), Is.EqualTo(25).Within(1e-9));
            Assert.That(RewardMath.IncomeSeconds(BigNumber.Zero, 30, 25).ToDouble(), Is.EqualTo(25).Within(1e-9));
            Assert.That(RewardMath.IncomeSeconds(BigNumber.FromDouble(-5), 30, 0).ToDouble(), Is.Zero);
        }

        [Test]
        public void Reward_KeepsHugeNumbers()
        {
            BigNumber rate = BigNumber.Create(1.5, 40);
            BigNumber reward = RewardMath.IncomeSeconds(rate, 20, 10);
            Assert.That(reward.Exponent, Is.EqualTo(41));
            Assert.That(reward.Mantissa, Is.EqualTo(3).Within(1e-9));
        }

        [Test]
        public void Visitor_WaitsOnlyThroughCalmTime()
        {
            var timer = new VisitorTimer();
            timer.Restart(0.5, 100, 200);
            Assert.That(timer.Remaining, Is.EqualTo(150).Within(1e-9));

            Assert.That(timer.Tick(500, false), Is.False, "Not calm: the wait does not move.");
            Assert.That(timer.Remaining, Is.EqualTo(150).Within(1e-9));
            Assert.That(timer.Tick(100, true), Is.False);
            Assert.That(timer.Tick(50, true), Is.True);
            Assert.That(timer.Tick(1, false), Is.False, "Due, but never behind a panel.");
            Assert.That(timer.Tick(0, true), Is.True);
        }

        [Test]
        public void Visitor_RandomIsClamped()
        {
            var timer = new VisitorTimer();
            timer.Restart(2, 100, 200);
            Assert.That(timer.Remaining, Is.EqualTo(200).Within(1e-9));
            timer.Restart(-1, 100, 200);
            Assert.That(timer.Remaining, Is.EqualTo(100).Within(1e-9));
            timer.Restart(0.5, 100, 50);
            Assert.That(timer.Remaining, Is.EqualTo(100).Within(1e-9), "A max below the min waits the min.");
        }
    }
}
