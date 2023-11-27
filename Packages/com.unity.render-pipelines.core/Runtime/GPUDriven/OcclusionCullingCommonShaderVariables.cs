namespace UnityEngine.Rendering
{
    // TODO make consistent with InstanceOcclusionCullerShaderVariables
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct OcclusionCullingCommonShaderVariables
    {
        [HLSLArray(OccluderContext.k_MaxOccluderMips, typeof(ShaderGenUInt4))]
        public fixed uint _OccluderMipBounds[OccluderContext.k_MaxOccluderMips * 4];

        public Matrix4x4 _ViewProjMatrix;   // from view-centered world space

        public Vector4 _ViewOriginWorldSpace;
        public Vector4 _FacingDirWorldSpace;
        public Vector4 _RadialDirWorldSpace;
        public Vector4 _DepthSizeInOccluderPixels;
        public Vector4 _OccluderTextureSize;
        public Vector4 _DebugPyramidSize;

        public int _RendererListSplitMask;
        public int _DebugAlwaysPassOcclusionTest;
        public int _DebugOverlayCountOccluded;
        public int _Padding0;

        internal OcclusionCullingCommonShaderVariables(
            in OccluderContext occluderCtx,
            int cullingSplitIndex,
            bool occlusionOverlayCountVisible,
            bool overrideOcclusionTestToAlwaysPass)
        {
            _ViewProjMatrix = occluderCtx.cameraData.viewProjMatrix;
            _ViewOriginWorldSpace = occluderCtx.cameraData.viewOriginWorldSpace;
            _DebugAlwaysPassOcclusionTest = overrideOcclusionTestToAlwaysPass ? 1 : 0;
            _FacingDirWorldSpace = occluderCtx.cameraData.facingDirWorldSpace;
            _RadialDirWorldSpace = occluderCtx.cameraData.radialDirWorldSpace;
            _DebugOverlayCountOccluded = occlusionOverlayCountVisible ? 0 : 1;
            _Padding0 = 0;
            _DebugPyramidSize = new Vector4(occluderCtx.debugTextureSize.x, occluderCtx.debugTextureSize.y, 0.0f, 0.0f);
            _RendererListSplitMask = 1 << cullingSplitIndex;
            _DepthSizeInOccluderPixels = occluderCtx.depthBufferSizeInOccluderPixels;
            Vector2Int textureSize = occluderCtx.occluderTextureSize;
            _OccluderTextureSize = new Vector4(textureSize.x, textureSize.y, 1.0f / textureSize.x, 1.0f / textureSize.y);
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
