namespace UnityEditor.Rendering.HighDefinition
{
    public unsafe static class AllocatorUtilities
    {
        public static T* Allocate<T, A>(this A allocator)
            where T: unmanaged
            where A : IMemoryAllocator
            => (T*)allocator.Allocate((ulong)sizeof(T));
    }
}
