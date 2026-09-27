using System.Collections.Generic;
using Buzzfield.Flowers;
using UnityEngine;

namespace Buzzfield.Economy
{
    /// <summary>Garden order and the "Move the Queen" prestige (design doc section 3b).</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Prestige Settings", fileName = "PrestigeSettings")]
    public sealed class PrestigeSettings : ScriptableObject
    {
        [Tooltip("Authored gardens in play order. After the last one it repeats with a value bonus.")]
        [SerializeField] private List<GardenConfig> gardens = new List<GardenConfig>();
        [Tooltip("Extra garden value per loop past the last authored garden (0.25 = +25%).")]
        [SerializeField, Min(0f)] private float loopValueBonus = 0.25f;

        [Header("Move the Queen")]
        [Tooltip("Bloom fraction that unlocks the move.")]
        [SerializeField, Range(0f, 1f)] private float moveUnlockBloom = 0.6f;

        [Header("Royal Jelly = floor(jellyScale * sqrt(runHoney / jellyBase)) * (1 + bloomBonus)")]
        [SerializeField, Min(0f)] private float jellyScale = 1f;
        [SerializeField] private double jellyBase = 1000;
        [Tooltip("Bloom bonus reached at 100% bloom, growing linearly from the unlock threshold.")]
        [SerializeField, Min(0f)] private float bloomBonusAtFull = 0.5f;
        [Tooltip("Flat bonus added on top when the garden is complete.")]
        [SerializeField, Min(0f)] private float gardenCompleteBonus = 0.5f;

        public IReadOnlyList<GardenConfig> Gardens => gardens;
        public float LoopValueBonus => loopValueBonus;
        public float MoveUnlockBloom => moveUnlockBloom;
        public float JellyScale => jellyScale;
        public double JellyBase => jellyBase;
        public float BloomBonusAtFull => bloomBonusAtFull;
        public float GardenCompleteBonus => gardenCompleteBonus;
    }
}
