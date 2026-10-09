using Buzzfield.Core;
using Buzzfield.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.Editor
{
    /// <summary>
    /// The scene builder's part for the game's feel: the settings gear and panel, the goal pill,
    /// the butterfly's offer ("double it" or "open the pouch"), the swipe hint's hand and the
    /// effects layer (rising numbers, flying honey, sparkles).
    /// </summary>
    public static partial class GreyboxSceneBuilder
    {
        const float GoalDewBadgeWidth = 132f;

        /// <summary>Top of the row just under the top bar: the goal pill and the gear.</summary>
        const float UnderBarY = -280f;
        const float GearSize = 100f;

        /// <summary>Small, quiet gear under the Queen button: settings are rarely needed, so it stays out of the way.</summary>
        static Button CreateSettingsButton(Transform root)
        {
            RectTransform rect = CreateRect("SettingsButton", root);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.sizeDelta = new Vector2(GearSize, GearSize);
            rect.anchoredPosition = new Vector2(-24f, UnderBarY);
            Image image = AddImage(rect, circleSprite, new Color(UiTheme.Cream.r, UiTheme.Cream.g, UiTheme.Cream.b, 0.8f), true);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TMP_Text icon = CreateIcon("Icon", rect, UiTheme.IconSettings, 62f, UiTheme.TextSoft);
            Stretch((RectTransform)icon.transform);
            return button;
        }

        /// <summary>Goal pill under the top bar, left of the gear: an icon and one line of text; hidden when the chain is done.</summary>
        static GoalView CreateGoal(Transform root)
        {
            RectTransform holder = CreateRect("Goal", root);
            holder.anchorMin = new Vector2(0f, 1f);
            holder.anchorMax = new Vector2(1f, 1f);
            holder.pivot = new Vector2(0.5f, 1f);
            holder.offsetMin = new Vector2(24f, UnderBarY - 100f);
            holder.offsetMax = new Vector2(-(GearSize + 48f), UnderBarY);

            RectTransform pill = CreateRect("Pill", holder);
            Stretch(pill);
            RectTransform shadow = CreateRect("PillShadow", holder);
            Stretch(shadow);
            shadow.offsetMin = new Vector2(-14f, -22f);
            shadow.offsetMax = new Vector2(14f, 8f);
            shadow.SetAsFirstSibling();
            AddImage(shadow, shadowSprite, UiTheme.ShadowTint, false);
            Image background = AddImage(pill, pillSprite, UiTheme.Cream, true);
            var button = pill.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            TMP_Text icon = CreateIcon("Icon", pill, UiTheme.IconStar, 56f, UiTheme.Primary);
            PlaceLeft((RectTransform)icon.transform, 18f, 66f);
            TMP_Text label = CreateText("Label", pill, Vector2.zero, 100f, 38f, FontStyles.Normal, UiTheme.TextDark);
            var labelRect = (RectTransform)label.transform;
            Stretch(labelRect);
            labelRect.offsetMin = new Vector2(92f, 0f);
            labelRect.offsetMax = new Vector2(-(GoalDewBadgeWidth + 22f), 0f);

            // The goal's Dewdrops on the right end: crystal and "+5" on a pale pill.
            RectTransform dewBadge = CreateRect("DewBadge", pill);
            dewBadge.anchorMin = dewBadge.anchorMax = new Vector2(1f, 0.5f);
            dewBadge.pivot = new Vector2(1f, 0.5f);
            dewBadge.sizeDelta = new Vector2(GoalDewBadgeWidth, 70f);
            dewBadge.anchoredPosition = new Vector2(-14f, 0f);
            AddImage(dewBadge, pillSprite, UiTheme.DewPale, false);
            RectTransform dewIcon = CreateRect("Icon", dewBadge);
            PlaceLeft(dewIcon, 6f, 58f);
            AddImage(dewIcon, UiAssets.DewIcon(), Color.white, false).preserveAspect = true;
            TMP_Text dewText = CreateText("DewText", dewBadge, Vector2.zero, 70f, 36f, FontStyles.Normal, UiTheme.TextDark);
            var dewTextRect = (RectTransform)dewText.transform;
            Stretch(dewTextRect);
            dewTextRect.offsetMin = new Vector2(62f, 0f);
            dewTextRect.offsetMax = new Vector2(-12f, 0f);
            dewText.verticalAlignment = VerticalAlignmentOptions.Middle;
            AutoSize(dewText, 20f);
            dewBadge.gameObject.SetActive(false);
            label.alignment = TextAlignmentOptions.Left;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            AutoSize(label, 22f);

            // On the always-active root: the view hides and shows the holder.
            var view = root.gameObject.AddComponent<GoalView>();
            EditorAssets.Set(view, ("root", holder.gameObject), ("button", button), ("background", background),
                ("label", label), ("icon", icon), ("workingColor", UiTheme.Cream), ("doneColor", UiTheme.Good),
                ("workingGlyph", UiTheme.IconStar), ("doneGlyph", UiTheme.IconGift),
                ("dewBadge", dewBadge.gameObject), ("dewText", dewText));
            holder.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// "Watch ad, +X more" (or "open the pouch") above the rewarded boost button, with a shrinking
        /// timer bar along its bottom and a round X on its corner for leaving the pouch.
        /// </summary>
        static GiftOfferView CreateGiftOffer(Transform root)
        {
            RectTransform holder = CreateRect("GiftOffer", root);
            holder.anchorMin = holder.anchorMax = Vector2.zero;
            holder.pivot = Vector2.zero;
            holder.sizeDelta = new Vector2(306f, 128f);
            holder.anchoredPosition = new Vector2(24f, 466f);

            (Button button, _, TMP_Text label) = CreateButton("Button", holder, Vector2.zero, new Vector2(306f, 128f), UiTheme.Ad, UiTheme.TextLight);
            var rect = (RectTransform)button.transform;
            Stretch(rect);
            AddShadow(rect);
            ((RectTransform)label.transform).offsetMin = new Vector2(88f, ButtonLip + 10f);
            label.fontSizeMax = 32f;
            label.text = string.Format(Strings.GiftDoubleFormat, "0");
            TMP_Text icon = CreateIcon("Icon", rect, UiTheme.IconAd, 58f, UiTheme.TextLight);
            PlaceLeft((RectTransform)icon.transform, 16f, 68f);
            ((RectTransform)icon.transform).anchoredPosition += new Vector2(0f, ButtonLip * 0.5f);

            RectTransform track = CreateRect("Timer", rect);
            track.anchorMin = new Vector2(0f, 0f);
            track.anchorMax = new Vector2(1f, 0f);
            track.pivot = new Vector2(0.5f, 0f);
            track.sizeDelta = new Vector2(-40f, 12f);
            track.anchoredPosition = new Vector2(0f, ButtonLip + 6f);
            AddImage(track, pillSprite, new Color(0f, 0f, 0f, 0.25f), false);
            RectTransform fill = CreateRect("Fill", track);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
            AddImage(fill, pillSprite, UiTheme.TextLight, false);

            RectTransform close = CreateRect("CloseButton", holder);
            close.anchorMin = close.anchorMax = new Vector2(1f, 1f);
            close.sizeDelta = new Vector2(70f, 70f);
            close.anchoredPosition = new Vector2(-6f, -2f);
            Image closeImage = AddImage(close, circleSprite, UiTheme.Cream, true);
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            TMP_Text closeIcon = CreateIcon("Icon", close, UiTheme.IconClose, 46f, UiTheme.TextDark);
            Stretch((RectTransform)closeIcon.transform);
            close.gameObject.SetActive(false);

            var view = root.gameObject.AddComponent<GiftOfferView>();
            EditorAssets.Set(view, ("root", holder.gameObject), ("button", button), ("label", label), ("timerFill", fill),
                ("closeButton", closeButton));
            holder.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Full-screen layer with a big hand that sweeps over the flowers; never takes a touch.</summary>
        static (RectTransform layer, RectTransform hand, CanvasGroup group) CreateSwipeHand(Transform root)
        {
            RectTransform layer = CreateRect("SwipeHand", root);
            Stretch(layer);
            var group = layer.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            RectTransform hand = CreateRect("Hand", layer);
            hand.anchorMin = hand.anchorMax = new Vector2(0.5f, 0.5f);
            hand.sizeDelta = new Vector2(170f, 170f);
            // The fingertip, not the middle of the glyph, sits on the path.
            hand.pivot = new Vector2(0.42f, 0.86f);
            TMP_Text shade = CreateIcon("Shadow", hand, UiTheme.IconTouch, 150f, new Color(0.1f, 0.06f, 0.02f, 0.35f));
            Stretch((RectTransform)shade.transform);
            ((RectTransform)shade.transform).anchoredPosition = new Vector2(6f, -8f);
            TMP_Text glyph = CreateIcon("Glyph", hand, UiTheme.IconTouch, 150f, UiTheme.Cream);
            Stretch((RectTransform)glyph.transform);
            layer.gameObject.SetActive(false);
            return (layer, hand, group);
        }

        const int FloatTexts = 14;
        const int Drops = 12;
        const int DewDrops = 10;
        const int Sparks = 16;

        /// <summary>
        /// Pools for the effects: rising "+honey" numbers in the outlined font, honey drops that fly
        /// to <paramref name="honeyTarget"/>, Dewdrop crystals that fly to <paramref name="dewTarget"/>,
        /// and star sparkles. All hidden until used; none take touches.
        /// </summary>
        static UiEffectsView CreateEffects(Transform root, RectTransform honeyTarget, RectTransform dewTarget)
        {
            RectTransform layer = CreateRect("Effects", root);
            Stretch(layer);

            var texts = new TMP_Text[FloatTexts];
            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = CreateText($"Float{i}", layer, Vector2.zero, 80f, 54f, FontStyles.Normal, UiTheme.Honey);
                var rect = (RectTransform)text.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(320f, 80f);
                text.fontSharedMaterial = outlinedText;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.text = "+0";
                text.gameObject.SetActive(false);
                texts[i] = text;
            }

            var drops = new RectTransform[Drops];
            for (int i = 0; i < drops.Length; i++)
            {
                TMP_Text drop = CreateIcon($"Drop{i}", layer, UiTheme.IconHoney, 62f, UiTheme.Honey);
                var rect = (RectTransform)drop.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                drop.gameObject.SetActive(false);
                drops[i] = rect;
            }

            Sprite dewIcon = UiAssets.DewIcon();
            var dewDrops = new RectTransform[DewDrops];
            for (int i = 0; i < dewDrops.Length; i++)
            {
                RectTransform rect = CreateRect($"Dew{i}", layer);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(64f, 64f);
                AddImage(rect, dewIcon, Color.white, false).preserveAspect = true;
                rect.gameObject.SetActive(false);
                dewDrops[i] = rect;
            }

            var sparks = new RectTransform[Sparks];
            for (int i = 0; i < sparks.Length; i++)
            {
                TMP_Text spark = CreateIcon($"Spark{i}", layer, UiTheme.IconSparkle, 46f, i % 2 == 0 ? UiTheme.Honey : UiTheme.Cream);
                var rect = (RectTransform)spark.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                spark.gameObject.SetActive(false);
                sparks[i] = rect;
            }

            var view = layer.gameObject.AddComponent<UiEffectsView>();
            EditorAssets.Set(view, ("layer", layer), ("floatTexts", texts), ("drops", drops), ("sparks", sparks), ("honeyTarget", honeyTarget),
                ("dewDrops", dewDrops), ("dewTarget", dewTarget));
            return view;
        }

        const float SettingsRowHeight = 112f;
        const float SettingsRowSpacing = 14f;

        /// <summary>
        /// Modal settings panel: a list of rows (a hidden row leaves no gap), the status line,
        /// "Delete my data" with its second-tap confirmation and "Keep my progress", the version
        /// and Close. 1420 px tall so it fits a 4:3 tablet at the reference scaling.
        /// </summary>
        static SettingsPanelView CreateSettingsPanel(Transform root, Button openButton)
        {
            RectTransform panel = CreateModal("SettingsPanel", root, new Vector2(900f, 1420f), UiTheme.Wood, out RectTransform window);
            CreateTitle(window, Strings.SettingsTitle);

            RectTransform list = CreateRect("Rows", window);
            list.anchorMin = new Vector2(0f, 1f);
            list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            list.sizeDelta = new Vector2(-100f, 6 * SettingsRowHeight + 5 * SettingsRowSpacing);
            list.anchoredPosition = new Vector2(0f, -166f);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = SettingsRowSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            (Button music, TMP_Text musicLabel, TMP_Text musicIcon) = CreateSettingsRow(list, "Music", UiTheme.IconMusic, Strings.MusicOn);
            (Button sound, TMP_Text soundLabel, TMP_Text soundIcon) = CreateSettingsRow(list, "Sound", UiTheme.IconSound, Strings.SoundOn);
            (Button vibration, TMP_Text vibrationLabel, _) = CreateSettingsRow(list, "Vibration", UiTheme.IconTouch, Strings.VibrationAll);
            (Button privacy, _, _) = CreateSettingsRow(list, "PrivacyChoices", UiTheme.IconPrivacy, Strings.PrivacyChoices);
            (Button restore, _, _) = CreateSettingsRow(list, "Restore", UiTheme.IconRestore, Strings.RestorePurchases);
            (Button policy, _, _) = CreateSettingsRow(list, "PrivacyPolicy", UiTheme.IconInfo, Strings.PrivacyPolicy);
            privacy.gameObject.SetActive(false);

            TMP_Text status = CreateText("StatusText", window, new Vector2(0f, -918f), 50f, 34f, FontStyles.Italic, UiTheme.TextSoft);
            status.text = string.Empty;

            (Button delete, _, TMP_Text deleteLabel) = CreateButton("DeleteButton", window, new Vector2(0f, -976f), new Vector2(700f, 124f), UiTheme.Alert, UiTheme.TextLight);
            deleteLabel.fontSizeMax = 36f;
            deleteLabel.text = Strings.DeleteData;
            (Button keep, _, TMP_Text keepLabel) = CreateButton("KeepButton", window, new Vector2(0f, -1112f), new Vector2(520f, 104f), UiTheme.Good);
            keepLabel.text = Strings.KeepProgress;
            keep.gameObject.SetActive(false);

            TMP_Text version = CreateText("VersionText", window, new Vector2(0f, -1228f), 40f, 28f, FontStyles.Normal, UiTheme.TextSoft);
            (Button close, _, TMP_Text closeLabel) = CreateButton("CloseButton", window, new Vector2(0f, -1284f), new Vector2(360f, 110f), UiTheme.Neutral);
            closeLabel.text = Strings.Close;

            // On the always-active root, like the other panel views, so it can open the hidden panel.
            var view = root.gameObject.AddComponent<SettingsPanelView>();
            EditorAssets.Set(view, ("openButton", openButton), ("panel", panel.gameObject), ("closeButton", close),
                ("musicButton", music), ("musicLabel", musicLabel), ("musicIcon", musicIcon),
                ("soundButton", sound), ("soundLabel", soundLabel), ("soundIcon", soundIcon),
                ("vibrationButton", vibration), ("vibrationLabel", vibrationLabel),
                ("musicOnGlyph", UiTheme.IconMusic), ("musicOffGlyph", UiTheme.IconMusicOff),
                ("soundOnGlyph", UiTheme.IconSound), ("soundOffGlyph", UiTheme.IconSoundOff),
                ("privacyChoicesButton", privacy), ("restoreButton", restore), ("policyButton", policy), ("statusText", status),
                ("deleteButton", delete), ("deleteLabel", deleteLabel), ("keepButton", keep), ("versionText", version));
            panel.gameObject.SetActive(false);
            return view;
        }

        /// <summary>One settings row: an icon on the left and the label, on a cream button the width of the list.</summary>
        static (Button, TMP_Text label, TMP_Text icon) CreateSettingsRow(Transform list, string name, string glyph, string text)
        {
            (Button button, _, TMP_Text label) = CreateButton(name, list, Vector2.zero, new Vector2(0f, SettingsRowHeight), UiTheme.CreamShade);
            var labelRect = (RectTransform)label.transform;
            labelRect.offsetMin = new Vector2(104f, ButtonLip);
            label.alignment = TextAlignmentOptions.Left;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            label.fontSizeMax = 40f;
            label.text = text;
            TMP_Text icon = CreateIcon("Icon", button.transform, glyph, 58f, UiTheme.TextSoft);
            PlaceLeft((RectTransform)icon.transform, 24f, 68f);
            ((RectTransform)icon.transform).anchoredPosition += new Vector2(0f, ButtonLip * 0.5f);
            return (button, label, icon);
        }
    }
}
