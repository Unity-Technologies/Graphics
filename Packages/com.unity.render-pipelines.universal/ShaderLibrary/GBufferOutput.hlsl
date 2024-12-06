// This file contains functionality for writing contents of URP GBuffers.
// The functionality provided here is intended to be used during in the material GBuffer pass.
#ifndef UNIVERSAL_GBUFFEROUTPUT_INCLUDED
#define UNIVERSAL_GBUFFEROUTPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferCommon.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
    DECL_OPT_GBUFFER_TARGET(half4, meshRenderingLayers, GBUFFER_IDX_R_RENDERING_LAYERS);
    #endif
};

#undef DECL_SV_TARGET
#undef DECL_OPT_GBUFFER_TARGET

// Pack SurfaceData into GBuffers.
GBufferFragOutput PackGBuffersSurfaceData(SurfaceData surfaceData, InputData inputData, half3 globalIllumination)
{
    half3 packedNormalWS = PackGBufferNormal(inputData.normalWS);

    uint materialFlags = 0;

    // SimpleLit does not use _SPECULARHIGHLIGHTS_OFF to disable specular highlights.

    #ifdef _RECEIVE_SHADOWS_OFF
    materialFlags |= kMaterialFlagReceiveShadowsOff;
    #endif

    #if defined(LIGHTMAP_ON) && defined(_MIXED_LIGHTING_SUBTRACTIVE)
    materialFlags |= kMaterialFlagSubtractiveMixedLighting;
    #endif

    GBufferFragOutput output;
    output.gBuffer0 = half4(surfaceData.albedo.rgb, PackGBufferMaterialFlags(materialFlags));   // albedo          albedo          albedo          materialFlags   (sRGB rendertarget)
    output.gBuffer1 = half4(surfaceData.specular.rgb, surfaceData.occlusion);                   // specular        specular        specular        occlusion
    output.gBuffer2 = half4(packedNormalWS, surfaceData.smoothness);                            // encoded-normal  encoded-normal  encoded-normal  smoothness
    output.color    = half4(globalIllumination, 1);                                             // GI              GI              GI              unused          (lighting buffer)

    #if defined(GBUFFER_FEATURE_DEPTH)
    output.depth = inputData.positionCS.z;
    #endif

    #if defined(GBUFFER_FEATURE_SHADOWMASK)
    output.shadowMask = inputData.shadowMask; // will have unity_ProbesOcclusion value if subtractive lighting is used (baked)
    #endif

    #if defined(GBUFFER_FEATURE_RENDERING_LAYERS)
    uint renderingLayers = GetMeshRenderingLayer();
    output.meshRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0.0, 0.0, 0.0);
    #endif

    return output;
}

// Pack BRDFData into GBuffers.
GBufferFragOutput PackGBuffersBRDFData(BRDFData brdfData, InputData inputData, half smoothness, half3 globalIllumination, half occlusion = 1.0)
{
    half3 packedNormalWS = PackGBufferNormal(inputData.normalWS);

    uint materialFlags = 0;

    #ifdef _RECEIVE_SHADOWS_OFF
    materialFlags |= kMaterialFlagReceiveShadowsOff;
    #endif

    half3 packedSpecular;

    #ifdef _SPECULAR_SETUP
    materialFlags |= kMaterialFlagSpecularSetup;
    packedSpecular = brdfData.specular.rgb;
    #else
    packedSpecular.r = brdfData.reflectivity;
    packedSpecular.gb = 0.0;
    #endif

    #ifdef _SPECULARHIGHLIGHTS_OFF
    // During the next deferred shading pass, we don't use a shader variant to disable specular calculations.
    // Instead, we can either silence specular contribution when writing the gbuffer, and/or reserve a bit in the gbuffer
    // and use this during shading to skip computations via dynamic branching. Fastest option depends on platforms.
    materialFlags |= kMaterialFlagSpecularHighlightsOff;
    packedSpecular = 0.0.xxx;
    #endif

    #if defined(LIGHTMAP_ON) && defined(_MIXED_LIGHTING_SUBTRACTIVE)
    materialFlags |= kMaterialFlagSubtractiveMixedLighting;
    #endif

    GBufferFragOutput output;
    output.gBuffer0 = half4(brdfData.albedo.rgb, PackGBufferMaterialFlags(materialFlags));  // diffuse           diffuse         diffuse         materialFlags   (sRGB rendertarget)
    output.gBuffer1 = half4(packedSpecular, occlusion);                                     // metallic/specular specular        specular        occlusion
    output.gBuffer2 = half4(packedNormalWS, smoothness);                                    // encoded-normal    encoded-normal  encoded-normal  smoothness
    output.color = half4(globalIllumination, 1);                                            // GI                GI              GI              unused          (lighting buffer)

    #if defined(GBUFFER_FEATURE_DEPTH)
    output.depth = inputData.positionCS.z;
    #endif

    #if defined(GBUFFER_FEATURE_SHADOWMASK)
    output.shadowMask = inputData.shadowMask; // will have unity_ProbesOcclusion value if subtractive lighting is used (baked)
    #endif

    #if defined(GBUFFER_FEATURE_RENDERING_LAYERS)
    uint renderingLayers = GetMeshRenderingLayer();
    output.meshRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0.0, 0.0, 0.0);
    #endif

    return output;
}

#endif // UNIVERSAL_GBUFFERUTIL_INCLUDED
