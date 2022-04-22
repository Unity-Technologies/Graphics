using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class ListExtensions
    {
        /// <summary>
        /// Reorders some elements in a list following <see cref="ReorderType"/>.
        /// </summary>
        /// <param name="list">The list that will get reordered.</param>
        /// <param name="elements">The elements to move.</param>
        /// <param name="reorderType">The way to move elements</param>
        /// <typeparam name="T">The type of elements to move.</typeparam>
        public static void ReorderElements<T>(this List<T> list, IReadOnlyList<T> elements, ReorderType reorderType)
        {
            if (elements == null || elements.Count == 0 || list.Count <= 1)
                return;

            bool increaseIndices = reorderType == ReorderType.MoveDown || reorderType == ReorderType.MoveLast;
            bool moveAllTheWay = reorderType == ReorderType.MoveLast || reorderType == ReorderType.MoveFirst;

            var nextEndIdx = increaseIndices ? list.Count - 1 : 0;

            for (var j = 1; j < list.Count; j++)
            {
                int i = increaseIndices ? list.Count - 1 - j : j;
                if (!elements.Contains(list[i]))
                    continue;

                var moveToIdx = increaseIndices ? i + 1 : i - 1;
                if (moveAllTheWay)
                {
                    while (elements.Contains(list[nextEndIdx]) && nextEndIdx != i)
                        nextEndIdx += increaseIndices ? -1 : 1;

                    if (nextEndIdx == i)
                        continue;

                    moveToIdx = nextEndIdx;
                }
                else
                {
                    if (elements.Contains(list[moveToIdx]))
                        continue;
                }

                var element = list[i];
                list.RemoveAt(i);
                list.Insert(moveToIdx, element);
            }
        }

    }
}
