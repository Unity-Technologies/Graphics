#ifndef __BUILTINGIUTILITIES_HLSL__
#define __BUILTINGIUTILITIES_HLSL__

// Helper function for indirect control volume
float GetIndirectDiffuseMultiplier(uint renderingLayers)
{
    uint indirectDiffuseLayers = uint(_IndirectLightingMultiplier.y * 255.5);
    return indirectDiffuseLayers & renderingLayers ? _IndirectLightingMultiplier.x : 1.0f;
}

float GetIndirectSpecularMultiplier(uint renderingLayers)
{
    uint indirectSpecularLayers = uint(_IndirectLightingMultiplier.w * 255.5);
    return indirectSpecularLayers & renderingLayers ? _IndirectLightingMultiplier.z : 1.0f;
}

#ifdef SHADERPASS
#if ((SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS) && (SHADERPASS == SHADERPASS_GBUFFER || SHADERPASS == SHADERPASS_FORWARD)) || \
     ((SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_LIGHT_LOOP) && (SHADERPASS == SHADERPASS_DEFERRED_LIGHTING || SHADERPASS == SHADERPASS_FORWARD))
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"
#endif
#endif // #ifdef SHADERPASS

#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_LIGHT_LOOP

#define UNINITIALIZED_GI float3((1 << 11), 1, (1 << 10))

bool IsUninitializedGI(float3 bakedGI)
{
    const float3 unitializedGI = UNINITIALIZED_GI;
    return all(bakedGI == unitializedGI);
}
#endif

// Return camera relative probe volume world to object transformation
float4x4 GetProbeVolumeWorldToObject()
{
    return ApplyCameraTranslationToInverseMatrix(unity_ProbeVolumeWorldToObject);
}

float3 EvaluateLightmap(float3 positionRWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap)
{
    float3 bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

#ifdef UNITY_LIGHTMAP_FULL_HDR
    bool useRGBMLightmap = false;
    float4 decodeInstructions = float4(0.0, 0.0, 0.0, 0.0); // Never used but needed for the interface since it supports gamma lightmaps
#else
    bool useRGBMLightmap = true;
#if defined(UNITY_LIGHTMAP_RGBM_ENCODING)
    float4 decodeInstructions = float4(34.493242, 2.2, 0.0, 0.0); // range^2.2 = 5^2.2, gamma = 2.2
#else
    float4 decodeInstructions = float4(2.0, 2.2, 0.0, 0.0); // range = 2.0^2.2 = 4.59
#endif
#endif

#ifdef LIGHTMAP_ON
#ifdef DIRLIGHTMAP_COMBINED
    bakeDiffuseLighting += SampleDirectionalLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap),
        TEXTURE2D_ARGS(unity_LightmapInd, samplerunity_Lightmap),
        uvStaticLightmap, unity_LightmapST, normalWS, useRGBMLightmap, decodeInstructions);
#else
    bakeDiffuseLighting += SampleSingleLightmap(TEXTURE2D_ARGS(unity_Lightmap, samplerunity_Lightmap), uvStaticLightmap, unity_LightmapST, useRGBMLightmap, decodeInstructions);
#endif
#endif

#ifdef DYNAMICLIGHTMAP_ON
#ifdef DIRLIGHTMAP_COMBINED
    bakeDiffuseLighting += SampleDirectionalLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap),
        TEXTURE2D_ARGS(unity_DynamicDirectionality, samplerunity_DynamicLightmap),
        uvDynamicLightmap, unity_DynamicLightmapST, normalWS, false, decodeInstructions);
#else
    bakeDiffuseLighting += SampleSingleLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap), uvDynamicLightmap, unity_DynamicLightmapST, false, decodeInstructions);
#endif
#endif

    return bakeDiffuseLighting;
}

