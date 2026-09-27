using System;

namespace Buzzfield.Core
{
    /// <summary>
    /// Brings older saves up to the current layout. Add one step per version bump:
    /// <c>if (savedVersion &lt; 2) { ... }</c>, oldest first, so a save several versions
    /// behind runs every step in order.
    /// </summary>
    public static class SaveMigration
    {
        /// <summary>1: first release layout (Phase 4).</summary>
        public const int CurrentVersion = 1;

        /// <summary>
        /// Upgrades a loaded save in place; returns true when anything changed.
        /// A save from a newer build is left as it is: its unknown fields are already dropped.
        /// </summary>
        public static bool Upgrade(SaveData data, int savedVersion)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            data.Normalize();
            if (savedVersion >= CurrentVersion)
                return false;

            // Version 0: development saves written before saveVersion existed. JsonUtility
            // reads the missing field as 0; the layout is the same as version 1.
            data.saveVersion = CurrentVersion;
            return true;
        }
    }
}
