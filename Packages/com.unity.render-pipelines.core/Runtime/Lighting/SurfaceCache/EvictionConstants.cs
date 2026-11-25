namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal struct SurfaceCacheEvictionConstants
    {
        public uint _RingConfigOffset;
        public uint _FrameIdx;
        public uint _BouncePatchAllocation;
        public uint _EvictionPad0;
    }
}
