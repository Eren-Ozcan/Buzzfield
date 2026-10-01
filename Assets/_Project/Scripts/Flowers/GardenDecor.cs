using System;
using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Flowers
{
    public enum DecorPlacement
    {
        /// <summary>Anywhere on the ground between the flowers.</summary>
        Inside,

        /// <summary>Along the left, right and back edges, just inside the fence.</summary>
        Border,
    }

    /// <summary>One kind of decor piece and how many of it a garden scatters.</summary>
    [Serializable]
    public struct DecorScatter
    {
        public GameObject prefab;
        [Min(0)] public int count;
        [Tooltip("Random uniform scale between x and y.")]
        public Vector2 scaleRange;
        [Tooltip("Free distance kept to every flower slot.")]
        [Min(0f)] public float clearance;
        [Tooltip("Free distance kept between two pieces of this kind.")]
        [Min(0f)] public float spacing;
        public DecorPlacement placement;
        [Tooltip("Turns from the dry colour to its own colour as the garden blooms.")]
        public bool tinted;
    }

    /// <summary>
    /// Non-interactive dressing that makes a garden read as one: grass, bushes, stones and a
    /// fence. Every garden scatters it from its own seed (<see cref="GardenConfig.DecorSeed"/>);
    /// the pieces are prefabs whose meshes are merged per material when the garden spawns.
    /// </summary>
    [CreateAssetMenu(menuName = "Buzzfield/Garden Decor", fileName = "GardenDecor")]
    public sealed class GardenDecor : ScriptableObject
    {
        [SerializeField] private List<DecorScatter> scatters = new List<DecorScatter>();
        [Tooltip("Free distance kept between decor and the hive.")]
        [SerializeField, Min(0f)] private float hiveClearance = 1.6f;
        [Tooltip("Border pieces stand this far inside the ground edge, plus a little random depth.")]
        [SerializeField, Min(0f)] private float borderInset = 0.5f;

        [Header("Fence")]
        [Tooltip("Post at every corner and along the left, right and back edges.")]
        [SerializeField] private GameObject fencePost;
        [Tooltip("Rail piece 1 unit long along +X; stretched between two posts.")]
        [SerializeField] private GameObject fenceRail;
        [SerializeField, Min(0.2f)] private float postSpacing = 1.5f;

        [Header("Tint")]
        [Tooltip("Colour tinted decor leans toward in a garden that has not bloomed at all.")]
        [SerializeField] private Color dryColor = new Color(0.66f, 0.64f, 0.55f);
        [Tooltip("How far tinted decor leans toward the dry colour before any bloom; below 1 a bush stays a bush.")]
        [SerializeField, Range(0f, 1f)] private float dryStrength = 0.7f;

        public IReadOnlyList<DecorScatter> Scatters => scatters;
        public float HiveClearance => hiveClearance;
        public float BorderInset => borderInset;
        public GameObject FencePost => fencePost;
        public GameObject FenceRail => fenceRail;
        public float PostSpacing => postSpacing;
        public Color DryColor => dryColor;
        public float DryStrength => dryStrength;

        /// <summary>Unbloomed colour of a tinted piece whose own colour is <paramref name="color"/>.</summary>
        public Color Dry(Color color) => Color.Lerp(color, dryColor, dryStrength);
    }
}
