using Buzzfield.Flowers;
using UnityEngine;

namespace Buzzfield.Bees
{
    public enum BeeState
    {
        Idle,
        FlyToFlower,
        Collecting,
        ReturnToHive,
        Depositing,
        /// <summary>Flying to a merge point to become one bee of the next tier (Evolve).</summary>
        Merging,
    }

    /// <summary>Runtime state of one bee. Plain data; <see cref="BeeManager"/> ticks every bee in one loop.</summary>
    public sealed class Bee
    {
        internal Bee(int tierIndex, BeeTier tier, GameObject instance, Vector3 position, float bobPhase)
        {
            TierIndex = tierIndex;
            Tier = tier;
            Instance = instance;
            Transform = instance.transform;
            Position = position;
            Rotation = Transform.rotation;
            BobPhase = bobPhase;
        }

        public int TierIndex { get; }
        public BeeTier Tier { get; }
        public BeeState State { get; internal set; }

        /// <summary>Nectar units carried back to the hive.</summary>
        public double Carried { get; internal set; }

        internal GameObject Instance { get; }
        internal Transform Transform { get; }

        /// <summary>Flight position without the visual bobbing.</summary>
        internal Vector3 Position;
        internal Quaternion Rotation;
        internal Vector3 HoverPoint;
        internal Flower Target;
        internal double CarriedValue;
        internal float Timer;
        internal readonly float BobPhase;
    }
}
