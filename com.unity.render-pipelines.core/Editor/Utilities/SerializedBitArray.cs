using UnityEngine.Rendering;
using System;
using System.Reflection;
using System.Linq.Expressions;

namespace UnityEditor.Rendering
{
    public static class SerializedBitArrayUtilities
    {
        public static SerializedBitArray8 ToSerializeBitArray8(this SerializedProperty serializedProperty)
        {
            if (!IsBitArrayOfCapacity(serializedProperty, 8u))
                throw new Exception("Cannot get SerializeBitArray of this Capacity");
            return new SerializedBitArray8(serializedProperty);
        }

        public static bool TryGetSerializeBitArray8(this SerializedProperty serializedProperty, out SerializedBitArray8 serializedBitArray)
        {
            serializedBitArray = null;
            if (!IsBitArrayOfCapacity(serializedProperty, 8u))
                return false;
            serializedBitArray = new SerializedBitArray8(serializedProperty);
            return true;
        }

        public static SerializedBitArray16 ToSerializeBitArray16(this SerializedProperty serializedProperty)
        {
            if (!IsBitArrayOfCapacity(serializedProperty, 16u))
                throw new Exception("Cannot get SerializeBitArray of this Capacity");
            return new SerializedBitArray16(serializedProperty);
        }

        public static bool TryGetSerializeBitArray16(this SerializedProperty serializedProperty, out SerializedBitArray16 serializedBitArray)
        {
            serializedBitArray = null;
            if (!IsBitArrayOfCapacity(serializedProperty, 16u))
                return false;
            serializedBitArray = new SerializedBitArray16(serializedProperty);
            return true;
        }
        
        public static SerializedBitArray32 ToSerializeBitArray32(this SerializedProperty serializedProperty)
        {
            if (!IsBitArrayOfCapacity(serializedProperty, 32u))
                throw new Exception("Cannot get SerializeBitArray of this Capacity");
            return new SerializedBitArray32(serializedProperty);
        }

        public static bool TryGetSerializeBitArray32(this SerializedProperty serializedProperty, out SerializedBitArray32 serializedBitArray)
        {
            serializedBitArray = null;
            if (!IsBitArrayOfCapacity(serializedProperty, 32u))
                return false;
            serializedBitArray = new SerializedBitArray32(serializedProperty);
            return true;
        }
        
        public static SerializedBitArray64 ToSerializeBitArray64(this SerializedProperty serializedProperty)
        {
            if (!IsBitArrayOfCapacity(serializedProperty, 64u))
                throw new Exception("Cannot get SerializeBitArray of this Capacity");
            return new SerializedBitArray64(serializedProperty);
        }

        public static bool TryGetSerializeBitArray64(this SerializedProperty serializedProperty, out SerializedBitArray64 serializedBitArray)
        {
            serializedBitArray = null;
            if (!IsBitArrayOfCapacity(serializedProperty, 64u))
                return false;
            serializedBitArray = new SerializedBitArray64(serializedProperty);
            return true;
        }
        
        public static SerializedBitArray128 ToSerializeBitArray128(this SerializedProperty serializedProperty)
        {
            if (!IsBitArrayOfCapacity(serializedProperty, 128u))
                throw new Exception("Cannot get SerializeBitArray of this Capacity");
            return new SerializedBitArray128(serializedProperty);
        }

        public static bool TryGetSerializeBitArray128(this SerializedProperty serializedProperty, out SerializedBitArray128 serializedBitArray)
        {
            serializedBitArray = null;
            if (!IsBitArrayOfCapacity(serializedProperty, 128u))
                return false;
            serializedBitArray = new SerializedBitArray128(serializedProperty);
            return true;
        }
        
        public static SerializedBitArray256 ToSerializeBitArray256(this SerializedProperty serializedProperty)
        {
            if (!IsBitArrayOfCapacity(serializedProperty, 256u))
                throw new Exception("Cannot get SerializeBitArray of this Capacity");
            return new SerializedBitArray256(serializedProperty);
        }

        public static bool TryGetSerializeBitArray256(this SerializedProperty serializedProperty, out SerializedBitArray256 serializedBitArray)
        {
            serializedBitArray = null;
            if (!IsBitArrayOfCapacity(serializedProperty, 256u))
                return false;
            serializedBitArray = new SerializedBitArray256(serializedProperty);
            return true;
        }

