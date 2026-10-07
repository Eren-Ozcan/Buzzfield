using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Low-poly bee and hive meshes after the concept art (pictures/buzzfield/masters/flow):
    /// a chubby smiling bee with dark stripes, and a round coiled skep with an arched door.
    /// One mesh each, coloured through the <see cref="Palette"/>, so a bee is one renderer.
    /// </summary>
    internal static class CharacterModels
    {
        /// <summary>
        /// Bee facing +Z (BeeManager turns +Z along the flight), about 0.85 units long before the
        /// tier scale. <paramref name="pollenBaskets"/> adds the Forager's pollen loads.
        /// </summary>
        public static Mesh Bee(string name, int body, int wing, bool pollenBaskets)
        {
            var b = new LowPolyBuilder(seed: 51);
            // Round body turned along +Z, so the stripes are clean rings.
            var radii = new Vector3(BodyRadius, BodyRadius, BodyHalfLength);
            var profile = new List<Vector2>();
            for (int i = 0; i <= BodySegments; i++)
            {
                float z = -BodyHalfLength + 2f * BodyHalfLength * i / BodySegments;
                float t = z / BodyHalfLength;
                profile.Add(new Vector2(BodyRadius * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)), z));
            }
            b.Lathe(Vector3.zero, profile, 14, band => IsStripe(band) ? Palette.Stripe : body, Quaternion.Euler(90f, 0f, 0f));

            // Eyes with a small highlight, rosy cheeks and a smile.
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 eye = Surface(radii, new Vector3(0.3f * side, 0.16f, 0.94f), out Vector3 normal);
                Quaternion facing = Quaternion.LookRotation(normal, Vector3.up);
                b.Swatch = Palette.Eye;
                b.Blob(eye + normal * 0.005f, new Vector3(0.07f, 0.085f, 0.03f), facing);
                b.Swatch = Palette.White;
                b.Blob(eye + normal * 0.03f + facing * new Vector3(0.025f, 0.035f, 0f), new Vector3(0.022f, 0.022f, 0.01f), facing);

                Vector3 cheek = Surface(radii, new Vector3(0.55f * side, -0.12f, 0.83f), out Vector3 cheekNormal);
                b.Swatch = Palette.Cheek;
                b.Blob(cheek, new Vector3(0.075f, 0.05f, 0.02f), Quaternion.LookRotation(cheekNormal, Vector3.up));
            }
            b.Swatch = Palette.Stripe;
            Vector3 smileLeft = Surface(radii, new Vector3(-0.09f, -0.06f, 1f), out Vector3 n0);
            Vector3 smileMiddle = Surface(radii, new Vector3(0f, -0.1f, 1f), out Vector3 n1);
            Vector3 smileRight = Surface(radii, new Vector3(0.09f, -0.06f, 1f), out Vector3 n2);
            b.Stalk(smileLeft + n0 * 0.01f, smileMiddle + n1 * 0.01f, 0.013f, 4);
            b.Stalk(smileMiddle + n1 * 0.01f, smileRight + n2 * 0.01f, 0.013f, 4);

            // Antennae with round tips.
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 root = Surface(radii, new Vector3(0.28f * side, 0.72f, 0.62f), out _);
                Vector3 tip = root + new Vector3(0.07f * side, 0.2f, 0.14f);
                b.Stalk(root, tip, 0.02f, 4);
                b.Blob(tip, Vector3.one * 0.045f, Quaternion.identity);
            }

            // Wings: flat ovals on the back, raised outward and swept back a little.
            b.Swatch = wing;
            foreach (float side in new[] { -1f, 1f })
            {
                Quaternion tilt = Quaternion.Euler(0f, 15f * side, 28f * side);
                b.Blob(new Vector3(0.24f * side, 0.38f, -0.1f), new Vector3(0.3f, 0.02f, 0.17f), tilt);
            }

            if (pollenBaskets)
            {
                b.Swatch = Palette.Pollen;
                foreach (float side in new[] { -1f, 1f })
                    b.Blob(new Vector3(0.3f * side, -0.22f, -0.2f), Vector3.one * 0.1f, Quaternion.identity, 0.1f);
            }
            return b.ToMesh(name);
        }

        /// <summary>
        /// Round skep of seven coils on a flat base, with an arched door facing -Z (toward the
        /// camera), a landing board and a drip of honey. About 1.6 units wide and tall.
        /// </summary>
        /// <param name="entrance">Point in front of the door where bees arrive.</param>
        public static Mesh Hive(out Vector3 entrance)
        {
            var b = new LowPolyBuilder(seed: 53);
            const int coils = 7;
            const float coilHeight = 0.2f;
            var profile = new List<Vector2>();
            for (int k = 0; k < coils; k++)
            {
                float y = k * coilHeight;
                profile.Add(new Vector2(HiveRadius(y) - 0.04f, y));
                profile.Add(new Vector2(HiveRadius(y + 0.06f) + 0.03f, y + 0.06f));
                profile.Add(new Vector2(HiveRadius(y + 0.14f) + 0.03f, y + 0.14f));
            }
            float top = coils * coilHeight;
            profile.Add(new Vector2(HiveRadius(top) - 0.04f, top));
            profile.Add(new Vector2(0.3f, top + 0.07f));
            profile.Add(new Vector2(0.12f, top + 0.13f));
            profile.Add(new Vector2(0.12f, top + 0.19f));
            profile.Add(new Vector2(0f, top + 0.21f));
            // The lower band of every coil reads as the groove between two coils.
            b.Lathe(Vector3.zero, profile, 14, band => band < coils * 3 && band % 3 == 0 ? Palette.HiveDark : Palette.HiveLight);

            b.Ellipsoid(new Vector3(0f, 0.42f, -0.79f), new Vector3(0.2f, 0.26f, 0.08f), Quaternion.identity, 1, _ => Palette.Door);
            b.Swatch = Palette.Wood;
            b.Stalk(new Vector3(-0.22f, 0.17f, -0.88f), new Vector3(0.22f, 0.17f, -0.88f), 0.045f, 4);
            // A drip of honey running down the front right, flat against the coils.
            Quaternion onSurface = Quaternion.LookRotation(new Vector3(0.7f, 0f, -0.7f));
            b.Ellipsoid(new Vector3(0.52f, 1.02f, -0.52f), new Vector3(0.09f, 0.16f, 0.05f), onSurface, 1, _ => Palette.Honey);
            b.Ellipsoid(new Vector3(0.53f, 0.82f, -0.53f), Vector3.one * 0.055f, Quaternion.identity, 1, _ => Palette.Honey);

            entrance = new Vector3(0f, 0.4f, -1f);
            return b.ToMesh("Hive");
        }

        /// <summary>
        /// Butterfly body facing +Z: a slim dark body, a round head and two antennae, about
        /// 0.75 units long. The wings are separate meshes (<see cref="ButterflyWing"/>) so they can beat.
        /// </summary>
        public static Mesh ButterflyBody()
        {
            var b = new LowPolyBuilder(seed: 61);
            b.Swatch = Palette.Stripe;
            b.Ellipsoid(new Vector3(0f, 0f, -0.06f), new Vector3(0.07f, 0.07f, 0.3f), Quaternion.identity, 1, _ => Palette.Stripe);
            b.Blob(new Vector3(0f, 0.02f, 0.27f), Vector3.one * 0.09f, Quaternion.identity);
            foreach (float side in new[] { -1f, 1f })
            {
                var root = new Vector3(0.03f * side, 0.07f, 0.32f);
                Vector3 tip = root + new Vector3(0.1f * side, 0.16f, 0.2f);
                b.Stalk(root, tip, 0.012f, 4);
                b.Blob(tip, Vector3.one * 0.03f, Quaternion.identity);
            }
            return b.ToMesh("ButterflyBody");
        }

        /// <summary>
        /// One wing, hinged at the body's long axis (x = 0) and reaching out along +X for the right
        /// wing or -X for the left: a big front lobe, a smaller back lobe and pale spots on top.
        /// About 0.7 units out from the hinge.
        /// </summary>
        public static Mesh ButterflyWing(bool right)
        {
            float side = right ? 1f : -1f;
            var b = new LowPolyBuilder(seed: right ? 63 : 67);
            b.Swatch = Palette.ButterflyWing;
            b.Blob(new Vector3(0.36f * side, 0f, 0.12f), new Vector3(0.36f, 0.025f, 0.27f), Quaternion.Euler(0f, -22f * side, 0f));
            b.Blob(new Vector3(0.27f * side, -0.01f, -0.2f), new Vector3(0.25f, 0.025f, 0.2f), Quaternion.Euler(0f, 28f * side, 0f));
            b.Swatch = Palette.ButterflySpot;
            b.Blob(new Vector3(0.5f * side, 0.025f, 0.2f), new Vector3(0.09f, 0.012f, 0.07f), Quaternion.identity);
            b.Blob(new Vector3(0.3f * side, 0.02f, -0.24f), new Vector3(0.07f, 0.012f, 0.06f), Quaternion.identity);
            b.Swatch = Palette.Stripe;
            b.Blob(new Vector3(0.68f * side, 0.02f, 0.22f), new Vector3(0.05f, 0.012f, 0.05f), Quaternion.identity);
            return b.ToMesh(right ? "ButterflyWingRight" : "ButterflyWingLeft");
        }

        /// <summary>
        /// The Dewdrop: a faceted crystal drop, round below and drawn to a point on top, with a pale
        /// highlight facet. About 1 unit tall; it stands on y = 0. Rendered into the currency icon.
        /// </summary>
        public static Mesh Dewdrop()
        {
            var b = new LowPolyBuilder(seed: 71);
            var profile = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(0.2f, 0.03f),
                new Vector2(0.33f, 0.12f),
                new Vector2(0.39f, 0.26f),
                new Vector2(0.36f, 0.42f),
                new Vector2(0.25f, 0.62f),
                new Vector2(0.12f, 0.82f),
                new Vector2(0f, 1.02f),
            };
            // Few sides: big flat facets read as a cut stone. The upper shoulder catches the light.
            b.Lathe(Vector3.zero, profile, 8, band => band == 4 || band == 5 ? Palette.DewLight : Palette.Dew);
            b.Swatch = Palette.White;
            b.Blob(new Vector3(-0.16f, 0.5f, -0.22f), new Vector3(0.045f, 0.08f, 0.02f), Quaternion.Euler(0f, 35f, 25f));
            return b.ToMesh("Dewdrop");
        }

        const float BodyRadius = 0.41f;
        const float BodyHalfLength = 0.5f;
        const int BodySegments = 14;

        /// <summary>
        /// Body bands (tail to face, 14 of them) drawn dark: the tail tip and two stripes behind
        /// the middle, leaving the front half for the face.
        /// </summary>
        static bool IsStripe(int band) => band == 0 || band == 3 || band == 4 || band == 7 || band == 8;

        /// <summary>Radius of the skep's body at height <paramref name="y"/>: widest just above the middle.</summary>
        static float HiveRadius(float y)
        {
            float t = (y - 0.5f) / 1.08f;
            return 0.82f * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t));
        }

        /// <summary>Point where the ray from the centre along <paramref name="direction"/> meets the ellipsoid, and the surface normal there.</summary>
        static Vector3 Surface(Vector3 radii, Vector3 direction, out Vector3 normal)
        {
            Vector3 d = direction.normalized;
            float t = 1f / Mathf.Sqrt(d.x * d.x / (radii.x * radii.x) + d.y * d.y / (radii.y * radii.y) + d.z * d.z / (radii.z * radii.z));
            Vector3 point = d * t;
            normal = new Vector3(point.x / (radii.x * radii.x), point.y / (radii.y * radii.y), point.z / (radii.z * radii.z)).normalized;
            return point;
        }
    }
}
