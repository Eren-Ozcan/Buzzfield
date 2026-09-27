using System.Collections;
using System.Reflection;
using Buzzfield.Bees;
using Buzzfield.Core;
using Buzzfield.Game;
using Buzzfield.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// The manual play checklist, automated: pacing of the first bee, visible speed-up,
    /// Forager look, button states and rapid Evolve presses.
    /// </summary>
    public class PlaytestChecklistTests
    {
        const BindingFlags NonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

        GameManager game;
        BeeSettings beeSettings;
        int originalMaxBees = -1;

        [UnitySetUp]
        public IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            Assert.That(game, Is.Not.Null);
            beeSettings = Hidden<BeeSettings>(game, "beeSettings");
        }

        [UnityTearDown]
        public IEnumerator ResetState()
        {
            Time.timeScale = 1f;
            // BeeSettings is an asset; never leave a test value in it.
            if (originalMaxBees >= 0)
                SetHidden(beeSettings, "maxBees", originalMaxBees);
            originalMaxBees = -1;
            yield return null;
        }

        [UnityTest]
        public IEnumerator FirstAddBee_AffordableWithinFifteenSeconds()
        {
            Time.timeScale = 4f;
            float start = Time.time;
            float end = start + 30f;
            while (!game.Upgrades.CanAddBee && Time.time < end)
                yield return null;

            float elapsed = Time.time - start;
            TestContext.WriteLine($"First Add Bee affordable after {elapsed:F1} s (cost {NumberFormat.Abbreviate(game.Upgrades.AddBeeCost)}).");
            Assert.That(game.Upgrades.CanAddBee, "First Add Bee never became affordable.");
            Assert.That(elapsed, Is.LessThanOrEqualTo(15f));

            yield return null;
            Assert.That(ButtonOf("addBeeButton").interactable, Is.True, "Add Bee button stays grey.");
        }

        [UnityTest]
        public IEnumerator Speed_BeesFlyFaster()
        {
            game.Bees.Spawn(0);
            game.Bees.Spawn(0);

            float baseline = 0f;
            yield return MeasureTopSpeed(v => baseline = v);

            game.Economy.Grant(BigNumber.Create(1, 9));
            for (int i = 0; i < 10; i++)
                Assert.That(game.Upgrades.TryBuySpeed(), Is.True);

            float boosted = 0f;
            yield return MeasureTopSpeed(v => boosted = v);

            float expected = game.Upgrades.SpeedMultiplier;
            TestContext.WriteLine($"Top speed {baseline:F2} -> {boosted:F2} u/s, ratio {boosted / baseline:F2}, expected {expected:F2}.");
            Assert.That(baseline, Is.EqualTo(beeSettings.Tiers[0].Speed).Within(0.1f));
            Assert.That(boosted / baseline, Is.EqualTo(expected).Within(0.1f));
        }

        [UnityTest]
        public IEnumerator Evolve_ButtonLightsUp_AndForagerIsBiggerAndOrange()
        {
            game.Economy.Grant(BigNumber.Create(1, 6));
            game.Bees.Spawn(0);
            yield return null;
            Assert.That(ButtonOf("evolveButton").interactable, Is.False, "Evolve lit with two Workers.");

            game.Bees.Spawn(0);
            yield return null;
            Assert.That(ButtonOf("evolveButton").interactable, Is.True, "Evolve stays grey with three Workers.");

            Bee evolved = null;
            game.Bees.OnBeeEvolved += bee => evolved = bee;
            ButtonOf("evolveButton").onClick.Invoke();

            Time.timeScale = 4f;
            float end = Time.time + 20f;
            while (evolved == null && Time.time < end)
                yield return null;
            Assert.That(evolved, Is.Not.Null, "Merge never finished.");
            Assert.That(evolved.TierIndex, Is.EqualTo(1));

            GameObject forager = Hidden<GameObject>(evolved, "Instance");
            Assert.That(forager.transform.localScale.x, Is.GreaterThan(beeSettings.Tiers[0].Scale));

            bool orange = false;
            foreach (Renderer renderer in forager.GetComponentsInChildren<Renderer>())
            {
                Color c = renderer.sharedMaterial.GetColor("_BaseColor");
                orange |= c.r > 0.9f && c.g > 0.4f && c.g < 0.7f && c.b < 0.3f;
            }
            Assert.That(orange, "Forager has no orange body.");

            yield return null;
            Assert.That(TextOf("evolveButton", "detailText"), Does.Contain("F1"));
        }

        [UnityTest]
        public IEnumerator AddBee_ShowsMaxAtLoweredCap()
        {
            originalMaxBees = beeSettings.MaxBees;
            SetHidden(beeSettings, "maxBees", 5);

            game.Economy.Grant(BigNumber.Create(1, 9));
            int bought = 0;
            while (game.Upgrades.TryAddBee())
                bought++;
            yield return null;

            Assert.That(game.Bees.Count, Is.EqualTo(5));
            Assert.That(bought, Is.EqualTo(4));
            Assert.That(TextOf("addBeeButton", "costText"), Is.EqualTo(Strings.Max));
            Assert.That(TextOf("addBeeButton", "detailText"), Is.EqualTo("5/5 bees"));
            Assert.That(ButtonOf("addBeeButton").interactable, Is.False);
        }

        [UnityTest]
        public IEnumerator Evolve_PressedTwice_SecondMergeNeedsThreeMoreWorkers()
        {
            for (int i = 0; i < 5; i++)
                game.Bees.Spawn(0);
            game.Economy.Grant(BigNumber.Create(1, 6));

            int evolvedCount = 0;
            game.Bees.OnBeeEvolved += _ => evolvedCount++;
            Button evolve = ButtonOf("evolveButton");

            evolve.onClick.Invoke();
            evolve.onClick.Invoke();
            evolve.onClick.Invoke();
            Assert.That(game.Bees.TierCounts[0], Is.EqualTo(0), "Six Workers should start exactly two merges.");

            Time.timeScale = 4f;
            float end = Time.time + 20f;
            while ((evolvedCount < 2 || game.Bees.IsMerging) && Time.time < end)
                yield return null;

            Assert.That(evolvedCount, Is.EqualTo(2));
            Assert.That(game.Bees.TierCounts[1], Is.EqualTo(2));
            Assert.That(game.Bees.Count, Is.EqualTo(2));
        }

        /// <summary>Highest per-frame flight speed over a few seconds of cruising bees.</summary>
        IEnumerator MeasureTopSpeed(System.Action<float> result)
        {
            FieldInfo positionField = typeof(Bee).GetField("Position", NonPublic);
            int count = game.Bees.Count;
            var last = new Vector3[count];
            var lastState = new BeeState[count];
            float top = 0f;
            float end = Time.time + 6f;
            bool first = true;
            while (Time.time < end)
            {
                for (int i = 0; i < count; i++)
                {
                    Bee bee = game.Bees.Bees[i];
                    var position = (Vector3)positionField.GetValue(bee);
                    bool cruising = bee.State == BeeState.FlyToFlower || bee.State == BeeState.ReturnToHive;
                    if (!first && cruising && lastState[i] == bee.State && Time.deltaTime > 0f)
                        top = Mathf.Max(top, Vector3.Distance(position, last[i]) / Time.deltaTime);
                    last[i] = position;
                    lastState[i] = bee.State;
                }
                first = false;
                yield return null;
            }
            result(top);
        }

        Button ButtonOf(string field) => Hidden<Button>(ButtonView(field), "button");

        string TextOf(string field, string textField) => Hidden<TMP_Text>(ButtonView(field), textField).text;

        UpgradeButtonView ButtonView(string field) =>
            Hidden<UpgradeButtonView>(Hidden<BottomBarView>(game, "bottomBar"), field);

        static T Hidden<T>(object owner, string name)
        {
            System.Type type = owner.GetType();
            FieldInfo field = type.GetField(name, NonPublic);
            if (field != null)
                return (T)field.GetValue(owner);
            PropertyInfo property = type.GetProperty(name, NonPublic);
            Assert.That(property, Is.Not.Null, $"{type.Name} has no member {name}.");
            return (T)property.GetValue(owner);
        }

        static void SetHidden(object owner, string name, object value) =>
            owner.GetType().GetField(name, NonPublic).SetValue(owner, value);
    }
}
