using System;
using System.Collections.Generic;

namespace Buzzfield.Core
{
    /// <summary>
    /// <see cref="BigNumber"/> in a form JsonUtility can write: BigNumber's fields are
    /// readonly, so the save stores mantissa and exponent here.
    /// </summary>
    [Serializable]
    public struct BigNumberData
    {
        public double m;
        public long e;

        public static BigNumberData From(BigNumber value) => new BigNumberData { m = value.Mantissa, e = value.Exponent };

        /// <summary>Back to a BigNumber; a damaged mantissa (NaN, infinity) reads as zero.</summary>
        public BigNumber ToBigNumber()
        {
            if (double.IsNaN(m) || double.IsInfinity(m))
                return BigNumber.Zero;
            return BigNumber.Create(m, e);
        }

        /// <summary>Like <see cref="ToBigNumber"/>, but never below zero (balances and totals).</summary>
        public BigNumber ToNonNegative()
        {
            BigNumber value = ToBigNumber();
            return value.IsNegative ? BigNumber.Zero : value;
        }
    }

    /// <summary>Readings <see cref="OfflineEarnings"/> needs from the moment the player left.</summary>
    [Serializable]
    public struct ClockStamp
    {
        /// <summary>Best-known UTC (trusted when available), unix seconds.</summary>
        public double lastSeenUtc;
        public double lastDeviceUtc;
        public double lastMonotonicSeconds;
        /// <summary>Device UTC minus monotonic seconds: tells whether the device rebooted since.</summary>
        public double lastBootUtc;
    }

    /// <summary>Totals that survive every Queen move.</summary>
    [Serializable]
    public sealed class LifetimeStats
    {
        public BigNumberData offlineHoney;
        public long flowersBloomed;
        public long gardensCompleted;
        public long beesEvolved;
        public double playSeconds;
        /// <summary>Flowers shaken for pollen; the swipe hint shows until the first few.</summary>
        public long flowersShaken;
    }

    /// <summary>The player's choices in the settings panel.</summary>
    [Serializable]
    public sealed class PlayerSettings
    {
        public bool musicOn = true;
        public bool soundOn = true;
        public VibrationMode vibration = VibrationMode.All;
    }

    /// <summary>One Queen ability level, keyed by the ability id so the list can be reordered or grow.</summary>
    [Serializable]
    public struct AbilityLevel
    {
        public string id;
        public int level;
    }

    /// <summary>
    /// Everything written to the save file. Plain fields for JsonUtility; the game maps
    /// its managers to and from this. Bee positions are not saved: bees respawn at the hive.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int saveVersion = SaveMigration.CurrentVersion;

        // Run state (reset by the Queen move)
        public BigNumberData honey;
        public BigNumberData runHoneyEarned;
        public int beesBought;
        public int speedLevel;
        public int honeyValueLevel;
        /// <summary>Bees per tier, lowest first; bees in a running merge count as the merged bee.</summary>
        public int[] beesPerTier;
        public float[] slotBloom;
        public bool[] slotActive;
        public double[] slotNectar;

        // Kept through the Queen move
        public BigNumberData lifetimeHoneyEarned;
        public BigNumberData royalJelly;
        /// <summary>Every Royal Jelly ever earned; the Queen XP.</summary>
        public BigNumberData lifetimeJelly;
        public int gardenIndex;
        public int movesMade;
        /// <summary>Bought Queen ability levels; abilities not listed are at level 0.</summary>
        public AbilityLevel[] abilities;
        public double rewardedBoostEndUtc;
        /// <summary>Honey boosts bought with Dewdrops, strongest first, as they were at <see cref="dewBoostAnchorUtc"/>.</summary>
        public DewBoostEntry[] dewBoosts;
        public double dewBoostAnchorUtc;
        public double lastFullScreenAdUtc;
        public Entitlements entitlements = new Entitlements();
        public LifetimeStats stats = new LifetimeStats();
        /// <summary>Goals claimed so far; the next one in the goal list is the current goal.</summary>
        public int goalIndex;
        /// <summary>Count the current endless goal started from (it counts from the moment it began).</summary>
        public long goalBase;
        /// <summary>Dewdrops, the premium currency.</summary>
        public long dewdrops;
        /// <summary>Every Dewdrop ever earned or bought; the counter shows from the first one.</summary>
        public long dewEarned;
        /// <summary>The one-time gift on the first Welcome back has been given.</summary>
        public bool welcomeGiftGiven;
        public PlayerSettings settings = new PlayerSettings();

        // Offline earnings
        /// <summary>Theoretical honey per second at save time, paid (times efficiency) while away.</summary>
        public BigNumberData offlineRate;
        public ClockStamp clock;

        /// <summary>Replaces missing arrays and objects with empty ones, so readers need no null checks.</summary>
        public void Normalize()
        {
            beesPerTier = beesPerTier ?? Array.Empty<int>();
            slotBloom = slotBloom ?? Array.Empty<float>();
            slotActive = slotActive ?? Array.Empty<bool>();
            slotNectar = slotNectar ?? Array.Empty<double>();
            abilities = abilities ?? Array.Empty<AbilityLevel>();
            dewBoosts = dewBoosts ?? Array.Empty<DewBoostEntry>();
            stats = stats ?? new LifetimeStats();
            entitlements = entitlements ?? new Entitlements();
            entitlements.grantedOrders = entitlements.grantedOrders ?? new List<string>();
            settings = settings ?? new PlayerSettings();
            if (goalIndex < 0)
                goalIndex = 0;
            if (goalBase < 0)
                goalBase = 0;
            if (dewdrops < 0)
                dewdrops = 0;
            if (dewEarned < dewdrops)
                dewEarned = dewdrops;
        }
    }
}
