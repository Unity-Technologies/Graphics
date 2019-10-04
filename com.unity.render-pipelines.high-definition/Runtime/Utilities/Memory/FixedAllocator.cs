namespace UnityEditor.Rendering.HighDefinition
{
    using System;

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
