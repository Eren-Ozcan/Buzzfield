using Buzzfield.Ads;
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
            var boostSettings = Load<BoostSettings>("Settings/BoostSettings");
            var offlineSettings = Load<OfflineSettings>("Settings/OfflineSettings");
            var adSettings = Load<AdSettings>("Settings/AdSettings");
            var storeCatalog = Load<StoreCatalog>("Settings/StoreCatalog");
            var feedbackSettings = Load<UiFeedbackSettings>("Settings/UiFeedbackSettings");
            var queenSettings = Load<QueenSettings>("Settings/QueenSettings");
            if (gameSettings == null || economySettings == null || beeSettings == null || prestigeSettings == null
                || bloomSettings == null || addBee == null || speed == null || honeyValue == null
                || boostSettings == null || offlineSettings == null || adSettings == null || storeCatalog == null
                || feedbackSettings == null || queenSettings == null)
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
            CanvasViews ui = CreateCanvas(queenSettings.Abilities.Count);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var game = systems.AddComponent<GameManager>();
            EditorAssets.Set(game,
                ("gameSettings", gameSettings), ("economySettings", economySettings),
                ("beeSettings", beeSettings), ("bloomSettings", bloomSettings), ("prestigeSettings", prestigeSettings),
                ("boostSettings", boostSettings), ("offlineSettings", offlineSettings), ("adSettings", adSettings), ("storeCatalog", storeCatalog),
                ("feedbackSettings", feedbackSettings), ("queenSettings", queenSettings),
                ("addBeeUpgrade", addBee), ("speedUpgrade", speed), ("honeyValueUpgrade", honeyValue),
                ("flowerManager", flowerManager), ("beeManager", beeManager),
                ("hud", ui.Hud), ("bottomBar", ui.BottomBar), ("gardenComplete", ui.GardenComplete), ("queenPanel", ui.QueenPanel),
                ("welcomeBack", ui.WelcomeBack), ("tapBoostView", ui.TapBoost), ("rewardedBoostView", ui.RewardedBoost), ("shopPanel", ui.ShopPanel), ("tapCatcher", ui.TapCatcher), ("backButton", ui.BackButton),
                ("buttonFeedbacks", ui.ButtonFeedbacks),
                ("cameraFitter", fitter), ("worldRoot", world));

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

        struct CanvasViews
        {
            public HudView Hud;
            public BottomBarView BottomBar;
            public GardenCompleteView GardenComplete;
            public QueenPanelView QueenPanel;
            public WelcomeBackView WelcomeBack;
            public TapBoostView TapBoost;
            public RewardedBoostView RewardedBoost;
            public ShopPanelView ShopPanel;
            public TapCatcher TapCatcher;
            public BackButtonHandler BackButton;
            public ButtonFeedback[] ButtonFeedbacks;
        }

        /// <param name="abilityCount">Queen ability rows to build, one per ability in QueenSettings.</param>
        static CanvasViews CreateCanvas(int abilityCount)
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Behind everything else: presses that no UI element takes are taps on the world.
            RectTransform catcherRect = CreateRect("TapCatcher", canvasObject.transform);
            Stretch(catcherRect);
            catcherRect.gameObject.AddComponent<Image>().color = Color.clear;
            var tapCatcher = catcherRect.gameObject.AddComponent<TapCatcher>();

            // Root panel for everything on screen, fitted to the notch-free area.
            RectTransform root = CreateRect("SafeArea", canvasObject.transform);
            Stretch(root);
            root.gameObject.AddComponent<SafeArea>();

            RectTransform topBar = CreateRect("TopBar", root);
            topBar.anchorMin = new Vector2(0f, 1f);
            topBar.anchorMax = new Vector2(1f, 1f);
            topBar.pivot = new Vector2(0.5f, 1f);
            topBar.sizeDelta = new Vector2(0f, 260f);
            topBar.anchoredPosition = Vector2.zero;
            var background = topBar.gameObject.AddComponent<Image>();
            background.color = new Color(0.12f, 0.09f, 0.04f, 0.55f);
            // Blocks the tap catcher: a press on the top bar is not a tap on the world.
            background.raycastTarget = true;

            TMP_Text honey = CreateText("HoneyText", topBar, new Vector2(0f, -20f), 110f, 96f, FontStyles.Bold, new Color(1f, 0.85f, 0.3f));
            TMP_Text rate = CreateText("RateText", topBar, new Vector2(0f, -130f), 56f, 44f, FontStyles.Normal, Color.white);
            // Keep the centred readouts clear of the Queen button in the top-right corner.
            foreach (TMP_Text text in new[] { honey, rate })
                ((RectTransform)text.transform).sizeDelta = new Vector2(-2f * QueenButtonMargin, ((RectTransform)text.transform).sizeDelta.y);
            (Button queenButton, GameObject readyBadge) = CreateQueenButton(topBar);
            (Button shopButton, GameObject shopBadge) = CreateShopButton(topBar);

            (TMP_Text bloom, RectTransform bloomFill) = CreateBloomBar(topBar);

            var hud = topBar.gameObject.AddComponent<HudView>();
            EditorAssets.Set(hud, ("honeyText", honey), ("rateText", rate), ("bloomText", bloom), ("bloomFill", bloomFill));
            GardenCompleteView gardenComplete = CreateGardenComplete(root);
            BottomBarView bottomBar = CreateBottomBar(root);
            TapBoostView tapBoost = CreateTapBoost(root);
            RewardedBoostView rewardedBoost = CreateRewardedBoost(root);
            // Modals last so they draw over the HUD and the bottom bar; the quit dialog on top.
            QueenPanelView queenPanel = CreateQueenPanel(root, queenButton, readyBadge, abilityCount);
            ShopPanelView shopPanel = CreateShopPanel(root, shopButton, shopBadge);
            WelcomeBackView welcomeBack = CreateWelcomeBack(root);
            BackButtonHandler backButton = CreateQuitDialog(root, queenPanel, welcomeBack, shopPanel);
            return new CanvasViews
            {
                ButtonFeedbacks = AddButtonFeedback(canvasObject.transform),
                Hud = hud, BottomBar = bottomBar, GardenComplete = gardenComplete, QueenPanel = queenPanel,
                WelcomeBack = welcomeBack, TapBoost = tapBoost, RewardedBoost = rewardedBoost, ShopPanel = shopPanel, TapCatcher = tapCatcher, BackButton = backButton,
            };
        }

        /// <summary>Round cooldown indicator in the bottom-right corner, just above the bottom bar.</summary>
        static TapBoostView CreateTapBoost(Transform root)
        {
            RectTransform rect = CreateRect("TapBoost", root);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(130f, 130f);
            rect.anchoredPosition = new Vector2(-30f, 330f);

            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var track = rect.gameObject.AddComponent<Image>();
            track.sprite = circle;
            track.color = new Color(0.12f, 0.09f, 0.04f, 0.55f);
            track.raycastTarget = false;

            RectTransform ringRect = CreateRect("Ring", rect);
            Stretch(ringRect);
            var ring = ringRect.gameObject.AddComponent<Image>();
            ring.sprite = circle;
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = false;
            ring.raycastTarget = false;

            TMP_Text label = CreateText("Label", rect, Vector2.zero, 130f, 40f, FontStyles.Bold, new Color(0.2f, 0.12f, 0.02f));
            Stretch((RectTransform)label.transform);
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            label.text = Strings.TapBoostReady;

            var view = rect.gameObject.AddComponent<TapBoostView>();
            EditorAssets.Set(view, ("ring", ring), ("label", label));
            return view;
        }

        /// <summary>Rewarded honey boost button in the bottom-left corner, opposite the tap boost ring.</summary>
        static RewardedBoostView CreateRewardedBoost(Transform root)
        {
            (Button button, Image image, TMP_Text label) = CreateButton("RewardedBoost", root, Vector2.zero, new Vector2(300f, 130f), new Color(0.45f, 0.8f, 0.95f));
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(30f, 330f);
            label.fontSizeMax = 40f;
            label.text = string.Format(Strings.RewardedBoostOfferFormat, "2", "5m");

            var view = rect.gameObject.AddComponent<RewardedBoostView>();
            EditorAssets.Set(view, ("button", button), ("background", image), ("label", label));
            return view;
        }

        static WelcomeBackView CreateWelcomeBack(Transform root)
        {
            RectTransform panel = CreateModal("WelcomeBack", root, new Vector2(880f, 860f), new Color(0.24f, 0.2f, 0.1f, 0.97f), out RectTransform window);
            var titleColor = new Color(1f, 0.9f, 0.55f);
            TMP_Text title = CreateText("Title", window, new Vector2(0f, -40f), 110f, 80f, FontStyles.Bold, titleColor);
            title.text = Strings.WelcomeBackTitle;
            TMP_Text away = CreateText("AwayText", window, new Vector2(0f, -170f), 70f, 46f, FontStyles.Normal, Color.white);
            TMP_Text honey = CreateText("HoneyText", window, new Vector2(0f, -260f), 120f, 96f, FontStyles.Bold, new Color(1f, 0.85f, 0.3f));
            TMP_Text cap = CreateText("CapText", window, new Vector2(0f, -400f), 60f, 38f, FontStyles.Italic, new Color(0.85f, 0.8f, 0.7f));
            (Button ad, _, TMP_Text adLabel) = CreateButton("AdButton", window, new Vector2(0f, -500f), new Vector2(560f, 150f), new Color(0.45f, 0.8f, 0.95f));
            adLabel.text = string.Format(Strings.OfflineAdOfferFormat, "3");
            (Button collect, _, TMP_Text collectLabel) = CreateButton("CollectButton", window, new Vector2(0f, -690f), new Vector2(420f, 130f), new Color(0.98f, 0.76f, 0.2f));
            collectLabel.text = Strings.Collect;

            // On the always-active root, like the Queen panel view, so it can hide and show the panel.
            var view = root.gameObject.AddComponent<WelcomeBackView>();
            EditorAssets.Set(view, ("panel", panel.gameObject), ("awayText", away), ("honeyText", honey),
                ("capText", cap), ("collectButton", collect), ("adButton", ad), ("adLabel", adLabel));
            panel.gameObject.SetActive(false);
            return view;
        }

        static BackButtonHandler CreateQuitDialog(Transform root, QueenPanelView queenPanel, WelcomeBackView welcomeBack, ShopPanelView shopPanel)
        {
            RectTransform dialog = CreateModal("QuitDialog", root, new Vector2(820f, 480f), new Color(0.2f, 0.16f, 0.1f, 1f), out RectTransform window);
            TMP_Text title = CreateText("Title", window, new Vector2(0f, -50f), 100f, 66f, FontStyles.Bold, new Color(1f, 0.9f, 0.55f));
            title.text = Strings.QuitTitle;
            TMP_Text body = CreateText("Body", window, new Vector2(0f, -170f), 80f, 44f, FontStyles.Normal, Color.white);
            body.text = Strings.QuitBody;
            (Button quit, _, TMP_Text quitLabel) = CreateButton("QuitButton", window, new Vector2(190f, -290f), new Vector2(340f, 140f), new Color(0.98f, 0.76f, 0.2f));
            quitLabel.text = Strings.Quit;
            (Button cancel, _, TMP_Text cancelLabel) = CreateButton("CancelButton", window, new Vector2(-190f, -290f), new Vector2(340f, 140f), new Color(0.5f, 0.46f, 0.55f));
            cancelLabel.text = Strings.Cancel;

            var handler = root.gameObject.AddComponent<BackButtonHandler>();
            EditorAssets.Set(handler, ("queenPanel", queenPanel), ("welcomeBack", welcomeBack), ("shopPanel", shopPanel),
                ("quitDialog", dialog.gameObject), ("quitButton", quit), ("cancelButton", cancel));
            dialog.gameObject.SetActive(false);
            return handler;
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

            return (button, CreateBadge("ReadyBadge", rect, new Color(0.45f, 0.9f, 0.35f)));
        }

        /// <summary>Small dot in the top-right corner of a button; starts hidden.</summary>
        static GameObject CreateBadge(string name, RectTransform button, Color color)
        {
            RectTransform badge = CreateRect(name, button);
            badge.anchorMin = badge.anchorMax = new Vector2(1f, 1f);
            badge.sizeDelta = new Vector2(40f, 40f);
            badge.anchoredPosition = new Vector2(-6f, -6f);
            var badgeImage = badge.gameObject.AddComponent<Image>();
            badgeImage.color = color;
            badgeImage.raycastTarget = false;
            badge.gameObject.SetActive(false);
            return badge.gameObject;
        }

        /// <summary>Press feel on every button under <paramref name="canvas"/>, reusing ones added earlier.</summary>
        static ButtonFeedback[] AddButtonFeedback(Transform canvas)
        {
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            var feedbacks = new ButtonFeedback[buttons.Length];
            for (int i = 0; i < buttons.Length; i++)
                feedbacks[i] = AddButtonFeedback(buttons[i]);
            return feedbacks;
        }

        static ButtonFeedback AddButtonFeedback(Button button)
        {
            var feedback = button.GetComponent<ButtonFeedback>();
            if (feedback == null)
            {
                feedback = button.gameObject.AddComponent<ButtonFeedback>();
                EditorAssets.Set(feedback, ("button", button));
            }
            return feedback;
        }

        /// <summary>Shop button in the top-left corner of the top bar, mirroring the Queen button.</summary>
        static (Button, GameObject) CreateShopButton(Transform topBar)
        {
            RectTransform rect = CreateRect("ShopButton", topBar);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(QueenButtonSize, QueenButtonSize);
            rect.anchoredPosition = new Vector2(20f, -20f);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.3f, 0.7f, 0.55f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = CreateText("Label", rect, Vector2.zero, QueenButtonSize, 36f, FontStyles.Bold, Color.white);
            Stretch((RectTransform)label.transform);
            label.text = Strings.ShopButton;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.fontSizeMax = 36f;
            return (button, CreateBadge("NewBadge", rect, new Color(0.95f, 0.35f, 0.3f)));
        }

        const float ShopRowHeight = 240f;
        const float ShopRowSpacing = 24f;

        /// <summary>Modal shop: product rows in a vertical list (a hidden row leaves no gap), status line, restore and close.</summary>
        static ShopPanelView CreateShopPanel(Transform root, Button openButton, GameObject newBadge)
        {
            RectTransform panel = CreateModal("ShopPanel", root, new Vector2(900f, 1320f), new Color(0.12f, 0.26f, 0.22f, 0.97f), out RectTransform window);
            TMP_Text title = CreateText("Title", window, new Vector2(0f, -40f), 110f, 80f, FontStyles.Bold, new Color(1f, 0.9f, 0.55f));
            title.text = Strings.ShopTitle;

            RectTransform list = CreateRect("Items", window);
            list.anchorMin = new Vector2(0f, 1f);
            list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            list.sizeDelta = new Vector2(-60f, 3 * ShopRowHeight + 2 * ShopRowSpacing);
            list.anchoredPosition = new Vector2(0f, -170f);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = ShopRowSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var items = new[]
            {
                CreateShopItem(list, ProductIds.RemoveAds),
                CreateShopItem(list, ProductIds.PermanentHoney2x),
                CreateShopItem(list, ProductIds.StarterPack),
            };

            TMP_Text status = CreateText("StatusText", window, new Vector2(0f, -960f), 70f, 42f, FontStyles.Italic, Color.white);
            status.text = string.Empty;
            (Button restore, _, TMP_Text restoreLabel) = CreateButton("RestoreButton", window, new Vector2(0f, -1040f), new Vector2(520f, 120f), new Color(0.45f, 0.8f, 0.95f));
            restoreLabel.text = Strings.RestorePurchases;
            (Button close, _, TMP_Text closeLabel) = CreateButton("CloseButton", window, new Vector2(0f, -1180f), new Vector2(360f, 120f), new Color(0.5f, 0.46f, 0.55f));
            closeLabel.text = Strings.Close;

            // On the always-active root, like the other panel views, so it can open the hidden panel.
            var view = root.gameObject.AddComponent<ShopPanelView>();
            EditorAssets.Set(view, ("openButton", openButton), ("newBadge", newBadge), ("panel", panel.gameObject), ("closeButton", close),
                ("items", items), ("statusText", status), ("restoreButton", restore));
            panel.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Product row: name and description on the left, buy button with the price on the right.</summary>
        static ShopItemView CreateShopItem(Transform list, string productId)
        {
            RectTransform row = CreateRect(productId, list);
            row.sizeDelta = new Vector2(0f, ShopRowHeight);
            var background = row.gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.25f);
            background.raycastTarget = false;

            const float buttonWidth = 260f;
            TMP_Text title = CreateText("Title", row, new Vector2(0f, -24f), 70f, 50f, FontStyles.Bold, new Color(1f, 0.9f, 0.55f));
            TMP_Text body = CreateText("Body", row, new Vector2(0f, -100f), 120f, 34f, FontStyles.Normal, Color.white);
            foreach (TMP_Text text in new[] { title, body })
            {
                var rect = (RectTransform)text.transform;
                // Left column, clear of the buy button on the right.
                rect.offsetMin = new Vector2(30f, rect.offsetMin.y);
                rect.offsetMax = new Vector2(-(buttonWidth + 60f), rect.offsetMax.y);
                text.alignment = TextAlignmentOptions.TopLeft;
                text.enableAutoSizing = true;
                text.fontSizeMin = 22f;
                text.fontSizeMax = text.fontSize;
            }

            (Button buy, Image buyImage, TMP_Text price) = CreateButton("BuyButton", row, Vector2.zero, new Vector2(buttonWidth, 140f), new Color(0.98f, 0.76f, 0.2f));
            var buyRect = (RectTransform)buy.transform;
            buyRect.anchorMin = buyRect.anchorMax = new Vector2(1f, 0.5f);
            buyRect.pivot = new Vector2(1f, 0.5f);
            buyRect.anchoredPosition = new Vector2(-30f, 0f);

            var view = row.gameObject.AddComponent<ShopItemView>();
            EditorAssets.Set(view, ("productId", productId), ("titleText", title), ("bodyText", body),
                ("buyButton", buy), ("buyImage", buyImage), ("priceText", price));
            return view;
        }

        const float AbilityRowHeight = 138f;
        const float AbilityRowSpacing = 12f;
        const float AbilityListHeight = 4 * AbilityRowHeight + 3 * AbilityRowSpacing;

        /// <summary>
        /// Modal Queen panel plus its confirm dialog; both start hidden. Top: Queen level and
        /// Royal Jelly. Middle: one row per ability in a list that scrolls once there are more
        /// than four. Bottom: the "Move the Queen" gate, preview and button. 1560 px tall so
        /// it still fits a 4:3 tablet at the reference scaling.
        /// </summary>
        static QueenPanelView CreateQueenPanel(Transform root, Button openButton, GameObject readyBadge, int abilityCount)
        {
            RectTransform panel = CreateModal("QueenPanel", root, new Vector2(960f, 1560f), new Color(0.2f, 0.14f, 0.3f, 0.97f), out RectTransform window);

            var titleColor = new Color(1f, 0.9f, 0.55f);
            TMP_Text title = CreateText("Title", window, new Vector2(0f, -24f), 90f, 70f, FontStyles.Bold, titleColor);
            title.text = Strings.QueenTitle;
            TMP_Text level = CreateText("LevelText", window, new Vector2(0f, -120f), 64f, 48f, FontStyles.Bold, new Color(0.85f, 0.7f, 1f));
            TMP_Text nextLevel = CreateText("NextLevelText", window, new Vector2(0f, -186f), 46f, 32f, FontStyles.Italic, new Color(0.85f, 0.8f, 0.9f));
            TMP_Text jelly = CreateText("JellyText", window, new Vector2(0f, -240f), 60f, 44f, FontStyles.Bold, Color.white);

            TMP_Text abilitiesTitle = CreateText("AbilitiesTitle", window, new Vector2(0f, -305f), 50f, 36f, FontStyles.Bold, titleColor);
            abilitiesTitle.text = Strings.AbilitiesTitle;
            QueenAbilityRowView[] rows = CreateAbilityList(window, new Vector2(0f, -360f), abilityCount);

            TMP_Text moveTitle = CreateText("MoveTitle", window, new Vector2(0f, -975f), 64f, 50f, FontStyles.Bold, titleColor);
            moveTitle.text = Strings.MoveTheQueen;
            TMP_Text garden = CreateText("GardenText", window, new Vector2(0f, -1040f), 44f, 34f, FontStyles.Normal, Color.white);
            TMP_Text requirement = CreateText("RequirementText", window, new Vector2(0f, -1085f), 44f, 34f, FontStyles.Normal, Color.white);
            TMP_Text cost = CreateText("CostText", window, new Vector2(0f, -1130f), 44f, 34f, FontStyles.Normal, Color.white);
            TMP_Text preview = CreateText("PreviewText", window, new Vector2(0f, -1180f), 74f, 58f, FontStyles.Bold, new Color(0.75f, 0.95f, 0.5f));
            TMP_Text bonus = CreateText("BonusText", window, new Vector2(0f, -1256f), 44f, 32f, FontStyles.Normal, Color.white);

            (Button move, Image moveImage, TMP_Text moveLabel) = CreateButton("MoveButton", window, new Vector2(0f, -1305f), new Vector2(600f, 130f), new Color(0.98f, 0.76f, 0.2f));
            moveLabel.text = Strings.MoveTheQueen;
            (Button close, _, TMP_Text closeLabel) = CreateButton("CloseButton", window, new Vector2(0f, -1450f), new Vector2(320f, 96f), new Color(0.5f, 0.46f, 0.55f));
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
                ("levelText", level), ("nextLevelText", nextLevel), ("abilityRows", rows),
                ("jellyText", jelly), ("gardenText", garden), ("requirementText", requirement),
                ("costText", cost), ("previewText", preview), ("bonusText", bonus),
                ("moveButton", move), ("moveButtonImage", moveImage),
                ("confirm", confirm.gameObject), ("confirmBodyText", confirmBody),
                ("confirmButton", confirmButton), ("cancelButton", cancelButton));
            panel.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Vertical list of ability rows in a scroll view; it only scrolls when the rows outgrow it.</summary>
        static QueenAbilityRowView[] CreateAbilityList(Transform window, Vector2 position, int abilityCount)
        {
            RectTransform viewport = CreateRect("Abilities", window);
            viewport.anchorMin = new Vector2(0f, 1f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.pivot = new Vector2(0.5f, 1f);
            viewport.sizeDelta = new Vector2(-60f, AbilityListHeight);
            viewport.anchoredPosition = position;
            viewport.gameObject.AddComponent<RectMask2D>();
            // A clear graphic so drags between rows still reach the scroll view.
            viewport.gameObject.AddComponent<Image>().color = Color.clear;

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = AbilityRowSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var rows = new QueenAbilityRowView[abilityCount];
            for (int i = 0; i < abilityCount; i++)
                rows[i] = CreateAbilityRow(content, i);
            return rows;
        }

        /// <summary>Ability row: name and level on top, what a level does below, buy button with the jelly cost on the right.</summary>
        static QueenAbilityRowView CreateAbilityRow(Transform list, int index)
        {
            RectTransform row = CreateRect($"Ability{index}", list);
            row.sizeDelta = new Vector2(0f, AbilityRowHeight);
            var background = row.gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.25f);
            background.raycastTarget = false;

            const float buttonWidth = 220f;
            TMP_Text name = CreateText("Name", row, new Vector2(0f, -12f), 54f, 40f, FontStyles.Bold, new Color(1f, 0.9f, 0.55f));
            TMP_Text level = CreateText("Level", row, new Vector2(0f, -12f), 54f, 34f, FontStyles.Bold, Color.white);
            TMP_Text body = CreateText("Body", row, new Vector2(0f, -70f), 56f, 28f, FontStyles.Normal, Color.white);
            foreach (TMP_Text text in new[] { name, level, body })
            {
                var rect = (RectTransform)text.transform;
                // Left column, clear of the buy button on the right.
                rect.offsetMin = new Vector2(24f, rect.offsetMin.y);
                rect.offsetMax = new Vector2(-(buttonWidth + 48f), rect.offsetMax.y);
                text.alignment = TextAlignmentOptions.TopLeft;
                text.enableAutoSizing = true;
                text.fontSizeMin = 20f;
                text.fontSizeMax = text.fontSize;
            }
            level.alignment = TextAlignmentOptions.TopRight;

            (Button buy, Image buyImage, TMP_Text cost) = CreateButton("BuyButton", row, Vector2.zero, new Vector2(buttonWidth, 110f), new Color(0.98f, 0.76f, 0.2f));
            var buyRect = (RectTransform)buy.transform;
            buyRect.anchorMin = buyRect.anchorMax = new Vector2(1f, 0.5f);
            buyRect.pivot = new Vector2(1f, 0.5f);
            buyRect.anchoredPosition = new Vector2(-24f, 0f);
            cost.fontSizeMax = 40f;

            var view = row.gameObject.AddComponent<QueenAbilityRowView>();
            EditorAssets.Set(view, ("abilityIndex", index), ("nameText", name), ("levelText", level), ("bodyText", body),
                ("buyButton", buy), ("feedback", AddButtonFeedback(buy)), ("buyImage", buyImage), ("costText", cost));
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
            EditorAssets.Set(view, ("button", button), ("feedback", AddButtonFeedback(button)), ("background", image),
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
