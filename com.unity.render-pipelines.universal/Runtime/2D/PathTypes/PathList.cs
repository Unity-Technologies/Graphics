using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace UnityEngine.Rendering.Universal
{
    internal struct PathList<T> : IDisposable where T : struct 
    {
        PathArray<T> m_InternalArray;

        int m_Capacity;
        int m_UsedElements;

        public bool IsCreated => m_InternalArray.IsCreated;

        public PathList(int capacity)
        {
            m_Capacity = capacity;
            m_UsedElements = 0;
            m_InternalArray = new PathArray<T>(capacity);
        }

        public void CopyTo(int index, PathArray<T> array, int arrayIndex, int count)
        {
            unsafe
            {
                for (int i = 0; i < count; i++)
                {
                    array[arrayIndex + i] = m_InternalArray[index + i];
                }
            }
        }

        public void CopyTo(int index, PathList<T> pathList, int arrayIndex, int count)
        {
            CopyTo(index, pathList.m_InternalArray, arrayIndex, count);
        }

        public void CopyTo(PathArray<T> array, int arrayIndex)
        {
            // Choose the smaller size to copy
            int targetArraySize = array.Length - arrayIndex;
            int itemsToCopy = targetArraySize > Capacity ? Capacity : targetArraySize;
            CopyTo(0, array, arrayIndex, itemsToCopy);
        }

        public int Capacity
        {
            get
            {
                if (IsCreated)
                    return m_Capacity;
                else
                    return 0;
            }
            set
            {
                int newCapacity = value;
                PathArray<T> newArray = new PathArray<T>(newCapacity);
                CopyTo(newArray, 0);

                // This will be false the very first time this is set
                if (IsCreated)
                    m_InternalArray.Dispose(false);

                m_InternalArray = newArray;
                m_Capacity = newCapacity;
            }
        }

        public int Count => m_UsedElements;

        public T this[int index]
        {
            get
            {
                return m_InternalArray[index];
            }
            set
            {
                m_InternalArray[index] = value;
            }
        }

        public void Reverse(int index, int count)
        {
            int halfCount = count >> 1;
            for (int i = 0; i < halfCount; i++)
            {
                int startIndex = index + i;
                int endIndex = index + count - 1;

                // Swap
                T tempValue = m_InternalArray[startIndex];
                m_InternalArray[startIndex] = m_InternalArray[endIndex];
                m_InternalArray[endIndex] = tempValue;
            }
        }

        public void Reverse()
        {
            Reverse(0, m_UsedElements);
        }

        public void TryAdjustCapacityRequirements()
        {
            if (Capacity == 0)
                Capacity = 1;
            else if (Count == Capacity)
                Capacity = 2 * Capacity;
        }

        public void Add(T item)
        {
            TryAdjustCapacityRequirements();

            m_InternalArray[m_UsedElements++] = item;
        }

        public void Insert(int index, T item)
        {
            TryAdjustCapacityRequirements();

            // move everything over
            for (int i = m_UsedElements; i > index; i--)
                m_InternalArray[i] = m_InternalArray[i - 1];

            m_InternalArray[index] = item;
            m_UsedElements++;
        }

        public void RemoveAt(int index)
        {
            m_UsedElements--;
            for (int i = index; i < m_UsedElements; i++)
            {
                m_InternalArray[i] = m_InternalArray[i + 1];
            }
        }

        public void Clear()
        {
            m_UsedElements = 0;
        }

        public void Dispose(bool recursive)
        {
            if (IsCreated)
                m_InternalArray.Dispose(recursive);
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
