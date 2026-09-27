using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class BloomMathTests
    {
        [Test]
        public void AddVisit_AddsScaledProgress()
        {
            Assert.That(BloomMath.AddVisit(0.2f, 0.1f, 1f), Is.EqualTo(0.3f).Within(1e-6f));
            Assert.That(BloomMath.AddVisit(0.2f, 0.1f, 1.5f), Is.EqualTo(0.35f).Within(1e-6f));
        }

        [Test]
        public void AddVisit_ClampsAtOne()
        {
            Assert.That(BloomMath.AddVisit(0.95f, 0.12f, 1f), Is.EqualTo(1f));
            Assert.That(BloomMath.AddVisit(1f, 0.12f, 1f), Is.EqualTo(1f));
        }

        [Test]
        public void AddVisit_NeverDecreases()
        {
            Assert.That(BloomMath.AddVisit(0.5f, -0.2f, 1f), Is.EqualTo(0.5f));
            Assert.That(BloomMath.AddVisit(0.5f, 0.2f, -1f), Is.EqualTo(0.5f));
        }

        [Test]
        public void AddVisit_DaisyBloomsAfterNineVisits()
        {
            float bloom = 0f;
            int visits = 0;
            while (bloom < 1f)
            {
                bloom = BloomMath.AddVisit(bloom, 0.12f, 1f);
                visits++;
            }
            Assert.That(visits, Is.EqualTo(9));
        }

        [Test]
        public void GardenFraction_CountsAllSlots()
        {
            Assert.That(BloomMath.GardenFraction(0, 16), Is.EqualTo(0f));
            Assert.That(BloomMath.GardenFraction(4, 16), Is.EqualTo(0.25f));
            Assert.That(BloomMath.GardenFraction(16, 16), Is.EqualTo(1f));
            Assert.That(BloomMath.GardenFraction(3, 0), Is.EqualTo(0f));
        }

        [Test]
        public void DisplayPercent_OnlyHundredWhenComplete()
        {
            Assert.That(BloomMath.DisplayPercent(0, 16), Is.EqualTo(0));
            Assert.That(BloomMath.DisplayPercent(1, 16), Is.EqualTo(6));
            Assert.That(BloomMath.DisplayPercent(31, 32), Is.EqualTo(96));
            Assert.That(BloomMath.DisplayPercent(199, 200), Is.EqualTo(99));
            Assert.That(BloomMath.DisplayPercent(16, 16), Is.EqualTo(100));
        }

        [Test]
        public void Influence_FullAtFlowerZeroAtRadius()
        {
            Assert.That(BloomMath.Influence(1f, 0f, 2.5f), Is.EqualTo(1f));
            Assert.That(BloomMath.Influence(1f, 2.5f * 2.5f, 2.5f), Is.EqualTo(0f));
            Assert.That(BloomMath.Influence(1f, 9f, 2.5f), Is.EqualTo(0f));
        }

        [Test]
        public void Influence_ScalesWithBloomAndFallsOff()
        {
            float near = BloomMath.Influence(1f, 0.5f * 0.5f, 2.5f);
            float far = BloomMath.Influence(1f, 2f * 2f, 2.5f);
            Assert.That(near, Is.GreaterThan(far));
            Assert.That(BloomMath.Influence(0.5f, 0.5f * 0.5f, 2.5f), Is.EqualTo(near * 0.5f).Within(1e-6f));
            // Halfway out, smoothstep gives exactly one half.
            Assert.That(BloomMath.Influence(1f, 1.25f * 1.25f, 2.5f), Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void Influence_ZeroWithoutBloomOrRadius()
        {
            Assert.That(BloomMath.Influence(0f, 0f, 2.5f), Is.EqualTo(0f));
            Assert.That(BloomMath.Influence(1f, 0f, 0f), Is.EqualTo(0f));
        }
    }
}
