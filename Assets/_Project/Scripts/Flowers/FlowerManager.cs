using System;
using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>
    /// Spawns the flowers of a garden, regenerates their nectar in one loop and hands out
    /// targets to bees. A plain list scan is enough: a garden stays under ~60 flowers.
    /// </summary>
    public sealed class FlowerManager : MonoBehaviour
    {
        private readonly List<Flower> flowers = new List<Flower>(64);
        private Transform flowerRoot;

        public IReadOnlyList<Flower> Flowers => flowers;

        /// <summary>Raised after a bee takes nectar: flower and units taken (bloom hooks in here).</summary>
        public event Action<Flower, double> OnNectarCollected;

        /// <param name="valueMultiplier">Nectar value factor of this garden (its own multiplier plus any loop bonus).</param>
        public void Init(GardenConfig garden, float valueMultiplier, Transform parent)
        {
            Clear();
            flowerRoot = new GameObject("Flowers").transform;
            flowerRoot.SetParent(parent, false);

            IReadOnlyList<FlowerSlot> slots = garden.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                FlowerSlot slot = slots[i];
                if (slot.type == null || slot.type.Prefab == null)
                {
                    Debug.LogError($"Garden '{garden.name}' slot {i} has no flower type or prefab.", garden);
                    continue;
                }

                FlowerView view = Instantiate(slot.type.Prefab, GardenConfig.ToWorld(slot.position), Quaternion.identity, flowerRoot);
                view.name = $"{slot.type.name}_{i:00}";
                // Sprout slots stay hidden until the bloom system activates them.
                view.gameObject.SetActive(slot.startsActive);
                flowers.Add(new Flower(i, slot.type, view, slot.position, valueMultiplier, slot.startsActive));
            }
        }

        /// <summary>
        /// Loads one slot's saved state; call after <see cref="Init"/> and before the bloom
        /// system loads the garden. Out-of-range slots (changed garden layout) are ignored.
        /// </summary>
        public void RestoreSlot(int slotIndex, bool active, float bloom, double nectar)
        {
            if (slotIndex < 0 || slotIndex >= flowers.Count)
                return;
            Flower flower = flowers[slotIndex];
            // A bloomed flower is always active; a starting flower never goes back to a sprout.
            bool isActive = active || flower.IsActive || bloom >= 1f;
            flower.IsActive = isActive;
            flower.View.gameObject.SetActive(isActive);
            flower.Bloom = float.IsNaN(bloom) ? 0f : Mathf.Clamp01(bloom);
            flower.SetNectar(nectar);
        }

        /// <summary>Regrows nectar on every active flower for time spent away.</summary>
        public void RegenerateFor(double seconds)
        {
            if (seconds <= 0)
                return;
            for (int i = 0; i < flowers.Count; i++)
            {
                Flower flower = flowers[i];
                if (flower.IsActive)
                    flower.SetNectar(flower.Nectar + flower.Type.RegenPerSecond * seconds);
            }
        }

        public void Tick(float deltaTime)
        {
            for (int i = 0; i < flowers.Count; i++)
            {
                Flower flower = flowers[i];
                if (flower.IsActive)
                    flower.Regenerate(deltaTime);
            }
        }

        /// <summary>
        /// Extra distance counted for an already bloomed flower, so bees spread the bloom
        /// outward instead of feeding on the flowers nearest the hive forever.
        /// </summary>
        public float BloomedTargetPenalty { get; set; }

        /// <summary>
        /// Nearest flower that offers a bee of <paramref name="capacity"/> a worthwhile load
        /// (<see cref="Flower.OffersLoad"/>), with bloomed flowers pushed back by
        /// <see cref="BloomedTargetPenalty"/>; null if none does. Does not reserve it.
        /// </summary>
        public Flower FindTarget(Vector3 from, float capacity, float loadFraction)
        {
            Flower best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < flowers.Count; i++)
            {
                Flower flower = flowers[i];
                if (!flower.OffersLoad(capacity, loadFraction))
                    continue;
                float distance = (flower.NectarPoint - from).magnitude;
                if (flower.IsBloomed)
                    distance += BloomedTargetPenalty;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = flower;
                }
            }
            return best;
        }

        /// <summary>Wakes a sprout slot: it shows up and bees may visit it. Returns false if it was already active.</summary>
        public bool Activate(Flower flower)
        {
            if (flower.IsActive)
                return false;
            flower.IsActive = true;
            flower.View.gameObject.SetActive(true);
            return true;
        }

        /// <summary>Sum of <see cref="Flower.Value"/> over the active flowers.</summary>
        public double SumActiveValue()
        {
            double sum = 0;
            for (int i = 0; i < flowers.Count; i++)
            {
                if (flowers[i].IsActive)
                    sum += flowers[i].Value;
            }
            return sum;
        }

        public void Reserve(Flower flower) => flower.Reserve();

        public void Release(Flower flower) => flower.Release();

        /// <summary>Takes up to <paramref name="capacity"/> units; returns what was taken.</summary>
        public double Collect(Flower flower, double capacity)
        {
            double taken = flower.Take(capacity);
            if (taken > 0)
                OnNectarCollected?.Invoke(flower, taken);
            return taken;
        }

        private void Clear()
        {
            flowers.Clear();
            if (flowerRoot != null)
                Destroy(flowerRoot.gameObject);
        }
    }
}
