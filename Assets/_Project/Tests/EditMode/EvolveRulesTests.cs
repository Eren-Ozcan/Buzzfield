using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class EvolveRulesTests
    {
        [TestCase(new[] { 0, 0, 0 }, -1)]
        [TestCase(new[] { 2, 2, 9 }, -1)]
        [TestCase(new[] { 3, 0, 0 }, 0)]
        [TestCase(new[] { 5, 4, 0 }, 0)]
        [TestCase(new[] { 2, 3, 0 }, 1)]
        [TestCase(new[] { 1, 1, 30 }, -1)]
        public void SourceTier_LowestFirst_NeverTopTier(int[] counts, int expected)
        {
            Assert.That(EvolveRules.SourceTier(counts), Is.EqualTo(expected));
        }

        [Test]
        public void SourceTier_SingleTier_NeverEvolves()
        {
            Assert.That(EvolveRules.SourceTier(new[] { 10 }), Is.EqualTo(-1));
        }
    }
}
