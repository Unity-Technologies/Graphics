using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    static class ListUtilities
    {
        // Ideally, we should build a non-yield return, struct version of Slice
        public static IEnumerable<T> Slice<T>(this List<T> list, int start, int end)
        {
            for (int i = start; i < end; i++)
                yield return list[i];
        }

        public static int RemoveAllFromRange<T>(this List<T> list, Predicate<T> match, int startIndex, int count)
        {
            // match behavior of RemoveRange
            if ((startIndex < 0) || (count < 0))
                throw new ArgumentOutOfRangeException();

            int endIndex = startIndex + count;
            if (endIndex > list.Count)
                throw new ArgumentException();

            int readIndex = startIndex;
            int writeIndex = startIndex;
            while (readIndex < endIndex)
            {
                T element = list[readIndex];
                bool remove = match(element);
                if (!remove)
                {
                    // skip some work if nothing removed (especially if T is a large struct)
                    if (writeIndex < readIndex)
                        list[writeIndex] = element;
                    writeIndex++;
                }
                readIndex++;
            }

            // once we're done, we can remove the entries at the end in one operation
            int numberRemoved = readIndex - writeIndex;
            if (numberRemoved > 0)
            {
                list.RemoveRange(writeIndex, numberRemoved);
            }

            return numberRemoved;
        }
    }
}
