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

        public void Init(GardenConfig garden, Transform parent)
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
                flowers.Add(new Flower(i, slot.type, view, garden.GardenValueMultiplier, slot.startsActive));
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

        /// <summary>Nearest flower a bee may fly to, or null. Does not reserve it.</summary>
        public Flower FindNearestAvailable(Vector3 from)
        {
            Flower best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < flowers.Count; i++)
            {
                Flower flower = flowers[i];
                if (!flower.IsAvailable)
                    continue;
                float distance = (flower.NectarPoint - from).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = flower;
                }
            }
            return best;
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
