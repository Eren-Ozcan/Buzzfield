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
        /// <summary>1: first release layout (Phase 4). 2: store entitlements. 3: Queen abilities by id. 4: shaken flower count.</summary>
        public const int CurrentVersion = 4;

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

            // Version 2 adds entitlements. Older saves predate the store, so nothing is
            // owned; Normalize has already filled the empty object.
            if (savedVersion < 2)
                data.entitlements = new Entitlements();

            // Version 3 replaces the index-based ability list with levels keyed by id. No
            // build could buy abilities before it, so the old list was always empty and there
            // is nothing to carry over; Normalize has already filled the empty array.
            if (savedVersion < 3)
                data.abilities = Array.Empty<AbilityLevel>();

            // Version 4 adds stats.flowersShaken (the pollen shake replaced the tap boost).
            // JsonUtility reads the missing field as 0, so older players see the swipe hint once.

            data.saveVersion = CurrentVersion;
            return true;
        }
    }
}
