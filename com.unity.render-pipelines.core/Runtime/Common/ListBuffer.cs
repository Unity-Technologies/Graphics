using System;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A list that stores value on a provided memory buffer.
    ///
    /// Usually use this to have a list on stack allocated memory.
    /// </summary>
    /// <typeparam name="T">The type of the data stored in the list.</typeparam>
    public unsafe struct ListBuffer<T>
        where T : unmanaged
    {
        private T* m_BufferPtr;
        private int m_Capacity;
        private int* m_CountPtr;

        /// <summary>
        /// The pointer to the memory storage.
        /// </summary>
        internal T* BufferPtr => m_BufferPtr;

        /// <summary>
        /// The number of item in the list.
        /// </summary>
        public int Count => *m_CountPtr;

        /// <summary>
        /// The maximum number of item stored in this list.
        /// </summary>
        public int Capacity => m_Capacity;

        /// <summary>
        /// Instantiate a new list.
        /// </summary>
        /// <param name="bufferPtr">The address in memory to store the data.</param>
        /// <param name="countPtr">The address in memory to store the number of item of this list..</param>
        /// <param name="capacity">The number of <typeparamref name="T"/> that can be stored in the buffer.</param>
        public ListBuffer(T* bufferPtr, int* countPtr, int capacity)
        {
            m_BufferPtr = bufferPtr;
            m_Capacity = capacity;
            m_CountPtr = countPtr;
        }

        /// <summary>
        /// Get an item from the list.
        /// </summary>
        /// <param name="index">The index of the item to get.</param>
        /// <returns>A reference to the item.</returns>
        /// <exception cref="IndexOutOfRangeException">If the index is invalid.</exception>
        public ref T this[in int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException(
                        $"Expected a value between 0 and {Count}, but received {index}.");
                return ref m_BufferPtr[index];
            }
        }

        /// <summary>
        /// Get an item from the list.
        ///
        /// Safety: index must be inside the bounds of the list.
        /// </summary>
        /// <param name="index">The index of the item to get.</param>
        /// <returns>A reference to the item.</returns>
        public unsafe ref T GetUnchecked(in int index) => ref m_BufferPtr[index];

        /// <summary>
        /// Try to add a value in the list.
        /// </summary>
        /// <param name="value">A reference to the value to add.</param>
        /// <returns>
        ///   <code>true</code> when the value was added,
        ///   <code>false</code> when the value was not added because the capacity was reached.
        /// </returns>
        public bool TryAdd(in T value)
        {
            if (Count >= m_Capacity)
                return false;

            m_BufferPtr[Count] = value;
            ++*m_CountPtr;
            return true;
        }

        /// <summary>
        /// Copy the content of this list into another buffer in memory.
        ///
        /// Safety:
        ///  * The destination must have enough memory to receive the copied data.
        /// </summary>
        /// <param name="dstBuffer">The destination buffer of the copy operation.</param>
        /// <param name="startDstIndex">The index of the first element that will be copied in the destination buffer.</param>
        /// <param name="copyCount">The number of item to copy.</param>
        public unsafe void CopyTo(T* dstBuffer, int startDstIndex, int copyCount)
        {
            UnsafeUtility.MemCpy(dstBuffer + startDstIndex, m_BufferPtr,
                UnsafeUtility.SizeOf<T>() * copyCount);
        }

        /// <summary>
        /// Try to copy the list into another list.
        /// </summary>
        /// <param name="other">The destination of the copy.</param>
        /// <returns>
        ///   * <code>true</code> when the copy was performed.
        ///   * <code>false</code> when the copy was aborted because the destination have a capacity too small.
        /// </returns>
        public bool TryCopyTo(ListBuffer<T> other)
        {
            if (other.Count + Count >= other.m_Capacity)
                return false;

            UnsafeUtility.MemCpy(other.m_BufferPtr + other.Count, m_BufferPtr, UnsafeUtility.SizeOf<T>() * Count);
            *other.m_CountPtr += Count;
            return true;
        }

        /// <summary>
        /// Try to copy the data from a buffer in this list.
        /// </summary>
        /// <param name="srcPtr">The pointer of the source memory to copy.</param>
        /// <param name="count">The number of item to copy from the source buffer.</param>
        /// <returns>
        ///   * <code>true</code> when the copy was performed.
        ///   * <code>false</code> when the copy was aborted because the capacity of this list is too small.
        /// </returns>
        public bool TryCopyFrom(T* srcPtr, int count)
        {
            if (count + Count > m_Capacity)
                return false;

            UnsafeUtility.MemCpy(m_BufferPtr + Count, srcPtr, UnsafeUtility.SizeOf<T>() * count);
            *m_CountPtr += count;
            return true;
        }
    }

    /// <summary>
    /// Extensions for <see cref="ListBuffer{T}"/>.
    /// </summary>
    public static class ListBufferExtensions
    {
        /// <summary>
        /// Perform a quick sort on a <see cref="ListBuffer{T}"/>.
        /// </summary>
        /// <param name="self">The list to sort.</param>
        /// <typeparam name="T">The type of the element in the list.</typeparam>
        public static void QuickSort<T>(this ListBuffer<T> self)
            where T : unmanaged, IComparable<T>
        {
            unsafe
            {
                CoreUnsafeUtils.QuickSort<int>(self.Count, self.BufferPtr);
            }
        }
    }
}
