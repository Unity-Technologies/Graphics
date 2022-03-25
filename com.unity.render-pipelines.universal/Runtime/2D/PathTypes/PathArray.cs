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
        private struct ArrayInfo
        {
            public int elementSize;
            public int length;
            public int usedElements;
        }

        private unsafe IntPtr m_InternalArrayPtr;
        private unsafe IntPtr m_InternalArrayInfoPtr;



        public int Length
        {
            get
            {
                return GetArrayInfo().length;
            }
        }

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

        public int UsedElements
        {
            get { return GetArrayInfo().usedElements; }
            set
            {
                unsafe
                {
                    ArrayInfo arrayInfo = GetArrayInfo();
                    arrayInfo.usedElements = value;
                    UnsafeUtility.CopyStructureToPtr<ArrayInfo>(ref arrayInfo, m_InternalArrayInfoPtr.ToPointer());
                }
            }
        }

        public PathArray(int count)
        {
            unsafe
            {
                int sizeOfT = UnsafeUtility.SizeOf(typeof(T));

                void* ptrToData = (byte*)UnsafeUtility.Malloc(sizeOfT * count, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);
                void* ptrToPtr = UnsafeUtility.Malloc(sizeof(IntPtr), UnsafeUtility.AlignOf<IntPtr>(), Allocator.Persistent); // This is a ptr to an array

                IntPtr intPtrToData = new IntPtr(ptrToData);
                UnsafeUtility.CopyStructureToPtr<IntPtr>(ref intPtrToData, ptrToPtr);
                m_InternalArrayPtr = new IntPtr(ptrToPtr);


                ArrayInfo arrayInfo = new ArrayInfo();
                arrayInfo.elementSize = sizeOfT;
                arrayInfo.length = count;
                arrayInfo.usedElements = 0;

                void* ptrToArrayInfo = (void*)UnsafeUtility.Malloc(sizeof(ArrayInfo), UnsafeUtility.AlignOf<ArrayInfo>(), Allocator.Persistent);
                m_InternalArrayInfoPtr = new IntPtr(ptrToArrayInfo);
                UnsafeUtility.CopyStructureToPtr<ArrayInfo>(ref arrayInfo, m_InternalArrayInfoPtr.ToPointer());
            }
        }

        private ArrayInfo GetArrayInfo()
        {
            return Marshal.PtrToStructure<ArrayInfo>(m_InternalArrayInfoPtr);
        }


        public unsafe byte* internalArray
        {
            get
            {
                IntPtr intPtrToPtr = Marshal.PtrToStructure<IntPtr>((IntPtr)(m_InternalArrayPtr));
                return (byte*)intPtrToPtr.ToPointer();
            }
        }

        public T this[int index]
        {
            get
            {
                unsafe
                {
                    Debug.Assert(IsCreated, PathTypes.k_CreationError);
                    Debug.Assert(index >= 0 && index < GetArrayInfo().length, PathTypes.k_OutOfRangeError);
                    return Marshal.PtrToStructure<T>((IntPtr)(internalArray + GetArrayInfo().elementSize * index));
                }
            }
            set
            {
                unsafe
                {
                    Debug.Assert(IsCreated, PathTypes.k_CreationError);
                    Debug.Assert(index >= 0 && index < GetArrayInfo().length, PathTypes.k_OutOfRangeError);
                    UnsafeUtility.CopyStructureToPtr<T>(ref value, internalArray + GetArrayInfo().elementSize * index);
                }
            }
        }

        public void DisposeElements(int start, int count)
        {
            unsafe
            {
                if (IsCreated)
                {
                    Debug.Assert(start + count <= GetArrayInfo().length, PathTypes.k_OutOfRangeError);

                    for (int i = start; i < count; i++)
                    {
                        IDisposable element = Marshal.PtrToStructure<T>((IntPtr)(internalArray + GetArrayInfo().elementSize * i)) as IDisposable;
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
                        DisposeElements(0, GetArrayInfo().length);

                    UnsafeUtility.Free(internalArray, Allocator.Persistent);
                    UnsafeUtility.Free(m_InternalArrayPtr.ToPointer(), Allocator.Persistent);
                    m_InternalArrayPtr = new IntPtr(null);

                    ArrayInfo arrayInfo = GetArrayInfo();
                    arrayInfo.usedElements = 0;
                    arrayInfo.length = 0;
                    UnsafeUtility.CopyStructureToPtr<ArrayInfo>(ref arrayInfo, m_InternalArrayInfoPtr.ToPointer());
                }
            }
        }

        public void Dispose()
        {
            Dispose(PathTypes.DisposeOptions.Deep);
        }
    }
}
