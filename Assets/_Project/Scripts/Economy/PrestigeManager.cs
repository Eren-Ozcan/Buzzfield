using System;
using Buzzfield.Core;
using Buzzfield.Flowers;

namespace Buzzfield.Economy
{
    /// <summary>
    /// State that survives a Queen move: Royal Jelly, lifetime jelly (Queen XP) and the
    /// garden index. Answers the gate, cost and reward questions; GameManager performs the
    /// actual reset because it owns every system that is reset.
    /// </summary>
    public sealed class PrestigeManager
    {
        private readonly PrestigeSettings settings;

        public PrestigeManager(PrestigeSettings settings)
        {
            if (settings.Gardens.Count == 0)
                throw new ArgumentException("PrestigeSettings has no gardens.", nameof(settings));
            this.settings = settings;
        }

        public BigNumber RoyalJelly { get; private set; }

        /// <summary>Every Royal Jelly ever earned; the Queen level is read from this.</summary>
        public BigNumber LifetimeJelly { get; private set; }

        /// <summary>Gardens moved through so far; 0 = the first garden. Keeps counting past the authored ones.</summary>
        public int GardenIndex { get; private set; }

        public int MovesMade { get; private set; }

        public float MoveUnlockBloom => settings.MoveUnlockBloom;

        public GardenConfig CurrentGarden =>
            settings.Gardens[PrestigeMath.ConfigIndex(GardenIndex, settings.Gardens.Count)];

        /// <summary>Extra factor on garden value and move cost once the last authored garden repeats.</summary>
        public double LoopMultiplier => PrestigeMath.LoopMultiplier(GardenIndex, settings.Gardens.Count, settings.LoopValueBonus);

        /// <summary>Nectar value factor of the current garden, loop bonus included.</summary>
        public float GardenValueMultiplier => (float)(CurrentGarden.GardenValueMultiplier * LoopMultiplier);

        public BigNumber MoveCost => BigNumber.FromDouble(CurrentGarden.MoveHoneyCost) * LoopMultiplier;

        /// <summary>Raised after a move has been committed: the jelly gained.</summary>
        public event Action<BigNumber> OnQueenMoved;

        public bool IsUnlocked(float bloom) => PrestigeMath.IsUnlocked(bloom, settings.MoveUnlockBloom);

        public bool CanMove(float bloom, BigNumber honey) => IsUnlocked(bloom) && honey >= MoveCost;

        public double BloomBonus(float bloom) =>
            PrestigeMath.BloomBonus(bloom, settings.MoveUnlockBloom, settings.BloomBonusAtFull, settings.GardenCompleteBonus);

        /// <summary>Royal Jelly a move would give right now.</summary>
        public BigNumber PreviewJelly(BigNumber runHoney, float bloom) =>
            PrestigeMath.Jelly(runHoney, settings.JellyBase, settings.JellyScale, BloomBonus(bloom));

        /// <summary>Credits the jelly and advances to the next garden. The caller resets the run.</summary>
        public void CommitMove(BigNumber jelly)
        {
            if (!jelly.IsNegative)
            {
                RoyalJelly += jelly;
                LifetimeJelly += jelly;
            }
            GardenIndex++;
            MovesMade++;
            OnQueenMoved?.Invoke(jelly);
        }
    }
}
