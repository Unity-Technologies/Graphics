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

        static public Reference<T> Create(T value)
        {
            unsafe
            {
                Reference<T> retRef = new Reference<T>();
                retRef.m_Ptr = new IntPtr(UnsafeUtility.Malloc(UnsafeUtility.SizeOf(typeof(T)), UnsafeUtility.AlignOf<T>(), Allocator.Temp));
                retRef.DeRef() = value;
                return retRef;
            }
        }

        public ref T DeRef()
        {
            unsafe
            {
                ref T foo = ref (*((T*)(m_Ptr.ToPointer())));
                return ref foo;
            }
        }

        public bool IsCreated { get { unsafe { return m_Ptr.ToPointer() == null; } } }
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
