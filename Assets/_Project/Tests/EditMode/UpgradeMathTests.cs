using System;
using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class UpgradeMathTests
    {
        [TestCase(4.0, 1.18, 0, 4.0)]
        [TestCase(4.0, 1.18, 1, 4.72)]
        [TestCase(10.0, 2.0, 10, 10240.0)]
        [TestCase(25.0, 1.6, 3, 102.4)]
        public void Cost_FollowsFormula(double baseCost, double multiplier, int level, double expected)
        {
            Assert.That(UpgradeMath.Cost(baseCost, multiplier, level).ToDouble(), Is.EqualTo(expected).Within(1e-9 * expected));
        }

        [Test]
        public void Cost_BeyondDoubleRange_StaysFinite()
        {
            BigNumber cost = UpgradeMath.Cost(10, 1.7, 2000);
            // log10(10 * 1.7^2000) = 1 + 2000 * log10(1.7)
            Assert.That(cost.Log10(), Is.EqualTo(1 + 2000 * Math.Log10(1.7)).Within(1e-6));
        }

        [Test]
        public void Cost_GrowsEveryLevel()
        {
            for (int level = 0; level < 200; level++)
                Assert.That(UpgradeMath.Cost(4, 1.18, level + 1), Is.GreaterThan(UpgradeMath.Cost(4, 1.18, level)));
        }

        [Test]
        public void Cost_NegativeLevel_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => UpgradeMath.Cost(1, 2, -1));
        }

        [TestCase(0.1, 0, 1.0)]
        [TestCase(0.1, 1, 1.1)]
        [TestCase(0.1, 3, 1.331)]
        [TestCase(0.15, 2, 1.3225)]
        public void Compound_IsMultiplicative(double perLevel, int level, double expected)
        {
            Assert.That(UpgradeMath.Compound(perLevel, level), Is.EqualTo(expected).Within(1e-12));
            Assert.That(UpgradeMath.CompoundBig(perLevel, level).ToDouble(), Is.EqualTo(expected).Within(1e-12));
        }

        [TestCase(0, 0, false)]
        [TestCase(1000, 0, false)]
        [TestCase(24, 25, false)]
        [TestCase(25, 25, true)]
        [TestCase(26, 25, true)]
        public void IsMaxed(int level, int maxLevel, bool expected)
        {
            Assert.That(UpgradeMath.IsMaxed(level, maxLevel), Is.EqualTo(expected));
        }
    }
}
