namespace UnityEditor.Rendering.HighDefinition
{
    public static class MemoryUtilities
    {
        public static uint Pad(uint byteSize, uint padding) => ((byteSize + padding - 1) / padding) * padding;
        public static ulong Pad(ulong byteSize, uint padding) => ((byteSize + padding - 1) / padding) * padding;
        public unsafe static void* Pad(void* pointer, uint padding) => (void*)((((ulong)pointer + padding - 1) / padding) * padding);
    }
}
