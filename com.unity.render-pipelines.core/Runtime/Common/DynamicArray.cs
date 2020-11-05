using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Generic growable array.
    /// </summary>
    /// <typeparam name="T">Type of the array.</typeparam>
    public class DynamicArray<T> where T: new()
    {
        T[] m_Array = null;

        /// <summary>
        /// Number of elements in the array.
        /// </summary>
        public int size { get; private set; }

        /// <summary>
        /// Allocated size of the array.
        /// </summary>
        public int capacity { get { return m_Array.Length; } }

        /// <summary>
        /// Constructor.
        /// Defaults to a size of 32 elements.
        /// </summary>
        public DynamicArray()
        {
            m_Array = new T[32];
            size = 0;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="size">Number of elements.</param>
        public DynamicArray(int size)
        {
            m_Array = new T[size];
            this.size = size;
        }

        /// <summary>
        /// Clear the array of all elements.
        /// </summary>
        public void Clear()
        {
            size = 0;
        }

        /// <summary>
        /// Add an element to the array.
        /// </summary>
        /// <param name="value">Element to add to the array.</param>
        /// <returns>The index of the element.</returns>
        public int Add(in T value)
        {
            int index = size;

            // Grow array if needed;
            if (index >= m_Array.Length)
            {
                var newArray = new T[m_Array.Length * 2];
                Array.Copy(m_Array, newArray, m_Array.Length);
                m_Array = newArray;
            }

            m_Array[index] = value;
            size++;
            return index;
        }

        /// <summary>
        /// Resize the Dynamic Array.
        /// This will reallocate memory if necessary and set the current size of the array to the provided size.
        /// </summary>
        /// <param name="newSize">New size for the array.</param>
        /// <param name="keepContent">Set to true if you want the current content of the array to be kept.</param>
        public void Resize(int newSize, bool keepContent = false)
        {
            if (newSize > m_Array.Length)
            {
                if (keepContent)
                {
                    var newArray = new T[newSize];
                    Array.Copy(m_Array, newArray, m_Array.Length);
                    m_Array = newArray;
                }
                else
                {
                    m_Array = new T[newSize];
                }
            }

            size = newSize;
        }

        /// <summary>
        /// ref access to an element.
        /// </summary>
        /// <param name="index">Element index</param>
        /// <returns>The requested element.</returns>
        public ref T this[int index]
        {
            get
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (index >= size)
                    throw new IndexOutOfRangeException();
#endif
                return ref m_Array[index];
            }
        }
    }

}
