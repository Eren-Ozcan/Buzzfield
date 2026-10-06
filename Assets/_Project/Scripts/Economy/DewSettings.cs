using UnityEngine;

namespace Buzzfield.Economy
{
    /// <summary>Dewdrops, the premium currency: what the game gives away. Goal rewards live in <see cref="GoalSettings"/>.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Dew Settings", fileName = "DewSettings")]
    public sealed class DewSettings : ScriptableObject
    {
        [Tooltip("Dewdrops for every garden bloomed to 100%.")]
        [SerializeField, Min(0)] private int gardenCompleteReward = 10;

        public int GardenCompleteReward => gardenCompleteReward;
    }
}
