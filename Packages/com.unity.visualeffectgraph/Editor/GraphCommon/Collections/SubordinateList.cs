using System;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Represents a list of cache items keyed by <see cref="GraphDataId"/> whose capacity tracks
    /// the slot count of a backing <see cref="GraphDataList{TSource}"/>.
    /// </summary>
    /// <typeparam name="T">The value type of the items in the list.</typeparam>
    /// <typeparam name="TSource">The element type of the backing GraphDataList.</typeparam>
    internal class SubordinateList<T, TSource> where T : struct where TSource : struct
    {
        readonly GraphDataList<TSource> m_Source;
        T[] m_Items;

        /// <summary>
        /// Initializes a new instance of the <see cref="SubordinateList{T, TSource}"/> class with the specified source list.
        /// </summary>
        /// <param name="source">The graph data list whose slot count drives this list's capacity.</param>
        public SubordinateList(GraphDataList<TSource> source)
        {
            m_Source = source;
            m_Items = new T[m_Source.SlotCount];
        }

        /// <summary>
        /// Gets a reference to the item at the specified <see cref="GraphDataId"/>.
        /// Resizes the internal array if the index is within the source's slot count but exceeds the current array length.
        /// </summary>
        /// <param name="id">The <see cref="GraphDataId"/> identifying the item.</param>
        /// <returns>A reference to the item at the specified index.</returns>
        public ref T this[GraphDataId id]
        {
            get
            {
                int index = id.Index;
                if (index >= m_Items.Length)
                {
                    if (index < m_Source.SlotCount)
                    {
                        Resize(m_Source.Capacity);
                    }
                    else
                    {
                        Debug.Assert(false, "Subordinate list index is out of range");
                    }
                }

                return ref m_Items[index];
            }
        }

        /// <summary>
        /// Resizes the internal array to the specified capacity.
        /// </summary>
        /// <param name="capacity">The new capacity for the internal array.</param>
        public void Resize(int capacity)
        {
            Array.Resize(ref m_Items, capacity);
        }

        /// <summary>
        /// Releases the resources used by the list and clears its contents.
        /// </summary>
        public void Release()
        {
            m_Items = Array.Empty<T>();
        }

        /// <summary>
        /// Copies the contents of <paramref name="other"/> into this list. Resizes if needed.
        /// </summary>
        /// <param name="other">The source list to copy items from.</param>
        /// <param name="cloneItem">
        /// Optional per-element clone function. Pass this when <typeparamref name="T"/> contains reference-type
        /// fields that must not be shared with <paramref name="other"/>; otherwise a shallow struct copy is performed.
        /// </param>
        public void CopyFrom(SubordinateList<T, TSource> other, Func<T, T> cloneItem = null)
        {
            if (m_Items.Length < other.m_Items.Length)
                Array.Resize(ref m_Items, other.m_Items.Length);

            if (cloneItem == null)
            {
                Array.Copy(other.m_Items, m_Items, other.m_Items.Length);
            }
            else
            {
                for (int i = 0; i < other.m_Items.Length; i++)
                    m_Items[i] = cloneItem(other.m_Items[i]);
            }
        }
    }
}
