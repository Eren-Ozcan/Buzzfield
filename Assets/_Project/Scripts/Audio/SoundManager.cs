using System;
using System.Collections.Generic;
using Buzzfield.Core;
using UnityEngine;

namespace Buzzfield.Audio
{
    /// <summary>
    /// Plays the library: one-shot effects on a small pool of voices (a request with no free
    /// voice, or over its rate, is dropped), stingers on their own voice with the music ducked,
    /// and the looping layers: music, the dry and bloom ambience crossfaded by the garden's bloom,
    /// and three swarm hums crossfaded by the bee count. Missing clips are silent, so the game
    /// runs the same before the sounds exist. Plain class: the game calls <see cref="Tick"/>.
    /// </summary>
    public sealed class SoundManager : IDisposable
    {
        /// <summary>Highest step of a rising run: blooms climb the pentatonic, shakes a semitone at a time.</summary>
        public const int RunSteps = 6;

        private static readonly int SfxCount = Enum.GetValues(typeof(Sfx)).Length;
        private static readonly int LoopCount = Enum.GetValues(typeof(SoundLoop)).Length;

        private readonly SoundLibrary library;
        private readonly SoundEntry[] entries;
        private readonly bool[] known;
        private readonly RateLimit[] limits;
        private readonly PitchRun[] runs;
        private readonly int[] played;
        private readonly AudioSource[] voices;
        private readonly AudioSource stingerVoice;
        private readonly AudioSource[] loopSources;
        private readonly float[] loopLevels;
        private readonly GameObject root;

        private float bloom;
        private int bees;
        private float duckTimer;
        private bool paused;

        public SoundManager(SoundLibrary library, Transform parent)
        {
            this.library = library;
            entries = new SoundEntry[SfxCount];
            known = new bool[SfxCount];
            limits = new RateLimit[SfxCount];
            runs = new PitchRun[SfxCount];
            played = new int[SfxCount];
            IReadOnlyList<SoundEntry> list = library.Effects;
            for (int i = 0; i < list.Count; i++)
            {
                int index = (int)list[i].id;
                if (index < 0 || index >= SfxCount)
                    continue;
                entries[index] = list[i];
                known[index] = true;
            }

            root = new GameObject("Sound");
            root.transform.SetParent(parent, false);
            voices = new AudioSource[library.EffectVoices];
            for (int i = 0; i < voices.Length; i++)
                voices[i] = CreateSource(false);
            stingerVoice = CreateSource(false);

            loopSources = new AudioSource[LoopCount];
            loopLevels = new float[LoopCount];
            IReadOnlyList<LoopEntry> loops = library.Loops;
            for (int i = 0; i < loops.Count; i++)
            {
                LoopEntry loop = loops[i];
                int index = (int)loop.id;
                if (index < 0 || index >= LoopCount || loop.clip == null)
                    continue;
                AudioSource source = CreateSource(true);
                source.clip = loop.clip;
                source.volume = 0f;
                // Start each loop somewhere else, so ambience and swarm never line up the same way.
                source.time = UnityEngine.Random.Range(0f, loop.clip.length * 0.9f);
                source.Play();
                loopSources[index] = source;
                loopLevels[index] = loop.volume;
            }
        }

        /// <summary>The music switch in the settings panel.</summary>
        public bool MusicOn { get; set; } = true;

        /// <summary>The sound switch: effects, interface, stingers, ambience and the swarm.</summary>
        public bool EffectsOn { get; set; } = true;

        /// <summary>
        /// Times <paramref name="id"/> was played, counted when it passed the sound switch and its
        /// rate (whether or not a clip exists); tests read it.
        /// </summary>
        public int PlayedCount(Sfx id) => played[(int)id];

        /// <summary>Current volume of a loop; zero when it has no clip.</summary>
        public float LoopVolume(SoundLoop loop) => loopSources[(int)loop] != null ? loopSources[(int)loop].volume : 0f;

