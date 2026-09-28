using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public sealed class TweenMathTests
    {
        [Test]
        public void Pop_StartsAtFrom_PeaksMidway_EndsAtOne()
        {
            Assert.That(TweenMath.Pop(1f, 0.2f, 0f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(TweenMath.Pop(1f, 0.2f, 0.5f), Is.EqualTo(1.2f).Within(1e-5f));
            Assert.That(TweenMath.Pop(1f, 0.2f, 1f), Is.EqualTo(1f));
        }

        [Test]
        public void Pop_FromPressedScale_ReturnsToOne()
        {
            Assert.That(TweenMath.Pop(0.9f, 0f, 0f), Is.EqualTo(0.9f).Within(1e-5f));
            Assert.That(TweenMath.Pop(0.9f, 0f, 0.5f), Is.GreaterThan(0.9f).And.LessThan(1f));
            Assert.That(TweenMath.Pop(0.9f, 0f, 1f), Is.EqualTo(1f));
        }

        [Test]
        public void Pop_ClampsTimeOutsideRange()
        {
            Assert.That(TweenMath.Pop(0f, 0.3f, -1f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(TweenMath.Pop(0f, 0.3f, 5f), Is.EqualTo(1f));
        }

        [Test]
        public void Hold_EasesToTargetAndStays()
        {
            Assert.That(TweenMath.Hold(1f, 0.9f, 0f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(TweenMath.Hold(1f, 0.9f, 1f), Is.EqualTo(0.9f).Within(1e-5f));
            Assert.That(TweenMath.Hold(1f, 0.9f, 3f), Is.EqualTo(0.9f).Within(1e-5f));
        }

        [Test]
        public void Wiggle_StartsAndEndsAtRest_StaysWithinOne()
        {
            Assert.That(TweenMath.Wiggle(0f, 3f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(TweenMath.Wiggle(1f, 3f), Is.EqualTo(0f));
            for (float t = 0f; t <= 1f; t += 0.01f)
                Assert.That(System.Math.Abs(TweenMath.Wiggle(t, 3f)), Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void Wiggle_FadesOut()
        {
            // Peaks of each swing get smaller.
            float first = System.Math.Abs(TweenMath.Wiggle(1f / 12f, 3f));
            float last = System.Math.Abs(TweenMath.Wiggle(9f / 12f, 3f));
            Assert.That(last, Is.LessThan(first));
        }
    }
}
