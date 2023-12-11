//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef OCCLUSIONCULLINGCOMMONSHADERVARIABLES_CS_HLSL
#define OCCLUSIONCULLINGCOMMONSHADERVARIABLES_CS_HLSL
// Generated from UnityEngine.Rendering.OcclusionCullingCommonShaderVariables
// PackingRules = Exact
CBUFFER_START(OcclusionCullingCommonShaderVariables)
    uint4 _OccluderMipBounds[8];
    float4x4 _ViewProjMatrix;
    float4 _ViewOriginWorldSpace;
    float4 _FacingDirWorldSpace;
    float4 _RadialDirWorldSpace;
    float4 _DepthSizeInOccluderPixels;
    float4 _OccluderTextureSize;
    float4 _DebugPyramidSize;
    int _RendererListSplitMask;
    int _DebugAlwaysPassOcclusionTest;
    int _DebugOverlayCountOccluded;
    int _Padding0;
CBUFFER_END


#endif
