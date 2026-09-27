using System;
using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>Runtime state of one flower slot. Owned and ticked by <see cref="FlowerManager"/>.</summary>
    public sealed class Flower
    {
        internal Flower(int slotIndex, FlowerType type, FlowerView view, float valueMultiplier, bool active)
        {
            SlotIndex = slotIndex;
            Type = type;
            View = view;
            Value = type.NectarValue * valueMultiplier;
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
