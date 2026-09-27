using System;
using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class PrestigeMathTests
    {
        [TestCase(0.59f, 0.6f, false)]
        [TestCase(0.6f, 0.6f, true)]
        [TestCase(6f / 10f, 0.6f, true)]
        [TestCase(10f / 16f, 0.6f, true)]
        [TestCase(1f, 0.6f, true)]
        [TestCase(0f, 0f, true)]
        public void IsUnlocked_AtOrAboveThreshold(float bloom, float unlock, bool expected)
        {
            Assert.That(PrestigeMath.IsUnlocked(bloom, unlock), Is.EqualTo(expected));
        }

        [TestCase(0.5f, 0.0)]
        [TestCase(0.6f, 0.0)]
        [TestCase(0.8f, 0.25)]
        [TestCase(0.99f, 0.4875)]
        [TestCase(1f, 1.0)]
        public void BloomBonus_GrowsFromThresholdAndJumpsAtComplete(float bloom, double expected)
        {
            Assert.That(PrestigeMath.BloomBonus(bloom, 0.6f, 0.5f, 0.5f), Is.EqualTo(expected).Within(1e-5));
        }

        [Test]
        public void BloomBonus_ThresholdAtFull_OnlyCompleteBonus()
        {
            Assert.That(PrestigeMath.BloomBonus(1f, 1f, 0.5f, 0.5f), Is.EqualTo(1.0).Within(1e-9));
            Assert.That(PrestigeMath.BloomBonus(0.9f, 1f, 0.5f, 0.5f), Is.Zero);
        }

        [TestCase(25_000.0, 0.0, 5.0)]
        [TestCase(25_000.0, 0.5, 7.0)]       // floor(5 * 1.5) = 7
        [TestCase(25_000.0, 1.0, 10.0)]
        [TestCase(5_000.0, 0.0, 2.0)]        // sqrt(5) = 2.236
        [TestCase(999.0, 1.0, 0.0)]          // below jellyBase: floor(0.99) = 0
        [TestCase(1_000_000.0, 0.25, 38.0)]  // floor(31.62) = 31, * 1.25 = 38.75
        public void Jelly_FollowsFormula(double runHoney, double bloomBonus, double expected)
        {
            Assert.That(PrestigeMath.Jelly(runHoney, 1000, 1, bloomBonus).ToDouble(), Is.EqualTo(expected));
        }

        [Test]
        public void Jelly_ScaleMultipliesBeforeFloor()
        {
            // floor(2 * sqrt(5)) = floor(4.47) = 4
            Assert.That(PrestigeMath.Jelly(5_000, 1000, 2, 0).ToDouble(), Is.EqualTo(4));
        }

        [Test]
        public void Jelly_ExactSquares_DoNotLoseOneToFloatDrift()
        {
            for (int n = 1; n <= 500; n++)
                Assert.That(PrestigeMath.Jelly(1000.0 * n * n, 1000, 1, 0).ToDouble(), Is.EqualTo(n), $"n = {n}");
        }

        [Test]
        public void Jelly_NoHoneyOrBadSettings_IsZero()
        {
            Assert.That(PrestigeMath.Jelly(BigNumber.Zero, 1000, 1, 1).IsZero);
            Assert.That(PrestigeMath.Jelly(-5, 1000, 1, 1).IsZero);
            Assert.That(PrestigeMath.Jelly(5_000, 0, 1, 1).IsZero);
            Assert.That(PrestigeMath.Jelly(5_000, 1000, 0, 1).IsZero);
        }

        [Test]
        public void Jelly_BeyondDoubleRange_StaysFinite()
        {
            BigNumber honey = BigNumber.FromLog10(400);
            BigNumber jelly = PrestigeMath.Jelly(honey, 1000, 1, 0.5);
            // log10(sqrt(1e400 / 1e3) * 1.5) = 198.5 + log10(1.5)
            Assert.That(jelly.Log10(), Is.EqualTo(198.5 + Math.Log10(1.5)).Within(1e-6));
        }

        [Test]
        public void Jelly_GrowsWithRunHoney()
        {
            BigNumber previous = BigNumber.Zero;
            for (int k = 3; k < 60; k++)
            {
                BigNumber jelly = PrestigeMath.Jelly(BigNumber.FromLog10(k), 1000, 1, 0);
                Assert.That(jelly, Is.GreaterThanOrEqualTo(previous));
                previous = jelly;
            }
        }

        [TestCase(0, 3, 1.0)]
        [TestCase(2, 3, 1.0)]
        [TestCase(3, 3, 1.25)]
        [TestCase(5, 3, 1.953125)]
        public void LoopMultiplier_CompoundsPastLastGarden(int gardenIndex, int authored, double expected)
        {
            Assert.That(PrestigeMath.LoopMultiplier(gardenIndex, authored, 0.25), Is.EqualTo(expected).Within(1e-12));
        }

        [TestCase(0, 3, 0)]
        [TestCase(2, 3, 2)]
        [TestCase(3, 3, 2)]
        [TestCase(40, 3, 2)]
        [TestCase(-1, 3, 0)]
        public void ConfigIndex_RepeatsLastGarden(int gardenIndex, int authored, int expected)
        {
            Assert.That(PrestigeMath.ConfigIndex(gardenIndex, authored), Is.EqualTo(expected));
        }

        [Test]
        public void NoAuthoredGardens_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PrestigeMath.ConfigIndex(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => PrestigeMath.LoopMultiplier(0, 0, 0.25));
        }

        [TestCase(4.9999999999, 5.0)]
        [TestCase(4.5, 4.0)]
        [TestCase(0.3, 0.0)]
        public void Floor_RoundsDownForgivingDrift(double value, double expected)
        {
            Assert.That(PrestigeMath.Floor(value).ToDouble(), Is.EqualTo(expected));
        }
    }
}
