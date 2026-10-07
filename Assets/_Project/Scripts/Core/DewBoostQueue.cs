using System;
using System.Collections.Generic;

namespace Buzzfield.Core
{
    /// <summary>One bought honey boost: its multiplier and the real seconds it still has to run.</summary>
    [Serializable]
    public struct DewBoostEntry
    {
        public float multiplier;
        public double secondsLeft;

        public DewBoostEntry(float multiplier, double secondsLeft)
        {
            this.multiplier = multiplier;
            this.secondsLeft = secondsLeft;
        }
    }

    /// <summary>
    /// The honey boosts bought with Dewdrops (docs/DEWDROPS.md 4.1). One runs at a time: the
    /// strongest. A weaker one waits in line with its time intact and never gets lost, and a
    /// boost of the same strength adds its time to the one already there. Time runs on the
    /// wall clock, so boosts keep going while the game is closed; a clock that went backwards
    /// pauses them instead of giving time back.
    /// </summary>
    public sealed class DewBoostQueue
    {
        /// <summary>Strongest first; only the head runs.</summary>
        private readonly List<DewBoostEntry> entries = new List<DewBoostEntry>();

        /// <summary>UTC up to which <see cref="entries"/> is current.</summary>
        public double AnchorUtc { get; private set; }

        public int Count => entries.Count;

        public IReadOnlyList<DewBoostEntry> Entries => entries;

        /// <summary>Multiplier of the running boost; 1 when none runs.</summary>
        public double Multiplier => entries.Count > 0 ? entries[0].multiplier : 1;

        /// <summary>Seconds the running boost has left; 0 when none runs.</summary>
        public double SecondsLeft => entries.Count > 0 ? entries[0].secondsLeft : 0;

        /// <summary>Lets <paramref name="utcNow"/> pass, using up the boosts in line order.</summary>
        public void Advance(double utcNow)
        {
            double elapsed = utcNow - AnchorUtc;
            AnchorUtc = utcNow;
            if (elapsed <= 0)
                return;
            while (entries.Count > 0 && elapsed > 0)
            {
                DewBoostEntry head = entries[0];
                if (head.secondsLeft > elapsed)
                {
                    head.secondsLeft -= elapsed;
                    entries[0] = head;
                    return;
                }
                elapsed -= head.secondsLeft;
                entries.RemoveAt(0);
            }
        }

        /// <summary>Adds a boost at <paramref name="utcNow"/>; it runs now if it is the strongest, else it waits.</summary>
        public void Add(float multiplier, double seconds, double utcNow)
        {
            Advance(utcNow);
            if (multiplier <= 1f || seconds <= 0)
                return;
            int index = 0;
            while (index < entries.Count && entries[index].multiplier > multiplier)
                index++;
            if (index < entries.Count && entries[index].multiplier == multiplier)
            {
                DewBoostEntry same = entries[index];
                same.secondsLeft += seconds;
                entries[index] = same;
                return;
            }
            entries.Insert(index, new DewBoostEntry(multiplier, seconds));
        }

        /// <summary>
        /// Extra multiplier-seconds the line pays over the next <paramref name="seconds"/> from
        /// <see cref="AnchorUtc"/>: the sum of (multiplier - 1) times the time each boost runs in
        /// that window. Offline honey for the window is the plain amount times
        /// (seconds + extra) / seconds. Does not use the boosts up.
        /// </summary>
        public double ExtraSeconds(double seconds)
        {
            double extra = 0;
            double left = seconds;
            for (int i = 0; i < entries.Count && left > 0; i++)
            {
                double run = Math.Min(left, entries[i].secondsLeft);
                extra += (entries[i].multiplier - 1) * run;
                left -= run;
            }
            return extra;
        }

        /// <summary>The line for the save.</summary>
        public DewBoostEntry[] Capture() => entries.ToArray();

        /// <summary>
        /// Loads a saved line as it was at <paramref name="anchorUtc"/>. Entries that cannot run
        /// (no time, no bonus) are dropped and the rest are sorted strongest first, so a damaged
        /// save never stalls the line.
        /// </summary>
        public void Restore(DewBoostEntry[] saved, double anchorUtc)
        {
            entries.Clear();
            AnchorUtc = anchorUtc;
            if (saved == null)
                return;
            for (int i = 0; i < saved.Length; i++)
            {
                if (saved[i].multiplier > 1f && saved[i].secondsLeft > 0)
                    Add(saved[i].multiplier, saved[i].secondsLeft, anchorUtc);
            }
        }
    }
}
