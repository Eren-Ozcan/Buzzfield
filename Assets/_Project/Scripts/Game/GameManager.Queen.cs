using Buzzfield.Core;
using Buzzfield.Economy;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// Queen level and Queen abilities: applies their bonuses to the systems they change
    /// whenever the Queen level, an ability or the Royal Jelly balance changes.
    /// </summary>
    public sealed partial class GameManager
    {
        [Header("Queen")]
        [SerializeField] private QueenSettings queenSettings;

        private QueenManager queen;

        public QueenManager Queen => queen;

        /// <summary>Workers every garden starts with: the base count plus Royal Brood.</summary>
        public int StartingWorkers => beeSettings.StartingBees + (int)System.Math.Round(queen.EffectTotal(QueenEffect.StartingWorkers));

        /// <summary>Longest absence that pays, Sweet Memory included.</summary>
        public double OfflineCapHours => offlineSettings.CapHours + queen.EffectTotal(QueenEffect.OfflineCapHours);

        /// <summary>Needs the economy, upgrades, bloom and prestige managers.</summary>
        private void InitQueen()
        {
            queen = new QueenManager(queenSettings, prestige);
            queen.OnChanged += ApplyQueenEffects;
            queen.OnAbilityBought += HandleAbilityBought;
            ApplyQueenEffects();
        }

        private void DisposeQueen()
        {
            if (queen == null)
                return;
            queen.OnChanged -= ApplyQueenEffects;
            queen.OnAbilityBought -= HandleAbilityBought;
            queen.Dispose();
        }

        private void ApplyQueenEffects()
        {
            economy.QueenMultiplier = queen.HoneyMultiplier;
            upgrades.QueenSpeedMultiplier = (float)QueenMath.PercentFactor(queen.EffectTotal(QueenEffect.FlightSpeedPercent));
            bloom.BloomPerVisitMultiplier = (float)QueenMath.PercentFactor(queen.EffectTotal(QueenEffect.BloomPerVisitPercent));
        }

        /// <summary>
        /// Royal Brood also helps the garden the player is in: each level bought adds its
        /// Workers right away instead of only from the next garden.
        /// </summary>
        private void HandleAbilityBought(int index)
        {
            QueenAbility ability = queen.Abilities[index];
            if (ability.Effect == QueenEffect.StartingWorkers)
            {
                int workers = Mathf.RoundToInt(ability.EffectPerLevel);
                for (int i = 0; i < workers; i++)
                    beeManager.Spawn(0);
            }
            SaveNow();
        }
    }
}
