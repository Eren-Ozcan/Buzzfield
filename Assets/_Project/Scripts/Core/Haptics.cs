namespace Buzzfield.Core
{
    /// <summary>The player's vibration setting.</summary>
    public enum VibrationMode
    {
        Off = 0,
        /// <summary>Only the big moments: a garden completed, a goal claimed, a butterfly caught, a Queen move.</summary>
        Important = 1,
        /// <summary>Also a light tick for every shake and purchase.</summary>
        All = 2,
    }

    /// <summary>Short vibrations on the device; silent where there is no motor.</summary>
    public interface IHapticService
    {
        /// <summary>One pulse: a light tick, or a firmer one for an <paramref name="important"/> moment.</summary>
        void Pulse(bool important);
    }

    /// <summary>Editor and desktop stand-in: never vibrates.</summary>
    public sealed class NullHaptics : IHapticService
    {
        public void Pulse(bool important)
        {
        }
    }

    public static class HapticRules
    {
        /// <summary>Whether a pulse of this weight plays under the player's setting.</summary>
        public static bool Plays(VibrationMode mode, bool important) =>
            mode == VibrationMode.All || (mode == VibrationMode.Important && important);

        /// <summary>
        /// Light ticks closer together than this merge into one: a swipe can shake several
        /// flowers in one frame, and one tick says enough.
        /// </summary>
        public const double MinTickGapSeconds = 0.07;
    }
}
