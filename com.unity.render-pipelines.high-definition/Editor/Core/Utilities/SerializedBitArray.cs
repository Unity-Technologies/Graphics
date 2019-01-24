using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.Rendering
{
    public static class SerializedBitArrayUrtilities
    {
        public static bool GetBitArrayAt(this SerializedProperty property, uint bitIndex)
        {
            const string baseTypeName = "BitArray";
            string type = property.type;
            uint capacity;
            if (type.StartsWith(baseTypeName) && uint.TryParse(type.Substring(baseTypeName.Length), out capacity))
            {
                switch (capacity)
                {
                    case 8u:   return property.Get8(bitIndex);
                    case 16u:  return property.Get16(bitIndex);
                    case 32u:  return property.Get32(bitIndex);
                    case 64u:  return property.Get64(bitIndex);
                    case 128u: return property.Get128(bitIndex);
                    case 256u: return property.Get256(bitIndex);
                }
            }
            throw new System.ArgumentException("Trying to call Get on unknown BitArray");
        }

        public static void SetBitArrayAt(this SerializedProperty property, uint bitIndex, bool value)
        {
            const string baseTypeName = "BitArray";
            string type = property.type;
            uint capacity;
            if (type.StartsWith(baseTypeName) && uint.TryParse(type.Substring(baseTypeName.Length), out capacity))
            {
                switch (capacity)
                {
                    case 8u:   property.Set8(bitIndex, value);   return;
                    case 16u:  property.Set16(bitIndex, value);  return;
                    case 32u:  property.Set32(bitIndex, value);  return;
                    case 64u:  property.Set64(bitIndex, value);  return;
                    case 128u: property.Set128(bitIndex, value); return;
                    case 256u: property.Set256(bitIndex, value); return;
                }
            }
            throw new System.ArgumentException("Trying to call Get on unknown BitArray");
        }

        public static uint GetBitArrayCapacity(this SerializedProperty property)
        {
            const string baseTypeName = "BitArray";
            string type = property.type;
            uint capacity;
            if (type.StartsWith(baseTypeName) && uint.TryParse(type.Substring(baseTypeName.Length), out capacity))
                return capacity;
            throw new System.ArgumentException("Trying to call Get on unknown BitArray");
        }

        static bool Get8(this SerializedProperty property, uint bitIndex)
            => BitArrayUtilities.Get8(bitIndex, (byte)property.FindPropertyRelative("data").intValue);
        static bool Get16(this SerializedProperty property, uint bitIndex)
            => BitArrayUtilities.Get16(bitIndex, (ushort)property.FindPropertyRelative("data").intValue);
        static bool Get32(this SerializedProperty property, uint bitIndex)
            => BitArrayUtilities.Get32(bitIndex, (uint)property.FindPropertyRelative("data").intValue);
        static bool Get64(this SerializedProperty property, uint bitIndex)
            => BitArrayUtilities.Get64(bitIndex, (ulong)property.FindPropertyRelative("data").longValue);
        static bool Get128(this SerializedProperty property, uint bitIndex)
            => BitArrayUtilities.Get128(
                bitIndex,
                (ulong)property.FindPropertyRelative("data1").longValue,
                (ulong)property.FindPropertyRelative("data2").longValue);
        static bool Get256(this SerializedProperty property, uint bitIndex)
            => BitArrayUtilities.Get256(
                bitIndex,
                (ulong)property.FindPropertyRelative("data1").longValue,
                (ulong)property.FindPropertyRelative("data2").longValue,
                (ulong)property.FindPropertyRelative("data3").longValue,
                (ulong)property.FindPropertyRelative("data4").longValue);

        static void Set8(this SerializedProperty property, uint bitIndex, bool value)
        {
            byte versionedData = (byte)property.FindPropertyRelative("data").intValue;
            BitArrayUtilities.Set8(bitIndex, ref versionedData, value);
            property.FindPropertyRelative("data").intValue = versionedData;
        }
        static void Set16(this SerializedProperty property, uint bitIndex, bool value)
        {
            ushort versionedData = (ushort)property.FindPropertyRelative("data").intValue;
            BitArrayUtilities.Set16(bitIndex, ref versionedData, value);
            property.FindPropertyRelative("data").intValue = versionedData;
        }
        static void Set32(this SerializedProperty property, uint bitIndex, bool value)
        {
            int versionedData = property.FindPropertyRelative("data").intValue;
            uint trueData;
            unsafe
            {
                trueData = *(uint*)(&versionedData);
            }
            BitArrayUtilities.Set32(bitIndex, ref trueData, value);
            unsafe
            {
                versionedData = *(int*)(&trueData);
            }
            property.FindPropertyRelative("data").intValue = versionedData;
        }
        static void Set64(this SerializedProperty property, uint bitIndex, bool value)
        {
            long versionedData = property.FindPropertyRelative("data").longValue;
            ulong trueData;
            unsafe
            {
                trueData = *(ulong*)(&versionedData);
            }
            BitArrayUtilities.Set64(bitIndex, ref trueData, value);
            unsafe
            {
                versionedData = *(long*)(&trueData);
            }
            property.FindPropertyRelative("data").longValue = versionedData;
        }
        static void Set128(this SerializedProperty property, uint bitIndex, bool value)
        {
            long versionedData1 = property.FindPropertyRelative("data1").longValue;
            long versionedData2 = property.FindPropertyRelative("data2").longValue;
            ulong trueData1;
            ulong trueData2;
            unsafe
            {
                trueData1 = *(ulong*)(&versionedData1);
                trueData2 = *(ulong*)(&versionedData2);
            }
            BitArrayUtilities.Set128(bitIndex, ref trueData1, ref trueData2, value);
            unsafe
            {
                versionedData1 = *(long*)(&trueData1);
                versionedData2 = *(long*)(&trueData2);
            }
            property.FindPropertyRelative("data1").longValue = versionedData1;
            property.FindPropertyRelative("data2").longValue = versionedData2;
        }
        static void Set256(this SerializedProperty property, uint bitIndex, bool value)
        {
            long versionedData1 = property.FindPropertyRelative("data1").longValue;
            long versionedData2 = property.FindPropertyRelative("data2").longValue;
            long versionedData3 = property.FindPropertyRelative("data3").longValue;
            long versionedData4 = property.FindPropertyRelative("data4").longValue;
            ulong trueData1;
            ulong trueData2;
            ulong trueData3;
            ulong trueData4;
            unsafe
            {
                trueData1 = *(ulong*)(&versionedData1);
                trueData2 = *(ulong*)(&versionedData2);
                trueData3 = *(ulong*)(&versionedData3);
                trueData4 = *(ulong*)(&versionedData4);
            }
            BitArrayUtilities.Set256(bitIndex, ref trueData1, ref trueData2, ref trueData3, ref trueData4, value);
            unsafe
            {
                versionedData1 = *(long*)(&trueData1);
                versionedData2 = *(long*)(&trueData2);
                versionedData3 = *(long*)(&trueData3);
                versionedData4 = *(long*)(&trueData4);
            }
            property.FindPropertyRelative("data1").longValue = versionedData1;
            property.FindPropertyRelative("data2").longValue = versionedData2;
            property.FindPropertyRelative("data3").longValue = versionedData3;
            property.FindPropertyRelative("data4").longValue = versionedData4;
        }
    }
}
