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
        public static Mesh Bee(string name, int body, int wing, bool pollenBaskets) => BuildBee(name, new BeeLook
        {
            Band = band => IsStripe(band) ? Palette.Stripe : body,
            Wing = wing,
            Girth = 1f,
            Length = 1f,
            PollenBaskets = pollenBaskets,
        });

        /// <summary>The rare bee species (docs/DEWDROPS.md 4.2), each built on the Worker's shape.</summary>
        public enum RareModel
        {
            Bumblebee,
            BlueBanded,
            Mason,
            Orchid,
            Leafcutter,
            Carpenter,
        }

        /// <summary>
        /// A rare bee facing +Z like the tier bees: the same smiling shape in the species' colours,
        /// with its own size and details (the Bumblebee's fur, the Orchid Bee's long tongue, the
        /// Leafcutter's leaf, a glossy shine on the metallic ones).
        /// </summary>
        public static Mesh RareBee(string name, RareModel model)
        {
            switch (model)
            {
                case RareModel.Bumblebee:
                    // Big and round: yellow face and collar, black bands, an orange tail and tufts of fur.
                    return BuildBee(name, new BeeLook
                    {
                        Band = BumbleBand,
                        Wing = Palette.Wing, Girth = 1.18f, Length = 1.04f, Extras = Fuzz,
                    });
                case RareModel.BlueBanded:
                    // Dark and slender with bright turquoise bands and a pale face.
                    return BuildBee(name, new BeeLook
                    {
                        Band = band => band >= 11 ? Palette.BlueBandFace
                            : band % 2 == 1 && band <= 9 ? Palette.BlueBand
                            : Palette.BlueBandBody,
                        Wing = Palette.Wing, Girth = 0.94f, Length = 1.03f,
                    });
                case RareModel.Mason:
                    // Compact and metallic navy, with thin sheen rings and a shine on the back.
                    return BuildBee(name, new BeeLook
                    {
                        Band = band => band == 0 ? Palette.Stripe
                            : band >= 11 || band == 3 || band == 7 ? Palette.MasonSheen
                            : Palette.Mason,
                        Wing = Palette.Wing, Girth = 1f, Length = 0.95f, Extras = Gloss,
                    });
                case RareModel.Orchid:
                    // Bright metallic green with a long tongue reaching forward.
                    return BuildBee(name, new BeeLook
                    {
                        Band = band => band >= 11 || band == 2 || band == 5 || band == 8 ? Palette.OrchidBeeSheen : Palette.OrchidBee,
                        Wing = Palette.Wing, Girth = 0.97f, Length = 1.05f, Extras = Tongue,
                    });
                case RareModel.Leafcutter:
                    // Grey-beige with pale bands, carrying a round piece of leaf under its body.
                    return BuildBee(name, new BeeLook
                    {
                        Band = band => band == 0 ? Palette.Stripe
                            : IsStripe(band) ? Palette.LeafcutterPale
                            : Palette.Leafcutter,
                        Wing = Palette.Wing, Girth = 0.96f, Length = 1f, Extras = LeafLoad,
                    });
                default:
                    // Big, glossy purple-black with violet wings.
                    return BuildBee(name, new BeeLook
                    {
                        Band = band => band >= 11 || band == 3 || band == 7 ? Palette.CarpenterSheen : Palette.Carpenter,
                        Wing = Palette.CarpenterWing, Girth = 1.16f, Length = 1.08f, Extras = Gloss,
                    });
            }
        }

        /// <summary>How a bee is built: colours of its body bands, wings, size and extra details.</summary>
        struct BeeLook
        {
            /// <summary>Palette swatch of body band 0..13, tail to face.</summary>
            public System.Func<int, int> Band;
            public int Wing;
            /// <summary>Body radius and half length as factors of the Worker's.</summary>
            public float Girth;
            public float Length;
            public bool PollenBaskets;
            /// <summary>Added last, with the builder and the body's radii.</summary>
            public System.Action<LowPolyBuilder, Vector3> Extras;
        }

        static Mesh BuildBee(string name, BeeLook look)
        {
            var b = new LowPolyBuilder(seed: 51);
            float g = look.Girth;
            float radius = BodyRadius * g;
            float half = BodyHalfLength * look.Length;
            // Round body turned along +Z, so the stripes are clean rings.
            var radii = new Vector3(radius, radius, half);
            var profile = new List<Vector2>();
            for (int i = 0; i <= BodySegments; i++)
            {
                float z = -half + 2f * half * i / BodySegments;
                float t = z / half;
                profile.Add(new Vector2(radius * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)), z));
            }
            b.Lathe(Vector3.zero, profile, 14, look.Band, Quaternion.Euler(90f, 0f, 0f));

            // Eyes with a small highlight, rosy cheeks and a smile.
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 eye = Surface(radii, new Vector3(0.3f * side, 0.16f, 0.94f), out Vector3 normal);
                Quaternion facing = Quaternion.LookRotation(normal, Vector3.up);
                b.Swatch = Palette.Eye;
                b.Blob(eye + normal * 0.005f, new Vector3(0.07f, 0.085f, 0.03f) * g, facing);
                b.Swatch = Palette.White;
                b.Blob(eye + normal * 0.03f + facing * (new Vector3(0.025f, 0.035f, 0f) * g), new Vector3(0.022f, 0.022f, 0.01f) * g, facing);

                Vector3 cheek = Surface(radii, new Vector3(0.55f * side, -0.12f, 0.83f), out Vector3 cheekNormal);
                b.Swatch = Palette.Cheek;
                b.Blob(cheek, new Vector3(0.075f, 0.05f, 0.02f) * g, Quaternion.LookRotation(cheekNormal, Vector3.up));
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
            b.Swatch = look.Wing;
            foreach (float side in new[] { -1f, 1f })
            {
                Quaternion tilt = Quaternion.Euler(0f, 15f * side, 28f * side);
                b.Blob(new Vector3(0.24f * side * g, 0.38f * g, -0.1f * look.Length), new Vector3(0.3f, 0.02f, 0.17f) * g, tilt);
            }

            if (look.PollenBaskets)
            {
                b.Swatch = Palette.Pollen;
                foreach (float side in new[] { -1f, 1f })
                    b.Blob(new Vector3(0.3f * side * g, -0.22f * g, -0.2f * look.Length), Vector3.one * (0.1f * g), Quaternion.identity, 0.1f);
            }
            look.Extras?.Invoke(b, radii);
            return b.ToMesh(name);
        }

        /// <summary>The Bumblebee's bands, tail to face: orange tail, black, a yellow band, black, then the yellow collar and face.</summary>
        static int BumbleBand(int band) => band <= 2 ? Palette.BumbleTail
            : band <= 4 || (band >= 7 && band <= 9) ? Palette.BumbleBlack
            : Palette.BumbleYellow;

        /// <summary>
        /// The Bumblebee's fur: small rough tufts over the back half and the sides, each in the colour
        /// of its band, clear of the face so they never frame it.
        /// </summary>
        static void Fuzz(LowPolyBuilder b, Vector3 radii)
        {
            const int tufts = 26;
            for (int k = 0; k < tufts; k++)
            {
                float z = b.Range(-0.95f, 0f) * radii.z;
                float angle = b.Range(-0.15f, 1.15f) * Mathf.PI;
                float ring = radii.x * Mathf.Sqrt(Mathf.Max(0f, 1f - z * z / (radii.z * radii.z)));
                var point = new Vector3(Mathf.Cos(angle) * ring, Mathf.Sin(angle) * ring, z);
                Vector3 normal = new Vector3(point.x / (radii.x * radii.x), point.y / (radii.y * radii.y), point.z / (radii.z * radii.z)).normalized;
                int band = Mathf.Clamp(Mathf.FloorToInt((z + radii.z) / (2f * radii.z) * BodySegments), 0, BodySegments - 1);
                b.Swatch = BumbleBand(band);
                b.Blob(point + normal * 0.008f, new Vector3(0.06f, 0.06f, 0.035f), Quaternion.LookRotation(normal, Vector3.up), 0.3f);
            }
        }

        /// <summary>White shine on the back of the metallic species: a long highlight and a small one ahead of it.</summary>
        static void Gloss(LowPolyBuilder b, Vector3 radii)
        {
            b.Swatch = Palette.White;
            Vector3 spot = Surface(radii, new Vector3(-0.3f, 0.9f, -0.35f), out Vector3 normal);
            b.Blob(spot + normal * 0.006f, new Vector3(0.09f, 0.05f, 0.012f), Quaternion.LookRotation(normal, Vector3.forward));
            spot = Surface(radii, new Vector3(-0.2f, 0.95f, 0.15f), out normal);
            b.Blob(spot + normal * 0.006f, new Vector3(0.04f, 0.03f, 0.01f), Quaternion.LookRotation(normal, Vector3.forward));
        }

        /// <summary>The Orchid Bee's long tongue, a short way forward and down from under the smile, and its shine.</summary>
        static void Tongue(LowPolyBuilder b, Vector3 radii)
        {
            Vector3 root = Surface(radii, new Vector3(0f, -0.45f, 0.9f), out _);
            Vector3 tip = root + new Vector3(0f, -0.07f, 0.15f);
            b.Swatch = Palette.Stripe;
            b.Stalk(root, tip, 0.014f, 4);
            b.Blob(tip, Vector3.one * 0.018f, Quaternion.identity);
            Gloss(b, radii);
        }

        /// <summary>
        /// The Leafcutter's load: a round, rough-edged piece of leaf held under the body and wider than
        /// it, its front tipped up, so it shows from the game camera above; with its midrib.
        /// </summary>
        static void LeafLoad(LowPolyBuilder b, Vector3 radii)
        {
            var centre = new Vector3(0.03f, -radii.y * 0.82f, 0.1f);
            Quaternion tilt = Quaternion.Euler(-16f, 28f, 8f);
            b.Swatch = Palette.LeafLight;
            b.Blob(centre, new Vector3(0.38f, 0.03f, 0.27f), tilt, 0.18f);
            b.Swatch = Palette.LeafDark;
            b.Stalk(centre + tilt * new Vector3(-0.3f, 0.035f, 0f), centre + tilt * new Vector3(0.3f, 0.035f, 0f), 0.014f, 3);
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

        /// <summary>
        /// The butterfly's Dewdrops pouch: a round burlap sack tied at the neck, a small crystal
        /// peeking out of it, hanging on a short thread. The origin is the top of the thread (where
        /// it meets the butterfly); the sack hangs below, about 0.55 units in all.
        /// </summary>
        public static Mesh DewPouch()
        {
            var b = new LowPolyBuilder(seed: 73);
            const float top = -0.14f;
            var sack = new List<Vector2>
            {
                new Vector2(0f, top - 0.3f),
                new Vector2(0.1f, top - 0.29f),
                new Vector2(0.16f, top - 0.24f),
                new Vector2(0.18f, top - 0.16f),
                new Vector2(0.15f, top - 0.08f),
                new Vector2(0.08f, top - 0.03f),
                new Vector2(0.06f, top - 0.01f),
                new Vector2(0.1f, top + 0.03f),
                new Vector2(0.07f, top + 0.05f),
                new Vector2(0f, top + 0.04f),
            };
            // The neck (bands 5 and 6) is the tie.
            b.Lathe(Vector3.zero, sack, 7, band => band == 5 || band == 6 ? Palette.Wood : Palette.Dirt);

            // Stuffed in the opening and leaning out, big enough to read from the game camera; the thread is tied beside it.
            var crystal = new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(0.08f, 0.03f),
                new Vector2(0.1f, 0.09f),
                new Vector2(0.06f, 0.16f),
                new Vector2(0f, 0.22f),
            };
            b.Lathe(new Vector3(0.025f, top - 0.08f, 0f), crystal, 6, band => band == 2 ? Palette.DewLight : Palette.Dew,
                Quaternion.Euler(0f, 0f, -20f));

            b.Swatch = Palette.Stripe;
            b.Stalk(new Vector3(-0.06f, top + 0.04f, 0f), Vector3.zero, 0.009f, 4);
            return b.ToMesh("DewPouch", 1.3f);
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
