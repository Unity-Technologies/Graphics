// This file contains functionality for reading the contents of URP GBuffers.
// Shaders using these functions must execute after the GBuffer pass, or else GBuffers will be invalid.
#ifndef UNIVERSAL_GBUFFERINPUT_INCLUDED
#define UNIVERSAL_GBUFFERINPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferCommon.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"

#if defined(GBUFFER_FBFETCH_AVAILABLE)
// Native render pass / framebuffer fetch available!
// Use framebuffer input.

// Static index GBuffers:
FRAMEBUFFER_INPUT_X_HALF(GBUFFER_IDX_RGB_BASECOLOR_A_FLAGS);
FRAMEBUFFER_INPUT_X_HALF(GBUFFER_IDX_RGB_SPECULAR_R_METALLIC_A_OCCLUSION);
FRAMEBUFFER_INPUT_X_HALF(GBUFFER_IDX_RGB_NORMALS_A_SMOOTHNESS);
FRAMEBUFFER_INPUT_X_FLOAT(GBUFFER_IDX_R_DEPTH);

// Dynamic index GBuffer: Shadow mask
#if defined(GBUFFER_FEATURE_SHADOWMASK)
FRAMEBUFFER_INPUT_X_HALF(GBUFFER_IDX_RGBA_SHADOWMASK);
#endif

// Dynamic index GBuffer: Rendering layers
#if defined(GBUFFER_FEATURE_RENDERING_LAYERS)
FRAMEBUFFER_INPUT_X_UINT(GBUFFER_IDX_R_RENDERING_LAYERS);
#endif

#else
// Native render pass / framebuffer fetch is not available.
// Fall back to regular texture bindings.

// Helper macro to convert GBuffer index macro to texture slot name.
#define GBUFFER_TEX2D_NAME(index) _GBuffer##index

// Static index GBuffers:
TEXTURE2D_X_HALF(GBUFFER_TEX2D_NAME(GBUFFER_IDX_RGB_BASECOLOR_A_FLAGS));
TEXTURE2D_X_HALF(GBUFFER_TEX2D_NAME(GBUFFER_IDX_RGB_SPECULAR_R_METALLIC_A_OCCLUSION));
TEXTURE2D_X_HALF(GBUFFER_TEX2D_NAME(GBUFFER_IDX_RGB_NORMALS_A_SMOOTHNESS));

// We never bind GBUFFER_TEX2D_NAME(GBUFFER_IDX_R_DEPTH), instead we expect the depth to be bound to the _CameraDepthTexture slot.
TEXTURE2D_X_FLOAT(_CameraDepthTexture);

// Dynamic index GBuffer: Shadow mask
#if defined(GBUFFER_FEATURE_SHADOWMASK)
TEXTURE2D_X_HALF(GBUFFER_TEX2D_NAME(GBUFFER_IDX_AFTER(GBUFFER_IDX_RGBA_SHADOWMASK)));
#endif

// Dynamic index GBuffer: Rendering layers
#if defined(GBUFFER_FEATURE_RENDERING_LAYERS)
TYPED_TEXTURE2D_X(uint4, GBUFFER_TEX2D_NAME(GBUFFER_IDX_AFTER(GBUFFER_IDX_R_RENDERING_LAYERS)));
#endif

#endif

// Load raw GBuffer data.
// Use this if overriding data in the GBuffers, otherwise use UnpackGBuffers().
// If shadow mask is not used, shadowMask defaults to (1, 1, 1, 1).
// If rendering layers is not used, renderingLayers defaults to 0xffff.
// Note that unCoord2 is in pixel coordinates, not screen UVs.
void LoadGBuffers(uint2 unCoord2, out half4 gBuffer0, out half4 gBuffer1, out half4 gBuffer2, out float depth,
                  out uint renderingLayers, out half4 shadowMask)
{
    renderingLayers = 0xFFFFFFFF;
    shadowMask = half4(1, 1, 1, 1);

    #if defined(GBUFFER_FBFETCH_AVAILABLE)

    depth    = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER_IDX_R_DEPTH, unCoord2).x;
    gBuffer0 = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER_IDX_RGB_BASECOLOR_A_FLAGS, unCoord2);
    gBuffer1 = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER_IDX_RGB_SPECULAR_R_METALLIC_A_OCCLUSION, unCoord2);
    gBuffer2 = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER_IDX_RGB_NORMALS_A_SMOOTHNESS, unCoord2);

    #if defined(GBUFFER_FEATURE_SHADOWMASK)
    shadowMask = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER_IDX_RGBA_SHADOWMASK, unCoord2);
    #endif

    #if defined(GBUFFER_FEATURE_RENDERING_LAYERS)
    renderingLayers = LOAD_FRAMEBUFFER_X_INPUT(GBUFFER_IDX_R_RENDERING_LAYERS, unCoord2).x;
    #endif

    #else

    depth    = LOAD_TEXTURE2D_X(_CameraDepthTexture, unCoord2).x;
    gBuffer0 = LOAD_TEXTURE2D_X(GBUFFER_TEX2D_NAME(GBUFFER_IDX_RGB_BASECOLOR_A_FLAGS), unCoord2);
    gBuffer1 = LOAD_TEXTURE2D_X(GBUFFER_TEX2D_NAME(GBUFFER_IDX_RGB_SPECULAR_R_METALLIC_A_OCCLUSION), unCoord2);
    gBuffer2 = LOAD_TEXTURE2D_X(GBUFFER_TEX2D_NAME(GBUFFER_IDX_RGB_NORMALS_A_SMOOTHNESS), unCoord2);

    #if defined(GBUFFER_FEATURE_SHADOWMASK)
    shadowMask = LOAD_TEXTURE2D_X(GBUFFER_TEX2D_NAME(GBUFFER_IDX_AFTER(GBUFFER_IDX_RGBA_SHADOWMASK)), unCoord2);
    #endif

    #if defined(GBUFFER_FEATURE_RENDERING_LAYERS)
    renderingLayers = LOAD_TEXTURE2D_X(GBUFFER_TEX2D_NAME(GBUFFER_IDX_AFTER(GBUFFER_IDX_R_RENDERING_LAYERS)), unCoord2).x;
    #endif

    #endif
}

