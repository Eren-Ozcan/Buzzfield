using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Builds the flat-shaded low-poly meshes (bees, hive, flower patches, garden decor) out of a
    /// few shapes. Every face gets its own vertices, so the lighting shows the facets. Faces carry
    /// the UV of the current <see cref="Swatch"/>, for meshes drawn with the palette material.
    /// </summary>
    internal sealed class LowPolyBuilder
    {
        static readonly float Phi = (1f + Mathf.Sqrt(5f)) * 0.5f;

        static readonly Vector3[] IcoVertices =
        {
            new Vector3(-1f, Phi, 0f), new Vector3(1f, Phi, 0f), new Vector3(-1f, -Phi, 0f), new Vector3(1f, -Phi, 0f),
            new Vector3(0f, -1f, Phi), new Vector3(0f, 1f, Phi), new Vector3(0f, -1f, -Phi), new Vector3(0f, 1f, -Phi),
            new Vector3(Phi, 0f, -1f), new Vector3(Phi, 0f, 1f), new Vector3(-Phi, 0f, -1f), new Vector3(-Phi, 0f, 1f),
        };

        static readonly int[] IcoFaces =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
            1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
            4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };

        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<int> triangles = new List<int>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly System.Random random;
        Vector2 swatchUv = Palette.Uv(0);
        int swatch;

        /// <summary>Palette colour of every face added from now on (<see cref="Palette"/>).</summary>
        public int Swatch
        {
            get => swatch;
            set
            {
                swatch = value;
                swatchUv = Palette.Uv(value);
            }
        }

        /// <param name="seed">Seeds the jitter of <see cref="Blob"/>, so a rebuild gives the same mesh.</param>
        public LowPolyBuilder(int seed = 1)
        {
            random = new System.Random(seed);
        }

        /// <summary>
        /// Icosahedron (20 faces) with the given radii, rotated and centred; <paramref name="jitter"/>
        /// moves each corner by up to that share of its radius for a rougher, hand-made look.
        /// </summary>
        public void Blob(Vector3 center, Vector3 radii, Quaternion rotation, float jitter = 0f)
        {
            var corners = new Vector3[IcoVertices.Length];
            for (int i = 0; i < corners.Length; i++)
            {
                float scale = 1f + jitter * (float)(random.NextDouble() * 2.0 - 1.0);
                corners[i] = center + rotation * Vector3.Scale(IcoVertices[i].normalized * scale, radii);
            }
            for (int f = 0; f < IcoFaces.Length; f += 3)
            {
                Vector3 a = corners[IcoFaces[f]];
                Vector3 b = corners[IcoFaces[f + 1]];
                Vector3 c = corners[IcoFaces[f + 2]];
                Face(a, b, c, (a + b + c) / 3f - center);
            }
        }

        /// <summary>
        /// Ellipsoid from an icosahedron split <paramref name="subdivisions"/> times (1 = 80 faces,
        /// 2 = 320). <paramref name="swatchFor"/> picks each face's colour from its direction on the
        /// unit sphere before scaling, so stripes and patches follow the shape.
        /// </summary>
        public void Ellipsoid(Vector3 center, Vector3 radii, Quaternion rotation, int subdivisions, System.Func<Vector3, int> swatchFor)
        {
            var faces = new List<(Vector3, Vector3, Vector3)>();
            for (int f = 0; f < IcoFaces.Length; f += 3)
                faces.Add((IcoVertices[IcoFaces[f]].normalized, IcoVertices[IcoFaces[f + 1]].normalized, IcoVertices[IcoFaces[f + 2]].normalized));
            for (int level = 0; level < subdivisions; level++)
            {
                var split = new List<(Vector3, Vector3, Vector3)>(faces.Count * 4);
                foreach ((Vector3 a, Vector3 b, Vector3 c) in faces)
                {
                    Vector3 ab = (a + b).normalized;
                    Vector3 bc = (b + c).normalized;
                    Vector3 ca = (c + a).normalized;
                    split.Add((a, ab, ca));
                    split.Add((ab, b, bc));
                    split.Add((ca, bc, c));
                    split.Add((ab, bc, ca));
                }
                faces = split;
            }
            int kept = Swatch;
            foreach ((Vector3 a, Vector3 b, Vector3 c) in faces)
            {
                Swatch = swatchFor((a + b + c).normalized);
                Vector3 pa = center + rotation * Vector3.Scale(a, radii);
                Vector3 pb = center + rotation * Vector3.Scale(b, radii);
                Vector3 pc = center + rotation * Vector3.Scale(c, radii);
                Face(pa, pb, pc, (pa + pb + pc) / 3f - center);
            }
            Swatch = kept;
        }

        /// <summary>
        /// Surface of revolution around the Y axis through <paramref name="center"/>: each profile
        /// point is (radius, height), bottom to top. <paramref name="swatchForBand"/> colours band i,
        /// the one between profile points i and i + 1. A radius of 0 closes the shape there.
        /// <paramref name="rotation"/> turns the whole shape (the axis becomes rotation * up).
        /// </summary>
        public void Lathe(Vector3 center, IReadOnlyList<Vector2> profile, int sides, System.Func<int, int> swatchForBand,
            Quaternion? rotation = null)
        {
            Quaternion turn = rotation ?? Quaternion.identity;
            int kept = Swatch;
            for (int band = 0; band < profile.Count - 1; band++)
            {
                Swatch = swatchForBand(band);
                Vector2 low = profile[band];
                Vector2 high = profile[band + 1];
                for (int i = 0; i < sides; i++)
                {
                    float a0 = i * Mathf.PI * 2f / sides;
                    float a1 = (i + 1) * Mathf.PI * 2f / sides;
                    var d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                    var d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
                    Vector3 radial = (d0 + d1).normalized;
                    // Outward in the profile plane: (dy, -dr) as (radial, up).
                    Vector3 outward = turn * (radial * (high.y - low.y) + Vector3.up * (low.x - high.x));
                    Vector3 p00 = center + turn * (d0 * low.x + Vector3.up * low.y);
                    Vector3 p10 = center + turn * (d1 * low.x + Vector3.up * low.y);
                    Vector3 p11 = center + turn * (d1 * high.x + Vector3.up * high.y);
                    Vector3 p01 = center + turn * (d0 * high.x + Vector3.up * high.y);
                    Face(p00, p10, p11, outward);
                    Face(p00, p11, p01, outward);
                }
            }
            Swatch = kept;
        }

        /// <summary>
        /// A flower head: a small centre and <paramref name="petals"/> flattened petals around it,
        /// in the XZ plane of <paramref name="orientation"/> (its local up is the flower's face).
        /// <paramref name="petalRadii"/> is (width, thickness, length); <paramref name="cupDegrees"/>
        /// lifts the petal tips into a cup.
        /// </summary>
        public void FlowerHead(Vector3 center, Quaternion orientation, int petals, Vector3 petalRadii, float ringRadius,
            float cupDegrees, Vector3 centerRadii)
        {
            Blob(center, centerRadii, orientation);
            for (int i = 0; i < petals; i++)
            {
                float angle = i * 360f / petals;
                Quaternion along = orientation * Quaternion.Euler(0f, angle, 0f);
                Vector3 direction = along * Vector3.forward;
                Blob(center + direction * ringRadius, petalRadii, along * Quaternion.Euler(-cupDegrees, 0f, 0f));
            }
        }

        /// <summary>Prism with <paramref name="sides"/> sides from <paramref name="from"/> to <paramref name="to"/>, closed at the top.</summary>
        public void Stalk(Vector3 from, Vector3 to, float radius, int sides)
        {
            Ring(from, to, radius, sides, out Vector3 side, out Vector3 side2);
            Vector3 axis = (to - from).normalized;
            for (int i = 0; i < sides; i++)
            {
                Vector3 o0 = RingOffset(side, side2, radius, i, sides);
                Vector3 o1 = RingOffset(side, side2, radius, i + 1, sides);
                Face(from + o0, from + o1, to + o1, o0 + o1);
                Face(from + o0, to + o1, to + o0, o0 + o1);
                Face(to + o0, to + o1, to, axis);
            }
        }

        /// <summary>Cone with <paramref name="sides"/> sides: base ring around <paramref name="from"/>, tip at <paramref name="to"/>.</summary>
        public void Spike(Vector3 from, Vector3 to, float radius, int sides)
        {
            Ring(from, to, radius, sides, out Vector3 side, out Vector3 side2);
            for (int i = 0; i < sides; i++)
            {
                Vector3 o0 = RingOffset(side, side2, radius, i, sides);
                Vector3 o1 = RingOffset(side, side2, radius, i + 1, sides);
                Face(from + o0, from + o1, to, o0 + o1);
            }
        }

        /// <summary>Flat diamond leaf from <paramref name="root"/> to <paramref name="tip"/>, seen from both sides.</summary>
        public void Leaf(Vector3 root, Vector3 tip, float width)
        {
            Vector3 along = tip - root;
            Vector3 across = Vector3.Cross(along, Vector3.up);
            if (across.sqrMagnitude < 1e-6f)
                across = Vector3.right;
            across = across.normalized * (width * 0.5f);
            Vector3 middle = root + along * 0.4f + Vector3.up * (along.magnitude * 0.12f);
            DoubleFace(root, middle - across, tip);
            DoubleFace(root, tip, middle + across);
        }

        /// <summary>Thin triangle seen from both sides: a grass blade.</summary>
        public void Blade(Vector3 baseCenter, Vector3 tip, float width)
        {
            Vector3 across = Vector3.Cross(tip - baseCenter, Vector3.up);
            if (across.sqrMagnitude < 1e-6f)
                across = Vector3.right;
            across = across.normalized * (width * 0.5f);
            DoubleFace(baseCenter - across, tip, baseCenter + across);
        }

        /// <summary>One triangle facing <paramref name="outward"/> (terrain and other hand-built surfaces).</summary>
        public void Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 outward) => Face(a, b, c, outward);

        /// <summary>Random value in [min, max) from this builder's seeded generator.</summary>
        public float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

        /// <param name="scale">Uniform scale applied to every vertex, around the origin.</param>
        public Mesh ToMesh(string name, float scale = 1f)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            if (scale != 1f)
            {
                for (int i = 0; i < vertices.Count; i++)
                    vertices[i] *= scale;
            }
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Ring(Vector3 from, Vector3 to, float radius, int sides, out Vector3 side, out Vector3 side2)
        {
            Vector3 axis = (to - from).normalized;
            Vector3 reference = Mathf.Abs(axis.y) < 0.99f ? Vector3.up : Vector3.right;
            side = Vector3.Cross(axis, reference).normalized;
            side2 = Vector3.Cross(axis, side).normalized;
        }

        static Vector3 RingOffset(Vector3 side, Vector3 side2, float radius, int index, int sides)
        {
            float angle = index * Mathf.PI * 2f / sides;
            return (side * Mathf.Cos(angle) + side2 * Mathf.Sin(angle)) * radius;
        }

        /// <summary>One triangle facing <paramref name="outward"/>.</summary>
        void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            // Closed ends of a lathe or prism collapse to a point: no triangle there.
            if (normal.sqrMagnitude < 1e-12f)
                return;
            if (Vector3.Dot(normal, outward) < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }
            Add(a, b, c, normal.normalized);
        }

        void DoubleFace(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            Add(a, b, c, normal);
            Add(a, c, b, -normal);
        }

        void Add(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            uvs.Add(swatchUv);
            uvs.Add(swatchUv);
            uvs.Add(swatchUv);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }
    }
}
