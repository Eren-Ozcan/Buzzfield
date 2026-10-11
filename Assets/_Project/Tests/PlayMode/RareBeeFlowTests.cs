using System.Collections;
using System.Linq;
using Buzzfield.Bees;
using Buzzfield.Core;
using Buzzfield.Flowers;
using Buzzfield.Game;
using Buzzfield.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// The rare bees against the real Main scene (docs/DEWDROPS.md 4.2): the hive button and panel
    /// from the first Queen move, the free Bumblebee, Dewdrops spends with the second tap, locked
    /// species, the bees in the garden through Queen moves and a reload, a tap on the hive, and
    /// every bonus reaching the system it changes.
    /// </summary>
    public class RareBeeFlowTests
    {
        GameManager game;
        HivePanelView hive;

        [UnitySetUp]
        public IEnumerator LoadFresh()
        {
            TestSave.Clear();
            yield return LoadMain();
        }

        IEnumerator LoadMain()
        {
            yield return SceneManager.LoadSceneAsync("Main", LoadSceneMode.Single);
            yield return null;
            game = Object.FindAnyObjectByType<GameManager>();
            hive = Object.FindAnyObjectByType<HivePanelView>();
            Assert.That(game, Is.Not.Null);
            Assert.That(hive, Is.Not.Null);
        }

        /// <summary>Completes the garden and moves the Queen to the next one.</summary>
        IEnumerator MoveQueen()
        {
            game.DebugCompleteGarden();
            game.DebugGrantHoney(BigNumber.Create(1, 15));
            Assert.That(game.TryMoveQueen());
            // The views redraw in LateUpdate.
            yield return null;
            yield return null;
        }

        int Index(string id) => game.RareBees.IndexOf(id);

        RareBeeCardView Card(string id) => hive.Cards.First(card => card.BeeIndex == Index(id));

        static IEnumerator Shot(string name)
        {
            yield return null;
            TestShots.SaveIfRequested($"rare_{name}_1080x1920.png", 1080, 1920);
            TestShots.SaveIfRequested($"rare_{name}_1440x1920.png", 1440, 1920);
        }

        [UnityTest]
        public IEnumerator BeforeTheFirstMove_TheHiveStaysShut()
        {
            yield return null;
            Assert.That(hive.IsButtonShown, Is.False);
            Assert.That(game.HiveUnlocked, Is.False);
            Assert.That(game.OpenHive(), Is.False);
            Assert.That(hive.IsOpen, Is.False);
            game.DebugGrantDew(1000);
            Assert.That(game.TryAdvanceRareBee(Index(RareBeeIds.Bumblebee)), Is.False);
            Assert.That(game.TryAdvanceRareBee(Index(RareBeeIds.BlueBandedBee)), Is.False);
            Assert.That(game.Bees.RareBees, Is.Empty);
        }

        [UnityTest]
        public IEnumerator AfterTheFirstMove_TheFreeBumblebeeMovesIn()
        {
            yield return MoveQueen();
            Assert.That(hive.IsButtonShown);
            Assert.That(hive.IsBadgeShown, "The free Bumblebee waits.");
            Assert.That(game.OpenHive());
            yield return null;

            int index = Index(RareBeeIds.Bumblebee);
            RareBeeCardView card = Card(RareBeeIds.Bumblebee);
            Assert.That(card.ShownState, Is.EqualTo(RareBeeCardView.State.Free));
            StringAssert.Contains("A bumblebee wants to move in!", card.Body);
            Assert.That(card.Cost, Is.EqualTo(Strings.RareBeeFree));
            yield return Shot("panel_free");

            long dew = game.Dew.Balance;
            int bees = game.Bees.Count;
            card.Press();
            Assert.That(game.RareBees.Level(index), Is.EqualTo(1));
            Assert.That(game.Dew.Balance, Is.EqualTo(dew), "The first one is free.");
            Assert.That(game.Bees.RareBees.Count, Is.EqualTo(1));
            Assert.That(game.Bees.Count, Is.EqualTo(bees), "Rare bees count toward neither the bees nor the cap.");
            Assert.That(game.Bees.CapacityMultiplier, Is.EqualTo(1.03f).Within(1e-5));
            Assert.That(hive.Status, Is.EqualTo(string.Format(Strings.RareBeeJoinedFormat, Strings.RareBumblebee)));

            yield return null;
            Assert.That(card.ShownState, Is.EqualTo(RareBeeCardView.State.TooExpensive), "Level 2 costs 20 Dewdrops.");
            Assert.That(hive.IsBadgeShown, Is.EqualTo(game.RareBees.AnyAffordable(game.GardenNumber, game.Dew.Balance)));
            Assert.That(game.TryAdvanceRareBee(index), Is.EqualTo(game.Dew.Balance >= 20));

            SaveData saved = TestSave.Files().Load();
            Assert.That(saved.rareBees.Any(level => level.id == RareBeeIds.Bumblebee && level.level >= 1), "The new bee is saved at once.");
        }

        [UnityTest]
        public IEnumerator LockedSpecies_ShowTheGardenTheyWaitFor()
        {
            yield return MoveQueen();
            game.DebugGrantDew(5000);
            Assert.That(game.OpenHive());
            yield return null;

            Assert.That(Card(RareBeeIds.MasonBee).ShownState, Is.EqualTo(RareBeeCardView.State.Locked));
            Assert.That(Card(RareBeeIds.MasonBee).Cost, Is.EqualTo(string.Format(Strings.RareBeeLockedFormat, 3)));
            Assert.That(Card(RareBeeIds.CarpenterBee).Cost, Is.EqualTo(string.Format(Strings.RareBeeLockedFormat, 5)));
            Assert.That(Card(RareBeeIds.BlueBandedBee).ShownState, Is.EqualTo(RareBeeCardView.State.Buyable));
            long balance = game.Dew.Balance;
            Assert.That(game.TryAdvanceRareBee(Index(RareBeeIds.MasonBee)), Is.False);
            Assert.That(game.RareBees.Level(Index(RareBeeIds.MasonBee)), Is.Zero);
            Assert.That(game.Dew.Balance, Is.EqualTo(balance), "Nothing was spent on a locked species.");
            yield return Shot("panel_locked");
        }

        [UnityTest]
        public IEnumerator BigSpend_NeedsASecondTap_ThenTheBeeJoins()
        {
            yield return MoveQueen();
            long start = game.Dew.Balance;
            game.DebugGrantDew(120);
            Assert.That(game.OpenHive());
            yield return null;

            int index = Index(RareBeeIds.BlueBandedBee);
            RareBeeCardView card = Card(RareBeeIds.BlueBandedBee);
            Assert.That(card.ShownState, Is.EqualTo(RareBeeCardView.State.Buyable));
            Assert.That(card.Cost, Is.EqualTo("120"));

            card.Press();
            Assert.That(hive.ConfirmingIndex, Is.EqualTo(index));
            Assert.That(card.ShownState, Is.EqualTo(RareBeeCardView.State.Confirming));
            Assert.That(card.Cost, Is.EqualTo(Strings.ConfirmSpend));
            Assert.That(game.RareBees.Level(index), Is.Zero, "The first tap only asks.");
            yield return Shot("panel_confirm");

            card.Press();
            Assert.That(game.RareBees.Level(index), Is.EqualTo(1));
            Assert.That(game.Dew.Balance, Is.EqualTo(start));
            Assert.That(hive.ConfirmingIndex, Is.EqualTo(-1));
            Assert.That(game.Bees.RareBees.Count, Is.EqualTo(1));
            Assert.That(TestSave.Files().Load().dewdrops, Is.EqualTo(start), "The spend is saved with the bee.");
        }

        [UnityTest]
        public IEnumerator SecondTap_RunsOut_AfterAFewSeconds()
        {
            yield return MoveQueen();
            game.DebugGrantDew(500);
            Assert.That(game.OpenHive());
            yield return null;
            Card(RareBeeIds.BlueBandedBee).Press();
            Assert.That(hive.ConfirmingIndex, Is.EqualTo(Index(RareBeeIds.BlueBandedBee)));
            yield return new WaitForSecondsRealtime(game.DewSettings.ConfirmSeconds + 0.3f);
            Assert.That(hive.ConfirmingIndex, Is.EqualTo(-1));
            Assert.That(Card(RareBeeIds.BlueBandedBee).ShownState, Is.EqualTo(RareBeeCardView.State.Buyable));
        }

        [UnityTest]
        public IEnumerator SmallSpend_LevelsUpAtOnce_AndShortDewdropsAreRefused()
        {
            yield return MoveQueen();
            int index = Index(RareBeeIds.Bumblebee);
            Assert.That(game.TryAdvanceRareBee(index));
            long balance = game.Dew.Balance;
            if (balance >= 20)
                Assert.Ignore("Started with enough Dewdrops for a level.");
            Assert.That(game.TryAdvanceRareBee(index), Is.False);
            Assert.That(game.RareBees.Level(index), Is.EqualTo(1));

            game.DebugGrantDew(20 - balance);
            Assert.That(game.OpenHive());
            yield return null;
            Card(RareBeeIds.Bumblebee).Press();
            Assert.That(game.RareBees.Level(index), Is.EqualTo(2), "20 Dewdrops is under the confirm limit: one tap.");
            Assert.That(game.Dew.Balance, Is.Zero);
            Assert.That(game.Bees.RareBees.Count, Is.EqualTo(1), "A level adds no second bee.");
            Assert.That(hive.Status, Is.EqualTo(string.Format(Strings.RareBeeLevelUpFormat, Strings.RareBumblebee, 2)));
        }

        [UnityTest]
        public IEnumerator RareBees_StayThroughTheQueenMove_AndKeepFlying()
        {
            yield return MoveQueen();
            Assert.That(game.TryAdvanceRareBee(Index(RareBeeIds.Bumblebee)));
            for (int i = 0; i < 60; i++)
                yield return null;

            yield return MoveQueen();
            Assert.That(game.GardenNumber, Is.EqualTo(3));
            Assert.That(game.Bees.RareBees.Count, Is.EqualTo(1));
            Assert.That(game.RareBees.Level(Index(RareBeeIds.Bumblebee)), Is.EqualTo(1));
            Assert.That(game.Bees.Count, Is.EqualTo(game.StartingWorkers), "The tier bees reset as before.");

            Assert.That(game.OpenHive());
            yield return null;
            Assert.That(Card(RareBeeIds.MasonBee).ShownState, Is.Not.EqualTo(RareBeeCardView.State.Locked), "Garden 3 opens the Mason Bee.");
            hive.Close();

            // The rare bee forgets the old garden's flowers and works the new one: it carries loads home.
            Bee rare = game.Bees.RareBees[0];
            bool carried = false;
            Time.timeScale = 4f;
            float end = Time.realtimeSinceStartup + 6f;
            while (!carried && Time.realtimeSinceStartup < end)
            {
                yield return null;
                carried = rare.State == BeeState.ReturnToHive && rare.Carried > 0;
            }
            Time.timeScale = 1f;
            Assert.That(carried, "The rare bee brings nectar home in the new garden.");
        }

        [UnityTest]
        public IEnumerator Reload_BringsTheRareBeesBack()
        {
            yield return MoveQueen();
            Assert.That(game.TryAdvanceRareBee(Index(RareBeeIds.Bumblebee)));
            game.SaveNow();

            yield return LoadMain();
            Assert.That(game.RareBees.Level(Index(RareBeeIds.Bumblebee)), Is.EqualTo(1));
            Assert.That(game.Bees.RareBees.Count, Is.EqualTo(1));
            Assert.That(game.Bees.CapacityMultiplier, Is.EqualTo(1.03f).Within(1e-5));
            yield return null;
            Assert.That(hive.IsButtonShown);
        }

        [UnityTest]
        public IEnumerator TapOnTheHive_OpensThePanel_ASwipeDoesNot()
        {
            yield return null;
            Tap(HiveScreenPoint());
            Assert.That(hive.IsOpen, Is.False, "No Hive panel before the first move.");

            yield return MoveQueen();
            Vector2 point = HiveScreenPoint();
            var catcher = Object.FindAnyObjectByType<SwipeCatcher>();
            var swipe = new PointerEventData(EventSystem.current) { position = point, pointerId = 0 };
            catcher.OnPointerDown(swipe);
            swipe.position = point + new Vector2(Screen.height * 0.2f, 0f);
            catcher.OnDrag(swipe);
            catcher.OnPointerUp(swipe);
            Assert.That(hive.IsOpen, Is.False, "A swipe that starts on the hive shakes flowers.");

            Tap(point);
            Assert.That(hive.IsOpen, "A tap on the hive opens it.");
            hive.Close();

            Tap(new Vector2(Screen.width * 0.5f, Screen.height * 0.95f));
            Assert.That(hive.IsOpen, Is.False, "A tap elsewhere does not.");
        }

        Vector2 HiveScreenPoint()
        {
            Transform hiveTransform = Object.FindAnyObjectByType<HiveView>().transform;
            return Camera.main.WorldToScreenPoint(hiveTransform.position + Vector3.up * (0.7f * hiveTransform.lossyScale.y));
        }

        static void Tap(Vector2 screen)
        {
            var catcher = Object.FindAnyObjectByType<SwipeCatcher>();
            var data = new PointerEventData(EventSystem.current) { position = screen, pointerId = 0 };
            catcher.OnPointerDown(data);
            catcher.OnPointerUp(data);
        }

        // ---- Every bonus at level 10, from a save in garden 5 ----

        /// <summary>A save in garden 5 (two loops in) with every species at <paramref name="level"/>.</summary>
        IEnumerator LoadWithEverySpecies(int level)
        {
            string[] ids =
            {
                RareBeeIds.Bumblebee, RareBeeIds.BlueBandedBee, RareBeeIds.MasonBee,
                RareBeeIds.OrchidBee, RareBeeIds.LeafcutterBee, RareBeeIds.CarpenterBee,
            };
            var data = new SaveData
            {
                gardenIndex = 4,
                movesMade = 4,
                dewdrops = 300,
                dewEarned = 9000,
                rareBees = ids.Select(id => new AbilityLevel { id = id, level = level }).ToArray(),
            };
            data.stats.flowersShaken = 100;
            TestSave.Clear();
            Assert.That(TestSave.Files().Save(data));
            yield return LoadMain();
        }

        [UnityTest]
        public IEnumerator EveryBonus_ReachesItsSystem()
        {
            yield return LoadWithEverySpecies(10);
            Assert.That(game.Bees.RareBees.Count, Is.EqualTo(6));
            Assert.That(game.Bees.CapacityMultiplier, Is.EqualTo(1.3f).Within(1e-5), "Bumblebee: +30% carrying.");
            Assert.That(game.OfflineEfficiency, Is.EqualTo(0.8).Within(1e-6), "Mason Bee: 50% + 30 points offline.");
            Assert.That(game.Upgrades.CostFactor, Is.EqualTo(0.8).Within(1e-9), "Leafcutter Bee: -20% Add Bee and Evolve.");
            Assert.That(game.Prestige.JellyMultiplier, Is.EqualTo(1.4).Within(1e-9), "Carpenter Bee: +40% Royal Jelly.");

            BigNumber discounted = game.Upgrades.AddBeeCost;
            game.Upgrades.CostFactor = 1;
            BigNumber full = game.Upgrades.AddBeeCost;
            game.Upgrades.CostFactor = 0.8;
            Assert.That(discounted, Is.EqualTo(UpgradeMath.Scaled(full, 0.8)));

            RareBee orchidBee = game.RareBees.Bees[Index(RareBeeIds.OrchidBee)];
            foreach (Flower flower in game.Flowers.Flowers)
            {
                bool boosted = orchidBee.Flowers.Contains(flower.Type);
                Assert.That(flower.Value, Is.EqualTo(flower.BaseValue * (boosted ? 1.6 : 1.0)).Within(1e-9),
                    $"Orchid Bee: +60% on its flowers only ({flower.Type.name}).");
            }
            Assert.That(game.Flowers.Flowers.Any(flower => flower.Value > flower.BaseValue), "Garden 5 grows Orchids or Lavender.");

            // Blue Banded Bee: a sweep pays 80% more honey than the plain shake nectar is worth.
            double nectar = game.ShakeNectar();
            double multiplier = game.Economy.TotalMultiplier.ToDouble();
            double honey = game.Economy.Honey.ToDouble();
            Assert.That(game.ShakeAllFlowers(), Is.GreaterThan(0));
            double shakenValue = game.Flowers.Flowers.Where(flower => flower.PollenCooldown > 0f).Sum(flower => flower.Value);
            double expected = nectar * 1.8 * shakenValue * multiplier;
            Assert.That(game.Economy.Honey.ToDouble() - honey, Is.EqualTo(expected).Within(expected * 1e-6));

            // Every rare bee flies out and works the garden.
            for (int i = 0; i < 120; i++)
                yield return null;
            Assert.That(game.Bees.RareBees.Count(bee => bee.State != BeeState.Idle), Is.GreaterThan(0));
            yield return Shot("garden_all");

            Assert.That(game.OpenHive());
            yield return null;
            foreach (RareBeeCardView card in hive.Cards)
                Assert.That(card.ShownState, Is.EqualTo(RareBeeCardView.State.Maxed));
            yield return Shot("panel_maxed");
        }

        [UnityTest]
        public IEnumerator HalfwaySpecies_ShowTheirNextLevel()
        {
            yield return LoadWithEverySpecies(3);
            Assert.That(game.OpenHive());
            yield return null;
            RareBeeCardView card = Card(RareBeeIds.LeafcutterBee);
            StringAssert.Contains(string.Format(Strings.RareEffectDiscountFormat, 6), card.Body);
            StringAssert.Contains(string.Format(Strings.RareBeeNextFormat, "-2%"), card.Body);
            Assert.That(card.Cost, Is.EqualTo("45"), "Level 3 to 4 costs 45.");
            yield return Shot("panel_halfway");
        }
    }
}
