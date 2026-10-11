using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    /// <summary>Rare bee costs and bonuses (docs/DEWDROPS.md 4.2).</summary>
    public class RareBeeMathTests
    {
        static readonly int[] LevelCosts = { 20, 30, 45, 65, 90, 120, 160, 210, 270 };

        [Test]
        public void NextCost_IsTheUnlockPriceAtLevelZero()
        {
            Assert.That(RareBeeMath.NextCost(0, 10, 120, LevelCosts), Is.EqualTo(120));
            Assert.That(RareBeeMath.NextCost(0, 10, 0, LevelCosts), Is.Zero, "The Bumblebee joins for free.");
            Assert.That(RareBeeMath.NextCost(0, 10, -5, LevelCosts), Is.Zero, "A broken price never pays the player.");
        }

        [TestCase(1, 20)]
        [TestCase(2, 30)]
        [TestCase(5, 90)]
        [TestCase(9, 270)]
        public void NextCost_ClimbsTheLevelList(int level, long expected)
        {
            Assert.That(RareBeeMath.NextCost(level, 10, 650, LevelCosts), Is.EqualTo(expected));
        }

        [Test]
        public void NextCost_IsMinusOneAtTheTopLevel()
        {
            Assert.That(RareBeeMath.NextCost(10, 10, 650, LevelCosts), Is.EqualTo(-1));
            Assert.That(RareBeeMath.NextCost(12, 10, 650, LevelCosts), Is.EqualTo(-1));
        }

        [Test]
        public void NextCost_RepeatsTheLastEntry_ForLevelsPastTheList()
        {
            Assert.That(RareBeeMath.NextCost(11, 15, 0, LevelCosts), Is.EqualTo(270));
            Assert.That(RareBeeMath.NextCost(3, 10, 0, new int[0]), Is.EqualTo(-1), "No level list, no levels to buy.");
        }

        [Test]
        public void LevelsCost_FromOneToTen_Is1010()
        {
            Assert.That(RareBeeMath.LevelsCost(10, LevelCosts), Is.EqualTo(1010));
            Assert.That(RareBeeMath.LevelsCost(1, LevelCosts), Is.Zero, "A one-level species has nothing to grow.");
        }

        [Test]
        public void Effect_AddsPerLevel_AndIsZeroBeforeJoining()
        {
            Assert.That(RareBeeMath.Effect(3, 10), Is.EqualTo(30).Within(1e-9));
            Assert.That(RareBeeMath.Effect(8, 1), Is.EqualTo(8).Within(1e-9));
            Assert.That(RareBeeMath.Effect(4, 0), Is.Zero);
            Assert.That(RareBeeMath.Effect(4, -2), Is.Zero);
        }

        [Test]
        public void ClampLevel_KeepsSavedLevelsInRange()
        {
            Assert.That(RareBeeMath.ClampLevel(-1, 10), Is.Zero);
            Assert.That(RareBeeMath.ClampLevel(7, 10), Is.EqualTo(7));
            Assert.That(RareBeeMath.ClampLevel(14, 10), Is.EqualTo(10));
        }

        [Test]
        public void IsAvailable_FromTheSpeciesGarden()
        {
            Assert.That(RareBeeMath.IsAvailable(2, 3), Is.False);
            Assert.That(RareBeeMath.IsAvailable(3, 3), Is.True);
            Assert.That(RareBeeMath.IsAvailable(9, 3), Is.True, "Loops past the authored gardens keep counting.");
        }

        [Test]
        public void DiscountFactor_TakesPercentOff_NeverBelowZero()
        {
            Assert.That(RareBeeMath.DiscountFactor(20), Is.EqualTo(0.8).Within(1e-9));
            Assert.That(RareBeeMath.DiscountFactor(0), Is.EqualTo(1));
            Assert.That(RareBeeMath.DiscountFactor(-10), Is.EqualTo(1), "A negative discount is no surcharge.");
            Assert.That(RareBeeMath.DiscountFactor(150), Is.Zero);
        }

        [Test]
        public void RaisedEfficiency_AddsPoints_UpToAHundredPercent()
        {
            Assert.That(RareBeeMath.RaisedEfficiency(0.5, 30), Is.EqualTo(0.8).Within(1e-9), "The Mason Bee at level 10.");
            Assert.That(RareBeeMath.RaisedEfficiency(0.5, 0), Is.EqualTo(0.5).Within(1e-9));
            Assert.That(RareBeeMath.RaisedEfficiency(0.9, 30), Is.EqualTo(1));
        }

        [Test]
        public void UpgradeCost_Scaled_RoundsUpToWholeHoney()
        {
            Assert.That(UpgradeMath.Scaled(BigNumber.FromDouble(4), 0.98).ToDouble(), Is.EqualTo(4), "3.92 honey costs 4.");
            Assert.That(UpgradeMath.Scaled(BigNumber.FromDouble(2500), 0.8).ToDouble(), Is.EqualTo(2000));
            Assert.That(UpgradeMath.Scaled(BigNumber.FromDouble(101), 1).ToDouble(), Is.EqualTo(101));
            Assert.That(UpgradeMath.Scaled(BigNumber.Create(5, 30), 0.8).ToDouble(), Is.EqualTo(4e30).Within(1e16), "Past 1e15 it scales as is.");
        }

        [Test]
        public void Jelly_WithTheCarpenterBee_IsFlooredOnce()
        {
            BigNumber plain = PrestigeMath.Jelly(BigNumber.FromDouble(4e6), 2000, 1, 0.5);
            Assert.That(PrestigeMath.Jelly(BigNumber.FromDouble(4e6), 2000, 1, 0.5, 1), Is.EqualTo(plain), "No bonus, the same jelly.");
            // floor(sqrt(2000)) = 44; 44 * 1.5 * 1.4 = 92.4.
            Assert.That(PrestigeMath.Jelly(BigNumber.FromDouble(4e6), 2000, 1, 0.5, 1.4).ToDouble(), Is.EqualTo(92));
        }
    }
}
