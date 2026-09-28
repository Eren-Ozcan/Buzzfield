using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Buzzfield.Core;
using Buzzfield.Game;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// Full swarm (the bee cap) in the real Main scene. After a warm-up, one test checks that the
    /// game's own per-frame code allocates nothing; the other logs game tick CPU time, heap
    /// growth and render batches (batches read 0 in batchmode, which does not render). Steady
    /// state means bees flying and depositing with no player input. Set BZ_PERF_OUT to a file
    /// path to also write the report there.
    /// </summary>
    public class PerformanceTests
    {
        const float WarmupSeconds = 4f;
        const int MeasuredFrames = 300;
        const int BaselineFrames = 120;

        GameManager game;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            TestSave.Clear();
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = UnityEngine.Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
        }

        /// <summary>
        /// Every Update and LateUpdate of the game's own components, called through delegates inside
        /// a GC.Alloc recorder one by one, once per frame for a run of frames with bees depositing.
        /// Each one may allocate on at most one frame (a first call under the JIT); repeats fail.
        /// </summary>
        /// <remarks>
        /// Players only: in the editor TextMeshPro rebuilds an inspector string on every text change,
        /// which a player build leaves out. Run with -testPlatform StandaloneWindows64 (or on a device).
        /// </remarks>
        [UnityTest]
        [UnityPlatform(exclude = new[] { RuntimePlatform.WindowsEditor, RuntimePlatform.OSXEditor, RuntimePlatform.LinuxEditor })]
        public IEnumerator FullSwarm_GameScripts_AllocateNothingPerFrame()
        {
            FillSwarm();
            yield return new WaitForSecondsRealtime(WarmupSeconds);

            var ticks = new List<TestDelegate>();
            var tickNames = new List<string>();
            var names = new StringBuilder();

            // GameManager.Update is switched off and its body run here step by step, in the same order,
            // so an allocation is pinned on the system that made it and nothing ticks twice.
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var tweener = (Tweener)typeof(GameManager).GetField("tweener", flags).GetValue(game);
            var autosave = (Action<float>)typeof(GameManager).GetMethod("TickAutosave", flags).CreateDelegate(typeof(Action<float>), game);
            AddTick(ticks, tickNames, "BoostManager.Tick", () => game.Boosts.Tick(GameClock.DeviceUtc));
            AddTick(ticks, tickNames, "BoostManager.SpeedMultiplier", () => game.Bees.BoostSpeedMultiplier = game.Boosts.SpeedMultiplier(Time.timeAsDouble));
            AddTick(ticks, tickNames, "FlowerManager.Tick", () => game.Flowers.Tick(Time.deltaTime));
            AddTick(ticks, tickNames, "BeeManager.Tick", () => game.Bees.Tick(Time.deltaTime));
            AddTick(ticks, tickNames, "GardenBloomManager.Tick", () => game.Bloom.Tick(Time.deltaTime));
            AddTick(ticks, tickNames, "AdManager.Tick", () => game.Ads.Tick(Time.unscaledDeltaTime));
            AddTick(ticks, tickNames, "StoreManager.Tick", () => game.Store.Tick(Time.unscaledDeltaTime));
            AddTick(ticks, tickNames, "GameManager.TickAutosave", () => autosave(Time.unscaledDeltaTime));
            AddTick(ticks, tickNames, "Tweener.Tick", () => tweener.Tick(Time.unscaledDeltaTime));
            game.enabled = false;

            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                Type type = behaviour.GetType();
                if (!behaviour.isActiveAndEnabled || type.Namespace == null
                    || !type.Namespace.StartsWith("Buzzfield.") || type.Namespace.StartsWith("Buzzfield.Tests"))
                    continue;
                foreach (string message in new[] { "Update", "LateUpdate" })
                {
                    MethodInfo method = type.GetMethod(message, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
                    if (method == null || method.ReturnType != typeof(void))
                        continue;
                    ticks.Add((TestDelegate)method.CreateDelegate(typeof(TestDelegate), behaviour));
                    tickNames.Add(type.Name + "." + message);
                    names.Append(type.Name).Append('.').Append(message).Append(' ');
                }
            }
            Assert.That(ticks.Count, Is.GreaterThan(5), names.ToString());
            Debug.Log("[Perf] Allocation check covers: " + names);

            // Same measurement as Is.Not.AllocatingGCMemory(), but every offender over the run is listed.
            Recorder recorder = Recorder.Get("GC.Alloc");
            recorder.FilterToCurrentThread();
            recorder.enabled = false;
            var offenders = new StringBuilder();
            var allocatingFrames = new int[ticks.Count];
            // Autosave (every 30 s, JSON and a file write) is the one planned allocation; keep it out of the window.
            game.SaveNow();
            BigNumber honeyBefore = game.Economy.Honey;
            for (int frame = 0; frame < MeasuredFrames; frame++)
            {
                for (int i = 0; i < ticks.Count; i++)
                {
                    recorder.enabled = true;
                    ticks[i]();
                    recorder.enabled = false;
                    recorder.CollectFromAllThreads();
                    if (recorder.sampleBlockCount == 0)
                        continue;
                    allocatingFrames[i]++;
                    offenders.Append(tickNames[i]).Append(" frame ").Append(frame).Append(" (").Append(recorder.sampleBlockCount).Append(" allocs); ");
                }
                yield return null;
            }
            game.enabled = true;
            Debug.Log("[Perf] Allocating ticks: " + (offenders.Length == 0 ? "none" : offenders.ToString()));
            // A method's first call can allocate once under the Mono JIT (the Windows test player);
            // IL2CPP builds do not. Anything that allocates on a second frame is a real per-frame cost.
            for (int i = 0; i < ticks.Count; i++)
                Assert.That(allocatingFrames[i], Is.LessThanOrEqualTo(1), $"{tickNames[i]} allocates repeatedly: {offenders}");
            Assert.That(game.Economy.Honey, Is.GreaterThan(honeyBefore), "Bees should keep depositing while measured.");
        }

        /// <summary>Numbers for the performance log: CPU, allocations and render batches over a steady run. Only checks bees keep working.</summary>
        [UnityTest]
        public IEnumerator FullSwarm_Report()
        {
            FillSwarm();

            var probe = new GameObject("AllocProbe");
            var start = probe.AddComponent<FrameStartProbe>();
            var end = probe.AddComponent<FrameEndProbe>();
            end.StartProbe = start;

            yield return new WaitForSecondsRealtime(WarmupSeconds);

            // The probe reads managed heap growth; make sure that sees an allocation at all.
            long before = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
            byte[] sample = new byte[64 * 1024];
            Assert.That(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() - before, Is.GreaterThanOrEqualTo(sample.Length), "Heap probe does not see allocations.");

            var scriptBytes = new List<long>(MeasuredFrames);
            var frameBytes = new List<long>(MeasuredFrames);
            var tickMs = new List<double>(MeasuredFrames);
            var frameMs = new List<double>(MeasuredFrames);
            var batches = new List<long>(MeasuredFrames);
            var setPass = new List<long>(MeasuredFrames);
            var drawCalls = new List<long>(MeasuredFrames);
            BigNumber honeyBefore = game.Economy.Honey;

            using (var gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
            using (var tickRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, GameManager.TickMarkerName))
            using (var mainRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread"))
            using (var batchRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count"))
            using (var setPassRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count"))
            using (var drawRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
            {
                // Recorders report the previous frame; skip the first one.
                yield return null;
                for (int i = 0; i < MeasuredFrames; i++)
                {
                    yield return null;
                    scriptBytes.Add(end.LastScriptBytes);
                    frameBytes.Add(gcRecorder.LastValue);
                    tickMs.Add(tickRecorder.LastValue / 1e6);
                    frameMs.Add(mainRecorder.LastValue / 1e6);
                    batches.Add(batchRecorder.LastValue);
                    setPass.Add(setPassRecorder.LastValue);
                    drawCalls.Add(drawRecorder.LastValue);
                }
            }
            UnityEngine.Object.Destroy(probe);
            string renderers = CountRenderers();
            BigNumber honeyEarned = game.Economy.Honey - honeyBefore;

            // Baseline: the same frames with the game scene unloaded, so allocations of the test
            // runner and the engine are not mistaken for the game's.
            var baseline = new List<long>(BaselineFrames);
            Scene main = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("PerfBaseline"));
            yield return SceneManager.UnloadSceneAsync(main);
            using (var gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
            {
                yield return null;
                for (int i = 0; i < BaselineFrames; i++)
                {
                    yield return null;
                    baseline.Add(gcRecorder.LastValue);
                }
            }

            string report = new StringBuilder()
                .AppendLine($"Bees: {game.Bees.Count}, frames: {MeasuredFrames}, honey earned while measuring: {NumberFormat.Abbreviate(honeyEarned)}")
                .AppendLine(renderers)
                .AppendLine(Line("Managed heap growth over Update..LateUpdate (4 KB pages, bytes)", scriptBytes))
                .AppendLine(Line("GC Allocated In Frame (profiler, whole frame, bytes)", frameBytes))
                .AppendLine(Line("GC Allocated In Frame with no game scene (baseline, bytes)", baseline))
                .AppendLine(Line("Game tick ms", tickMs))
                .AppendLine(Line("Main thread ms", frameMs))
                .AppendLine(Line("Batches", batches))
                .AppendLine(Line("SetPass calls", setPass))
                .AppendLine(Line("Draw calls", drawCalls))
                .ToString();
            Debug.Log("[Perf] " + report);
            string output = Environment.GetEnvironmentVariable("BZ_PERF_OUT");
            if (!string.IsNullOrEmpty(output))
                File.WriteAllText(output, report);

            Assert.That(game.Economy.Honey, Is.GreaterThan(honeyBefore), "Bees should keep depositing while measured.");
        }

        /// <summary>Active renderers by owner, to see where the draw calls come from.</summary>
        static string CountRenderers()
        {
            int bees = 0, flowers = 0, particles = 0, other = 0;
            foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;
                if (renderer is ParticleSystemRenderer)
                    particles++;
                else if (renderer.GetComponentInParent<Buzzfield.Flowers.FlowerView>() != null)
                    flowers++;
                else if (renderer.transform.root.name == "Systems")
                    bees++;
                else
                    other++;
            }
            return $"Active renderers: bees {bees}, flowers {flowers}, particle systems {particles}, other {other}";
        }

        static void AddTick(List<TestDelegate> ticks, List<string> names, string name, TestDelegate tick)
        {
            ticks.Add(tick);
            names.Add(name);
        }

        /// <summary>
        /// Bees up to the cap, and enough honey that every upgrade is affordable and stays so
        /// while the counter still changes with each deposit (a steady screen, no player input).
        /// </summary>
        void FillSwarm()
        {
            while (game.Bees.Spawn(0) != null) { }
            game.Economy.Grant(BigNumber.Create(9, 2));
            Assert.That(game.Bees.IsAtCap);
        }

        static string Line(string name, List<long> values)
        {
            var sorted = new List<long>(values);
            sorted.Sort();
            long sum = 0;
            int nonZero = 0;
            foreach (long value in values)
            {
                sum += value;
                if (value != 0)
                    nonZero++;
            }
            return $"{name}: median {sorted[sorted.Count / 2]}, p95 {sorted[(int)(sorted.Count * 0.95)]}, max {sorted[sorted.Count - 1]}, avg {sum / (double)values.Count:0.#}, non-zero frames {nonZero}";
        }

        static string Line(string name, List<double> values)
        {
            var sorted = new List<double>(values);
            sorted.Sort();
            double sum = 0;
            foreach (double value in values)
                sum += value;
            return $"{name}: median {sorted[sorted.Count / 2]:0.###}, p95 {sorted[(int)(sorted.Count * 0.95)]:0.###}, max {sorted[sorted.Count - 1]:0.###}, avg {sum / values.Count:0.###}";
        }
    }
}