float3 EvaluateProbeVolumeLegacy(float3 positionRWS, float3 normalWS)
{
    if (unity_ProbeVolumeParams.x == 0.0)
    {
        // TODO: pass a tab of coefficient instead!
        real4 SHCoefficients[7];
        SHCoefficients[0] = unity_SHAr;
        SHCoefficients[1] = unity_SHAg;
        SHCoefficients[2] = unity_SHAb;
        SHCoefficients[3] = unity_SHBr;
        SHCoefficients[4] = unity_SHBg;
        SHCoefficients[5] = unity_SHBb;
        SHCoefficients[6] = unity_SHC;

        return SampleSH9(SHCoefficients, normalWS);
    }
    else
    {
#if RAYTRACING_ENABLED
        if (unity_ProbeVolumeParams.w == 1.0)
            return SampleProbeVolumeSH9(TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionRWS, normalWS, GetProbeVolumeWorldToObject(),
                unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z, unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz);
        else
#endif
            return SampleProbeVolumeSH4(TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionRWS, normalWS, GetProbeVolumeWorldToObject(),
                unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z, unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz);
    }
}

float3 EvaluateProbeVolumes(inout float probeVolumeHierarchyWeight, PositionInputs posInputs, float3 normalWS, uint renderingLayers)
{
    // SHADEROPTIONS_PROBE_VOLUMES can be defined in ShaderConfig.cs.hlsl but set to 0 for disabled.
    #if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_LIGHT_LOOP
        // If probe volumes are evaluated in the lightloop, we place a sentinel value to detect that no lightmap data is present at the current pixel,
        // and we can safely overwrite baked data value with value from probe volume evaluation in light loop.
        return UNINITIALIZED_GI;
    #elif SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS
        #ifdef SHADERPASS
        #if SHADERPASS == SHADERPASS_GBUFFER || SHADERPASS == SHADERPASS_FORWARD

        #if SHADERPASS == SHADERPASS_GBUFFER || (SHADERPASS == SHADERPASS_FORWARD && defined(USE_FPTL_LIGHTLIST))
        // posInputs.tileCoord will be zeroed out in GBuffer pass.
        // posInputs.tileCoord will be incorrect for probe volumes (which use clustered) in forward if forward lightloop is using FTPL lightlist (i.e: in ForwardOnly lighting configuration). 
        // Need to manually compute tile coord here.
        float2 positionSS = posInputs.positionNDC.xy * _ScreenSize.xy;
        uint2 tileCoord = uint2(positionSS) / ProbeVolumeGetTileSize();
        posInputs.tileCoord = tileCoord;
        #endif

        float3 combinedGI = EvaluateProbeVolumesMaterialPass(probeVolumeHierarchyWeight, posInputs, normalWS, renderingLayers);
        combinedGI += EvaluateProbeVolumeAmbientProbeFallback(probeVolumeHierarchyWeight, normalWS);
        return combinedGI;

        #else
        // !(SHADERPASS == SHADERPASS_GBUFFER || SHADERPASS == SHADERPASS_FORWARD)
        return float3(0, 0, 0);
        #endif
        #else 
        return float3(0, 0, 0);
        #endif // #ifdef SHADERPASS
    #else
        // SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_DISABLED
        return float3(0, 0, 0);
    #endif
}

// In unity we can have a mix of fully baked lightmap (static lightmap) + enlighten realtime lightmap (dynamic lightmap)
// for each case we can have directional lightmap or not.
// Else we have lightprobe for dynamic/moving entity. Either SH9 per object lightprobe or SH4 per pixel per object volume probe
float3 SampleBakedGI(PositionInputs posInputs, float3 normalWS, uint renderingLayers, float2 uvStaticLightmap, float2 uvDynamicLightmap)
{
    float3 positionRWS = posInputs.positionWS;

#define SAMPLE_LIGHTMAP (defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON))
#define SAMPLE_PROBEVOLUME (SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE != PROBEVOLUMESEVALUATIONMODES_DISABLED) \
    && (!SAMPLE_LIGHTMAP || SHADEROPTIONS_PROBE_VOLUMES_ADDITIVE_BLENDING)
#define SAMPLE_PROBEVOLUME_LEGACY (!SAMPLE_LIGHTMAP && !SAMPLE_PROBEVOLUME)

    float3 combinedGI = float3(0, 0, 0);

#if SAMPLE_LIGHTMAP
    combinedGI += EvaluateLightmap(positionRWS, normalWS, uvStaticLightmap, uvDynamicLightmap);
#endif

#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_LIGHT_LOOP
    // If probe volumes are evaluated in the lightloop, we place a sentinel value to detect that no lightmap data is present at the current pixel,
    // and we can safely overwrite baked data value with value from probe volume evaluation in light loop.
#if !SAMPLE_LIGHTMAP
    return UNINITIALIZED_GI;
#endif 

#else // PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS || PROBEVOLUMESEVALUATIONMODES_DISABLED
#if SAMPLE_PROBEVOLUME
#if SAMPLE_LIGHTMAP
    float probeVolumeHierarchyWeight = 1.0f;
#else
    float probeVolumeHierarchyWeight = 0.0f;
#endif
    combinedGI += EvaluateProbeVolumes(probeVolumeHierarchyWeight, posInputs, normalWS, renderingLayers);
#endif

#if SAMPLE_PROBEVOLUME_LEGACY
    combinedGI += EvaluateProbeVolumeLegacy(positionRWS, normalWS);
#endif
#endif

return combinedGI;

#undef SAMPLE_LIGHTMAP
#undef SAMPLE_PROBEVOLUME
#undef SAMPLE_PROBEVOLUME_LEGACY
}

