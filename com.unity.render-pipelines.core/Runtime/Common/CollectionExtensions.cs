using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A set of extension methods for collections
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Removes a given count from the back of the given list
        /// </summary>
        /// <param name="inputList">The list to be removed</param>
        /// <param name="elements">The number of elements to be removed from the back</param>
        public static void RemoveBack<T>([NotNull] this IList<T> inputList, int elementsToRemoveFromBack)
        {
            // Collection is empty
            var count = inputList.Count;
            if (count == 0)
                return;

            // Nothing to remove or negative numbers
            if (elementsToRemoveFromBack < 1)
                return;

            elementsToRemoveFromBack = Mathf.Min(elementsToRemoveFromBack, count);

            if (inputList is List<T> genericList)
            {
                genericList.RemoveRange(count - elementsToRemoveFromBack, elementsToRemoveFromBack);
            }
            else
            {
                for (var i = count - 1; i >= elementsToRemoveFromBack; --i)
                    inputList.RemoveAt(i);
            }
        }
    }
}
