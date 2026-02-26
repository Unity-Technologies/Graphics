using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// A Linearized List of Lists
    /// </summary>
    /// <remarks>
    /// - All items contiguous in memory
    /// - Access O(1)
    /// - Insert/remove at end (last item of last list) amortized O(1)Linearized list of list
    /// - Random insert/remove O(n)all items contiguous in memory
    /// </remarks>
    [Serializable]
    class LinearMultiList<T> : IIndexable<int, int, T>
    {
        [SerializeField]
        List<int> m_ListOffsets = new();
        [SerializeField]
        List<int> m_ListCounts = new();
        [SerializeField]
        List<T> m_Items = new();

        /// <summary>
        /// Gets the number of lists in the collection.
        /// </summary>
        public int ListCount => m_ListOffsets.Count;

        /// <summary>
        /// Gets the number of items in the specified list.
        /// </summary>
        /// <param name="listIndex">The index of the list.</param>
        /// <returns>The number of items in the list.</returns>
        public int CountInList(int listIndex) => GetCount(listIndex);

        /// <summary>
        /// Gets or sets the item at the specified list and item indices.
        /// </summary>
        /// <param name="listIndex">The index of the list.</param>
        /// <param name="itemIndex">The index of the item within the list.</param>
        /// <returns>The item at the specified indices.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown when the indices are out of range.</exception>
        public T this[int listIndex, int itemIndex]
        {
            get
            {
                int flatIndex = GetFlatIndex(listIndex, itemIndex);
                return m_Items[flatIndex];
            }
            set
            {
                int flatIndex = GetFlatIndex(listIndex, itemIndex);
                m_Items[flatIndex] = value;
            }
        }

        /// <summary>
        /// Gets a view of the list at the specified index.
        /// </summary>
        /// <param name="listIndex">The index of the list.</param>
        /// <returns>A <see cref="Sublist"/> providing access to the list.</returns>
        public Sublist this[int listIndex] => new Sublist(this, listIndex);

        /// <summary>
        /// Adds a new empty list with the specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the list. Default is 0.</param>
        public void AddList(int capacity = 0)
        {
            m_Items.Capacity = m_Items.Count + capacity;
            for (int i = 0; i < capacity; ++i)
                m_Items.Add(default);

            m_ListOffsets.Add(m_Items.Count);
            m_ListCounts.Add(0);
        }

        /// <summary>
        /// Adds a new list and populates it with the specified items.
        /// </summary>
        /// <param name="items">The items to add to the new list.</param>
        public void AddList(IEnumerable<T> items)
        {
            var oldCount = m_Items.Count;
            m_Items.AddRange(items);

            m_ListOffsets.Add(m_Items.Count);
            m_ListCounts.Add(m_Items.Count - oldCount);
        }

        /// <summary>
        /// Removes the list at the specified index.
        /// </summary>
        /// <param name="listIndex">The index of the list to remove.</param>
        /// <remarks>This operation invalidates all subsequent list indices.</remarks>
        public void RemoveList(int listIndex) // Invalidate all subsequent list indices
        {
            var offset = GetOffset(listIndex);
            var count = GetCount(listIndex);

            m_Items.RemoveRange(offset, count);

            for (var i = listIndex; i < m_ListOffsets.Count - 1; ++i)
                m_ListOffsets[i] = m_ListOffsets[i + 1] - count;
            m_ListOffsets.RemoveAt(m_ListOffsets.Count - 1);

            m_ListCounts.RemoveAt(listIndex);
        }

        /// <summary>
        /// Adds an item to the end of the specified list.
        /// </summary>
        /// <param name="listIndex">The index of the list to add the item to.</param>
        /// <param name="item">The item to add.</param>
        /// <returns>The flat index of the added item in the underlying array.</returns>
        public int AddItem(int listIndex, in T item)
        {
            var count = GetCount(listIndex);
            var (offset, capacity) = GetOffsetAndCapacity(listIndex);

            var index = offset + count;
            if (count < capacity)
            {
                m_Items[index] = item;
            }
            else
            {
                for (var i = listIndex; i < m_ListOffsets.Count; ++i)
                    ++m_ListOffsets[i];
                m_Items.Insert(index, item);
            }

            m_ListCounts[listIndex]++;

            return index;
        }

        /// <summary>
        /// Removes the item at the specified index from the specified list.
        /// </summary>
        /// <param name="listIndex">The index of the list containing the item.</param>
        /// <param name="itemIndex">The index of the item within the list.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown when the list is empty.</exception>
        public void RemoveItem(int listIndex, int itemIndex)
        {
            var count = GetCount(listIndex);
            if (count <= 0)
                throw new IndexOutOfRangeException();

            int index = GetFlatIndex(listIndex, itemIndex);
            var lastIndex = m_ListOffsets[listIndex] - 1;
            for (int i = index; i < lastIndex; ++i)
            {
                m_Items[i] = m_Items[i + 1];
            }
            m_Items[lastIndex] = default;

            m_ListCounts[listIndex] = count - 1;
        }

        /// <summary>
        /// Finds an item in the specified list.
        /// </summary>
        /// <param name="listIndex">The index of the list to search.</param>
        /// <param name="value">The value to find.</param>
        /// <param name="itemIndex">When this method returns, contains the index of the item if found; otherwise, -1.</param>
        /// <returns><c>true</c> if the item was found; otherwise, <c>false</c>.</returns>
        public bool FindItem(int listIndex, T value, out int itemIndex)
        {
            var offset = GetOffset(listIndex);
            var count = GetCount(listIndex);
            for (int i = 0; i < count; ++i)
            {
                if (m_Items[offset + i].Equals(value))
                {
                    itemIndex = i;
                    return true;
                }
            }
            itemIndex = -1;
            return false;
        }

        /// <summary>
        /// Optimizes the internal storage by removing unused capacity and making the collection more compact.
        /// </summary>
        public void Pack()
        {
            int oldOffset = 0;
            int newOffset = 0;
            for (int listIndex = 0; listIndex < m_ListCounts.Count; ++listIndex)
            {
                int count = m_ListCounts[listIndex];
                for (int index = 0; index < count; ++index)
                {
                    m_Items[newOffset + index] = m_Items[oldOffset + index];
                }
                oldOffset = m_ListOffsets[listIndex];
                newOffset += count;
                m_ListOffsets[listIndex] = newOffset;
            }
            m_Items.RemoveRange(newOffset, oldOffset - newOffset);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the lists in the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the lists.</returns>
        public Enumerator GetEnumerator() => new Enumerator(this);

        int GetOffset(int listIndex)
        {
            var offset = listIndex == 0 ? 0 : m_ListOffsets[listIndex - 1];
            return offset;
        }

        int GetCapacity(int listIndex)
        {
            var offset = GetOffset(listIndex);
            return m_ListOffsets[listIndex] - offset;
        }

        (int offset, int capacity) GetOffsetAndCapacity(int listIndex)
        {
            var offset = GetOffset(listIndex);
            var capacity = m_ListOffsets[listIndex] - offset;
            return (offset, capacity);
        }

        int GetCount(int listIndex)
        {
            return m_ListCounts[listIndex];
        }

        int GetFlatIndex(int listIndex, int itemIndex)
        {
            var (offset, capacity) = GetOffsetAndCapacity(listIndex);

            if (itemIndex >= capacity)
                throw new IndexOutOfRangeException();

            return offset + itemIndex;
        }

    /// <summary>
        /// Represents a view of a single list within the <see cref="LinearMultiList{T}"/>.
        /// </summary>
        public struct Sublist
        {
            /// <summary>
            /// Gets the parent <see cref="LinearMultiList{T}"/> that contains this list.
            /// </summary>
            public LinearMultiList<T> Owner { get; }

            /// <summary>
            /// Gets the index of this list within the parent collection.
            /// </summary>
            public int ListIndex { get; }

            /// <summary>
            /// Gets the number of items in this list.
            /// </summary>
            public int Count => Owner.GetCount(ListIndex);

            /// <summary>
            /// Gets the capacity of this list.
            /// </summary>
            public int Capacity => Owner.GetCapacity(ListIndex);

            /// <summary>
            /// Initializes a new instance of the <see cref="Sublist"/> struct.
            /// </summary>
            /// <param name="owner">The parent collection.</param>
            /// <param name="listIndex">The index of the list within the parent collection.</param>
            public Sublist(LinearMultiList<T> owner, int listIndex)
            {
                Owner = owner;
                ListIndex = listIndex;
            }

            /// <summary>
            /// Gets the item at the specified index within this list.
            /// </summary>
            /// <param name="index">The index of the item.</param>
            /// <returns>The item at the specified index.</returns>
            public T this[int index] => Owner[ListIndex, index];

            /// <summary>
            /// Returns an enumerator that iterates through the items in this list.
            /// </summary>
            /// <returns>An enumerator that can be used to iterate through the items.</returns>
            public ListRangeEnumerator<T> GetEnumerator() => new(Owner.m_Items, Owner.GetOffset(ListIndex), Owner.GetCount(ListIndex));
        }

        /// <summary>
        /// Enumerates the lists in a <see cref="LinearMultiList{T}"/>.
        /// </summary>
        public struct Enumerator
        {
            LinearMultiList<T> m_Owner;
            int m_Index;

            /// <summary>
            /// Gets the current list in the enumeration.
            /// </summary>
            public Sublist Current => m_Owner[m_Index];

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerator"/> struct.
            /// </summary>
            /// <param name="owner">The collection to enumerate.</param>
            public Enumerator(LinearMultiList<T> owner)
            {
                m_Owner = owner;
                m_Index = -1;
            }

            /// <summary>
            /// Advances the enumerator to the next list in the collection.
            /// </summary>
            /// <returns><c>true</c> if the enumerator was successfully advanced to the next element; <c>false</c> if the enumerator has passed the end of the collection.</returns>
            public bool MoveNext()
            {
                return ++m_Index < m_Owner.ListCount;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            public void Reset()
            {
                m_Index = -1;
            }

            /// <summary>
            /// Releases any resources used by the enumerator.
            /// </summary>
            public void Dispose()
            {
                m_Owner = null;
            }
        }
    }
}
