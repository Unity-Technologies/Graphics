// This file contains functionality for writing contents of URP GBuffers.
// The functionality provided here is intended to be used during in the material GBuffer pass.
#ifndef UNIVERSAL_GBUFFEROUTPUT_INCLUDED
#define UNIVERSAL_GBUFFEROUTPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferCommon.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferFragOutput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.deprecated.hlsl"

// Pack SurfaceData into GBuffers.
GBufferFragOutput PackGBuffersSurfaceData(SurfaceData surfaceData, InputData inputData, half3 globalIllumination, bool receiveShadows)
{
    half3 packedNormalWS = PackGBufferNormal(inputData.normalWS);

    uint materialFlags = 0;

    // SimpleLit does not use _SPECULARHIGHLIGHTS_OFF to disable specular highlights.

    if (!receiveShadows)
        materialFlags |= kMaterialFlagReceiveShadowsOff;

    if (LightmapAvailable() && MixedLightingSubtractive())
        materialFlags |= kMaterialFlagSubtractiveMixedLighting;

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
    output.meshRenderingLayers = EncodeMeshRenderingLayer();
    #endif

    return output;
}

// Pack BRDFData into GBuffers.
// isSpecularSetup: packs the specular color instead of reflectivity (specular vs metallic workflow).
// useSpecularHighlights: when false, silences packed specular and flags the surface so the deferred shading pass skips specular.
GBufferFragOutput PackGBuffersBRDFData(BRDFData brdfData, InputData inputData, half smoothness, half3 globalIllumination, half occlusion, bool receiveShadows, bool isSpecularSetup, bool useSpecularHighlights)
{
    half3 packedNormalWS = PackGBufferNormal(inputData.normalWS);

    uint materialFlags = 0;

    if (!receiveShadows)
        materialFlags |= kMaterialFlagReceiveShadowsOff;

    half3 packedSpecular;

    if (isSpecularSetup)
    {
        materialFlags |= kMaterialFlagSpecularSetup;
        packedSpecular = brdfData.specular.rgb;
    }
    else
    {
        packedSpecular.r = brdfData.reflectivity;
        packedSpecular.gb = 0.0;
    }

    if (!useSpecularHighlights)
    {
        // During the next deferred shading pass, we don't use a shader variant to disable specular calculations.
        // Instead, we can either silence specular contribution when writing the gbuffer, and/or reserve a bit in the gbuffer
        // and use this during shading to skip computations via dynamic branching. Fastest option depends on platforms.
        materialFlags |= kMaterialFlagSpecularHighlightsOff;
        packedSpecular = half3(0.0, 0.0, 0.0);
    }

    if (LightmapAvailable() && MixedLightingSubtractive())
        materialFlags |= kMaterialFlagSubtractiveMixedLighting;

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
    output.meshRenderingLayers = EncodeMeshRenderingLayer();
    #endif

    return output;
}

#endif // UNIVERSAL_GBUFFERUTIL_INCLUDED
