using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

internal struct PathArray<T> : IDisposable where T : struct
{
    const string k_CreationError = "Array has either been disposed or has not been created with a size.";
    const string k_OutOfRangeError = "Array index out of range.";

    private unsafe byte* m_InternalArray;
    private int m_ElementSize;
    public int  m_Length;
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
                Debug.Assert(IsCreated, k_CreationError);
                Debug.Assert(index >= 0 && index < m_Length, k_OutOfRangeError);
                return Marshal.PtrToStructure<T>((IntPtr)(m_InternalArray + m_ElementSize * index)); ;
            }
        }
        set
        {
            unsafe
            {
                Debug.Assert(IsCreated, k_CreationError);
                Debug.Assert(index >= 0 && index < m_Length, k_OutOfRangeError);
                UnsafeUtility.CopyStructureToPtr<T>(ref value, m_InternalArray + m_ElementSize * index);
            }
        }
    }


    public void Dispose(bool recursive)
    {
        unsafe
        {
            if (IsCreated)
            {
                // Allow for recursive disposal of
                if (recursive && typeof(T) is IDisposable)
                {
                    for (int i = 0; i < m_Length; i++)
                    {
                        IDisposable element = (IDisposable)Marshal.PtrToStructure<T>((IntPtr)(m_InternalArray + m_ElementSize * i));
                        element.Dispose();
                    }
                }

                UnsafeUtility.Free(m_InternalArray, Allocator.Persistent);

                m_IsCreated = false;
            }
        }
    }


    public void Dispose()
    {
        Dispose(true);
    }
}