// Load raw GBuffer content and unpack into GBufferData.
// Note that unCoord2 is in pixel coordinates, not screen UVs.
GBufferData UnpackGBuffers(uint2 unCoord2)
{
    half4 gBuffer0;
    half4 gBuffer1;
    half4 gBuffer2;
    float depth;
    uint renderingLayers;
    half4 shadowMask;

    LoadGBuffers(unCoord2, gBuffer0, gBuffer1, gBuffer2, depth, renderingLayers, shadowMask);

    GBufferData gBufferData;
    ZERO_INITIALIZE(GBufferData, gBufferData);

    gBufferData.baseColor = gBuffer0.rgb;
    gBufferData.materialFlags = UnpackGBufferMaterialFlags(gBuffer0.a);
    gBufferData.specularColor = gBuffer1.rgb;
    gBufferData.occlusion = gBuffer1.a;
    gBufferData.normalWS = normalize(UnpackGBufferNormal(gBuffer2.rgb));
    gBufferData.smoothness = gBuffer2.a;
    gBufferData.depth = depth;
    gBufferData.shadowMask = shadowMask;
    gBufferData.meshRenderingLayers = renderingLayers;

    return gBufferData;
}

// Interpret SurfaceData from GBufferData.
// Used by SimpleLit materials.
SurfaceData GBufferDataToSurfaceData(GBufferData gBufferData)
{
    SurfaceData surfaceData = (SurfaceData)0;

    surfaceData.albedo = gBufferData.baseColor;
    surfaceData.occlusion = 0.0;
    surfaceData.specular = gBufferData.specularColor;
    surfaceData.metallic = 0.0; // Not used by SimpleLit material.
    surfaceData.alpha = 1.0; // GBuffer only contains opaque materials
    surfaceData.smoothness = gBufferData.smoothness;

    return surfaceData;
}

// Interpret BRDFData from GBufferData.
// Used by Lit materials.
BRDFData GBufferDataToBRDFData(GBufferData gBufferData)
{
    half3 albedo = gBufferData.baseColor;
    half3 specular = gBufferData.specularColor;
    uint materialFlags = gBufferData.materialFlags;
    half smoothness = gBufferData.smoothness;

    BRDFData brdfData = (BRDFData)0;
    half alpha = half(1.0); // NOTE: alpha can get modfied, forward writes it out (_ALPHAPREMULTIPLY_ON).

    half3 brdfDiffuse;
    half3 brdfSpecular;
    half reflectivity;
    half oneMinusReflectivity;

    UNITY_BRANCH if ((materialFlags & kMaterialFlagSpecularSetup) != 0)
    {
        // Specular setup
        reflectivity = ReflectivitySpecular(specular);
        oneMinusReflectivity = half(1.0) - reflectivity;
        brdfDiffuse = albedo * oneMinusReflectivity;
        brdfSpecular = specular;
    }
    else
    {
        // Metallic setup
        reflectivity = specular.r;
        oneMinusReflectivity = 1.0 - reflectivity;
        half metallic = MetallicFromReflectivity(reflectivity);
        brdfDiffuse = albedo * oneMinusReflectivity;
        brdfSpecular = lerp(kDielectricSpec.rgb, albedo, metallic);
    }

    InitializeBRDFDataDirect(albedo, brdfDiffuse, brdfSpecular, reflectivity, oneMinusReflectivity, smoothness, alpha, brdfData);

    return brdfData;
}

#ifdef GBUFFER_TEX2D_NAME
#undef GBUFFER_TEX2D_NAME
#endif

#endif
