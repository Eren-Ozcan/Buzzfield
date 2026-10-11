using Buzzfield.Bees;
using Buzzfield.Core;
using Buzzfield.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.Editor
{
    /// <summary>
    /// The scene builder's part for the rare bees (docs/DEWDROPS.md 4.2): the row under the top bar
    /// that holds the round hive button and the goal pill, and the Hive panel with one card per species.
    /// </summary>
    public static partial class GreyboxSceneBuilder
    {
        const float RareCardHeight = 152f;
        const float RareCardSpacing = 12f;
        const float HiveListTop = -338f;

        /// <summary>
        /// Row under the top bar, left of the gear: <c>[hive] [goal pill]</c>. A layout group lines
        /// them up, so the goal pill takes the whole row while the hive button is hidden.
        /// </summary>
        static RectTransform CreateUnderBarRow(Transform root)
        {
            RectTransform row = CreateRect("UnderBar", root);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0.5f, 1f);
            row.offsetMin = new Vector2(24f, UnderBarY - GearSize);
            row.offsetMax = new Vector2(-(GearSize + 48f), UnderBarY);
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            return row;
        }

        /// <summary>Round cream button with the hive on it and an alert dot; hidden until the first Queen move.</summary>
        static (GameObject holder, Button button, GameObject badge) CreateHiveButton(RectTransform row)
        {
            RectTransform holder = CreateRect("HiveButton", row);
            var element = holder.gameObject.AddComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = GearSize;
            element.flexibleWidth = 0f;
            // The view hides the button, so its shadow lives inside it.
            RectTransform face = CreateRect("Face", holder);
            Stretch(face);
            AddShadow(face);
            Image image = AddImage(face, circleSprite, UiTheme.Cream, true);
            var button = holder.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            RectTransform icon = CreateRect("Icon", holder);
            Stretch(icon);
            icon.offsetMin = new Vector2(12f, 12f);
            icon.offsetMax = new Vector2(-12f, -10f);
            AddImage(icon, UiAssets.HiveIcon(), Color.white, false).preserveAspect = true;
            GameObject badge = CreateBadge("Dot", holder, UiTheme.Alert);
            ((RectTransform)badge.transform).sizeDelta = new Vector2(38f, 38f);
            ((RectTransform)badge.transform).anchoredPosition = new Vector2(-4f, -4f);
            holder.gameObject.SetActive(false);
            return (holder.gameObject, button, badge);
        }

        /// <summary>
        /// Modal Hive panel: the Dewdrops balance and a line about rare bees on top, one card per
        /// species in a list that scrolls once it outgrows the window, a status line and Close.
        /// 1520 px tall, so it fits a 4:3 tablet.
        /// </summary>
        static HivePanelView CreateHivePanel(Transform root, GameObject buttonRoot, Button openButton, GameObject badge, RareBeeSettings settings)
        {
            RectTransform panel = CreateModal("HivePanel", root, new Vector2(960f, 1520f), UiTheme.Primary, out RectTransform window);
            CreateTitle(window, Strings.HiveTitle);

            // The balance: crystal and number on a pale pill.
            RectTransform balance = CreateRect("DewBalance", window);
            balance.anchorMin = balance.anchorMax = new Vector2(0.5f, 1f);
            balance.pivot = new Vector2(0.5f, 1f);
            balance.sizeDelta = new Vector2(280f, 84f);
            balance.anchoredPosition = new Vector2(0f, -150f);
            AddImage(balance, pillSprite, UiTheme.DewPale, false);
            RectTransform crystal = CreateRect("Icon", balance);
            PlaceLeft(crystal, 10f, 70f);
            AddImage(crystal, UiAssets.DewIcon(), Color.white, false).preserveAspect = true;
            TMP_Text dewText = CreateText("DewText", balance, Vector2.zero, 84f, 46f, FontStyles.Normal, UiTheme.TextDark);
            var dewRect = (RectTransform)dewText.transform;
            Stretch(dewRect);
            dewRect.offsetMin = new Vector2(88f, 0f);
            dewRect.offsetMax = new Vector2(-22f, 0f);
            dewText.verticalAlignment = VerticalAlignmentOptions.Middle;
            AutoSize(dewText, 24f);

            TMP_Text hint = CreateText("Hint", window, new Vector2(0f, -244f), 84f, 32f, FontStyles.Normal, UiTheme.TextSoft);
            AutoSize(hint, 20f);
            hint.text = Strings.HiveHint;

            int count = settings.Bees.Count;
            float listHeight = count * RareCardHeight + Mathf.Max(0, count - 1) * RareCardSpacing;
            RectTransform content = CreateScrollList("Species", window, HiveListTop, Mathf.Min(listHeight, 6 * RareCardHeight + 5 * RareCardSpacing), RareCardSpacing);
            var cards = new RareBeeCardView[count];
            for (int i = 0; i < count; i++)
                cards[i] = CreateRareBeeCard(content, i, settings.Bees[i]);

            float bottom = HiveListTop - (6 * RareCardHeight + 5 * RareCardSpacing);
            TMP_Text status = CreateText("StatusText", window, new Vector2(0f, bottom - 10f), 50f, 32f, FontStyles.Normal, UiTheme.TextDark);
            AutoSize(status, 20f);
            (Button close, _, TMP_Text closeLabel) = CreateButton("CloseButton", window, new Vector2(0f, bottom - 74f), new Vector2(320f, 96f), UiTheme.Neutral);
            closeLabel.text = Strings.Close;

            // On the always-active root, like the other panel views, so it can open the hidden panel.
            var view = root.gameObject.AddComponent<HivePanelView>();
            EditorAssets.Set(view, ("buttonRoot", buttonRoot), ("openButton", openButton), ("badge", badge),
                ("panel", panel.gameObject), ("closeButton", close), ("dewText", dewText), ("statusText", status), ("cards", cards));
            panel.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Vertical list in a scroll view under <paramref name="window"/>'s top edge; it only scrolls when the rows outgrow it.</summary>
        static RectTransform CreateScrollList(string name, Transform window, float top, float height, float spacing)
        {
            RectTransform viewport = CreateRect(name, window);
            viewport.anchorMin = new Vector2(0f, 1f);
            viewport.anchorMax = new Vector2(1f, 1f);
            viewport.pivot = new Vector2(0.5f, 1f);
            viewport.sizeDelta = new Vector2(-60f, height);
            viewport.anchoredPosition = new Vector2(0f, top);
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
            layout.spacing = spacing;
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
            return content;
        }

        /// <summary>
        /// One species: its portrait on the left, name and level on top, what it gives below, and on
        /// the right the button with the Dewdrops cost (or Free, MAX, the garden it waits for).
        /// </summary>
        static RareBeeCardView CreateRareBeeCard(Transform list, int index, RareBee bee)
        {
            RectTransform card = CreateRect($"Species{index}", list);
            card.sizeDelta = new Vector2(0f, RareCardHeight);
            AddImage(card, roundSprite, UiTheme.CreamShade, false);

            RectTransform portrait = CreateRect("Icon", card);
            PlaceLeft(portrait, 10f, 132f);
            Image icon = AddImage(portrait, UiAssets.RareBeeIcon(bee.Id), Color.white, false);
            icon.preserveAspect = true;

            const float textLeft = 152f;
            const float buttonWidth = 220f;
            TMP_Text name = CreateText("Name", card, new Vector2(0f, -12f), 48f, 36f, FontStyles.Normal, UiTheme.TextDark);
            TMP_Text level = CreateText("Level", card, new Vector2(0f, -14f), 48f, 30f, FontStyles.Normal, RareLevelColor);
            TMP_Text body = CreateText("Body", card, new Vector2(0f, -60f), 82f, 26f, FontStyles.Normal, UiTheme.TextSoft);
            foreach (TMP_Text text in new[] { name, level, body })
            {
                var rect = (RectTransform)text.transform;
                // Between the portrait and the button.
                rect.offsetMin = new Vector2(textLeft, rect.offsetMin.y);
                rect.offsetMax = new Vector2(-(buttonWidth + 40f), rect.offsetMax.y);
                text.alignment = TextAlignmentOptions.TopLeft;
                AutoSize(text, 18f);
            }
            level.alignment = TextAlignmentOptions.TopRight;

            (Button buy, Image buyImage, TMP_Text cost) = CreateButton("BuyButton", card, Vector2.zero, new Vector2(buttonWidth, 104f), UiTheme.Dew, UiTheme.TextLight);
            var buyRect = (RectTransform)buy.transform;
            buyRect.anchorMin = buyRect.anchorMax = new Vector2(1f, 0.5f);
            buyRect.pivot = new Vector2(1f, 0.5f);
            buyRect.anchoredPosition = new Vector2(-18f, 0f);
            cost.fontSizeMax = 40f;
            const float costInset = 74f;
            ((RectTransform)cost.transform).offsetMin = new Vector2(costInset, ButtonLip);

            RectTransform costIcon = CreateRect("CostIcon", buy.transform);
            PlaceLeft(costIcon, 14f, 62f);
            costIcon.anchoredPosition += new Vector2(0f, ButtonLip * 0.5f);
            AddImage(costIcon, UiAssets.DewIcon(), Color.white, false).preserveAspect = true;

            var view = card.gameObject.AddComponent<RareBeeCardView>();
            EditorAssets.Set(view, ("beeIndex", index), ("icon", icon), ("nameText", name), ("levelText", level), ("bodyText", body),
                ("buyButton", buy), ("feedback", AddButtonFeedback(buy)), ("buyImage", buyImage), ("costText", cost),
                ("costIcon", costIcon.gameObject), ("costInset", costInset),
                ("buyColor", UiTheme.Dew), ("confirmColor", UiTheme.Good), ("freeColor", UiTheme.Good),
                ("disabledColor", UiTheme.Disabled), ("maxedColor", UiTheme.Good));
            return view;
        }

        /// <summary>A deep turquoise for the level, so it reads as Dewdrops progress on the cream card.</summary>
        static readonly Color RareLevelColor = new Color(0.1f, 0.5f, 0.58f);
    }
}
