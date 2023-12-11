//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef OCCLUDERDEPTHPYRAMIDCONSTANTS_CS_HLSL
#define OCCLUDERDEPTHPYRAMIDCONSTANTS_CS_HLSL
// Generated from UnityEngine.Rendering.OccluderDepthPyramidConstants
// PackingRules = Exact
CBUFFER_START(OccluderDepthPyramidConstants)
    float4x4 _InvViewProjMatrix;
    float4 _SilhouettePlanes[6];
    uint4 _MipOffsetAndSize[5];
    uint _MipCount;
    uint _SilhouettePlaneCount;
    uint _OccluderDepthPyramidPad0;
    uint _OccluderDepthPyramidPad1;
CBUFFER_END


#endif
