using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// On List Changed Event Args.
    /// </summary>
    /// <typeparam name="T">List type.</typeparam>
    public sealed class ListChangedEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Index
        /// </summary>
        public readonly int index;
        /// <summary>
        /// Item
        /// </summary>
        public readonly T item;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="item">Item</param>
        public ListChangedEventArgs(int index, T item)
        {
            this.index = index;
            this.item = item;
        }
    }

    /// <summary>
    /// List changed event handler.
    /// </summary>
    /// <typeparam name="T">List type.</typeparam>
    /// <param name="sender">Sender.</param>
    /// <param name="e">List changed even arguments.</param>
    public delegate void ListChangedEventHandler<T>(ObservableList<T> sender, ListChangedEventArgs<T> e);

    /// <summary>
    /// Observable list.
    /// </summary>
    /// <typeparam name="T">Type of the list.</typeparam>
    public class ObservableList<T> : IList<T>
    {
        IList<T> m_List;

        /// <summary>
        /// Added item event.
        /// </summary>
        public event ListChangedEventHandler<T> ItemAdded;
        /// <summary>
        /// Removed item event.
        /// </summary>
        public event ListChangedEventHandler<T> ItemRemoved;

        /// <summary>
        /// Accessor.
        /// </summary>
        /// <param name="index">Item index.</param>
        /// <returns>The item at the provided index.</returns>
        public T this[int index]
        {
            get { return m_List[index]; }
            set
            {
                OnEvent(ItemRemoved, index, m_List[index]);
                m_List[index] = value;
                OnEvent(ItemAdded, index, value);
            }
        }

        /// <summary>
        /// Number of elements in the list.
        /// </summary>
        public int Count
        {
            get { return m_List.Count; }
        }

        /// <summary>
        /// Is the list read only?
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public ObservableList()
            : this(0) { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="capacity">Allocation size.</param>
        public ObservableList(int capacity)
        {
            m_List = new List<T>(capacity);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="collection">Input list.</param>
        public ObservableList(IEnumerable<T> collection)
        {
            m_List = new List<T>(collection);
        }

        void OnEvent(ListChangedEventHandler<T> e, int index, T item)
        {
            if (e != null)
                e(this, new ListChangedEventArgs<T>(index, item));
        }

        /// <summary>
        /// Check if an element is present in the list.
        /// </summary>
        /// <param name="item">Item to test against.</param>
        /// <returns>True if the item is in the list.</returns>
        public bool Contains(T item)
        {
            return m_List.Contains(item);
        }

        /// <summary>
        /// Get the index of an item.
        /// </summary>
        /// <param name="item">The object to locate in the list.</param>
        /// <returns>The index of the item in the list if it exists, -1 otherwise.</returns>
        public int IndexOf(T item)
        {
            return m_List.IndexOf(item);
        }

        /// <summary>
        /// Add an item to the list.
        /// </summary>
        /// <param name="item">Item to add to the list.</param>
        public void Add(T item)
        {
            m_List.Add(item);
            OnEvent(ItemAdded, m_List.IndexOf(item), item);
        }

        /// <summary>
        /// Add multiple objects to the list.
        /// </summary>
        /// <param name="items">Items to add to the list.</param>
        public void Add(params T[] items)
        {
            foreach (var i in items)
                Add(i);
        }

        /// <summary>
        /// Insert an item in the list.
        /// </summary>
        /// <param name="index">Index at which to insert the new item.</param>
        /// <param name="item">Item to insert in the list.</param>
        public void Insert(int index, T item)
        {
            m_List.Insert(index, item);
            OnEvent(ItemAdded, index, item);
        }

        /// <summary>
        /// Remove an item from the list.
        /// </summary>
        /// <param name="item">Item to remove from the list.</param>
        /// <returns>True if the item was successfuly removed. False otherise.</returns>
        public bool Remove(T item)
        {
            int index = m_List.IndexOf(item);
            bool ret = m_List.Remove(item);
            if (ret)
                OnEvent(ItemRemoved, index, item);
            return ret;
        }

        /// <summary>
        /// Remove multiple items from the list.
        /// </summary>
        /// <param name="items">Items to remove from the list.</param>
        /// <returns>The number of removed items.</returns>
        public int Remove(params T[] items)
        {
            if (items == null)
                return 0;

            int count = 0;

            foreach (var i in items)
                count += Remove(i) ? 1 : 0;

            return count;
        }

        /// <summary>
        /// Remove an item at a specific index.
        /// </summary>
        /// <param name="index">Index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            var item = m_List[index];
            m_List.RemoveAt(index);
            OnEvent(ItemRemoved, index, item);
        }

        /// <summary>
        /// Clear the list.
        /// </summary>
        public void Clear()
        {
            while (Count > 0)
                RemoveAt(Count - 1);
        }

        /// <summary>
        /// Copy items in the list to an array.
        /// </summary>
        /// <param name="array">Destination array.</param>
        /// <param name="arrayIndex">Starting index.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            m_List.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Get enumerator.
        /// </summary>
        /// <returns>The list enumerator.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return m_List.GetEnumerator();
        }

        /// <summary>
        /// Get enumerator.
        /// </summary>
        /// <returns>The list enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