        static bool IsBitArrayOfCapacity(SerializedProperty serializedProperty, uint capacity)
        {
            const string baseTypeName = "BitArray";
            string type = serializedProperty.type;
            uint serializedCapacity;
            return type.StartsWith(baseTypeName)
                && uint.TryParse(type.Substring(baseTypeName.Length), out serializedCapacity)
                && capacity == serializedCapacity;
        }
    }

    public interface ISerializedBitArray
    {
        uint capacity { get; }
        bool GetBitAt(uint bitIndex);
        void SetBitAt(uint bitIndex, bool value);
        bool HasBitMultipleDifferentValue(uint bitIndex);
    }

    public abstract class SerializedBitArray : ISerializedBitArray
    {
        // Note: this should be exposed at the same time as issue with type other than Int32 is fixed on C++ side
        protected static Action<SerializedProperty, int, bool> SetBitAtIndexForAllTargetsImmediate;
        protected static Func<SerializedProperty, int> HasMultipleDifferentValuesBitwise;
        static SerializedBitArray()
        {
            var type = typeof(SerializedProperty);
            var setBitAtIndexForAllTargetsImmediateMethodInfo = type.GetMethod("SetBitAtIndexForAllTargetsImmediate", BindingFlags.Instance | BindingFlags.NonPublic);
            var hasMultipleDifferentValuesBitwisePropertyInfo = type.GetProperty("hasMultipleDifferentValuesBitwise", BindingFlags.Instance | BindingFlags.NonPublic);
            var serializedPropertyParameter = Expression.Parameter(typeof(SerializedProperty), "property");
            var indexParameter = Expression.Parameter(typeof(int), "index");
            var valueParameter = Expression.Parameter(typeof(bool), "value");
            var hasMultipleDifferentValuesBitwiseProperty = Expression.Property(serializedPropertyParameter, hasMultipleDifferentValuesBitwisePropertyInfo);
            var setBitAtIndexForAllTargetsImmediateCall = Expression.Call(serializedPropertyParameter, setBitAtIndexForAllTargetsImmediateMethodInfo, indexParameter, valueParameter);
            var setBitAtIndexForAllTargetsImmediateLambda = Expression.Lambda<Action<SerializedProperty, int, bool>>(setBitAtIndexForAllTargetsImmediateCall, serializedPropertyParameter, indexParameter, valueParameter);
            var hasMultipleDifferentValuesBitwiseLambda = Expression.Lambda<Func<SerializedProperty, int>>(hasMultipleDifferentValuesBitwiseProperty, serializedPropertyParameter);
            SetBitAtIndexForAllTargetsImmediate = setBitAtIndexForAllTargetsImmediateLambda.Compile();
            HasMultipleDifferentValuesBitwise = hasMultipleDifferentValuesBitwiseLambda.Compile();
        }

        protected SerializedProperty m_SerializedProperty;
        SerializedProperty[] m_SerializedProperties;

        public uint capacity { get; }

        internal SerializedBitArray(SerializedProperty serializedProperty, uint capacity)
        {
            this.capacity = capacity;
            m_SerializedProperty = serializedProperty;
        }

        protected SerializedProperty[] GetOrInitializeSerializedProperties()
        {
            if (m_SerializedProperties == null)
            {
                UnityEngine.Object[] targets = m_SerializedProperty.serializedObject.targetObjects;
                int size = targets.Length;
                if (size == 1)
                    m_SerializedProperties = new[] { m_SerializedProperty };
                else
                {
                    string propertyPath = m_SerializedProperty.propertyPath;
                    m_SerializedProperties = new SerializedProperty[size];
                    for (int i = 0; i < size; ++i)
                    {
                        m_SerializedProperties[i] = new SerializedObject(targets[i]).FindProperty(propertyPath);
                    }
                }
            }
            return m_SerializedProperties;
        }

        public bool HasBitMultipleDifferentValue(uint bitIndex)
        {
            if (bitIndex >= capacity)
                throw new IndexOutOfRangeException("Index out of bound in BitArray" + capacity);
            return HasBitMultipleDifferentValue_Internal(bitIndex);
        }

        abstract protected bool HasBitMultipleDifferentValue_Internal(uint bitIndex);

