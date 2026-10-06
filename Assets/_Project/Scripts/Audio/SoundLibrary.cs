using System;
using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Audio
{
    /// <summary>One effect: its clips (a random one plays), level and how often it may sound.</summary>
    [Serializable]
    public struct SoundEntry
    {
        public Sfx id;
        public SoundBus bus;
        [Tooltip("Variants; one is picked at random. Empty: the sound is silent.")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume;
        [Tooltip("Plays per second at most; extra requests are dropped. 0: no limit.")]
        [Min(0f)] public float maxPerSecond;
        [Tooltip("Random pitch spread, as a fraction (0.06 = +-6%).")]
        [Range(0f, 0.5f)] public float pitchJitter;
    }

    /// <summary>A looping layer: music, ambience or the swarm hum.</summary>
    [Serializable]
    public struct LoopEntry
    {
        public SoundLoop id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    /// <summary>
    /// Every sound and its mix (docs/audio.md). Clips are linked by file name from
    /// Assets/_Project/Audio by the editor (Buzzfield > Audio > Link Clips, and on import), so
    /// adding a file needs no manual wiring. A sound without clips is silent.
    /// </summary>
    [CreateAssetMenu(menuName = "Buzzfield/Sound Library", fileName = "SoundLibrary")]
    public sealed class SoundLibrary : ScriptableObject
    {
        [SerializeField] private List<SoundEntry> effects = new List<SoundEntry>();
        [SerializeField] private List<LoopEntry> loops = new List<LoopEntry>();

        [Header("Mix")]
        [Tooltip("Effect voices that can sound at once; a request with no free voice is dropped.")]
        [SerializeField, Range(1, 16)] private int effectVoices = 8;
        [SerializeField, Range(0f, 1f)] private float effectsVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.7f;
        [Tooltip("Music level while a stinger plays, as a share of its normal level.")]
        [SerializeField, Range(0f, 1f)] private float stingerDuck = 0.5f;
        [Tooltip("How fast loop levels follow their targets, in volume per second.")]
        [SerializeField, Min(0.1f)] private float fadeSpeed = 1.5f;

        [Header("Swarm")]
        [Tooltip("Bees at which the medium swarm loop starts to fade in (it is alone from twice this).")]
        [SerializeField, Min(1)] private int swarmMediumFrom = 10;
        [Tooltip("Bees at which the large swarm loop starts to fade in (it is alone from twice this).")]
        [SerializeField, Min(1)] private int swarmLargeFrom = 50;

        [Header("Runs")]
        [Tooltip("A pause longer than this ends a run of rising notes (blooms, shakes).")]
        [SerializeField, Min(0.05f)] private float runGapSeconds = 0.6f;

        public IReadOnlyList<SoundEntry> Effects => effects;
        public IReadOnlyList<LoopEntry> Loops => loops;
        public int EffectVoices => effectVoices;
        public float EffectsVolume => effectsVolume;
        public float UiVolume => uiVolume;
        public float StingerDuck => stingerDuck;
        public float FadeSpeed => fadeSpeed;
        public int SwarmMediumFrom => swarmMediumFrom;
        public int SwarmLargeFrom => swarmLargeFrom;
        public float RunGapSeconds => runGapSeconds;
    }
}
