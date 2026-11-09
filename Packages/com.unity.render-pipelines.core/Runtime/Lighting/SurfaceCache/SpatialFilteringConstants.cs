namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal struct SurfaceCacheSpatialFilteringConstants
    {
        public Vector4 _VolumeTargetPos; // xyz: position, w: unused.

        public uint _FrameIdx;
        public uint _VolumeCascadeCount;
        public uint _VolumeSpatialResolution;
        public uint _SampleCount;

        public uint _RingConfigOffset;
        public float _Radius;
        public int _PatchWarping;
        public float _VolumeVoxelMinSize;
    }
}
