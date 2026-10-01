using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class PollenMathTests
    {
        // ---- StrokeHits ----

        [Test]
        public void Stroke_PassingOverTheFlower_Hits()
        {
            Assert.That(PollenMath.StrokeHits(-2, 0, 2, 0, 0, 0.5, 0.8));
            Assert.That(PollenMath.StrokeHits(-2, 0, 2, 0, 0, 0.9, 0.8), Is.False);
        }

        [Test]
        public void Stroke_EndingShortOfTheFlower_Misses()
        {
            Assert.That(PollenMath.StrokeHits(-2, 0, -1, 0, 0, 0, 0.8), Is.False, "Closest point is the stroke end, 1 away.");
            Assert.That(PollenMath.StrokeHits(-2, 0, -0.5, 0, 0, 0, 0.8), "Ends inside the radius.");
        }

        [Test]
        public void Stroke_OfZeroLength_IsATap()
        {
            Assert.That(PollenMath.StrokeHits(0.3, 0.3, 0.3, 0.3, 0, 0, 0.8));
            Assert.That(PollenMath.StrokeHits(1, 1, 1, 1, 0, 0, 0.8), Is.False);
        }

        [Test]
        public void Stroke_DiagonalDistance_IsPerpendicular()
        {
            // The line z = x passes (1, 0) at a distance of sqrt(0.5) ~ 0.707.
            Assert.That(PollenMath.StrokeHits(-3, -3, 3, 3, 1, 0, 0.72));
            Assert.That(PollenMath.StrokeHits(-3, -3, 3, 3, 1, 0, 0.70), Is.False);
        }

        // ---- ShakeNectar ----

        [Test]
        public void ShakeNectar_SweepPaysIncomeSecondsSplitByValue()
        {
            // 12 honey/s, 3 s per sweep = 36 honey over flowers worth 6 per nectar in total, x1.
            double nectar = PollenMath.ShakeNectar(BigNumber.FromDouble(12), 3, 6, BigNumber.One, 0);
            Assert.That(nectar, Is.EqualTo(6).Within(1e-9));
            // A sweep over three flowers of value 1, 2 and 3 then pays 6 + 12 + 18 = 36 honey.
            Assert.That(nectar * (1 + 2 + 3), Is.EqualTo(36).Within(1e-9));
        }

        [Test]
        public void ShakeNectar_HoneyMultiplierDoesNotCountTwice()
        {
            // The measured rate already carries the x4; the nectar it stands for does not.
            double nectar = PollenMath.ShakeNectar(BigNumber.FromDouble(48), 3, 6, BigNumber.FromDouble(4), 0);
            Assert.That(nectar, Is.EqualTo(6).Within(1e-9));
        }

        [Test]
        public void ShakeNectar_NeverBelowTheMinimum()
        {
            Assert.That(PollenMath.ShakeNectar(BigNumber.Zero, 3, 6, BigNumber.One, 0.5), Is.EqualTo(0.5));
            Assert.That(PollenMath.ShakeNectar(BigNumber.FromDouble(0.1), 3, 6, BigNumber.One, 0.5), Is.EqualTo(0.5));
            Assert.That(PollenMath.ShakeNectar(BigNumber.FromDouble(12), 3, 0, BigNumber.One, 0.5), Is.EqualTo(0.5), "No active flower.");
            Assert.That(PollenMath.ShakeNectar(BigNumber.FromDouble(12), 0, 6, BigNumber.One, 0.5), Is.EqualTo(0.5), "Income share switched off.");
        }

        [Test]
        public void ShakeNectar_HugeRateAndMultiplier_StayFinite()
        {
            BigNumber rate = BigNumber.Create(3, 400);
            BigNumber multiplier = BigNumber.Create(1, 398);
            double nectar = PollenMath.ShakeNectar(rate, 2, 6, multiplier, 0);
            Assert.That(nectar, Is.EqualTo(100).Within(1e-6));
        }
    }
}
