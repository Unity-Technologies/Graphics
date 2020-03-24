#ifndef __BUILTINGIUTILITIES_HLSL__
#define __BUILTINGIUTILITIES_HLSL__

#if defined(SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE)

#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS

#if SHADERPASS == SHADERPASS_GBUFFER || SHADERPASS == SHADERPASS_FORWARD
// G-Buffer pass does not constain the standard light loop.
// Need to add all the required includes to use our custom probe volume clustered light list.
// Need PositionInputs definition for use as argument in LightLoopDef accessor functions.
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"
#endif // endof SHADER_PASS == SHADERPASS_GBUFFER

#elif SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_LIGHT_LOOP
#define UNINITIALIZED_GI float3((1 << 11), 1, (1 << 10))

bool IsUninitializedGI(float3 bakedGI)
{
    const float3 unitializedGI = UNINITIALIZED_GI;
    return all(bakedGI == unitializedGI);
}
#endif
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

float3 EvaluateProbeVolumes(PositionInputs posInputs, float3 normalWS, uint renderingLayers)
{
    #if defined(SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE)
    // SHADEROPTIONS_PROBE_VOLUMES can be defined in ShaderConfig.cs.hlsl but set to 0 for disabled.
    #if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_LIGHT_LOOP
        // If probe volumes are evaluated in the lightloop, we place a sentinel value to detect that no lightmap data is present at the current pixel,
        // and we can safely overwrite baked data value with value from probe volume evaluation in light loop.
        return _EnableProbeVolumes ? UNINITIALIZED_GI : float3(0, 0, 0);
    #elif SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS
    #if SHADERPASS == SHADERPASS_GBUFFER || SHADERPASS == SHADERPASS_FORWARD

    #if SHADERPASS == SHADERPASS_GBUFFER
        // posInputs.tileCoord will be zeroed out in GBuffer pass. Need to manually compute tile coord here.
        float2 positionSS = posInputs.positionNDC.xy * _ScreenSize.xy;
        uint2 tileCoord = uint2(positionSS) / ProbeVolumeGetTileSize();
        posInputs.tileCoord = tileCoord;
    #endif

        return _EnableProbeVolumes ? EvaluateProbeVolumesMaterialPass(posInputs, normalWS, renderingLayers) : float3(0, 0, 0);
    #else
        // !(SHADERPASS == SHADERPASS_GBUFFER || SHADERPASS == SHADERPASS_FORWARD)
        return float3(0, 0, 0);
    #endif
    #else
        // SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_DISABLED
        return float3(0, 0, 0);
    #endif

    #else
        // !defined(SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE)
        return float3(0, 0, 0);
    #endif
}

// In unity we can have a mix of fully baked lightmap (static lightmap) + enlighten realtime lightmap (dynamic lightmap)
// for each case we can have directional lightmap or not.
// Else we have lightprobe for dynamic/moving entity. Either SH9 per object lightprobe or SH4 per pixel per object volume probe
float3 SampleBakedGI(PositionInputs posInputs, float3 normalWS, uint renderingLayers, float2 uvStaticLightmap, float2 uvDynamicLightmap)
{
    float3 positionRWS = posInputs.positionWS;

#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
    // TODO: (Nick): If probe volumes are enabled, should we blend lightmap data with additive / subtractive probe volume data?
    return EvaluateLightmap(positionRWS, normalWS, uvStaticLightmap, uvDynamicLightmap);

#elif defined(SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE)
#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE != PROBEVOLUMESEVALUATIONMODES_DISABLED
    return EvaluateProbeVolumes(posInputs, normalWS, renderingLayers);
#else
    // Fallback to legacy ProbeVolume when lightmaps are not availible and Probe Volumes are disabled.
    return EvaluateProbeVolumeLegacy(positionRWS, normalWS);
#endif
#else
    // Fallback to legacy ProbeVolume when lightmaps are not availible and Probe Volumes are disabled.
    return EvaluateProbeVolumeLegacy(positionRWS, normalWS);
#endif
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

#if defined(SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE)
#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIAL_PASS
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
#endif
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
