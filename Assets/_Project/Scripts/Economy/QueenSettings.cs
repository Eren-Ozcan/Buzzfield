using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Economy
{
    /// <summary>Queen level curve and the list of abilities shown in the Queen panel.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Queen Settings", fileName = "QueenSettings")]
    public sealed class QueenSettings : ScriptableObject
    {
        [Tooltip("Royal Jelly (lifetime) needed to reach each level: x = level, y = total jelly.")]
        [SerializeField] private AnimationCurve xpForLevel = AnimationCurve.Linear(1f, 1f, 50f, 5000f);
        [Tooltip("Permanent honey multiplier gained per Queen level (0.1 = +10%).")]
        [SerializeField, Min(0f)] private float honeyBonusPerLevel = 0.1f;
        [SerializeField] private List<QueenAbility> abilities = new List<QueenAbility>();

        public AnimationCurve XpForLevel => xpForLevel;
        public float HoneyBonusPerLevel => honeyBonusPerLevel;
        public IReadOnlyList<QueenAbility> Abilities => abilities;
    }
}
