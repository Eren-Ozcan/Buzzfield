using System;
using System.Collections.Generic;
using Buzzfield.Core;
using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>
    /// Pollen shake: a swipe across a flower that has pollen wiggles it, puffs pollen and
    /// starts its cooldown; the game pays the honey and bloom through <see cref="OnFlowerShaken"/>.
    /// Flowers with pollen let drifting motes rise, so the player can see which ones are ready.
    /// Plain class: GameManager feeds it strokes and calls <see cref="Tick"/>.
    /// </summary>
    public sealed class PollenShaker
    {
        private readonly PollenSettings settings;
        private readonly FlowerManager flowerManager;
        private readonly Tweener tweener;
        private readonly ParticlePool puffs;
        private readonly ParticlePool motes;

        /// <param name="effectsRoot">Parent of the pollen particle systems; outlives every garden.</param>
        public PollenShaker(PollenSettings settings, FlowerManager flowerManager, Tweener tweener, Transform effectsRoot)
        {
            this.settings = settings;
            this.flowerManager = flowerManager;
            this.tweener = tweener;
            if (settings.PuffPrefab != null)
                puffs = new ParticlePool(settings.PuffPrefab, effectsRoot, 1);
            if (settings.MotePrefab != null)
                motes = new ParticlePool(settings.MotePrefab, effectsRoot, 1);
        }

        /// <summary>Raised for every flower a swipe shook, after its cooldown started.</summary>
        public event Action<Flower> OnFlowerShaken;

        /// <summary>
        /// Shakes every flower with pollen that a stroke between two screen rays passes over,
        /// measured on the plane at the flower heads. Returns how many were shaken.
        /// </summary>
        public int Stroke(Ray from, Ray to)
        {
            var plane = new Plane(Vector3.up, new Vector3(0f, settings.HitPlaneHeight, 0f));
            if (!plane.Raycast(from, out float fromDistance) || !plane.Raycast(to, out float toDistance))
                return 0;
            return Stroke(from.GetPoint(fromDistance), to.GetPoint(toDistance));
        }

        /// <summary>Shakes every flower with pollen within the hit radius of the world segment (XZ only).</summary>
        public int Stroke(Vector3 from, Vector3 to)
        {
            int shaken = 0;
            IReadOnlyList<Flower> flowers = flowerManager.Flowers;
            for (int i = 0; i < flowers.Count; i++)
            {
                Flower flower = flowers[i];
                if (!flower.HasPollen)
                    continue;
                Vector3 point = flower.NectarPoint;
                if (!PollenMath.StrokeHits(from.x, from.z, to.x, to.z, point.x, point.z, settings.HitRadius))
                    continue;
                Shake(flower);
                shaken++;
            }
            return shaken;
        }

        /// <summary>Shakes every flower that has pollen, as one perfect sweep would. Returns how many were shaken.</summary>
        public int ShakeAll()
        {
            int shaken = 0;
            IReadOnlyList<Flower> flowers = flowerManager.Flowers;
            for (int i = 0; i < flowers.Count; i++)
            {
                if (!flowers[i].HasPollen)
                    continue;
                Shake(flowers[i]);
                shaken++;
            }
            return shaken;
        }

        /// <summary>Runs the cooldowns and lets flowers with pollen emit their motes.</summary>
        public void Tick(float deltaTime)
        {
            IReadOnlyList<Flower> flowers = flowerManager.Flowers;
            for (int i = 0; i < flowers.Count; i++)
            {
                Flower flower = flowers[i];
                if (!flower.IsActive)
                    continue;
                if (flower.PollenCooldown > 0f)
                {
                    flower.PollenCooldown = Mathf.Max(0f, flower.PollenCooldown - deltaTime);
                    continue;
                }
                if (motes == null)
                    continue;
                flower.MoteTimer -= deltaTime;
                if (flower.MoteTimer > 0f)
                    continue;
                // Jittered so the motes of a whole garden never rise in step.
                flower.MoteTimer = settings.MoteInterval * UnityEngine.Random.Range(0.5f, 1.5f);
                motes.Emit(flower.NectarPoint, 1);
            }
        }

        private void Shake(Flower flower)
        {
            flower.PollenCooldown = settings.CooldownSeconds;
            // The first mote rises the moment the pollen is back.
            flower.MoteTimer = 0f;
            tweener.Wiggle(flower.View.transform, settings.WiggleDegrees, settings.WiggleCycles, settings.WiggleDuration);
            puffs?.Emit(flower.NectarPoint, settings.PuffParticles);
            OnFlowerShaken?.Invoke(flower);
        }
    }
}
