using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// A <see cref="List{T}"/> rented from <see cref="ListPool{T}"/>, returned to the pool on
    /// <see cref="Dispose"/>. Use it through <c>using</c> so the rental is scoped:
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /*public*/ readonly struct PooledList<T> : IDisposable
    {
        readonly List<T> m_Items;

        internal PooledList(List<T> items) => m_Items = items;

        /// <summary>Rents an empty list from the pool.</summary>
        public static PooledList<T> Rent() => new PooledList<T>(ListPool<T>.Get());

        /// <summary>The rented list, for the rare case an API needs the <see cref="List{T}"/> itself.</summary>
        public List<T> List => m_Items;

        /// <summary>Gets the number of elements.</summary>
        public int Count => m_Items.Count;

        /// <summary>Gets the element at the specified index.</summary>
        /// <param name="index">The zero-based index.</param>
        public T this[int index] => m_Items[index];

        /// <summary>Appends an element.</summary>
        /// <param name="item">The element to append.</param>
        public void Add(T item) => m_Items.Add(item);

        /// <summary>Returns a struct enumerator over the elements.</summary>
        /// <returns>An enumerator that does not allocate.</returns>
        public List<T>.Enumerator GetEnumerator() => m_Items.GetEnumerator();

        /// <summary>Returns the list to the pool. <see cref="ListPool{T}"/> clears it on release.</summary>
        public void Dispose() => ListPool<T>.Release(m_Items);
    }
}
