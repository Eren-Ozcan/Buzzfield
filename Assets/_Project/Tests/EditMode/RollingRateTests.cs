using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class RollingRateTests
    {
        const double Window = 30;

        static RollingRate Create(double start = 0) => new RollingRate(Window, 30, start);

        [Test]
        public void Empty_IsZero()
        {
            Assert.That(Create().PerSecond(10).IsZero);
        }

        [Test]
        public void FullWindow_AveragesOverWindow()
        {
            var rate = Create();
            for (int t = 0; t < 30; t++)
                rate.Add(t + 0.5, 3);
            Assert.That(rate.PerSecond(29.9).ToDouble(), Is.EqualTo(90 / 29.9).Within(1e-9));
            Assert.That(rate.PerSecond(30).ToDouble(), Is.EqualTo(87 / 30.0).Within(1e-9));
        }

        [Test]
        public void EarlySession_DividesByElapsedTime()
        {
            var rate = Create(100);
            rate.Add(104, 10);
            Assert.That(rate.PerSecond(105).ToDouble(), Is.EqualTo(2).Within(1e-9));
        }

        [Test]
        public void FirstDeposit_DividesByAtLeastOneBucket()
        {
            var rate = Create();
            rate.Add(0.1, 5);
            Assert.That(rate.PerSecond(0.1).ToDouble(), Is.EqualTo(5).Within(1e-9));
        }

        [Test]
        public void OldSamples_FallOutOfWindow()
        {
            var rate = Create();
            rate.Add(1, 100);
            rate.Add(40, 30);
            Assert.That(rate.PerSecond(40).ToDouble(), Is.EqualTo(1).Within(1e-9));
            Assert.That(rate.PerSecond(80).IsZero);
        }

        [Test]
        public void SameBucket_Accumulates()
        {
            var rate = Create();
            rate.Add(30.1, 1);
            rate.Add(30.9, 2);
            Assert.That(rate.PerSecond(31).ToDouble(), Is.EqualTo(0.1).Within(1e-9));
        }

        [Test]
        public void Reset_ForgetsSamples()
        {
            var rate = Create();
            rate.Add(5, 50);
            rate.Reset(10);
            Assert.That(rate.PerSecond(11).IsZero);
        }
    }
}
