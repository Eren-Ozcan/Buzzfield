using System;
using System.Collections.Generic;
using Buzzfield.Core;
using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>
    /// Garden bloom (design doc section 3): every nectar collection adds bloom to its
    /// flower; at 100% the flower turns from grey to its colour with a pop and a burst,
    /// and wakes the nearest sprout slots. Also owns the ground tiles, the garden bloom
    /// fraction and the Garden Complete moment. GameManager calls <see cref="Tick"/>.
    /// </summary>
    public sealed class GardenBloomManager : IDisposable
    {
        private readonly BloomSettings settings;
        private readonly FlowerManager flowerManager;
        private readonly List<Flower> animating = new List<Flower>(64);

        private GardenConfig garden;
        private GroundView ground;
        private Color32[] tileColors;
        private ParticleSystem[] bursts;
        private int nextBurst;
        private ParticleSystem confetti;
        private int bloomedCount;
        private bool groundDirty;

        public GardenBloomManager(BloomSettings settings, FlowerManager flowerManager)
        {
            this.settings = settings;
            this.flowerManager = flowerManager;
            flowerManager.OnNectarCollected += HandleNectarCollected;
        }

        /// <summary>Scales every flower's bloom per visit (the Pollen Touch Queen ability).</summary>
        public float BloomPerVisitMultiplier { get; set; } = 1f;

        public int BloomedCount => bloomedCount;
        public int TotalSlots => flowerManager.Flowers.Count;
        public float Fraction => BloomMath.GardenFraction(bloomedCount, TotalSlots);
        public int Percent => BloomMath.DisplayPercent(bloomedCount, TotalSlots);
        public bool IsComplete { get; private set; }

        public event Action<Flower> OnFlowerBloomed;
        public event Action<Flower> OnSproutActivated;
        /// <summary>Raised when a flower blooms and after a garden loads; read <see cref="Fraction"/> or <see cref="Percent"/>.</summary>
        public event Action OnBloomChanged;
        public event Action OnGardenCompleted;

        /// <summary>Takes over the garden that FlowerManager and GardenSpawner just built.</summary>
        public void Load(GardenConfig config, GardenInstance instance)
        {
            garden = config;
            ground = instance.Ground;
            animating.Clear();
            bloomedCount = 0;
            IsComplete = false;

            IReadOnlyList<Flower> flowers = flowerManager.Flowers;
            for (int i = 0; i < flowers.Count; i++)
            {
                Flower flower = flowers[i];
                flower.Animation = FlowerAnimation.None;
                flower.View.InitVisuals(HeadColor(flower));
                if (flower.IsBloomed)
                    bloomedCount++;
            }

            Vector2Int grid = config.TileGrid;
            tileColors = new Color32[Mathf.Max(1, grid.x) * Mathf.Max(1, grid.y)];
            CreateEffects(instance.Root);
            RefreshGround();
            groundDirty = false;
            IsComplete = TotalSlots > 0 && bloomedCount >= TotalSlots;
            OnBloomChanged?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            if (groundDirty)
            {
                // Many collections can land in one frame; the tiles are rebuilt once.
                groundDirty = false;
                RefreshGround();
            }

            for (int i = animating.Count - 1; i >= 0; i--)
            {
                Flower flower = animating[i];
                if (Animate(flower, deltaTime))
                    continue;
                flower.Animation = FlowerAnimation.None;
                int last = animating.Count - 1;
                animating[i] = animating[last];
                animating.RemoveAt(last);
            }
        }

        public void Dispose()
        {
            flowerManager.OnNectarCollected -= HandleNectarCollected;
        }

        private void HandleNectarCollected(Flower flower, double taken)
        {
            if (flower.IsBloomed)
                return;
            flower.Bloom = BloomMath.AddVisit(flower.Bloom, flower.Type.BloomPerVisit, BloomPerVisitMultiplier);
            groundDirty = true;
            if (flower.IsBloomed)
                BloomFlower(flower);
            else
                flower.View.SetHeadColor(HeadColor(flower));
        }

        private void BloomFlower(Flower flower)
        {
            bloomedCount++;
            StartAnimation(flower, FlowerAnimation.Bloom, 0f);
            PlayBurst(flower);
            ActivateNearestSprouts(flower);

            OnFlowerBloomed?.Invoke(flower);
            OnBloomChanged?.Invoke();
            if (!IsComplete && bloomedCount >= TotalSlots)
                CompleteGarden();
        }

        private void ActivateNearestSprouts(Flower source)
        {
            IReadOnlyList<Flower> flowers = flowerManager.Flowers;
            for (int k = 0; k < settings.SproutsPerBloom; k++)
            {
                Flower nearest = null;
                float nearestDistance = float.MaxValue;
                for (int i = 0; i < flowers.Count; i++)
                {
                    Flower candidate = flowers[i];
                    if (candidate.IsActive)
                        continue;
                    float distance = (candidate.GardenPosition - source.GardenPosition).sqrMagnitude;
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = candidate;
                    }
                }
                if (nearest == null)
                    return;

                flowerManager.Activate(nearest);
                nearest.View.SetScale(0f);
                StartAnimation(nearest, FlowerAnimation.Sprout, 0f);
                OnSproutActivated?.Invoke(nearest);
            }
        }

        private void CompleteGarden()
        {
            IsComplete = true;
            if (confetti != null)
            {
                confetti.Clear(true);
                confetti.Play(true);
            }

            // Every flower pulses once the last bloom pop is over, in a wave out from the hive.
            IReadOnlyList<Flower> flowers = flowerManager.Flowers;
            Vector2 hive = garden.HivePosition;
            for (int i = 0; i < flowers.Count; i++)
            {
                Flower flower = flowers[i];
                float delay = settings.BloomDuration + (flower.GardenPosition - hive).magnitude * 0.04f;
                StartAnimation(flower, FlowerAnimation.Pulse, -delay);
            }
            OnGardenCompleted?.Invoke();
        }

        private void StartAnimation(Flower flower, FlowerAnimation animation, float startTime)
        {
            // A bloom cut short still has to end on the full colour.
            if (flower.Animation == FlowerAnimation.Bloom)
                flower.View.SetHeadColor(flower.Type.BloomedColor);
            if (flower.Animation == FlowerAnimation.None)
                animating.Add(flower);
            flower.Animation = animation;
            flower.AnimationTime = startTime;
        }

        /// <summary>Advances one flower's animation; false once it has finished.</summary>
        private bool Animate(Flower flower, float deltaTime)
        {
            flower.AnimationTime += deltaTime;
            float time = flower.AnimationTime;
            if (time < 0f)
                return true;

            FlowerView view = flower.View;
            switch (flower.Animation)
            {
                case FlowerAnimation.Sprout:
                {
                    float t = Mathf.Clamp01(time / settings.SproutGrowDuration);
                    view.SetScale(EaseOutBack(t));
                    return t < 1f;
                }
                case FlowerAnimation.Bloom:
                {
                    float t = Mathf.Clamp01(time / settings.BloomDuration);
                    Color from = Color.Lerp(settings.UnbloomedHeadColor, flower.Type.BloomedColor, settings.ProgressTint);
                    view.SetHeadColor(Color.Lerp(from, flower.Type.BloomedColor, t));
                    view.SetScale(1f + settings.BloomScalePop * Mathf.Sin(t * Mathf.PI));
                    return t < 1f;
                }
                case FlowerAnimation.Pulse:
                {
                    float t = Mathf.Clamp01(time / settings.CompletePulseDuration);
                    view.SetScale(1f + settings.CompletePulseScale * Mathf.Sin(t * Mathf.PI));
                    return t < 1f;
                }
                default:
                    return false;
            }
        }

        private Color HeadColor(Flower flower)
        {
            if (flower.IsBloomed)
                return flower.Type.BloomedColor;
            return Color.Lerp(settings.UnbloomedHeadColor, flower.Type.BloomedColor, flower.Bloom * settings.ProgressTint);
        }

        /// <summary>Tile green = strongest nearby flower influence, never below the garden-wide share.</summary>
        private void RefreshGround()
        {
            if (ground == null || garden == null)
                return;

            IReadOnlyList<Flower> flowers = flowerManager.Flowers;
            Vector2Int grid = garden.TileGrid;
            float radius = garden.BloomInfluenceRadius;
            float floor = Fraction * settings.GardenWideGreen;
            Color grey = settings.GroundGrey;
            Color green = settings.GroundGreen;

            for (int y = 0; y < grid.y; y++)
            for (int x = 0; x < grid.x; x++)
            {
                Vector2 center = garden.TileCenter(x, y);
                float green01 = floor;
                for (int i = 0; i < flowers.Count; i++)
                {
                    Flower flower = flowers[i];
                    if (flower.Bloom <= 0f)
                        continue;
                    float influence = BloomMath.Influence(flower.Bloom, (flower.GardenPosition - center).sqrMagnitude, radius);
                    if (influence > green01)
                        green01 = influence;
                }
                tileColors[y * grid.x + x] = Color.Lerp(grey, green, green01);
            }
            ground.SetTiles(tileColors);
        }

        private void CreateEffects(Transform root)
        {
            nextBurst = 0;
            bursts = Array.Empty<ParticleSystem>();
            confetti = null;
            if (settings.BloomBurstPrefab != null)
            {
                bursts = new ParticleSystem[settings.BurstPoolSize];
                for (int i = 0; i < bursts.Length; i++)
                {
                    bursts[i] = UnityEngine.Object.Instantiate(settings.BloomBurstPrefab, root);
                    bursts[i].name = $"BloomBurst_{i}";
                }
            }
            if (settings.ConfettiPrefab != null)
            {
                confetti = UnityEngine.Object.Instantiate(settings.ConfettiPrefab, root);
                confetti.name = "Confetti";
                Vector2 size = garden.GroundSize;
                confetti.transform.localPosition = new Vector3(0f, 4f, 0f);
                ParticleSystem.ShapeModule shape = confetti.shape;
                shape.scale = new Vector3(size.x, 0.5f, size.y);
            }
        }

        private void PlayBurst(Flower flower)
        {
            if (bursts.Length == 0)
                return;
            ParticleSystem burst = bursts[nextBurst];
            nextBurst = (nextBurst + 1) % bursts.Length;
            burst.transform.position = flower.NectarPoint;
            ParticleSystem.MainModule main = burst.main;
            main.startColor = flower.Type.BloomedColor;
            burst.Clear(true);
            burst.Play(true);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
