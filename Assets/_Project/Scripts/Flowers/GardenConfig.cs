using System;
using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>One fixed spot where a flower grows. Garden XZ coordinates, origin at the garden centre.</summary>
    [Serializable]
    public struct FlowerSlot
    {
        public Vector2 position;
        public FlowerType type;
        [Tooltip("False = sprout slot that activates when a nearby flower blooms.")]
        public bool startsActive;
    }

    /// <summary>Layout and value of one garden. Bloom % = bloomed slots / all slots.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Garden Config", fileName = "GardenConfig")]
    public sealed class GardenConfig : ScriptableObject
    {
        [Tooltip("Ground size in world units (x = width, y = depth).")]
        [SerializeField] private Vector2 groundSize = new Vector2(12f, 18f);
        [Tooltip("Ground tile grid used for the bloom colouring.")]
        [SerializeField] private Vector2Int tileGrid = new Vector2Int(12, 18);
        [SerializeField] private Vector2 hivePosition = new Vector2(0f, -6.5f);
        [SerializeField] private List<FlowerSlot> slots = new List<FlowerSlot>();
        [Tooltip("Multiplies every flower's nectar value in this garden.")]
        [SerializeField, Min(0f)] private float gardenValueMultiplier = 1f;
        [Tooltip("World radius in which a flower's bloom colours the ground.")]
        [SerializeField, Min(0f)] private float bloomInfluenceRadius = 2.5f;
        [Tooltip("Honey cost of moving the Queen away from this garden.")]
        [SerializeField] private double moveHoneyCost = 5000;
        [SerializeField] private HiveView hivePrefab;
        [Tooltip("1x1 ground prefab, scaled to the ground size; its surface shows the bloom tiles.")]
        [SerializeField] private GroundView groundView;
        [Tooltip("Grass, bushes, stones and fence; none when empty.")]
        [SerializeField] private GardenDecor decor;
        [Tooltip("Seeds the decor layout, so every garden looks its own and always the same.")]
        [SerializeField] private int decorSeed = 1;

        public Vector2 GroundSize => groundSize;
        public Vector2Int TileGrid => tileGrid;
        public Vector2 HivePosition => hivePosition;
        public IReadOnlyList<FlowerSlot> Slots => slots;
        public float GardenValueMultiplier => gardenValueMultiplier;
        public float BloomInfluenceRadius => bloomInfluenceRadius;
        public double MoveHoneyCost => moveHoneyCost;
        public HiveView HivePrefab => hivePrefab;
        public GroundView GroundView => groundView;
        public GardenDecor Decor => decor;
        public int DecorSeed => decorSeed;

        public static Vector3 ToWorld(Vector2 gardenPosition) => new Vector3(gardenPosition.x, 0f, gardenPosition.y);

        /// <summary>Garden XZ position of the centre of tile (x, y); tile (0, 0) is the -X/-Z corner.</summary>
        public Vector2 TileCenter(int x, int y) => new Vector2(
            (x + 0.5f) / tileGrid.x * groundSize.x - groundSize.x * 0.5f,
            (y + 0.5f) / tileGrid.y * groundSize.y - groundSize.y * 0.5f);
    }
}
