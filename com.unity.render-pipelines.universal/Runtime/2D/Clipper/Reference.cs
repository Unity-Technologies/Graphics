using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.Universal
{

    internal struct Reference<T> where T : unmanaged 
    {
        unsafe IntPtr m_Ptr;

        static public void Create(T value, out Reference<T> retRef)
        {
            unsafe
            {
                int sizeOfT = UnsafeUtility.SizeOf(typeof(T));
                retRef = new Reference<T>();
                retRef.m_Ptr = new IntPtr(UnsafeUtility.Malloc(sizeOfT, UnsafeUtility.AlignOf<T>(), Allocator.Temp));
                UnsafeUtility.MemClear(retRef.m_Ptr.ToPointer(), sizeOfT);
                retRef.DeRef() = value;
            }
        }

        public ref T DeRef()
        {
            unsafe
            {
                Debug.Assert(IsCreated);
                ref T foo = ref (*((T*)(m_Ptr.ToPointer())));
                return ref foo;
            }
        }

        public bool IsCreated { get { unsafe { return m_Ptr.ToPointer() != null; } } }
        public bool IsNull { get { unsafe { return m_Ptr.ToPointer() == null; } } }
        public bool IsEqual(Reference<T> arg)
        {
            unsafe
            {
                return m_Ptr.ToPointer() == arg.m_Ptr.ToPointer();
            }
        }
        public void SetNull() { unsafe { m_Ptr = new IntPtr(null); }  }

    }
}
