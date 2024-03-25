namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct OccluderDepthPyramidConstants
    {
        [HLSLArray(OccluderContext.k_MaxSubviewsPerView, typeof(Matrix4x4))]
        public fixed float _InvViewProjMatrix[OccluderContext.k_MaxSubviewsPerView * 16];

        [HLSLArray(OccluderContext.k_MaxSilhouettePlanes, typeof(Vector4))]
        public fixed float _SilhouettePlanes[OccluderContext.k_MaxSilhouettePlanes * 4];

        [HLSLArray(OccluderContext.k_MaxSubviewsPerView, typeof(ShaderGenUInt4))]
        public fixed uint _SrcOffset[OccluderContext.k_MaxSubviewsPerView * 4];

        [HLSLArray(5, typeof(ShaderGenUInt4))]
        public fixed uint _MipOffsetAndSize[5 * 4];

        public uint _OccluderMipLayoutSizeX;
        public uint _OccluderMipLayoutSizeY;
        public uint _OccluderDepthPyramidPad0;
        public uint _OccluderDepthPyramidPad1;

        public uint _SrcSliceIndices; // packed 4 bits each
        public uint _DstSubviewIndices; // packed 4 bits each
        public uint _MipCount;
        public uint _SilhouettePlaneCount;
    }
}
