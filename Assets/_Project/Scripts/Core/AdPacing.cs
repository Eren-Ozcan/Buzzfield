using System;

namespace Buzzfield.Core
{
    /// <summary>Why an interstitial may not play now.</summary>
    public enum ForcedAdBlock
    {
        /// <summary>Nothing blocks it.</summary>
        None,
        /// <summary>The player bought remove_ads.</summary>
        Removed,
        /// <summary>A new player: not enough play time or no Queen move yet.</summary>
        TooEarly,
        /// <summary>A full-screen ad (rewarded included) played too recently.</summary>
        Cooldown,
    }

    /// <summary>
    /// Spacing between full-screen ads. Every full-screen ad stamps one shared "last ad"
    /// time (UTC seconds, saved). Rewarded ads stamp it but never wait for it (the player
    /// asked for them); interstitials wait until the gap has passed.
    /// </summary>
    public static class AdPacing
    {
        /// <summary>
        /// Seconds until the next full-screen ad is allowed; 0 when it is allowed now.
        /// A stamp more than one gap in the future (the device clock went back) is ignored,
        /// so a clock change never locks ads for longer than one gap.
        /// </summary>
        public static double SecondsUntilAllowed(double nowUtc, double lastAdUtc, double minGapSeconds)
        {
            if (minGapSeconds <= 0 || lastAdUtc <= 0)
                return 0;
            if (lastAdUtc > nowUtc + minGapSeconds)
                return 0;
            return Math.Max(0, lastAdUtc + minGapSeconds - nowUtc);
        }

        public static bool IsAllowed(double nowUtc, double lastAdUtc, double minGapSeconds) =>
            SecondsUntilAllowed(nowUtc, lastAdUtc, minGapSeconds) <= 0;

        /// <summary>
        /// Whether an interstitial may follow the natural break that just ended. A paying
        /// player never gets one; a new player (less than <paramref name="minPlaySeconds"/>
        /// of play or fewer than <paramref name="minQueenMoves"/> moves) neither; everyone
        /// else waits out the shared cooldown.
        /// </summary>
        public static ForcedAdBlock InterstitialBlock(double nowUtc, double lastAdUtc, double cooldownSeconds,
            bool adsRemoved, double playSeconds, double minPlaySeconds, int queenMoves, int minQueenMoves)
        {
            if (adsRemoved)
                return ForcedAdBlock.Removed;
            if (playSeconds < minPlaySeconds || queenMoves < minQueenMoves)
                return ForcedAdBlock.TooEarly;
            if (!IsAllowed(nowUtc, lastAdUtc, cooldownSeconds))
                return ForcedAdBlock.Cooldown;
            return ForcedAdBlock.None;
        }
    }
}
