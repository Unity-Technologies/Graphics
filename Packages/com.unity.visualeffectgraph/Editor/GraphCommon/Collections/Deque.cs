using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.GraphCommon.LowLevel.Editor
{
    /// <summary>
    /// A double-ended queue (deque) implementation that allows adding and removing elements from both ends.
    /// </summary>
    /// <typeparam name="T">The type that the deque is holding.</typeparam>
    /*public*/ class Deque<T> : IEnumerable<T>
    {
        private T[] m_Buffer;
        private int m_Head;
        private int m_Tail;
        private int m_Count;
        private const int DefaultCapacity = 4;

        /// <summary>
        /// Initializes a new instance of the <see cref="Deque{T}"/> class with the default capacity.
        /// </summary>
        public Deque() : this(DefaultCapacity)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="Deque{T}"/> class with a specified capacity.
        /// </summary>
        /// <param name="capacity"> The initial capacity of the deque. </param>
        /// <exception cref="ArgumentOutOfRangeException">Throws if capacity is negative.</exception>
        public Deque(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity cannot be negative");

            m_Buffer = new T[capacity];
            m_Head = 0;
            m_Tail = 0;
            m_Count = 0;
        }

        /// <summary>
        /// Gets the number of elements in the deque.
        /// </summary>
        public int Count => m_Count;

        /// <summary>
        /// Returns true if the deque is empty. False otherwise.
        /// </summary>
        public bool IsEmpty => m_Count == 0;

        /// <summary>
        /// Adds an element to the front of the deque.
        /// </summary>
        /// <param name="item">The element to add.</param>
        public void AddFront(T item)
        {
            if (m_Count == m_Buffer.Length)
            {
                Resize();
            }

            m_Head = (m_Head - 1 + m_Buffer.Length) % m_Buffer.Length;
            m_Buffer[m_Head] = item;
            m_Count++;
        }

        /// <summary>
        /// Adds an element to the back of the deque.
        /// </summary>
        /// <param name="item">The element to add.</param>
        public void AddBack(T item)
        {
            if (m_Count == m_Buffer.Length)
            {
                Resize();
            }

            m_Buffer[m_Tail] = item;
            m_Tail = (m_Tail + 1) % m_Buffer.Length;
            m_Count++;
        }

        /// <summary>
        /// Removes and returns the element at the front of the deque.
        /// </summary>
        /// <returns>The element removed from the front.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the deque is empty.</exception>
        public T RemoveFront()
        {
            if (IsEmpty)
                throw new InvalidOperationException("Deque is empty");

            T item = m_Buffer[m_Head];
            m_Buffer[m_Head] = default(T); // Clear reference
            m_Head = (m_Head + 1) % m_Buffer.Length;
            m_Count--;
            return item;
        }

        /// <summary>
        /// Removes and returns the element at the back of the deque.
        /// </summary>
        /// <returns>The element removed from the back.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the deque is empty.</exception>
        public T RemoveBack()
        {
            if (IsEmpty)
                throw new InvalidOperationException("Deque is empty");

            m_Tail = (m_Tail - 1 + m_Buffer.Length) % m_Buffer.Length;
            T item = m_Buffer[m_Tail];
            m_Buffer[m_Tail] = default(T); // Clear reference
            m_Count--;
            return item;
        }

        /// <summary>
        /// Returns the element at the front of the deque without removing it.
        /// </summary>
        /// <returns>The element at the front.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the deque is empty.</exception>
        public T PeekFront()
        {
            if (IsEmpty)
                throw new InvalidOperationException("Deque is empty");

            return m_Buffer[m_Head];
        }

        /// <summary>
        /// Returns the element at the back of the deque without removing it.
        /// </summary>
        /// <returns>The element at the back.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the deque is empty.</exception>
        public T PeekBack()
        {
            if (IsEmpty)
                throw new InvalidOperationException("Deque is empty");

            int lastIndex = (m_Tail - 1 + m_Buffer.Length) % m_Buffer.Length;
            return m_Buffer[lastIndex];
        }

        // Indexer for random access
        private T this[int index]
        {
            get
            {
                if (index < 0 || index >= m_Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                int actualIndex = (m_Head + index) % m_Buffer.Length;
                return m_Buffer[actualIndex];
            }
            set
            {
                if (index < 0 || index >= m_Count)
                    throw new ArgumentOutOfRangeException(nameof(index));

                int actualIndex = (m_Head + index) % m_Buffer.Length;
                m_Buffer[actualIndex] = value;
            }
        }

        /// <summary>
        /// Removes all elements from the deque.
        /// </summary>
        public void Clear()
        {
            if (m_Count > 0)
            {
                Array.Clear(m_Buffer, 0, m_Buffer.Length);
                m_Head = 0;
                m_Tail = 0;
                m_Count = 0;
            }
        }

        // Resize the internal buffer when capacity is reached
        private void Resize()
        {
            int newCapacity = m_Buffer.Length == 0 ? DefaultCapacity : m_Buffer.Length * 2;
            T[] newBuffer = new T[newCapacity];

            // Copy elements to new buffer in order
            for (int i = 0; i < m_Count; i++)
            {
                newBuffer[i] = this[i];
            }

            m_Buffer = newBuffer;
            m_Head = 0;
            m_Tail = m_Count;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the deque.
        /// </summary>
        /// <returns>An enumerator for the deque.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < m_Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
