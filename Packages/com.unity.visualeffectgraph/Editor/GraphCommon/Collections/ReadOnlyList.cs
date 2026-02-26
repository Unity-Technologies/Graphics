using System.Collections;
using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel
{
    /// <summary>
    /// A readonly wrapper to a List
    /// </summary>
    /// <remarks>
    /// This allows to get a read only enumerator that does not allocate (contrary to IReadOnlyList interface).
    /// </remarks>
    /// <typeparam name="T">The element type stored in the wrapped List.</typeparam>
    /*public*/ readonly struct ReadOnlyList<T> : IReadOnlyList<T>
    {
        readonly List<T> m_List;

        /// <summary>
        /// Construct a new ReadOnlyList wrapped for the passed List.
        /// </summary>
        /// <param name="list">The List to wrap as read only.</param>
        public ReadOnlyList(List<T> list)
        {
            m_List = list;
        }

        /// <summary>
        /// Gets the element at the specified index in the read-only list.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get.</param>
        /// <value>The element at the specified index in the read-only list.</value>
        public T this[int index] => m_List[index];

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        /// <value>The number of elements in the collection.</value>
        public int Count => m_List.Count;

        /// <summary>
        /// Returns an enumerator that iterates through the wrapped List.
        /// </summary>
        /// <returns>A List Enumerator for the wrapped list</returns>
        public List<T>.Enumerator GetEnumerator() => m_List.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Converts a System.Collections.Generic.List into a ReadOnlyList.
        /// </summary>
        /// <param name="list">The List to convert to ReadOnlyList.</param>
        /// <returns>A ReadOnlyList encapsulating the list.</returns>
        public static implicit operator ReadOnlyList<T>(List<T> list) => new ReadOnlyList<T>(list);
    }
}
