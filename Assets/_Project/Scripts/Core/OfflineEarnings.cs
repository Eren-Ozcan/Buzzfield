using System;

namespace Buzzfield.Core
{
    public enum OfflineStatus
    {
        /// <summary>Elapsed time is known (trusted time, or the monotonic clock of the same boot).</summary>
        Credited,

        /// <summary>The device rebooted and no trusted time has arrived yet. The amount is a preview
        /// from the device clock; evaluate again once trusted time arrives.</summary>
        PendingTrustedTime,

        /// <summary>The device clock jumped forward while the monotonic clock did not.</summary>
        RejectedClockTamper,

        /// <summary>Now is earlier than the saved timestamp. No reward; the next save resets the timestamp.</summary>
        RejectedClockBackwards,
    }

    /// <summary>Clock readings taken when the player last left (saved) and now.</summary>
    public struct OfflineClockInput
    {
        /// <summary>Best-known UTC when the player left, in unix seconds.</summary>
        public double LastSeenUtc;

        /// <summary>UTC from a trusted source (time server), or null when not known yet.</summary>
        public double? TrustedNowUtc;

        /// <summary>Device wall clock at leave and now, unix seconds.</summary>
        public double LastDeviceUtc;
        public double DeviceNowUtc;

        /// <summary>Monotonic seconds since boot at leave and now.</summary>
        public double LastMonotonicSeconds;
        public double MonotonicNowSeconds;

        /// <summary>False when the device rebooted in between; monotonic readings are then incomparable.</summary>
        public bool SameBoot;
    }

    public readonly struct OfflineResult
    {
        public readonly OfflineStatus Status;
        public readonly double ElapsedSeconds;
        public readonly double CreditedSeconds;
        public readonly BigNumber Amount;
        public readonly bool CapReached;

        public OfflineResult(OfflineStatus status, double elapsedSeconds, double creditedSeconds, BigNumber amount, bool capReached)
        {
            Status = status;
            ElapsedSeconds = elapsedSeconds;
            CreditedSeconds = creditedSeconds;
            Amount = amount;
            CapReached = capReached;
        }

        public bool IsPayable => Status == OfflineStatus.Credited && !Amount.IsZero;

        public static OfflineResult Rejected(OfflineStatus status) => new OfflineResult(status, 0, 0, BigNumber.Zero, false);
    }

    /// <summary>
    /// Offline honey (design doc section 7): rate x credited time x efficiency, capped.
    /// The device clock alone never pays out after a reboot, a forward clock jump without
    /// matching monotonic time is rejected, and a timestamp in the future pays nothing.
    /// </summary>
    public static class OfflineEarnings
    {
        /// <param name="capSeconds">Longest absence that pays.</param>
        /// <param name="ratePerSecond">Theoretical honey per second saved with the game.</param>
        /// <param name="efficiency">Share of <paramref name="ratePerSecond"/> earned while away (0..1).</param>
        /// <param name="tamperToleranceSeconds">How far the wall clock may run ahead of the monotonic clock.</param>
        public static OfflineResult Evaluate(OfflineClockInput clock, double capSeconds, BigNumber ratePerSecond,
            double efficiency, double tamperToleranceSeconds)
        {
            if (clock.SameBoot && IsTampered(clock, tamperToleranceSeconds))
                return OfflineResult.Rejected(OfflineStatus.RejectedClockTamper);

            bool trusted = clock.TrustedNowUtc.HasValue;
            double now = trusted ? clock.TrustedNowUtc.Value : clock.DeviceNowUtc;
            if (now < clock.LastSeenUtc)
                return OfflineResult.Rejected(OfflineStatus.RejectedClockBackwards);

            double elapsed;
            OfflineStatus status;
            if (trusted)
            {
                elapsed = now - clock.LastSeenUtc;
                status = OfflineStatus.Credited;
            }
            else if (clock.SameBoot)
            {
                // Same boot: the monotonic clock measured the absence and nobody can set it.
                elapsed = Math.Max(0, clock.MonotonicNowSeconds - clock.LastMonotonicSeconds);
                status = OfflineStatus.Credited;
            }
            else
            {
                elapsed = now - clock.LastSeenUtc;
                status = OfflineStatus.PendingTrustedTime;
            }

            double cap = Math.Max(0, capSeconds);
            double credited = Math.Min(elapsed, cap);
            double share = Math.Max(0, Math.Min(1, efficiency));
            BigNumber amount = ratePerSecond.IsNegative || ratePerSecond.IsZero
                ? BigNumber.Zero
                : ratePerSecond * (credited * share);
            return new OfflineResult(status, elapsed, credited, amount, elapsed >= cap && cap > 0);
        }

        static bool IsTampered(OfflineClockInput clock, double toleranceSeconds)
        {
            double wallDelta = clock.DeviceNowUtc - clock.LastDeviceUtc;
            double monotonicDelta = clock.MonotonicNowSeconds - clock.LastMonotonicSeconds;
            return wallDelta - monotonicDelta > toleranceSeconds;
        }
    }
}
