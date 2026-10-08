#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Buzzfield.Core;
using Buzzfield.Flowers;
using UnityEngine;

namespace Buzzfield.Game
{
    /// <summary>
    /// Shortcuts for trying the game by hand (Buzzfield > Play Test Tools). Editor and
    /// development builds only; a release build leaves them out.
    /// </summary>
    public sealed partial class GameManager
    {
        /// <summary>Adds honey outside the run total, like a store grant.</summary>
        public void DebugGrantHoney(BigNumber amount) => economy.Grant(amount);

        /// <summary>Adds Dewdrops; the crystals fly from the goal pill (the editor window has its own Screen size).</summary>
        public void DebugGrantDew(long amount) =>
            EarnDew(amount, "debug", RectTransformUtility.WorldToScreenPoint(null, goalView.Pill.position));

        /// <summary>Blooms the active, unbloomed flower nearest the hive; false when none is left.</summary>
        public bool DebugBloomNext()
        {
            Flower next = null;
            float nearest = float.MaxValue;
            Vector2 hive = prestige.CurrentGarden.HivePosition;
            IReadOnlyList<Flower> flowers = flowerManager.Flowers;
            for (int i = 0; i < flowers.Count; i++)
            {
                Flower flower = flowers[i];
                if (!flower.IsActive || flower.IsBloomed)
                    continue;
                float distance = (flower.GardenPosition - hive).sqrMagnitude;
                if (distance < nearest)
                {
                    nearest = distance;
                    next = flower;
                }
            }
            if (next == null)
                return false;
            bloom.AddNectar(next, next.Type.NectarToBloom);
            return true;
        }

        /// <summary>Blooms flowers until the garden is complete (sprouts wake as their neighbours bloom).</summary>
        public void DebugCompleteGarden()
        {
            while (!bloom.IsComplete)
            {
                if (!DebugBloomNext())
                    return;
            }
        }

        /// <summary>Gives every flower its pollen back.</summary>
        public void DebugRefillPollen() => pollen.Refill();

        /// <summary>Sends the butterfly across the garden now, whatever the play time; false when one is flying.</summary>
        public bool DebugLaunchButterfly() => visitors.LaunchNow(garden.Bounds);

        /// <summary>Shows the swipe hint again, as for a new player.</summary>
        public void DebugResetSwipeHint()
        {
            stats.flowersShaken = 0;
            RefreshSwipeHint();
        }
    }
}
#endif
