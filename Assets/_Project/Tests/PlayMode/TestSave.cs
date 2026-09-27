using System.IO;
using Buzzfield.Save;
using UnityEngine;

namespace Buzzfield.Tests.PlayMode
{
    /// <summary>
    /// Points the game's save at a temp folder, so tests never read or overwrite the
    /// real save and every test starts a new game.
    /// </summary>
    internal static class TestSave
    {
        /// <summary>GameSettings.saveFileName of the default data.</summary>
        public const string FileName = "buzzfield_save.json";

        public static string Directory => Path.Combine(Application.temporaryCachePath, "bz_playmode_save");

        /// <summary>Call before loading Main: removes any save left by an earlier test.</summary>
        public static void Clear()
        {
            SaveManager.DirectoryOverride = Directory;
            if (System.IO.Directory.Exists(Directory))
                System.IO.Directory.Delete(Directory, true);
        }

        /// <summary>A second handle on the same files, for editing a save between scene loads.</summary>
        public static SaveManager Files()
        {
            SaveManager.DirectoryOverride = Directory;
            return new SaveManager(FileName);
        }
    }
}
