using System.Collections.Generic;

namespace Buzzfield.Core
{
    /// <summary>Which bees the Evolve upgrade merges: three of one tier into one of the next, lowest tier first.</summary>
    public static class EvolveRules
    {
        public const int BeesPerMerge = 3;

        /// <summary>
        /// Lowest tier that has enough bees and is not the top tier, or -1 when nothing can merge.
        /// <paramref name="tierCounts"/> holds bees per tier, lowest tier first.
        /// </summary>
        public static int SourceTier(IReadOnlyList<int> tierCounts)
        {
            for (int tier = 0; tier < tierCounts.Count - 1; tier++)
            {
                if (tierCounts[tier] >= BeesPerMerge)
                    return tier;
            }
            return -1;
        }
    }
}
