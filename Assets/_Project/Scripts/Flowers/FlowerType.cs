using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>Balance and visuals of one flower species (Daisy, Lavender, Orchid).</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Flower Type", fileName = "FlowerType")]
    public sealed class FlowerType : ScriptableObject
    {
        [Tooltip("Nectar units the flower holds when full.")]
        [SerializeField, Min(1f)] private float maxNectar = 6f;
        [Tooltip("Nectar units regenerated per second.")]
        [SerializeField, Min(0f)] private float regenPerSecond = 0.5f;
        [Tooltip("Honey per nectar unit before multipliers.")]
        [SerializeField, Min(0f)] private float nectarValue = 1f;
        [Tooltip("How many bees may fly to this flower at the same time.")]
        [SerializeField, Min(1)] private int maxBeesTargeting = 2;
        [Tooltip("Nectar units bees must collect from this flower before it blooms.")]
        [SerializeField, Min(1f)] private float nectarToBloom = 60f;
        [SerializeField] private Color bloomedColor = Color.white;
        [SerializeField] private FlowerView prefab;

        public float MaxNectar => maxNectar;
        public float RegenPerSecond => regenPerSecond;
        public float NectarValue => nectarValue;
        public int MaxBeesTargeting => maxBeesTargeting;
        public float NectarToBloom => nectarToBloom;
        public Color BloomedColor => bloomedColor;
        public FlowerView Prefab => prefab;
    }
}
