using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class DewBoostQueueTests
    {
        const double T0 = 1_700_000_000;

        [Test]
        public void Empty_PaysNothingExtra()
        {
            var line = new DewBoostQueue();
            line.Advance(T0);
            Assert.That(line.Multiplier, Is.EqualTo(1));
            Assert.That(line.SecondsLeft, Is.Zero);
            Assert.That(line.ExtraSeconds(3600), Is.Zero);
        }

        [Test]
        public void Boost_RunsOnTheClock_ThenEnds()
        {
            var line = new DewBoostQueue();
            line.Add(2f, 3600, T0);
            Assert.That(line.Multiplier, Is.EqualTo(2));
            line.Advance(T0 + 1000);
            Assert.That(line.SecondsLeft, Is.EqualTo(2600).Within(1e-9));
            line.Advance(T0 + 3600);
            Assert.That(line.Count, Is.Zero);
            Assert.That(line.Multiplier, Is.EqualTo(1));
        }

        [Test]
        public void StrongerBoost_RunsFirst_AndTheWeakerWaitsWithItsTimeIntact()
        {
            var line = new DewBoostQueue();
            line.Add(2f, 3600, T0);
            line.Advance(T0 + 600);
            line.Add(3f, 1800, T0 + 600);

            Assert.That(line.Multiplier, Is.EqualTo(3));
            line.Advance(T0 + 600 + 1800);
            Assert.That(line.Multiplier, Is.EqualTo(2), "The 2x comes back once the 3x is over.");
            Assert.That(line.SecondsLeft, Is.EqualTo(3000).Within(1e-9), "Nothing of the 2x ran while it waited.");
        }

        [Test]
        public void WeakerBoost_WaitsBehindTheRunningOne()
        {
            var line = new DewBoostQueue();
            line.Add(3f, 1800, T0);
            line.Add(2f, 3600, T0 + 100);
            Assert.That(line.Multiplier, Is.EqualTo(3));
            Assert.That(line.Count, Is.EqualTo(2));
            Assert.That(line.Entries[1].secondsLeft, Is.EqualTo(3600));
        }

        [Test]
        public void SameStrength_AddsItsTime()
        {
            var line = new DewBoostQueue();
            line.Add(2f, 3600, T0);
            line.Add(2f, 3600, T0 + 600);
            Assert.That(line.Count, Is.EqualTo(1));
            Assert.That(line.SecondsLeft, Is.EqualTo(6600).Within(1e-9));
        }

        [Test]
        public void LongAbsence_UsesUpTheWholeLineInOrder()
        {
            var line = new DewBoostQueue();
            line.Add(3f, 1800, T0);
            line.Add(2f, 3600, T0);
            line.Advance(T0 + 1800 + 1000);
            Assert.That(line.Multiplier, Is.EqualTo(2));
            Assert.That(line.SecondsLeft, Is.EqualTo(2600).Within(1e-9));
            line.Advance(T0 + 100_000);
            Assert.That(line.Count, Is.Zero);
        }

        [Test]
        public void ClockGoingBack_PausesWithoutGivingTimeBack()
        {
            var line = new DewBoostQueue();
            line.Add(2f, 3600, T0);
            line.Advance(T0 + 600);
            line.Advance(T0 - 5000);
            Assert.That(line.SecondsLeft, Is.EqualTo(3000).Within(1e-9));
            line.Advance(T0 - 4000);
            Assert.That(line.SecondsLeft, Is.EqualTo(2000).Within(1e-9), "Runs on from the new reading.");
        }

        [Test]
        public void ExtraSeconds_CountsOnlyTheBoostedPartOfTheWindow()
        {
            var line = new DewBoostQueue();
            line.Add(3f, 1800, T0);
            line.Add(2f, 3600, T0);
            // 1800 s at 3x (+2 each) and 1200 s at 2x (+1 each) inside a 3000 s window.
            Assert.That(line.ExtraSeconds(3000), Is.EqualTo(2 * 1800 + 1200).Within(1e-9));
            // A window longer than the line: the rest is unboosted.
            Assert.That(line.ExtraSeconds(100_000), Is.EqualTo(2 * 1800 + 3600).Within(1e-9));
            Assert.That(line.Count, Is.EqualTo(2), "Measuring does not use the boosts up.");
        }

        [Test]
        public void NoBonusOrNoTime_IsIgnored()
        {
            var line = new DewBoostQueue();
            line.Add(1f, 3600, T0);
            line.Add(2f, 0, T0);
            line.Add(0.5f, 100, T0);
            Assert.That(line.Count, Is.Zero);
        }

        [Test]
        public void Restore_DropsBrokenEntries_AndSortsStrongestFirst()
        {
            var line = new DewBoostQueue();
            line.Restore(new[]
            {
                new DewBoostEntry(2f, 3600), new DewBoostEntry(3f, -5), new DewBoostEntry(3f, 900),
                new DewBoostEntry(float.NaN, 100), new DewBoostEntry(2f, double.NaN),
            }, T0);
            Assert.That(line.Count, Is.EqualTo(2));
            Assert.That(line.Multiplier, Is.EqualTo(3));
            Assert.That(line.SecondsLeft, Is.EqualTo(900));
            Assert.That(line.AnchorUtc, Is.EqualTo(T0));

            line.Advance(T0 + 1000);
            Assert.That(line.Multiplier, Is.EqualTo(2));
            Assert.That(line.SecondsLeft, Is.EqualTo(3500).Within(1e-9));
        }

        [Test]
        public void Restore_Null_IsEmpty()
        {
            var line = new DewBoostQueue();
            line.Add(2f, 60, T0);
            line.Restore(null, T0);
            Assert.That(line.Count, Is.Zero);
        }

        [Test]
        public void CaptureThenRestore_GivesTheSameLine()
        {
            var line = new DewBoostQueue();
            line.Add(3f, 1800, T0);
            line.Add(2f, 3600, T0);
            line.Advance(T0 + 300);

            var copy = new DewBoostQueue();
            copy.Restore(line.Capture(), line.AnchorUtc);
            Assert.That(copy.Count, Is.EqualTo(2));
            Assert.That(copy.SecondsLeft, Is.EqualTo(line.SecondsLeft));
            Assert.That(copy.ExtraSeconds(10_000), Is.EqualTo(line.ExtraSeconds(10_000)).Within(1e-9));
        }
    }
}
