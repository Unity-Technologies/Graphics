using System;
using System.Collections.Generic;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    [Obsolete("0.10+ This class will be removed from GTF public API")]
    public static class HashSetExtensions
    {
        [Obsolete("0.10+ This method will be removed from GTF public API")]
        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> entriesToAdd)
        {
            AddRangeInternal(set, entriesToAdd);
        }

        internal static void AddRangeInternal<T>(this HashSet<T> set, IEnumerable<T> entriesToAdd)
        {
            foreach (var entry in entriesToAdd)
            {
                set.Add(entry);
            }
        }
    }
}
