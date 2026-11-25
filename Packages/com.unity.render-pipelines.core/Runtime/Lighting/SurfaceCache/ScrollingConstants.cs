namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct SurfaceCacheScrollingConstants
    {
        // Signed offsets stored as uints, since constant buffer arrays must be declared with 4-component elements.
        [HLSLArray((int)SurfaceCache.CascadeMax, typeof(ShaderGenUInt4))]
        public fixed uint _OldVolumeCascadeOffsets[(int)SurfaceCache.CascadeMax * 4];

        public uint _VolumeSpatialResolution;
        public uint _VolumeCascadeCount;
        public uint _ScrollingPad0;
        public uint _ScrollingPad1;
    }
}
