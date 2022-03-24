using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace UnityEngine.Rendering.Universal
{
    internal struct PathList<T> : IDisposable where T : struct 
    {
        unsafe void* m_InternalArray;
        AtomicSafetyHandle m_SafetyHandle;
        int m_Capacity;
        int m_UsedElements;
        bool m_IsCreated;

        private void SetInternalPointer(NativeArray<T> array)
        {
            unsafe
            {
                m_InternalArray = NativeArrayUnsafeUtility.GetUnsafePtr(array);
                m_SafetyHandle = NativeArrayUnsafeUtility.GetAtomicSafetyHandle<T>(array);
            }
        }

        private NativeArray<T> GetNativeArray(Allocator allocator = Allocator.Invalid)
        {
            unsafe
            {
                if (m_InternalArray == null)
                    return new NativeArray<T>();
                else
                {
                    NativeArray<T> internalArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(m_InternalArray, m_Capacity, allocator);
                    NativeArrayUnsafeUtility.SetAtomicSafetyHandle<T>(ref internalArray, m_SafetyHandle);
                    return internalArray;
                }
            }
        }

        public PathList(int capacity)
        {
            NativeArray<T> array = new NativeArray<T>(capacity, Allocator.Persistent);
            m_Capacity = capacity;
            m_UsedElements = 0;
            m_IsCreated = array.IsCreated;  // check in case we ran out of memory or there was some other error
            m_SafetyHandle = default(AtomicSafetyHandle);

            unsafe
            {
                m_InternalArray = null;
            }

            SetInternalPointer(array);
        }



        public void CopyTo(int index, NativeArray<T> array, int arrayIndex, int count)
        {
            unsafe
            {
                NativeArray<T> internalArray = GetNativeArray();
                for (int i = 0; i < count; i++)
                {
                    array[arrayIndex + i] = internalArray[index + i];
                }
            }
        }

        public void CopyTo(int index, PathList<T> pathList, int arrayIndex, int count)
        {
            CopyTo(index, pathList.GetNativeArray(), arrayIndex, count);
        }

        public void CopyTo(NativeArray<T> array, int arrayIndex)
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
                if (m_IsCreated)
                    return m_Capacity;
                else
                    return 0;
            }
            set
            {
                int newCapacity = value;
                NativeArray<T> newArray = new NativeArray<T>(newCapacity, Allocator.Persistent);
                CopyTo(newArray, 0);

                // This will be false the very first time this is set
                if (m_IsCreated)
                    GetNativeArray(Allocator.Persistent).Dispose();

                SetInternalPointer(newArray);

                m_Capacity = newCapacity;
                m_IsCreated = true;
            }
        }

        public int Count => m_UsedElements;

        public T this[int index]
        {
            get
            {
                return GetNativeArray()[index];
            }
            set
            {
                NativeArray<T> internalArray = GetNativeArray();
                internalArray[index] = value;
            }
        }

        public void Reverse(int index, int count)
        {
            NativeArray<T> internalArray = GetNativeArray();

            int halfCount = count >> 1;
            for (int i = 0; i < halfCount; i++)
            {
                int startIndex = index + i;
                int endIndex = index + count - 1;

                // Swap
                T tempValue = internalArray[startIndex];
                internalArray[startIndex] = internalArray[endIndex];
                internalArray[endIndex] = tempValue;
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

            NativeArray<T> internalArray = GetNativeArray();
            internalArray[m_UsedElements++] = item;
        }

        public void Insert(int index, T item)
        {
            TryAdjustCapacityRequirements();

            NativeArray<T> internalArray = GetNativeArray();

            // move everything over
            for (int i = m_UsedElements; i > index; i--)
                internalArray[i] = internalArray[i - 1];

            internalArray[index] = item;
            m_UsedElements++;
        }

        public void RemoveAt(int index)
        {
            NativeArray<T> internalArray = GetNativeArray();

            m_UsedElements--;
            for (int i = index; i < m_UsedElements; i++)
            {
                internalArray[i] = internalArray[i + 1];
            }
        }

        public void Clear()
        {
            m_UsedElements = 0;
        }

        public void TryToDispose(T item)
        {
            if (typeof(T) is IDisposable)
            {
                IDisposable disposableItem = (IDisposable)item;
                disposableItem.Dispose();
            }
        }

        public void Dispose()
        {
            if (m_IsCreated)
            {
                NativeArray<T> internalArray = GetNativeArray(Allocator.Persistent);

                // If we are holding disposable stuff, dispose of the elements...
                if (typeof(T) is IDisposable)
                {
                    internalArray = GetNativeArray();
                    for (int i = 0; i < m_UsedElements; i++)
                    {
                        IDisposable disposableItem = (IDisposable)internalArray[i];
                        disposableItem.Dispose();
                    }
                }

                internalArray.Dispose();
            }

            m_IsCreated = false;
        }
    }
}
