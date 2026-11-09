namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal struct SurfaceCacheDefragConstants
    {
        public uint _RingConfigReadOffset;
        public uint _RingConfigWriteOffset;
        public uint _PatchOffset;
        public uint _DefragPad0;
    }
}
