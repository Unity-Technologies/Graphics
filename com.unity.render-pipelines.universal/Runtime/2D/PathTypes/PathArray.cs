using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.Universal
{
    // Testing in PathStructTests
    internal struct PathArray<T> : IDisposable where T : struct
    {
        private unsafe byte* m_InternalArray;
        private int m_ElementSize;
        public int m_Length;
        public bool m_IsCreated;

        public int Length => m_Length;
        public bool IsCreated => m_IsCreated;

        public PathArray(int count)
        {
            unsafe
            {
                int sizeOfT = UnsafeUtility.SizeOf(typeof(T));
                m_InternalArray = (byte*)UnsafeUtility.Malloc(sizeOfT * count, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
                m_ElementSize = sizeOfT;
                m_Length = count;
                m_IsCreated = true;
            }
        }

        public T this[int index]
        {
            get
            {
                unsafe
                {
                    Debug.Assert(IsCreated, PathTypes.k_CreationError);
                    Debug.Assert(index >= 0 && index < m_Length, PathTypes.k_OutOfRangeError);
                    return Marshal.PtrToStructure<T>((IntPtr)(m_InternalArray + m_ElementSize * index)); ;
                }
            }
            set
            {
                unsafe
                {
                    Debug.Assert(IsCreated, PathTypes.k_CreationError);
                    Debug.Assert(index >= 0 && index < m_Length, PathTypes.k_OutOfRangeError);
                    UnsafeUtility.CopyStructureToPtr<T>(ref value, m_InternalArray + m_ElementSize * index);
                }
            }
        }

        public void DisposeElements(int start, int count)
        {
            unsafe
            {
                if (IsCreated)
                {
                    Debug.Assert(start + count <= m_Length, PathTypes.k_OutOfRangeError);

                    for (int i = start; i < count; i++)
                    {
                        IDisposable element = Marshal.PtrToStructure<T>((IntPtr)(m_InternalArray + m_ElementSize * i)) as IDisposable;
                        if(element != null)
                            element.Dispose();
                    }
                }
            }
        }

        public void Dispose(PathTypes.DisposeOptions option)
        {
            unsafe
            {
                if (IsCreated)
                {
                    if (option == PathTypes.DisposeOptions.Deep)
                        DisposeElements(0, m_Length);

                    UnsafeUtility.Free(m_InternalArray, Allocator.Persistent);

                    m_IsCreated = false;
                }
            }
        }

        public void Dispose()
        {
            Dispose(PathTypes.DisposeOptions.Deep);
        }
    }
}
