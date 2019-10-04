namespace UnityEditor.Rendering.HighDefinition
{
    using System;
    using System.Collections;
    using UnityEngine.Assertions;

    //
    // Enumerator Utilities
    //

    /// <summary>
    /// EXPERIMENTAL: Iterate by reference over a collection.
    ///
    /// similar to <see cref="System.Collections.Generic.IEnumerator{T}"/> but with a reference to the strong type.
    /// </summary>
    /// <typeparam name="T">The type of the iterator.</typeparam>
    public interface IRefEnumerator<T>
    {
        /// <summary>A reference to the current value.</summary>
        ref readonly T current { get; }

        /// <summary>Move to the next value.</summary>
        /// <returns><c>true</c> when a value was found, <c>false</c> when the enumerator has completed.</returns>
        bool MoveNext();

        /// <summary>Reset the enumerator to its initial state.</summary>
        void Reset();
    }

    /// <summary>
    /// EXPERIMENTAL: Iterate by mutable reference over a collection.
    ///
    /// similar to <see cref="System.Collections.Generic.IEnumerator{T}"/> but with a mutable reference to the strong type.
    /// </summary>
    /// <typeparam name="T">The type of the iterator.</typeparam>
    public interface IMutEnumerator<T> 
    {
        /// <summary>A mutable reference to the current value.</summary>
        ref T current { get; }

        /// <summary>Move to the next value.</summary>
        /// <returns><c>true</c> when a value was found, <c>false</c> when the enumerator has completed.</returns>
        bool MoveNext();

        /// <summary>Reset the enumerator to its initial state.</summary>
        void Reset();
    }

    /// <summary>
    /// EXPERIMENTAL: Interface similar to <see cref="System.Func{T, TResult}"/> but consuming <c>in</c> arguments.
    ///
    /// Implement this interface on a struct to have inlined callbacks by the compiler.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument.</typeparam>
    /// <typeparam name="R">The type of the return.</typeparam>
    public interface IInFunc<T1, R>
    {
        /// <summary>Execute the function.</summary>
        R Execute(in T1 t1);
    }

    /// <summary>EXPERIMENTAL: An enumerator performing a select function.</summary>
    /// <typeparam name="I">Type of the input value.</typeparam>
    /// <typeparam name="O">Type of the output value.</typeparam>
    /// <typeparam name="En">Type of the enumerator to consume by reference.</typeparam>
    /// <typeparam name="S">Type of the select function.</typeparam>
    public struct SelectRefEnumerator<I, O, En, S> : System.Collections.Generic.IEnumerator<O>
        where En : struct, IRefEnumerator<I>
        where S: struct, IInFunc<I, O>
    {
        En m_Enumerator;
        S m_Select;

        public SelectRefEnumerator(En enumerator, S select)
        {
            m_Enumerator = enumerator;
            m_Select = select;
        }

        public O Current => m_Select.Execute(m_Enumerator.current);

        object IEnumerator.Current => m_Select.Execute(m_Enumerator.current);

        public void Dispose() {}

        public bool MoveNext() => m_Enumerator.MoveNext();

        public void Reset() => m_Enumerator.Reset();
    }

    /// <summary>
    /// EXPERIMENTAL: An enumerator by reference thats skip values based on a where clause.
    /// </summary>
    /// <typeparam name="T">Type of the enumerated values.</typeparam>
    /// <typeparam name="En">Type of the enumerator.</typeparam>
    /// <typeparam name="Wh">Type of the where clause.</typeparam>
    public struct WhereRefIterator<T, En, Wh> : IRefEnumerator<T>
        where Wh : struct, IInFunc<T, bool>
        where En : struct, IRefEnumerator<T>
    {
        class Data
        {
            public En enumerator;
            public Wh whereClause;
        }

        Data m_Data;

        public WhereRefIterator(En enumerator, Wh whereClause)
        {
            m_Data = new Data
            {
                enumerator = enumerator,
                whereClause = whereClause
            };
        }

        public ref readonly T current
        {
            get
            {
                if (m_Data == null)
                    throw new InvalidOperationException("Enumerator not initialized.");

                return ref m_Data.enumerator.current;
            }
        }

        public bool MoveNext()
        {
            if (m_Data == null)
                return false;

            while (m_Data.enumerator.MoveNext())
            {
                ref readonly var value = ref m_Data.enumerator.current;
                if (m_Data.whereClause.Execute(value))
                    return true;
            }

            return false;
        }

        public void Reset()
        {
            if (m_Data == null)
                throw new InvalidOperationException("Enumerator not initialized.");

            m_Data.enumerator.Reset();
        }
    }

    /// <summary>
    /// EXPERIMENTAL: An enumerator by mutable reference thats skip values based on a where clause.
    /// </summary>
    /// <typeparam name="T">Type of the enumerated values.</typeparam>
    /// <typeparam name="En">Type of the enumerator.</typeparam>
    /// <typeparam name="Wh">Type of the where clause.</typeparam>
    public struct WhereMutIterator<T, En, Wh> : IMutEnumerator<T>
        where Wh : struct, IInFunc<T, bool>
        where En : struct, IMutEnumerator<T>
    {
        class Data
        {
            public En enumerator;
            public Wh whereClause;
        }

        Data m_Data;

        public WhereMutIterator(En enumerator, Wh whereClause)
        {
            m_Data = new Data
            {
                enumerator = enumerator,
                whereClause = whereClause
            };
        }

        public ref T current
        {
            get
            {
                if (m_Data == null)
                    throw new InvalidOperationException("Enumerator not initialized.");

                return ref m_Data.enumerator.current;
            }
        }

        public bool MoveNext()
        {
            if (m_Data == null)
                return false;

            while (m_Data.enumerator.MoveNext())
            {
                ref var value = ref m_Data.enumerator.current;
                if (m_Data.whereClause.Execute(value))
                    return true;
            }

            return false;
        }

        public void Reset()
        {
            if (m_Data == null)
                throw new InvalidOperationException("Enumerator not initialized.");

            m_Data.enumerator.Reset();
        }
    }

    //
    // ArrayList Utilities
    //

    /// <summary>
    /// EXPERIMENTAL: A list based on an array with reference access.
    /// </summary>
    /// <remarks>
    /// If the array does not have any values, it does not allocate memory on the heap.
    ///
    /// This list grows its backend array when items are pushed.
    /// </remarks>
    /// <typeparam name="T">Type of the array's item.</typeparam>
    public unsafe struct ArrayList
    {
        internal unsafe struct Data
        {
            public int count;
            public int length;
            public void* storage;
        }

        const float GrowFactor = 2.0f;

        Data* m_Data;

        internal Data* ptr => m_Data;

        /// <summary>Get an immutable span over the items.</summary>
        /// <remarks>
        /// Safety:
        /// * <typeparamref name="T"/> must be the type of the items in the collection.</remarks>
        /// <typeparam name="T">The type of the items in the collections.</typeparam>
        /// <returns>The immutable span over the items.</returns>
        ReadOnlySpan<T> GetSpan<T>() where T: struct => new ReadOnlySpan<T>(m_Data->storage, m_Data->count);
        /// <summary>Get a mutable span over the items.</summary>
        /// <remarks>
        /// Safety:
        /// * <typeparamref name="T"/> must be the type of the items in the collection.</remarks>
        /// <typeparam name="T">The type of the items in the collections.</typeparam>
        /// <returns>The mutable span over the items.</returns>
        Span<T> GetSpanMut<T>() where T: struct => new Span<T>(m_Data->storage, m_Data->count);

        /// <summary>Number of item in the list.</summary>
        public int count => m_Data->count;

        public static ArrayList New<A>(A allocator)
            where A: struct, IMemoryAllocator
        {
            var list = new ArrayList();
            list.m_Data = allocator.Allocate<Data, A>();
            list.m_Data->storage = null;
            list.m_Data->count = 0;
            list.m_Data->length = 0;

            return list;
        }

        internal ArrayList(Data* data) => m_Data = data;

        /// <summary>Add a value to the list.</summary>
        /// <remarks>
        /// If the backend don't have enough memory, the list will increase the allocated memory.
        ///
        /// Safety:
        /// * <typeparamref name="T"/> must be the exact type of the collection's item.
        /// * <paramref name="allocator"/> must be the allocator of this collection.
        /// </remarks>
        /// <param name="value">The value to add.</param>
        /// <param name="allocator">The allocator for this collection.</param>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <typeparam name="A">The type of the allocator.</typeparam>
        /// <returns>The index of the added value.</returns>
        public unsafe int Add<T, A>(in T value, A allocator)
            where T: struct
            where A: struct, IMemoryAllocator
        {
            var index = m_Data->count;
            m_Data->count++;

            GrowIfRequiredFor<T, A>(m_Data->count, allocator);

            GetSpanMut<T>()[index] = value;
            return index;
        }

        /// <summary>Get a reference to an item's value.</summary>
        /// <remarks>
        /// Safety:
        /// * If the index is out of bounds, the behaviour is undefined.
        /// * <typeparamref name="T"/> must be the exact type of the collection's item.
        /// </remarks>
        /// <param name="index">The index of the item.</param>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <returns>A reference to the item.</returns>
        public unsafe ref readonly T GetUnchecked<T>(int index) where T: struct => ref GetSpan<T>()[index];

        /// <summary>Get a reference to an item's value.</summary>
        /// <remarks>
        /// Safety:
        /// * <typeparamref name="T"/> must be the exact type of the collection's item.
        /// </remarks>
        /// <param name="index">The index of the item.</param>
        /// <returns>A reference to the item.</returns>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is out of bounds.</exception>
        public unsafe ref readonly T Get<T>(int index)
            where T: struct
        {
            if (index < 0 || index >= m_Data->count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return ref GetUnchecked<T>(index);
        }

        /// <summary>Get a mutable reference to an item's value.</summary>
        /// <remarks>
        /// Safety:
        /// * If the index is out of bounds, the behaviour is undefined.
        /// * <typeparamref name="T"/> must be the exact type of the collection's item.
        /// </remarks>
        /// <param name="index">The index of the item.</param>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <returns>A reference to the item.</returns>
        public unsafe ref T GetMutUnchecked<T>(int index) where T: struct => ref GetSpanMut<T>()[index];

        /// <summary>Get a mutable reference to an item's value.</summary>
        /// <remarks>
        /// Safety:
        /// * <typeparamref name="T"/> must be the exact type of the collection's item.
        /// </remarks>
        /// <param name="index">The index of the item.</param>
        /// <returns>A mutable reference to the item.</returns>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is out of bounds.</exception>
        public unsafe ref T GetMut<T>(int index)
            where T: struct
        {
            if (index < 0 || index >= m_Data->count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return ref GetMutUnchecked<T>(index);
        }

        /// <summary>Removes an item by copying the last entry at <paramref name="index"/> position.</summary>
        /// <remarks>
        /// Safety:
        /// * Behaviour is undefined when <paramref name="index"/> is out of bounds.
        /// * <typeparamref name="T"/> must be the exact type of the collection's item.
        /// </remarks>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <param name="index">The index of the item to remove.</param>
        public unsafe void RemoveSwapBackAtUnchecked<T>(int index)
            where T: struct
        {
            var spanMut = this.GetSpanMut<T>();
            spanMut[index] = spanMut[m_Data->count - 1];
            m_Data->count--;
        }

        /// <summary>Removes an item by copying the last entry at <paramref name="index"/> position.</summary>
        /// <remarks>
        /// Safety:
        /// * <typeparamref name="T"/> must be the exact type of the collection's item.
        /// </remarks>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <param name="index">The index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">When the <paramref name="index"/> is out of bounds.</exception>
        public unsafe void RemoveSwapBackAt<T>(int index)
            where T: struct
        {
            if (index < 0 || index >= m_Data->count)
                throw new ArgumentOutOfRangeException(nameof(index));

            RemoveSwapBackAtUnchecked<T>(index);
        }

        /// <summary>Grow the capacity of the current array.</summary>
        /// <remarks>
        /// Safety:
        /// * <paramref name="allocator"/> must be the allocator used for this collection.
        /// * <typeparamref name="T"/> must be the exact type of the collection's item.
        /// </remarks>
        /// <param name="size">the capacity to reach.</param>
        /// <param name="allocator">The allocator used for this collection.</param>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <typeparam name="A">The type of the allocator.</typeparam>
        public unsafe void GrowCapacity<T, A>(int size, A allocator)
            where T : struct
            where A: struct, IMemoryAllocator
        {
            if (size > m_Data->length)
                GrowIfRequiredFor<T, A>(size, allocator);
        }

        /// <summary>Dispose the allocated memory.</summary>
        /// <remarks>
        /// Safety:
        /// * <paramref name="allocator"/> must be the allocator used for this collection.
        /// </remarks>
        /// <typeparam name="A">The type of the allocator.</typeparam>
        /// <param name="allocator">The allocator used for this collection.</param>
        public unsafe void Dispose<A>(A allocator)
            where A: struct, IMemoryAllocator
        {
            var ptr = m_Data->storage;
            m_Data->storage = null;
            m_Data->length = 0;
            m_Data->count = 0;
            allocator.Deallocate(ptr);
            allocator.Deallocate(m_Data);
            m_Data = null;
        }

        /// <summary>Grow the current backend to have at least <paramref name="size"/> item in memory.</summary>
        /// <remarks>
        /// If the current backend is null, <paramref name="size"/> items will be allocated.
        ///
        /// Safety:
        /// * Behaviour is undefined if <paramref name="size"/> is negative or 0.
        /// * <paramref name="allocator"/> must be the allocator used for this collection.
        /// * <typeparamref name="T"/> must be the exact type of the collection's item.
        /// </remarks>
        /// <typeparam name="A">The type of the memory allocator.</typeparam>
        /// <typeparam name="T">The type of the items in the collection.</typeparam>
        /// <param name="allocator">The allocator used for this collection.</param>
        /// <param name="size">The requested size.</param>
        unsafe void GrowIfRequiredFor<T, A>(int size, A allocator)
            where T: struct
            where A: struct, IMemoryAllocator
        {
            Assert.IsTrue(size > 0);

            if (m_Data->storage == null)
            {
                m_Data->storage = allocator.Allocate((ulong)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<T>() * (ulong)size);
                m_Data->length = size;
            }
            else if (m_Data->length < size)
            {
                var nextSize = (float)m_Data->length;
                while (nextSize < size)
                    nextSize *= GrowFactor;

                var byteSize = (ulong)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<T>() * (ulong)nextSize;

                allocator.Reallocate(m_Data->storage, byteSize);
            }
        }
    }

    public static class ArrayList<T>
        where T : struct
    {
        public static ArrayList<T, A> New<A>(A allocator)
        where A : struct, IMemoryAllocator
            => new ArrayList<T, A>(allocator);
    }

    public unsafe struct ArrayList<T, A> : IDisposable
        where T: struct
        where A: struct, IMemoryAllocator
    {
        ArrayList m_ArrayList;
        A m_Allocator;

        internal unsafe ArrayList.Data* ptr => m_ArrayList.ptr;

        /// <summary>Number of item in the list.</summary>
        public int count => m_ArrayList.count;

        /// <summary>Iterate over the values of the list by reference.</summary>
        public ArrayListRefEnumerator<T, A> values => ArrayListEnumerator<T>.Ref(this, m_Allocator);
        /// <summary>Iterate over the values of the list by mutable reference.</summary>
        public ArrayListMutEnumerator<T, A> valuesMut => ArrayListEnumerator<T>.Mut(this, m_Allocator);

        public ArrayList(A allocator)
        {
            m_Allocator = allocator;

            m_ArrayList = ArrayList.New(allocator);
        }

        /// <summary>
        /// Add a value to the list.
        ///
        /// If the backend don't have enough memory, the list will increase the allocated memory.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The index of the added value.</returns>
        public int Add(in T value) => m_ArrayList.Add(value, m_Allocator);

        /// <summary>
        /// Get a reference to an item's value.
        ///
        /// Safety:
        /// If the index is out of bounds, the behaviour is undefined.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>A reference to the item.</returns>
        public unsafe ref readonly T GetUnsafe(int index) => ref m_ArrayList.GetUnchecked<T>(index);

        /// <summary>Get a reference to an item's value.</summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>A reference to the item.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is out of bounds.</exception>
        public ref readonly T Get(int index) => ref m_ArrayList.Get<T>(index);

        /// <summary>
        /// Get a mutable reference to an item's value.
        ///
        /// Safety:
        /// If the index is out of bounds, the behaviour is undefined.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>A reference to the item.</returns>
        public unsafe ref T GetMutUnsafe(int index) => ref m_ArrayList.GetMutUnchecked<T>(index);

        /// <summary>Get a mutable reference to an item's value.</summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>A mutable reference to the item.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is out of bounds.</exception>
        public ref T GetMut(int index) => ref m_ArrayList.GetMut<T>(index);

        /// <summary>
        /// Removes an item by copying the last entry at <paramref name="index"/> position.
        ///
        /// Safety:
        /// Behaviour is undefined when <paramref name="index"/> is out of bounds.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        public unsafe void RemoveSwapBackAtUnsafe(int index) => m_ArrayList.RemoveSwapBackAtUnchecked<T>(index);

        /// <summary>/// Removes an item by copying the last entry at <paramref name="index"/> position.</summary>
        /// <param name="index">The index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">When the <paramref name="index"/> is out of bounds.</exception>
        public void RemoveSwapBackAt(int index) => m_ArrayList.RemoveSwapBackAt<T>(index);

        /// <summary>
        /// Grow the capacity of the current array.
        /// </summary>
        /// <param name="size">the capacity to reach.</param>
        public void GrowCapacity(int size) => m_ArrayList.GrowCapacity<T, A>(size, m_Allocator);

        /// <summary>Dispose the collection.</summary>
        public void Dispose()
        {
            m_ArrayList.Dispose(m_Allocator);
        }
    }

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

    /// <summary>Memory allocator interface.</summary>
    public unsafe interface IMemoryAllocator
    {
        /// <summary>
        /// Allocate <paramref name="byteSize"/> bytes of memory.
        /// </summary>
        /// <param name="byteSize">The number of bytes to allocate.</param>
        /// <returns>The address of the allocated memory.</returns>
        void* Allocate(ulong byteSize);

        /// <summary>
        /// Rellocate the memory from <paramref name="pointer"/> to have the size <paramref name="byteSize"/> in bytes.
        ///
        /// If the memory address can be kept, then <paramref name="pointer"/> is returned.
        /// Otherwise, the data from <paramref name="pointer"/>
        /// will be copied up to <paramref name="byteSize"/> bytes.
        ///
        /// <paramref name="pointer"/> must have been allocated by this allocator before.
        /// </summary>
        /// <param name="pointer">The address that was previously allocated.</param>
        /// <param name="byteSize">The number of bytes to allocate.</param>
        /// <returns></returns>
        void* Reallocate(void* pointer, ulong byteSize);

        /// <summary>
        /// Deallocate a memory allocated by this allocator.
        /// </summary>
        /// <param name="pointer">The address that was allocated.</param>
        void Deallocate(void* pointer);
    }

    public unsafe static class AllocatorUtilities
    {
        public static T* Allocate<T, A>(this A allocator)
            where T: unmanaged
            where A : IMemoryAllocator
            => (T*)allocator.Allocate((ulong)sizeof(T));
    }

    public static class MemoryUtilities
    {
        public static uint Pad(uint byteSize, uint padding) => ((byteSize + padding - 1) / padding) * padding;
        public static ulong Pad(ulong byteSize, uint padding) => ((byteSize + padding - 1) / padding) * padding;
        public unsafe static void* Pad(void* pointer, uint padding) => (void*)((((ulong)pointer + padding - 1) / padding) * padding);
    }

    /// <summary>
    /// Allocate memory in a provided buffer.
    /// This allocator is not thread safe.
    /// </summary>
    public unsafe struct FixedAllocator : IMemoryAllocator
    {
        /// <summary>All allocation will be padded with this value.</summary>
        const uint k_MemoryPadding = 4;
        /// <summary>Padded size of the <see cref="Data"/> payload.</summary>
        static readonly uint k_PaddedDataSize = MemoryUtilities.Pad((uint)sizeof(Data), k_MemoryPadding);
        /// <summary>Padded size of the <see cref="AllocationHeader"/> payload.</summary>
        static readonly uint k_PaddedHeaderSize = MemoryUtilities.Pad((uint)sizeof(AllocationHeader), k_MemoryPadding);

        /// <summary>Space required by the allocator for its internal data.</summary>
        public static readonly uint OverheadSize = k_PaddedDataSize;
        /// <summary>Space required by the allocator per allocation.</summary>
        public static readonly uint OverheadSizePerAllocation = k_PaddedHeaderSize;

        unsafe struct Data
        {
            /// <summary>Initial provided buffer pointer.</summary>
            public void* buffer;
            /// <summary>Initial provided buffer byte size.</summary>
            public ulong byteSize;
            /// <summary>Next address to use to allocate memory.</summary>
            public void* tail;
            /// <summary>The thread used to create this allocator.</summary>
            public int threadId;
        }

        unsafe struct AllocationHeader
        {
            public ulong byteSize;
        }

        Data* m_Data;

        /// <summary>Number of bytes left in this allocator.</summary>
        ulong bytesLeft {
            get
            {
                var bufferEnd = (byte*)m_Data->buffer + m_Data->byteSize;
                var bufferStart = (byte*)m_Data->tail;
                if (bufferEnd < bufferStart)
                    throw new InvalidOperationException($"buffer overflow, this must never happen.");

                return (ulong)(bufferEnd - bufferStart);
            }
        }

        public FixedAllocator(void* buffer, ulong byteSize)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            m_Data = null;

            // Take memory for the allocator data
            if (byteSize < k_PaddedDataSize)
                throw new ArgumentException($"{nameof(byteSize)} must be greater than {k_PaddedDataSize} to hold allocator's data.");

            m_Data = (Data*)buffer;
            // Initialize the data memory
            m_Data->buffer = buffer;
            m_Data->byteSize = byteSize;
            // Initialize tail
            m_Data->tail = (void*)((byte*)buffer + k_PaddedDataSize);
            // Get the allowed thread for this allocator
            m_Data->threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        public unsafe void* Allocate(ulong byteSize)
        {
            AssertIsCurrentThreadValid();

            // We need space to store the AllocationHeader before the allocated space
            // And we also need to pad it to k_MemoryPadding.
            var requiredAllocationByteSize = MemoryUtilities.Pad(byteSize, k_MemoryPadding) + k_PaddedHeaderSize;

            // Check for out of memory
            if (requiredAllocationByteSize > (ulong)bytesLeft)
                throw new OutOfMemoryException($"Out of memory, required {requiredAllocationByteSize}," +
                                    $" but only {bytesLeft} bytes are remaining.");

            // We can now allocate
            // We need to compute the address of both the allocated memory and the allocation header
            // Pad the tail
            var headerPtr = (AllocationHeader*)MemoryUtilities.Pad(m_Data->tail, k_MemoryPadding);
            headerPtr->byteSize = byteSize;
            var bufferPtr = (void*)((byte*)headerPtr + k_PaddedHeaderSize);

            // Move the tail of the allocator
            m_Data->tail = (void*)((byte*)headerPtr + requiredAllocationByteSize);

            return bufferPtr;
        }

        public unsafe void Deallocate(void* pointer)
        {
            AssertIsCurrentThreadValid();
            AssertIsAddressInAllocationRange(pointer);
            AssertAddressIsPadded(pointer);
        }

        public unsafe void* Reallocate(void* pointer, ulong byteSize)
        {
            AssertIsCurrentThreadValid();
            AssertIsAddressInAllocationRange(pointer);
            AssertAddressIsPadded(pointer);

            var header = (AllocationHeader*)((byte*)pointer - k_PaddedHeaderSize);
            var allocatedSize = header->byteSize;

            if (allocatedSize >= byteSize)
            {
                // We only shrink the memory
                // So we can return the same pointer
                header->byteSize = byteSize;
                return pointer;
            }

            // We need to reallocate a bigger chunk of memory
            var newPtr = Allocate(byteSize);
            // Copy existing data
            Unity.Collections.LowLevel.Unsafe.UnsafeUtility.MemCpy(newPtr, pointer, (long)allocatedSize);
            Deallocate(pointer);

            return newPtr;
        }

        /// <summary>Check that <paramref name="address"/> is inside the allocation range.</summary>
        /// <param name="address">The address to check.</param>
        /// <returns><c>true</c> when the address is inside the allocation range.</returns>
        bool IsAddressInAllocationRange(void* address)
        {
            var startAllocationRange = (byte*)m_Data->buffer + k_PaddedDataSize;
            var endAllocationRange = (byte*)m_Data->buffer + m_Data->byteSize - k_PaddedHeaderSize;
            var byteAddress = (byte*)address;
            return startAllocationRange <= address && address <= endAllocationRange;
        }

        void AssertIsAddressInAllocationRange(void* address)
        {
            if (!IsAddressInAllocationRange(address))
                throw new ArgumentException($"{nameof(address)}: {(ulong)address:X} is outside the allocated range" +
                    $", it was not allocated by this allocator.");
        }

        bool IsCurrentThreadValid() => m_Data->threadId == System.Threading.Thread.CurrentThread.ManagedThreadId;

        void AssertIsCurrentThreadValid()
        {
            if (!IsCurrentThreadValid())
                throw new InvalidOperationException($"Allocator is used in an unauthorized thread.");
        }

        void AssertAddressIsPadded(void* pointer)
        {
            if (MemoryUtilities.Pad(pointer, k_MemoryPadding) != pointer)
                throw new ArgumentException($"{nameof(pointer)} is not padded properly, it was not allocated by this allocator.");
        }
    }
}
