using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public struct Reference<T> where T : struct
{
    unsafe IntPtr m_Ptr;

    static Reference<RefType> Create<RefType>() where RefType : struct
    {
        unsafe
        {
            Reference<RefType> retRef = new Reference<RefType>();
            retRef.m_Ptr = new IntPtr(UnsafeUtility.Malloc(UnsafeUtility.SizeOf(typeof(RefType)), UnsafeUtility.AlignOf<RefType>(), Allocator.Temp));
            return retRef;
        }
    }

    void SetValue(T value)
    {
        Marshal.StructureToPtr<T>(value, m_Ptr, false);
    }

    T GetValue()
    {
        return Marshal.PtrToStructure<T>(m_Ptr);
    }
}
