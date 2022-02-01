using System;
using System.Collections.Generic;

namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    // ReSharper disable once InconsistentNaming
    [Obsolete("0.10+ This class will be removed from GTF public API")]
    public static class IEnumerableExtensions
    {
        [Obsolete("0.10+ This method will be removed from GTF public API")]
        public static int IndexOf<T>(this IEnumerable<T> source, T element)
        {
            return IndexOfInternal(source, element);
        }

        internal static int IndexOfInternal<T>(this IEnumerable<T> source, T element)
        {
            if (source is IList<T> list)
                return list.IndexOf(element);

            int i = 0;
            foreach (var x in source)
            {
                if (Equals(x, element))
                    return i;
                i++;
            }

            return -1;
        }
    }
}
