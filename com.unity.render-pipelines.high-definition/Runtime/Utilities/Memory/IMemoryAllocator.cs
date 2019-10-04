namespace UnityEditor.Rendering.HighDefinition
{
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
}
