using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a hierarchical data path composed of a sequence of <see cref="IDataKey"/> elements.
    /// </summary>
    /*public*/ record DataPath : IEnumerable<IDataKey>
    {
        /// <summary>
        /// Represents an empty data path (root).
        /// </summary>
        public static DataPath Root { get; } = new();

        readonly IDataKey m_Key;
        readonly DataPath m_Parent;

        /// <summary>
        /// Initializes a new, empty instance of the <see cref="DataPath"/> class (root).
        /// </summary>
        DataPath()
        {
            m_Parent = null;
            m_Key = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPath"/> class with a parent path and a data key.
        /// </summary>
        /// <param name="parent">The parent data path.</param>
        /// <param name="key">The data key for this path segment.</param>
        DataPath(DataPath parent, IDataKey key)
        {
            Debug.Assert(parent != null);
            Debug.Assert(key != null);
            m_Parent = parent;
            m_Key = key;
        }

        /// <summary>
        /// Gets a value indicating whether this data path is the root path.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if this is the root path; otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsRoot => m_Parent == null && m_Key == null;

        /// <summary>
        /// Gets the number of elements in this data path.
        /// </summary>
        public int Length => m_Parent == null ? 1 : 1 + m_Parent.Length;

        /// <summary>
        /// Creates a new child data path by appending a key to the current path.
        /// </summary>
        /// <param name="parent">The parent data path.</param>
        /// <param name="key">The data key to append.</param>
        /// <returns>A new <see cref="DataPath"/> with the key appended.</returns>
        public static DataPath operator +(DataPath parent, IDataKey key) => new(parent, key);

        /// <summary>
        /// Returns the string representation of this data path.
        /// </summary>
        /// <returns>
        /// A string representing the sequence of <see cref="IDataKey"/> elements, separated by "/".
        /// If the path is empty, "All" is returned.
        /// </returns>
        public override string ToString()
        {
            if (IsRoot)
                return "Root";

            StringBuilder sb = new StringBuilder();
            sb.Append("Root");
            foreach (var dataKey in this)
            {
                sb.Append("/");
                sb.Append(dataKey);
            }
            return sb.ToString();
        }

        public Enumerator GetEnumerator() => new(this);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<IDataKey> IEnumerable<IDataKey>.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<IDataKey>
        {
            readonly DataPath m_Path;
            int m_Steps;

            internal Enumerator(DataPath path)
            {
                m_Path = path;
                m_Steps = path.Length - 1;
            }

            public IDataKey Current
            {
                get
                {
                    var current = m_Path;
                    for (int i = 0; i < m_Steps; i++)
                    {
                        current = current.m_Parent;
                    }
                    return current.m_Key;
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext() => --m_Steps >= 0;

            public void Reset() => m_Steps = m_Path.Length - 1;

            public void Dispose() { }
        }
    }
}
