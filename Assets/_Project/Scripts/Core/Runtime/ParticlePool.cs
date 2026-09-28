using UnityEngine;

namespace Buzzfield.Core
{
    /// <summary>
    /// A fixed ring of instances of one particle prefab, created once. <see cref="Play"/> restarts
    /// the oldest instance at a new spot; <see cref="Emit"/> adds a few particles to the next
    /// instance without cutting the ones already flying, for effects that fire many times a second.
    /// </summary>
    public sealed class ParticlePool
    {
        private readonly ParticleSystem[] instances;
        private int next;

        public ParticlePool(ParticleSystem prefab, Transform parent, int size)
        {
            instances = new ParticleSystem[Mathf.Max(1, size)];
            for (int i = 0; i < instances.Length; i++)
            {
                instances[i] = Object.Instantiate(prefab, parent);
                instances[i].name = $"{prefab.name}_{i}";
                WarmUp(instances[i]);
            }
        }

        public int Size => instances.Length;

        public ParticleSystem Play(Vector3 position)
        {
            ParticleSystem instance = Next();
            instance.transform.position = position;
            instance.Clear(true);
            instance.Play(true);
            return instance;
        }

        /// <summary>Restarts the next instance tinted with <paramref name="color"/>.</summary>
        public ParticleSystem Play(Vector3 position, Color color)
        {
            ParticleSystem instance = Next();
            ParticleSystem.MainModule main = instance.main;
            main.startColor = color;
            instance.transform.position = position;
            instance.Clear(true);
            instance.Play(true);
            return instance;
        }

        /// <summary>Emits <paramref name="particles"/> at <paramref name="position"/> with the prefab's shape and start values.</summary>
        public void Emit(Vector3 position, int particles)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                applyShapeToPosition = true,
            };
            Next().Emit(emit, particles);
        }

        public void Clear()
        {
            for (int i = 0; i < instances.Length; i++)
                instances[i].Clear(true);
        }

        /// <summary>
        /// The first start colour write, Emit, Play and Clear on each instance allocate once
        /// (runtime binding and per-system caches); pay that while loading instead of on the
        /// first blooms or deposits. A looping system that plays on awake keeps playing.
        /// </summary>
        private static void WarmUp(ParticleSystem instance)
        {
            bool wasPlaying = instance.isPlaying;
            ParticleSystem.MainModule main = instance.main;
            main.startColor = main.startColor;
            instance.Play(true);
            instance.Emit(new ParticleSystem.EmitParams { position = instance.transform.position, applyShapeToPosition = true }, 1);
            instance.Clear(true);
            if (!wasPlaying)
                instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private ParticleSystem Next()
        {
            ParticleSystem instance = instances[next];
            next = (next + 1) % instances.Length;
            return instance;
        }
    }
}
