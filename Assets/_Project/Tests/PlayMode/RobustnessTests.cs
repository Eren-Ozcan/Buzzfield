using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Buzzfield.Core;
using Buzzfield.Flowers;
using Buzzfield.Game;
using Buzzfield.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// What a feature test does not look for: a save field the game forgets to bring back,
    /// saves with values no build writes, and thousands of random taps and swipes. Any error or
    /// exception logged during a test fails it (Unity Test Framework), and so does a character
    /// the fonts cannot draw.
    /// </summary>
    public class RobustnessTests
    {
        /// <summary>Buttons a random tap must not press: they leave the app or wipe the game (each has its own test).</summary>
        static readonly HashSet<string> MonkeySkip = new HashSet<string> { "QuitButton", "PrivacyPolicy", "DeleteButton" };

        readonly List<string> missingGlyphs = new List<string>();
        GameManager game;

        [UnitySetUp]
        public IEnumerator LoadFresh()
        {
            Application.logMessageReceived += WatchLog;
            TestSave.Clear();
            yield return LoadMain();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Application.logMessageReceived -= WatchLog;
            Time.timeScale = 1f;
            yield return null;
            Assert.That(missingGlyphs, Is.Empty, "text the fonts cannot draw:\n" + string.Join("\n", missingGlyphs.Distinct()));
        }

        void WatchLog(string message, string stackTrace, LogType type)
        {
            if (message.Contains("was not found in the") && message.Contains("font asset"))
                missingGlyphs.Add(message);
        }

        IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            // Start has run (offline evaluation and the first save); no autosave yet.
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
        }

        /// <summary>
        /// Removes the running game before a test writes a save for the next load, so nothing
        /// of the old game (a late trusted time answer, an autosave) can write over it.
        /// </summary>
        IEnumerator StopGame()
        {
            Object.Destroy(game.gameObject);
            game = null;
            yield return null;
        }

        // --- save ---

        /// <summary>Paths the game writes fresh on every save rather than carrying over.</summary>
        static readonly string[] Recomputed = { "saveVersion", "clock.", "offlineRate.", "stats.playSeconds" };

        static bool IsRecomputed(string path) => Recomputed.Any(r => path.StartsWith(r, StringComparison.Ordinal));

        /// <summary>
        /// Every field of <see cref="SaveData"/> is moved off a new game's value, loaded by the game
        /// and saved again on the spot. A field the game forgets to restore or capture comes back
        /// changed. A new field fails the coverage check until this test gives it a value.
        /// </summary>
        [UnityTest]
        public IEnumerator EverySavedField_SurvivesAReload()
        {
            SaveData fresh = TestSave.Files().Load();
            Assert.That(fresh, Is.Not.Null, "a new game saves at once");

            // Garden 3, so the flower slots below match the garden the save is for.
            yield return MoveQueen();
            yield return MoveQueen();
            SaveData expected = RichSave();
            yield return StopGame();
            Assert.That(TestSave.Files().Save(expected));

            Dictionary<string, object> freshLeaves = Leaves(fresh);
            Dictionary<string, object> expectedLeaves = Leaves(expected);
            List<string> untouched = Groups(expectedLeaves.Keys.Concat(freshLeaves.Keys))
                .Where(group => !IsRecomputed(group))
                .Where(group => SameGroup(group, expectedLeaves, freshLeaves))
                .ToList();
            Assert.That(untouched, Is.Empty, "give these fields a value a new game does not have, or a missing restore hides");

            yield return LoadMain();
            SaveData loaded = TestSave.Files().Load();
            Dictionary<string, object> loadedLeaves = Leaves(loaded);
            List<string> lost = expectedLeaves.Keys.Union(loadedLeaves.Keys)
                .Where(path => !IsRecomputed(path))
                .Where(path => !Same(expectedLeaves.TryGetValue(path, out object a) ? a : "(none)", loadedLeaves.TryGetValue(path, out object b) ? b : "(none)"))
                .Select(path => $"{path}: saved {Show(expectedLeaves, path)}, came back {Show(loadedLeaves, path)}")
                .ToList();
            Assert.That(lost, Is.Empty, "fields the game does not carry through a reload");
            Assert.That(loaded.stats.playSeconds, Is.EqualTo(expected.stats.playSeconds).Within(5.0), "play time");
        }

        /// <summary>A save at garden 3 with every field set to a value a real player could have.</summary>
        SaveData RichSave()
        {
            double now = GameClock.DeviceUtc;
            int tiers = game.Bees.TierCounts.Count;
            IReadOnlyList<Flower> flowers = game.Flowers.Flowers;
            var data = new SaveData
            {
                honey = BigNumberData.From(BigNumber.Create(1.25, 20)),
                runHoneyEarned = BigNumberData.From(BigNumber.Create(5.5, 20)),
                beesBought = 4,
                speedLevel = 2,
                honeyValueLevel = 3,
                beesPerTier = Enumerable.Range(0, tiers).Select(t => Math.Max(1, 5 - 2 * t)).ToArray(),
                slotBloom = flowers.Select((_, i) => i % 2 == 0 ? 0.25f : 0.75f).ToArray(),
                slotActive = flowers.Select(_ => true).ToArray(),
                slotNectar = flowers.Select(f => Math.Round(f.Type.MaxNectar * 0.5, 2)).ToArray(),
                lifetimeHoneyEarned = BigNumberData.From(BigNumber.Create(9.75, 21)),
                royalJelly = BigNumberData.From(BigNumber.FromDouble(120)),
                lifetimeJelly = BigNumberData.From(BigNumber.FromDouble(4000)),
                gardenIndex = 2,
                movesMade = 2,
                abilities = game.Queen.Abilities.Select(a => new AbilityLevel { id = a.Id, level = Math.Min(2, a.MaxLevel) }).ToArray(),
                rewardedBoostEndUtc = Math.Round(now + 60),
                dewBoosts = new[] { new DewBoostEntry(2f, 600), new DewBoostEntry(1.5f, 300) },
                dewBoostAnchorUtc = Math.Round(now - 5),
                lastFullScreenAdUtc = Math.Round(now - 500),
                entitlements = new Entitlements
                {
                    removeAds = true, permanentHoney2x = true, starterPackBought = true,
                    grantedOrders = new List<string> { "GPA.0001", "GPA.0002" },
                },
                stats = new LifetimeStats
                {
                    offlineHoney = BigNumberData.From(BigNumber.Create(7, 12)),
                    flowersBloomed = 40, gardensCompleted = 2, beesEvolved = 6, playSeconds = 5000, flowersShaken = 90,
                },
                goalIndex = game.Goals.Count + 2,
                goalBase = 7,
                dewdrops = 345,
                dewEarned = 900,
                welcomeGiftGiven = true,
                daily = new DailyStreak { firstDay = 20000, lastClaimDay = 20003, streak = 3 },
                pouch = new DewPouch { day = 20003, today = 1, since = 2 },
                rareBees = game.RareBees.Bees.Select((b, i) => new AbilityLevel { id = b.Id, level = Math.Min(1 + i % 3, b.MaxLevel) }).ToArray(),
                settings = new PlayerSettings { musicOn = false, soundOn = false, vibration = VibrationMode.Important },
            };
            // No clock reading: the reload pays nothing for the time between, so honey stays put.
            data.clock = default;
            return data;
        }

        IEnumerator MoveQueen()
        {
            game.DebugCompleteGarden();
            game.DebugGrantHoney(game.Prestige.MoveCost * BigNumber.FromDouble(2));
            Assert.That(game.TryMoveQueen(), "Queen move");
            yield return null;
        }

        /// <summary>
        /// Values no build writes (negative balances, levels past their maximum, unknown ids, a
        /// garden far past the last one, a clock in the future) must load, play and save.
        /// </summary>
        [UnityTest]
        public IEnumerator OutOfRangeSave_LoadsAndPlays()
        {
            double now = GameClock.DeviceUtc;
            var data = new SaveData
            {
                honey = new BigNumberData { m = -5, e = 3 },
                runHoneyEarned = new BigNumberData { m = 3, e = -40 },
                beesBought = -10,
                speedLevel = 9999,
                honeyValueLevel = -4,
                beesPerTier = new[] { -3, 999, 50, 7, 8, 9 },
                slotBloom = new[] { -1f, 5f, 0.5f },
                slotActive = new[] { false },
                slotNectar = new[] { -100.0, 1e9, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 },
                lifetimeHoneyEarned = new BigNumberData { m = 1, e = -1 },
                royalJelly = new BigNumberData { m = -2, e = 5 },
                lifetimeJelly = new BigNumberData { m = 1, e = 2 },
                gardenIndex = 999,
                movesMade = -2,
                abilities = new[]
                {
                    new AbilityLevel { id = "NoSuchAbility", level = 3 }, new AbilityLevel { id = QueenAbilityIdsFirst(), level = 9999 },
                    new AbilityLevel { id = null, level = 1 }, new AbilityLevel { id = QueenAbilityIdsFirst(), level = -5 },
                },
                rewardedBoostEndUtc = 1e300,
                dewBoosts = new[] { new DewBoostEntry(-2f, 1e300), new DewBoostEntry(0f, -50), new DewBoostEntry(1e30f, 10) },
                dewBoostAnchorUtc = now + 1e6,
                lastFullScreenAdUtc = now + 1e9,
                entitlements = new Entitlements { grantedOrders = Enumerable.Range(0, 100).Select(i => i % 7 == 0 ? null : "GPA." + i).ToList() },
                stats = new LifetimeStats
                {
                    offlineHoney = new BigNumberData { m = -1, e = 9 }, flowersBloomed = -5, gardensCompleted = -1,
                    beesEvolved = long.MaxValue, playSeconds = -100, flowersShaken = -3,
                },
                goalIndex = 99999,
                goalBase = -7,
                dewdrops = -50,
                dewEarned = -1,
                daily = new DailyStreak { firstDay = -5, lastClaimDay = 1_000_000_000_000, streak = 99 },
                pouch = new DewPouch { day = -1, today = -3, since = 99999 },
                rareBees = new[]
                {
                    new AbilityLevel { id = "Dragonfly", level = 2 }, new AbilityLevel { id = null, level = 4 },
                    new AbilityLevel { id = game.RareBees.Bees[0].Id, level = 9999 }, new AbilityLevel { id = game.RareBees.Bees[1].Id, level = -1 },
                },
                settings = new PlayerSettings { musicOn = true, soundOn = true, vibration = (VibrationMode)42 },
                offlineRate = new BigNumberData { m = -3, e = 4 },
                clock = new ClockStamp
                {
                    lastSeenUtc = now - 3600, lastDeviceUtc = now - 3600, lastMonotonicSeconds = -50, lastBootUtc = now - 3550,
                },
            };
            yield return StopGame();
            Assert.That(TestSave.Files().Save(data));

            yield return LoadMain();
            CheckInvariants(game);
            Assert.That(game.Prestige.GardenIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(game.Bees.Count, Is.LessThanOrEqualTo(MaxBees(game)));
            yield return Monkey(new Random(7), 400, false);
            game.SaveNow();
            Assert.That(TestSave.Files().Load(), Is.Not.Null, "the game saves again");
        }

        string QueenAbilityIdsFirst() => game.Queen.Abilities[0].Id;

        /// <summary>
        /// Hundreds of Queen moves past the last garden: the loop bonus grows the flower values past
        /// what a float holds, and honey must still be finite and keep coming.
        /// </summary>
        [UnityTest]
        public IEnumerator FarPastTheLastGarden_HoneyStaysFinite()
        {
            var data = new SaveData
            {
                gardenIndex = 600, movesMade = 600,
                beesPerTier = new[] { 12 },
                royalJelly = BigNumberData.From(BigNumber.Create(1, 60)), lifetimeJelly = BigNumberData.From(BigNumber.Create(1, 60)),
                welcomeGiftGiven = true,
            };
            yield return StopGame();
            Assert.That(TestSave.Files().Save(data));
            yield return LoadMain();
            Assert.That(game.Prestige.GardenIndex, Is.EqualTo(600));
            foreach (Flower flower in game.Flowers.Flowers)
                Assert.That(double.IsInfinity(flower.Value) || double.IsNaN(flower.Value), Is.False, "flower value");

            game.ShakeAllFlowers();
            Time.timeScale = 8f;
            float end = Time.time + 20f;
            while (Time.time < end)
                yield return null;
            CheckInvariants(game);
            Assert.That(game.Economy.Honey.IsZero, Is.False, "honey comes in");
            game.SaveNow();
            Assert.That(TestSave.Files().Load().offlineRate.ToBigNumber().IsZero, Is.False, "offline rate");
        }

        // --- leaks ---

        /// <summary>
        /// Queen moves rebuild the garden (decor meshes, tinted materials, the ground texture) and
        /// panels open and close: once the last garden repeats, the same garden again must leave
        /// exactly as many objects behind as the time before.
        /// </summary>
        [UnityTest]
        public IEnumerator RepeatedMovesAndPanels_LeaveNothingBehind()
        {
            // Past the authored gardens, so every move from here spawns the same one.
            for (int i = 0; i < 4; i++)
                yield return MoveQueen();
            yield return OpenAndCloseEveryPanel(3);
            yield return Settle();
            Dictionary<string, int> before = CountObjects();
            long memoryBefore = GC.GetTotalMemory(true);

            for (int i = 0; i < 8; i++)
                yield return MoveQueen();
            yield return OpenAndCloseEveryPanel(30);
            yield return Settle();
            Dictionary<string, int> after = CountObjects();
            long memoryAfter = GC.GetTotalMemory(true);

            List<string> grown = after.Where(p => p.Value > before[p.Key]).Select(p => $"{p.Key}: {before[p.Key]} -> {p.Value}").ToList();
            TestContext.Out.WriteLine(string.Join(", ", after.Select(p => $"{p.Key} {before[p.Key]}->{p.Value}")) +
                $", managed {memoryBefore / 1024} KB -> {memoryAfter / 1024} KB");
            Assert.That(grown, Is.Empty, "objects left behind by 8 Queen moves and 30 rounds of panels");
            Assert.That(memoryAfter - memoryBefore, Is.LessThan(2 * 1024 * 1024), "managed memory growth");
        }

        static Dictionary<string, int> CountObjects() => new Dictionary<string, int>
        {
            ["GameObject"] = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
            ["Mesh"] = Resources.FindObjectsOfTypeAll<Mesh>().Length,
            ["Material"] = Resources.FindObjectsOfTypeAll<Material>().Length,
            ["Texture2D"] = Resources.FindObjectsOfTypeAll<Texture2D>().Length,
        };

        /// <summary>Destroy runs at the end of the frame; effects and pops finish within a few seconds.</summary>
        static IEnumerator Settle()
        {
            float end = Time.realtimeSinceStartup + 4f;
            while (Time.realtimeSinceStartup < end)
                yield return null;
        }

        IEnumerator OpenAndCloseEveryPanel(int rounds)
        {
            var back = Object.FindAnyObjectByType<BackButtonHandler>();
            string[] openers = { "QueenButton", "ShopButton", "SettingsButton", "HiveButton" };
            for (int r = 0; r < rounds; r++)
            {
                foreach (string opener in openers)
                {
                    Button button = TestUi.Find(opener);
                    if (button == null)
                        continue;
                    button.onClick.Invoke();
                    yield return null;
                    while (back.IsModalOpen)
                        back.HandleBack();
                    yield return null;
                }
            }
        }

        // --- random taps ---

        /// <summary>
        /// A new player who taps and swipes anywhere: 3000 steps at 20x speed with only what the
        /// game offers on its own.
        /// </summary>
        [UnityTest]
        public IEnumerator RandomTaps_FromAFreshStart()
        {
            Time.timeScale = 20f;
            yield return Monkey(new Random(99), 3000, false);
            TestContext.Out.WriteLine($"reached: garden {game.GardenNumber}, {game.Bees.Count} bees, " +
                $"{NumberFormat.Abbreviate(game.Economy.LifetimeHoneyEarned)} honey earned, {game.Dew.Balance} Dewdrops, goal {game.Goals.Index}");
        }

        /// <summary>
        /// Late game: honey, Dewdrops and Royal Jelly to spend, two Queen moves behind (Hive and Boosts
        /// open), the daily streak running, the butterfly flying often. 2500 steps.
        /// </summary>
        [UnityTest]
        public IEnumerator RandomTaps_WithEverythingOpen()
        {
            yield return MoveQueen();
            yield return MoveQueen();
            game.DebugGrantHoney(BigNumber.Create(1, 40));
            game.DebugGrantDew(20000);
            game.DebugTrustDeviceClock();
            game.DebugShiftDays(2);
            yield return null;
            Time.timeScale = 4f;
            yield return Monkey(new Random(12345), 2500, true);
            TestContext.Out.WriteLine($"reached: garden {game.GardenNumber}, Queen level {game.Queen.Level}, " +
                $"rare bees {Enumerable.Range(0, game.RareBees.Count).Count(game.RareBees.IsOwned)}, streak {game.Daily.streak}");
        }

        /// <param name="lateGame">Also sends the butterfly and moves the date now and then.</param>
        IEnumerator Monkey(Random random, int steps, bool lateGame)
        {
            var pressed = new Dictionary<string, int>();
            int swipes = 0, taps = 0, backs = 0, catches = 0;
            var back = Object.FindAnyObjectByType<BackButtonHandler>();
            var catcher = Object.FindAnyObjectByType<SwipeCatcher>();
            for (int i = 0; i < steps; i++)
            {
                double roll = random.NextDouble();
                if (roll < 0.4)
                {
                    Button button = PickButton(random);
                    if (button != null)
                    {
                        pressed[button.name] = pressed.TryGetValue(button.name, out int n) ? n + 1 : 1;
                        TestUi.Tap(TestUi.Center((RectTransform)button.transform));
                    }
                }
                else if (roll < 0.55)
                {
                    Vector2 point = RandomPoint(random);
                    GameObject hit = TestUi.TopHit(point);
                    if (hit != null && !Skipped(hit.transform))
                    {
                        TestUi.Tap(point);
                        taps++;
                    }
                }
                else if (roll < 0.85)
                {
                    if (Swipe(random, catcher))
                        swipes++;
                }
                else if (roll < 0.9)
                {
                    // The Android back button, unless it would only open the quit dialog's Quit.
                    back.HandleBack();
                    backs++;
                }
                else if (roll < 0.95)
                {
                    // A butterfly in flight is worth a tap on it.
                    if (game.Visitors.IsFlying && !back.IsModalOpen)
                    {
                        Vector2 at = Camera.main.WorldToScreenPoint(game.Visitors.Position);
                        GameObject hit = TestUi.TopHit(at);
                        if (hit != null && hit.GetComponent<SwipeCatcher>() != null)
                        {
                            catcher.OnPointerDown(new PointerEventData(EventSystem.current) { pointerId = 0, position = at });
                            catcher.OnPointerUp(new PointerEventData(EventSystem.current) { pointerId = 0, position = at });
                            catches++;
                        }
                    }
                    else if (lateGame && random.NextDouble() < 0.3)
                    {
                        game.DebugLaunchButterfly(random.NextDouble() < 0.3 ? true : (bool?)null);
                    }
                }
                else
                {
                    if (lateGame && random.NextDouble() < 0.05)
                        game.DebugShiftDays(1);
                    // Time passes: bees fly, flowers regrow, offers and confirmations run out.
                    int frames = random.Next(1, 15);
                    for (int f = 0; f < frames; f++)
                        yield return null;
                }
                yield return null;
                if (i % 50 == 0)
                    CheckInvariants(game);
            }
            CheckInvariants(game);
            TestContext.Out.WriteLine($"{swipes} swipes, {taps} taps, {backs} back presses, {catches} butterfly taps, " +
                $"{pressed.Count} distinct buttons: " + string.Join(", ", pressed.OrderByDescending(p => p.Value).Select(p => $"{p.Key} {p.Value}")));
        }

        static Vector2 RandomPoint(Random random) =>
            new Vector2((float)random.NextDouble() * Screen.width, (float)random.NextDouble() * Screen.height);

        static bool Skipped(Transform hit)
        {
            for (Transform t = hit; t != null; t = t.parent)
            {
                if (MonkeySkip.Contains(t.name))
                    return true;
            }
            return false;
        }

        /// <summary>Any button a finger could press now; mostly not one that closes, so the taps get inside panels.</summary>
        static Button PickButton(Random random)
        {
            List<Button> buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.InstanceID)
                .Where(b => b.isActiveAndEnabled && b.interactable && !MonkeySkip.Contains(b.name) && TestUi.Reachable(b))
                .ToList();
            if (buttons.Count == 0)
                return null;
            List<Button> stay = buttons.Where(b => b.name != "CloseButton" && b.name != "CancelButton").ToList();
            if (stay.Count > 0 && random.NextDouble() < 0.85)
                buttons = stay;
            return buttons[random.Next(buttons.Count)];
        }

        /// <summary>A stroke of a few moves between two random points, when the first lands on the world.</summary>
        static bool Swipe(Random random, SwipeCatcher catcher)
        {
            Vector2 from = RandomPoint(random);
            GameObject hit = TestUi.TopHit(from);
            if (hit == null || hit.GetComponent<SwipeCatcher>() == null)
                return false;
            Vector2 to = RandomPoint(random);
            var pointer = new PointerEventData(EventSystem.current) { pointerId = 0, position = from };
            catcher.OnPointerDown(pointer);
            int moves = random.Next(1, 6);
            for (int m = 1; m <= moves; m++)
            {
                pointer.position = Vector2.Lerp(from, to, m / (float)moves);
                catcher.OnDrag(pointer);
            }
            catcher.OnPointerUp(pointer);
            return true;
        }

        static int MaxBees(GameManager game) =>
            ((Buzzfield.Bees.BeeSettings)typeof(GameManager).GetField("beeSettings", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(game)).MaxBees;

        static void Finite(BigNumber value, string what)
        {
            Assert.That(double.IsNaN(value.Mantissa) || double.IsInfinity(value.Mantissa), Is.False, $"{what} {value} is not finite");
            Assert.That(value.IsNegative, Is.False, $"{what} {value} is negative");
        }

        internal static void CheckInvariants(GameManager game)
        {
            Finite(game.Economy.Honey, "honey");
            Finite(game.Economy.RunHoneyEarned, "run honey");
            Finite(game.Economy.LifetimeHoneyEarned, "lifetime honey");
            Finite(game.Economy.HoneyPerSecond(Time.timeAsDouble), "honey per second");
            Finite(game.Economy.TotalMultiplier, "honey multiplier");
            Finite(game.Prestige.RoyalJelly, "Royal Jelly");
            Finite(game.Prestige.MoveCost, "move cost");
            double boost = game.Economy.BoostMultiplier;
            Assert.That(boost >= 1.0 && !double.IsInfinity(boost), $"boost multiplier {boost}");
            Assert.That(game.Dew.Balance, Is.GreaterThanOrEqualTo(0), "Dewdrops");
            Assert.That(game.Dew.TotalEarned, Is.GreaterThanOrEqualTo(game.Dew.Balance), "Dewdrops earned");
            Assert.That(game.Bees.Count, Is.InRange(1, MaxBees(game)), "bees");
            Assert.That(game.Goals.Index, Is.GreaterThanOrEqualTo(0), "goal");
            foreach (Flower flower in game.Flowers.Flowers)
            {
                Assert.That(flower.Bloom, Is.InRange(0f, 1f), "bloom");
                Assert.That(flower.Nectar, Is.InRange(0.0, (double)flower.Type.MaxNectar), "nectar");
            }
            for (int i = 0; i < game.RareBees.Count; i++)
                Assert.That(game.RareBees.Level(i), Is.InRange(0, game.RareBees.Bees[i].MaxLevel), "rare bee level");
            for (int i = 0; i < game.Queen.AbilityCount; i++)
                Assert.That(game.Queen.AbilityLevel(i), Is.InRange(0, game.Queen.Abilities[i].MaxLevel), "ability level");

            // One modal at a time: a panel that opens by itself waits for the screen to be free.
            var open = new List<string>();
            if (Object.FindAnyObjectByType<QueenPanelView>().IsOpen) open.Add("Queen");
            if (Object.FindAnyObjectByType<ShopPanelView>().IsOpen) open.Add("Shop");
            if (Object.FindAnyObjectByType<SettingsPanelView>().IsOpen) open.Add("Settings");
            if (Object.FindAnyObjectByType<DailyPanelView>().IsOpen) open.Add("Daily");
            if (Object.FindAnyObjectByType<HivePanelView>().IsOpen) open.Add("Hive");
            if (Object.FindAnyObjectByType<WelcomeBackView>().IsOpen) open.Add("WelcomeBack");
            Assert.That(open.Count, Is.LessThanOrEqualTo(1), "panels open together: " + string.Join(", ", open));
        }

        // --- save comparison ---

        /// <summary>Every value in the object tree by path ("abilities[1].level"); lists also give their Count.</summary>
        static Dictionary<string, object> Leaves(object root)
        {
            var leaves = new Dictionary<string, object>();
            Collect(root, "", leaves);
            return leaves;
        }

        static void Collect(object value, string path, Dictionary<string, object> leaves)
        {
            if (value == null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum)
            {
                leaves[path] = value;
                return;
            }
            if (value is IList list)
            {
                leaves[path + ".Count"] = list.Count;
                for (int i = 0; i < list.Count; i++)
                    Collect(list[i], $"{path}[{i}]", leaves);
                return;
            }
            foreach (FieldInfo field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                Collect(field.GetValue(value), path.Length == 0 ? field.Name : path + "." + field.Name, leaves);
        }

        /// <summary>A path without list indices and counts: "abilities[1].level" and "abilities.Count" are "abilities.level" and "abilities".</summary>
        static string Group(string path) =>
            System.Text.RegularExpressions.Regex.Replace(path, @"\[\d+\]", "").Replace(".Count", "");

        static IEnumerable<string> Groups(IEnumerable<string> paths) =>
            paths.Select(Group).Where(g => !paths.Any(p => Group(p) != g && Group(p).StartsWith(g + "."))).Distinct();

        /// <summary>True when no value in the group differs between the two saves.</summary>
        static bool SameGroup(string group, Dictionary<string, object> a, Dictionary<string, object> b)
        {
            List<string> paths = a.Keys.Concat(b.Keys).Where(p => Group(p) == group).Distinct().ToList();
            return paths.All(p => a.ContainsKey(p) && b.ContainsKey(p) && Same(a[p], b[p]));
        }

        static bool Same(object a, object b)
        {
            if (a is double x && b is double y)
                return Math.Abs(x - y) <= 1e-6 * Math.Max(1.0, Math.Abs(x));
            if (a is float f && b is float g)
                return Math.Abs(f - g) <= 1e-5f * Math.Max(1f, Math.Abs(f));
            return Equals(a, b);
        }

        static string Show(Dictionary<string, object> leaves, string path) =>
            leaves.TryGetValue(path, out object value) ? value?.ToString() ?? "null" : "(missing)";
    }
}
