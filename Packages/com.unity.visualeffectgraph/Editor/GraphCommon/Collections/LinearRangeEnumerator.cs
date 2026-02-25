using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// Provides an enumerator for a specific range of elements in a <see cref="List{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /*public*/ struct ListRangeEnumerator<T> : IEnumerator<T>
    {
        /// <summary>
        /// Gets the total number of elements in the enumeration range.
        /// </summary>
        public int Count => m_LastIndex - m_FirstIndex;

        List<T> m_Items;
        int m_FirstIndex;
        int m_LastIndex;
        int m_Index;

        /// <summary>
        /// Gets the element at the current position of the enumerator.
        /// </summary>
        public T Current => m_Items[m_Index];

        /// <summary>
        /// Gets the element at the current position of the enumerator as an <see cref="object"/>.
        /// </summary>
        object IEnumerator.Current => Current;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListRangeEnumerator{T}"/> struct for a specific range of elements in a list.
        /// </summary>
        /// <param name="items">The list containing the elements to enumerate.</param>
        /// <param name="offset">The starting index of the range to enumerate.</param>
        /// <param name="count">The number of elements in the range to enumerate.</param>
        public ListRangeEnumerator(List<T> items, int offset, int count)
        {
            Debug.Assert(offset + count <= items.Count);
            m_Items = items;
            m_FirstIndex = offset;
            m_LastIndex = offset + count;
            m_Index = m_FirstIndex - 1;
        }

        /// <summary>
        /// Advances the enumerator to the next element in the range.
        /// </summary>
        /// <returns><c>true</c> if the enumerator successfully advanced to the next element; <c>false</c> if the end of the range has been reached.</returns>
        public bool MoveNext()
        {
            return ++m_Index < m_LastIndex;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the range.
        /// </summary>
        public void Reset()
        {
            m_Index = m_FirstIndex - 1;
        }

        /// <summary>
        /// Releases all resources used by the enumerator.
        /// </summary>
        public void Dispose()
        {
            m_Items = null;
        }
    }
}
