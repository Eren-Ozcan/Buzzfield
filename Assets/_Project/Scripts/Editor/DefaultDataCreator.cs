using System.Collections.Generic;
using Buzzfield.Ads;
using Buzzfield.Bees;
using Buzzfield.Economy;
using Buzzfield.Flowers;
using Buzzfield.Game;
using Buzzfield.UI;
using Buzzfield.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Menu "Buzzfield > Create Default Data": materials, placeholder prefabs and every
    /// ScriptableObject with starting balance. Existing assets are never overwritten, so
    /// tuned values survive a re-run; delete an asset to regenerate it.
    ///
    /// Balance, checked with the balance report bot (BalanceReportTests):
    /// - One Worker round trip to the nearest Daisy is ~4.5 s for 2 nectar = ~0.45 honey/s,
    ///   so Add Bee at 4 honey is affordable within ~10 s.
    /// - Forager evolve at 100 honey (plus 3 Workers) lands around minute 3.
    /// - Bloom follows collected nectar and sprouts wake one per bloom: garden 1 blooms
    ///   fully in ~9 min and its 2K move follows within a minute; garden 2 takes ~17 min,
    ///   garden 3 ~19 min, and each loop after that ~16-17 min as the Queen grows.
    /// - The bot shakes every flower the moment it has pollen again: 2.5 s of bee income per
    ///   10 s sweep (+25% honey) with 40% of that nectar going to bloom.
    /// </summary>
    public static class DefaultDataCreator
    {
        const string Data = EditorAssets.DataRoot;

        [MenuItem("Buzzfield/Create Default Data", priority = 0)]
        public static void CreateAll()
        {
            TmpResources.EnsureImported();
            ProjectSetup.Apply();

            Materials materials = CreateMaterials();
            CreateBees(materials);
            var flowers = CreateFlowerTypes(materials);
            var hive = CreateHivePrefab(materials);
            var ground = CreateGroundPrefab(materials);
            var gardens = CreateGardens(flowers, hive, ground);
            CreateDecor(materials, gardens);
            CreateBloom(materials);
            CreatePollen(materials);

            CreateEconomy(gardens);
            CreateUpgrades();
            CreateMonetization();
            EditorAssets.LoadOrCreate<GameSettings>($"{Data}/Settings/GameSettings.asset", _ => { });
            EditorAssets.LoadOrCreate<UiFeedbackSettings>($"{Data}/Settings/UiFeedbackSettings.asset", _ => { });

            AssetDatabase.SaveAssets();
            Debug.Log("Buzzfield default data ready. Next: Buzzfield > Build Greybox Scene.");
        }

        struct Materials
        {
            public Material Ground, GroundSurface, Hive, HiveDoor, Stem, Wing, MergeFlash, BloomParticle;
            public Material Worker, Forager, Golden;
            public Material Daisy, Lavender, Orchid;
            public Material Grass, Bush, Stone, Wood;
        }

        static readonly Color DaisyColor = new Color(1f, 0.95f, 0.55f);
        static readonly Color LavenderColor = new Color(0.62f, 0.45f, 0.9f);
        static readonly Color OrchidColor = new Color(0.95f, 0.4f, 0.7f);

        static Materials CreateMaterials() => new Materials
        {
            Ground = EditorAssets.LoadOrCreateMaterial("Ground", new Color(0.55f, 0.6f, 0.5f)),
            // White: the runtime tile texture carries the colour.
            GroundSurface = EditorAssets.LoadOrCreateMaterial("GroundSurface", Color.white),
            Hive = EditorAssets.LoadOrCreateMaterial("Hive", new Color(0.93f, 0.68f, 0.2f)),
            HiveDoor = EditorAssets.LoadOrCreateMaterial("HiveDoor", new Color(0.25f, 0.15f, 0.05f)),
            Stem = EditorAssets.LoadOrCreateMaterial("FlowerStem", new Color(0.3f, 0.6f, 0.25f)),
            Wing = EditorAssets.LoadOrCreateMaterial("BeeWing", new Color(0.92f, 0.96f, 1f)),
            MergeFlash = EditorAssets.LoadOrCreateMaterial("MergeFlash", new Color(1f, 0.97f, 0.75f), "Universal Render Pipeline/Unlit"),
            // White: each burst tints its particles through the start colour.
            BloomParticle = EditorAssets.LoadOrCreateMaterial("BloomParticle", Color.white, "Universal Render Pipeline/Particles/Unlit"),
            Worker = EditorAssets.LoadOrCreateMaterial("BeeWorker", new Color(1f, 0.82f, 0.1f)),
            Forager = EditorAssets.LoadOrCreateMaterial("BeeForager", new Color(1f, 0.55f, 0.1f)),
            Golden = EditorAssets.LoadOrCreateMaterial("BeeGolden", new Color(1f, 0.9f, 0.45f)),
            Daisy = EditorAssets.LoadOrCreateMaterial("FlowerDaisy", DaisyColor),
            Lavender = EditorAssets.LoadOrCreateMaterial("FlowerLavender", LavenderColor),
            Orchid = EditorAssets.LoadOrCreateMaterial("FlowerOrchid", OrchidColor),
            // Darker than the bloomed ground, so tufts still read on a green garden.
            Grass = EditorAssets.LoadOrCreateMaterial("DecorGrass", new Color(0.3f, 0.55f, 0.22f)),
            Bush = EditorAssets.LoadOrCreateMaterial("DecorBush", new Color(0.26f, 0.5f, 0.24f)),
            Stone = EditorAssets.LoadOrCreateMaterial("DecorStone", new Color(0.64f, 0.63f, 0.6f)),
            Wood = EditorAssets.LoadOrCreateMaterial("DecorWood", new Color(0.6f, 0.42f, 0.25f)),
        };

        // ---- Bees ----

        static void CreateBees(Materials m)
        {
            BeeTier worker = CreateTier("Worker", m.Worker, m.Wing, speed: 2.5f, capacity: 2f, collect: 1f, scale: 0.35f, evolveCost: 0);
            // Forager: 3x capacity and faster, so one Forager beats the three Workers it costs (~+30%).
            BeeTier forager = CreateTier("Forager", m.Forager, m.Wing, speed: 3.5f, capacity: 6f, collect: 0.8f, scale: 0.42f, evolveCost: 100);
            // Golden: pays off on Lavender and Orchid loads; out of reach in garden 1, whose 2K move comes first.
            BeeTier golden = CreateTier("Golden", m.Golden, m.Wing, speed: 5f, capacity: 16f, collect: 0.6f, scale: 0.5f, evolveCost: 2500);

            GameObject flash = EditorAssets.LoadOrCreatePrefab("MergeFlash", () =>
            {
                var root = new GameObject("MergeFlash");
                EditorAssets.Primitive(PrimitiveType.Sphere, "Glow", root.transform, Vector3.zero, Vector3.one, m.MergeFlash);
                return root;
            });

            BeeSettings settings = EditorAssets.LoadOrCreate<BeeSettings>($"{Data}/Bees/BeeSettings.asset", s =>
            {
                EditorAssets.SetList(s, "tiers", worker, forager, golden);
                EditorAssets.Set(s, ("maxBees", 150), ("startingBees", 1));
            });
            // Added in Phase 2; filled on older data too.
            EditorAssets.SetIfMissing(settings, "mergeFlashPrefab", flash);

            // Only ever emitted into by BeeManager: it loops with no emission of its own.
            ParticleSystem sparkle = CreateParticlePrefab("HoneySparkle", m.BloomParticle, ps =>
            {
                ParticleSystem.MainModule main = ps.main;
                main.loop = true;
                main.playOnAwake = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.2f), new Color(1f, 0.95f, 0.6f));
                main.gravityModifier = -0.15f;
                main.maxParticles = 200;

                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = 0f;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.1f;
            });
            EditorAssets.SetIfMissing(settings, "depositSparklePrefab", sparkle);
        }

        static BeeTier CreateTier(string name, Material body, Material wing, float speed, float capacity, float collect, float scale, double evolveCost)
        {
            GameObject prefab = EditorAssets.LoadOrCreatePrefab($"Bee_{name}", () =>
            {
                var root = new GameObject($"Bee_{name}");
                // Body first: BeeManager treats the first renderer as the body for material overrides.
                EditorAssets.Primitive(PrimitiveType.Sphere, "Body", root.transform, Vector3.zero, new Vector3(0.8f, 0.8f, 1.1f), body);
                EditorAssets.Primitive(PrimitiveType.Sphere, "WingL", root.transform, new Vector3(-0.35f, 0.35f, 0f), new Vector3(0.55f, 0.08f, 0.35f), wing);
                EditorAssets.Primitive(PrimitiveType.Sphere, "WingR", root.transform, new Vector3(0.35f, 0.35f, 0f), new Vector3(0.55f, 0.08f, 0.35f), wing);
                return root;
            });

            return EditorAssets.LoadOrCreate<BeeTier>($"{Data}/Bees/Tier_{name}.asset", t => EditorAssets.Set(t,
                ("speed", speed), ("capacity", capacity), ("collectDuration", collect),
                ("scale", scale), ("prefab", prefab), ("evolveCost", evolveCost)));
        }

        // ---- Flowers ----

        struct FlowerTypes
        {
            public FlowerType Daisy, Lavender, Orchid;
        }

        /// <summary>Patches are built at a comfortable size, then scaled up to read from the camera.</summary>
        const float PatchScale = 1.4f;

        enum PatchShape
        {
            Daisy,
            Lavender,
            Orchid,
        }

        static FlowerTypes CreateFlowerTypes(Materials m) => new FlowerTypes
        {
            // Nectar to bloom over regen is the fastest a flower can bloom: ~2 min for a Daisy,
            // ~6 min for a Lavender and ~12 min for an Orchid. Full flowers hold a Golden-sized load.
            // Daisy: cheap and quick to refill; the whole first garden starts on these.
            Daisy = CreateFlowerType("Daisy", PatchShape.Daisy, m.Daisy, m.Stem, DaisyColor, maxNectar: 12f, regen: 0.6f, value: 1f, maxBees: 2, nectarToBloom: 70f),
            // Lavender: 3x value, sprouts in garden 1 and is common from garden 2.
            Lavender = CreateFlowerType("Lavender", PatchShape.Lavender, m.Lavender, m.Stem, LavenderColor, maxNectar: 20f, regen: 0.5f, value: 3f, maxBees: 3, nectarToBloom: 170f),
            // Orchid: slow refill, 8x value; the late-garden earner.
            Orchid = CreateFlowerType("Orchid", PatchShape.Orchid, m.Orchid, m.Stem, OrchidColor, maxNectar: 32f, regen: 0.35f, value: 8f, maxBees: 3, nectarToBloom: 250f),
        };

        static FlowerType CreateFlowerType(string name, PatchShape shape, Material head, Material stem, Color bloomed, float maxNectar, float regen, float value, int maxBees, float nectarToBloom)
        {
            FlowerView patch = CreateFlowerPatch(name, shape, head, stem);
            FlowerType type = EditorAssets.LoadOrCreate<FlowerType>($"{Data}/Flowers/Flower_{name}.asset", t => EditorAssets.Set(t,
                ("maxNectar", maxNectar), ("regenPerSecond", regen), ("nectarValue", value),
                ("maxBeesTargeting", maxBees), ("nectarToBloom", nectarToBloom), ("bloomedColor", bloomed),
                ("prefab", patch)));
            // Patches replaced the single placeholder flower after the first data; filled on older data too.
            EditorAssets.SetIfMissing(type, "prefab", patch);
            return type;
        }

        /// <summary>
        /// A patch of several flowers of one kind: one merged mesh for the stems and leaves, one
        /// for the heads (the part the bloom tints) and the nectar point just above the tallest head.
        /// </summary>
        static FlowerView CreateFlowerPatch(string name, PatchShape shape, Material head, Material stem)
        {
            GameObject prefab = EditorAssets.LoadOrCreatePrefab($"FlowerPatch_{name}", () =>
            {
                var greens = new LowPolyBuilder(seed: 11 + (int)shape);
                var heads = new LowPolyBuilder(seed: 23 + (int)shape);
                float top = shape switch
                {
                    PatchShape.Daisy => DaisyPatch(greens, heads),
                    PatchShape.Lavender => LavenderPatch(greens, heads),
                    _ => OrchidPatch(greens, heads),
                };
                Mesh greenMesh = EditorAssets.SaveMesh(greens.ToMesh($"FlowerPatch_{name}_Greens", PatchScale));
                Mesh headMesh = EditorAssets.SaveMesh(heads.ToMesh($"FlowerPatch_{name}_Heads", PatchScale));

                var root = new GameObject($"FlowerPatch_{name}");
                EditorAssets.MeshObject("Greens", root.transform, greenMesh, stem);
                GameObject headObject = EditorAssets.MeshObject("Heads", root.transform, headMesh, head);
                var nectarPoint = new GameObject("NectarPoint").transform;
                nectarPoint.SetParent(root.transform, false);
                nectarPoint.localPosition = new Vector3(0f, top * PatchScale + 0.1f, 0f);

                var view = root.AddComponent<FlowerView>();
                EditorAssets.Set(view, ("nectarPoint", nectarPoint), ("headRenderer", headObject.GetComponent<Renderer>()));
                return root;
            });
            return prefab.GetComponent<FlowerView>();
        }

        /// <summary>Direction <paramref name="index"/> of a sunflower spiral, so stems never line up.</summary>
        static Vector3 SpiralDirection(int index, float offsetDegrees)
        {
            float angle = (index * 137.5f + offsetDegrees) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        /// <summary>Repeatable 0..1 spread for the heights in a patch.</summary>
        static float Spread(int index) => index * 0.618f % 1f;

        static void Leaves(LowPolyBuilder greens, int count, float length, float lift, float width, float offsetDegrees)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (i * 360f / count + offsetDegrees) * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                greens.Leaf(new Vector3(0f, 0.02f, 0f), direction * length + Vector3.up * lift, width);
            }
        }

        /// <summary>Five flat flower discs on thin stems over a rosette of leaves. Returns the top height.</summary>
        static float DaisyPatch(LowPolyBuilder greens, LowPolyBuilder heads)
        {
            const int count = 5;
            float top = 0f;
            for (int k = 0; k < count; k++)
            {
                Vector3 direction = SpiralDirection(k, 20f);
                float radius = 0.08f + 0.26f * Mathf.Sqrt((k + 0.5f) / count);
                float height = 0.48f + 0.2f * Spread(k);
                Vector3 tip = direction * radius + Vector3.up * height;
                greens.Stalk(direction * (radius * 0.5f), tip, 0.025f, 4);
                Quaternion tilt = Quaternion.AngleAxis(14f, Vector3.Cross(Vector3.up, direction));
                heads.Blob(tip + Vector3.up * 0.03f, new Vector3(0.17f, 0.06f, 0.17f), tilt, 0.08f);
                top = Mathf.Max(top, height + 0.09f);
            }
            Leaves(greens, 5, 0.38f, 0.12f, 0.13f, 10f);
            return top;
        }

        /// <summary>Seven tall spikes of buds among narrow upright leaves. Returns the top height.</summary>
        static float LavenderPatch(LowPolyBuilder greens, LowPolyBuilder heads)
        {
            const int count = 7;
            float top = 0f;
            for (int k = 0; k < count; k++)
            {
                Vector3 direction = SpiralDirection(k, 60f);
                float radius = 0.05f + 0.24f * Mathf.Sqrt((k + 0.5f) / count);
                float height = 0.7f + 0.28f * Spread(k);
                Vector3 foot = direction * (radius * 0.4f);
                Vector3 tip = direction * radius + Vector3.up * height;
                greens.Stalk(foot, tip, 0.018f, 4);
                Quaternion along = Quaternion.FromToRotation(Vector3.up, (tip - foot).normalized);
                heads.Blob(tip + Vector3.up * 0.06f, new Vector3(0.065f, 0.2f, 0.065f), along, 0.1f);
                top = Mathf.Max(top, height + 0.26f);
            }
            Leaves(greens, 6, 0.3f, 0.3f, 0.05f, 25f);
            return top;
        }

        /// <summary>Three arching stems, each with a wide bloom and a bud, over broad leaves. Returns the top height.</summary>
        static float OrchidPatch(LowPolyBuilder greens, LowPolyBuilder heads)
        {
            const int count = 3;
            float top = 0f;
            for (int k = 0; k < count; k++)
            {
                float angle = (k * 120f + 15f) * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                float height = 0.62f + 0.14f * Spread(k);
                Vector3 foot = direction * 0.06f;
                Vector3 bend = direction * 0.16f + Vector3.up * (height * 0.65f);
                Vector3 tip = direction * 0.3f + Vector3.up * height;
                greens.Stalk(foot, bend, 0.03f, 5);
                greens.Stalk(bend, tip, 0.026f, 5);
                Quaternion tilt = Quaternion.AngleAxis(25f, Vector3.Cross(Vector3.up, direction));
                heads.Blob(tip + direction * 0.05f + Vector3.up * 0.04f, new Vector3(0.2f, 0.13f, 0.2f), tilt, 0.12f);
                heads.Blob(bend + direction * 0.1f + Vector3.up * 0.1f, new Vector3(0.08f, 0.07f, 0.08f), tilt, 0.1f);
                top = Mathf.Max(top, height + 0.17f);
            }
            Leaves(greens, 3, 0.42f, 0.06f, 0.22f, 75f);
            return top;
        }

        static HiveView CreateHivePrefab(Materials m)
        {
            GameObject prefab = EditorAssets.LoadOrCreatePrefab("Hive", () =>
            {
                var root = new GameObject("Hive");
                EditorAssets.Primitive(PrimitiveType.Cylinder, "Body", root.transform, new Vector3(0f, 0.7f, 0f), new Vector3(1.3f, 0.7f, 1.3f), m.Hive);
                EditorAssets.Primitive(PrimitiveType.Sphere, "Roof", root.transform, new Vector3(0f, 1.4f, 0f), new Vector3(1.35f, 0.6f, 1.35f), m.Hive);
                // The door faces the camera (-Z), so deposits read clearly.
                EditorAssets.Primitive(PrimitiveType.Cube, "Door", root.transform, new Vector3(0f, 0.55f, -0.62f), new Vector3(0.4f, 0.3f, 0.1f), m.HiveDoor);
                var entrance = new GameObject("Entrance").transform;
                entrance.SetParent(root.transform, false);
                entrance.localPosition = new Vector3(0f, 0.6f, -0.8f);

                var view = root.AddComponent<HiveView>();
                EditorAssets.Set(view, ("entrance", entrance));
                return root;
            });
            return prefab.GetComponent<HiveView>();
        }

        static GroundView CreateGroundPrefab(Materials m)
        {
            GameObject prefab = EditorAssets.LoadOrCreatePrefab("GardenGround", () =>
            {
                // 1x1 footprint: GardenSpawner scales the root to the garden size.
                var root = new GameObject("GardenGround");
                EditorAssets.Primitive(PrimitiveType.Cube, "Slab", root.transform, new Vector3(0f, -0.1f, 0f), new Vector3(1f, 0.2f, 1f), m.Ground);
                // Quad lying flat just above the slab: UV u runs along +X, v along +Z, as GroundView expects.
                GameObject surface = EditorAssets.Primitive(PrimitiveType.Quad, "Surface", root.transform, new Vector3(0f, 0.002f, 0f), Vector3.one, m.GroundSurface);
                surface.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

                var view = root.AddComponent<GroundView>();
                EditorAssets.Set(view, ("surface", surface.GetComponent<Renderer>()));
                return root;
            });
            return prefab.GetComponent<GroundView>();
        }

        // ---- Garden decor ----

        static void CreateDecor(Materials m, List<GardenConfig> gardens)
        {
            GameObject grass = DecorPrefab("GrassTuft", m.Grass, 31, b =>
            {
                const int blades = 7;
                for (int i = 0; i < blades; i++)
                {
                    float angle = i * Mathf.PI * 2f / blades + b.Range(-0.3f, 0.3f);
                    var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    Vector3 tip = direction * b.Range(0.1f, 0.2f) + Vector3.up * b.Range(0.16f, 0.3f);
                    b.Blade(direction * b.Range(0.02f, 0.07f), tip, 0.07f);
                }
            });
            GameObject bush = DecorPrefab("Bush", m.Bush, 37, b =>
            {
                b.Blob(new Vector3(0f, 0.3f, 0f), new Vector3(0.5f, 0.36f, 0.5f), Quaternion.identity, 0.12f);
                b.Blob(new Vector3(0.38f, 0.22f, 0.12f), new Vector3(0.32f, 0.26f, 0.32f), Quaternion.Euler(0f, 40f, 0f), 0.12f);
                b.Blob(new Vector3(-0.32f, 0.2f, -0.14f), new Vector3(0.34f, 0.25f, 0.34f), Quaternion.Euler(0f, 75f, 0f), 0.12f);
            });
            GameObject stone = DecorPrefab("Stone", m.Stone, 41, b =>
                b.Blob(new Vector3(0f, 0.05f, 0f), new Vector3(0.26f, 0.15f, 0.2f), Quaternion.Euler(0f, 20f, 0f), 0.18f));
            GameObject post = DecorPrefab("FencePost", m.Wood, 43, b =>
            {
                b.Stalk(Vector3.zero, new Vector3(0f, 0.52f, 0f), 0.06f, 4);
                b.Spike(new Vector3(0f, 0.52f, 0f), new Vector3(0f, 0.62f, 0f), 0.06f, 4);
            });
            // 1 unit along +X: DecorLayout stretches it from one post to the next.
            GameObject rail = DecorPrefab("FenceRail", m.Wood, 47, b =>
            {
                b.Stalk(new Vector3(0f, 0.2f, 0f), new Vector3(1f, 0.2f, 0f), 0.03f, 4);
                b.Stalk(new Vector3(0f, 0.4f, 0f), new Vector3(1f, 0.4f, 0f), 0.03f, 4);
            });

            GardenDecor decor = EditorAssets.LoadOrCreate<GardenDecor>($"{Data}/Gardens/GardenDecor.asset", d =>
            {
                EditorAssets.SetList(d, "scatters",
                    Scatter(grass, 110, new Vector2(0.8f, 1.35f), clearance: 0.55f, spacing: 0.42f, DecorPlacement.Inside, tinted: true),
                    Scatter(bush, 14, new Vector2(0.75f, 1.2f), clearance: 1f, spacing: 1.4f, DecorPlacement.Border, tinted: true),
                    Scatter(stone, 8, new Vector2(0.7f, 1.3f), clearance: 0.8f, spacing: 1.6f, DecorPlacement.Inside, tinted: false));
                EditorAssets.Set(d, ("fencePost", post), ("fenceRail", rail));
            });

            for (int i = 0; i < gardens.Count; i++)
            {
                if (gardens[i].Decor != null)
                    continue;
                // Decor came after the first gardens; filled on older data too, each garden with its own seed.
                EditorAssets.Set(gardens[i], ("decor", decor), ("decorSeed", i + 1));
                EditorUtility.SetDirty(gardens[i]);
            }
        }

        static (string, object)[] Scatter(GameObject prefab, int count, Vector2 scale, float clearance, float spacing, DecorPlacement placement, bool tinted) => new (string, object)[]
        {
            ("prefab", prefab), ("count", count), ("scaleRange", scale), ("clearance", clearance),
            ("spacing", spacing), ("placement", placement), ("tinted", tinted),
        };

        /// <summary>Decor piece prefab: one low-poly mesh, one material.</summary>
        static GameObject DecorPrefab(string name, Material material, int seed, System.Action<LowPolyBuilder> build) =>
            EditorAssets.LoadOrCreatePrefab(name, () =>
            {
                var builder = new LowPolyBuilder(seed);
                build(builder);
                Mesh mesh = EditorAssets.SaveMesh(builder.ToMesh(name));
                var root = new GameObject(name);
                EditorAssets.MeshObject("Mesh", root.transform, mesh, material);
                return root;
            });

        // ---- Bloom ----

        static void CreateBloom(Materials m)
        {
            ParticleSystem burst = CreateParticlePrefab("BloomBurst", m.BloomParticle, ps =>
            {
                ParticleSystem.MainModule main = ps.main;
                main.duration = 1f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
                main.gravityModifier = 0.6f;
                main.maxParticles = 40;

                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.15f;
            });

            ParticleSystem confetti = CreateParticlePrefab("GardenConfetti", m.BloomParticle, ps =>
            {
                ParticleSystem.MainModule main = ps.main;
                main.duration = 2f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
                main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                main.gravityModifier = 0.25f;
                main.maxParticles = 220;
                var colors = new Gradient();
                colors.SetKeys(
                    new[]
                    {
                        new GradientColorKey(DaisyColor, 0f), new GradientColorKey(LavenderColor, 0.33f),
                        new GradientColorKey(OrchidColor, 0.66f), new GradientColorKey(new Color(0.45f, 0.85f, 0.4f), 1f),
                    },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                main.startColor = new ParticleSystem.MinMaxGradient(colors) { mode = ParticleSystemGradientMode.RandomColor };

                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 90), new ParticleSystem.Burst(0.4f, 90) });

                // GardenBloomManager sizes the box to the garden.
                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Box;

                ParticleSystem.RotationOverLifetimeModule rotation = ps.rotationOverLifetime;
                rotation.enabled = true;
                rotation.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            });

            BloomSettings settings = EditorAssets.LoadOrCreate<BloomSettings>($"{Data}/Flowers/BloomSettings.asset", _ => { });
            EditorAssets.SetIfMissing(settings, "bloomBurstPrefab", burst);
            EditorAssets.SetIfMissing(settings, "confettiPrefab", confetti);
        }

        // ---- Pollen ----

        static void CreatePollen(Materials m)
        {
            var pollen = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.25f), new Color(1f, 0.97f, 0.65f));

            // Only ever emitted into by PollenShaker: both loop with no emission of their own.
            ParticleSystem puff = CreateParticlePrefab("PollenPuff", m.BloomParticle, ps =>
            {
                ParticleSystem.MainModule main = ps.main;
                main.loop = true;
                main.playOnAwake = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
                main.startColor = pollen;
                main.gravityModifier = 0.15f;
                main.maxParticles = 400;

                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = 0f;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.3f;
            });

            ParticleSystem mote = CreateParticlePrefab("PollenMote", m.BloomParticle, ps =>
            {
                ParticleSystem.MainModule main = ps.main;
                main.loop = true;
                main.playOnAwake = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.4f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.13f);
                main.startColor = pollen;
                // Motes drift up out of the flowers.
                main.gravityModifier = -0.05f;
                main.maxParticles = 200;

                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = 0f;

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.3f;
            });

            PollenSettings settings = EditorAssets.LoadOrCreate<PollenSettings>($"{Data}/Flowers/PollenSettings.asset", _ => { });
            EditorAssets.SetIfMissing(settings, "puffPrefab", puff);
            EditorAssets.SetIfMissing(settings, "motePrefab", mote);
        }

        /// <summary>One-shot particle prefab: no play on awake, world space, particles shrink out.</summary>
        static ParticleSystem CreateParticlePrefab(string name, Material material, System.Action<ParticleSystem> setup)
        {
            GameObject prefab = EditorAssets.LoadOrCreatePrefab(name, () =>
            {
                var root = new GameObject(name);
                var ps = root.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                ParticleSystem.MainModule main = ps.main;
                main.playOnAwake = false;
                main.loop = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;

                ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
                size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

                setup(ps);

                var renderer = root.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                return root;
            });
            return prefab.GetComponent<ParticleSystem>();
        }

        // ---- Gardens ----

        static List<GardenConfig> CreateGardens(FlowerTypes f, HiveView hive, GroundView ground)
        {
            // Garden 1: hand-placed. 6 active Daisies around the hive side, 10 sprouts further out.
            var garden1 = new List<(Vector2, FlowerType, bool)>
            {
                (new Vector2(-3f, -3f), f.Daisy, true),
                (new Vector2(0f, -2.5f), f.Daisy, true),
                (new Vector2(3f, -3f), f.Daisy, true),
                (new Vector2(-4f, 0f), f.Daisy, true),
                (new Vector2(0f, 0.5f), f.Daisy, true),
                (new Vector2(4f, 0f), f.Daisy, true),
                (new Vector2(-4.5f, -5.5f), f.Daisy, false),
                (new Vector2(4.5f, -5.5f), f.Daisy, false),
                (new Vector2(-2f, 2.8f), f.Daisy, false),
                (new Vector2(2f, 2.8f), f.Daisy, false),
                (new Vector2(-4.8f, 3.5f), f.Daisy, false),
                (new Vector2(4.8f, 3.5f), f.Daisy, false),
                (new Vector2(-3f, 6f), f.Lavender, false),
                (new Vector2(3f, 6f), f.Lavender, false),
                (new Vector2(0f, 5f), f.Lavender, false),
                (new Vector2(0f, 7.8f), f.Lavender, false),
            };

            return new List<GardenConfig>
            {
                // Move costs sit just under the honey a full bloom brings in, so bloom stays the gate.
                CreateGarden("Garden_01", garden1, valueMultiplier: 1f, moveCost: 2_000, hive, ground),
                // Garden 2: 24 slots, x3 value; target 15-20 min.
                CreateGarden("Garden_02", Generate(f, seed: 2, active: 8, total: 24, daisyShare: 0.4f, lavenderShare: 0.4f), valueMultiplier: 3f, moveCost: 50_000, hive, ground),
                // Garden 3: 32 slots, x9 value; ~20 min.
                CreateGarden("Garden_03", Generate(f, seed: 3, active: 10, total: 32, daisyShare: 0.25f, lavenderShare: 0.4f), valueMultiplier: 9f, moveCost: 500_000, hive, ground),
            };
        }

        static GardenConfig CreateGarden(string name, List<(Vector2 position, FlowerType type, bool active)> slots, float valueMultiplier, double moveCost, HiveView hive, GroundView ground)
        {
            GardenConfig garden = EditorAssets.LoadOrCreate<GardenConfig>($"{Data}/Gardens/{name}.asset", g =>
            {
                EditorAssets.Set(g,
                    ("groundSize", new Vector2(12f, 18f)), ("tileGrid", new Vector2Int(12, 18)),
                    ("hivePosition", new Vector2(0f, -6.8f)), ("gardenValueMultiplier", valueMultiplier),
                    ("moveHoneyCost", moveCost), ("hivePrefab", hive), ("groundView", ground));
                var elements = new object[slots.Count];
                for (int i = 0; i < slots.Count; i++)
                    elements[i] = new (string, object)[]
                    {
                        ("position", slots[i].position), ("type", slots[i].type), ("startsActive", slots[i].active),
                    };
                EditorAssets.SetList(g, "slots", elements);
            });
            // Phase 3 replaced the plain ground prefab with the tiled one; filled on older data too.
            EditorAssets.SetIfMissing(garden, "groundView", ground);
            return garden;
        }

        /// <summary>
        /// Jittered-grid layout above the hive. Slots nearest the hive start active; flower
        /// value rises with distance (Daisy near, Orchid far).
        /// </summary>
        static List<(Vector2, FlowerType, bool)> Generate(FlowerTypes f, int seed, int active, int total, float daisyShare, float lavenderShare)
        {
            var random = new System.Random(seed);
            var candidates = new List<Vector2>();
            for (int row = 0; row < 9; row++)
            for (int col = 0; col < 7; col++)
                candidates.Add(new Vector2(-4.8f + col * 1.6f, -4.6f + row * 1.6f));

            // Fisher-Yates shuffle, then keep the first `total`.
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }
            var picked = candidates.GetRange(0, total);
            var hive = new Vector2(0f, -6.8f);
            picked.Sort((a, b) => (a - hive).sqrMagnitude.CompareTo((b - hive).sqrMagnitude));

            var slots = new List<(Vector2, FlowerType, bool)>(total);
            for (int i = 0; i < total; i++)
            {
                float jitterX = (float)(random.NextDouble() - 0.5) * 0.6f;
                float jitterY = (float)(random.NextDouble() - 0.5) * 0.6f;
                float rank = i / (float)total;
                FlowerType type = rank < daisyShare ? f.Daisy : rank < daisyShare + lavenderShare ? f.Lavender : f.Orchid;
                slots.Add((picked[i] + new Vector2(jitterX, jitterY), type, i < active));
            }
            return slots;
        }

        // ---- Economy, upgrades, monetization ----

        static void CreateEconomy(List<GardenConfig> gardens)
        {
            // Start from zero honey with one Worker (BeeSettings.startingBees).
            EditorAssets.LoadOrCreate<EconomySettings>($"{Data}/Settings/EconomySettings.asset", s => EditorAssets.Set(s,
                ("startingHoney", 0.0), ("honeyRateWindowSeconds", 30f), ("honeyRateBuckets", 30)));
            EditorAssets.LoadOrCreate<BoostSettings>($"{Data}/Settings/BoostSettings.asset", _ => { });
            EditorAssets.LoadOrCreate<OfflineSettings>($"{Data}/Settings/OfflineSettings.asset", _ => { });

            EditorAssets.LoadOrCreate<PrestigeSettings>($"{Data}/Settings/PrestigeSettings.asset", s =>
            {
                EditorAssets.SetList(s, "gardens", gardens.ConvertAll(g => (object)g).ToArray());
                // jellyBase 1000: a ~10K-honey first run gives floor(sqrt(10)) = 3 Royal Jelly, 6 with the full-bloom bonus.
                EditorAssets.Set(s, ("moveUnlockBloom", 0.6f), ("jellyScale", 1f), ("jellyBase", 1000.0));
            });

            var abilities = new object[]
            {
                CreateAbility(QueenAbilityIds.RoyalBrood, QueenEffect.StartingWorkers, perLevel: 1f, maxLevel: 5, baseCost: 2, multiplier: 2.5f),
                CreateAbility(QueenAbilityIds.RoyalWings, QueenEffect.FlightSpeedPercent, perLevel: 5f, maxLevel: 10, baseCost: 1, multiplier: 1.8f),
                CreateAbility(QueenAbilityIds.SweetMemory, QueenEffect.OfflineCapHours, perLevel: 1f, maxLevel: 6, baseCost: 3, multiplier: 2f),
                CreateAbility(QueenAbilityIds.PollenTouch, QueenEffect.BloomSpeedPercent, perLevel: 10f, maxLevel: 5, baseCost: 2, multiplier: 2.2f),
            };
            EditorAssets.LoadOrCreate<QueenSettings>($"{Data}/Settings/QueenSettings.asset", s =>
            {
                EditorAssets.SetList(s, "abilities", abilities);
                // Lifetime Royal Jelly per Queen level, +10% honey each. A first move gives a few
                // jelly (Lv 2); the first three gardens reach about Lv 10; Lv 50 is the long tail.
                EditorAssets.Set(s, ("maxLevel", 50), ("honeyBonusPerLevel", 0.1f), ("xpForLevel", LinearCurve(
                    (1, 1), (2, 4), (3, 10), (4, 20), (5, 35), (10, 150), (20, 700), (50, 6000))));
            });
        }

        /// <summary>Straight lines between the keys, so a level never needs less than the curve shows.</summary>
        static AnimationCurve LinearCurve(params (float time, float value)[] points)
        {
            var keys = new Keyframe[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                float inSlope = i > 0 ? Slope(points[i - 1], points[i]) : 0f;
                float outSlope = i < points.Length - 1 ? Slope(points[i], points[i + 1]) : 0f;
                keys[i] = new Keyframe(points[i].time, points[i].value, inSlope, outSlope);
            }
            return new AnimationCurve(keys);
        }

        static float Slope((float time, float value) a, (float time, float value) b) => (b.value - a.value) / (b.time - a.time);

        static QueenAbility CreateAbility(string id, QueenEffect effect, float perLevel, int maxLevel, double baseCost, float multiplier) =>
            EditorAssets.LoadOrCreate<QueenAbility>($"{Data}/Settings/Queen_{id}.asset", a => EditorAssets.Set(a,
                ("id", id), ("effect", effect), ("effectPerLevel", perLevel), ("maxLevel", maxLevel),
                ("baseCost", baseCost), ("costMultiplier", multiplier)));

        static void CreateUpgrades()
        {
            // Add Bee: first one at 4 honey (~10 s), +18% per bee bought.
            CreateUpgrade("AddBee", UpgradeKind.AddBee, baseCost: 4, multiplier: 1.18f, perLevel: 1f, maxLevel: 0);
            // Speed: +10% flight speed per level, capped so bees stay readable.
            CreateUpgrade("Speed", UpgradeKind.Speed, baseCost: 25, multiplier: 1.6f, perLevel: 0.1f, maxLevel: 25);
            // Honey Value: +15% honey per nectar per level.
            CreateUpgrade("HoneyValue", UpgradeKind.HoneyValue, baseCost: 40, multiplier: 1.7f, perLevel: 0.15f, maxLevel: 0);
        }

        static void CreateUpgrade(string name, UpgradeKind kind, double baseCost, float multiplier, float perLevel, int maxLevel) =>
            EditorAssets.LoadOrCreate<UpgradeDefinition>($"{Data}/Upgrades/Upgrade_{name}.asset", u => EditorAssets.Set(u,
                ("kind", kind), ("baseCost", baseCost), ("costMultiplier", multiplier),
                ("effectPerLevel", perLevel), ("maxLevel", maxLevel)));

        static void CreateMonetization()
        {
            EditorAssets.LoadOrCreate<AdSettings>($"{Data}/Settings/AdSettings.asset", _ => { });
            EditorAssets.LoadOrCreate<StoreCatalog>($"{Data}/Settings/StoreCatalog.asset", s => EditorAssets.SetList(s, "products",
                new (string, object)[] { ("id", "remove_ads"), ("type", StoreProductType.NonConsumable), ("fallbackPrice", "$2.99") },
                new (string, object)[] { ("id", "permanent_2x_honey"), ("type", StoreProductType.NonConsumable), ("fallbackPrice", "$4.99") },
                new (string, object)[] { ("id", "starter_pack"), ("type", StoreProductType.Consumable), ("fallbackPrice", "$0.99"), ("honeyGrant", 5000.0), ("jellyGrant", 5) }));
        }
    }
}
