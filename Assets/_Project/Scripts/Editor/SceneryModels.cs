using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// The land around a garden, after the concept art: a low-poly meadow that stays flat under
    /// the garden and in front of it, rises into hills behind it and a little at the sides, with
    /// round trees and pines, rocks, wildflowers and stepping stones up to the open front of the
    /// fence. One mesh on the palette material, its tree and rock shadows baked in as darker
    /// patches. The garden is 12 x 18 units around the origin and the camera looks at it from -Z,
    /// so the front stays low and clear.
    /// </summary>
    internal static class SceneryModels
    {
        const float GardenHalfWidth = 6f;
        const float GardenHalfDepth = 9f;
        /// <summary>Flat border around the garden before the land starts to rise.</summary>
        const float FlatMargin = 1.5f;
        const float Cell = 2f;
        const float MinX = -36f;
        const float MaxX = 36f;
        const float MinZ = -24f;
        const float MaxZ = 64f;
        /// <summary>Just under the garden ground, so its slab edge disappears in the meadow.</summary>
        const float GroundY = -0.03f;
        /// <summary>
        /// Ground offset of a shadow per unit of height, for the scene's key light
        /// (GreyboxSceneBuilder.CreateLight, rotation 55, -30, 0).
        /// </summary>
        static readonly Vector3 ShadowLean = new Vector3(-0.35f, 0f, 0.61f);

        public static Mesh Scenery(int seed)
        {
            var b = new LowPolyBuilder(seed);
            var random = new System.Random(seed);
            var noise = new Vector2((float)random.NextDouble() * 100f, (float)random.NextDouble() * 100f);

            Terrain(b, random, noise);
            var taken = new List<Vector2>();
            Trees(b, random, noise, taken);
            Rocks(b, random, noise, taken);
            Wildflowers(b, random, noise);
            SteppingStones(b, random, noise);
            return b.ToMesh("Scenery");
        }

        static float Height(float x, float z, Vector2 noise)
        {
            float side = Mathf.Max(0f, Mathf.Abs(x) - (GardenHalfWidth + FlatMargin));
            float back = Mathf.Max(0f, z - (GardenHalfDepth + FlatMargin));
            float front = Mathf.Max(0f, -(GardenHalfDepth + FlatMargin) - z);
            float rise = 0.1f * Mathf.Pow(side, 1.35f) + 0.3f * Mathf.Pow(back, 1.25f) - 0.03f * front;
            float away = Mathf.Max(side, back);
            float bumps = (Mathf.PerlinNoise(x * 0.13f + noise.x, z * 0.13f + noise.y) - 0.5f) * 1.6f * Mathf.Clamp01(away / 4f);
            return GroundY + rise + bumps;
        }

        static Vector3 Ground(float x, float z, Vector2 noise) => new Vector3(x, Height(x, z, noise), z);

        static bool InGarden(float x, float z, float margin) =>
            Mathf.Abs(x) < GardenHalfWidth + margin && Mathf.Abs(z) < GardenHalfDepth + margin;

        static void Terrain(LowPolyBuilder b, System.Random random, Vector2 noise)
        {
            for (float z = MinZ; z < MaxZ; z += Cell)
            for (float x = MinX; x < MaxX; x += Cell)
            {
                // Cells under the garden slab are never seen.
                if (InGarden(x + Cell * 0.5f, z + Cell * 0.5f, -Cell * 0.5f))
                    continue;
                Vector3 p00 = Ground(x, z, noise);
                Vector3 p10 = Ground(x + Cell, z, noise);
                Vector3 p11 = Ground(x + Cell, z + Cell, noise);
                Vector3 p01 = Ground(x, z + Cell, noise);
                // Alternate the diagonal so the facets do not line up.
                bool flip = ((int)(x / Cell) + (int)(z / Cell)) % 2 == 0;
                if (flip)
                {
                    GroundTriangle(b, random, p00, p01, p11);
                    GroundTriangle(b, random, p00, p11, p10);
                }
                else
                {
                    GroundTriangle(b, random, p00, p01, p10);
                    GroundTriangle(b, random, p10, p01, p11);
                }
            }
        }

        static void GroundTriangle(LowPolyBuilder b, System.Random random, Vector3 a, Vector3 c, Vector3 d)
        {
            float height = (a.y + c.y + d.y) / 3f;
            double roll = random.NextDouble();
            // Mostly the middle green, a few lighter and darker facets for texture.
            if (height > 5f)
                b.Swatch = roll < 0.6 ? Palette.HillFar : Palette.MeadowDark;
            else if (roll < 0.2)
                b.Swatch = Palette.MeadowLight;
            else if (roll < 0.4 || height > 1.2f && roll < 0.6)
                b.Swatch = Palette.MeadowDark;
            else
                b.Swatch = Palette.MeadowMid;
            b.Triangle(a, c, d, Vector3.up);
        }

        static bool Free(List<Vector2> taken, Vector2 point, float spacing)
        {
            for (int i = 0; i < taken.Count; i++)
            {
                if ((taken[i] - point).sqrMagnitude < spacing * spacing)
                    return false;
            }
            return true;
        }

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        static void Trees(LowPolyBuilder b, System.Random random, Vector2 noise, List<Vector2> taken)
        {
            const int count = 70;
            for (int tries = 0; tries < 3000 && taken.Count < count; tries++)
            {
                float x = Range(random, -26f, 26f);
                float z = Range(random, -14f, 46f);
                // Clear of the fence, and the view in front of the garden stays open.
                if (InGarden(x, z, 2.4f) || (z < -GardenHalfDepth - 1f && Mathf.Abs(x) < 11f))
                    continue;
                var spot = new Vector2(x, z);
                if (!Free(taken, spot, 2.3f))
                    continue;
                taken.Add(spot);
                Vector3 foot = Ground(x, z, noise);
                float scale = Range(random, 0.8f, 1.5f);
                if (random.NextDouble() < 0.3)
                    Pine(b, random, foot, scale);
                else
                    RoundTree(b, random, foot, scale);
            }
        }

        /// <summary>A flat darker patch on the ground where something of this height and radius would cast its shadow.</summary>
        static void BakedShadow(LowPolyBuilder b, Vector3 foot, float height, float radius)
        {
            Vector3 lean = ShadowLean * height;
            Quaternion along = Quaternion.LookRotation(lean.normalized);
            b.Ellipsoid(foot + lean * 0.55f + Vector3.up * 0.02f, new Vector3(radius, 0.05f, radius + lean.magnitude * 0.45f),
                along, 1, _ => Palette.Shade);
        }

        static void RoundTree(LowPolyBuilder b, System.Random random, Vector3 foot, float scale)
        {
            BakedShadow(b, foot, 1.6f * scale, 0.9f * scale);
            b.Swatch = Palette.Trunk;
            b.Stalk(foot - Vector3.up * 0.2f, foot + Vector3.up * (1.1f * scale), 0.13f * scale, 5);
            Vector3 crown = foot + Vector3.up * (1.6f * scale);
            b.Swatch = random.NextDouble() < 0.5 ? Palette.LeafMid : Palette.LeafLight;
            b.Blob(crown, new Vector3(0.95f, 0.8f, 0.95f) * scale, Quaternion.Euler(0f, Range(random, 0f, 360f), 0f), 0.15f);
            int extra = random.Next(1, 3);
            for (int i = 0; i < extra; i++)
            {
                float angle = Range(random, 0f, Mathf.PI * 2f);
                var offset = new Vector3(Mathf.Cos(angle) * 0.6f, Range(random, -0.1f, 0.5f), Mathf.Sin(angle) * 0.6f) * scale;
                b.Swatch = random.NextDouble() < 0.5 ? Palette.LeafDark : Palette.LeafLight;
                b.Blob(crown + offset, Vector3.one * (0.55f * scale), Quaternion.identity, 0.15f);
            }
        }

        static void Pine(LowPolyBuilder b, System.Random random, Vector3 foot, float scale)
        {
            BakedShadow(b, foot, 1.4f * scale, 0.7f * scale);
            b.Swatch = Palette.Trunk;
            b.Stalk(foot - Vector3.up * 0.2f, foot + Vector3.up * (0.6f * scale), 0.12f * scale, 5);
            b.Swatch = Palette.Pine;
            float[] radii = { 0.95f, 0.75f, 0.52f };
            for (int tier = 0; tier < radii.Length; tier++)
            {
                Vector3 baseCenter = foot + Vector3.up * ((0.45f + tier * 0.55f) * scale);
                b.Spike(baseCenter, baseCenter + Vector3.up * (1.05f * scale), radii[tier] * scale, 7);
            }
        }

        static void Rocks(LowPolyBuilder b, System.Random random, Vector2 noise, List<Vector2> taken)
        {
            int placed = 0;
            for (int tries = 0; tries < 500 && placed < 14; tries++)
            {
                float x = Range(random, -18f, 18f);
                float z = Range(random, -13f, 30f);
                var spot = new Vector2(x, z);
                if (InGarden(x, z, 1.6f) || !Free(taken, spot, 1.5f))
                    continue;
                taken.Add(spot);
                placed++;
                float scale = Range(random, 0.6f, 1.4f);
                BakedShadow(b, Ground(x, z, noise), 0.3f * scale, 0.5f * scale);
                b.Swatch = random.NextDouble() < 0.5 ? Palette.Rock : Palette.RockDark;
                b.Blob(Ground(x, z, noise) + Vector3.up * (0.1f * scale), new Vector3(0.55f, 0.32f, 0.42f) * scale,
                    Quaternion.Euler(0f, Range(random, 0f, 360f), 0f), 0.25f);
            }
        }

        /// <summary>Little white, pink and yellow dots in the meadow close around the garden.</summary>
        static void Wildflowers(LowPolyBuilder b, System.Random random, Vector2 noise)
        {
            int[] colours = { Palette.White, Palette.Cheek, Palette.Pollen };
            int placed = 0;
            for (int tries = 0; tries < 2000 && placed < 140; tries++)
            {
                float x = Range(random, -16f, 16f);
                float z = Range(random, -15f, 20f);
                if (InGarden(x, z, 0.6f))
                    continue;
                placed++;
                b.Swatch = colours[random.Next(colours.Length)];
                b.Blob(Ground(x, z, noise) + Vector3.up * 0.08f, new Vector3(0.08f, 0.05f, 0.08f), Quaternion.identity);
            }
        }

        /// <summary>Flat stones from the near edge of the view up to the open front of the garden.</summary>
        static void SteppingStones(LowPolyBuilder b, System.Random random, Vector2 noise)
        {
            for (float z = -GardenHalfDepth - 0.6f; z > -GardenHalfDepth - 9f; z -= 0.95f)
            {
                float x = Mathf.Sin(z * 0.45f) * 0.5f + Range(random, -0.12f, 0.12f);
                b.Swatch = random.NextDouble() < 0.5 ? Palette.Rock : Palette.RockDark;
                b.Blob(Ground(x, z, noise) + Vector3.up * 0.02f, new Vector3(0.34f, 0.06f, 0.27f),
                    Quaternion.Euler(0f, Range(random, 0f, 180f), 0f), 0.15f);
            }
        }
    }
}
