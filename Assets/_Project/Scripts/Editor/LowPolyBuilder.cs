using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Builds the flat-shaded low-poly placeholder meshes (flower patches, garden decor) out of
    /// a few shapes. Every face gets its own vertices, so the lighting shows the facets.
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
        readonly System.Random random;

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
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }
    }
}
