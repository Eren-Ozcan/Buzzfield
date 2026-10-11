using System;
using System.Collections.Generic;
using Buzzfield.Core;
using Buzzfield.Flowers;

namespace Buzzfield.Bees
{
    /// <summary>
    /// Rare bee levels (docs/DEWDROPS.md 4.2): which species have joined and how far each has
    /// grown. Survives every Queen move. Plain class: the game pays the Dewdrops, spawns the bee
    /// when a species joins and applies <see cref="EffectTotal"/> to the systems the bonuses
    /// change whenever <see cref="OnChanged"/> fires.
    /// </summary>
    public sealed class RareBeeManager
    {
        private readonly RareBeeSettings settings;
        private readonly int[] levels;

        public RareBeeManager(RareBeeSettings settings)
        {
            this.settings = settings;
            levels = new int[settings.Bees.Count];
        }

        public int Count => levels.Length;

        public IReadOnlyList<RareBee> Bees => settings.Bees;

        /// <summary>Raised when any level changes (a species joined or grew, a save was loaded).</summary>
        public event Action OnChanged;

        public int Level(int index) => levels[index];

        /// <summary>The species has joined the hive (level 1 or more).</summary>
        public bool IsOwned(int index) => levels[index] > 0;

        public bool IsMaxed(int index) => levels[index] >= settings.Bees[index].MaxLevel;

        /// <summary>The hive button shows and the Hive panel opens from garden <see cref="RareBeeSettings.HiveUnlockGarden"/>.</summary>
        public bool IsHiveOpen(int gardenNumber) => gardenNumber >= settings.HiveUnlockGarden;

        /// <summary>The player has reached the garden the species needs.</summary>
        public bool IsAvailable(int index, int gardenNumber) => RareBeeMath.IsAvailable(gardenNumber, settings.Bees[index].UnlockGarden);

        /// <summary>Dewdrops for the next step: joining, then each level; -1 at the top level.</summary>
        public long NextCost(int index)
        {
            RareBee bee = settings.Bees[index];
            return RareBeeMath.NextCost(levels[index], bee.MaxLevel, bee.UnlockCost, settings.LevelCosts);
        }

        /// <summary>A species that joins for free and waits for its tap at <paramref name="gardenNumber"/>; -1 when none.</summary>
        public int FreeWaiting(int gardenNumber)
        {
            if (!IsHiveOpen(gardenNumber))
                return -1;
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] == 0 && IsAvailable(i, gardenNumber) && NextCost(i) == 0)
                    return i;
            }
            return -1;
        }

        /// <summary>Some species could take its next step at <paramref name="gardenNumber"/> with <paramref name="dewdrops"/>.</summary>
        public bool AnyAffordable(int gardenNumber, long dewdrops)
        {
            if (!IsHiveOpen(gardenNumber))
                return false;
            for (int i = 0; i < levels.Length; i++)
            {
                long cost = NextCost(i);
                if (cost >= 0 && cost <= dewdrops && IsAvailable(i, gardenNumber))
                    return true;
            }
            return false;
        }

        /// <summary>Adds a level to species <paramref name="index"/> (level 1 = it joins); the caller has paid. False at the top level.</summary>
        public bool AddLevel(int index)
        {
            if (index < 0 || index >= levels.Length || IsMaxed(index))
                return false;
            levels[index]++;
            OnChanged?.Invoke();
            return true;
        }

        /// <summary>Bonus of species <paramref name="index"/> at <paramref name="level"/>, in the unit of its effect.</summary>
        public double Effect(int index, int level) => RareBeeMath.Effect(settings.Bees[index].EffectPerLevel, level);

        /// <summary>Summed bonus of every species with <paramref name="effect"/>, in the effect's unit.</summary>
        public double EffectTotal(RareBeeEffect effect)
        {
            double total = 0;
            IReadOnlyList<RareBee> bees = settings.Bees;
            for (int i = 0; i < levels.Length; i++)
            {
                if (bees[i].Effect == effect)
                    total += RareBeeMath.Effect(bees[i].EffectPerLevel, levels[i]);
            }
            return total;
        }

        /// <summary>Honey factor on nectar from flowers of <paramref name="type"/> (1 = no bonus).</summary>
        public double FlowerFactor(FlowerType type)
        {
            double percent = 0;
            IReadOnlyList<RareBee> bees = settings.Bees;
            for (int i = 0; i < levels.Length; i++)
            {
                RareBee bee = bees[i];
                if (bee.Effect != RareBeeEffect.FlowerHoneyPercent || levels[i] == 0)
                    continue;
                IReadOnlyList<FlowerType> flowers = bee.Flowers;
                for (int f = 0; f < flowers.Count; f++)
                {
                    if (flowers[f] == type)
                    {
                        percent += RareBeeMath.Effect(bee.EffectPerLevel, levels[i]);
                        break;
                    }
                }
            }
            return 1 + percent / 100.0;
        }

        /// <summary>Loads saved levels by id. Unknown ids are dropped; levels are clamped to the current max.</summary>
        public void Restore(AbilityLevel[] saved)
        {
            Array.Clear(levels, 0, levels.Length);
            if (saved != null)
            {
                for (int s = 0; s < saved.Length; s++)
                {
                    int index = IndexOf(saved[s].id);
                    if (index >= 0)
                        levels[index] = RareBeeMath.ClampLevel(saved[s].level, settings.Bees[index].MaxLevel);
                }
            }
            OnChanged?.Invoke();
        }

        /// <summary>Levels for the save; species that have not joined are left out.</summary>
        public AbilityLevel[] Capture()
        {
            int count = 0;
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] > 0)
                    count++;
            }
            var saved = new AbilityLevel[count];
            for (int i = 0, s = 0; i < levels.Length; i++)
            {
                if (levels[i] > 0)
                    saved[s++] = new AbilityLevel { id = settings.Bees[i].Id, level = levels[i] };
            }
            return saved;
        }

        public int IndexOf(string id)
        {
            IReadOnlyList<RareBee> bees = settings.Bees;
            for (int i = 0; i < bees.Count; i++)
            {
                if (bees[i].Id == id)
                    return i;
            }
            return -1;
        }
    }
}