// Function signature of SampleBakedGI changed when probe volumes we added, as they require full PositionInputs.
// This legacy function signature is exposed in a shader graph node, so must continue to be supported.
float3 SampleBakedGI(float3 positionRWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap)
{
    // Need PositionInputs for indexing probe volume clusters, but they are not availbile from the current SampleBakedGI() function signature.
    // Reconstruct.
    uint renderingLayers = DEFAULT_LIGHT_LAYERS;
    PositionInputs posInputs;
    ZERO_INITIALIZE(PositionInputs, posInputs);
    posInputs.positionWS = positionRWS;

#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS
    #ifdef SHADERPASS
    #if SHADERPASS == SHADERPASS_GBUFFER || SHADERPASS == SHADERPASS_FORWARD
    float4 positionCS = mul(UNITY_MATRIX_VP, float4(positionRWS, 1.0));
    positionCS.xyz /= positionCS.w;
    float2 positionNDC = positionCS.xy * float2(0.5, (_ProjectionParams.x > 0) ? 0.5 : -0.5) + 0.5;
    float2 positionSS = positionNDC.xy * _ScreenSize.xy;
    uint2 tileCoord = uint2(positionSS) / ProbeVolumeGetTileSize();

    posInputs.tileCoord = tileCoord; // Needed for probe volume cluster Indexing.
    posInputs.linearDepth = LinearEyeDepth(positionRWS, UNITY_MATRIX_V); // Needed for probe volume cluster Indexing.
    posInputs.positionNDC = float2(0, 0); // Not needed for probe volume cluster indexing.
    posInputs.deviceDepth = 0.0f; // Not needed for probe volume cluster indexing.

    // Use uniform directly - The float need to be cast to uint (as unity don't support to set a uint as uniform)
    renderingLayers = _EnableLightLayers ? asuint(unity_RenderingLayer.x) : DEFAULT_LIGHT_LAYERS;
    #endif
    #endif // #ifdef SHADERPASS
#endif

    return SampleBakedGI(posInputs, normalWS, renderingLayers, uvStaticLightmap, uvDynamicLightmap);
}

float4 SampleShadowMask(float3 positionRWS, float2 uvStaticLightmap) // normalWS not use for now
{
#if defined(LIGHTMAP_ON)
    float2 uv = uvStaticLightmap * unity_LightmapST.xy + unity_LightmapST.zw;
    float4 rawOcclusionMask = SAMPLE_TEXTURE2D(unity_ShadowMask, samplerunity_ShadowMask, uv); // Can't reuse sampler from Lightmap because with shader graph, the compile could optimize out the lightmaps if metal is 1
#else
    float4 rawOcclusionMask;
    if (unity_ProbeVolumeParams.x == 1.0)
    {
        rawOcclusionMask = SampleProbeOcclusion(TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionRWS, GetProbeVolumeWorldToObject(),
            unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z, unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz);
    }
    else
    {
        // Note: Default value when the feature is not enabled is float(1.0, 1.0, 1.0, 1.0) in C++
        rawOcclusionMask = unity_ProbesOcclusion;
    }
#endif

    return rawOcclusionMask;
}

#endif
