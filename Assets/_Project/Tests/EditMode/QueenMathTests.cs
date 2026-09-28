using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class QueenMathTests
    {
        static readonly double[] Levels = { 1, 5, 15, 40 };

        [Test]
        public void Level_IsZeroWithoutJelly()
        {
            Assert.That(QueenMath.Level(BigNumber.Zero, Levels), Is.Zero);
            Assert.That(QueenMath.Level(BigNumber.FromDouble(-3), Levels), Is.Zero);
        }

        [TestCase(1, 1)]
        [TestCase(4, 1)]
        [TestCase(5, 2)]
        [TestCase(14.9, 2)]
        [TestCase(15, 3)]
        [TestCase(39, 3)]
        [TestCase(40, 4)]
        public void Level_CountsReachedThresholds(double jelly, int expected)
        {
            Assert.That(QueenMath.Level(BigNumber.FromDouble(jelly), Levels), Is.EqualTo(expected));
        }

        [Test]
        public void Level_StopsAtTheTopLevel()
        {
            Assert.That(QueenMath.Level(BigNumber.FromDouble(1e9), Levels), Is.EqualTo(4));
            Assert.That(QueenMath.Level(BigNumber.Create(1, 500), Levels), Is.EqualTo(4), "Past a double's range.");
        }

        [Test]
        public void Level_ForgivesCurveDriftJustBelowAWholeNumber()
        {
            Assert.That(QueenMath.Level(BigNumber.FromDouble(5), new[] { 1.0, 5.0000000001 }), Is.EqualTo(2));
        }

        [Test]
        public void NextThreshold_IsTheNextLevelOrMinusOneAtTop()
        {
            Assert.That(QueenMath.NextThreshold(0, Levels), Is.EqualTo(1));
            Assert.That(QueenMath.NextThreshold(2, Levels), Is.EqualTo(15));
            Assert.That(QueenMath.NextThreshold(4, Levels), Is.EqualTo(-1));
        }

        [Test]
        public void Thresholds_ReadTheCurveAtEachLevel()
        {
            double[] thresholds = QueenMath.Thresholds(level => level * level, 4);
            Assert.That(thresholds, Is.EqualTo(new double[] { 1, 4, 9, 16 }));
        }

        [Test]
        public void Thresholds_NeverFall()
        {
            double[] thresholds = QueenMath.Thresholds(level => level == 3 ? 2 : level == 4 ? double.NaN : level * 10, 5);
            Assert.That(thresholds, Is.EqualTo(new double[] { 10, 20, 20, 20, 50 }));
        }

        [Test]
        public void Thresholds_EmptyForNoLevels()
        {
            Assert.That(QueenMath.Thresholds(level => level, 0), Is.Empty);
            Assert.That(QueenMath.Level(BigNumber.FromDouble(100), QueenMath.Thresholds(level => level, 0)), Is.Zero);
        }

        [Test]
        public void HoneyMultiplier_AddsTheBonusPerLevel()
        {
            Assert.That(QueenMath.HoneyMultiplier(0, 0.1), Is.EqualTo(1));
            Assert.That(QueenMath.HoneyMultiplier(3, 0.1), Is.EqualTo(1.3).Within(1e-12));
            Assert.That(QueenMath.HoneyMultiplier(-2, 0.1), Is.EqualTo(1), "A damaged level gives no bonus.");
            Assert.That(QueenMath.HoneyMultiplier(5, -1), Is.EqualTo(1), "A negative bonus is ignored.");
        }

        [Test]
        public void AbilityEffect_AddsPerLevel()
        {
            Assert.That(QueenMath.AbilityEffect(5, 0), Is.Zero);
            Assert.That(QueenMath.AbilityEffect(5, 3), Is.EqualTo(15));
            Assert.That(QueenMath.AbilityEffect(5, -1), Is.Zero);
        }

        [Test]
        public void AbilityCost_FollowsTheUpgradeCurve()
        {
            Assert.That(UpgradeMath.Cost(2, 2.5, 0).ToDouble(), Is.EqualTo(2));
            Assert.That(UpgradeMath.Cost(2, 2.5, 2).ToDouble(), Is.EqualTo(12.5).Within(1e-9));
        }

        [Test]
        public void ClampAbilityLevel_KeepsSavedLevelsInRange()
        {
            Assert.That(QueenMath.ClampAbilityLevel(-4, 5), Is.Zero);
            Assert.That(QueenMath.ClampAbilityLevel(3, 5), Is.EqualTo(3));
            Assert.That(QueenMath.ClampAbilityLevel(9, 5), Is.EqualTo(5), "A lowered max level caps old saves.");
        }

        [Test]
        public void PercentFactor_TurnsPercentIntoAFactor()
        {
            Assert.That(QueenMath.PercentFactor(0), Is.EqualTo(1));
            Assert.That(QueenMath.PercentFactor(25), Is.EqualTo(1.25));
            Assert.That(QueenMath.PercentFactor(-10), Is.EqualTo(1));
        }
    }
}
