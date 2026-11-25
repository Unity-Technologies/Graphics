namespace UnityEngine.Rendering.Universal
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct SurfaceCacheGIDebugConstants
    {
        [HLSLArray(2, typeof(Matrix4x4))]
        public fixed float _ClipToWorldTransforms[2 * 16];

        public Vector4 _VolumeTargetPos; // xyz: position, w: unused.

        public uint _RingConfigOffset;
        public uint _VolumeSpatialResolution;
        public uint _VolumeCascadeCount;
        public uint _ViewMode; // Match the C# debug view-mode enum; 0 (None) is never dispatched.

        public uint _ShowSamplePosition;
        public uint _FrameIdx;
        public int _PatchWarping;
        public float _MaterialAtlasTexelSize; // The size of 1 texel in the albedo atlas.

        public uint _TileOffsetY;
        public uint _TileHeight;
        public uint _DummyBlackbox; // Always zero. Read through a constant so the compiler can't make assumptions about it.
        public float _VolumeVoxelMinSize;
    }
}
