namespace UnityEngine.Rendering.Universal
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct SurfaceCacheGIScreenResolveLookupConstants
    {
        [HLSLArray(2, typeof(Matrix4x4))]
        public fixed float _ClipToWorldTransforms[2 * 16];

        public Vector4 _VolumeTargetPos; // xyz: position, w: unused.

        public uint _VolumeSpatialResolution;
        public uint _VolumeCascadeCount;
        public uint _SampleCount;
        public int _PatchWarping;

        public uint _DistanceFallback;
        public float _VolumeVoxelMinSize;
        public uint _ScreenResolveLookupPad0;
        public uint _ScreenResolveLookupPad1;
    }
}
