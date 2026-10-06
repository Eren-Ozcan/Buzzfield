namespace Buzzfield.Core
{
    /// <summary>
    /// When the next garden visitor (the butterfly) comes: a random wait counted in calm play
    /// time only, so it never arrives behind a panel, an ad or the Garden Complete celebration.
    /// </summary>
    public struct VisitorTimer
    {
        /// <summary>Calm seconds left before the next visitor.</summary>
        public double Remaining { get; private set; }

        /// <summary>Starts a new wait between <paramref name="min"/> and <paramref name="max"/> seconds; <paramref name="random01"/> picks where.</summary>
        public void Restart(double random01, double min, double max)
        {
            if (max < min)
                max = min;
            double t = random01 < 0 ? 0 : random01 > 1 ? 1 : random01;
            Remaining = min + (max - min) * t;
        }

        /// <summary>Counts down while <paramref name="calm"/>; true on every calm tick once the wait is over.</summary>
        public bool Tick(double deltaSeconds, bool calm)
        {
            if (!calm)
                return false;
            if (deltaSeconds > 0)
                Remaining -= deltaSeconds;
            return Remaining <= 0;
        }
    }
}
