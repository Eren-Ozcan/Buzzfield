using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>App-level settings that belong to no single system.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Game Settings", fileName = "GameSettings")]
    public sealed class GameSettings : ScriptableObject
    {
        [SerializeField, Min(30)] private int targetFrameRate = 60;
        [SerializeField, Min(5f)] private float autosaveIntervalSeconds = 30f;

        public int TargetFrameRate => targetFrameRate;
        public float AutosaveIntervalSeconds => autosaveIntervalSeconds;
    }
}
