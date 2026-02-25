using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a set of unique <see cref="DataPath"/> objects, with utilities for set operations such as union and intersection.
    /// </summary>
    /*public*/ class DataPathSet
    {
        /// <summary>
        /// Check if the set is empty.
        /// </summary>
        public bool Empty => m_DataPaths.Count == 0;

        /// <summary>
        /// Gets an enumerable collection of all <see cref="IDataKey"/> objects in the set.
        /// </summary>
        public IEnumerable<DataPath> DataPaths => m_DataPaths;

        /// <summary>
        /// Determines whether the set contains the specified <see cref="IDataKey"/>.
        /// </summary>
        /// <param name="path">The <see cref="DataPath"/> to check for containment.</param>
        /// <returns>
        /// <see langword="true"/> if the set contains the specified <see cref="IDataKey"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Contains(DataPath path) => m_DataPaths.Contains(path);

        /// <summary>
        /// Adds the specified <see cref="IDataKey"/> to the set.
        /// </summary>
        /// <param name="path">The <see cref="DataPath"/> to add to the set.</param>
        /// <returns>
        /// <see langword="true"/> if the <see cref="IDataKey"/> was added;
        /// <see langword="false"/> if it was already in the set.
        /// </returns>
        public bool Add(DataPath path) => m_DataPaths.Add(path);

        /// <summary>
        /// Adds all <see cref="IDataKey"/> objects from the provided <see cref="DataPathSet"/> to the current set.
        /// </summary>
        /// <param name="dataPathSet">The set of <see cref="IDataKey"/> objects to add.</param>
        public void Add(DataPathSet dataPathSet)
        {
            foreach (var path in dataPathSet.DataPaths)
            {
                m_DataPaths.Add(path);
            }
        }

        /// <summary>
        /// Removes all <see cref="DataPath"/> objects from the current set that exist in the provided <see cref="DataPathSet"/>.
        /// </summary>
        /// <param name="dataPathSet">The set of <see cref="DataPath"/> objects to remove.</param>
        public void Remove(DataPathSet dataPathSet)
        {
            foreach (var path in dataPathSet.DataPaths)
            {
                m_DataPaths.Remove(path);
            }
        }

        /// <summary>
        /// Creates a new <see cref="DataPathSet"/> containing the union of two sets.
        /// </summary>
        /// <param name="setA">The first set.</param>
        /// <param name="setB">The second set.</param>
        /// <returns>
        /// A new <see cref="DataPathSet"/> that contains all <see cref="DataPath"/> objects from both <paramref name="setA"/> and <paramref name="setB"/>.
        /// </returns>
        public static DataPathSet Union(DataPathSet setA, DataPathSet setB)
        {
            DataPathSet union = new DataPathSet();
            union.Add(setA);
            union.Add(setB);
            return union;
        }

        /// <summary>
        /// Creates a new <see cref="DataPathSet"/> containing the intersection of two sets.
        /// </summary>
        /// <param name="setA">The first set.</param>
        /// <param name="setB">The second set.</param>
        /// <returns>
        /// A new <see cref="DataPathSet"/> that contains only the <see cref="DataPath"/> objects present in both <paramref name="setA"/> and <paramref name="setB"/>.
        /// </returns>
        public static DataPathSet Intersection(DataPathSet setA, DataPathSet setB)
        {
            DataPathSet intersection = new DataPathSet();
            foreach (var path in setA.DataPaths)
            {
                if (setB.Contains(path))
                {
                    intersection.Add(path);
                }
            }
            return intersection;
        }

        /// <summary>
        /// The internal hash set that stores the <see cref="DataPath"/> objects.
        /// </summary>
        private HashSet<DataPath> m_DataPaths = new();
    }
}

