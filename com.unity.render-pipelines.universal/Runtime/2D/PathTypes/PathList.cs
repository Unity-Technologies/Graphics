using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace UnityEngine.Rendering.Universal
{
    // Testing in PathStructTests
    internal struct PathList<T> : IDisposable where T : struct 
    {
        IntPtr m_InternalArrayPtr; // Pointer to a PathArray

        public bool IsCreated
        {
            get
            {
                unsafe
                {
                    return m_InternalArrayPtr.ToPointer() != null;
                }
            }
         
        }

        public PathList(int capacity)
        {
            m_InternalArrayPtr = AllocatePathArray(capacity);
        }

        static private IntPtr AllocatePathArray(int capacity)
        {
            unsafe
            {
                int sizeOfT = UnsafeUtility.SizeOf(typeof(PathArray<T>));
                void* ptrToPathArray = (byte*)UnsafeUtility.Malloc(sizeOfT, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);

                PathArray<T> newPathArray = new PathArray<T>(capacity);
                UnsafeUtility.CopyStructureToPtr<PathArray<T>>(ref newPathArray, ptrToPathArray);

                return new IntPtr(ptrToPathArray);
            }
        }

        private PathArray<T> internalArray
        {
            set
            {
                unsafe
                {
                    UnsafeUtility.CopyStructureToPtr<PathArray<T>>(ref value, m_InternalArrayPtr.ToPointer());
                }
            }
            get
            {
                unsafe
                {
                    return Marshal.PtrToStructure<PathArray<T>>(m_InternalArrayPtr);
                }
            }
        }

        private void SetArrayElement(int index, T item)
        {
            PathArray<T> array = internalArray;
            array[index] = item;

            T foo = array[index];
        }

        public void CopyTo(int index, PathArray<T> array, int arrayIndex, int count)
        {
            unsafe
            {
                Debug.Assert(array.IsCreated && internalArray.IsCreated, PathTypes.k_CreationError);

                for (int i = 0; i < count; i++)
                {
                    array[arrayIndex + i] = internalArray[index + i];
                }
            }
        }

        public void CopyTo(int index, PathList<T> pathList, int arrayIndex, int count)
        {
            int copySize = arrayIndex + count;
            if (pathList.Capacity < copySize)
                pathList.Capacity = copySize;

            CopyTo(index, pathList.internalArray, arrayIndex, count);
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
                    return internalArray.Length;
                else
                    return 0;
            }
            set
            {
                int newCapacity = value;
                PathArray<T> newArray = new PathArray<T>(newCapacity);

                if (IsCreated && internalArray.Length > 0)
                {
                    CopyTo(newArray, 0);
                    newArray.UsedElements = internalArray.UsedElements;
                }

                // This will be false the very first time this is set
                if (IsCreated)
                    internalArray.Dispose(PathTypes.DisposeOptions.Shallow);  // A shallow dispose is done since the elements are copied to the new array
                else
                    m_InternalArrayPtr = AllocatePathArray(newCapacity);

                internalArray = newArray;
            }
        }

        public int Count
        {
            get
            {
                if (IsCreated)
                    return internalArray.UsedElements;
                else
                    return 0;
            }
        }


        public T this[int index]
        {
            get
            {
                return internalArray[index];
            }
            set
            {
                SetArrayElement(index, value);
            }
        }

        public void Reverse(int index, int count)
        {
            int halfCount = count >> 1;
            for (int i = 0; i < halfCount; i++)
            {
                int startIndex = index + i;
                int endIndex = index + count - 1 - i;

                // Swap
                T tempValue = internalArray[startIndex];

                SetArrayElement(startIndex, internalArray[endIndex]);
                SetArrayElement(endIndex, tempValue);
            }
        }


        public void Reverse()
        {
            Reverse(0, internalArray.UsedElements);
        }

        // Needs test
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

            SetArrayElement(internalArray.UsedElements, item);

            PathArray<T> pathArray = internalArray;
            pathArray.UsedElements = (internalArray.UsedElements + 1);
        }

        public void Insert(int index, T item)
        {
            TryAdjustCapacityRequirements();

            // move everything over
            for (int i = internalArray.UsedElements; i > index; i--)
                SetArrayElement(i, internalArray[i - 1]);


            SetArrayElement(index, item);

            PathArray<T> pathArray = internalArray;
            pathArray.UsedElements = pathArray.UsedElements + 1;
        }

        // Needs Test
        public void RemoveAt(int index)
        {
            if (IsCreated)
            {
                PathArray<T> pathArray = internalArray;
                pathArray.UsedElements = pathArray.UsedElements - 1;

                for (int i = index; i < internalArray.UsedElements; i++)
                {
                    SetArrayElement(i, internalArray[i + 1]);
                }
            }
        }

        public void Clear(PathTypes.DisposeOptions option = PathTypes.DisposeOptions.Deep)
        {
            if (IsCreated)
            {
                if (option == PathTypes.DisposeOptions.Deep)
                    internalArray.DisposeElements(0, internalArray.UsedElements);

                PathArray<T> pathArray = internalArray;
                pathArray.UsedElements = 0;
            }
        }

        public void Dispose(PathTypes.DisposeOptions option)
        {
            if (IsCreated)
            {
                unsafe
                {
                    internalArray.Dispose(option);
                    UnsafeUtility.Free(m_InternalArrayPtr.ToPointer(), Allocator.Persistent);
                    m_InternalArrayPtr = new IntPtr(null);
                }
            }
        }

        public void Dispose()
        {
            Dispose(PathTypes.DisposeOptions.Deep);
        }
    }
}
