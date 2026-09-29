using System;
using System.Collections.Generic;
using Buzzfield.Bees;
using Buzzfield.Core;
using Buzzfield.Flowers;
using Buzzfield.Save;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// Save, load, autosave and offline earnings. Saves every <see cref="GameSettings.AutosaveIntervalSeconds"/>,
    /// when the app pauses or loses focus, after a Queen move and on quit (desktop); mobile
    /// never relies on OnApplicationQuit. Every save stamps the clock, so a killed app still
    /// pays offline earnings from its last save.
    /// </summary>
    public sealed partial class GameManager
    {
        private readonly SaveData saveData = new SaveData();
        private SaveManager saveManager;
        private GameClock clock;
        private LifetimeStats stats = new LifetimeStats();
        private float autosaveTimer;

        /// <summary>Clock and rate of the loaded save; null for a new game.</summary>
        private ClockStamp? loadedClock;
        private BigNumber loadedRate;

        /// <summary>Clock and rate written by the latest save: the base for the next return.</summary>
        private ClockStamp lastStamp;
        private BigNumber lastRate;
        private bool paused;

        /// <summary>
        /// The app lost the screen to a full-screen ad or the store's payment sheet, not to the
        /// player leaving: the time away is still credited, but no Welcome back panel opens.
        /// </summary>
        private bool pausedByOverlay;

        /// <summary>A return that waits for trusted time (the device rebooted while away).</summary>
        private PendingReturn? pendingReturn;

        public SaveSource LastLoadSource => saveManager.LastLoadSource;

        /// <summary>The last offline evaluation, for tests and debugging.</summary>
        public OfflineResult LastOffline { get; private set; }

        public bool IsWelcomeBackOpen => welcomeBack.IsOpen;

        private struct PendingReturn
        {
            public ClockStamp Stamp;
            public BigNumber Rate;
            public double DeviceNow;
            public double MonotonicNow;
        }

        private void InitSave()
        {
            saveManager = new SaveManager(gameSettings.SaveFileName);
            clock = new GameClock();
            clock.TrustedTimeArrived += HandleTrustedTimeArrived;
            autosaveTimer = gameSettings.AutosaveIntervalSeconds;
        }

        private void DisposeSave()
        {
            if (clock != null)
                clock.TrustedTimeArrived -= HandleTrustedTimeArrived;
        }

        /// <summary>Pays for the time since <paramref name="stamp"/> (if any) and writes a fresh save.</summary>
        private void StartSession(ClockStamp? stamp)
        {
            if (gameSettings.FetchTrustedTime)
                StartCoroutine(clock.FetchTrustedTime());
            if (stamp.HasValue)
                EvaluateReturn(stamp.Value, loadedRate, GameClock.DeviceUtc, GameClock.MonotonicSeconds);
            SaveNow();
        }

        /// <summary>Writes the game now. Also stamps the clock and the offline rate.</summary>
        public void SaveNow()
        {
            if (saveManager == null)
                return;
            Capture(saveData);
            lastStamp = saveData.clock;
            lastRate = saveData.offlineRate.ToBigNumber();
            saveManager.Save(saveData);
            autosaveTimer = gameSettings.AutosaveIntervalSeconds;
        }

        /// <summary>
        /// Honey per second the current bees, upgrades and garden produce in theory: bee
        /// trips limited by flower regrowth. Timed boosts are not included; the bought multiplier is.
        /// </summary>
        public BigNumber TheoreticalHoneyPerSecond()
        {
            IReadOnlyList<Flower> flowers = flowerManager.Flowers;
            Vector3 hive = garden.Hive != null ? garden.Hive.EntrancePoint : Vector3.zero;
            double regen = 0;
            double valueRegen = 0;
            double distance = 0;
            int active = 0;
            for (int i = 0; i < flowers.Count; i++)
            {
                Flower flower = flowers[i];
                if (!flower.IsActive)
                    continue;
                double flowerRegen = flower.Type.RegenPerSecond;
                regen += flowerRegen;
                valueRegen += flowerRegen * flower.Value;
                distance += Vector3.Distance(hive, flower.NectarPoint);
                active++;
            }
            if (active == 0 || regen <= 0)
                return BigNumber.Zero;

            double roundTrip = 2 * distance / active;
            int[] counts = TierCounts();
            IReadOnlyList<BeeTier> tiers = beeSettings.Tiers;
            double beeNectar = 0;
            for (int t = 0; t < counts.Length; t++)
            {
                if (counts[t] == 0)
                    continue;
                BeeTier tier = tiers[t];
                beeNectar += counts[t] * IncomeMath.BeeNectarPerSecond(tier.Capacity, tier.Speed * beeManager.SpeedMultiplier,
                    roundTrip, tier.CollectDuration, beeSettings.DepositDuration);
            }
            // The Queen level and the bought multiplier are permanent, so they count; timed boosts do not.
            return IncomeMath.HoneyPerSecond(beeNectar, regen, valueRegen / regen,
                economy.HoneyValueMultiplier * economy.PermanentMultiplier);
        }

        private void TickAutosave(float unscaledDeltaTime)
        {
            stats.playSeconds += unscaledDeltaTime;
            autosaveTimer -= unscaledDeltaTime;
            if (autosaveTimer <= 0f)
                SaveNow();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                paused = true;
                pausedByOverlay = ads.IsShowing || store.IsPurchasing;
                // A break the player walked away from is not a break any more.
                if (!pausedByOverlay)
                    ads.ClearPendingBreak();
                SaveNow();
                return;
            }
            // Unity also sends pause(false) once at startup; only a real resume pays.
            if (!paused)
                return;
            paused = false;
            pendingReturn = null;
            EvaluateReturn(lastStamp, lastRate, GameClock.DeviceUtc, GameClock.MonotonicSeconds);
            pausedByOverlay = false;
            SaveNow();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                SaveNow();
        }

        private void OnApplicationQuit() => SaveNow();

        private void EvaluateReturn(ClockStamp stamp, BigNumber rate, double deviceNow, double monotonicNow)
        {
            double capSeconds = OfflineCapHours * 3600.0;
            OfflineClockInput input = clock.ReturnInput(stamp, deviceNow, monotonicNow);
            OfflineResult result = OfflineEarnings.Evaluate(input, capSeconds, rate,
                offlineSettings.Efficiency, offlineSettings.ClockToleranceSeconds);
            LastOffline = result;

            switch (result.Status)
            {
                case OfflineStatus.Credited:
                    ApplyOffline(result, capSeconds);
                    break;
                case OfflineStatus.PendingTrustedTime:
                    pendingReturn = new PendingReturn { Stamp = stamp, Rate = rate, DeviceNow = deviceNow, MonotonicNow = monotonicNow };
                    break;
                default:
                    // The next save stamps the clock again, which resets the bad timestamp.
                    Debug.LogWarning($"Offline earnings skipped: {result.Status}.");
                    break;
            }
        }

        private void HandleTrustedTimeArrived()
        {
            if (!pendingReturn.HasValue)
                return;
            PendingReturn pending = pendingReturn.Value;
            pendingReturn = null;
            EvaluateReturn(pending.Stamp, pending.Rate, pending.DeviceNow, pending.MonotonicNow);
            SaveNow();
        }

        private void ApplyOffline(OfflineResult result, double capSeconds)
        {
            flowerManager.RegenerateFor(result.CreditedSeconds);
            if (!result.IsPayable)
                return;
            GrantOffline(result.Amount);
            if (result.ElapsedSeconds >= offlineSettings.MinAwaySeconds && !pausedByOverlay)
                OfferOfflineAd(result.Amount, result.ElapsedSeconds, result.CapReached, capSeconds);
        }

        private void GrantOffline(BigNumber amount)
        {
            economy.Grant(amount);
            stats.offlineHoney = BigNumberData.From(stats.offlineHoney.ToNonNegative() + amount);
        }

        private int[] tierCountBuffer;

        private int[] TierCounts()
        {
            int tiers = beeSettings.Tiers.Count;
            if (tierCountBuffer == null || tierCountBuffer.Length != tiers)
                tierCountBuffer = new int[tiers];
            beeManager.GetSaveCounts(tierCountBuffer);
            return tierCountBuffer;
        }

        private void Capture(SaveData data)
        {
            data.honey = BigNumberData.From(economy.Honey);
            data.runHoneyEarned = BigNumberData.From(economy.RunHoneyEarned);
            data.lifetimeHoneyEarned = BigNumberData.From(economy.LifetimeHoneyEarned);
            data.beesBought = upgrades.BeesBought;
            data.speedLevel = upgrades.SpeedLevel;
            data.honeyValueLevel = upgrades.HoneyValueLevel;
            data.beesPerTier = (int[])TierCounts().Clone();

            IReadOnlyList<Flower> flowers = flowerManager.Flowers;
            data.slotBloom = Resize(data.slotBloom, flowers.Count);
            data.slotActive = Resize(data.slotActive, flowers.Count);
            data.slotNectar = Resize(data.slotNectar, flowers.Count);
            for (int i = 0; i < flowers.Count; i++)
            {
                data.slotBloom[i] = flowers[i].Bloom;
                data.slotActive[i] = flowers[i].IsActive;
                data.slotNectar[i] = flowers[i].Nectar;
            }

            data.royalJelly = BigNumberData.From(prestige.RoyalJelly);
            data.lifetimeJelly = BigNumberData.From(prestige.LifetimeJelly);
            data.gardenIndex = prestige.GardenIndex;
            data.movesMade = prestige.MovesMade;
            data.abilities = queen.Capture();
            data.rewardedBoostEndUtc = boosts.RewardedHoneyEndUtc;
            data.lastFullScreenAdUtc = ads.LastFullScreenAdUtc;
            data.entitlements.CopyFrom(store.Entitlements);
            data.stats = stats;
            data.offlineRate = BigNumberData.From(TheoreticalHoneyPerSecond());
            data.clock = clock.Stamp();
        }

        private void RestoreGame(SaveData data)
        {
            prestige.Restore(data.royalJelly.ToNonNegative(), data.lifetimeJelly.ToNonNegative(), data.gardenIndex, data.movesMade);
            economy.Restore(data.honey.ToNonNegative(), data.runHoneyEarned.ToNonNegative(), data.lifetimeHoneyEarned.ToNonNegative());
            upgrades.Restore(data.beesBought, data.speedLevel, data.honeyValueLevel);
            stats = data.stats;
            queen.Restore(data.abilities);
            ads.Restore(data.lastFullScreenAdUtc);
            RestoreStore(data.entitlements);
            boosts.RestoreRewardedHoney(data.rewardedBoostEndUtc, GameClock.DeviceUtc);

            LoadGarden(data);
            int spawned = 0;
            int tiers = beeSettings.Tiers.Count;
            for (int t = 0; t < tiers && t < data.beesPerTier.Length; t++)
            {
                for (int i = 0; i < data.beesPerTier[t]; i++)
                {
                    if (beeManager.Spawn(t) != null)
                        spawned++;
                }
            }
            if (spawned == 0)
                SpawnStartingBees();

            // A save without a clock reading (zeroed fields) pays nothing for the absence.
            loadedClock = data.clock.lastDeviceUtc > 0 ? data.clock : (ClockStamp?)null;
            loadedRate = data.offlineRate.ToNonNegative();
        }

        private void RestoreSlots(SaveData data)
        {
            int count = Math.Min(flowerManager.Flowers.Count, Math.Min(data.slotBloom.Length,
                Math.Min(data.slotActive.Length, data.slotNectar.Length)));
            for (int i = 0; i < count; i++)
                flowerManager.RestoreSlot(i, data.slotActive[i], data.slotBloom[i], data.slotNectar[i]);
        }

        private static T[] Resize<T>(T[] array, int length) =>
            array != null && array.Length == length ? array : new T[length];
    }
}
