using Buzzfield.Audio;
using Buzzfield.Bees;
using Buzzfield.Core;
using Buzzfield.Flowers;
using Buzzfield.Platform;
using Buzzfield.UI;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// Rare bees (docs/DEWDROPS.md 4.2): species that join the hive for Dewdrops from the first Queen
    /// move on (the first one for free), each flying in the garden as one bee of its own and giving
    /// a permanent bonus that grows with its level. Their bonuses are applied to the systems they
    /// change whenever a level changes. The Hive panel opens from the hive button or a tap on the hive.
    /// </summary>
    public sealed partial class GameManager
    {
        [Header("Rare bees")]
        [SerializeField] private RareBeeSettings rareBeeSettings;
        [SerializeField] private HivePanelView hivePanel;

        private RareBeeManager rareBees;
        private System.Func<FlowerType, double> flowerFactor;

        /// <summary>Factor on the honey of every pollen shake (the Blue Banded Bee).</summary>
        private double shakeHoneyFactor = 1;

        /// <summary>The current press started on the hive and has not turned into a swipe or a catch.</summary>
        private bool hiveTapPending;
        private Vector2 hiveTapStart;

        public RareBeeManager RareBees => rareBees;

        public bool IsHivePanelOpen => hivePanel.IsOpen;

        /// <summary>The garden the player is in, 1 for the first.</summary>
        public int GardenNumber => prestige.GardenIndex + 1;

        /// <summary>The hive button shows and the Hive panel opens (from the first Queen move).</summary>
        public bool HiveUnlocked => rareBees.IsHiveOpen(GardenNumber);

        /// <summary>Share of the theoretical honey per second paid while away, the Mason Bee's points included.</summary>
        public double OfflineEfficiency =>
            RareBeeMath.RaisedEfficiency(offlineSettings.Efficiency, rareBees.EffectTotal(RareBeeEffect.OfflineEfficiencyPoints));

        /// <summary>Needs the bees, flowers, upgrades, prestige, Dewdrops and the swipe catcher.</summary>
        private void InitRareBees()
        {
            rareBees = new RareBeeManager(rareBeeSettings);
            flowerFactor = rareBees.FlowerFactor;
            rareBees.OnChanged += ApplyRareBeeEffects;
            hivePanel.Init(rareBees, dew, prestige, dewSettings, TryAdvanceRareBee, tweener, feedbackSettings);
            swipeCatcher.OnPress += HandleHivePress;
            swipeCatcher.OnStroke += HandleHiveStroke;
            swipeCatcher.OnRelease += HandleHiveRelease;
            ApplyRareBeeEffects();
        }

        private void DisposeRareBees()
        {
            if (rareBees != null)
                rareBees.OnChanged -= ApplyRareBeeEffects;
            if (swipeCatcher == null)
                return;
            swipeCatcher.OnPress -= HandleHivePress;
            swipeCatcher.OnStroke -= HandleHiveStroke;
            swipeCatcher.OnRelease -= HandleHiveRelease;
        }

        /// <summary>Opens the Hive panel; false before the first Queen move.</summary>
        public bool OpenHive() => hivePanel.Open();

        /// <summary>
        /// Pays for the next step of species <paramref name="index"/> and takes it: at level 0 the
        /// species joins and its bee flies out of the hive, after that it gains a level. False when
        /// the hive or the species is not open yet, it is at its top level or the Dewdrops are short.
        /// </summary>
        public bool TryAdvanceRareBee(int index)
        {
            if (index < 0 || index >= rareBees.Count || !HiveUnlocked || !rareBees.IsAvailable(index, GardenNumber) || rareBees.IsMaxed(index))
                return false;
            RareBee bee = rareBees.Bees[index];
            long cost = rareBees.NextCost(index);
            if (cost > 0 && !TrySpendDew(cost, "rare_bee_" + bee.Id))
                return false;
            rareBees.AddLevel(index);
            int level = rareBees.Level(index);
            if (level == 1)
            {
                beeManager.SpawnRare(bee.Flight);
                sound.Play(Sfx.BeeSpawn);
            }
            else
            {
                sound.Play(Sfx.AbilityBuy);
            }
            Pulse(true);
            FirebaseServices.Log("rare_bee", ("id", bee.Id), ("level", (long)level));
            SaveNow();
            return true;
        }

        private void ApplyRareBeeEffects()
        {
            beeManager.CapacityMultiplier = (float)QueenMath.PercentFactor(rareBees.EffectTotal(RareBeeEffect.CarryCapacityPercent));
            shakeHoneyFactor = QueenMath.PercentFactor(rareBees.EffectTotal(RareBeeEffect.ShakeHoneyPercent));
            upgrades.CostFactor = RareBeeMath.DiscountFactor(rareBees.EffectTotal(RareBeeEffect.UpgradeDiscountPercent));
            prestige.JellyMultiplier = QueenMath.PercentFactor(rareBees.EffectTotal(RareBeeEffect.RoyalJellyPercent));
            flowerManager.ApplyValueFactor(flowerFactor);
        }

        /// <summary>One bee for every species that has joined; called once after the first garden is loaded.</summary>
        private void SpawnRareBees()
        {
            for (int i = 0; i < rareBees.Count; i++)
            {
                if (rareBees.IsOwned(i))
                    beeManager.SpawnRare(rareBees.Bees[i].Flight);
            }
        }

        // ---- A tap on the hive ----

        private void HandleHivePress(Vector2 screen)
        {
            hiveTapStart = screen;
            hiveTapPending = HiveUnlocked && garden.Hive != null && HitsHive(worldCamera.ScreenPointToRay(screen));
        }

        private void HandleHiveStroke(Vector2 from, Vector2 to)
        {
            if (hiveTapPending && (to - hiveTapStart).magnitude > rareBeeSettings.TapSlop * Screen.height)
                hiveTapPending = false;
        }

        private void HandleHiveRelease()
        {
            if (!hiveTapPending)
                return;
            hiveTapPending = false;
            OpenHive();
        }

        /// <summary>The ray passes within the tap radius of the hive's middle, both at the hive's scale.</summary>
        private bool HitsHive(Ray ray)
        {
            Transform hive = garden.Hive.transform;
            float scale = hive.lossyScale.y;
            Vector3 middle = hive.position + Vector3.up * (rareBeeSettings.HiveTapHeight * scale);
            Vector3 toMiddle = middle - ray.origin;
            if (Vector3.Dot(toMiddle, ray.direction) <= 0f)
                return false;
            float radius = rareBeeSettings.HiveTapRadius * scale;
            return Vector3.Cross(ray.direction, toMiddle).sqrMagnitude <= radius * radius;
        }
    }
}
