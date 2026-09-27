using System;

namespace Buzzfield.Core
{
    /// <summary>
    /// Average amount per second over a sliding time window, built from real events
    /// (honey deposits). Samples go into a fixed ring of time buckets, so adding and
    /// reading never allocate. Early in a session the average divides by the time that
    /// has actually passed instead of the full window, so it is not understated.
    /// </summary>
    public sealed class RollingRate
    {
        readonly double windowSeconds;
        readonly double bucketSeconds;
        readonly BigNumber[] sums;
        readonly long[] bucketIds;
        double startTime;

        public RollingRate(double windowSeconds, int bucketCount, double startTime)
        {
            if (windowSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(windowSeconds));
            if (bucketCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(bucketCount));
            this.windowSeconds = windowSeconds;
            bucketSeconds = windowSeconds / bucketCount;
            sums = new BigNumber[bucketCount];
            bucketIds = new long[bucketCount];
            Reset(startTime);
        }

        /// <summary>Forgets all samples; the window starts filling again at <paramref name="now"/>.</summary>
        public void Reset(double now)
        {
            for (int i = 0; i < sums.Length; i++)
            {
                sums[i] = BigNumber.Zero;
                bucketIds[i] = long.MinValue;
            }
            startTime = now;
        }

        public void Add(double now, BigNumber amount)
        {
            long id = BucketId(now);
            int slot = Slot(id);
            if (bucketIds[slot] != id)
            {
                bucketIds[slot] = id;
                sums[slot] = BigNumber.Zero;
            }
            sums[slot] += amount;
        }

        /// <summary>Amount per second over the window ending at <paramref name="now"/>.</summary>
        public BigNumber PerSecond(double now)
        {
            long current = BucketId(now);
            long oldest = current - sums.Length + 1;
            BigNumber total = BigNumber.Zero;
            for (int i = 0; i < sums.Length; i++)
            {
                long id = bucketIds[i];
                if (id >= oldest && id <= current)
                    total += sums[i];
            }
            if (total.IsZero)
                return BigNumber.Zero;

            double elapsed = Math.Min(windowSeconds, now - startTime);
            // At least one bucket, so the very first deposit does not read as a huge spike.
            double divisor = Math.Max(bucketSeconds, elapsed);
            return total / divisor;
        }

        long BucketId(double time) => (long)Math.Floor(time / bucketSeconds);

        int Slot(long id)
        {
            long slot = id % sums.Length;
            return (int)(slot < 0 ? slot + sums.Length : slot);
        }
    }
}
