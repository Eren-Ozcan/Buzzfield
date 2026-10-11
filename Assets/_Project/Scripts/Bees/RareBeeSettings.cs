using System.Collections.Generic;
using UnityEngine;

namespace Buzzfield.Bees
{
    /// <summary>
    /// The rare bee species shown in the Hive panel, their shared level costs and the hive's way in
    /// (docs/DEWDROPS.md 4.2): the round hive button and a tap on the hive in the garden.
    /// </summary>
    [CreateAssetMenu(menuName = "Buzzfield/Rare Bee Settings", fileName = "RareBeeSettings")]
    public sealed class RareBeeSettings : ScriptableObject
    {
        [Tooltip("Shown in the Hive panel in this order. Add an asset here (and rebuild the scene) to add a species.")]
        [SerializeField] private List<RareBee> bees = new List<RareBee>();
        [Tooltip("Dewdrops for each level after joining: level 1 to 2, 2 to 3, and so on; the last entry repeats.")]
        [SerializeField] private List<int> levelCosts = new List<int> { 20, 30, 45, 65, 90, 120, 160, 210, 270 };
        [Tooltip("Garden number from which the hive button shows and the Hive panel opens (2 = after the first Queen move).")]
        [SerializeField, Min(1)] private int hiveUnlockGarden = 2;

        [Header("Tap on the hive")]
        [Tooltip("A tap this close to the hive's middle (world units, at the hive's scale) opens the Hive panel.")]
        [SerializeField, Min(0.1f)] private float hiveTapRadius = 1f;
        [Tooltip("Height of the hive's middle above its base, in world units at the hive's scale.")]
        [SerializeField, Min(0f)] private float hiveTapHeight = 0.7f;
        [Tooltip("A press that travels farther than this share of the screen height is a swipe, not a tap.")]
        [SerializeField, Range(0f, 0.2f)] private float tapSlop = 0.03f;

        public IReadOnlyList<RareBee> Bees => bees;
        public IReadOnlyList<int> LevelCosts => levelCosts;
        public int HiveUnlockGarden => hiveUnlockGarden;
        public float HiveTapRadius => hiveTapRadius;
        public float HiveTapHeight => hiveTapHeight;
        public float TapSlop => tapSlop;
    }
}
