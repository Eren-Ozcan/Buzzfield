using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>App-level settings that belong to no single system.</summary>
    [CreateAssetMenu(menuName = "Buzzfield/Game Settings", fileName = "GameSettings")]
    public sealed class GameSettings : ScriptableObject
    {
        [SerializeField, Min(30)] private int targetFrameRate = 60;
        [SerializeField, Min(5f)] private float autosaveIntervalSeconds = 30f;
        [SerializeField] private string saveFileName = "buzzfield_save.json";
        [Tooltip("Ask a time server for UTC, so offline earnings survive device clock changes and reboots.")]
        [SerializeField] private bool fetchTrustedTime = true;

        public int TargetFrameRate => targetFrameRate;
        public float AutosaveIntervalSeconds => autosaveIntervalSeconds;
        public string SaveFileName => saveFileName;
        public bool FetchTrustedTime => fetchTrustedTime;
    }
}
