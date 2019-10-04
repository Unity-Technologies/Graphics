using System;
using UnityEngine.Assertions;

namespace UnityEditor.Rendering.HighDefinition
{
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
}
