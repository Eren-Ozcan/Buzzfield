using System.IO;
using UnityEditor;
using UnityEngine;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Menu "Buzzfield > Render Model Sheet": renders the three bee tiers and the six rare bees up
    /// close, the hive with the flower patches, and the butterfly with its Dewdrops pouch, in the
    /// game's light, to PNG files, for a look at the art without playing. Writes to BZ_SHOT_DIR when set, else Temp/ModelSheet.
    /// Batchmode: -executeMethod Buzzfield.Editor.ModelSheet.RenderToFiles -quit
    /// </summary>
    public static class ModelSheet
    {
        static readonly Color Sky = new Color(0.66f, 0.84f, 0.93f);

        [MenuItem("Buzzfield/Render Model Sheet", priority = 3)]
        public static void RenderFromMenu() => EditorUtility.RevealInFinder(RenderToFiles());

        /// <summary>Renders both sheets; returns the folder they were written to.</summary>
        public static string RenderToFiles()
        {
            string directory = System.Environment.GetEnvironmentVariable("BZ_SHOT_DIR");
            if (string.IsNullOrEmpty(directory))
                directory = Path.Combine("Temp", "ModelSheet");
            Directory.CreateDirectory(directory);

            // Bees at their model size (no tier scale), from slightly above like the game camera:
            // a Worker three-quarters from the front, a Forager from behind, a Golden in profile.
            RenderSheet(Path.Combine(directory, "models_bees.png"), 1500, 600, new Vector3(0f, 1.5f, -3.4f), 0f,
                ("Bee_Worker", new Vector3(-1.2f, 0f, 0f), 140f, 1f),
                ("Bee_Forager", new Vector3(0f, 0f, 0f), 30f, 1f),
                ("Bee_Golden", new Vector3(1.2f, 0f, 0f), 90f, 1f));
            // The rare bees three-quarters from the front, with a Worker for size.
            RenderSheet(Path.Combine(directory, "models_rare_bees.png"), 2000, 600, new Vector3(0f, 1.5f, -4.2f), 0f,
                ("Bee_Worker", new Vector3(-3.3f, 0f, 0f), 150f, 1f),
                ("Bee_Bumblebee", new Vector3(-2.2f, 0f, 0f), 150f, 1f),
                ("Bee_BlueBandedBee", new Vector3(-1.1f, 0f, 0f), 150f, 1f),
                ("Bee_MasonBee", new Vector3(0f, 0f, 0f), 150f, 1f),
                ("Bee_OrchidBee", new Vector3(1.1f, 0f, 0f), 120f, 1f),
                ("Bee_LeafcutterBee", new Vector3(2.2f, 0f, 0f), 150f, 1f),
                ("Bee_CarpenterBee", new Vector3(3.3f, 0f, 0f), 150f, 1f));
            // Hive and the three patches as they stand in the garden, with a Worker beside the hive for scale.
            RenderSheet(Path.Combine(directory, "models_garden.png"), 1500, 600, new Vector3(0f, 2.2f, -6.2f), 0.6f,
                ("Hive", new Vector3(-2.4f, 0f, 0f), 0f, 1f),
                ("Bee_Worker", new Vector3(-1.3f, 1.2f, -0.6f), 150f, 0.35f),
                ("FlowerPatch_Daisy", new Vector3(-0.2f, 0f, 0f), 30f, 1f),
                ("FlowerPatch_Lavender", new Vector3(1.4f, 0f, 0f), 60f, 1f),
                ("FlowerPatch_Orchid", new Vector3(3f, 0f, 0f), 10f, 1f));
            // The butterfly with the Dewdrops pouch hanging where the game puts it, and the Dewdrop crystal.
            RenderSheet(Path.Combine(directory, "models_visitors.png"), 1200, 600, new Vector3(0f, 1.3f, -3f), 0.5f,
                ("Butterfly", new Vector3(-0.5f, 0.75f, 0f), 200f, 1f),
                ("DewPouch", new Vector3(-0.5f, 0.7f, 0f), 200f, 1f),
                ("Dewdrop", new Vector3(1f, 0.1f, 0f), 20f, 0.8f));
            Debug.Log($"Model sheets written to {Path.GetFullPath(directory)}");
            return directory;
        }

        static void RenderSheet(string path, int width, int height, Vector3 cameraPosition, float lookHeight,
            params (string prefab, Vector3 position, float yaw, float scale)[] items)
        {
            var preview = new PreviewRenderUtility();
            try
            {
                Camera camera = preview.camera;
                camera.fieldOfView = 32f;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Sky;
                camera.transform.position = cameraPosition;
                camera.transform.LookAt(new Vector3(cameraPosition.x, lookHeight, 0f));

                // The game's key light and flat ambient (GreyboxSceneBuilder.CreateLight).
                preview.lights[0].intensity = 1.1f;
                preview.lights[0].color = new Color(1f, 0.96f, 0.88f);
                preview.lights[0].transform.rotation = Quaternion.Euler(55f, -30f, 0f);
                preview.lights[1].intensity = 0f;
                preview.ambientColor = new Color(0.55f, 0.58f, 0.62f);

                foreach ((string prefab, Vector3 position, float yaw, float scale) in items)
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{EditorAssets.PrefabRoot}/{prefab}.prefab");
                    if (asset == null)
                    {
                        Debug.LogError($"Model sheet: no prefab {prefab}.");
                        continue;
                    }
                    GameObject instance = preview.InstantiatePrefabInScene(asset);
                    instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
                    instance.transform.localScale = Vector3.one * scale;
                }

                preview.BeginStaticPreview(new Rect(0, 0, width, height));
                preview.Render(true);
                Texture2D image = preview.EndStaticPreview();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.DestroyImmediate(image);
            }
            finally
            {
                preview.Cleanup();
            }
        }
    }
}
