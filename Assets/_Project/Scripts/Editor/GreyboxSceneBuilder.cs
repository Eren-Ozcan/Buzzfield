using Buzzfield.Bees;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Flowers;
using Buzzfield.Game;
using Buzzfield.UI;
using Buzzfield.Upgrades;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Buzzfield.Editor
{
    /// <summary>
    /// Menu "Buzzfield > Build Greybox Scene": writes Main.unity with camera, light,
    /// managers, HUD canvas and EventSystem, all references wired. The garden itself
    /// (ground, hive, flowers) is spawned at runtime from the GardenConfig, so the next
    /// garden can replace it after a Queen move.
    /// Batchmode: -executeMethod Buzzfield.Editor.GreyboxSceneBuilder.Build
    /// </summary>
    public static class GreyboxSceneBuilder
    {
        static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        [MenuItem("Buzzfield/Build Greybox Scene", priority = 1)]
        public static void Build()
        {
            if (!TmpResources.IsImported)
            {
                Debug.LogError("TextMeshPro resources are missing. Run Buzzfield > Create Default Data first.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // Load data after NewScene: opening a scene unloads unused assets, which would
            // turn references loaded earlier into dead objects.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var gameSettings = Load<GameSettings>("Settings/GameSettings");
            var economySettings = Load<EconomySettings>("Settings/EconomySettings");
            var beeSettings = Load<BeeSettings>("Bees/BeeSettings");
            var prestigeSettings = Load<PrestigeSettings>("Settings/PrestigeSettings");
            var addBee = Load<UpgradeDefinition>("Upgrades/Upgrade_AddBee");
            var speed = Load<UpgradeDefinition>("Upgrades/Upgrade_Speed");
            var honeyValue = Load<UpgradeDefinition>("Upgrades/Upgrade_HoneyValue");
            if (gameSettings == null || economySettings == null || beeSettings == null || prestigeSettings == null
                || addBee == null || speed == null || honeyValue == null)
            {
                Debug.LogError("Default data is missing. Run Buzzfield > Create Default Data first.");
                return;
            }

            CameraFitter fitter = CreateCamera();
            CreateLight();

            var world = new GameObject("World").transform;
            var systems = new GameObject("Systems");
            var flowerManager = systems.AddComponent<FlowerManager>();
            var beeManager = systems.AddComponent<BeeManager>();
            (HudView hud, BottomBarView bottomBar) = CreateCanvas();
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var game = systems.AddComponent<GameManager>();
            EditorAssets.Set(game,
                ("gameSettings", gameSettings), ("economySettings", economySettings),
                ("beeSettings", beeSettings), ("prestigeSettings", prestigeSettings),
                ("addBeeUpgrade", addBee), ("speedUpgrade", speed), ("honeyValueUpgrade", honeyValue),
                ("flowerManager", flowerManager), ("beeManager", beeManager),
                ("hud", hud), ("bottomBar", bottomBar), ("cameraFitter", fitter), ("worldRoot", world));

            EditorAssets.EnsureFolder(System.IO.Path.GetDirectoryName(EditorAssets.ScenePath));
            EditorSceneManager.SaveScene(scene, EditorAssets.ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(EditorAssets.ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"Greybox scene written to {EditorAssets.ScenePath}");
        }

        static T Load<T>(string relativePath) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>($"{EditorAssets.DataRoot}/{relativePath}.asset");

        static CameraFitter CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.66f, 0.84f, 0.93f);
            go.AddComponent<AudioListener>();
            go.transform.SetPositionAndRotation(new Vector3(0f, 18f, -20f), Quaternion.Euler(50f, 0f, 0f));
            return go.AddComponent<CameraFitter>();
        }

        static void CreateLight()
        {
            var go = new GameObject("Directional Light");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.None;
            go.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

            // Flat ambient keeps the low-poly placeholders readable without a skybox.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.skybox = null;
        }

        static (HudView, BottomBarView) CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Root panel for everything on screen; the safe-area component goes here in Phase 4.
            RectTransform root = CreateRect("SafeArea", canvasObject.transform);
            Stretch(root);

            RectTransform topBar = CreateRect("TopBar", root);
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.sizeDelta = new Vector2(0f, 200f);
            topBar.anchoredPosition = Vector2.zero;
            var background = topBar.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.09f, 0.04f, 0.55f);
            background.raycastTarget = false;

            TMP_Text honey = CreateText("HoneyText", topBar, new Vector2(0f, -20f), 110f, 96f, FontStyles.Bold, new Color(1f, 0.85f, 0.3f));
            TMP_Text rate = CreateText("RateText", topBar, new Vector2(0f, -130f), 56f, 44f, FontStyles.Normal, Color.white);

            var hud = topBar.gameObject.AddComponent<HudView>();
            EditorAssets.Set(hud, ("honeyText", honey), ("rateText", rate));
            return (hud, CreateBottomBar(root));
        }

        /// <summary>Four equal buttons in thumb reach; 250 px tall at the reference resolution.</summary>
        static BottomBarView CreateBottomBar(Transform root)
        {
            RectTransform bar = CreateRect("BottomBar", root);
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.sizeDelta = new Vector2(0f, 300f);
            bar.anchoredPosition = Vector2.zero;
            var background = bar.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.09f, 0.04f, 0.55f);

            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 25, 25);
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var view = bar.gameObject.AddComponent<BottomBarView>();
            EditorAssets.Set(view,
                ("addBeeButton", CreateUpgradeButton("AddBeeButton", bar)),
                ("speedButton", CreateUpgradeButton("SpeedButton", bar)),
                ("evolveButton", CreateUpgradeButton("EvolveButton", bar)),
                ("honeyValueButton", CreateUpgradeButton("HoneyValueButton", bar)));
            return view;
        }

        static UpgradeButtonView CreateUpgradeButton(string name, Transform parent)
        {
            RectTransform rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.98f, 0.76f, 0.2f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var dark = new Color(0.2f, 0.12f, 0.02f);
            TMP_Text title = CreateText("Title", rect, new Vector2(0f, -18f), 60f, 38f, FontStyles.Bold, dark);
            TMP_Text detail = CreateText("Detail", rect, new Vector2(0f, -88f), 50f, 32f, FontStyles.Normal, dark);
            TMP_Text cost = CreateText("Cost", rect, new Vector2(0f, -160f), 60f, 42f, FontStyles.Bold, dark);
            foreach (TMP_Text text in new[] { title, detail, cost })
            {
                // Narrow buttons: keep the side margin small and let long words shrink.
                ((RectTransform)text.transform).sizeDelta = new Vector2(-16f, ((RectTransform)text.transform).sizeDelta.y);
                text.enableAutoSizing = true;
                text.fontSizeMin = 20f;
                text.fontSizeMax = text.fontSize;
            }

            var view = rect.gameObject.AddComponent<UpgradeButtonView>();
            EditorAssets.Set(view, ("button", button), ("background", image),
                ("titleText", title), ("detailText", detail), ("costText", cost));
            return view;
        }

        static TMP_Text CreateText(string name, Transform parent, Vector2 position, float height, float fontSize, FontStyles style, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-80f, height);
            rect.anchoredPosition = position;

            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = "0";
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
