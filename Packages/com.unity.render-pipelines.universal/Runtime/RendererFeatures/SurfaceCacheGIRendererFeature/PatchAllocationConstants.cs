using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct SurfaceCacheGIPatchAllocationConstants
    {
        [HLSLArray(2, typeof(Matrix4x4))]
        public fixed float _CurrentClipToWorldTransforms[2 * 16];
        [HLSLArray(2, typeof(Matrix4x4))]
        public fixed float _PreviousClipToWorldTransforms[2 * 16];

        public Vector4 _VolumeTargetPos; // xyz: position, w: unused.

        public uint _FrameIdx;
        public uint _StereoEyeIndex;
        public uint _VolumeSpatialResolution;
        public uint _VolumeCascadeCount;

        public uint2 _LowResScreenSize;
        public uint2 _FullResPixelOffset;

        public uint _RingConfigOffset;
        public int _PatchWarping;
        public float _VolumeVoxelMinSize;
        public uint _PatchAllocationPad0;
    }
}
