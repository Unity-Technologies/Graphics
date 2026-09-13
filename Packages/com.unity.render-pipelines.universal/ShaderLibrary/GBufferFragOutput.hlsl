#ifndef UNIVERSAL_GBUFFER_FRAG_OUTPUT_INCLUDED
#define UNIVERSAL_GBUFFER_FRAG_OUTPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferCommon.hlsl"

#define DECL_SV_TARGET(idx) SV_Target##idx
#define DECL_OPT_GBUFFER_TARGET(type, name, idx) type name : DECL_SV_TARGET(GBUFFER_IDX_AFTER(idx))

// URP GBuffer pass fragment shader output struct.
struct GBufferFragOutput
{
    half4 gBuffer0 : SV_Target0;
    half4 gBuffer1 : SV_Target1;
    half4 gBuffer2 : SV_Target2;
    half4 color    : SV_Target3; // Camera color attachment, used for GI during GBuffer laydown

    #if defined(GBUFFER_FEATURE_DEPTH)
    DECL_OPT_GBUFFER_TARGET(float, depth, GBUFFER_IDX_R_DEPTH);
    #endif

    #if defined(GBUFFER_FEATURE_SHADOWMASK)
    DECL_OPT_GBUFFER_TARGET(half4, shadowMask, GBUFFER_IDX_RGBA_SHADOWMASK);
    #endif

    #if defined(GBUFFER_FEATURE_RENDERING_LAYERS)
    DECL_OPT_GBUFFER_TARGET(uint, meshRenderingLayers, GBUFFER_IDX_R_RENDERING_LAYERS);
    #endif
};

#undef DECL_SV_TARGET
#undef DECL_OPT_GBUFFER_TARGET

#endif // UNIVERSAL_GBUFFER_FRAG_OUTPUT_INCLUDED
