using Buzzfield.Core;
using NUnit.Framework;

namespace Buzzfield.Tests.EditMode
{
    public class SoundRulesTests
    {
        [Test]
        public void Pentatonic_ClimbsFGACD_ThenTheOctave()
        {
            float[] expectedSemitones = { 0f, 2f, 4f, 7f, 9f, 12f, 14f };
            for (int step = 0; step < expectedSemitones.Length; step++)
                Assert.That(SoundRules.PentatonicPitch(step), Is.EqualTo(SoundRules.SemitonePitch(expectedSemitones[step])).Within(1e-5f), $"step {step}");
        }

        [Test]
        public void Pentatonic_NegativeStep_IsTheRoot()
        {
            Assert.That(SoundRules.PentatonicPitch(-2), Is.EqualTo(1f));
        }

        [Test]
        public void Octave_DoublesThePitch()
        {
            Assert.That(SoundRules.SemitonePitch(12f), Is.EqualTo(2f).Within(1e-5f));
            Assert.That(SoundRules.SemitonePitch(-12f), Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void Gain_SixDecibelsDown_IsAboutHalf()
        {
            Assert.That(SoundRules.Gain(0f), Is.EqualTo(1f));
            Assert.That(SoundRules.Gain(-6f), Is.EqualTo(0.501f).Within(0.001f));
        }

        [Test]
        public void Crossfade_KeepsEqualPower()
        {
            for (float t = 0f; t <= 1f; t += 0.1f)
            {
                float a = SoundRules.FadeOut(t);
                float b = SoundRules.FadeIn(t);
                Assert.That(a * a + b * b, Is.EqualTo(1f).Within(1e-4f), $"t {t}");
            }
            Assert.That(SoundRules.FadeIn(-1f), Is.Zero);
            Assert.That(SoundRules.FadeIn(2f), Is.EqualTo(1f).Within(1e-6f));
        }

        [TestCase(1, 1f, 0f, 0f)]
        [TestCase(10, 1f, 0f, 0f)]
        [TestCase(20, 0f, 1f, 0f)]
        [TestCase(50, 0f, 1f, 0f)]
        [TestCase(100, 0f, 0f, 1f)]
        [TestCase(150, 0f, 0f, 1f)]
        public void Swarm_OneLoopAlone_AtTheEnds(int bees, float small, float medium, float large)
        {
            SoundRules.SwarmWeights(bees, 10, 50, out float s, out float m, out float l);
            Assert.That(s, Is.EqualTo(small).Within(1e-4f));
            Assert.That(m, Is.EqualTo(medium).Within(1e-4f));
            Assert.That(l, Is.EqualTo(large).Within(1e-4f));
        }

        [Test]
        public void Swarm_BlendsBetweenLoops()
        {
            SoundRules.SwarmWeights(15, 10, 50, out float s, out float m, out float l);
            Assert.That(s, Is.GreaterThan(0f));
            Assert.That(m, Is.GreaterThan(0f));
            Assert.That(l, Is.Zero);
        }

        [Test]
        public void PitchRun_ClimbsWhileQuick_AndRestartsAfterAPause()
        {
            var run = new PitchRun();
            Assert.That(run.Next(0.0, 0.5, 8), Is.EqualTo(0));
            Assert.That(run.Next(0.2, 0.5, 8), Is.EqualTo(1));
            Assert.That(run.Next(0.4, 0.5, 8), Is.EqualTo(2));
            Assert.That(run.Next(1.5, 0.5, 8), Is.EqualTo(0), "A pause starts over.");
        }

        [Test]
        public void PitchRun_StartsOver_AtTheTop()
        {
            var run = new PitchRun();
            for (int i = 0; i < 3; i++)
                Assert.That(run.Next(i * 0.1, 1.0, 3), Is.EqualTo(i));
            Assert.That(run.Next(0.3, 1.0, 3), Is.EqualTo(0));
        }

        [Test]
        public void RateLimit_DropsWhatComesTooSoon()
        {
            var limit = new RateLimit();
            Assert.That(limit.TryPass(0.0, 4f), Is.True);
            Assert.That(limit.TryPass(0.1, 4f), Is.False);
            Assert.That(limit.TryPass(0.25, 4f), Is.True);
            Assert.That(limit.TryPass(0.3, 0f), Is.True, "Zero means no limit.");
        }

        [Test]
        public void Haptics_FollowTheSetting()
        {
            Assert.That(HapticRules.Plays(VibrationMode.Off, true), Is.False);
            Assert.That(HapticRules.Plays(VibrationMode.Important, false), Is.False);
            Assert.That(HapticRules.Plays(VibrationMode.Important, true), Is.True);
            Assert.That(HapticRules.Plays(VibrationMode.All, false), Is.True);
        }
    }
}
