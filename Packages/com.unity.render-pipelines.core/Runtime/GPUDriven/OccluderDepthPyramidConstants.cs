namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct OccluderDepthPyramidConstants
    {
        public Matrix4x4 _InvViewProjMatrix;

        [HLSLArray((int)OcclusionCullingCommonConfig.MaxOccluderSilhouettePlanes, typeof(Vector4))]
        public fixed float _SilhouettePlanes[(int)OcclusionCullingCommonConfig.MaxOccluderSilhouettePlanes * 4];

        [HLSLArray(5, typeof(ShaderGenUInt4))]
        public fixed uint _MipOffsetAndSize[5 * 4];

        public uint _MipCount;
        public uint _SilhouettePlaneCount;
        public uint _OccluderDepthPyramidPad0;
        public uint _OccluderDepthPyramidPad1;
    }
}
