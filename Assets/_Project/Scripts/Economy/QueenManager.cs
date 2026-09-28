using System;
using System.Collections.Generic;
using Buzzfield.Core;

namespace Buzzfield.Economy
{
    /// <summary>
    /// Queen level (read from lifetime Royal Jelly) and Queen ability levels. Both survive
    /// every Queen move. Plain class: the game applies <see cref="HoneyMultiplier"/> and
    /// <see cref="EffectTotal"/> to the systems they change whenever <see cref="OnChanged"/> fires.
    /// </summary>
    public sealed class QueenManager : IDisposable
    {
        private readonly QueenSettings settings;
        private readonly PrestigeManager prestige;
        private readonly double[] thresholds;
        private readonly int[] levels;

        public QueenManager(QueenSettings settings, PrestigeManager prestige)
        {
            this.settings = settings;
            this.prestige = prestige;
            thresholds = QueenMath.Thresholds(level => settings.XpForLevel.Evaluate(level), settings.MaxLevel);
            levels = new int[settings.Abilities.Count];
            prestige.OnJellyChanged += HandleJellyChanged;
            Level = QueenMath.Level(prestige.LifetimeJelly, thresholds);
        }

        /// <summary>Queen level; 0 before the first Royal Jelly.</summary>
        public int Level { get; private set; }

        public int MaxLevel => thresholds.Length;

        public bool IsMaxLevel => Level >= MaxLevel;

        /// <summary>Permanent honey factor from the Queen level (1 = no bonus).</summary>
        public double HoneyMultiplier => QueenMath.HoneyMultiplier(Level, settings.HoneyBonusPerLevel);

        /// <summary>Honey factor one level up, for the panel.</summary>
        public double NextHoneyMultiplier => QueenMath.HoneyMultiplier(Math.Min(Level + 1, MaxLevel), settings.HoneyBonusPerLevel);

        /// <summary>Lifetime jelly the next level needs; -1 at the top level.</summary>
        public double NextLevelJelly => QueenMath.NextThreshold(Level, thresholds);

        public int AbilityCount => levels.Length;

        public IReadOnlyList<QueenAbility> Abilities => settings.Abilities;

        /// <summary>Raised when the Queen level, an ability level or the Royal Jelly balance changes.</summary>
        public event Action OnChanged;

        /// <summary>Raised after an ability was bought: its index.</summary>
        public event Action<int> OnAbilityBought;

        public int AbilityLevel(int index) => levels[index];

        public bool IsMaxed(int index) => levels[index] >= settings.Abilities[index].MaxLevel;

        /// <summary>Royal Jelly price of the next level of ability <paramref name="index"/>.</summary>
        public BigNumber AbilityCost(int index)
        {
            QueenAbility ability = settings.Abilities[index];
            return UpgradeMath.Cost(ability.BaseCost, ability.CostMultiplier, levels[index]);
        }

        public bool CanBuy(int index) => !IsMaxed(index) && prestige.RoyalJelly >= AbilityCost(index);

        /// <summary>Spends Royal Jelly on the next level of ability <paramref name="index"/>.</summary>
        public bool TryBuy(int index)
        {
            if (index < 0 || index >= levels.Length || IsMaxed(index))
                return false;
            BigNumber cost = AbilityCost(index);
            if (prestige.RoyalJelly < cost)
                return false;
            // Level first: spending raises OnJellyChanged, and listeners must see the new level.
            levels[index]++;
            prestige.TrySpendJelly(cost);
            OnAbilityBought?.Invoke(index);
            return true;
        }

        /// <summary>Summed effect of every ability with <paramref name="effect"/>, in the effect's unit.</summary>
        public double EffectTotal(QueenEffect effect)
        {
            double total = 0;
            IReadOnlyList<QueenAbility> abilities = settings.Abilities;
            for (int i = 0; i < levels.Length; i++)
            {
                if (abilities[i].Effect == effect)
                    total += QueenMath.AbilityEffect(abilities[i].EffectPerLevel, levels[i]);
            }
            return total;
        }

        /// <summary>Loads saved ability levels by id. Unknown ids are dropped; levels are clamped to the current max.</summary>
        public void Restore(AbilityLevel[] saved)
        {
            Array.Clear(levels, 0, levels.Length);
            if (saved != null)
            {
                IReadOnlyList<QueenAbility> abilities = settings.Abilities;
                for (int s = 0; s < saved.Length; s++)
                {
                    int index = IndexOf(saved[s].id);
                    if (index >= 0)
                        levels[index] = QueenMath.ClampAbilityLevel(saved[s].level, abilities[index].MaxLevel);
                }
            }
            Level = QueenMath.Level(prestige.LifetimeJelly, thresholds);
            OnChanged?.Invoke();
        }

        /// <summary>Ability levels for the save; level-0 abilities are left out.</summary>
        public AbilityLevel[] Capture()
        {
            int count = 0;
            for (int i = 0; i < levels.Length; i++)
            {
                if (levels[i] > 0)
                    count++;
            }
            var saved = new AbilityLevel[count];
            IReadOnlyList<QueenAbility> abilities = settings.Abilities;
            for (int i = 0, s = 0; i < levels.Length; i++)
            {
                if (levels[i] > 0)
                    saved[s++] = new AbilityLevel { id = abilities[i].Id, level = levels[i] };
            }
            return saved;
        }

        public void Dispose() => prestige.OnJellyChanged -= HandleJellyChanged;

        private int IndexOf(string id)
        {
            IReadOnlyList<QueenAbility> abilities = settings.Abilities;
            for (int i = 0; i < abilities.Count; i++)
            {
                if (abilities[i].Id == id)
                    return i;
            }
            return -1;
        }

        private void HandleJellyChanged()
        {
            Level = QueenMath.Level(prestige.LifetimeJelly, thresholds);
            OnChanged?.Invoke();
        }
    }
}
