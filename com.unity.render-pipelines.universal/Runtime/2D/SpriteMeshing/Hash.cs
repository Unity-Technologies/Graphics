using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public class Hash
    {
        // Wrapper in case we want/need to change our hashing algorithm later

        public static uint GetHash<T>(T data) where T : struct
        {
            int size = Marshal.SizeOf(data);
            byte[] bytes = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(data, ptr, true);
            Marshal.Copy(ptr, bytes, 0, size);
            Marshal.FreeHGlobal(ptr);

            return GetHash(bytes);
        }

        public static uint GetHash(byte[] data)
        {
            return MurmurHash2.Hash(data);
        }
    }
}
