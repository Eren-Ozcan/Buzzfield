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
        /// <summary>
        /// 1: first release layout (Phase 4). 2: store entitlements. 3: Queen abilities by id. 4: shaken
        /// flower count. 5: player settings, goal index and the first-return gift. 6: Dewdrops and the
        /// endless goals' starting count. 7: Dewdrop honey boosts and the granted store orders.
        /// </summary>
        public const int CurrentVersion = 7;

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

            // Version 5 adds the settings panel, the goal chain and the first-return gift. Older
            // saves get the default settings (sound and music on); a missing bool would read as
            // off. The goal chain starts at its first goal and the gift is still to come.
            if (savedVersion < 5)
            {
                data.settings = new PlayerSettings();
                data.goalIndex = 0;
                data.welcomeGiftGiven = false;
            }

            // Version 6 adds Dewdrops and goalBase. Nobody had Dewdrops before, and no save could
            // be past the authored goal chain yet, so JsonUtility's zeros are right.

            // Version 7 adds the Dewdrop boost line and the granted store order ids. Nobody could
            // buy either before it; Normalize has already filled the empty array and list.

            data.saveVersion = CurrentVersion;
            return true;
        }
    }
}
