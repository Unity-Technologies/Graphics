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

    internal struct Reference<T> where T : struct
    {
        unsafe IntPtr m_Ptr;

        static public Reference<T> Create()
        {
            unsafe
            {
                Reference<T> retRef = new Reference<T>();
                retRef.m_Ptr = new IntPtr(UnsafeUtility.Malloc(UnsafeUtility.SizeOf(typeof(T)), UnsafeUtility.AlignOf<T>(), Allocator.Temp));
                return retRef;
            }
        }

        public void SetValue(T value)
        {
            Marshal.StructureToPtr<T>(value, m_Ptr, false);
        }

        public T GetValue()
        {
            return Marshal.PtrToStructure<T>(m_Ptr);
        }
    }
}
