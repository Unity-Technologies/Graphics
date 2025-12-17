namespace UnityEngine.Rendering
{
    // TODO make consistent with InstanceOcclusionCullerShaderVariables
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct OcclusionCullingCommonShaderVariables
    {
        [HLSLArray(OccluderContext.k_MaxOccluderMips, typeof(ShaderGenUInt4))]
        public fixed uint _OccluderMipBounds[OccluderContext.k_MaxOccluderMips * 4];

        [HLSLArray(OccluderContext.k_MaxSubviewsPerView, typeof(Matrix4x4))]
        public fixed float _ViewProjMatrix[OccluderContext.k_MaxSubviewsPerView * 16]; // from view-centered world space

        [HLSLArray(OccluderContext.k_MaxSubviewsPerView, typeof(Vector4))]
        public fixed float _ViewOriginWorldSpace[OccluderContext.k_MaxSubviewsPerView * 4];

        [HLSLArray(OccluderContext.k_MaxSubviewsPerView, typeof(Vector4))]
        public fixed float _FacingDirWorldSpace[OccluderContext.k_MaxSubviewsPerView * 4];

        [HLSLArray(OccluderContext.k_MaxSubviewsPerView, typeof(Vector4))]
        public fixed float _RadialDirWorldSpace[OccluderContext.k_MaxSubviewsPerView * 4];

        public Vector4 _DepthSizeInOccluderPixels;
        public Vector4 _OccluderDepthPyramidSize;

        public uint _OccluderMipLayoutSizeX;
        public uint _OccluderMipLayoutSizeY;
        public uint _OcclusionTestDebugFlags;
        public uint _OcclusionCullingCommonPad0;

        public int _OcclusionTestCount;
        public int _OccluderSubviewIndices; // packed 4 bits each
        public int _CullingSplitIndices; // packed 4 bits each
        public int _CullingSplitMask; // only used for early out

        internal OcclusionCullingCommonShaderVariables(
            in OccluderContext occluderCtx,
            in InstanceOcclusionTestSubviewSettings subviewSettings,
            bool occlusionOverlayCountVisible,
            bool overrideOcclusionTestToAlwaysPass)
        {
            for (int i = 0; i < occluderCtx.subviewCount; ++i)
            { 
                if (occluderCtx.IsSubviewValid(i))
                {
                    unsafe
                    {
                        for (int j = 0; j < 16; ++j)
                            _ViewProjMatrix[16 * i + j] = occluderCtx.subviewData[i].viewProjMatrix[j];

                        for (int j = 0; j < 4; ++j)
                        {
                            _ViewOriginWorldSpace[4 * i + j] = occluderCtx.subviewData[i].viewOriginWorldSpace[j];
                            _FacingDirWorldSpace[4 * i + j] = occluderCtx.subviewData[i].facingDirWorldSpace[j];
                            _RadialDirWorldSpace[4 * i + j] = occluderCtx.subviewData[i].radialDirWorldSpace[j];
                        }
                    }
                }
            }
            _OccluderMipLayoutSizeX = (uint)occluderCtx.occluderMipLayoutSize.x;
            _OccluderMipLayoutSizeY = (uint)occluderCtx.occluderMipLayoutSize.y;
            _OcclusionTestDebugFlags
                = (overrideOcclusionTestToAlwaysPass ? (uint)OcclusionTestDebugFlag.AlwaysPass : 0)
                | (occlusionOverlayCountVisible ? (uint)OcclusionTestDebugFlag.CountVisible : 0);
            _OcclusionCullingCommonPad0 = 0;

            _OcclusionTestCount = subviewSettings.testCount;
            _OccluderSubviewIndices = subviewSettings.occluderSubviewIndices;
            _CullingSplitIndices = subviewSettings.cullingSplitIndices;
            _CullingSplitMask = subviewSettings.cullingSplitMask;

            _DepthSizeInOccluderPixels = occluderCtx.depthBufferSizeInOccluderPixels;

            Vector2Int textureSize = occluderCtx.occluderDepthPyramidSize;
            _OccluderDepthPyramidSize = new Vector4(textureSize.x, textureSize.y, 1.0f / textureSize.x, 1.0f / textureSize.y);

            for (int i = 0; i < occluderCtx.occluderMipBounds.Length; ++i)
            {
                var mipBounds = occluderCtx.occluderMipBounds[i];
                unsafe
                {
                    _OccluderMipBounds[4*i + 0] = (uint)mipBounds.offset.x;
                    _OccluderMipBounds[4*i + 1] = (uint)mipBounds.offset.y;
                    _OccluderMipBounds[4*i + 2] = (uint)mipBounds.size.x;
                    _OccluderMipBounds[4*i + 3] = (uint)mipBounds.size.y;
                }
            }
        }
    }
}
