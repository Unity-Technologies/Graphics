using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a unique identifier for graph data elements.
    /// </summary>
    [Serializable]
    /*public*/ readonly struct GraphDataId : IEquatable<GraphDataId>
    {
        /// <summary>
        /// Represents an invalid graph data identifier.
        /// </summary>
        public static readonly GraphDataId Invalid = new GraphDataId(-1);

        readonly int m_Value;

        /// <summary>
        /// Gets the zero-based index of this graph data element.
        /// Index is offset by 1 to have 0 (default) as the invalid value.
        /// </summary>
        public int Index => m_Value - 1;

        /// <summary>
        /// Gets a value indicating whether this identifier is valid.
        /// </summary>
        public bool IsValid => Index != Invalid.Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphDataId"/> struct.
        /// </summary>
        /// <param name="index">The zero-based index of the graph data element.</param>
        internal GraphDataId(int index)
        {
            m_Value = index + 1;
        }

        /// <summary>
        /// Determines whether this instance is equal to another <see cref="GraphDataId"/>.
        /// </summary>
        /// <param name="other">The <see cref="GraphDataId"/> to compare with.</param>
        /// <returns><c>true</c> if the identifiers are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(GraphDataId other) => Index == other.Index;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance.</returns>
        public override int GetHashCode() => Index;

        /// <summary>
        /// Returns a string representation of this instance.
        /// </summary>
        /// <returns>A string representation of this instance.</returns>
        public override string ToString() => Index.ToString();
    }

    /// <summary>
    /// Represents a collection of graph data elements with optional cached information.
    /// </summary>
    /// <typeparam name="T">The type of the graph data elements.</typeparam>
    /// <typeparam name="TCacheInfo">The type of the cached information.</typeparam>
    [Serializable]
    class GraphDataList<T> : ICountable, IEnumerable<T> where T : struct
    {
        [SerializeField]
        T[] m_Items = new T[0];

        public int Count { get; private set; }

        /// <summary>
        /// Gets or sets the capacity of the collection.
        /// </summary>
        public int Capacity
        {
            get => m_Items.Length;
            set
            {
                Debug.Assert(value >= Count);
                Array.Resize(ref m_Items, value);
            }
        }

        /// <summary>
        /// Gets a reference to the element at the specified identifier.
        /// </summary>
        /// <param name="id">The identifier of the element to get.</param>
        /// <returns>A reference to the element at the specified identifier.</returns>
        public ref T this[GraphDataId id]
        {
            get
            {
                int index = id.Index;
                Debug.Assert(index < Count);
                return ref m_Items[index];
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphDataList{T, TCacheInfo}"/> class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the collection.</param>
        public GraphDataList(int capacity = 0)
        {
            Capacity = capacity;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphDataList{T, TCacheInfo}"/> class by copying another instance.
        /// </summary>
        /// <param name="list">The instance to copy.</param>
        /// <param name="copyCache">Whether to copy cache information.</param>
        public GraphDataList(GraphDataList<T> list, bool copyCache = true) : this(list.Count)
        {
            Count = list.Count;
            Array.Copy(list.m_Items, m_Items, Count);
        }

        /// <summary>
        /// Clears all elements from the collection.
        /// </summary>
        public void Clear()
        {
            Count = 0;
        }

        /// <summary>
        /// Releases all resources used by the collection.
        /// </summary>
        public void Release()
        {
            m_Items = new T[0];
        }

        /// <summary>
        /// Allocates a new element in the collection.
        /// </summary>
        /// <param name="id">When this method returns, contains the identifier of the allocated element.</param>
        /// <returns>A reference to the allocated element.</returns>
        public ref T Allocate(out GraphDataId id)
        {
            if (Count >= Capacity)
            {
                Grow(Count + 1);
            }

            id = new GraphDataId(Count++);
            return ref this[id];
        }

        /// <summary>
        /// Increases the capacity of the collection to at least the specified value.
        /// </summary>
        /// <param name="minCapacity">The minimum capacity to ensure.</param>
        public void Grow(int minCapacity)
        {
            Capacity = Math.Max(2 * Capacity, minCapacity);
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        struct Enumerator : IEnumerator<T>
        {
            GraphDataList<T> m_List;
            int m_Index;

            public T Current => m_List.m_Items[m_Index];
            object IEnumerator.Current => Current;

            public Enumerator(GraphDataList<T> list)
            {
                m_List = list;
                m_Index = -1;
            }

            public bool MoveNext()
            {
                return ++m_Index < m_List.Count;
            }

            public void Reset()
            {
                m_Index = -1;
            }

            public void Dispose()
            {
                m_List = null;
            }
        }
    }


}
