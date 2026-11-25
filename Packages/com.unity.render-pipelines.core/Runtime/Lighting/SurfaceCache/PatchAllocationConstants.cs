namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal struct SurfaceCachePatchAllocationConstants
    {
        public Vector4 _VolumeTargetPos; // xyz: position, w: unused.

        public uint _RingConfigOffset;
        public uint _VolumeSpatialResolution;
        public uint _VolumeCascadeCount;
        public int _PatchWarping;

        public float _VolumeVoxelMinSize;
        public uint _FrameIndex;
        public uint _PatchAllocationPad0;
        public uint _PatchAllocationPad1;
    }
}
