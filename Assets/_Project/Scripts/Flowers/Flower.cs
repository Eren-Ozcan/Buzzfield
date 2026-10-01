using System;
using Buzzfield.Core;
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

        /// <summary>Seconds until a swipe can shake pollen out of this flower again; 0 when it can.</summary>
        public float PollenCooldown { get; internal set; }

        // Seconds to the next drifting pollen mote while the flower has pollen; driven by PollenShaker.
        internal float MoteTimer;

        /// <summary>At least one whole unit is left to collect.</summary>
        public bool HasNectar => IsActive && Nectar >= 1;

        /// <summary>A swipe across this flower shakes pollen out of it now.</summary>
        public bool HasPollen => IsActive && PollenCooldown <= 0f;

        public bool IsAvailable => HasNectar && AssignedBees < Type.MaxBeesTargeting;

        /// <summary>
        /// Available, and the nectar left after the bees already on their way still makes a
        /// worthwhile load for a bee of this <paramref name="capacity"/> (see <see cref="ForagingMath"/>).
        /// </summary>
        public bool OffersLoad(float capacity, float loadFraction) =>
            IsAvailable && ForagingMath.OffersLoad(Nectar, Type.MaxNectar, AssignedBees, capacity, loadFraction);

        internal void Reserve() => AssignedBees++;

        internal void Release() => AssignedBees = Math.Max(0, AssignedBees - 1);

        internal void Regenerate(float deltaTime)
        {
            if (Nectar < Type.MaxNectar)
                Nectar = Math.Min(Type.MaxNectar, Nectar + Type.RegenPerSecond * deltaTime);
        }

        /// <summary>Loads saved nectar, clamped to the flower's range.</summary>
        internal void SetNectar(double nectar) =>
            Nectar = double.IsNaN(nectar) ? Type.MaxNectar : Math.Max(0, Math.Min(Type.MaxNectar, nectar));

        internal double Take(double capacity)
        {
            double taken = Math.Min(capacity, Nectar);
            Nectar -= taken;
            return taken;
        }
    }
}
