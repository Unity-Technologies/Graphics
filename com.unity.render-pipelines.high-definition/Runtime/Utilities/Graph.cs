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
        where En : IRefEnumerator<T>
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
        where En : IMutEnumerator<T>
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
    ///
    /// If the array does not have any values, it does not allocate memory on the heap.
    ///
    /// This list grows its backend array when items are pushed.
    /// </summary>
    /// <typeparam name="T">Type of the array's item.</typeparam>
    static class ArrayList
    {
        internal unsafe struct Data
        {
            public int count;
            public int length;
            public void* storage;
        }
    }

    public static class ArrayList<T>
        where T : struct
    {
        public static ArrayList<T, A> New<A>(A allocator)
        where A : IMemoryAllocator
            => new ArrayList<T, A>(allocator);
    }

    public unsafe struct ArrayList<T, A>
        where T: struct
        where A: IMemoryAllocator
    {
        const float GrowFactor = 2.0f;

        ArrayList.Data* m_Data;
        A m_Allocator;

        ReadOnlySpan<T> span => new ReadOnlySpan<T>(m_Data->storage, m_Data->count);
        Span<T> spanMut => new Span<T>(m_Data->storage, m_Data->count);

        /// <summary>Number of item in the list.</summary>
        public int count => m_Data->count;

        /// <summary>Iterate over the values of the list by reference.</summary>
        public ArrayListRefEnumerator<T, A> values => new ArrayListRefEnumerator<T, A>(this);
        /// <summary>Iterate over the values of the list by mutable reference.</summary>
        public ArrayListMutEnumerator<T, A> valuesMut => new ArrayListMutEnumerator<T, A>(this);

        public ArrayList(A allocator)
        {
            m_Allocator = allocator;

            m_Data = allocator.Allocate<ArrayList.Data, A>();
            m_Data->storage = null;
            m_Data->count = 0;
            m_Data->length = 0;
        }

        /// <summary>
        /// Add a value to the list.
        ///
        /// If the backend don't have enough memory, the list will increase the allocated memory.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The index of the added value.</returns>
        public int Add(in T value)
        {
            var index = m_Data->count;
            m_Data->count++;

            unsafe { GrowIfRequiredFor(m_Data->count); }

            spanMut[index] = value;
            return index;
        }

        /// <summary>
        /// Get a reference to an item's value.
        ///
        /// Safety:
        /// If the index is out of bounds, the behaviour is undefined.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>A reference to the item.</returns>
        public unsafe ref readonly T GetUnsafe(int index) => ref span[index];

        /// <summary>Get a reference to an item's value.</summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>A reference to the item.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is out of bounds.</exception>
        public ref readonly T Get(int index)
        {
            if (index < 0 || index >= m_Data->count)
                throw new ArgumentOutOfRangeException(nameof(index));

            unsafe { return ref GetUnsafe(index); }
        }

        /// <summary>
        /// Get a mutable reference to an item's value.
        ///
        /// Safety:
        /// If the index is out of bounds, the behaviour is undefined.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>A reference to the item.</returns>
        public unsafe ref T GetMutUnsafe(int index) => ref spanMut[index];

        /// <summary>Get a mutable reference to an item's value.</summary>
        /// <param name="index">The index of the item.</param>
        /// <returns>A mutable reference to the item.</returns>
        /// <exception cref="ArgumentOutOfRangeException">When <paramref name="index"/> is out of bounds.</exception>
        public ref T GetMut(int index)
        {
            if (index < 0 || index >= m_Data->count)
                throw new ArgumentOutOfRangeException(nameof(index));

            unsafe { return ref GetMutUnsafe(index); }
        }

        /// <summary>
        /// Removes an item by copying the last entry at <paramref name="index"/> position.
        ///
        /// Safety:
        /// Behaviour is undefined when <paramref name="index"/> is out of bounds.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        public unsafe void RemoveSwapBackAtUnsafe(int index)
        {
            var spanMut = this.spanMut;
            spanMut[index] = spanMut[m_Data->count - 1];
            m_Data->count--;
        }

        /// <summary>/// Removes an item by copying the last entry at <paramref name="index"/> position.</summary>
        /// <param name="index">The index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException">When the <paramref name="index"/> is out of bounds.</exception>
        public void RemoveSwapBackAt(int index)
        {
            if (index < 0 || index >= m_Data->count)
                throw new ArgumentOutOfRangeException(nameof(index));

            unsafe { RemoveSwapBackAtUnsafe(index); }
        }

        /// <summary>
        /// Grow the capacity of the current array.
        /// </summary>
        /// <param name="size">the capacity to reach.</param>
        public void GrowCapacity(int size)
        {
            if (size > m_Data->length)
                GrowIfRequiredFor(size);
        }

        /// <summary>
        /// Grow the current backend to have at least <paramref name="size"/> item in memory.
        ///
        /// If the current backend is null, <paramref name="size"/> items will be allocated.
        ///
        /// Safety:
        /// Behaviour is undefined if <paramref name="size"/> is negative or 0.
        /// </summary>
        /// <param name="size"></param>
        unsafe void GrowIfRequiredFor(int size)
        {
            Assert.IsTrue(size > 0);

            if (m_Data->storage == null)
            {
                m_Data->storage = m_Allocator.Allocate((ulong)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<T>() * (ulong)size);
                m_Data->length = size;
            }
            else if (m_Data->length < size)
            {
                var nextSize = (float)m_Data->length;
                while (nextSize < size)
                    nextSize *= GrowFactor;

                var byteSize = (ulong)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.SizeOf<T>() * (ulong)nextSize;

                m_Allocator.Reallocate(m_Data->storage, byteSize);
            }
        }
    }

    /// <summary>
    /// EXPERIMENTAL: An enumerator by reference over a <see cref="ArrayList{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of the enumerated values.</typeparam>
    public struct ArrayListRefEnumerator<T, A> : IRefEnumerator<T>
        where T : struct
        where A : IMemoryAllocator
    {
        class Data
        {
            public ArrayList<T, A> source;
            public int index;
        }

        Data m_Data;

        public ArrayListRefEnumerator(ArrayList<T, A> source)
        {
            m_Data = new Data
            {
                source = source,
                index = -1
            };
        }

        public ref readonly T current
        {
            get
            {
                if (m_Data == null || m_Data.index < 0 || m_Data.index >= m_Data.source.count)
                    throw new InvalidOperationException("Enumerator was not initialized");

                return ref m_Data.source.Get(m_Data.index);
            }
        }

        public bool MoveNext()
        {
            if (m_Data == null)
                return false;

            var next = m_Data.index + 1;
            if (next < m_Data.source.count)
            {
                m_Data.index++;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            if (m_Data == null)
                return;

            m_Data.index = 0;
        }
    }

    /// <summary>
    /// EXPERIMENTAL: An enumerator by mutable reference over a <see cref="ArrayList{T}"/>.
    /// </summary>
    /// <typeparam name="T">Type of the enumerated values.</typeparam>
    public struct ArrayListMutEnumerator<T, A> : IMutEnumerator<T>
        where T : struct
        where A : IMemoryAllocator
    {
        class Data
        {
            public ArrayList<T, A> source;
            public int index;
        }

        Data m_Data;

        public ArrayListMutEnumerator(ArrayList<T, A> source)
        {
            m_Data = new Data
            {
                source = source,
                index = -1
            };
        }

        public ref T current
        {
            get
            {
                if (m_Data == null || m_Data.index < 0 || m_Data.index >= m_Data.source.count)
                    throw new InvalidOperationException("Enumerator was not initialized");

                return ref m_Data.source.GetMut(m_Data.index);
            }
        }

        public bool MoveNext()
        {
            if (m_Data == null)
                return false;

            var next = m_Data.index + 1;
            if (next < m_Data.source.count)
            {
                m_Data.index++;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            if (m_Data == null)
                return;

            m_Data.index = 0;
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
