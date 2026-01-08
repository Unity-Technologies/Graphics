using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    internal unsafe struct EmbeddedArray32<T> : IDisposable where T : unmanaged
    {
        FixedList32Bytes<T> m_FixedArray;
        UnsafeList<T> m_List;
        int m_Length;
        bool m_Created;
        bool m_IsEmbedded;

        public int Length => m_Length;

        public EmbeddedArray32(NativeArray<T> array, Allocator allocator)
        {
            m_FixedArray = default;
            m_List = default;
            m_Length = array.Length;
            m_Created = true;

            if (array.Length <= m_FixedArray.Capacity)
            {
                m_FixedArray.AddRangeNoResize(array.GetUnsafeReadOnlyPtr(), array.Length);
                m_IsEmbedded = true;
            }
            else
            {
                m_List = new UnsafeList<T>(array.Length, allocator, NativeArrayOptions.UninitializedMemory);
                m_List.AddRangeNoResize(array.GetUnsafeReadOnlyPtr(), array.Length);
                m_IsEmbedded = false;
            }
        }

        public T this[int index]
        {
            get
            {
                Assert.IsTrue(m_Created && index < Length);

                if (m_IsEmbedded)
                    return m_FixedArray[index];
                else
                    return m_List[index];
            }
            set
            {
                Assert.IsTrue(m_Created && index < Length);

                if (m_IsEmbedded)
                    m_FixedArray[index] = value;
                else
                    m_List[index] = value;
            }
        }

        public unsafe void Dispose()
        {
            if (!m_Created)
                return;

            m_List.Dispose();
            m_Created = false;
        }
    }

    internal unsafe struct EmbeddedArray64<T> : IDisposable where T : unmanaged
    {
        FixedList64Bytes<T> m_FixedArray;
        UnsafeList<T> m_List;
        int m_Length;
        bool m_Created;
        bool m_IsEmbedded;

        public int Length => m_Length;

        public EmbeddedArray64(NativeArray<T> array, Allocator allocator)
        {
            m_FixedArray = default;
            m_List = default;
            m_Length = array.Length;
            m_Created = true;

            if (array.Length <= m_FixedArray.Capacity)
            {
                m_FixedArray.AddRangeNoResize(array.GetUnsafeReadOnlyPtr(), array.Length);
                m_IsEmbedded = true;
            }
            else
            {
                m_List = new UnsafeList<T>(array.Length, allocator, NativeArrayOptions.UninitializedMemory);
                m_List.AddRangeNoResize(array.GetUnsafeReadOnlyPtr(), array.Length);
                m_IsEmbedded = false;
            }
        }

        public T this[int index]
        {
            get
            {
                Assert.IsTrue(m_Created && index < Length);

                if (m_IsEmbedded)
                    return m_FixedArray[index];
                else
                    return m_List[index];
            }
            set
            {
                Assert.IsTrue(m_Created && index < Length);

                if (m_IsEmbedded)
                    m_FixedArray[index] = value;
                else
                    m_List[index] = value;
            }
        }

        public unsafe void Dispose()
        {
            if (!m_Created)
                return;

            m_List.Dispose();
            m_Created = false;
        }
    }
}