        /// <summary>
        /// Safety: serializedProperty must match its path
        /// </summary>
        /// <param name="propertyPath">serializedProperty must match its path</param>
        /// <param name="serializedProperty">serializedProperty must match its path</param>
        /// <param name="bitIndex"></param>
        /// <returns></returns>
        unsafe protected bool HasBitMultipleDifferentValue_For64Bits(string propertyPath, SerializedProperty serializedProperty, uint bitIndex)
        {
            if (!serializedProperty.hasMultipleDifferentValues)
                return false;

            var serializedProperties = GetOrInitializeSerializedProperties();
            int length = serializedProperties.Length;
            ulong mask = 1uL << (int)bitIndex;
            bool value = ((ulong)m_SerializedProperties[0].FindPropertyRelative(propertyPath).longValue & mask) != 0uL;
            for (int i = 1; i < length; ++i)
            {
                if ((((ulong)m_SerializedProperties[i].FindPropertyRelative(propertyPath).longValue & mask) != 0uL) ^ value)
                    return true;
            }
            return false;
        }

        public bool GetBitAt(uint bitIndex)
        {
            if (bitIndex >= capacity)
                throw new IndexOutOfRangeException("Index out of bound in BitArray" + capacity);
            return GetBitAt_Internal(bitIndex);
        }

        abstract protected bool GetBitAt_Internal(uint bitIndex);


        public void SetBitAt(uint bitIndex, bool value)
        {
            if (bitIndex >= capacity)
                throw new IndexOutOfRangeException("Index out of bound in BitArray" + capacity);
            SetBitAt_Internal(bitIndex, value);
        }

        abstract protected void SetBitAt_Internal(uint bitIndex, bool value);

        protected void ResyncSerialization()
        {
            foreach (var property in m_SerializedProperties)
                property.serializedObject.ApplyModifiedProperties();
            Update();
        }

        public void Update()
        {
            foreach (var property in m_SerializedProperties)
                property.serializedObject.Update();
            m_SerializedProperty.serializedObject.Update();
        }
    }

    public class SerializedBitArray8 : SerializedBitArray
    {
        SerializedProperty m_Data;

        public SerializedBitArray8(SerializedProperty serializedProperty) : base(serializedProperty, 8u)
            => m_Data = m_SerializedProperty.FindPropertyRelative("data");

        protected override bool HasBitMultipleDifferentValue_Internal(uint bitIndex)
            => (HasMultipleDifferentValuesBitwise(m_Data) & (1 << (int)bitIndex)) != 0;

        protected override bool GetBitAt_Internal(uint bitIndex)
            => BitArrayUtilities.Get8(bitIndex, (byte)m_Data.intValue);

        protected override void SetBitAt_Internal(uint bitIndex, bool value)
        {
            foreach (var property in GetOrInitializeSerializedProperties())
            {
                byte versionedData = (byte)property.FindPropertyRelative("data").intValue;
                BitArrayUtilities.Set8(bitIndex, ref versionedData, value);
                property.FindPropertyRelative("data").intValue = versionedData;
            }
            ResyncSerialization();
        }
    }

    public class SerializedBitArray16 : SerializedBitArray
    {
        SerializedProperty m_Data;

        public SerializedBitArray16(SerializedProperty serializedProperty) : base(serializedProperty, 16u)
            => m_Data = m_SerializedProperty.FindPropertyRelative("data");

        protected override bool HasBitMultipleDifferentValue_Internal(uint bitIndex)
            => (HasMultipleDifferentValuesBitwise(m_Data) & (1 << (int)bitIndex)) != 0;

        protected override bool GetBitAt_Internal(uint bitIndex)
            => BitArrayUtilities.Get16(bitIndex, (ushort)m_Data.intValue);

        protected override void SetBitAt_Internal(uint bitIndex, bool value)
        {
            foreach (var property in GetOrInitializeSerializedProperties())
            {
                ushort versionedData = (ushort)property.FindPropertyRelative("data").intValue;
                BitArrayUtilities.Set16(bitIndex, ref versionedData, value);
                property.FindPropertyRelative("data").intValue = versionedData;
            }
            ResyncSerialization();
        }
    }

    public class SerializedBitArray32 : SerializedBitArray
    {
        SerializedProperty m_Data;

        public SerializedBitArray32(SerializedProperty serializedProperty) : base(serializedProperty, 32u)
            => m_Data = m_SerializedProperty.FindPropertyRelative("data");

        protected override bool HasBitMultipleDifferentValue_Internal(uint bitIndex)
            => (HasMultipleDifferentValuesBitwise(m_Data) & (1 << (int)bitIndex)) != 0;

