using System.Collections.Generic;
using Buzzfield.Bees;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Create Default Data's part for the rare bees (docs/DEWDROPS.md 4.2): one modelled prefab, flight
    /// tier and <see cref="RareBee"/> asset per species and the <see cref="RareBeeSettings"/> that lists
    /// them; also the rebuild of palette meshes made for an older palette layout, which their colours needed.
    /// </summary>
    public static partial class DefaultDataCreator
    {
        const string RareData = Data + "/Bees/Rare";
        const int RareMaxLevel = 10;

        /// <summary>
        /// The species in Hive panel order. The bonus is per level (+30% carrying at level 10 and so
        /// on); the unlock garden counts from 1 and the price is in Dewdrops.
        /// </summary>
        static readonly (string id, CharacterModels.RareModel model, RareBeeEffect effect, float perLevel, int garden, int cost, float scale)[] RareSpecies =
        {
            (RareBeeIds.Bumblebee, CharacterModels.RareModel.Bumblebee, RareBeeEffect.CarryCapacityPercent, 3f, 2, 0, 0.5f),
            (RareBeeIds.BlueBandedBee, CharacterModels.RareModel.BlueBanded, RareBeeEffect.ShakeHoneyPercent, 8f, 2, 120, 0.48f),
            (RareBeeIds.MasonBee, CharacterModels.RareModel.Mason, RareBeeEffect.OfflineEfficiencyPoints, 3f, 3, 200, 0.48f),
            (RareBeeIds.OrchidBee, CharacterModels.RareModel.Orchid, RareBeeEffect.FlowerHoneyPercent, 6f, 3, 320, 0.48f),
            (RareBeeIds.LeafcutterBee, CharacterModels.RareModel.Leafcutter, RareBeeEffect.UpgradeDiscountPercent, 2f, 4, 480, 0.48f),
            (RareBeeIds.CarpenterBee, CharacterModels.RareModel.Carpenter, RareBeeEffect.RoyalJellyPercent, 4f, 5, 650, 0.5f),
        };

        /// <summary>Rare bees fly a little above the Golden bee (speed 5, capacity 16, 0.6 s on a flower): seen, but not carrying the economy alone.</summary>
        const float RareSpeed = 5.5f;
        const float RareCapacity = 18f;
        const float RareCollect = 0.55f;

        static void CreateRareBees(Materials m, FlowerTypes flowers)
        {
            var bees = new List<RareBee>();
            foreach ((string id, CharacterModels.RareModel model, RareBeeEffect effect, float perLevel, int garden, int cost, float scale) in RareSpecies)
            {
                GameObject prefab = EditorAssets.LoadOrCreatePrefab(RarePrefabName(id), () =>
                {
                    var root = new GameObject(RarePrefabName(id));
                    Mesh mesh = EditorAssets.SaveMesh(CharacterModels.RareBee(RarePrefabName(id), model));
                    // Like the tier bees: one renderer and no shadow.
                    EditorAssets.MeshObject("Body", root.transform, mesh, m.PaletteMaterial, castShadows: false);
                    return root;
                });
                BeeTier flight = EditorAssets.LoadOrCreate<BeeTier>($"{RareData}/Tier_{id}.asset", t => EditorAssets.Set(t,
                    ("speed", RareSpeed), ("capacity", RareCapacity), ("collectDuration", RareCollect),
                    ("scale", scale), ("prefab", prefab), ("evolveCost", 0d)));
                RareBee bee = EditorAssets.LoadOrCreate<RareBee>($"{RareData}/RareBee_{id}.asset", b =>
                {
                    EditorAssets.Set(b, ("id", id), ("effect", effect), ("effectPerLevel", perLevel), ("maxLevel", RareMaxLevel),
                        ("unlockGarden", garden), ("unlockCost", cost), ("flight", flight));
                    // The Orchid Bee: the two richer flowers.
                    if (effect == RareBeeEffect.FlowerHoneyPercent)
                        EditorAssets.SetList(b, "flowers", flowers.Lavender, flowers.Orchid);
                });
                // A deleted prefab or tier is made again and linked back, as for the tier bees.
                EditorAssets.SetIfMissing(flight, "prefab", prefab);
                EditorAssets.SetIfMissing(bee, "flight", flight);
                bees.Add(bee);
            }
            EditorAssets.LoadOrCreate<RareBeeSettings>($"{Data}/Bees/RareBeeSettings.asset", s => EditorAssets.SetList(s, "bees", bees.ToArray()));
        }

        static string RarePrefabName(string id) => $"Bee_{id}";

        /// <summary>
        /// Rebuilds, in place, every palette mesh whose UVs point at an older palette layout (the
        /// palette grew from 32 to 64 swatches): prefabs keep their references, only the UVs move.
        /// </summary>
        static void RefreshPaletteMeshes()
        {
            RefreshPaletteMesh("Bee_Worker", () => CharacterModels.Bee("Bee_Worker", Palette.Worker, Palette.Wing, false));
            RefreshPaletteMesh("Bee_Forager", () => CharacterModels.Bee("Bee_Forager", Palette.Forager, Palette.Wing, true));
            RefreshPaletteMesh("Bee_Golden", () => CharacterModels.Bee("Bee_Golden", Palette.Golden, Palette.GoldenWing, false));
            RefreshPaletteMesh("Hive", () => CharacterModels.Hive(out _));
            RefreshPaletteMesh("Scenery", () => SceneryModels.Scenery(seed: 7));
            RefreshPaletteMesh("ButterflyBody", CharacterModels.ButterflyBody);
            RefreshPaletteMesh("ButterflyWingLeft", () => CharacterModels.ButterflyWing(false));
            RefreshPaletteMesh("ButterflyWingRight", () => CharacterModels.ButterflyWing(true));
            RefreshPaletteMesh("Dewdrop", CharacterModels.Dewdrop);
            RefreshPaletteMesh("DewPouch", CharacterModels.DewPouch);
            foreach ((string id, CharacterModels.RareModel model, _, _, _, _, _) in RareSpecies)
                RefreshPaletteMesh(RarePrefabName(id), () => CharacterModels.RareBee(RarePrefabName(id), model));
        }

        static void RefreshPaletteMesh(string name, System.Func<Mesh> build)
        {
            var mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>($"{EditorAssets.MeshRoot}/{name}.asset");
            if (mesh == null || Palette.FitsLayout(mesh))
                return;
            EditorAssets.SaveMesh(build());
            Debug.Log($"Rebuilt {name} for the palette layout");
        }
    }
}
