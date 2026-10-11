using Buzzfield.Ads;
using Buzzfield.Audio;
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
using UnityEngine.Rendering.Universal;
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
    public static partial class GreyboxSceneBuilder
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
            var pollenSettings = Load<PollenSettings>("Flowers/PollenSettings");
            var addBee = Load<UpgradeDefinition>("Upgrades/Upgrade_AddBee");
            var speed = Load<UpgradeDefinition>("Upgrades/Upgrade_Speed");
            var honeyValue = Load<UpgradeDefinition>("Upgrades/Upgrade_HoneyValue");
            var boostSettings = Load<BoostSettings>("Settings/BoostSettings");
            var offlineSettings = Load<OfflineSettings>("Settings/OfflineSettings");
            var adSettings = Load<AdSettings>("Settings/AdSettings");
            var storeCatalog = Load<StoreCatalog>("Settings/StoreCatalog");
            var feedbackSettings = Load<UiFeedbackSettings>("Settings/UiFeedbackSettings");
            var queenSettings = Load<QueenSettings>("Settings/QueenSettings");
            var postProfile = Load<VolumeProfile>("Settings/GardenPost");
            var soundLibrary = Load<SoundLibrary>("Settings/SoundLibrary");
            var goalSettings = Load<GoalSettings>("Settings/GoalSettings");
            var visitorSettings = Load<VisitorSettings>("Flowers/VisitorSettings");
            var dewSettings = Load<DewSettings>("Settings/DewSettings");
            var rareBeeSettings = Load<RareBeeSettings>("Bees/RareBeeSettings");
            if (gameSettings == null || economySettings == null || beeSettings == null || prestigeSettings == null
                || bloomSettings == null || pollenSettings == null || addBee == null || speed == null || honeyValue == null
                || boostSettings == null || offlineSettings == null || adSettings == null || storeCatalog == null
                || feedbackSettings == null || queenSettings == null || postProfile == null
                || soundLibrary == null || goalSettings == null || visitorSettings == null || dewSettings == null
                || rareBeeSettings == null)
            {
                Debug.LogError("Default data is missing. Run Buzzfield > Create Default Data first.");
                return;
            }

            CameraFitter fitter = CreateCamera();
            CreateLight();
            CreatePostProcessing(fitter.GetComponent<Camera>(), postProfile);

            var world = new GameObject("World").transform;
            var systems = new GameObject("Systems");
            var flowerManager = systems.AddComponent<FlowerManager>();
            var beeManager = systems.AddComponent<BeeManager>();
            LoadTheme();
            CanvasViews ui = CreateCanvas(queenSettings.Abilities.Count, storeCatalog, dewSettings, rareBeeSettings);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var game = systems.AddComponent<GameManager>();
            EditorAssets.Set(game,
                ("gameSettings", gameSettings), ("economySettings", economySettings),
                ("beeSettings", beeSettings), ("bloomSettings", bloomSettings), ("pollenSettings", pollenSettings), ("prestigeSettings", prestigeSettings),
                ("boostSettings", boostSettings), ("offlineSettings", offlineSettings), ("adSettings", adSettings), ("storeCatalog", storeCatalog),
                ("feedbackSettings", feedbackSettings), ("queenSettings", queenSettings),
                ("addBeeUpgrade", addBee), ("speedUpgrade", speed), ("honeyValueUpgrade", honeyValue),
                ("flowerManager", flowerManager), ("beeManager", beeManager),
                ("hud", ui.Hud), ("bottomBar", ui.BottomBar), ("gardenComplete", ui.GardenComplete), ("queenPanel", ui.QueenPanel),
                ("welcomeBack", ui.WelcomeBack), ("rewardedBoostView", ui.RewardedBoost), ("shopPanel", ui.ShopPanel),
                ("swipeCatcher", ui.SwipeCatcher), ("swipeHint", ui.SwipeHint), ("backButton", ui.BackButton),
                ("buttonFeedbacks", ui.ButtonFeedbacks),
                ("soundLibrary", soundLibrary), ("uiEffects", ui.Effects),
                ("goalSettings", goalSettings), ("goalView", ui.Goal),
                ("visitorSettings", visitorSettings), ("giftOffer", ui.GiftOffer),
                ("settingsPanel", ui.Settings), ("dewSettings", dewSettings), ("dewBoostChip", ui.DewBoostChip),
                ("dailyPanel", ui.Daily), ("rareBeeSettings", rareBeeSettings), ("hivePanel", ui.Hive),
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
            // The fog colour: distant hills fade into the sky.
            camera.backgroundColor = Haze;
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
            // Soft, light shadows ground the hive, flowers, trees and bees.
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            go.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

            // Flat ambient keeps the low-poly placeholders readable without a skybox.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.skybox = null;

            // Linear haze well behind the garden, so the hills fade into the sky.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Haze;
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 110f;
        }

        static readonly Color Haze = new Color(0.7f, 0.85f, 0.93f);

        static void CreatePostProcessing(Camera camera, VolumeProfile profile)
        {
            var volume = new GameObject("PostProcessing").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }

        struct CanvasViews
        {
            public HudView Hud;
            public BottomBarView BottomBar;
            public GardenCompleteView GardenComplete;
            public QueenPanelView QueenPanel;
            public WelcomeBackView WelcomeBack;
            public RewardedBoostView RewardedBoost;
            public ShopPanelView ShopPanel;
            public SwipeCatcher SwipeCatcher;
            public SwipeHintView SwipeHint;
            public BackButtonHandler BackButton;
            public ButtonFeedback[] ButtonFeedbacks;
            public SettingsPanelView Settings;
            public GoalView Goal;
            public UiEffectsView Effects;
            public GiftOfferView GiftOffer;
            public DewBoostChipView DewBoostChip;
            public DailyPanelView Daily;
            public HivePanelView Hive;
        }

        // ---- Interface theme (colours in UiTheme, art from UiAssets) ----

        static TMP_FontAsset textFont;
        static TMP_FontAsset iconFont;
        static Material outlinedText;
        static Sprite roundSprite;
        static Sprite buttonSprite;
        static Sprite pillSprite;
        static Sprite circleSprite;
        static Sprite shadowSprite;

        /// <summary>Lip of the button sprite: labels sit above it, on the face.</summary>
        const float ButtonLip = 14f;

        /// <summary>Loads (or makes) the fonts, sprites and icons the UI helpers below use.</summary>
        static void LoadTheme()
        {
            textFont = UiAssets.TextFont();
            iconFont = UiAssets.IconFont();
            outlinedText = UiAssets.OutlinedText();
            roundSprite = UiAssets.Round();
            buttonSprite = UiAssets.Button();
            pillSprite = UiAssets.Pill();
            circleSprite = UiAssets.Circle();
            shadowSprite = UiAssets.Shadow();
        }

        /// <param name="abilityCount">Queen ability rows to build, one per ability in QueenSettings.</param>
        /// <param name="catalog">The shop builds one row per product.</param>
        /// <param name="dewSettings">The Boosts tab builds one row per offer.</param>
        /// <param name="rareBeeSettings">The Hive panel builds one card per species.</param>
        static CanvasViews CreateCanvas(int abilityCount, StoreCatalog catalog, DewSettings dewSettings, RareBeeSettings rareBeeSettings)
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Behind everything else: presses and drags that no UI element takes are swipes on the world.
            RectTransform catcherRect = CreateRect("SwipeCatcher", canvasObject.transform);
            Stretch(catcherRect);
            catcherRect.gameObject.AddComponent<Image>().color = Color.clear;
            var swipeCatcher = catcherRect.gameObject.AddComponent<SwipeCatcher>();

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
            // Invisible: the HUD floats over the garden, but a press up here is not a swipe on the world.
            var blocker = topBar.gameObject.AddComponent<Image>();
            blocker.color = Color.clear;
            blocker.raycastTarget = true;

            (TMP_Text honey, TMP_Text rate, RectTransform honeyPill) = CreateHoneyCounter(topBar);
            (GameObject dewPill, TMP_Text dewText, Button dewButton) = CreateDewCounter(topBar);
            (Button queenButton, GameObject readyBadge) = CreateCornerButton("QueenButton", topBar, true, UiTheme.Royal, UiTheme.IconCrown, Strings.QueenButton, "ReadyBadge", UiTheme.Good);
            (Button shopButton, GameObject shopBadge) = CreateCornerButton("ShopButton", topBar, false, UiTheme.Shop, UiTheme.IconShop, Strings.ShopButton, "NewBadge", UiTheme.Alert);
            (TMP_Text bloom, RectTransform bloomFill) = CreateBloomBar(topBar);

            var hud = topBar.gameObject.AddComponent<HudView>();
            EditorAssets.Set(hud, ("honeyText", honey), ("rateText", rate), ("bloomText", bloom), ("bloomFill", bloomFill),
                ("dewPill", dewPill), ("dewText", dewText));
            Button settingsButton = CreateSettingsButton(root);
            RectTransform underBar = CreateUnderBarRow(root);
            (GameObject hiveButton, Button hiveOpenButton, GameObject hiveBadge) = CreateHiveButton(underBar);
            GoalView goal = CreateGoal(root, underBar);
            GardenCompleteView gardenComplete = CreateGardenComplete(root);
            BottomBarView bottomBar = CreateBottomBar(root);
            SwipeHintView swipeHint = CreateSwipeHint(root);
            RewardedBoostView rewardedBoost = CreateRewardedBoost(root);
            GiftOfferView giftOffer = CreateGiftOffer(root);
            DewBoostChipView dewBoostChip = CreateDewBoostChip(root);
            (GameObject dailyBadge, Button dailyBadgeButton) = CreateDailyBadge(root);
            // Over the HUD and the bottom bar, under the panels.
            UiEffectsView effects = CreateEffects(root, honeyPill, (RectTransform)dewPill.transform);
            // Modals last so they draw over the HUD and the bottom bar; the quit dialog on top.
            QueenPanelView queenPanel = CreateQueenPanel(root, queenButton, readyBadge, abilityCount);
            ShopPanelView shopPanel = CreateShopPanel(root, shopButton, shopBadge, dewButton, catalog, dewSettings);
            SettingsPanelView settings = CreateSettingsPanel(root, settingsButton);
            DailyPanelView daily = CreateDailyPanel(root, dailyBadge, dailyBadgeButton);
            HivePanelView hive = CreateHivePanel(root, hiveButton, hiveOpenButton, hiveBadge, rareBeeSettings);
            WelcomeBackView welcomeBack = CreateWelcomeBack(root);
            BackButtonHandler backButton = CreateQuitDialog(root, queenPanel, welcomeBack, shopPanel, settings, daily, hive);
            return new CanvasViews
            {
                ButtonFeedbacks = AddButtonFeedback(canvasObject.transform),
                Hud = hud, BottomBar = bottomBar, GardenComplete = gardenComplete, QueenPanel = queenPanel,
                WelcomeBack = welcomeBack, RewardedBoost = rewardedBoost, ShopPanel = shopPanel,
                SwipeCatcher = swipeCatcher, SwipeHint = swipeHint, BackButton = backButton,
                Settings = settings, Goal = goal, Effects = effects, GiftOffer = giftOffer, DewBoostChip = dewBoostChip,
                Daily = daily, Hive = hive,
            };
        }

        const float CornerButtonSize = 150f;
        const float CornerButtonMargin = CornerButtonSize + 40f;

        /// <summary>Honey pill at the top centre (drop icon and total), with the honey per second under it.</summary>
        static (TMP_Text honey, TMP_Text rate, RectTransform pill) CreateHoneyCounter(Transform topBar)
        {
            RectTransform pill = CreateRect("HoneyCounter", topBar);
            pill.anchorMin = new Vector2(0f, 1f);
            pill.anchorMax = new Vector2(1f, 1f);
            pill.pivot = new Vector2(0.5f, 1f);
            pill.offsetMin = new Vector2(CounterRowInset, -24f - 112f);
            pill.offsetMax = new Vector2(-(CounterRowInset + DewPillWidth + CounterGap), -24f);
            AddShadow(pill);
            AddImage(pill, pillSprite, UiTheme.Cream, false);

            TMP_Text icon = CreateIcon("Icon", pill, UiTheme.IconHoney, 72f, UiTheme.Honey);
            PlaceLeft((RectTransform)icon.transform, 22f, 88f);

            TMP_Text honey = CreateText("HoneyText", pill, Vector2.zero, 112f, 78f, FontStyles.Normal, UiTheme.TextDark);
            var honeyRect = (RectTransform)honey.transform;
            Stretch(honeyRect);
            honeyRect.offsetMin = new Vector2(104f, 0f);
            honeyRect.offsetMax = new Vector2(-34f, 0f);
            honey.verticalAlignment = VerticalAlignmentOptions.Middle;
            AutoSize(honey, 40f);

            // Under the honey pill, so it lines up with it rather than the screen.
            TMP_Text rate = CreateText("RateText", topBar, new Vector2(0f, -140f), 48f, 36f, FontStyles.Normal, UiTheme.TextLight);
            rate.fontSharedMaterial = outlinedText;
            var rateRect = (RectTransform)rate.transform;
            rateRect.offsetMin = new Vector2(CounterRowInset, rateRect.offsetMin.y);
            rateRect.offsetMax = new Vector2(-(CounterRowInset + DewPillWidth + CounterGap), rateRect.offsetMax.y);
            return (honey, rate, pill);
        }

        /// <summary>Space between the screen edge and the counter row: the corner button and a gap.</summary>
        const float CounterRowInset = 20f + CornerButtonSize + 12f;
        const float DewPillWidth = 214f;
        const float CounterGap = 12f;

        /// <summary>
        /// Dewdrops pill right of the honey pill: the crystal icon and the balance, smaller than
        /// honey. A tap opens the shop. Hidden until the first Dewdrop.
        /// </summary>
        static (GameObject, TMP_Text, Button) CreateDewCounter(Transform topBar)
        {
            RectTransform pill = CreateRect("DewCounter", topBar);
            pill.anchorMin = pill.anchorMax = new Vector2(1f, 1f);
            pill.pivot = new Vector2(1f, 1f);
            pill.sizeDelta = new Vector2(DewPillWidth, 96f);
            pill.anchoredPosition = new Vector2(-CounterRowInset, -32f);
            // The view hides the pill, so its shadow lives inside it.
            RectTransform face = CreateRect("Face", pill);
            Stretch(face);
            AddShadow(face);
            Image image = AddImage(face, pillSprite, UiTheme.DewPale, true);
            var button = pill.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            RectTransform icon = CreateRect("Icon", pill);
            PlaceLeft(icon, 10f, 78f);
            AddImage(icon, UiAssets.DewIcon(), Color.white, false).preserveAspect = true;

            TMP_Text text = CreateText("DewText", pill, Vector2.zero, 96f, 52f, FontStyles.Normal, UiTheme.TextDark);
            var textRect = (RectTransform)text.transform;
            Stretch(textRect);
            textRect.offsetMin = new Vector2(88f, 0f);
            textRect.offsetMax = new Vector2(-18f, 0f);
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            AutoSize(text, 26f);
            pill.gameObject.SetActive(false);
            return (pill.gameObject, text, button);
        }

        /// <summary>Square button in a top corner: a big icon over a short label, with a dot badge that starts hidden.</summary>
        static (Button, GameObject) CreateCornerButton(string name, Transform topBar, bool right, Color color, string glyph, string label, string badgeName, Color badgeColor)
        {
            RectTransform rect = CreateRect(name, topBar);
            rect.anchorMin = rect.anchorMax = new Vector2(right ? 1f : 0f, 1f);
            rect.pivot = new Vector2(right ? 1f : 0f, 1f);
            rect.sizeDelta = new Vector2(CornerButtonSize, CornerButtonSize);
            rect.anchoredPosition = new Vector2(right ? -20f : 20f, -20f);
            AddShadow(rect);
            Image image = AddImage(rect, buttonSprite, color, true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text icon = CreateIcon("Icon", rect, glyph, 80f, UiTheme.TextLight);
            PlaceTop((RectTransform)icon.transform, 6f, 90f);
            TMP_Text text = CreateText("Label", rect, new Vector2(0f, -88f), 42f, 30f, FontStyles.Normal, UiTheme.TextLight);
            ((RectTransform)text.transform).sizeDelta = new Vector2(-12f, 42f);
            text.text = label;
            AutoSize(text, 18f);
            return (button, CreateBadge(badgeName, rect, badgeColor));
        }

        /// <summary>Dot with a cream ring in the top-right corner of a button; starts hidden.</summary>
        static GameObject CreateBadge(string name, RectTransform button, Color color)
        {
            RectTransform badge = CreateRect(name, button);
            badge.anchorMin = badge.anchorMax = new Vector2(1f, 1f);
            badge.sizeDelta = new Vector2(46f, 46f);
            badge.anchoredPosition = new Vector2(-8f, -8f);
            AddImage(badge, circleSprite, UiTheme.Cream, false);
            RectTransform dot = CreateRect("Dot", badge);
            Stretch(dot);
            dot.offsetMin = new Vector2(7f, 7f);
            dot.offsetMax = new Vector2(-7f, -7f);
            AddImage(dot, circleSprite, color, false);
            badge.gameObject.SetActive(false);
            return badge.gameObject;
        }

        /// <summary>Rounded bar along the bottom of the top bar with a flower badge; the fill's right anchor is the bloom fraction.</summary>
        static (TMP_Text, RectTransform) CreateBloomBar(Transform topBar)
        {
            RectTransform track = CreateRect("BloomBar", topBar);
            track.anchorMin = new Vector2(0f, 0f);
            track.anchorMax = new Vector2(1f, 0f);
            track.pivot = new Vector2(0.5f, 0f);
            track.sizeDelta = new Vector2(-150f, 50f);
            track.anchoredPosition = new Vector2(25f, 14f);
            AddImage(track, pillSprite, UiTheme.BarTrack, false);

            RectTransform fill = CreateRect("Fill", track);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            AddImage(fill, pillSprite, UiTheme.BarFill, false);

            RectTransform badge = CreateRect("FlowerBadge", track);
            badge.anchorMin = badge.anchorMax = new Vector2(0f, 0.5f);
            badge.sizeDelta = new Vector2(86f, 86f);
            badge.anchoredPosition = new Vector2(-10f, 0f);
            AddShadow(badge);
            AddImage(badge, circleSprite, UiTheme.Cream, false);
            TMP_Text flower = CreateIcon("Flower", badge, UiTheme.IconFlower, 58f, UiTheme.FlowerPink);
            Stretch((RectTransform)flower.transform);

            TMP_Text label = CreateText("BloomText", track, Vector2.zero, 50f, 30f, FontStyles.Normal, UiTheme.TextLight);
            Stretch((RectTransform)label.transform);
            label.fontSharedMaterial = outlinedText;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            label.text = string.Format(Strings.BloomFormat, 0);
            return (label, fill);
        }

        /// <summary>Swipe hint right of the rewarded boost button, just above the bottom bar; never blocks a swipe.</summary>
        static SwipeHintView CreateSwipeHint(Transform root)
        {
            RectTransform holder = CreateRect("SwipeHint", root);
            holder.anchorMin = Vector2.zero;
            holder.anchorMax = new Vector2(1f, 0f);
            holder.pivot = new Vector2(0.5f, 0f);
            holder.offsetMin = new Vector2(350f, 322f);
            holder.offsetMax = new Vector2(-24f, 440f);

            // The view hides the panel, so its shadow lives inside it.
            RectTransform panel = CreateRect("Panel", holder);
            Stretch(panel);
            RectTransform face = CreateRect("Face", panel);
            Stretch(face);
            AddShadow(face);
            AddImage(face, pillSprite, UiTheme.Cream, false);

            TMP_Text icon = CreateIcon("Icon", panel, UiTheme.IconTouch, 64f, UiTheme.Primary);
            PlaceLeft((RectTransform)icon.transform, 24f, 76f);
            TMP_Text label = CreateText("Label", panel, Vector2.zero, 118f, 34f, FontStyles.Normal, UiTheme.TextDark);
            var labelRect = (RectTransform)label.transform;
            Stretch(labelRect);
            labelRect.offsetMin = new Vector2(100f, 0f);
            labelRect.offsetMax = new Vector2(-28f, 0f);
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            AutoSize(label, 22f);
            label.text = Strings.SwipeHint;

            (RectTransform handLayer, RectTransform hand, CanvasGroup handGroup) = CreateSwipeHand(root);

            var view = holder.gameObject.AddComponent<SwipeHintView>();
            EditorAssets.Set(view, ("panel", panel.gameObject), ("handLayer", handLayer), ("hand", hand), ("handGroup", handGroup));
            return view;
        }

        /// <summary>Rewarded honey boost button in the bottom-left corner, beside the swipe hint.</summary>
        static RewardedBoostView CreateRewardedBoost(Transform root)
        {
            (Button button, Image image, TMP_Text label) = CreateButton("RewardedBoost", root, Vector2.zero, new Vector2(306f, 130f), UiTheme.Ad, UiTheme.TextLight);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(24f, 318f);
            AddShadow(rect);
            ((RectTransform)label.transform).offsetMin = new Vector2(88f, ButtonLip);
            label.fontSizeMax = 34f;
            label.text = string.Format(Strings.RewardedBoostOfferFormat, "2", "5m");
            TMP_Text icon = CreateIcon("Icon", rect, UiTheme.IconAd, 62f, UiTheme.TextLight);
            PlaceLeft((RectTransform)icon.transform, 18f, 72f);
            ((RectTransform)icon.transform).anchoredPosition += new Vector2(0f, ButtonLip * 0.5f);

            var view = rect.gameObject.AddComponent<RewardedBoostView>();
            EditorAssets.Set(view, ("button", button), ("background", image), ("label", label),
                ("offerColor", UiTheme.Ad), ("activeColor", UiTheme.Good));
            return view;
        }

        static WelcomeBackView CreateWelcomeBack(Transform root)
        {
            RectTransform panel = CreateModal("WelcomeBack", root, new Vector2(880f, 940f), UiTheme.Primary, out RectTransform window);
            CreateTitle(window, Strings.WelcomeBackTitle);
            TMP_Text away = CreateText("AwayText", window, new Vector2(0f, -164f), 70f, 44f, FontStyles.Normal, UiTheme.TextSoft);
            TMP_Text honey = CreateText("HoneyText", window, new Vector2(0f, -236f), 130f, 100f, FontStyles.Normal, UiTheme.TextDark);

            // The one-time gift: a gift icon and its line, shown only on the first return.
            RectTransform giftRow = CreateRect("GiftRow", window);
            giftRow.anchorMin = new Vector2(0f, 1f);
            giftRow.anchorMax = new Vector2(1f, 1f);
            giftRow.pivot = new Vector2(0.5f, 1f);
            giftRow.sizeDelta = new Vector2(-80f, 76f);
            giftRow.anchoredPosition = new Vector2(0f, -372f);
            AddImage(giftRow, pillSprite, UiTheme.CreamShade, false);
            TMP_Text giftIcon = CreateIcon("Icon", giftRow, UiTheme.IconGift, 52f, UiTheme.FlowerPink);
            PlaceLeft((RectTransform)giftIcon.transform, 18f, 60f);
            TMP_Text gift = CreateText("GiftText", giftRow, Vector2.zero, 76f, 34f, FontStyles.Normal, UiTheme.TextDark);
            var giftRect = (RectTransform)gift.transform;
            Stretch(giftRect);
            giftRect.offsetMin = new Vector2(88f, 0f);
            giftRect.offsetMax = new Vector2(-20f, 0f);
            gift.verticalAlignment = VerticalAlignmentOptions.Middle;
            AutoSize(gift, 22f);
            giftRow.gameObject.SetActive(false);

            TMP_Text cap = CreateText("CapText", window, new Vector2(0f, -462f), 60f, 36f, FontStyles.Italic, UiTheme.TextSoft);
            (Button ad, _, TMP_Text adLabel) = CreateButton("AdButton", window, new Vector2(0f, -560f), new Vector2(560f, 150f), UiTheme.Ad, UiTheme.TextLight);
            adLabel.text = string.Format(Strings.OfflineAdOfferFormat, "3");
            (Button collect, _, TMP_Text collectLabel) = CreateButton("CollectButton", window, new Vector2(0f, -752f), new Vector2(440f, 136f), UiTheme.Primary);
            collectLabel.text = Strings.Collect;

            // On the always-active root, like the Queen panel view, so it can hide and show the panel.
            var view = root.gameObject.AddComponent<WelcomeBackView>();
            EditorAssets.Set(view, ("panel", panel.gameObject), ("awayText", away), ("honeyText", honey),
                ("capText", cap), ("collectButton", collect), ("adButton", ad), ("adLabel", adLabel),
                ("giftRow", giftRow.gameObject), ("giftText", gift));
            panel.gameObject.SetActive(false);
            return view;
        }

        static BackButtonHandler CreateQuitDialog(Transform root, QueenPanelView queenPanel, WelcomeBackView welcomeBack, ShopPanelView shopPanel,
            SettingsPanelView settingsPanel, DailyPanelView dailyPanel, HivePanelView hivePanel)
        {
            RectTransform dialog = CreateModal("QuitDialog", root, new Vector2(820f, 500f), UiTheme.Primary, out RectTransform window);
            CreateTitle(window, Strings.QuitTitle);
            TMP_Text body = CreateText("Body", window, new Vector2(0f, -176f), 80f, 42f, FontStyles.Normal, UiTheme.TextDark);
            body.text = Strings.QuitBody;
            (Button quit, _, TMP_Text quitLabel) = CreateButton("QuitButton", window, new Vector2(190f, -300f), new Vector2(340f, 140f), UiTheme.Primary);
            quitLabel.text = Strings.Quit;
            (Button cancel, _, TMP_Text cancelLabel) = CreateButton("CancelButton", window, new Vector2(-190f, -300f), new Vector2(340f, 140f), UiTheme.Neutral);
            cancelLabel.text = Strings.Cancel;

            var handler = root.gameObject.AddComponent<BackButtonHandler>();
            EditorAssets.Set(handler, ("queenPanel", queenPanel), ("welcomeBack", welcomeBack), ("shopPanel", shopPanel), ("settingsPanel", settingsPanel),
                ("dailyPanel", dailyPanel), ("hivePanel", hivePanel),
                ("quitDialog", dialog.gameObject), ("quitButton", quit), ("cancelButton", cancel));
            dialog.gameObject.SetActive(false);
            return handler;
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
            RectTransform panel = CreateModal("QueenPanel", root, new Vector2(960f, 1560f), UiTheme.Royal, out RectTransform window);

            CreateTitle(window, Strings.QueenTitle);
            var royalText = new Color(0.42f, 0.25f, 0.66f);
            TMP_Text level = CreateText("LevelText", window, new Vector2(0f, -150f), 60f, 46f, FontStyles.Normal, royalText);
            TMP_Text nextLevel = CreateText("NextLevelText", window, new Vector2(0f, -208f), 40f, 30f, FontStyles.Italic, UiTheme.TextSoft);
            TMP_Text jelly = CreateText("JellyText", window, new Vector2(0f, -250f), 54f, 40f, FontStyles.Normal, UiTheme.TextDark);

            TMP_Text abilitiesTitle = CreateText("AbilitiesTitle", window, new Vector2(0f, -310f), 48f, 36f, FontStyles.Normal, UiTheme.TextDark);
            abilitiesTitle.text = Strings.AbilitiesTitle;
            QueenAbilityRowView[] rows = CreateAbilityList(window, new Vector2(0f, -362f), abilityCount);

            TMP_Text moveTitle = CreateText("MoveTitle", window, new Vector2(0f, -975f), 64f, 48f, FontStyles.Normal, royalText);
            moveTitle.text = Strings.MoveTheQueen;
            TMP_Text garden = CreateText("GardenText", window, new Vector2(0f, -1040f), 44f, 32f, FontStyles.Normal, UiTheme.TextDark);
            TMP_Text requirement = CreateText("RequirementText", window, new Vector2(0f, -1085f), 44f, 32f, FontStyles.Normal, UiTheme.TextDark);
            TMP_Text cost = CreateText("CostText", window, new Vector2(0f, -1130f), 44f, 32f, FontStyles.Normal, UiTheme.TextDark);
            TMP_Text preview = CreateText("PreviewText", window, new Vector2(0f, -1180f), 74f, 56f, FontStyles.Normal, new Color(0.3f, 0.58f, 0.2f));
            TMP_Text bonus = CreateText("BonusText", window, new Vector2(0f, -1256f), 44f, 30f, FontStyles.Normal, UiTheme.TextSoft);

            (Button move, Image moveImage, TMP_Text moveLabel) = CreateButton("MoveButton", window, new Vector2(0f, -1305f), new Vector2(600f, 130f), UiTheme.Primary);
            moveLabel.text = Strings.MoveTheQueen;
            (Button close, _, TMP_Text closeLabel) = CreateButton("CloseButton", window, new Vector2(0f, -1448f), new Vector2(320f, 96f), UiTheme.Neutral);
            closeLabel.text = Strings.Close;

            RectTransform confirm = CreateModal("Confirm", panel, new Vector2(820f, 620f), UiTheme.Royal, out RectTransform confirmWindow);
            CreateTitle(confirmWindow, Strings.MoveConfirmTitle);
            TMP_Text confirmBody = CreateText("Body", confirmWindow, new Vector2(0f, -170f), 200f, 42f, FontStyles.Normal, UiTheme.TextDark);
            (Button confirmButton, _, TMP_Text confirmLabel) = CreateButton("ConfirmButton", confirmWindow, new Vector2(190f, -420f), new Vector2(340f, 140f), UiTheme.Primary);
            confirmLabel.text = Strings.MoveConfirm;
            (Button cancelButton, _, TMP_Text cancelLabel) = CreateButton("CancelButton", confirmWindow, new Vector2(-190f, -420f), new Vector2(340f, 140f), UiTheme.Neutral);
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
                ("moveEnabledColor", UiTheme.Primary), ("moveDisabledColor", UiTheme.Disabled),
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
            AddImage(row, roundSprite, UiTheme.CreamShade, false);

            const float buttonWidth = 220f;
            TMP_Text name = CreateText("Name", row, new Vector2(0f, -14f), 54f, 38f, FontStyles.Normal, UiTheme.TextDark);
            TMP_Text level = CreateText("Level", row, new Vector2(0f, -14f), 54f, 32f, FontStyles.Normal, new Color(0.42f, 0.25f, 0.66f));
            TMP_Text body = CreateText("Body", row, new Vector2(0f, -72f), 56f, 26f, FontStyles.Normal, UiTheme.TextSoft);
            foreach (TMP_Text text in new[] { name, level, body })
            {
                var rect = (RectTransform)text.transform;
                // Left column, clear of the buy button on the right.
                rect.offsetMin = new Vector2(26f, rect.offsetMin.y);
                rect.offsetMax = new Vector2(-(buttonWidth + 48f), rect.offsetMax.y);
                text.alignment = TextAlignmentOptions.TopLeft;
                AutoSize(text, 20f);
            }
            level.alignment = TextAlignmentOptions.TopRight;

            (Button buy, Image buyImage, TMP_Text cost) = CreateButton("BuyButton", row, Vector2.zero, new Vector2(buttonWidth, 110f), UiTheme.Primary);
            var buyRect = (RectTransform)buy.transform;
            buyRect.anchorMin = buyRect.anchorMax = new Vector2(1f, 0.5f);
            buyRect.pivot = new Vector2(1f, 0.5f);
            buyRect.anchoredPosition = new Vector2(-22f, 0f);
            cost.fontSizeMax = 38f;

            var view = row.gameObject.AddComponent<QueenAbilityRowView>();
            EditorAssets.Set(view, ("abilityIndex", index), ("nameText", name), ("levelText", level), ("bodyText", body),
                ("buyButton", buy), ("feedback", AddButtonFeedback(buy)), ("buyImage", buyImage), ("costText", cost),
                ("buyColor", UiTheme.Primary), ("disabledColor", UiTheme.Disabled), ("maxedColor", UiTheme.Good));
            return view;
        }

        /// <summary>
        /// Full-screen dimmed backdrop that swallows taps, with a centred cream window: a soft
        /// shadow behind it and a header band in the <paramref name="accent"/> colour for the title.
        /// </summary>
        static RectTransform CreateModal(string name, Transform parent, Vector2 windowSize, Color accent, out RectTransform window)
        {
            RectTransform overlay = CreateRect(name, parent);
            Stretch(overlay);
            var backdrop = overlay.gameObject.AddComponent<Image>();
            backdrop.color = UiTheme.Scrim;

            window = CreateRect("Window", overlay);
            window.anchorMin = window.anchorMax = new Vector2(0.5f, 0.5f);
            window.sizeDelta = windowSize;
            window.anchoredPosition = Vector2.zero;
            AddShadow(window);
            AddImage(window, roundSprite, UiTheme.Cream, true);

            RectTransform header = CreateRect("Header", window);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(-24f, 124f);
            header.anchoredPosition = new Vector2(0f, -12f);
            AddImage(header, buttonSprite, accent, false);
            return overlay;
        }

        /// <summary>Panel title on the header band of a <see cref="CreateModal"/> window.</summary>
        static TMP_Text CreateTitle(RectTransform window, string text)
        {
            TMP_Text title = CreateText("Title", window, new Vector2(0f, -18f), 104f, 66f, FontStyles.Normal, UiTheme.TextLight);
            title.fontSharedMaterial = outlinedText;
            title.verticalAlignment = VerticalAlignmentOptions.Middle;
            AutoSize(title, 36f);
            title.text = text;
            return title;
        }

        /// <summary>
        /// Pressable button anchored to the top centre of <paramref name="parent"/>, offset by
        /// <paramref name="position"/>: a face over a darker lip, the label on the face.
        /// </summary>
        static (Button, Image, TMP_Text) CreateButton(string name, Transform parent, Vector2 position, Vector2 size, Color color, Color? labelColor = null)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            Image image = AddImage(rect, buttonSprite, color, true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text label = CreateText("Label", rect, Vector2.zero, size.y, 48f, FontStyles.Normal, labelColor ?? UiTheme.TextDark);
            var labelRect = (RectTransform)label.transform;
            Stretch(labelRect);
            labelRect.offsetMin = new Vector2(12f, ButtonLip);
            labelRect.offsetMax = new Vector2(-12f, 0f);
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            AutoSize(label, 22f);
            return (button, image, label);
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
            banner.sizeDelta = new Vector2(-120f, 200f);
            banner.anchoredPosition = new Vector2(0f, 260f);
            RectTransform shadow = CreateRect("Shadow", banner);
            Stretch(shadow);
            shadow.offsetMin = new Vector2(-22f, -30f);
            shadow.offsetMax = new Vector2(22f, 14f);
            AddImage(shadow, shadowSprite, UiTheme.ShadowTint, false);
            RectTransform face = CreateRect("Face", banner);
            Stretch(face);
            AddImage(face, buttonSprite, UiTheme.Good, false);

            foreach (float side in new[] { -1f, 1f })
            {
                TMP_Text trophy = CreateIcon(side < 0f ? "TrophyLeft" : "TrophyRight", banner, UiTheme.IconTrophy, 84f, UiTheme.Honey);
                var trophyRect = (RectTransform)trophy.transform;
                trophyRect.anchorMin = trophyRect.anchorMax = new Vector2(side < 0f ? 0f : 1f, 0.5f);
                trophyRect.sizeDelta = new Vector2(100f, 100f);
                trophyRect.anchoredPosition = new Vector2(side * -66f, ButtonLip * 0.5f);
            }

            TMP_Text title = CreateText("Title", banner, Vector2.zero, 200f, 90f, FontStyles.Normal, UiTheme.TextLight);
            var titleRect = (RectTransform)title.transform;
            Stretch(titleRect);
            titleRect.offsetMin = new Vector2(120f, ButtonLip);
            titleRect.offsetMax = new Vector2(-120f, 0f);
            title.fontSharedMaterial = outlinedText;
            title.alignment = TextAlignmentOptions.Center;
            title.text = Strings.GardenComplete;
            AutoSize(title, 40f);

            var view = overlay.gameObject.AddComponent<GardenCompleteView>();
            EditorAssets.Set(view, ("group", group), ("banner", banner), ("titleText", title));
            return view;
        }

        /// <summary>Wooden bar with four cards in thumb reach; its rounded bottom corners sit off screen.</summary>
        static BottomBarView CreateBottomBar(Transform root)
        {
            RectTransform bar = CreateRect("BottomBar", root);
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = new Vector2(1f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.sizeDelta = new Vector2(0f, 340f);
            bar.anchoredPosition = new Vector2(0f, -40f);
            AddShadow(bar);
            AddImage(bar, roundSprite, UiTheme.Wood, true);

            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 22, 62);
            layout.spacing = 14f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var view = bar.gameObject.AddComponent<BottomBarView>();
            EditorAssets.Set(view,
                ("addBeeButton", CreateUpgradeButton("AddBeeButton", bar, UiAssets.BeeIcon(), null, Color.white)),
                ("speedButton", CreateUpgradeButton("SpeedButton", bar, null, UiTheme.IconSpeed, UiTheme.Primary)),
                ("evolveButton", CreateUpgradeButton("EvolveButton", bar, UiAssets.GoldenBeeIcon(), null, Color.white)),
                ("honeyValueButton", CreateUpgradeButton("HoneyValueButton", bar, null, UiTheme.IconHoney, UiTheme.Honey)));
            return view;
        }

        /// <summary>
        /// Upgrade card: an icon (a rendered model or a glyph), the name and its detail, and the
        /// price on a honey pill along the bottom. Cream when it can be bought, dimmed when not.
        /// </summary>
        static UpgradeButtonView CreateUpgradeButton(string name, Transform parent, Sprite iconSprite, string glyph, Color glyphColor)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = AddImage(rect, buttonSprite, UiTheme.Cream, true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Graphic icon;
            if (iconSprite != null)
            {
                RectTransform iconRect = CreateRect("Icon", rect);
                PlaceTop(iconRect, 0f, 112f);
                Image iconImage = AddImage(iconRect, iconSprite, Color.white, false);
                iconImage.preserveAspect = true;
                icon = iconImage;
            }
            else
            {
                TMP_Text iconText = CreateIcon("Icon", rect, glyph, 82f, glyphColor);
                PlaceTop((RectTransform)iconText.transform, 8f, 96f);
                icon = iconText;
            }

            TMP_Text title = CreateText("Title", rect, new Vector2(0f, -104f), 38f, 31f, FontStyles.Normal, UiTheme.TextDark);
            TMP_Text detail = CreateText("Detail", rect, new Vector2(0f, -140f), 32f, 27f, FontStyles.Normal, UiTheme.TextSoft);
            foreach (TMP_Text text in new[] { title, detail })
            {
                // Narrow cards: keep the side margin small and let long words shrink.
                ((RectTransform)text.transform).sizeDelta = new Vector2(-14f, ((RectTransform)text.transform).sizeDelta.y);
                AutoSize(text, 18f);
            }

            RectTransform pill = CreateRect("CostPill", rect);
            pill.anchorMin = new Vector2(0f, 0f);
            pill.anchorMax = new Vector2(1f, 0f);
            pill.pivot = new Vector2(0.5f, 0f);
            pill.sizeDelta = new Vector2(-22f, 60f);
            pill.anchoredPosition = new Vector2(0f, 24f);
            Image pillImage = AddImage(pill, pillSprite, UiTheme.Primary, false);
            TMP_Text drop = CreateIcon("HoneyIcon", pill, UiTheme.IconHoney, 34f, UiTheme.TextDark);
            PlaceLeft((RectTransform)drop.transform, 10f, 40f);
            TMP_Text cost = CreateText("Cost", pill, Vector2.zero, 60f, 34f, FontStyles.Normal, UiTheme.TextDark);
            var costRect = (RectTransform)cost.transform;
            Stretch(costRect);
            costRect.offsetMin = new Vector2(44f, 0f);
            costRect.offsetMax = new Vector2(-10f, 0f);
            cost.verticalAlignment = VerticalAlignmentOptions.Middle;
            AutoSize(cost, 18f);

            var view = rect.gameObject.AddComponent<UpgradeButtonView>();
            EditorAssets.Set(view, ("button", button), ("feedback", AddButtonFeedback(button)), ("background", image),
                ("titleText", title), ("detailText", detail), ("costText", cost),
                ("enabledColor", UiTheme.Cream), ("disabledColor", UiTheme.Disabled),
                ("costBackground", pillImage), ("costEnabledColor", UiTheme.Primary), ("costDisabledColor", UiTheme.Neutral),
                ("icon", icon));
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
            text.font = textFont;
            text.text = "0";
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>One icon glyph from the icon font, centred in its rect.</summary>
        static TMP_Text CreateIcon(string name, Transform parent, string glyph, float size, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.sizeDelta = new Vector2(size * 1.2f, size * 1.2f);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = iconFont;
            text.text = glyph;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        static Image AddImage(RectTransform rect, Sprite sprite, Color color, bool raycast)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>Soft shadow just below and behind <paramref name="rect"/>: a sibling drawn first, with the same anchors.</summary>
        static void AddShadow(RectTransform rect)
        {
            RectTransform shadow = CreateRect($"{rect.name}Shadow", rect.parent);
            shadow.anchorMin = rect.anchorMin;
            shadow.anchorMax = rect.anchorMax;
            shadow.pivot = rect.pivot;
            shadow.sizeDelta = rect.sizeDelta + new Vector2(36f, 36f);
            Vector2 pivotShift = new Vector2((rect.pivot.x - 0.5f) * 36f, (rect.pivot.y - 0.5f) * 36f);
            shadow.anchoredPosition = rect.anchoredPosition + pivotShift + new Vector2(0f, -10f);
            shadow.SetSiblingIndex(rect.GetSiblingIndex());
            if (rect.GetComponent<LayoutElement>() != null || rect.parent.GetComponent<LayoutGroup>() != null)
                shadow.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            AddImage(shadow, shadowSprite, UiTheme.ShadowTint, false);
        }

        /// <summary>Anchors <paramref name="rect"/> to the top centre, <paramref name="top"/> below the edge, <paramref name="size"/> square.</summary>
        static void PlaceTop(RectTransform rect, float top, float size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(0f, -top);
        }

        /// <summary>Anchors <paramref name="rect"/> to the middle of the left edge, <paramref name="left"/> in, <paramref name="size"/> square.</summary>
        static void PlaceLeft(RectTransform rect, float left, float size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(left, 0f);
        }

        static void AutoSize(TMP_Text text, float minimum)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = minimum;
            text.fontSizeMax = text.fontSize;
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
