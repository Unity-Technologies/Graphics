using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    static class ListUtilities
    {
        // Ideally, we should build a non-yield return, struct version of EnumerateRange
        public static IEnumerable<T> EnumerateRange<T>(this List<T> list, int startIndex, int count)
        {
            ValidateRange(list, startIndex, count);
            return Iterate(list, startIndex, startIndex + count);

            static IEnumerable<T> Iterate(List<T> list, int startIndex, int endIndex)
            {
                for (int i = startIndex; i < endIndex; i++)
                    yield return list[i];
            }
        }

        public static int RemoveAllFromRange<T>(this List<T> list, Predicate<T> match, int startIndex, int count)
        {
            ValidateRange(list, startIndex, count);

            int endIndex = startIndex + count;
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

        static void ValidateRange<T>(List<T> list, int startIndex, int count)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, "Must be non-negative.");
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "Must be non-negative.");
            if (list.Count - startIndex < count)
                throw new ArgumentException($"Range starting at {startIndex} with count {count} is outside the list of {list.Count} elements.");
        }
    }
}