        protected override bool GetBitAt_Internal(uint bitIndex)
            => BitArrayUtilities.Get32(bitIndex, (uint)m_Data.intValue);

        protected override void SetBitAt_Internal(uint bitIndex, bool value)
        {
            foreach (var property in GetOrInitializeSerializedProperties())
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
            ResyncSerialization();
        }
    }

    public class SerializedBitArray64 : SerializedBitArray
    {
        SerializedProperty m_Data;

        public SerializedBitArray64(SerializedProperty serializedProperty) : base(serializedProperty, 64u)
            => m_Data = m_SerializedProperty.FindPropertyRelative("data");

        protected override bool HasBitMultipleDifferentValue_Internal(uint bitIndex)
            => HasBitMultipleDifferentValue_For64Bits("data", m_Data, bitIndex);

        protected override bool GetBitAt_Internal(uint bitIndex)
            => BitArrayUtilities.Get64(bitIndex, (ulong)m_Data.longValue);

        protected override void SetBitAt_Internal(uint bitIndex, bool value)
        {
            foreach (var property in GetOrInitializeSerializedProperties())
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
            ResyncSerialization();
        }
    }

    public class SerializedBitArray128 : SerializedBitArray
    {
        SerializedProperty m_Data1;
        SerializedProperty m_Data2;

        public SerializedBitArray128(SerializedProperty serializedProperty) : base(serializedProperty, 128u)
        {
            m_Data1 = m_SerializedProperty.FindPropertyRelative("data1");
            m_Data2 = m_SerializedProperty.FindPropertyRelative("data2");
        }

        protected override bool HasBitMultipleDifferentValue_Internal(uint bitIndex)
            => bitIndex < 64u
                ? HasBitMultipleDifferentValue_For64Bits("data1", m_Data1, bitIndex)
                : HasBitMultipleDifferentValue_For64Bits("data2", m_Data2, bitIndex - 64u);


        protected override bool GetBitAt_Internal(uint bitIndex)
            => BitArrayUtilities.Get128(
                bitIndex,
                (ulong)m_SerializedProperty.FindPropertyRelative("data1").longValue,
                (ulong)m_SerializedProperty.FindPropertyRelative("data2").longValue);

        protected override void SetBitAt_Internal(uint bitIndex, bool value)
        {
            foreach (var property in GetOrInitializeSerializedProperties())
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
            ResyncSerialization();
        }
    }

    public class SerializedBitArray256 : SerializedBitArray
    {
        SerializedProperty m_Data1;
        SerializedProperty m_Data2;
        SerializedProperty m_Data3;
        SerializedProperty m_Data4;

        public SerializedBitArray256(SerializedProperty serializedProperty) : base(serializedProperty, 128u)
        {
            m_Data1 = m_SerializedProperty.FindPropertyRelative("data1");
            m_Data2 = m_SerializedProperty.FindPropertyRelative("data2");
            m_Data3 = m_SerializedProperty.FindPropertyRelative("data3");
            m_Data4 = m_SerializedProperty.FindPropertyRelative("data4");
        }

        protected override bool HasBitMultipleDifferentValue_Internal(uint bitIndex)
            => bitIndex < 128u
                ? bitIndex < 64u
                    ? HasBitMultipleDifferentValue_For64Bits("data1", m_Data1, bitIndex)
                    : HasBitMultipleDifferentValue_For64Bits("data2", m_Data2, bitIndex - 64u)
                : bitIndex < 192u
                    ? HasBitMultipleDifferentValue_For64Bits("data3", m_Data3, bitIndex - 128u)
                    : HasBitMultipleDifferentValue_For64Bits("data4", m_Data4, bitIndex - 192u);
        
        protected override bool GetBitAt_Internal(uint bitIndex)
            => BitArrayUtilities.Get256(
                bitIndex,
                (ulong)m_SerializedProperty.FindPropertyRelative("data1").longValue,
                (ulong)m_SerializedProperty.FindPropertyRelative("data2").longValue,
                (ulong)m_SerializedProperty.FindPropertyRelative("data3").longValue,
                (ulong)m_SerializedProperty.FindPropertyRelative("data4").longValue);

        protected override void SetBitAt_Internal(uint bitIndex, bool value)
        {
            foreach (var property in GetOrInitializeSerializedProperties())
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
            ResyncSerialization();
        }
    }
}
