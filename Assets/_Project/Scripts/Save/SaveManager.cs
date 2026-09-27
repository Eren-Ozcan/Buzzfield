using System;
using System.IO;
using Buzzfield.Core;
using UnityEngine;

namespace Buzzfield.Save
{
    /// <summary>
    /// Reads and writes <see cref="SaveData"/> as checksummed JSON in
    /// Application.persistentDataPath. A damaged main file falls back to the backup, a
    /// damaged backup to a new game; loading never throws.
    /// </summary>
    public sealed class SaveManager
    {
        private readonly SaveFileStore store;

        /// <summary>Tests point this at a temp folder before the scene loads; null = persistentDataPath.</summary>
        public static string DirectoryOverride { get; set; }

        public SaveManager(string fileName)
        {
            store = new SaveFileStore(DirectoryOverride ?? Application.persistentDataPath, fileName);
        }

        public SaveSource LastLoadSource { get; private set; }
        public string MainPath => store.MainPath;
        public string BackupPath => store.BackupPath;

        /// <summary>The saved game, migrated to the current version, or null for a new game.</summary>
        public SaveData Load()
        {
            SaveData loaded = null;
            try
            {
                LastLoadSource = store.Read(text => TryParse(text, out loaded));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save could not be read ({e.GetType().Name}: {e.Message}); starting a new game.");
                LastLoadSource = SaveSource.Unreadable;
                return null;
            }

            switch (LastLoadSource)
            {
                case SaveSource.Backup:
                    Debug.LogWarning("Main save is damaged; loaded the backup.");
                    break;
                case SaveSource.Unreadable:
                    Debug.LogWarning("Save and backup are damaged; starting a new game.");
                    return null;
                case SaveSource.None:
                    return null;
            }
            SaveMigration.Upgrade(loaded, loaded.saveVersion);
            return loaded;
        }

        /// <summary>Writes the save; returns false (and logs a warning) when the disk refuses.</summary>
        public bool Save(SaveData data)
        {
            data.saveVersion = SaveMigration.CurrentVersion;
            try
            {
                store.Write(SaveEnvelope.Wrap(JsonUtility.ToJson(data)));
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"Save failed ({e.GetType().Name}: {e.Message}).");
                return false;
            }
        }

        public void Delete() => store.Delete();

        private static bool TryParse(string text, out SaveData data)
        {
            data = null;
            if (!SaveEnvelope.TryUnwrap(text, out string json))
                return false;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }
            return data != null;
        }
    }
}
