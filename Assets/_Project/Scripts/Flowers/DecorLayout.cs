using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Flowers
{
    /// <summary>One decor piece in garden space.</summary>
    public struct DecorPiece
    {
        public GameObject Prefab;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public bool Tinted;
    }

    /// <summary>
    /// Where a garden's decor goes: the fence along the left, right and back edges, then every
    /// scatter of <see cref="GardenDecor"/> at random spots clear of the flower slots, the hive
    /// and each other. The same garden always gets the same layout (seeded by its config).
    /// </summary>
    public static class DecorLayout
    {
        const int TriesPerPiece = 12;
        /// <summary>Extra random depth of a border piece inward from <see cref="GardenDecor.BorderInset"/>.</summary>
        const float BorderDepth = 0.6f;

        public static List<DecorPiece> Place(GardenConfig garden)
        {
            var pieces = new List<DecorPiece>();
            GardenDecor decor = garden.Decor;
            if (decor == null)
                return pieces;

            Vector2 half = garden.GroundSize * 0.5f;
            PlaceFence(decor, half, pieces);

            var random = new System.Random(garden.DecorSeed);
            var placed = new List<Vector2>();
            IReadOnlyList<DecorScatter> scatters = decor.Scatters;
            for (int s = 0; s < scatters.Count; s++)
            {
                DecorScatter scatter = scatters[s];
                if (scatter.prefab == null || scatter.count <= 0)
                    continue;
                placed.Clear();
                int tries = scatter.count * TriesPerPiece;
                for (int t = 0; t < tries && placed.Count < scatter.count; t++)
                {
                    Vector2 point = scatter.placement == DecorPlacement.Border
                        ? BorderPoint(random, half, decor.BorderInset)
                        : InsidePoint(random, half, decor.BorderInset);
                    if (!IsFree(point, garden, decor, scatter, placed))
                        continue;
                    placed.Add(point);
                    float scale = Mathf.Lerp(scatter.scaleRange.x, scatter.scaleRange.y, (float)random.NextDouble());
                    pieces.Add(new DecorPiece
                    {
                        Prefab = scatter.prefab,
                        Position = GardenConfig.ToWorld(point),
                        Rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f),
                        Scale = Vector3.one * scale,
                        Tinted = scatter.tinted,
                    });
                }
            }
            return pieces;
        }

        /// <summary>True when <paramref name="point"/> keeps every distance the scatter and the decor ask for.</summary>
        public static bool IsFree(Vector2 point, GardenConfig garden, GardenDecor decor, DecorScatter scatter, List<Vector2> placed)
        {
            if ((point - garden.HivePosition).sqrMagnitude < decor.HiveClearance * decor.HiveClearance)
                return false;
            IReadOnlyList<FlowerSlot> slots = garden.Slots;
            float clearance = scatter.clearance * scatter.clearance;
            for (int i = 0; i < slots.Count; i++)
            {
                if ((point - slots[i].position).sqrMagnitude < clearance)
                    return false;
            }
            float spacing = scatter.spacing * scatter.spacing;
            for (int i = 0; i < placed.Count; i++)
            {
                if ((point - placed[i]).sqrMagnitude < spacing)
                    return false;
            }
            return true;
        }

        static Vector2 InsidePoint(System.Random random, Vector2 half, float inset) => new Vector2(
            Mathf.Lerp(-half.x + inset, half.x - inset, (float)random.NextDouble()),
            Mathf.Lerp(-half.y + inset, half.y - inset, (float)random.NextDouble()));

        /// <summary>A point just inside the left, right or back edge, each edge as likely as its length.</summary>
        static Vector2 BorderPoint(System.Random random, Vector2 half, float inset)
        {
            float depth = inset + (float)random.NextDouble() * BorderDepth;
            float side = 2f * half.y;
            float along = (float)random.NextDouble() * (2f * side + 2f * half.x);
            if (along < side)
                return new Vector2(-half.x + depth, along - half.y);
            along -= side;
            if (along < side)
                return new Vector2(half.x - depth, along - half.y);
            along -= side;
            return new Vector2(along - half.x, half.y - depth);
        }

        /// <summary>Posts along the left, right and back edges (the front stays open to the camera), rails between them.</summary>
        static void PlaceFence(GardenDecor decor, Vector2 half, List<DecorPiece> pieces)
        {
            if (decor.FencePost == null)
                return;
            var corners = new[]
            {
                new Vector2(-half.x, -half.y), new Vector2(-half.x, half.y),
                new Vector2(half.x, half.y), new Vector2(half.x, -half.y),
            };
            for (int edge = 0; edge < corners.Length - 1; edge++)
            {
                Vector2 from = corners[edge];
                Vector2 to = corners[edge + 1];
                int segments = Mathf.Max(1, Mathf.CeilToInt((to - from).magnitude / decor.PostSpacing));
                // Each edge starts with its own post; the last edge also closes with one.
                int posts = edge == corners.Length - 2 ? segments + 1 : segments;
                for (int i = 0; i < posts; i++)
                    pieces.Add(Piece(decor.FencePost, Vector2.Lerp(from, to, i / (float)segments), Quaternion.identity, Vector3.one));
                if (decor.FenceRail == null)
                    continue;
                Vector3 direction = GardenConfig.ToWorld(to - from).normalized;
                Quaternion rotation = Quaternion.FromToRotation(Vector3.right, direction);
                float length = (to - from).magnitude / segments;
                for (int i = 0; i < segments; i++)
                    pieces.Add(Piece(decor.FenceRail, Vector2.Lerp(from, to, i / (float)segments), rotation, new Vector3(length, 1f, 1f)));
            }
        }

        static DecorPiece Piece(GameObject prefab, Vector2 point, Quaternion rotation, Vector3 scale) => new DecorPiece
        {
            Prefab = prefab,
            Position = GardenConfig.ToWorld(point),
            Rotation = rotation,
            Scale = scale,
        };
    }
}
