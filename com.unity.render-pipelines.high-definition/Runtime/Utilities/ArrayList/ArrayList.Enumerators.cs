namespace UnityEditor.Rendering.HighDefinition
{
    using System;

    public static class ArrayListEnumerator
    {
        internal unsafe struct Data
        {
            public ArrayList.Data* source;
            public int index;
        }
    }

    public static class ArrayListEnumerator<T>
            where T : struct
    {
        public static ArrayListRefEnumerator<T, A2> Ref<A, A2>(in ArrayList<T, A> arrayList, A2 allocator)
            where A : struct, IMemoryAllocator
            where A2 : struct, IMemoryAllocator
        => ArrayListRefEnumerator<T, A2>.New(arrayList, allocator);

        public static ArrayListMutEnumerator<T, A2> Mut<A, A2>(in ArrayList<T, A> arrayList, A2 allocator)
            where A : struct, IMemoryAllocator
            where A2 : struct, IMemoryAllocator
        => ArrayListMutEnumerator<T, A2>.New(arrayList, allocator);
    }

    /// <summary>EXPERIMENTAL: An enumerator by reference over a <see cref="ArrayList{T}"/>.</summary>
    /// <typeparam name="T">Type of the enumerated values.</typeparam>
    public struct ArrayListRefEnumerator<T, A> : IRefEnumerator<T>
        where A : struct, IMemoryAllocator
        where T : struct
    {
        unsafe ArrayListEnumerator.Data* m_Data;
        A m_Allocator;

        bool TryGetArrayList(out ArrayList arrayList)
        {
            unsafe
            {
                if (m_Data->source == null)
                {
                    arrayList = default;
                    return false;
                }
                arrayList = new ArrayList(m_Data->source);
                return true;
            }
        }

        internal static ArrayListRefEnumerator<T, A> New<A2>(in ArrayList<T, A2> arrayList, A allocator)
            where A2: struct, IMemoryAllocator
        {
            unsafe
            {
                var data = allocator.Allocate<ArrayListEnumerator.Data, A>();
                data->source = arrayList.ptr;
                data->index = -1;

                return new ArrayListRefEnumerator<T, A>
                {
                    m_Data = data,
                    m_Allocator = allocator
                };
            }
        }

        public ref readonly T current
        {
            get
            {
                unsafe
                {
                    if (!TryGetArrayList(out var arrayList)
                        || m_Data->index >= arrayList.count
                        || m_Data->index < 0)
                        throw new InvalidOperationException("Enumerator was not initialized");

                    return ref arrayList.GetUnchecked<T>(m_Data->index);
                }
            }
        }

        public bool MoveNext()
        {
            if (!TryGetArrayList(out var arrayList))
                return false;

            unsafe
            {
                var next = m_Data->index + 1;
                if (next < arrayList.count)
                {
                    m_Data->index++;
                    return true;
                }

                return false;
            }
        }

        public void Reset()
        {
            unsafe
            {
                if (m_Data == null)
                    return;

                m_Data->index = 0;
            }
        }
    }

    /// <summary>
    /// EXPERIMENTAL: An enumerator by mutable reference over a <see cref="ArrayList{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of the enumerated values.</typeparam>
    public struct ArrayListMutEnumerator<T, A> : IMutEnumerator<T>
       where A : struct, IMemoryAllocator
       where T : struct
    {
        unsafe ArrayListEnumerator.Data* m_Data;
        A m_Allocator;

        bool TryGetArrayList(out ArrayList arrayList)
        {
            unsafe
            {
                if (m_Data->source == null)
                {
                    arrayList = default;
                    return false;
                }
                arrayList = new ArrayList(m_Data->source);
                return true;
            }
        }

        internal static ArrayListMutEnumerator<T, A> New<A2>(in ArrayList<T, A2> arrayList, A allocator)
            where A2 : struct, IMemoryAllocator
        {
            unsafe
            {
                var data = allocator.Allocate<ArrayListEnumerator.Data, A>();
                data->source = arrayList.ptr;
                data->index = -1;

                return new ArrayListMutEnumerator<T, A>
                {
                    m_Data = data,
                    m_Allocator = allocator
                };
            }
        }

        public ref T current
        {
            get
            {
                unsafe
                {
                    if (!TryGetArrayList(out var arrayList)
                        || m_Data->index >= arrayList.count
                        || m_Data->index < 0)
                        throw new InvalidOperationException("Enumerator was not initialized");

                    return ref arrayList.GetMutUnchecked<T>(m_Data->index);
                }
            }
        }

        public bool MoveNext()
        {
            if (!TryGetArrayList(out var arrayList))
                return false;

            unsafe
            {
                var next = m_Data->index + 1;
                if (next < arrayList.count)
                {
                    m_Data->index++;
                    return true;
                }

                return false;
            }
        }

        public void Reset()
        {
            unsafe
            {
                if (m_Data == null)
                    return;

                m_Data->index = 0;
            }
        }
    }
}
