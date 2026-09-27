using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class OfflineEarningsTests
    {
        const double Start = 1_700_000_000;
        const double Cap = 2 * 3600;
        const double Tolerance = 120;

        /// <summary>Device and monotonic clock both moved <paramref name="away"/> seconds; same boot.</summary>
        static OfflineClockInput Honest(double away, double? trustedNow = null) => new OfflineClockInput
        {
            LastSeenUtc = Start,
            LastDeviceUtc = Start,
            DeviceNowUtc = Start + away,
            LastMonotonicSeconds = 5000,
            MonotonicNowSeconds = 5000 + away,
            TrustedNowUtc = trustedNow,
            SameBoot = true,
        };

        static OfflineResult Evaluate(OfflineClockInput clock, double rate = 10, double efficiency = 0.5) =>
            OfflineEarnings.Evaluate(clock, Cap, rate, efficiency, Tolerance);

        [Test]
        public void SameBoot_PaysRateTimesElapsedTimesEfficiency()
        {
            OfflineResult result = Evaluate(Honest(600));
            Assert.That(result.Status, Is.EqualTo(OfflineStatus.Credited));
            Assert.That(result.ElapsedSeconds, Is.EqualTo(600).Within(1e-9));
            Assert.That(result.Amount.ToDouble(), Is.EqualTo(10 * 600 * 0.5).Within(1e-6));
            Assert.That(result.CapReached, Is.False);
            Assert.That(result.IsPayable);
        }

        [Test]
        public void LongAbsence_IsCappedButReportsFullElapsed()
        {
            OfflineResult result = Evaluate(Honest(10 * 3600));
            Assert.That(result.ElapsedSeconds, Is.EqualTo(10 * 3600).Within(1e-6));
            Assert.That(result.CreditedSeconds, Is.EqualTo(Cap));
            Assert.That(result.Amount.ToDouble(), Is.EqualTo(10 * Cap * 0.5).Within(1e-6));
            Assert.That(result.CapReached);
        }

        [Test]
        public void ExactlyAtCap_CountsAsCapReached()
        {
            Assert.That(Evaluate(Honest(Cap)).CapReached);
        }

        [Test]
        public void TimestampInTheFuture_PaysNothing()
        {
            OfflineClockInput clock = Honest(0);
            clock.LastSeenUtc = Start + 3600;
            OfflineResult result = Evaluate(clock);
            Assert.That(result.Status, Is.EqualTo(OfflineStatus.RejectedClockBackwards));
            Assert.That(result.Amount.IsZero);
            Assert.That(result.IsPayable, Is.False);
        }

        [Test]
        public void DeviceClockMovedForward_WithoutMonotonicTime_IsTamper()
        {
            OfflineClockInput clock = Honest(60);
            clock.DeviceNowUtc = Start + 5 * 3600;
            OfflineResult result = Evaluate(clock);
            Assert.That(result.Status, Is.EqualTo(OfflineStatus.RejectedClockTamper));
            Assert.That(result.Amount.IsZero);
        }

        [Test]
        public void SmallClockDrift_WithinTolerance_IsNotTamper()
        {
            OfflineClockInput clock = Honest(600);
            clock.DeviceNowUtc += Tolerance - 1;
            OfflineResult result = Evaluate(clock);
            Assert.That(result.Status, Is.EqualTo(OfflineStatus.Credited));
            // The monotonic clock, not the drifted wall clock, measures the absence.
            Assert.That(result.ElapsedSeconds, Is.EqualTo(600).Within(1e-9));
        }

        [Test]
        public void Rebooted_WithoutTrustedTime_IsPending()
        {
            OfflineClockInput clock = Honest(600);
            clock.SameBoot = false;
            clock.MonotonicNowSeconds = 30;
            OfflineResult result = Evaluate(clock);
            Assert.That(result.Status, Is.EqualTo(OfflineStatus.PendingTrustedTime));
            Assert.That(result.IsPayable, Is.False);
            Assert.That(result.Amount.ToDouble(), Is.EqualTo(3000).Within(1e-6), "Preview uses the device clock.");
        }

        [Test]
        public void Rebooted_WithTrustedTime_PaysFromTrustedTime()
        {
            OfflineClockInput clock = Honest(99_999);
            clock.SameBoot = false;
            clock.MonotonicNowSeconds = 30;
            clock.TrustedNowUtc = Start + 900;
            OfflineResult result = Evaluate(clock);
            Assert.That(result.Status, Is.EqualTo(OfflineStatus.Credited));
            Assert.That(result.ElapsedSeconds, Is.EqualTo(900).Within(1e-6));
        }

        [Test]
        public void Rebooted_ClockSetBack_IsRejected()
        {
            OfflineClockInput clock = Honest(-3600);
            clock.SameBoot = false;
            Assert.That(Evaluate(clock).Status, Is.EqualTo(OfflineStatus.RejectedClockBackwards));
        }

        [TestCase(0.0)]
        [TestCase(-5.0)]
        public void NoIncome_PaysNothing(double rate)
        {
            OfflineResult result = Evaluate(Honest(600), rate);
            Assert.That(result.Status, Is.EqualTo(OfflineStatus.Credited));
            Assert.That(result.Amount.IsZero);
            Assert.That(result.IsPayable, Is.False);
        }

        [Test]
        public void Efficiency_IsClampedToZeroToOne()
        {
            Assert.That(Evaluate(Honest(100), 10, 3).Amount.ToDouble(), Is.EqualTo(1000).Within(1e-6));
            Assert.That(Evaluate(Honest(100), 10, -1).Amount.IsZero);
        }

        [Test]
        public void HugeRate_StaysBigNumber()
        {
            OfflineResult result = OfflineEarnings.Evaluate(Honest(3600), Cap, BigNumber.Create(5, 300), 0.5, Tolerance);
            Assert.That(result.Amount.Exponent, Is.EqualTo(303));
            Assert.That(result.Amount.Mantissa, Is.EqualTo(9).Within(1e-9));
        }
    }
}
