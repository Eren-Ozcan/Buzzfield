using Buzzfield.Audio;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Flowers;
using UnityEditor;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Default data for the game's feel: the swipe trail, the butterfly and its sparkles, the goal
    /// chain and the sound library (clips linked from Assets/_Project/Audio by file name).
    /// </summary>
    public static partial class DefaultDataCreator
    {
        /// <summary>Glowing streak behind the finger: honey gold at the finger, fading and thinning toward its tail.</summary>
        static TrailRenderer CreateSwipeTrail(Materials m)
        {
            GameObject prefab = EditorAssets.LoadOrCreatePrefab("SwipeTrail", () =>
            {
                var root = new GameObject("SwipeTrail");
                var trail = root.AddComponent<TrailRenderer>();
                trail.time = 0.28f;
                trail.minVertexDistance = 0.04f;
                trail.widthMultiplier = 0.32f;
                trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
                trail.numCapVertices = 4;
                trail.numCornerVertices = 2;
                trail.alignment = LineAlignment.View;
                trail.emitting = false;
                trail.autodestruct = false;
                var gradient = new Gradient();
                gradient.SetKeys(
                    new[] { new GradientColorKey(new Color(1f, 0.97f, 0.75f), 0f), new GradientColorKey(new Color(1f, 0.78f, 0.25f), 1f) },
                    new[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0f, 1f) });
                trail.colorGradient = gradient;
                trail.sharedMaterial = m.BloomParticle;
                trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                trail.receiveShadows = false;
                return root;
            });
            return prefab.GetComponent<TrailRenderer>();
        }

        // ---- Visitors ----

        static void CreateVisitors(Materials m)
        {
            GameObject butterfly = EditorAssets.LoadOrCreatePrefab("Butterfly", () =>
            {
                var root = new GameObject("Butterfly");
                EditorAssets.MeshObject("Body", root.transform, EditorAssets.SaveMesh(CharacterModels.ButterflyBody()), m.PaletteMaterial);
                GameObject left = EditorAssets.MeshObject("LeftWing", root.transform, EditorAssets.SaveMesh(CharacterModels.ButterflyWing(false)), m.PaletteMaterial);
                GameObject right = EditorAssets.MeshObject("RightWing", root.transform, EditorAssets.SaveMesh(CharacterModels.ButterflyWing(true)), m.PaletteMaterial);
                var view = root.AddComponent<ButterflyView>();
                EditorAssets.Set(view, ("leftWing", left.transform), ("rightWing", right.transform));
                return root;
            });

            ParticleSystem burst = CreateParticlePrefab("ButterflyBurst", m.BloomParticle, ps =>
            {
                ParticleSystem.MainModule main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.62f, 0.26f), new Color(1f, 0.95f, 0.7f));
                main.gravityModifier = 0.25f;
                main.maxParticles = 64;

                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });

                ParticleSystem.ShapeModule shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.2f;
            });

            VisitorSettings settings = EditorAssets.LoadOrCreate<VisitorSettings>($"{Data}/Flowers/VisitorSettings.asset", _ => { });
            EditorAssets.SetIfMissing(settings, "prefab", butterfly.GetComponent<ButterflyView>());
            EditorAssets.SetIfMissing(settings, "catchBurstPrefab", burst);
        }

        // ---- Goals ----

        /// <summary>
        /// The goal chain: the first goals teach the swipe, buying, Evolve and the bloom; the later
        /// ones lead through the Queen move and its abilities. Rewards are seconds of the bees'
        /// honey per second, so a claim is worth about the same share of the economy in every
        /// garden; they grow slowly along the chain.
        /// </summary>
        static void CreateGoals()
        {
            GoalSettings settings = EditorAssets.LoadOrCreate<GoalSettings>($"{Data}/Settings/GoalSettings.asset", s => EditorAssets.SetList(s, "goals",
                Goal(GoalKind.ShakeFlowers, 10, 30f, GoalDew[0]),
                Goal(GoalKind.OwnBees, 3, 30f, GoalDew[1]),
                Goal(GoalKind.SpeedLevel, 2, 30f, GoalDew[2]),
                Goal(GoalKind.BloomFlowers, 2, 40f, GoalDew[3]),
                Goal(GoalKind.EvolveBees, 1, 45f, GoalDew[4]),
                Goal(GoalKind.HoneyValueLevel, 3, 45f, GoalDew[5]),
                Goal(GoalKind.OwnBees, 8, 60f, GoalDew[6]),
                Goal(GoalKind.GardenBloomPercent, 60, 60f, GoalDew[7]),
                Goal(GoalKind.MoveQueen, 1, 60f, GoalDew[8]),
                Goal(GoalKind.AbilityLevels, 1, 60f, GoalDew[9]),
                Goal(GoalKind.ShakeFlowers, 300, 90f, GoalDew[10]),
                Goal(GoalKind.CompleteGardens, 1, 90f, GoalDew[11]),
                Goal(GoalKind.EvolveBees, 8, 90f, GoalDew[12]),
                // Lifetime blooms, not bees owned: Evolve keeps the swarm small, so "own 20 bees" would stall the chain.
                Goal(GoalKind.BloomFlowers, 60, 90f, GoalDew[13]),
                Goal(GoalKind.MoveQueen, 3, 120f, GoalDew[14]),
                Goal(GoalKind.AbilityLevels, 6, 120f, GoalDew[15]),
                Goal(GoalKind.CompleteGardens, 3, 120f, GoalDew[16]),
                Goal(GoalKind.MoveQueen, 5, 150f, GoalDew[17])));
            FillGoalDew(settings);
            FillEndlessGoals(settings);
        }

        /// <summary>
        /// Dewdrops per goal of the chain (docs/DEWDROPS.md section 3): 5 while the game is taught,
        /// 10 around the first garden and Queen move, 15-25 after.
        /// </summary>
        static readonly int[] GoalDew = { 5, 5, 5, 5, 10, 10, 10, 10, 10, 10, 15, 15, 20, 20, 20, 25, 25, 25 };

        /// <summary>
        /// Endless goals after the chain: lifetime counts only, each counted from the moment it
        /// starts, about one a day for a player who plays half an hour.
        /// </summary>
        static readonly (string, object)[][] EndlessGoals =
        {
            Goal(GoalKind.ShakeFlowers, 500, 60f, 15),
            Goal(GoalKind.BloomFlowers, 30, 60f, 15),
            Goal(GoalKind.EvolveBees, 10, 60f, 15),
            Goal(GoalKind.MoveQueen, 1, 90f, 15),
        };

        static (string, object)[] Goal(GoalKind kind, long target, float rewardSeconds, int dew) => new (string, object)[]
        {
            ("kind", kind), ("target", target), ("rewardSeconds", rewardSeconds), ("dewReward", dew),
        };

        /// <summary>Fills the Dewdrops of a goal chain made before goals paid them; a chain with any already set is left alone.</summary>
        static void FillGoalDew(GoalSettings settings)
        {
            var so = new SerializedObject(settings);
            SerializedProperty goals = so.FindProperty("goals");
            for (int i = 0; i < goals.arraySize; i++)
            {
                if (goals.GetArrayElementAtIndex(i).FindPropertyRelative("dewReward").intValue > 0)
                    return;
            }
            for (int i = 0; i < goals.arraySize; i++)
                goals.GetArrayElementAtIndex(i).FindPropertyRelative("dewReward").intValue = GoalDew[Mathf.Min(i, GoalDew.Length - 1)];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            Debug.Log("Filled GoalSettings.goals dewReward");
        }

        static void FillEndlessGoals(GoalSettings settings)
        {
            if (settings.EndlessGoals.Count > 0)
                return;
            EditorAssets.SetList(settings, "endlessGoals", EndlessGoals);
            EditorUtility.SetDirty(settings);
            Debug.Log("Filled GoalSettings.endlessGoals");
        }

        // ---- Dewdrops ----

        /// <summary>The Dewdrop crystal (it is rendered into the currency icon) and the Dewdrops settings.</summary>
        static void CreateDew(Materials m)
        {
            EditorAssets.LoadOrCreatePrefab("Dewdrop", () =>
            {
                var root = new GameObject("Dewdrop");
                EditorAssets.MeshObject("Crystal", root.transform, EditorAssets.SaveMesh(CharacterModels.Dewdrop()), m.PaletteMaterial);
                return root;
            });
            EditorAssets.LoadOrCreate<DewSettings>($"{Data}/Settings/DewSettings.asset", _ => { });
        }

        // ---- Sound ----

        /// <summary>The sound library with the mix from docs/audio.md; entries added later are filled in on older libraries.</summary>
        static void CreateSound()
        {
            SoundLibrary library = EditorAssets.LoadOrCreate<SoundLibrary>($"{Data}/Settings/SoundLibrary.asset", _ => { });
            SoundClipLinker.EnsureEntries(library);
            SoundClipLinker.Link(library);
        }
    }
}
