// This file contains common functionality used for URP GBuffer passes.
// This should not be included directly, instead include GBufferInput.hlsl or GBufferOutput.hlsl.
#ifndef UNIVERSAL_GBUFFERCOMMON_INCLUDED
#define UNIVERSAL_GBUFFERCOMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

// Material flags:

#define kMaterialFlagReceiveShadowsOff        1 // Does not receive dynamic shadows
#define kMaterialFlagSpecularHighlightsOff    2 // Does not receive specular
#define kMaterialFlagSubtractiveMixedLighting 4 // The geometry uses subtractive mixed lighting
#define kMaterialFlagSpecularSetup            8 // Lit material use specular setup instead of metallic setup

// GBuffer feature macros. Deduced from active keywords:

#if defined(_RENDER_PASS_ENABLED)
    #define GBUFFER_FBFETCH_AVAILABLE 1
#endif

#if defined(GBUFFER_FBFETCH_AVAILABLE)
    #define GBUFFER_FEATURE_DEPTH 1
#endif

#if defined(SHADOWS_SHADOWMASK)
    #define GBUFFER_FEATURE_SHADOWMASK 1
#elif !defined(LIGHTMAP_ON) && defined(LIGHTMAP_SHADOW_MIXING)
    #define GBUFFER_FEATURE_SHADOWMASK 1
#elif defined(_DEFERRED_MIXED_LIGHTING)
    #define GBUFFER_FEATURE_SHADOWMASK 1
#endif

#if (defined(_WRITE_RENDERING_LAYERS) || defined(_LIGHT_LAYERS))
    #define GBUFFER_FEATURE_RENDERING_LAYERS 1
#endif

// Setup static GBuffer index macros:

#define GBUFFER_IDX_RGB_BASECOLOR_A_FLAGS               0
#define GBUFFER_IDX_RGB_SPECULAR_R_METALLIC_A_OCCLUSION 1
#define GBUFFER_IDX_RGB_NORMALS_A_SMOOTHNESS            2

// Helper macro to get GBuffer index + 1
#define GBUFFER_IDX_AFTER(x) GBUFFER_IDX_AFTER_ ## x
#define GBUFFER_IDX_AFTER_0 1
#define GBUFFER_IDX_AFTER_1 2
#define GBUFFER_IDX_AFTER_2 3
#define GBUFFER_IDX_AFTER_3 4
#define GBUFFER_IDX_AFTER_4 5
#define GBUFFER_IDX_AFTER_5 6
#define GBUFFER_IDX_AFTER_6 7
#define GBUFFER_IDX_AFTER_7 8

// Setup dynamic GBuffer index macros.
// These are extra GBuffers that may be used depending on which features are enabled.
// Index is dynamically assigned for each combination of enabled/disabled features.
// Possible features: [GBUFFER_FEATURE_DEPTH, GBUFFER_FEATURE_RENDERING_LAYERS, GBUFFER_FEATURE_SHADOWMASK]
#if defined(GBUFFER_FEATURE_DEPTH)
    // [1, 0, 0]
    #define GBUFFER_IDX_R_DEPTH GBUFFER_IDX_AFTER(GBUFFER_IDX_RGB_NORMALS_A_SMOOTHNESS)
    #if defined(GBUFFER_FEATURE_RENDERING_LAYERS)
        // [1, 1, 0]
        #define GBUFFER_IDX_R_RENDERING_LAYERS GBUFFER_IDX_AFTER(GBUFFER_IDX_R_DEPTH)
        #if defined(GBUFFER_FEATURE_SHADOWMASK)
            // [1, 1, 1]
            #define GBUFFER_IDX_RGBA_SHADOWMASK GBUFFER_IDX_AFTER(GBUFFER_IDX_R_RENDERING_LAYERS)
        #endif
    #elif defined(GBUFFER_FEATURE_SHADOWMASK)
        // [1, 0, 1]
        #define GBUFFER_IDX_RGBA_SHADOWMASK GBUFFER_IDX_AFTER(GBUFFER_IDX_R_DEPTH)
    #endif
#elif defined(GBUFFER_FEATURE_RENDERING_LAYERS)
    // [0, 1, 0]
    #define GBUFFER_IDX_R_RENDERING_LAYERS GBUFFER_IDX_AFTER(GBUFFER_IDX_RGB_NORMALS_A_SMOOTHNESS)
    #if defined(GBUFFER_FEATURE_SHADOWMASK)
        // [0, 1, 1]
        #define GBUFFER_IDX_RGBA_SHADOWMASK GBUFFER_IDX_AFTER(GBUFFER_IDX_R_RENDERING_LAYERS)
    #endif
#elif defined(GBUFFER_FEATURE_SHADOWMASK)
    // [0, 0, 1]
    #define GBUFFER_IDX_RGBA_SHADOWMASK GBUFFER_IDX_AFTER(GBUFFER_IDX_RGB_NORMALS_A_SMOOTHNESS)
#endif

// Unpacked URP GBuffer data.
struct GBufferData
{
    half3 baseColor;
    half smoothness;
    // For Lit materials, if !(materialFlags & kMaterialFlagSpecularSetup) then specularColor.r contains metallic.
    half3 specularColor;
    half occlusion;
    float3 normalWS;
    uint materialFlags;
    float depth;

    // Only assigned if GBUFFER_FEATURE_SHADOWMASK is defined!
    // Otherwise, defaults to (1, 1, 1, 1)
    half4 shadowMask;

    // Only assigned if GBUFFER_FEATURE_RENDERING_LAYERS is defined!
    // Otherwise, defaults to 0xFFFF (Everything)
    uint meshRenderingLayers;
};

// GBuffer utility functions:

float PackGBufferMaterialFlags(uint materialFlags)
{
    return materialFlags * (half(1.0) / half(255.0));
}

uint UnpackGBufferMaterialFlags(float packedMaterialFlags)
{
    return uint((packedMaterialFlags * half(255.0)) + half(0.5));
}

#if defined(_GBUFFER_NORMALS_OCT)

half3 PackGBufferNormal(half3 normalWS)
{
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms.
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0, +1]
    return half3(PackFloat2To888(remappedOctNormalWS));               // values between [ 0, +1]
}

half3 UnpackGBufferNormal(half3 packedNormalWS)
{
    half2 remappedOctNormalWS = half2(Unpack888ToFloat2(packedNormalWS));// values between [ 0, +1]
    half2 octNormalWS = remappedOctNormalWS.xy * half(2.0) - half(1.0);  // values between [-1, +1]
    return half3(UnpackNormalOctQuadEncode(octNormalWS));                // values between [-1, +1]
}

#else

half3 PackGBufferNormal(half3 normalWS)
{ return normalWS; }                                                      // values between [-1, +1]

half3 UnpackGBufferNormal(half3 packedNormalWS)
{ return packedNormalWS; }                                                // values between [-1, +1]

#endif

#endif
