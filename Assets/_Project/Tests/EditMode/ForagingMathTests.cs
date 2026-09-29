using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class ForagingMathTests
    {
        [Test]
        public void MinLoad_IsShareOfCapacityOrFullFlower()
        {
            Assert.That(ForagingMath.MinLoad(capacity: 6, maxNectar: 12, loadFraction: 0.75), Is.EqualTo(4.5).Within(1e-9));
            Assert.That(ForagingMath.MinLoad(capacity: 16, maxNectar: 12, loadFraction: 0.75), Is.EqualTo(9).Within(1e-9), "A full flower caps the load.");
        }

        [Test]
        public void MinLoad_IsNeverBelowOneUnit()
        {
            Assert.That(ForagingMath.MinLoad(capacity: 2, maxNectar: 12, loadFraction: 0.25), Is.EqualTo(1));
            Assert.That(ForagingMath.MinLoad(capacity: 2, maxNectar: 12, loadFraction: 0), Is.EqualTo(1));
            Assert.That(ForagingMath.MinLoad(capacity: 2, maxNectar: 12, loadFraction: -1), Is.EqualTo(1));
        }

        [Test]
        public void OffersLoad_NeedsOneLoadForAFreeFlower()
        {
            Assert.That(ForagingMath.OffersLoad(nectar: 4.5, maxNectar: 12, assignedBees: 0, capacity: 6, loadFraction: 0.75));
            Assert.That(ForagingMath.OffersLoad(nectar: 4.4, maxNectar: 12, assignedBees: 0, capacity: 6, loadFraction: 0.75), Is.False);
        }

        [Test]
        public void OffersLoad_LeavesTheLoadsOfBeesOnTheirWay()
        {
            Assert.That(ForagingMath.OffersLoad(nectar: 8, maxNectar: 12, assignedBees: 1, capacity: 6, loadFraction: 0.75), Is.False);
            Assert.That(ForagingMath.OffersLoad(nectar: 9, maxNectar: 12, assignedBees: 1, capacity: 6, loadFraction: 0.75));
        }

        [Test]
        public void OffersLoad_WorkerNeedsLessThanGolden()
        {
            Assert.That(ForagingMath.OffersLoad(nectar: 3, maxNectar: 32, assignedBees: 0, capacity: 2, loadFraction: 0.75));
            Assert.That(ForagingMath.OffersLoad(nectar: 3, maxNectar: 32, assignedBees: 0, capacity: 16, loadFraction: 0.75), Is.False);
        }
    }
}
