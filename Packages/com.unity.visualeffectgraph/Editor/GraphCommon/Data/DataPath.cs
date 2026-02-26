using System;
using System.Collections.Generic;
using System.Text;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a hierarchical data path composed of a sequence of <see cref="IDataKey"/> elements.
    /// </summary>
    /*public*/ class DataPath : IEquatable<DataPath>
    {
        /// <summary>
        /// Represents an empty data path.
        /// </summary>
        public static DataPath Empty = new DataPath();

        /// <summary>
        /// Checks if this data path is empty.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the path contains no elements; otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsEmpty()
        {
            return this.Equals(Empty);
        }

        /// <summary>
        /// Gets the sequence of data keys that make up this path, from root to leaf.
        /// </summary>
        /// <value>
        /// A read-only span containing the sequence of <see cref="IDataKey"/> elements.
        /// </value>
        public ReadOnlySpan<IDataKey> PathSequence
        {
            get
            {
                s_PathSequenceScratch[m_Depth] = m_SelfDataKey;
                var parentPath = m_ParentDataPath;
                while(parentPath != null)
                {
                    s_PathSequenceScratch[parentPath.m_Depth] = parentPath.m_SelfDataKey;
                    parentPath = parentPath.m_ParentDataPath;
                }

                return new ReadOnlySpan<IDataKey>(s_PathSequenceScratch, 0, (int)(m_Depth + 1));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPath"/> class with a parent path and a data key.
        /// </summary>
        /// <param name="parentDataPath">The parent data path.</param>
        /// <param name="selfDataKey">The data key for this path segment.</param>
        public DataPath(DataPath parentDataPath, IDataKey selfDataKey)
        {
            m_ParentDataPath = parentDataPath;
            m_SelfDataKey = selfDataKey;
            m_Depth = m_ParentDataPath.m_Depth + 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPath"/> class with a single data key.
        /// </summary>
        /// <param name="selfDataKey">The data key for this path.</param>
        public DataPath(IDataKey selfDataKey)
        {
            m_ParentDataPath = Empty;
            m_SelfDataKey = selfDataKey;
            m_Depth = m_ParentDataPath.m_Depth + 1;
        }

        internal DataPath(ReadOnlySpan<IDataKey> pathSequence)
        {
            var parentDataPath = Empty;
            foreach (var key in pathSequence)
            {
                var dataPath = new DataPath(parentDataPath, key);
                parentDataPath = dataPath;
            }

            m_ParentDataPath = parentDataPath.m_ParentDataPath;
            m_SelfDataKey = parentDataPath.m_SelfDataKey;
            m_Depth = parentDataPath.m_Depth;
        }

        /// <summary>
        /// Returns a partial data path starting from the specified index.
        /// </summary>
        /// <param name="start">The zero-based index to start the partial path from (exclusive).</param>
        /// <returns>
        /// A new <see cref="DataPath"/> representing the partial path sequence starting after the specified index.
        /// </returns>
        public DataPath GetPartialPath(int start)
        {
            ReadOnlySpan<IDataKey> partialSequence = PathSequence.Slice(start + 1);
            return new DataPath(partialSequence);
        }

        /// <summary>
        /// Determines whether the specified <see cref="DataPath"/> is equal to the current <see cref="DataPath"/>.
        /// </summary>
        /// <param name="other">The <see cref="DataPath"/> to compare with the current <see cref="DataPath"/>.</param>
        /// <returns>
        /// <see langword="true"/> if the specified <see cref="DataPath"/> is equal to the current <see cref="DataPath"/>;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(DataPath other)
        {
            return m_SelfDataKey == other.m_SelfDataKey && (m_ParentDataPath == other.m_ParentDataPath || m_ParentDataPath.Equals(other.m_ParentDataPath));
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(m_SelfDataKey, m_ParentDataPath);
        }

        /// <summary>
        /// Returns the string representation of this data path.
        /// </summary>
        /// <returns>
        /// A string representing the sequence of <see cref="IDataKey"/> elements, separated by "/".
        /// If the path is empty, "All" is returned.
        /// </returns>
        public override string ToString()
        {
            if (IsEmpty())
                return "All";

            StringBuilder sb = new StringBuilder();
            foreach (var dataKey in PathSequence)
            {
                sb.Append(dataKey == null ? "Root" : dataKey);
                sb.Append("/");
            }
            return sb.ToString(0, sb.Length - 1);
        }

        /// <summary>
        /// Initializes a new, empty instance of the <see cref="DataPath"/> class.
        /// </summary>
        private DataPath()
        {
            m_ParentDataPath = null;
            m_SelfDataKey = null;
        }

        private static IDataKey[] s_PathSequenceScratch = new IDataKey[8];
        private readonly DataPath m_ParentDataPath;
        private readonly IDataKey m_SelfDataKey;
        private readonly uint m_Depth = 0;
    }
}
