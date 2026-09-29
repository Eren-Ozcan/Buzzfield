using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class BloomMathTests
    {
        [Test]
        public void AddNectar_AddsNectarShareScaled()
        {
            Assert.That(BloomMath.AddNectar(0.2f, 7, 70f, 1f), Is.EqualTo(0.3f).Within(1e-6f));
            Assert.That(BloomMath.AddNectar(0.2f, 7, 70f, 1.5f), Is.EqualTo(0.35f).Within(1e-6f));
        }

        [Test]
        public void AddNectar_ClampsAtOne()
        {
            Assert.That(BloomMath.AddNectar(0.95f, 12, 70f, 1f), Is.EqualTo(1f));
            Assert.That(BloomMath.AddNectar(1f, 12, 70f, 1f), Is.EqualTo(1f));
        }

        [Test]
        public void AddNectar_NeverDecreases()
        {
            Assert.That(BloomMath.AddNectar(0.5f, -2, 70f, 1f), Is.EqualTo(0.5f));
            Assert.That(BloomMath.AddNectar(0.5f, 2, 70f, -1f), Is.EqualTo(0.5f));
            Assert.That(BloomMath.AddNectar(0.5f, 2, 0f, 1f), Is.EqualTo(0.5f));
        }

        [Test]
        public void AddNectar_SameNectarBloomsTheSameInSmallOrBigLoads()
        {
            float small = 0f, big = 0f;
            for (int i = 0; i < 30; i++)
                small = BloomMath.AddNectar(small, 2, 70f, 1f);
            for (int i = 0; i < 10; i++)
                big = BloomMath.AddNectar(big, 6, 70f, 1f);
            Assert.That(small, Is.EqualTo(big).Within(1e-5f));
            Assert.That(small, Is.LessThan(1f));
            Assert.That(BloomMath.AddNectar(small, 10.01, 70f, 1f), Is.EqualTo(1f), "About 70 units in all bloom the flower.");
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
