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
    class GraphDataList<T> : ICountable, IVersioned, IEnumerable<T> where T : struct
    {
        [SerializeField]
        T[] m_Items = Array.Empty<T>();

        [SerializeField]
        bool[] m_Alive = Array.Empty<bool>();

        [SerializeField]
        int m_LiveCount;

        [SerializeField]
        int m_SlotCount;

        /// <summary>
        /// Gets the number of live (non-removed) elements in the collection.
        /// </summary>
        public int Count => m_LiveCount;

        /// <summary>
        /// The version of the collection, incremented on every mutation. Starts at 1u, 0u is reserved for invalid.
        /// </summary>
        public uint Version { get; private set; } = 1u;

        /// <summary>
        /// Gets the high-water mark of allocated slots. Valid <see cref="GraphDataId.Index"/>
        /// values for this collection are in the range [0, SlotCount). Slots remain reserved
        /// after <see cref="Remove"/> so that ids stay stable for parallel cache structures.
        /// </summary>
        public int SlotCount => m_SlotCount;

        /// <summary>
        /// Gets or sets the capacity of the collection.
        /// </summary>
        public int Capacity
        {
            get => m_Items.Length;
            set
            {
                Debug.Assert(value >= m_SlotCount);
                Array.Resize(ref m_Items, value);
                Array.Resize(ref m_Alive, value);
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
                Debug.Assert(index < m_SlotCount);
                Debug.Assert(m_Alive[index]);
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
        public GraphDataList(GraphDataList<T> list) : this(list.m_SlotCount)
        {
            m_SlotCount = list.m_SlotCount;
            m_LiveCount = list.m_LiveCount;
            Array.Copy(list.m_Items, m_Items, m_SlotCount);
            Array.Copy(list.m_Alive, m_Alive, m_SlotCount);
        }

        /// <summary>
        /// Clears all elements from the collection.
        /// </summary>
        public void Clear()
        {
            Array.Clear(m_Items, 0, m_SlotCount);
            Array.Clear(m_Alive, 0, m_SlotCount);
            m_SlotCount = 0;
            m_LiveCount = 0;
            Version = 1u;
        }

        /// <summary>
        /// Releases all resources used by the collection.
        /// </summary>
        public void Release()
        {
            m_Items = new T[0];
            m_Alive = new bool[0];
            m_SlotCount = 0;
            m_LiveCount = 0;
            Version = 1u;
        }

        /// <summary>
        /// Allocates a new element in the collection.
        /// </summary>
        /// <param name="id">When this method returns, contains the identifier of the allocated element.</param>
        /// <returns>A reference to the allocated element.</returns>
        public ref T Allocate(out GraphDataId id)
        {
            if (m_SlotCount >= Capacity)
            {
                Grow(m_SlotCount + 1);
            }

            id = new GraphDataId(m_SlotCount);
            m_Alive[m_SlotCount] = true;
            m_SlotCount++;
            m_LiveCount++;
            Version++;
            return ref m_Items[id.Index];
        }

        /// <summary>
        /// Marks the element at the specified identifier as removed. The slot remains reserved
        /// so that the identifier indices of other elements stay stable.
        /// </summary>
        /// <param name="id">The identifier of the element to remove.</param>
        public void Remove(GraphDataId id)
        {
            int index = id.Index;
            Debug.Assert(index >= 0 && index < m_SlotCount);
            Debug.Assert(m_Alive[index]);
            m_Alive[index] = false;
            m_Items[index] = default;
            Version++;
            m_LiveCount--;
        }

        /// <summary>
        /// Returns true if the element at the specified identifier is alive (allocated and not removed).
        /// </summary>
        /// <param name="id">The identifier to test.</param>
        public bool IsAlive(GraphDataId id)
        {
            int index = id.Index;
            return (uint)index < (uint)m_SlotCount && m_Alive[index];
        }

        /// <summary>
        /// Returns true if the element at the specified raw slot index is alive (allocated and not removed).
        /// </summary>
        /// <param name="slot">The zero-based slot index. Must be in <c>[0, SlotCount)</c>.</param>
        public bool IsAliveAtSlot(int slot) => (uint)slot < (uint)m_SlotCount && m_Alive[slot];

        /// <summary>
        /// Gets a reference to the element at the specified raw slot index. The slot must be alive.
        /// </summary>
        /// <param name="slot">The zero-based slot index.</param>
        public ref T AtSlot(int slot)
        {
            Debug.Assert(slot >= 0 && slot < m_SlotCount);
            Debug.Assert(m_Alive[slot]);
            return ref m_Items[slot];
        }

        /// <summary>
        /// Increases the capacity of the collection to at least the specified value.
        /// </summary>
        /// <param name="minCapacity">The minimum capacity to ensure.</param>
        public void Grow(int minCapacity)
        {
            Capacity = Math.Max(2 * Capacity, minCapacity);
        }

        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

        public struct Enumerator : IEnumerator<T>
        {
            GraphDataList<T> m_List;
            readonly uint m_StartVersion;
            int m_Index;

            public T Current => m_List.m_Items[m_Index];
            object IEnumerator.Current => Current;

            public Enumerator(GraphDataList<T> list)
            {
                m_List = list;
                m_StartVersion = m_List.Version;
                m_Index = -1;
            }

            public bool MoveNext()
            {
                if (m_StartVersion != m_List.Version)
                {
                    throw new InvalidOperationException("Collection was modified during enumeration.");
                }
                while (++m_Index < m_List.m_SlotCount)
                {
                    if (m_List.m_Alive[m_Index])
                        return true;
                }
                return false;
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
