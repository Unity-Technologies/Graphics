using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct JaggedSpan<T> : IDisposable where T : unmanaged
    {
        UnsafeList<UnsafeList<T>> m_Sections;
        int m_TotalLength;

        public bool isCreated => m_Sections.IsCreated;
        public int sectionCount => m_Sections.IsCreated ? m_Sections.Length : 0;
        public int totalLength => m_TotalLength;
        public bool isEmpty => totalLength == 0;
        public NativeArray<UnsafeList<T>> sections => m_Sections.IsCreated ? m_Sections.AsNativeArray() : default;
        public NativeArray<UntypedUnsafeList> untypedSections => m_Sections.IsCreated ? m_Sections.AsUntypedUnsafeList().AsNativeArray() : default;

        public JaggedSpan(int initialCapacity, Allocator allocator)
        {
            m_Sections = new UnsafeList<UnsafeList<T>>(initialCapacity, allocator);
            m_TotalLength = 0;
        }

        public void Dispose()
        {
            m_Sections.Dispose();
        }

        public JobHandle Dispose(JobHandle jobHandle)
        {
            return m_Sections.Dispose(jobHandle);
        }

        // NativeArray lifetime must match or exceed the lifetime of the JaggedSpan
        public void Add(in NativeArray<T> section)
        {
            m_Sections.Add(SectionAsUnsafeList(section));
            m_TotalLength += section.Length;
        }

        public bool HasSameLayout<U>(in JaggedSpan<U> other)
            where U : unmanaged
        {
            if (sectionCount != other.sectionCount)
                return false;

            for (int i = 0; i < sectionCount; i++)
            {
                if (this[i].Length != other[i].Length)
                    return false;
            }

            return true;
        }

        public NativeArray<T> this[int index]
        {
            get => SectionAsArray(m_Sections[index]);
            set
            {
                ref UnsafeList<T> section = ref m_Sections.ElementAt(index);
                int deltaLength = value.Length - section.Length;

                section = SectionAsUnsafeList(value);
                m_TotalLength += deltaLength;
            }
        }

        private static NativeArray<T> SectionAsArray(in UnsafeList<T> section) => section.AsNativeArray();
        private static UnsafeList<T> SectionAsUnsafeList(in NativeArray<T> section) => section.AsUnsafeList();
    }

    internal struct JaggedBitSpan
    {
        UnsafeList<UnsafeBitArray> m_Sections;
        int m_TotalLength;

        public bool isCreated => m_Sections.IsCreated;
        public int sectionCount => m_Sections.IsCreated ? m_Sections.Length : 0;
        public int totalLength => m_TotalLength;
        public bool isEmpty => totalLength == 0;
        public NativeArray<UnsafeBitArray> sections => m_Sections.IsCreated ? m_Sections.AsNativeArray() : default;

        public JaggedBitSpan(int initialCapacity, Allocator allocator)
        {
            m_Sections = new UnsafeList<UnsafeBitArray>(initialCapacity, allocator);
            m_TotalLength = 0;
        }

        public void Dispose()
        {
            m_Sections.Dispose();
        }

        public JobHandle Dispose(JobHandle jobHandle)
        {
            return m_Sections.Dispose(jobHandle);
        }

        public void Add(in NativeBitArray section)
        {
            m_Sections.Add(section.AsUnsafeBitArray());
            m_TotalLength += section.Length;
        }

        public bool HasSameLayout<U>(in JaggedSpan<U> other)
            where U : unmanaged
        {
            if (sectionCount != other.sectionCount)
                return false;

            for (int i = 0; i < sectionCount; i++)
            {
                if (this[i].Length != other[i].Length)
                    return false;
            }

            return true;
        }

        public UnsafeBitArray this[int index]
        {
            get => m_Sections[index];
            set
            {
                ref UnsafeBitArray section = ref m_Sections.ElementAt(index);
                int deltaLength = value.Length - section.Length;

                section = value;
                m_TotalLength += deltaLength;
            }
        }
    }

    internal static class JaggedSpanExtensions
    {
        public static JaggedSpan<T> ToJaggedSpan<T>(this NativeArray<T> array, Allocator allocator) where T : unmanaged
        {
            var jaggedSpan = new JaggedSpan<T>(1, allocator);
            jaggedSpan.Add(array);
            return jaggedSpan;
        }

        public static bool HasDuplicates(this JaggedSpan<EntityId> jaggedSpan)
        {
            var uniqueItems = new NativeHashSet<EntityId>(jaggedSpan.totalLength, Allocator.Temp);

            for (int s = 0; s < jaggedSpan.sectionCount; s++)
            {
                NativeArray<EntityId> section = jaggedSpan[s];

                for (int i = 0; i < section.Length; i++)
                {
                    if (!uniqueItems.Add(section[i]))
                        return true;
                }
            }

            return false;
        }
    }
}
