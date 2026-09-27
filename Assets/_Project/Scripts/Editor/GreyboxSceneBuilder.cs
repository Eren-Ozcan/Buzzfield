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
            var bloomSettings = Load<BloomSettings>("Flowers/BloomSettings");
            var addBee = Load<UpgradeDefinition>("Upgrades/Upgrade_AddBee");
            var speed = Load<UpgradeDefinition>("Upgrades/Upgrade_Speed");
            var honeyValue = Load<UpgradeDefinition>("Upgrades/Upgrade_HoneyValue");
            if (gameSettings == null || economySettings == null || beeSettings == null || prestigeSettings == null
                || bloomSettings == null || addBee == null || speed == null || honeyValue == null)
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
            (HudView hud, BottomBarView bottomBar, GardenCompleteView gardenComplete, QueenPanelView queenPanel) = CreateCanvas();
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var game = systems.AddComponent<GameManager>();
            EditorAssets.Set(game,
                ("gameSettings", gameSettings), ("economySettings", economySettings),
                ("beeSettings", beeSettings), ("bloomSettings", bloomSettings), ("prestigeSettings", prestigeSettings),
                ("addBeeUpgrade", addBee), ("speedUpgrade", speed), ("honeyValueUpgrade", honeyValue),
                ("flowerManager", flowerManager), ("beeManager", beeManager),
                ("hud", hud), ("bottomBar", bottomBar), ("gardenComplete", gardenComplete), ("queenPanel", queenPanel), ("cameraFitter", fitter), ("worldRoot", world));

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

        static (HudView, BottomBarView, GardenCompleteView, QueenPanelView) CreateCanvas()
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
            topBar.sizeDelta = new Vector2(0f, 260f);
            topBar.anchoredPosition = Vector2.zero;
            var background = topBar.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.09f, 0.04f, 0.55f);
            background.raycastTarget = false;

            TMP_Text honey = CreateText("HoneyText", topBar, new Vector2(0f, -20f), 110f, 96f, FontStyles.Bold, new Color(1f, 0.85f, 0.3f));
            TMP_Text rate = CreateText("RateText", topBar, new Vector2(0f, -130f), 56f, 44f, FontStyles.Normal, Color.white);
            // Keep the centred readouts clear of the Queen button in the top-right corner.
            foreach (TMP_Text text in new[] { honey, rate })
                ((RectTransform)text.transform).sizeDelta = new Vector2(-2f * QueenButtonMargin, ((RectTransform)text.transform).sizeDelta.y);
            (Button queenButton, GameObject readyBadge) = CreateQueenButton(topBar);

            (TMP_Text bloom, RectTransform bloomFill) = CreateBloomBar(topBar);

            var hud = topBar.gameObject.AddComponent<HudView>();
            EditorAssets.Set(hud, ("honeyText", honey), ("rateText", rate), ("bloomText", bloom), ("bloomFill", bloomFill));
            GardenCompleteView gardenComplete = CreateGardenComplete(root);
            BottomBarView bottomBar = CreateBottomBar(root);
            // Created last so the modal panel draws over the HUD and the bottom bar.
            QueenPanelView queenPanel = CreateQueenPanel(root, queenButton, readyBadge);
            return (hud, bottomBar, gardenComplete, queenPanel);
        }

        const float QueenButtonSize = 150f;
        const float QueenButtonMargin = QueenButtonSize + 40f;

        /// <summary>Queen button in the top-right corner of the top bar, with a dot shown when a move is possible.</summary>
        static (Button, GameObject) CreateQueenButton(Transform topBar)
        {
            RectTransform rect = CreateRect("QueenButton", topBar);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(QueenButtonSize, QueenButtonSize);
            rect.anchoredPosition = new Vector2(-20f, -20f);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.62f, 0.36f, 0.85f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = CreateText("Label", rect, Vector2.zero, QueenButtonSize, 36f, FontStyles.Bold, Color.white);
            Stretch((RectTransform)label.transform);
            label.text = Strings.QueenButton;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 36f;

            RectTransform badge = CreateRect("ReadyBadge", rect);
            badge.anchorMin = badge.anchorMax = new Vector2(1f, 1f);
            badge.sizeDelta = new Vector2(40f, 40f);
            badge.anchoredPosition = new Vector2(-6f, -6f);
            var badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.color = new Color(0.45f, 0.9f, 0.35f);
            badgeImage.raycastTarget = false;
            badge.gameObject.SetActive(false);
            return (button, badge.gameObject);
        }

        /// <summary>Modal Queen panel plus its confirm dialog; both start hidden.</summary>
        static QueenPanelView CreateQueenPanel(Transform root, Button openButton, GameObject readyBadge)
        {
            RectTransform panel = CreateModal("QueenPanel", root, new Vector2(900f, 1180f), new Color(0.2f, 0.14f, 0.3f, 0.97f), out RectTransform window);

            var titleColor = new Color(1f, 0.9f, 0.55f);
            TMP_Text title = CreateText("Title", window, new Vector2(0f, -40f), 110f, 80f, FontStyles.Bold, titleColor);
            title.text = Strings.QueenTitle;
            TMP_Text jelly = CreateText("JellyText", window, new Vector2(0f, -170f), 80f, 56f, FontStyles.Bold, Color.white);
            TMP_Text garden = CreateText("GardenText", window, new Vector2(0f, -260f), 64f, 44f, FontStyles.Normal, Color.white);

            TMP_Text moveTitle = CreateText("MoveTitle", window, new Vector2(0f, -380f), 80f, 60f, FontStyles.Bold, titleColor);
            moveTitle.text = Strings.MoveTheQueen;
            TMP_Text requirement = CreateText("RequirementText", window, new Vector2(0f, -470f), 60f, 42f, FontStyles.Normal, Color.white);
            TMP_Text cost = CreateText("CostText", window, new Vector2(0f, -540f), 60f, 42f, FontStyles.Normal, Color.white);
            TMP_Text preview = CreateText("PreviewText", window, new Vector2(0f, -630f), 90f, 68f, FontStyles.Bold, new Color(0.75f, 0.95f, 0.5f));
            TMP_Text bonus = CreateText("BonusText", window, new Vector2(0f, -730f), 60f, 40f, FontStyles.Normal, Color.white);

            (Button move, Image moveImage, TMP_Text moveLabel) = CreateButton("MoveButton", window, new Vector2(0f, -830f), new Vector2(620f, 160f), new Color(0.98f, 0.76f, 0.2f));
            moveLabel.text = Strings.MoveTheQueen;
            (Button close, _, TMP_Text closeLabel) = CreateButton("CloseButton", window, new Vector2(0f, -1020f), new Vector2(360f, 120f), new Color(0.5f, 0.46f, 0.55f));
            closeLabel.text = Strings.Close;

            RectTransform confirm = CreateModal("Confirm", panel, new Vector2(820f, 600f), new Color(0.26f, 0.18f, 0.36f, 1f), out RectTransform confirmWindow);
            TMP_Text confirmTitle = CreateText("Title", confirmWindow, new Vector2(0f, -40f), 100f, 66f, FontStyles.Bold, titleColor);
            confirmTitle.text = Strings.MoveConfirmTitle;
            TMP_Text confirmBody = CreateText("Body", confirmWindow, new Vector2(0f, -160f), 200f, 44f, FontStyles.Normal, Color.white);
            (Button confirmButton, _, TMP_Text confirmLabel) = CreateButton("ConfirmButton", confirmWindow, new Vector2(190f, -410f), new Vector2(340f, 140f), new Color(0.98f, 0.76f, 0.2f));
            confirmLabel.text = Strings.MoveConfirm;
            (Button cancelButton, _, TMP_Text cancelLabel) = CreateButton("CancelButton", confirmWindow, new Vector2(-190f, -410f), new Vector2(340f, 140f), new Color(0.5f, 0.46f, 0.55f));
            cancelLabel.text = Strings.Cancel;
            confirm.gameObject.SetActive(false);

            // The view sits on the always-active root so it can open the hidden panel.
            var view = root.gameObject.AddComponent<QueenPanelView>();
            EditorAssets.Set(view,
                ("openButton", openButton), ("readyBadge", readyBadge),
                ("panel", panel.gameObject), ("closeButton", close),
                ("jellyText", jelly), ("gardenText", garden), ("requirementText", requirement),
                ("costText", cost), ("previewText", preview), ("bonusText", bonus),
                ("moveButton", move), ("moveButtonImage", moveImage),
                ("confirm", confirm.gameObject), ("confirmBodyText", confirmBody),
                ("confirmButton", confirmButton), ("cancelButton", cancelButton));
            panel.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Full-screen dimmed backdrop that swallows taps, with a centred window.</summary>
        static RectTransform CreateModal(string name, Transform parent, Vector2 windowSize, Color windowColor, out RectTransform window)
        {
            RectTransform overlay = CreateRect(name, parent);
            Stretch(overlay);
            var backdrop = overlay.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.6f);

            window = CreateRect("Window", overlay);
            window.anchorMin = window.anchorMax = new Vector2(0.5f, 0.5f);
            window.sizeDelta = windowSize;
            window.anchoredPosition = Vector2.zero;
            var windowImage = window.gameObject.AddComponent<Image>();
            windowImage.color = windowColor;
            return overlay;
        }

        /// <summary>Button anchored to the top centre of <paramref name="parent"/>, offset by <paramref name="position"/>.</summary>
        static (Button, Image, TMP_Text) CreateButton(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = CreateText("Label", rect, Vector2.zero, size.y, 52f, FontStyles.Bold, new Color(0.2f, 0.12f, 0.02f));
            Stretch((RectTransform)label.transform);
            label.enableAutoSizing = true;
            label.fontSizeMin = 24f;
            label.fontSizeMax = 52f;
            return (button, image, label);
        }

        /// <summary>Thin bar along the bottom of the top bar; the fill's right anchor is the bloom fraction.</summary>
        static (TMP_Text, RectTransform) CreateBloomBar(Transform topBar)
        {
            RectTransform track = CreateRect("BloomBar", topBar);
            track.anchorMin = new Vector2(0f, 0f);
            track.anchorMax = new Vector2(1f, 0f);
            track.pivot = new Vector2(0.5f, 0f);
            track.sizeDelta = new Vector2(-80f, 44f);
            track.anchoredPosition = new Vector2(0f, 16f);
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = new Color(0.35f, 0.33f, 0.3f, 0.9f);
            trackImage.raycastTarget = false;

            RectTransform fill = CreateRect("Fill", track);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.45f, 0.78f, 0.35f);
            fillImage.raycastTarget = false;

            TMP_Text label = CreateText("BloomText", track, Vector2.zero, 44f, 32f, FontStyles.Bold, Color.white);
            var labelRect = (RectTransform)label.transform;
            Stretch(labelRect);
            label.text = string.Format(Strings.BloomFormat, 0);
            return (label, fill);
        }

        static GardenCompleteView CreateGardenComplete(Transform root)
        {
            RectTransform overlay = CreateRect("GardenComplete", root);
            Stretch(overlay);
            var group = overlay.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            RectTransform banner = CreateRect("Banner", overlay);
            banner.anchorMin = new Vector2(0f, 0.5f);
            banner.anchorMax = new Vector2(1f, 0.5f);
            banner.sizeDelta = new Vector2(-120f, 220f);
            banner.anchoredPosition = new Vector2(0f, 260f);
            var background = banner.gameObject.AddComponent<Image>();
            background.color = new Color(0.3f, 0.62f, 0.25f, 0.92f);
            background.raycastTarget = false;

            TMP_Text title = CreateText("Title", banner, Vector2.zero, 220f, 96f, FontStyles.Bold, new Color(1f, 0.95f, 0.6f));
            Stretch((RectTransform)title.transform);
            title.alignment = TextAlignmentOptions.Center;
            title.text = Strings.GardenComplete;
            title.enableAutoSizing = true;
            title.fontSizeMin = 40f;
            title.fontSizeMax = 96f;

            var view = overlay.gameObject.AddComponent<GardenCompleteView>();
            EditorAssets.Set(view, ("group", group), ("banner", banner), ("titleText", title));
            return view;
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
