using System;
using UnityEngine;

namespace Buzzfield.Flowers
{
    internal enum FlowerAnimation
    {
        None,
        Sprout,
        Bloom,
        Pulse,
    }

    /// <summary>Runtime state of one flower slot. Owned and ticked by <see cref="FlowerManager"/>.</summary>
    public sealed class Flower
    {
        internal Flower(int slotIndex, FlowerType type, FlowerView view, Vector2 gardenPosition, float valueMultiplier, bool active)
        {
            SlotIndex = slotIndex;
            Type = type;
            View = view;
            Value = type.NectarValue * valueMultiplier;
            GardenPosition = gardenPosition;
            IsActive = active;
            Nectar = type.MaxNectar;
            NectarPoint = view.NectarPoint;
        }

        public int SlotIndex { get; }
        public FlowerType Type { get; }
        public FlowerView View { get; }

        /// <summary>Honey per nectar unit in this garden, before player multipliers.</summary>
        public double Value { get; }

        public Vector3 NectarPoint { get; }
        public bool IsActive { get; internal set; }
        public double Nectar { get; private set; }
        public int AssignedBees { get; private set; }

        /// <summary>Bloom progress 0..1; grows with every collection and never goes back.</summary>
        public float Bloom { get; internal set; }
        public bool IsBloomed => Bloom >= 1f;
        public Vector2 GardenPosition { get; }

        // Animation state, driven by GardenBloomManager.
        internal FlowerAnimation Animation;
        internal float AnimationTime;

        /// <summary>At least one whole unit is left to collect.</summary>
        public bool HasNectar => IsActive && Nectar >= 1;

        public bool IsAvailable => HasNectar && AssignedBees < Type.MaxBeesTargeting;

        internal void Reserve() => AssignedBees++;

        internal void Release() => AssignedBees = Math.Max(0, AssignedBees - 1);

        internal void Regenerate(float deltaTime)
        {
            if (Nectar < Type.MaxNectar)
                Nectar = Math.Min(Type.MaxNectar, Nectar + Type.RegenPerSecond * deltaTime);
        }

        internal double Take(double capacity)
        {
            double taken = Math.Min(capacity, Nectar);
            Nectar -= taken;
            return taken;
        }
    }
}
