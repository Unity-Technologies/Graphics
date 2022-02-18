using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Generic growable array.
    /// </summary>
    /// <typeparam name="T">Type of the array.</typeparam>
    public class DynamicArray<T> where T : new()
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
        /// Determines whether the DynamicArray contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the DynamicArray.</param>
        /// <returns>true if item is found in the DynamicArray; otherwise, false.</returns>
        public bool Contains(T item)
        {
            return IndexOf(item) != -1;
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
        /// Adds the elements of the specified collection to the end of the DynamicArray.
        /// </summary>
        /// <param name="array">The array whose elements should be added to the end of the DynamicArray. The array itself cannot be null, but it can contain elements that are null, if type T is a reference type.</param>
        public void AddRange(DynamicArray<T> array)
        {
            Reserve(size + array.size, true);
            for (int i = 0; i < array.size; ++i)
                m_Array[size++] = array[i];
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the DynamicArray.
        /// </summary>
        /// <param name="item">The object to remove from the DynamicArray. The value can be null for reference types.</param>
        /// <returns>true if item is successfully removed; otherwise, false. This method also returns false if item was not found in the DynamicArray.</returns>
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index != -1)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the element at the specified index of the DynamicArray.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= size)
                throw new IndexOutOfRangeException();

            if (index != size - 1)
                Array.Copy(m_Array, index + 1, m_Array, index, size - index - 1);

            size--;
        }

        /// <summary>
        /// Removes a range of elements from the DynamicArray.
        /// </summary>
        /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
        /// <param name="count">The number of elements to remove.</param>
        public void RemoveRange(int index, int count)
        {
            if (count == 0)
                return;

            if (index < 0 || index >= size || count < 0 || index + count > size)
                throw new ArgumentOutOfRangeException();

            Array.Copy(m_Array, index + count, m_Array, index, size - index - count);
            size -= count;
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the first occurrence within the range of elements in the DynamicArray that starts at the specified index and contains the specified number of elements.
        /// </summary>
        /// <param name="startIndex">The zero-based starting index of the search.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <param name="match">The Predicate delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by match, if found; otherwise, -1.</returns>
        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            for (int i = startIndex; i < size; ++i)
            {
                if (match(m_Array[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence within the range of elements in the DynamicArray that starts at the specified index and contains the specified number of elements.
        /// </summary>
        /// <param name="item">The object to locate in the DynamicArray. The value can be null for reference types.</param>
        /// <param name="index">The zero-based starting index of the search. 0 (zero) is valid in an empty list.</param>
        /// <param name="count">The number of elements in the section to search.</param>
        /// <returns></returns>
        public int IndexOf(T item, int index, int count)
        {
            for (int i = index; i < size && count > 0; ++i, --count)
            {
                if (m_Array[i].Equals(item))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence within the range of elements in the DynamicArray that extends from the specified index to the last element.
        /// </summary>
        /// <param name="item">The object to locate in the DynamicArray. The value can be null for reference types.</param>
        /// <param name="index">The zero-based starting index of the search. 0 (zero) is valid in an empty list.</param>
        /// <returns>The zero-based index of the first occurrence of item within the range of elements in the DynamicArray that extends from index to the last element, if found; otherwise, -1.</returns>
        public int IndexOf(T item, int index)
        {
            for (int i = index; i < size; ++i)
            {
                if (m_Array[i].Equals(item))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first occurrence within the entire DynamicArray.
        /// </summary>
        /// <param name="item">The object to locate in the DynamicArray. The value can be null for reference types.</param>
        /// <returns>he zero-based index of the first occurrence of item within the entire DynamicArray, if found; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            return IndexOf(item, 0);
        }

        /// <summary>
        /// Resize the Dynamic Array.
        /// This will reallocate memory if necessary and set the current size of the array to the provided size.
        /// </summary>
        /// <param name="newSize">New size for the array.</param>
        /// <param name="keepContent">Set to true if you want the current content of the array to be kept.</param>
        public void Resize(int newSize, bool keepContent = false)
        {
            Reserve(newSize, keepContent);
            size = newSize;
        }

        /// <summary>
        /// Sets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
        /// <param name="newCapacity">New capacity for the array.</param>
        /// <param name="keepContent">Set to true if you want the current content of the array to be kept.</param>
        public void Reserve(int newCapacity, bool keepContent = false)
        {
            if (newCapacity > m_Array.Length)
            {
                if (keepContent)
                {
                    var newArray = new T[newCapacity];
                    Array.Copy(m_Array, newArray, m_Array.Length);
                    m_Array = newArray;
                }
                else
                {
                    m_Array = new T[newCapacity];
                }
            }
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

        /// <summary>
        /// Implicit conversion to regular array.
        /// </summary>
        /// <param name="array">Input DynamicArray.</param>
        /// <returns>The internal array.</returns>
        public static implicit operator T[](DynamicArray<T> array) => array.m_Array;
    }

    /// <summary>
    /// Extension class for DynamicArray
    /// </summary>
    public static class DynamicArrayExtensions
    {
        static int Partition<T>(T[] data, int left, int right) where T : IComparable<T>, new()
        {
            var pivot = data[left];

            --left;
            ++right;
            while (true)
            {
                var c = 0;
                var lvalue = default(T);
                do
                {
                    ++left;
                    lvalue = data[left];
                    c = lvalue.CompareTo(pivot);
                }
                while (c < 0);

                var rvalue = default(T);
                do
                {
                    --right;
                    rvalue = data[right];
                    c = rvalue.CompareTo(pivot);
                }
                while (c > 0);

                if (left < right)
                {
                    data[right] = lvalue;
                    data[left] = rvalue;
                }
                else
                {
                    return right;
                }
            }
        }

        static void QuickSort<T>(T[] data, int left, int right) where T : IComparable<T>, new()
        {
            if (left < right)
            {
                int pivot = Partition(data, left, right);

                if (pivot >= 1)
                    QuickSort(data, left, pivot);

                if (pivot + 1 < right)
                    QuickSort(data, pivot + 1, right);
            }
        }

        /// <summary>
        /// Perform a quick sort on the DynamicArray
        /// </summary>
        /// <typeparam name="T">Type of the array.</typeparam>
        /// <param name="array">Array on which to perform the quick sort.</param>
        public static void QuickSort<T>(this DynamicArray<T> array) where T : IComparable<T>, new()
        {
            QuickSort<T>(array, 0, array.size - 1);
        }
    }
}
