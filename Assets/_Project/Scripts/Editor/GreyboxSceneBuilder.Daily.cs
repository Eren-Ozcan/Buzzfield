using Buzzfield.Core;
using Buzzfield.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buzzfield.Editor
{
    /// <summary>
    /// The scene builder's part for the daily login streak: the calendar badge under the goal pill
    /// and the Daily Dewdrops panel (docs/DEWDROPS.md section 6).
    /// </summary>
    public static partial class GreyboxSceneBuilder
    {
        const int DailyDays = 7;
        const float DayTileWidth = 190f;
        const float DayTileHeight = 230f;
        const float DayTileGap = 18f;
        const float DailyTilesTop = -168f;

        /// <summary>Round calendar button on the left under the goal pill, with an alert dot; hidden until today's Dewdrops wait.</summary>
        static (GameObject badge, Button button) CreateDailyBadge(Transform root)
        {
            RectTransform holder = CreateRect("DailyBadge", root);
            holder.anchorMin = holder.anchorMax = new Vector2(0f, 1f);
            holder.pivot = new Vector2(0f, 1f);
            holder.sizeDelta = new Vector2(GearSize, GearSize);
            holder.anchoredPosition = new Vector2(24f, UnderBarY - 100f - 18f);
            // The view hides the badge, so its shadow lives inside it.
            RectTransform face = CreateRect("Face", holder);
            Stretch(face);
            AddShadow(face);
            Image image = AddImage(face, circleSprite, UiTheme.Cream, true);
            var button = holder.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            TMP_Text icon = CreateIcon("Icon", holder, UiTheme.IconCalendar, 64f, UiTheme.Dew);
            Stretch((RectTransform)icon.transform);
            GameObject dot = CreateBadge("Dot", holder, UiTheme.Alert);
            ((RectTransform)dot.transform).sizeDelta = new Vector2(38f, 38f);
            ((RectTransform)dot.transform).anchoredPosition = new Vector2(-4f, -4f);
            dot.SetActive(true);
            holder.gameObject.SetActive(false);
            return (holder.gameObject, button);
        }

        /// <summary>
        /// Modal Daily Dewdrops panel: seven day tiles (four, then two and the wide last day), the
        /// line about the streak, Claim and Close. 1150 px tall, so it fits a 4:3 tablet.
        /// </summary>
        static DailyPanelView CreateDailyPanel(Transform root, GameObject badge, Button badgeButton)
        {
            RectTransform panel = CreateModal("DailyPanel", root, new Vector2(900f, 1150f), UiTheme.Dew, out RectTransform window);
            CreateTitle(window, Strings.DailyTitle);

            var backgrounds = new Image[DailyDays];
            var labels = new TMP_Text[DailyDays];
            var amounts = new TMP_Text[DailyDays];
            var checks = new GameObject[DailyDays];
            float rowWidth = 4f * DayTileWidth + 3f * DayTileGap;
            for (int i = 0; i < DailyDays; i++)
            {
                bool last = i == DailyDays - 1;
                int row = i < 4 ? 0 : 1;
                int column = i < 4 ? i : i - 4;
                float width = last ? 2f * DayTileWidth + DayTileGap : DayTileWidth;
                float left = -rowWidth * 0.5f + column * (DayTileWidth + DayTileGap);
                var position = new Vector2(left + width * 0.5f, DailyTilesTop - row * (DayTileHeight + DayTileGap));
                (backgrounds[i], labels[i], amounts[i], checks[i]) = CreateDayTile(window, i, position, new Vector2(width, DayTileHeight), last);
            }

            float statusTop = DailyTilesTop - 2f * DayTileHeight - DayTileGap - 24f;
            TMP_Text status = CreateText("StatusText", window, new Vector2(0f, statusTop), 110f, 36f, FontStyles.Normal, UiTheme.TextSoft);
            AutoSize(status, 22f);
            status.text = Strings.DailyHint;

            (Button claim, _, TMP_Text claimLabel) = CreateButton("ClaimButton", window, new Vector2(0f, statusTop - 128f), new Vector2(560f, 150f), UiTheme.Dew, UiTheme.TextLight);
            claimLabel.fontSharedMaterial = outlinedText;
            claimLabel.text = string.Format(Strings.DailyClaimFormat, 10);
            (Button close, _, TMP_Text closeLabel) = CreateButton("CloseButton", window, new Vector2(0f, statusTop - 300f), new Vector2(360f, 110f), UiTheme.Neutral);
            closeLabel.text = Strings.Close;

            // On the always-active root, like the other panel views, so it can open the hidden panel.
            var view = root.gameObject.AddComponent<DailyPanelView>();
            EditorAssets.Set(view, ("panel", panel.gameObject), ("closeButton", close), ("claimButton", claim), ("claimLabel", claimLabel),
                ("statusText", status), ("dayBackgrounds", backgrounds), ("dayLabels", labels), ("dayAmounts", amounts), ("dayChecks", checks),
                ("doneColor", UiTheme.GoodPale), ("todayColor", UiTheme.DewPale), ("laterColor", UiTheme.CreamShade),
                ("badge", badge), ("badgeButton", badgeButton));
            panel.gameObject.SetActive(false);
            return view;
        }

        /// <summary>One day: "Day N" on top, the crystal, the Dewdrops under it and a tick in the corner once claimed.</summary>
        static (Image, TMP_Text, TMP_Text, GameObject) CreateDayTile(RectTransform window, int index, Vector2 position, Vector2 size, bool big)
        {
            RectTransform tile = CreateRect($"Day{index + 1}", window);
            tile.anchorMin = tile.anchorMax = new Vector2(0.5f, 1f);
            tile.pivot = new Vector2(0.5f, 1f);
            tile.sizeDelta = size;
            tile.anchoredPosition = position;
            Image background = AddImage(tile, roundSprite, UiTheme.CreamShade, false);

            TMP_Text label = CreateText("Label", tile, new Vector2(0f, -10f), 48f, 34f, FontStyles.Normal, UiTheme.TextSoft);
            ((RectTransform)label.transform).sizeDelta = new Vector2(-20f, 48f);
            AutoSize(label, 20f);
            label.text = string.Format(Strings.DailyDayFormat, index + 1);

            RectTransform crystal = CreateRect("Crystal", tile);
            crystal.anchorMin = crystal.anchorMax = new Vector2(0.5f, 0.5f);
            float crystalSize = big ? 124f : 92f;
            crystal.sizeDelta = new Vector2(crystalSize, crystalSize);
            crystal.anchoredPosition = new Vector2(0f, 4f);
            AddImage(crystal, UiAssets.DewIcon(), Color.white, false).preserveAspect = true;

            TMP_Text amount = CreateText("Amount", tile, new Vector2(0f, -size.y + 62f), 54f, big ? 48f : 40f, FontStyles.Normal, UiTheme.TextDark);
            ((RectTransform)amount.transform).sizeDelta = new Vector2(-20f, 54f);
            AutoSize(amount, 22f);
            amount.text = string.Format(Strings.DewGainFormat, 10);

            RectTransform check = CreateRect("Check", tile);
            check.anchorMin = check.anchorMax = new Vector2(1f, 1f);
            check.sizeDelta = new Vector2(62f, 62f);
            check.anchoredPosition = new Vector2(-14f, -14f);
            AddImage(check, circleSprite, UiTheme.Good, false);
            TMP_Text tick = CreateIcon("Icon", check, UiTheme.IconCheck, 46f, UiTheme.TextLight);
            Stretch((RectTransform)tick.transform);
            check.gameObject.SetActive(false);
            return (background, label, amount, check.gameObject);
        }
    }
}
