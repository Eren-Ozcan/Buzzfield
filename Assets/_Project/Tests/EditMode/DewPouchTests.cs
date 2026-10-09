using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class DewPouchTests
    {
        const int Every = 4;
        const int PerDay = 4;

        [Test]
        public void EveryFourthButterfly_CarriesThePouch()
        {
            var pouch = new DewPouch();
            var carried = new bool[12];
            for (int i = 0; i < carried.Length; i++)
                carried[i] = pouch.NextCarries(100, true, Every, PerDay);
            Assert.That(carried, Is.EqualTo(new[] { false, false, false, true, false, false, false, true, false, false, false, true }));
            Assert.That(pouch.today, Is.EqualTo(3));
        }

        [Test]
        public void ButterflyThatMayNotCarryIt_PassesItOn()
        {
            var pouch = new DewPouch();
            for (int i = 0; i < 3; i++)
                pouch.NextCarries(100, true, Every, PerDay);
            Assert.That(pouch.NextCarries(100, false, Every, PerDay), Is.False, "No ad ready: a honey butterfly.");
            Assert.That(pouch.NextCarries(100, true, Every, PerDay), "The next one takes the pouch.");
        }

        [Test]
        public void UnknownDay_NeverCarriesIt()
        {
            var pouch = new DewPouch();
            for (int i = 0; i < 8; i++)
                Assert.That(pouch.NextCarries(0, true, Every, PerDay), Is.False);
        }

        [Test]
        public void AFewADay_ThenTheNextDayStartsOver()
        {
            var pouch = new DewPouch();
            int carried = 0;
            for (int i = 0; i < 40; i++)
            {
                if (pouch.NextCarries(100, true, Every, PerDay))
                    carried++;
            }
            Assert.That(carried, Is.EqualTo(PerDay));
            Assert.That(pouch.LeftToday(100, PerDay), Is.Zero);
            Assert.That(pouch.LeftToday(101, PerDay), Is.EqualTo(PerDay));
            Assert.That(pouch.NextCarries(101, true, Every, PerDay), "The butterflies waited past the cap; the new day's first one carries it.");
        }

        [Test]
        public void ClockMovedBack_KeepsTheDaysCount()
        {
            var pouch = new DewPouch();
            for (int i = 0; i < 16; i++)
                pouch.NextCarries(100, true, Every, PerDay);
            Assert.That(pouch.today, Is.EqualTo(PerDay));
            for (int i = 0; i < 16; i++)
                Assert.That(pouch.NextCarries(99, true, Every, PerDay), Is.False);
            Assert.That(pouch.day, Is.EqualTo(100));
        }

        [Test]
        public void Roll_IsOneToSix_EachAsLikely()
        {
            Assert.That(DewPouch.Roll(0, 1, 6), Is.EqualTo(1));
            Assert.That(DewPouch.Roll(0.9999999, 1, 6), Is.EqualTo(6));
            Assert.That(DewPouch.Roll(1, 1, 6), Is.EqualTo(6), "1 itself stays in range.");
            Assert.That(DewPouch.Roll(-0.5, 1, 6), Is.EqualTo(1));
            var counts = new int[7];
            const int steps = 6000;
            for (int i = 0; i < steps; i++)
                counts[DewPouch.Roll((i + 0.5) / steps, 1, 6)]++;
            for (int value = 1; value <= 6; value++)
                Assert.That(counts[value], Is.EqualTo(steps / 6), $"Value {value}");
            Assert.That(DewPouch.Roll(0.5, 3, 3), Is.EqualTo(3));
        }
    }
}
