using System;

namespace Buzzfield.Core
{
    /// <summary>
    /// The pure side of the game's sound: pitches on the F major pentatonic (the music's key, so
    /// any pitched chime fits), decibel gains and the equal-power crossfades between loops.
    /// </summary>
    public static class SoundRules
    {
        /// <summary>Semitones of the major pentatonic above its root: F G A C D in F.</summary>
        private static readonly int[] Pentatonic = { 0, 2, 4, 7, 9 };

        /// <summary>Notes in one octave of the pentatonic.</summary>
        public static int PentatonicSteps => Pentatonic.Length;

        /// <summary>Pitch factor for <paramref name="step"/> on the pentatonic: 0 is the clip's own note, 5 an octave up.</summary>
        public static float PentatonicPitch(int step)
        {
            if (step < 0)
                step = 0;
            int semitones = 12 * (step / Pentatonic.Length) + Pentatonic[step % Pentatonic.Length];
            return SemitonePitch(semitones);
        }

        /// <summary>Pitch factor that moves a clip by <paramref name="semitones"/>.</summary>
        public static float SemitonePitch(float semitones) => (float)Math.Pow(2.0, semitones / 12.0);

        /// <summary>Linear volume for a level in decibels (0 dB = 1).</summary>
        public static float Gain(float db) => (float)Math.Pow(10.0, db / 20.0);

        /// <summary>Volume of the layer fading in at blend <paramref name="t"/> (0..1), equal power.</summary>
        public static float FadeIn(float t) => (float)Math.Sin(Clamp01(t) * Math.PI * 0.5);

        /// <summary>Volume of the layer fading out at blend <paramref name="t"/> (0..1), equal power.</summary>
        public static float FadeOut(float t) => (float)Math.Cos(Clamp01(t) * Math.PI * 0.5);

        /// <summary>
        /// Volumes of the small, medium and large swarm loops for <paramref name="bees"/>: small
        /// alone up to <paramref name="mediumFrom"/> bees, medium alone from twice that, large
        /// fading in from <paramref name="largeFrom"/> and alone from twice that.
        /// </summary>
        public static void SwarmWeights(int bees, int mediumFrom, int largeFrom, out float small, out float medium, out float large)
        {
            float toMedium = Blend(bees, mediumFrom);
            float toLarge = Blend(bees, largeFrom);
            small = FadeOut(toMedium);
            medium = FadeIn(toMedium) * FadeOut(toLarge);
            large = FadeIn(toLarge);
        }

        private static float Blend(int count, int from) =>
            from <= 0 ? 1f : Clamp01((count - from) / (float)from);

        private static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }

    /// <summary>
    /// A run of quick repeats (a swipe shaking flower after flower, blooms in a row) that climbs
    /// one step per event and starts over after a pause or at the top.
    /// </summary>
    public struct PitchRun
    {
        private double lastTime;
        private int step;
        private bool started;

        /// <summary>Step for an event at <paramref name="now"/>: 0 after a pause longer than <paramref name="gapSeconds"/>.</summary>
        public int Next(double now, double gapSeconds, int steps)
        {
            if (!started || now - lastTime > gapSeconds || step + 1 >= steps)
                step = 0;
            else
                step++;
            started = true;
            lastTime = now;
            return step;
        }
    }

    /// <summary>Lets at most a given number of events through per second; the rest are dropped, never queued.</summary>
    public struct RateLimit
    {
        private double nextAllowed;

        public bool TryPass(double now, float perSecond)
        {
            if (perSecond <= 0f)
                return true;
            if (now < nextAllowed)
                return false;
            nextAllowed = now + 1.0 / perSecond;
            return true;
        }
    }
}