        /// <summary>Plays <paramref name="id"/> at <paramref name="pitch"/> (before its random spread). False when it was dropped.</summary>
        public bool Play(Sfx id, float pitch = 1f)
        {
            int index = (int)id;
            if (!EffectsOn || paused || !known[index])
                return false;
            ref SoundEntry entry = ref entries[index];
            if (!limits[index].TryPass(Time.unscaledTimeAsDouble, entry.maxPerSecond))
                return false;
            played[index]++;

            AudioClip[] clips = entry.clips;
            if (clips == null || clips.Length == 0)
                return true;
            AudioClip clip = clips.Length == 1 ? clips[0] : clips[UnityEngine.Random.Range(0, clips.Length)];
            if (clip == null)
                return true;

            AudioSource voice = entry.bus == SoundBus.Stinger ? stingerVoice : FreeVoice();
            if (voice == null)
                return true;
            float jitter = entry.pitchJitter > 0f ? UnityEngine.Random.Range(-entry.pitchJitter, entry.pitchJitter) : 0f;
            voice.clip = clip;
            voice.pitch = pitch * (1f + jitter);
            voice.volume = entry.volume * (entry.bus == SoundBus.Ui ? library.UiVolume : library.EffectsVolume);
            voice.Play();
            if (entry.bus == SoundBus.Stinger)
                duckTimer = clip.length;
            return true;
        }

        /// <summary>Plays the next note of a rising run on the F major pentatonic (blooms in a row).</summary>
        public bool PlayNote(Sfx id) =>
            Play(id, SoundRules.PentatonicPitch(runs[(int)id].Next(Time.unscaledTimeAsDouble, library.RunGapSeconds, RunSteps)));

        /// <summary>Plays one semitone higher than the last within a quick run (flowers shaken by one swipe).</summary>
        public bool PlayClimb(Sfx id) =>
            Play(id, SoundRules.SemitonePitch(runs[(int)id].Next(Time.unscaledTimeAsDouble, library.RunGapSeconds, RunSteps)));

        /// <summary>The garden's bloom, 0..1: crossfades the dry ambience into the bloom ambience.</summary>
        public void SetBloom(float fraction) => bloom = Mathf.Clamp01(fraction);

        /// <summary>Bees in the garden: picks the swarm loops.</summary>
        public void SetBees(int count) => bees = Mathf.Max(0, count);

        /// <summary>A full-screen ad is on screen: everything pauses until it closes.</summary>
        public void SetPaused(bool value)
        {
            if (paused == value)
                return;
            paused = value;
            AudioListener.pause = value;
        }

        public void Tick(float unscaledDeltaTime)
        {
            if (duckTimer > 0f)
                duckTimer -= unscaledDeltaTime;

            float music = MusicOn ? (duckTimer > 0f ? library.StingerDuck : 1f) : 0f;
            float world = EffectsOn ? 1f : 0f;
            SoundRules.SwarmWeights(bees, library.SwarmMediumFrom, library.SwarmLargeFrom, out float small, out float medium, out float large);
            float swarm = bees > 0 ? world : 0f;

            float step = library.FadeSpeed * unscaledDeltaTime;
            Fade(SoundLoop.Music, music, step);
            Fade(SoundLoop.AmbienceDry, world * SoundRules.FadeOut(bloom), step);
            Fade(SoundLoop.AmbienceBloom, world * SoundRules.FadeIn(bloom), step);
            Fade(SoundLoop.SwarmSmall, swarm * small, step);
            Fade(SoundLoop.SwarmMedium, swarm * medium, step);
            Fade(SoundLoop.SwarmLarge, swarm * large, step);
        }

        public void Dispose()
        {
            if (paused)
                AudioListener.pause = false;
            paused = false;
            if (root != null)
                UnityEngine.Object.Destroy(root);
        }

        private void Fade(SoundLoop loop, float share, float step)
        {
            AudioSource source = loopSources[(int)loop];
            if (source == null)
                return;
            float target = share * loopLevels[(int)loop];
            if (!Mathf.Approximately(source.volume, target))
                source.volume = Mathf.MoveTowards(source.volume, target, step);
        }

        private AudioSource FreeVoice()
        {
            for (int i = 0; i < voices.Length; i++)
            {
                if (!voices[i].isPlaying)
                    return voices[i];
            }
            return null;
        }

        private AudioSource CreateSource(bool loop)
        {
            AudioSource source = root.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
