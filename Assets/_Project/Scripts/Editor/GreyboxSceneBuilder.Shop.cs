using Buzzfield.Ads;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.Editor
{
    /// <summary>
    /// The scene builder's part for the shop (docs/DEWDROPS.md 9): the modal with its Dewdrops
    /// and Boosts tabs, the Dewdrops balance in its header, and the boost chip under the gear
    /// that shows a running Dewdrops boost.
    /// </summary>
    public static partial class GreyboxSceneBuilder
    {
        static readonly Vector2 ShopWindowSize = new Vector2(940f, 1520f);
        const float ShopTabsY = -156f;
        const float ShopContentY = -276f;
        const float ShopContentHeight = 1000f;
        const float ShopRowHeight = 190f;
        const float ShopRowSpacing = 16f;
        const float OfferRowHeight = 150f;
        const float OfferRowSpacing = 14f;

        /// <summary>
        /// Modal shop: the title band with the Dewdrops balance on the right and a small Privacy
        /// button on the left (shown only where the ad consent choices must be reachable), the
        /// two tab buttons, the tab contents, a status line and the close button.
        /// </summary>
        static ShopPanelView CreateShopPanel(Transform root, Button openButton, GameObject newBadge, Button dewButton,
            StoreCatalog catalog, DewSettings dewSettings)
        {
            RectTransform panel = CreateModal("ShopPanel", root, ShopWindowSize, UiTheme.Shop, out RectTransform window);
            CreateTitle(window, Strings.ShopTitle);
            TMP_Text balance = CreateShopBalance(window);

            (Button dewTabButton, Image dewTabImage, TMP_Text dewTabLabel) = CreateButton("DewTabButton", window,
                new Vector2(-215f, ShopTabsY), new Vector2(410f, 100f), UiTheme.Shop, UiTheme.TextLight);
            dewTabLabel.text = Strings.ShopTabDewdrops;
            (Button boostsTabButton, Image boostsTabImage, TMP_Text boostsTabLabel) = CreateButton("BoostsTabButton", window,
                new Vector2(215f, ShopTabsY), new Vector2(410f, 100f), UiTheme.Neutral, UiTheme.TextLight);
            boostsTabLabel.text = Strings.ShopTabBoosts;

            (GameObject dewTab, ShopItemView[] items, Button restore) = CreateDewTab(window, catalog);
            DewBoostsTabView boostsTab = CreateBoostsTab(window, dewSettings);

            TMP_Text status = CreateText("StatusText", window, new Vector2(0f, ShopContentY - ShopContentHeight - 14f), 64f, 38f, FontStyles.Italic, UiTheme.TextSoft);
            AutoSize(status, 24f);
            status.text = string.Empty;
            (Button close, _, TMP_Text closeLabel) = CreateButton("CloseButton", window, new Vector2(0f, -1370f), new Vector2(360f, 120f), UiTheme.Neutral);
            closeLabel.text = Strings.Close;
            (Button privacy, _, TMP_Text privacyLabel) = CreateButton("PrivacyButton", window, new Vector2(-340f, -36f), new Vector2(180f, 80f), UiTheme.Neutral);
            privacyLabel.text = Strings.PrivacyButton;
            privacy.gameObject.SetActive(false);

            // On the always-active root, like the other panel views, so it can open the hidden panel.
            var view = root.gameObject.AddComponent<ShopPanelView>();
            EditorAssets.Set(view, ("openButton", openButton), ("dewButton", dewButton), ("newBadge", newBadge), ("panel", panel.gameObject), ("closeButton", close),
                ("items", items), ("statusText", status), ("restoreButton", restore), ("privacyButton", privacy), ("dewBalanceText", balance),
                ("dewTabButton", dewTabButton), ("dewTabImage", dewTabImage), ("boostsTabButton", boostsTabButton), ("boostsTabImage", boostsTabImage),
                ("dewTab", dewTab), ("boostsTab", boostsTab), ("tabOnColor", UiTheme.Shop), ("tabOffColor", UiTheme.Neutral));
            boostsTab.gameObject.SetActive(false);
            panel.gameObject.SetActive(false);
            return view;
        }

        /// <summary>Pale turquoise pill on the right of the title band: crystal icon and the balance.</summary>
        static TMP_Text CreateShopBalance(RectTransform window)
        {
            RectTransform pill = CreateRect("DewBalance", window);
            pill.anchorMin = pill.anchorMax = new Vector2(0.5f, 1f);
            pill.pivot = new Vector2(0.5f, 1f);
            pill.sizeDelta = new Vector2(230f, 84f);
            pill.anchoredPosition = new Vector2(330f, -32f);
            AddImage(pill, pillSprite, UiTheme.DewPale, false);
            RectTransform icon = CreateRect("Icon", pill);
            PlaceLeft(icon, 8f, 70f);
            AddImage(icon, UiAssets.DewIcon(), Color.white, false).preserveAspect = true;
            TMP_Text text = CreateText("BalanceText", pill, Vector2.zero, 84f, 46f, FontStyles.Normal, UiTheme.TextDark);
            var rect = (RectTransform)text.transform;
            Stretch(rect);
            rect.offsetMin = new Vector2(80f, 0f);
            rect.offsetMax = new Vector2(-16f, 0f);
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            AutoSize(text, 24f);
            return text;
        }

        /// <summary>The content area under the tab buttons; both tabs fill it.</summary>
        static RectTransform CreateShopTabArea(RectTransform window, string name)
        {
            RectTransform area = CreateRect(name, window);
            area.anchorMin = new Vector2(0f, 1f);
            area.anchorMax = new Vector2(1f, 1f);
            area.pivot = new Vector2(0.5f, 1f);
            area.sizeDelta = new Vector2(-50f, ShopContentHeight);
            area.anchoredPosition = new Vector2(0f, ShopContentY);
            return area;
        }

        /// <summary>
        /// Dewdrops tab: one row per catalog product in catalog order (a hidden row leaves no gap)
        /// and the restore button at the end, in a list that scrolls.
        /// </summary>
        static (GameObject, ShopItemView[], Button) CreateDewTab(RectTransform window, StoreCatalog catalog)
        {
            RectTransform area = CreateShopTabArea(window, "DewTab");
            area.gameObject.AddComponent<RectMask2D>();
            // A clear graphic so drags between rows still reach the scroll view.
            area.gameObject.AddComponent<Image>().color = Color.clear;

            RectTransform content = CreateRect("Content", area);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = ShopRowSpacing;
            layout.padding = new RectOffset(0, 0, 4, 24);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = area.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = area;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var items = new ShopItemView[catalog.Products.Count];
            for (int i = 0; i < items.Length; i++)
                items[i] = CreateShopItem(content, catalog.Products[i]);

            RectTransform footer = CreateRect("RestoreRow", content);
            footer.sizeDelta = new Vector2(0f, 130f);
            (Button restore, _, TMP_Text restoreLabel) = CreateButton("RestoreButton", footer, new Vector2(0f, -6f), new Vector2(520f, 116f), UiTheme.Ad, UiTheme.TextLight);
            restoreLabel.text = Strings.RestorePurchases;
            return (area.gameObject, items, restore);
        }

        /// <summary>
        /// Product row: a crystal for products that give Dewdrops, name and description on the
        /// left, buy button with the price on the right.
        /// </summary>
        static ShopItemView CreateShopItem(Transform list, StoreProduct product)
        {
            RectTransform row = CreateRect(product.id, list);
            row.sizeDelta = new Vector2(0f, ShopRowHeight);
            AddImage(row, roundSprite, product.dewGrant > 0 ? UiTheme.DewPale : UiTheme.CreamShade, false);

            const float buttonWidth = 250f;
            float textLeft = 30f;
            if (product.dewGrant > 0)
            {
                RectTransform icon = CreateRect("Icon", row);
                PlaceLeft(icon, 20f, 116f);
                AddImage(icon, UiAssets.DewIcon(), Color.white, false).preserveAspect = true;
                textLeft = 150f;
            }

            TMP_Text title = CreateText("Title", row, new Vector2(0f, -22f), 62f, 46f, FontStyles.Normal, UiTheme.TextDark);
            TMP_Text body = CreateText("Body", row, new Vector2(0f, -88f), 86f, 30f, FontStyles.Normal, UiTheme.TextSoft);
            foreach (TMP_Text text in new[] { title, body })
            {
                var rect = (RectTransform)text.transform;
                // Left column, clear of the buy button on the right.
                rect.offsetMin = new Vector2(textLeft, rect.offsetMin.y);
                rect.offsetMax = new Vector2(-(buttonWidth + 50f), rect.offsetMax.y);
                text.alignment = TextAlignmentOptions.TopLeft;
                AutoSize(text, 22f);
            }

            (Button buy, Image buyImage, TMP_Text price) = CreateButton("BuyButton", row, Vector2.zero, new Vector2(buttonWidth, 130f), UiTheme.Primary);
            var buyRect = (RectTransform)buy.transform;
            buyRect.anchorMin = buyRect.anchorMax = new Vector2(1f, 0.5f);
            buyRect.pivot = new Vector2(1f, 0.5f);
            buyRect.anchoredPosition = new Vector2(-26f, 0f);

            var view = row.gameObject.AddComponent<ShopItemView>();
            EditorAssets.Set(view, ("productId", product.id), ("titleText", title), ("bodyText", body),
                ("buyButton", buy), ("buyImage", buyImage), ("priceText", price),
                ("buyColor", UiTheme.Primary), ("ownedColor", UiTheme.Good), ("disabledColor", UiTheme.Disabled));
            return view;
        }

        /// <summary>
        /// Boosts tab: the running boost and the next in line on top, one row per offer below;
        /// a lock and a short note replace both until the tab unlocks.
        /// </summary>
        static DewBoostsTabView CreateBoostsTab(RectTransform window, DewSettings dewSettings)
        {
            RectTransform area = CreateShopTabArea(window, "BoostsTab");

            RectTransform content = CreateRect("Content", area);
            Stretch(content);
            TMP_Text running = CreateText("RunningText", content, new Vector2(0f, -6f), 58f, 42f, FontStyles.Normal, UiTheme.TextDark);
            AutoSize(running, 26f);
            running.text = Strings.BoostNone;
            TMP_Text queue = CreateText("QueueText", content, new Vector2(0f, -66f), 44f, 30f, FontStyles.Italic, UiTheme.TextSoft);
            AutoSize(queue, 20f);
            queue.text = string.Empty;

            RectTransform list = CreateRect("Offers", content);
            list.anchorMin = new Vector2(0f, 1f);
            list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            int count = dewSettings.Offers.Count;
            list.sizeDelta = new Vector2(0f, count * OfferRowHeight + Mathf.Max(0, count - 1) * OfferRowSpacing);
            list.anchoredPosition = new Vector2(0f, -126f);
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = OfferRowSpacing;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var rows = new DewOfferRowView[count];
            for (int i = 0; i < count; i++)
                rows[i] = CreateDewOfferRow(list, i);

            RectTransform locked = CreateRect("LockedNote", area);
            Stretch(locked);
            TMP_Text lockIcon = CreateIcon("Icon", locked, UiTheme.IconLock, 120f, UiTheme.Neutral);
            PlaceTop((RectTransform)lockIcon.transform, 220f, 150f);
            TMP_Text note = CreateText("Note", locked, new Vector2(0f, -400f), 140f, 46f, FontStyles.Normal, UiTheme.TextSoft);
            note.text = Strings.BoostsLocked;
            locked.gameObject.SetActive(false);

            var view = area.gameObject.AddComponent<DewBoostsTabView>();
            EditorAssets.Set(view, ("rows", rows), ("content", content.gameObject), ("lockedNote", locked.gameObject),
                ("runningText", running), ("queueText", queue));
            return view;
        }

        /// <summary>Offer row: name and what it gives on the left, a turquoise buy button with a crystal and the cost on the right.</summary>
        static DewOfferRowView CreateDewOfferRow(Transform list, int index)
        {
            RectTransform row = CreateRect($"Offer{index}", list);
            row.sizeDelta = new Vector2(0f, OfferRowHeight);
            AddImage(row, roundSprite, UiTheme.CreamShade, false);

            const float buttonWidth = 230f;
            TMP_Text title = CreateText("Title", row, new Vector2(0f, -16f), 54f, 40f, FontStyles.Normal, UiTheme.TextDark);
            TMP_Text body = CreateText("Body", row, new Vector2(0f, -72f), 66f, 26f, FontStyles.Normal, UiTheme.TextSoft);
            foreach (TMP_Text text in new[] { title, body })
            {
                var rect = (RectTransform)text.transform;
                rect.offsetMin = new Vector2(28f, rect.offsetMin.y);
                rect.offsetMax = new Vector2(-(buttonWidth + 48f), rect.offsetMax.y);
                text.alignment = TextAlignmentOptions.TopLeft;
                AutoSize(text, 20f);
            }

            (Button buy, Image buyImage, TMP_Text cost) = CreateButton("BuyButton", row, Vector2.zero, new Vector2(buttonWidth, 112f), UiTheme.Dew, UiTheme.TextLight);
            var buyRect = (RectTransform)buy.transform;
            buyRect.anchorMin = buyRect.anchorMax = new Vector2(1f, 0.5f);
            buyRect.pivot = new Vector2(1f, 0.5f);
            buyRect.anchoredPosition = new Vector2(-22f, 0f);
            cost.fontSize = cost.fontSizeMax = 44f;
            const float costInset = 82f;
            ((RectTransform)cost.transform).offsetMin = new Vector2(costInset, ButtonLip);

            RectTransform icon = CreateRect("CostIcon", buy.transform);
            PlaceLeft(icon, 16f, 70f);
            icon.anchoredPosition += new Vector2(0f, ButtonLip * 0.5f);
            AddImage(icon, UiAssets.DewIcon(), Color.white, false).preserveAspect = true;

            var view = row.gameObject.AddComponent<DewOfferRowView>();
            EditorAssets.Set(view, ("offerIndex", index), ("titleText", title), ("bodyText", body),
                ("buyButton", buy), ("buyImage", buyImage), ("costText", cost), ("costIcon", icon.gameObject), ("costInset", costInset),
                ("buyColor", UiTheme.Dew), ("confirmColor", UiTheme.Good), ("disabledColor", UiTheme.Disabled));
            return view;
        }

        /// <summary>Turquoise chip under the gear while a Dewdrops boost runs: crystal, multiplier and time left; a tap opens the Boosts tab.</summary>
        static DewBoostChipView CreateDewBoostChip(Transform root)
        {
            RectTransform holder = CreateRect("DewBoostChip", root);
            holder.anchorMin = holder.anchorMax = new Vector2(1f, 1f);
            holder.pivot = new Vector2(1f, 1f);
            holder.sizeDelta = new Vector2(250f, 84f);
            holder.anchoredPosition = new Vector2(-24f, UnderBarY - GearSize - 16f);

            // The view hides the chip, so its shadow lives inside it.
            RectTransform chip = CreateRect("Chip", holder);
            Stretch(chip);
            RectTransform face = CreateRect("Face", chip);
            Stretch(face);
            AddShadow(face);
            Image image = AddImage(face, pillSprite, UiTheme.Dew, true);
            var button = chip.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            RectTransform icon = CreateRect("Icon", chip);
            PlaceLeft(icon, 8f, 68f);
            AddImage(icon, UiAssets.DewIcon(), Color.white, false).preserveAspect = true;
            TMP_Text label = CreateText("Label", chip, Vector2.zero, 84f, 40f, FontStyles.Normal, UiTheme.TextLight);
            var labelRect = (RectTransform)label.transform;
            Stretch(labelRect);
            labelRect.offsetMin = new Vector2(78f, 0f);
            labelRect.offsetMax = new Vector2(-16f, 0f);
            label.fontSharedMaterial = outlinedText;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            AutoSize(label, 22f);
            label.text = string.Format(Strings.BoostChipFormat, "3", "29m");
            chip.gameObject.SetActive(false);

            var view = holder.gameObject.AddComponent<DewBoostChipView>();
            EditorAssets.Set(view, ("chip", chip.gameObject), ("button", button), ("label", label));
            return view;
        }
    }
}
