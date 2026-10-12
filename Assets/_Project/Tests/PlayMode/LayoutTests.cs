using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Buzzfield.Core;
using Buzzfield.Economy;
using Buzzfield.Flowers;
using Buzzfield.Game;
using Buzzfield.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// The HUD and every panel on the screen shapes players have, rendered through the camera
    /// at each size: no control off screen or under another, no tap target under 48 dp on the
    /// common phones, no text spilling out of its box, and every flower and the hive clear of
    /// the HUD so a swipe or a tap reaches them.
    /// </summary>
    public class LayoutTests
    {
        static readonly Vector2Int[] Screens =
        {
            new Vector2Int(1080, 1920), // 16:9
            new Vector2Int(1080, 2340), // 19.5:9
            new Vector2Int(1080, 2400), // 20:9
            new Vector2Int(1080, 2520), // 21:9
            new Vector2Int(720, 1600),  // low-end 20:9
            new Vector2Int(1440, 3200), // high-density 20:9
            new Vector2Int(1200, 1920), // 16:10 tablet
            new Vector2Int(1536, 2048), // 4:3 tablet
        };

        /// <summary>Android's minimum touch target; held on the common 1080 px wide phones.</summary>
        const float MinTapDp = 48f;

        /// <summary>Width in dp of the reference phone: a 1080 px wide screen is about 411 dp.</summary>
        const float PhoneWidthDp = 411f;

        GameManager game;
        RenderTexture target;
        readonly List<string> offScreen = new List<string>();
        readonly SortedSet<string> small = new SortedSet<string>();
        readonly SortedSet<string> covered = new SortedSet<string>();
        readonly SortedSet<string> spilled = new SortedSet<string>();

        [UnitySetUp]
        public IEnumerator LoadFresh()
        {
            TestSave.Clear();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return Resize(Vector2Int.zero);
        }

        // --- panels ---

        [UnityTest]
        public IEnumerator EveryPanel_FitsCommonScreens()
        {
            // A new player's HUD first, then the late game with every HUD control showing.
            foreach (Vector2Int screen in Screens)
            {
                yield return Resize(screen);
                Measure("HUD new game", screen, null);
            }

            yield return MoveQueen();
            yield return MoveQueen();
            game.DebugGrantHoney(BigNumber.Create(1, 40));
            game.DebugGrantDew(20000);
            game.DebugTrustDeviceClock();
            game.DebugShiftDays(2);
            // A bloomed garden, so the Queen panel's Move button opens its confirmation.
            game.DebugCompleteGarden();
            // The daily panel opens by itself on the new day once the celebration is over; its
            // badge stays while the reward waits.
            float until = Time.realtimeSinceStartup + 10f;
            while (!game.IsDailyOpen && Time.realtimeSinceStartup < until)
                yield return null;
            Assert.That(game.IsDailyOpen, "the daily panel opens by itself on a new day");
            CloseEverything();
            // A running Dewdrops boost shows its chip under the gear.
            for (int i = 0; i < game.DewSettings.Offers.Count; i++)
            {
                if (game.DewSettings.Offers[i].kind == DewOfferKind.HoneyBoost && game.TryBuyDewOffer(i))
                    break;
            }
            yield return null;

            var queen = Object.FindAnyObjectByType<QueenPanelView>();
            var shop = Object.FindAnyObjectByType<ShopPanelView>();
            var settings = Object.FindAnyObjectByType<SettingsPanelView>();
            var hive = Object.FindAnyObjectByType<HivePanelView>();
            var daily = Object.FindAnyObjectByType<DailyPanelView>();
            var welcome = Object.FindAnyObjectByType<WelcomeBackView>();
            var gift = Object.FindAnyObjectByType<GiftOfferView>();
            var back = Object.FindAnyObjectByType<BackButtonHandler>();

            var states = new (string name, Func<IEnumerator> open, Func<GameObject> front)[]
            {
                ("HUD late game", null, null),
                ("Queen panel", () => Press("QueenButton"), () => Panel(queen)),
                ("Queen move confirm", () => Then(Press("QueenButton"), Press("MoveButton")), () => Panel(queen).transform.Find("Confirm").gameObject),
                ("Shop Dewdrops", () => Press("ShopButton"), () => Panel(shop)),
                ("Shop Boosts", () => Then(Press("ShopButton"), Press("BoostsTabButton")), () => Panel(shop)),
                ("Settings", () => Press("SettingsButton"), () => Panel(settings)),
                ("Settings delete confirm", () => Then(Press("SettingsButton"), Press("DeleteButton")), () => Panel(settings)),
                ("Hive", () => Press("HiveButton"), () => Panel(hive)),
                ("Daily", () => Press("DailyBadge"), () => Panel(daily)),
                ("Welcome back", () => Do(() => welcome.Show(7200, BigNumber.FromDouble(1e9), true, 4 * 3600, BigNumber.FromDouble(1e8))), () => Panel(welcome)),
                ("Quit dialog", () => Do(back.HandleBack), () => GameObject.Find("QuitDialog")),
                ("Butterfly gift offer", () => Do(() => gift.Show(BigNumber.FromDouble(5e7), 8f)), null),
                ("Butterfly pouch offer", () => Do(() => gift.ShowPouch(8f)), null),
            };

            foreach (Vector2Int screen in Screens)
            {
                yield return Resize(screen);
                foreach ((string name, Func<IEnumerator> open, Func<GameObject> front) in states)
                {
                    if (open != null)
                        yield return open();
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    Measure(name, screen, front?.Invoke());
                    if (screen == new Vector2Int(1080, 2400) || screen == new Vector2Int(1536, 2048))
                        SaveShot($"layout_{name.Replace(' ', '_').ToLowerInvariant()}_{screen.x}x{screen.y}");
                    CloseEverything();
                    yield return null;
                }
            }

            yield return Resize(Vector2Int.zero);
            var report = new List<string>();
            if (offScreen.Count > 0)
                report.Add("controls off screen:\n  " + string.Join("\n  ", offScreen.Distinct()));
            if (covered.Count > 0)
                report.Add("controls a finger cannot reach (something else is on top):\n  " + string.Join("\n  ", covered));
            if (small.Count > 0)
                report.Add($"tap targets under {MinTapDp} dp on a {PhoneWidthDp} dp wide phone:\n  " + string.Join("\n  ", small));
            if (spilled.Count > 0)
                report.Add("text spilling out of its box:\n  " + string.Join("\n  ", spilled));
            Assert.That(report, Is.Empty, string.Join("\n", report));
        }

        static GameObject Panel(Component view) =>
            (GameObject)view.GetType().GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);

        static IEnumerator Press(string name)
        {
            TestUi.Press(name);
            yield return null;
        }

        static IEnumerator Then(IEnumerator first, IEnumerator second)
        {
            yield return first;
            yield return second;
        }

        static IEnumerator Do(Action action)
        {
            action();
            yield return null;
        }

        void CloseEverything()
        {
            var back = Object.FindAnyObjectByType<BackButtonHandler>();
            for (int i = 0; i < 6 && back.IsModalOpen; i++)
                back.HandleBack();
            var gift = Object.FindAnyObjectByType<GiftOfferView>();
            if (gift.IsShowing)
                gift.Hide();
        }

        /// <param name="front">The open panel: its controls must be reachable. Null for the HUD.</param>
        void Measure(string state, Vector2Int screen, GameObject front)
        {
            var visible = new Rect(0, 0, screen.x, screen.y);
            float minPixels = MinTapDp * screen.x / PhoneWidthDp;
            bool phone = screen.x == 1080 && screen.y >= 2340;
            string where = $"{state} {screen.x}x{screen.y}";

            foreach (Selectable control in Object.FindObjectsByType<Selectable>(FindObjectsSortMode.InstanceID))
            {
                if (!control.isActiveAndEnabled)
                    continue;
                var rect = (RectTransform)control.transform;
                Rect r = TestUi.ScreenRect(rect);
                ScrollRect list = control.GetComponentInParent<ScrollRect>();
                // List items scroll into view: only the ones inside the viewport count for position.
                bool inView = list == null || TestUi.ScreenRect(list.viewport != null ? list.viewport : (RectTransform)list.transform).Contains(r.center);
                if (list == null && (r.xMin < visible.xMin - 1 || r.yMin < visible.yMin - 1 || r.xMax > visible.xMax + 1 || r.yMax > visible.yMax + 1))
                    offScreen.Add($"{where}: {TestUi.Path(control.transform)} at {Show(r)}");

                Rect hit = TestUi.HitRect(control);
                if (phone && Math.Min(hit.width, hit.height) < minPixels - 0.5f)
                    small.Add($"{TestUi.Path(control.transform)} {Mathf.RoundToInt(hit.width / minPixels * MinTapDp)}x{Mathf.RoundToInt(hit.height / minPixels * MinTapDp)} dp");

                // Controls of the front layer (the open panel, or the HUD when none is open) must take the tap.
                bool inFront = front != null ? control.transform.IsChildOf(front.transform) && !UnderNestedOpenModal(control.transform, front.transform)
                    : !UnderAnyPanel(control.transform);
                if (inFront && inView && control.interactable && !TestUi.Reachable(control))
                {
                    GameObject top = TestUi.TopHit(TestUi.Center(rect));
                    covered.Add($"{state}: {TestUi.Path(control.transform)} under {(top != null ? TestUi.Path(top.transform) : "nothing")} ({screen.x}x{screen.y})");
                }
            }

            foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.InstanceID))
            {
                if (!text.isActiveAndEnabled || string.IsNullOrEmpty(text.text) || IsIcon(text))
                    continue;
                text.ForceMeshUpdate();
                Rect box = text.rectTransform.rect;
                Bounds bounds = text.textBounds;
                if (bounds.size.x <= 0f)
                    continue;
                // A few units of slack for glyph overhang and the outline.
                float slackX = Math.Max(6f, box.width * 0.02f);
                float slackY = Math.Max(8f, box.height * 0.12f);
                bool wide = bounds.min.x < box.xMin - slackX || bounds.max.x > box.xMax + slackX;
                bool tall = bounds.min.y < box.yMin - slackY || bounds.max.y > box.yMax + slackY;
                bool shrunkOut = text.enableAutoSizing && text.isTextOverflowing;
                if (wide || tall || shrunkOut || text.isTextTruncated)
                    spilled.Add($"{TestUi.Path(text.transform)} \"{Clip(text.text)}\" {(wide ? "too wide" : tall ? "too tall" : "cut off")} " +
                        $"(text {bounds.size.x:0}x{bounds.size.y:0} in {box.width:0}x{box.height:0}, {where})");
            }
        }

        static bool IsIcon(TMP_Text text) => text.font != null && text.font.name.StartsWith("Icons", StringComparison.Ordinal);

        static string Clip(string text) => (text.Length > 30 ? text.Substring(0, 30) + "..." : text).Replace("\n", " ");

        static string Show(Rect r) => $"({r.xMin:0},{r.yMin:0})-({r.xMax:0},{r.yMax:0})";

        /// <summary>Panels are full-screen modals named in the scene builder; the HUD is everything else.</summary>
        static readonly string[] Modals = { "QueenPanel", "ShopPanel", "SettingsPanel", "DailyPanel", "HivePanel", "WelcomeBack", "QuitDialog" };

        static bool UnderAnyPanel(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent)
            {
                if (Array.IndexOf(Modals, p.name) >= 0)
                    return true;
            }
            return false;
        }

        /// <summary>The Queen panel's Confirm overlay sits inside the panel and covers it.</summary>
        static bool UnderNestedOpenModal(Transform t, Transform front)
        {
            Transform confirm = front.Find("Confirm");
            return confirm != null && confirm.gameObject.activeSelf && t != confirm && !t.IsChildOf(confirm);
        }

        // --- the garden under the HUD ---

        /// <summary>
        /// On every screen shape and in every garden the flower heads and the hive sit where a
        /// finger can reach them: on screen and not under a HUD control or the bars' tap blockers.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryFlowerAndTheHive_StayClearOfTheHud()
        {
            int gardens = ((PrestigeSettings)typeof(GameManager).GetField("prestigeSettings", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(game)).Gardens.Count;
            var findings = new SortedSet<string>();
            // The late-game HUD: goal pill, hive button, Dewdrops counter.
            game.DebugGrantDew(10);
            for (int g = 0; g < gardens; g++)
            {
                if (g > 0)
                    yield return MoveQueen();
                foreach (Vector2Int screen in Screens)
                {
                    yield return Resize(screen);
                    var visible = new Rect(0, 0, screen.x, screen.y);
                    IReadOnlyList<Flower> flowers = game.Flowers.Flowers;
                    for (int i = 0; i < flowers.Count; i++)
                        Check($"garden {g + 1} flower {i}", flowers[i].NectarPoint, screen, visible, findings);
                    Transform hive = Object.FindAnyObjectByType<HiveView>().transform;
                    Check($"garden {g + 1} hive", hive.position + Vector3.up * (0.7f * hive.lossyScale.y), screen, visible, findings);
                    if (g == 0)
                    {
                        LogFraming(screen);
                        SaveShot($"garden_{screen.x}x{screen.y}");
                    }
                }
            }
            yield return Resize(Vector2Int.zero);
            Assert.That(findings, Is.Empty, string.Join("\n", findings));
        }

        /// <summary>Where the flower heads sit on screen, as fractions: for comparing the framing between builds.</summary>
        void LogFraming(Vector2Int screen)
        {
            float left = 1f, right = 0f, bottom = 1f, top = 0f;
            foreach (Flower flower in game.Flowers.Flowers)
            {
                Vector3 p = Camera.main.WorldToScreenPoint(flower.NectarPoint);
                left = Mathf.Min(left, p.x / screen.x);
                right = Mathf.Max(right, p.x / screen.x);
                bottom = Mathf.Min(bottom, p.y / screen.y);
                top = Mathf.Max(top, p.y / screen.y);
            }
            TestContext.Out.WriteLine($"{screen.x}x{screen.y}: flowers x {left:0.000}-{right:0.000}, y {bottom:0.000}-{top:0.000} (from the bottom)");
        }

        /// <summary>Writes what the camera renders at the current size to BZ_SHOT_DIR, when set.</summary>
        void SaveShot(string name)
        {
            string directory = Environment.GetEnvironmentVariable("BZ_SHOT_DIR");
            if (string.IsNullOrEmpty(directory) || target == null)
                return;
            Camera.main.Render();
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, name + ".png"), image.EncodeToPNG());
            Object.Destroy(image);
        }

        static void Check(string what, Vector3 world, Vector2Int screen, Rect visible, SortedSet<string> findings)
        {
            Vector3 point = Camera.main.WorldToScreenPoint(world);
            if (point.z <= 0f || !visible.Contains(point))
            {
                findings.Add($"{what} off screen at {screen.x}x{screen.y} ({point.x:0},{point.y:0})");
                return;
            }
            GameObject top = TestUi.TopHit(point);
            if (top == null || top.GetComponent<SwipeCatcher>() == null)
                findings.Add($"{what} under {(top != null ? TestUi.Path(top.transform) : "nothing")} at {screen.x}x{screen.y}");
        }

        IEnumerator MoveQueen()
        {
            game.DebugCompleteGarden();
            game.DebugGrantHoney(game.Prestige.MoveCost * BigNumber.FromDouble(2));
            Assert.That(game.TryMoveQueen(), "Queen move");
            yield return null;
            // The move is a natural break; a celebration or panel left over would sit in the way.
            CloseEverything();
            yield return null;
        }

        // --- rendering at a size ---

        /// <summary>
        /// Renders the camera and the UI into a texture of the given size: the canvas switches to
        /// camera space, so the scaler, the hit tests and the garden framing all see that size.
        /// Zero goes back to the overlay.
        /// </summary>
        IEnumerator Resize(Vector2Int size)
        {
            Camera camera = Camera.main;
            Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c => c.isRootCanvas);
            if (target != null)
            {
                camera.targetTexture = null;
                target.Release();
                Object.Destroy(target);
                target = null;
            }
            if (size == Vector2Int.zero)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                camera.ResetAspect();
                camera.SendMessage("Refit", SendMessageOptions.DontRequireReceiver);
                yield return null;
                yield break;
            }
            target = new RenderTexture(size.x, size.y, 24);
            camera.targetTexture = target;
            camera.aspect = size.x / (float)size.y;
            camera.SendMessage("Refit", SendMessageOptions.DontRequireReceiver);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = camera.nearClipPlane + 0.1f;
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            Vector2 drawn = ((RectTransform)canvas.transform).rect.size * canvas.scaleFactor;
            Assert.That(Vector2.Distance(drawn, size), Is.LessThan(2f), $"the canvas draws {drawn} for a {size} screen");
        }
    }
}
