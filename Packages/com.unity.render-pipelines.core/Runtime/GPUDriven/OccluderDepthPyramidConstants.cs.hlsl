//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef OCCLUDERDEPTHPYRAMIDCONSTANTS_CS_HLSL
#define OCCLUDERDEPTHPYRAMIDCONSTANTS_CS_HLSL
// Generated from UnityEngine.Rendering.OccluderDepthPyramidConstants
// PackingRules = Exact
CBUFFER_START(OccluderDepthPyramidConstants)
    float4x4 _InvViewProjMatrix[6];
    float4 _SilhouettePlanes[6];
    uint4 _SrcOffset[6];
    uint4 _MipOffsetAndSize[5];
    uint _OccluderMipLayoutSizeX;
    uint _OccluderMipLayoutSizeY;
    uint _OccluderDepthPyramidPad0;
    uint _OccluderDepthPyramidPad1;
    uint _SrcSliceIndices;
    uint _DstSubviewIndices;
    uint _MipCount;
    uint _SilhouettePlaneCount;
CBUFFER_END


#endif
