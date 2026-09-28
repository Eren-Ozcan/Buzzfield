using System;

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
        public int[] abilityLevels;
        public double rewardedBoostEndUtc;
        public double lastFullScreenAdUtc;
        public Entitlements entitlements = new Entitlements();
        public LifetimeStats stats = new LifetimeStats();

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
            abilityLevels = abilityLevels ?? Array.Empty<int>();
            stats = stats ?? new LifetimeStats();
            entitlements = entitlements ?? new Entitlements();
        }
    }
}
