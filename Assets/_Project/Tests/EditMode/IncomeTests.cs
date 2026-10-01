using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class IncomeMathTests
    {
        [Test]
        public void BeeNectar_IsCapacityPerRoundTrip()
        {
            // 10 units there and back at 2.5/s = 4 s, + 1 s collect + 0.25 s deposit = 5.25 s per 2 units.
            Assert.That(IncomeMath.BeeNectarPerSecond(2, 2.5, 10, 1, 0.25), Is.EqualTo(2 / 5.25).Within(1e-12));
        }

        [TestCase(0.0, 2.5)]
        [TestCase(2.0, 0.0)]
        public void BeeNectar_WithoutCapacityOrSpeed_IsZero(double capacity, double speed)
        {
            Assert.That(IncomeMath.BeeNectarPerSecond(capacity, speed, 10, 1, 0.25), Is.Zero);
        }

        [Test]
        public void Honey_BeesAreTheLimit()
        {
            BigNumber rate = IncomeMath.HoneyPerSecond(0.4, 3.6, 1.5, BigNumber.FromDouble(2));
            Assert.That(rate.ToDouble(), Is.EqualTo(0.4 * 1.5 * 2).Within(1e-12));
        }

        [Test]
        public void Honey_FlowersAreTheLimit()
        {
            BigNumber rate = IncomeMath.HoneyPerSecond(50, 3.6, 1, BigNumber.One);
            Assert.That(rate.ToDouble(), Is.EqualTo(3.6).Within(1e-12));
        }

        [Test]
        public void Honey_NoFlowersOrBees_IsZero()
        {
            Assert.That(IncomeMath.HoneyPerSecond(0, 3, 1, BigNumber.One).IsZero);
            Assert.That(IncomeMath.HoneyPerSecond(3, 0, 1, BigNumber.One).IsZero);
            Assert.That(IncomeMath.HoneyPerSecond(3, 3, 0, BigNumber.One).IsZero);
        }

        [Test]
        public void Honey_HugeMultiplier_StaysBigNumber()
        {
            BigNumber rate = IncomeMath.HoneyPerSecond(2, 2, 1, BigNumber.Create(1, 400));
            Assert.That(rate.Exponent, Is.EqualTo(400));
            Assert.That(rate.Mantissa, Is.EqualTo(2).Within(1e-12));
        }
    }

    public class TimeFormatTests
    {
        [TestCase(0.0, "0s")]
        [TestCase(-5.0, "0s")]
        [TestCase(45.9, "45s")]
        [TestCase(60.0, "1m")]
        [TestCase(754.0, "12m 34s")]
        [TestCase(3600.0, "1h")]
        [TestCase(7500.0, "2h 5m")]
        [TestCase(7259.0, "2h")]
        [TestCase(90000.0, "25h")]
        public void Duration_TwoLargestUnits(double seconds, string expected)
        {
            Assert.That(TimeFormat.Duration(seconds), Is.EqualTo(expected));
        }

        [TestCase(3725.0, "{0}h {1}m", 1L, 2L)]
        [TestCase(7200.0, "{0}h", 2L, 0L)]
        [TestCase(125.0, "{0}m {1}s", 2L, 5L)]
        [TestCase(45.9, "{0}s", 45L, 0L)]
        public void DurationSplit_ReturnsFormatAndUnits(double seconds, string format, long first, long second)
        {
            Assert.That(TimeFormat.Split(seconds, out long a, out long b), Is.EqualTo(format));
            Assert.That(a, Is.EqualTo(first));
            Assert.That(b, Is.EqualTo(second));
        }
    }
}
