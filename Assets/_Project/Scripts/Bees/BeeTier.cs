using UnityEngine;

namespace Buzzfield.Bees
{
    /// <summary>Balance and visuals of one bee tier (Worker, Forager, Golden).</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Bee Tier", fileName = "BeeTier")]
    public sealed class BeeTier : ScriptableObject
    {
        [Tooltip("Flight speed in world units per second.")]
        [SerializeField, Min(0.1f)] private float speed = 3f;
        [Tooltip("Nectar units carried per trip.")]
        [SerializeField, Min(0.1f)] private float capacity = 2f;
        [Tooltip("Seconds spent on a flower collecting.")]
        [SerializeField, Min(0f)] private float collectDuration = 1f;
        [Tooltip("Optional override for the prefab's material. Shared, so batching is kept.")]
        [SerializeField] private Material material;
        [SerializeField, Min(0.01f)] private float scale = 0.35f;
        [SerializeField] private GameObject prefab;
        [Tooltip("Honey cost of merging three bees of the tier below into one of this tier. Unused for the first tier.")]
        [SerializeField] private double evolveCost;

        public float Speed => speed;
        public float Capacity => capacity;
        public float CollectDuration => collectDuration;
        public Material Material => material;
        public float Scale => scale;
        public GameObject Prefab => prefab;
        public double EvolveCost => evolveCost;
    }
}
