using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using Buzzfield.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace Buzzfield.Game
{
    /// <summary>
    /// Clock readings for offline earnings. Trusted UTC comes from the Date header of an
    /// HTTPS response and is carried forward on the monotonic clock, so it survives device
    /// clock changes for the rest of the process. All times are UTC unix seconds.
    /// </summary>
    public sealed class GameClock
    {
        private const string TimeUrl = "https://www.google.com/generate_204";
        private const float RetrySeconds = 15f;
        private const int TimeoutSeconds = 8;
        /// <summary>Boot estimates closer than this are the same boot (wall clock rounding, small drift).</summary>
        private const double SameBootToleranceSeconds = 5;

        /// <summary>Trusted UTC minus monotonic seconds; null until a time response arrives.</summary>
        private double? trustedOffset;

        public bool HasTrustedTime => trustedOffset.HasValue;

        public event Action TrustedTimeArrived;

        public static double DeviceUtc => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;

        /// <summary>Seconds since device boot; keeps counting through sleep on Android.</summary>
        public static double MonotonicSeconds
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                using (var clock = new AndroidJavaClass("android.os.SystemClock"))
                    return clock.CallStatic<long>("elapsedRealtime") / 1000.0;
#else
                return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
#endif
            }
        }

        public double? TrustedUtcAt(double monotonicSeconds) => trustedOffset + monotonicSeconds;

        /// <summary>Readings for the save: the moment the player (possibly) leaves.</summary>
        public ClockStamp Stamp()
        {
            double device = DeviceUtc;
            double monotonic = MonotonicSeconds;
            return new ClockStamp
            {
                lastSeenUtc = TrustedUtcAt(monotonic) ?? device,
                lastDeviceUtc = device,
                lastMonotonicSeconds = monotonic,
                lastBootUtc = device - monotonic,
            };
        }

        /// <summary>
        /// Clock input for a return seen at <paramref name="deviceNow"/>/<paramref name="monotonicNow"/>.
        /// A later call with the same readings picks up trusted time that arrived in between.
        /// </summary>
        public OfflineClockInput ReturnInput(ClockStamp stamp, double deviceNow, double monotonicNow)
        {
            double bootNow = deviceNow - monotonicNow;
            return new OfflineClockInput
            {
                LastSeenUtc = stamp.lastSeenUtc,
                TrustedNowUtc = TrustedUtcAt(monotonicNow),
                LastDeviceUtc = stamp.lastDeviceUtc,
                DeviceNowUtc = deviceNow,
                LastMonotonicSeconds = stamp.lastMonotonicSeconds,
                MonotonicNowSeconds = monotonicNow,
                // A forward clock change also moves the boot estimate; that case then waits
                // for trusted time, so it is still safe.
                SameBoot = Math.Abs(bootNow - stamp.lastBootUtc) < SameBootToleranceSeconds,
            };
        }

        /// <summary>Retries until one response with a Date header arrives.</summary>
        public IEnumerator FetchTrustedTime()
        {
            while (!trustedOffset.HasValue)
            {
                using (UnityWebRequest request = UnityWebRequest.Head(TimeUrl))
                {
                    request.timeout = TimeoutSeconds;
                    double sentAt = MonotonicSeconds;
                    yield return request.SendWebRequest();
                    double receivedAt = MonotonicSeconds;

                    string date = request.result == UnityWebRequest.Result.Success ? request.GetResponseHeader("Date") : null;
                    if (date != null && DateTimeOffset.TryParseExact(date, "r", CultureInfo.InvariantCulture,
                            DateTimeStyles.AssumeUniversal, out DateTimeOffset serverTime))
                    {
                        // The header has 1 s resolution; the midpoint of the round trip is the best estimate.
                        double midpoint = (sentAt + receivedAt) / 2;
                        trustedOffset = serverTime.ToUnixTimeSeconds() + 0.5 - midpoint;
                        TrustedTimeArrived?.Invoke();
                        yield break;
                    }
                }
                yield return new WaitForSecondsRealtime(RetrySeconds);
            }
        }
    }
}
