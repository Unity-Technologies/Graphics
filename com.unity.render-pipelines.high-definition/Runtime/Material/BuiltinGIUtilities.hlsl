#ifndef __BUILTINGIUTILITIES_HLSL__
#define __BUILTINGIUTILITIES_HLSL__

#if defined(SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE)

#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIALPASS

#if SHADERPASS == SHADERPASS_GBUFFER || SHADERPASS == SHADERPASS_FORWARD
// G-Buffer pass does not constain the standard light loop.
// Need to add all the required includes to use our custom probe volume clustered light list.
// Need PositionInputs definition for use as argument in LightLoopDef accessor functions.
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"
#endif // endof SHADER_PASS == SHADERPASS_GBUFFER

#elif SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_LIGHTLOOP
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

// In unity we can have a mix of fully baked lightmap (static lightmap) + enlighten realtime lightmap (dynamic lightmap)
// for each case we can have directional lightmap or not.
// Else we have lightprobe for dynamic/moving entity. Either SH9 per object lightprobe or SH4 per pixel per object volume probe
float3 SampleBakedGI(float3 positionRWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap)
{
    // If there is no lightmap, it assume lightprobe
#if !defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)

#if defined(SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE)
// SHADEROPTIONS_PROBE_VOLUMES can be defined in ShaderConfig.cs.hlsl but set to 0 for disabled.
#if SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_LIGHTLOOP
    // ProbeVolumes are incompatible with legacy Light probes
    return _EnableProbeVolumes ? UNINITIALIZED_GI : float3(0, 0, 0);

#elif SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIALPASS
    if (!_EnableProbeVolumes)
    {
        return float3(0, 0, 0);
    }

#if SHADERPASS == SHADERPASS_GBUFFER || SHADERPASS == SHADERPASS_FORWARD

    // Need PositionInputs for indexing tiles / clusters, but they are not availbile from the current SampleBakedGI() function signature.
    // Avoiding updating the function signature for now, because there is a shadergraph node that this would effect.
    // For now, pay the cost of reconstructing PositionInputs here.
    float4 positionCS = mul(UNITY_MATRIX_VP, float4(positionRWS, 1.0));
    positionCS.xyz /= positionCS.w;
    float2 positionNDC = positionCS.xy * float2(0.5, -0.5) + 0.5;
    float2 positionSS = positionNDC.xy * _ScreenSize.xy;
    uint2 tileCoord = uint2(positionSS) / ProbeVolumeGetTileSize();

    PositionInputs posInputs;
    posInputs.positionWS = positionRWS;
    posInputs.tileCoord = tileCoord; // Needed for cluster Indexing.
    posInputs.linearDepth = LinearEyeDepth(positionRWS, UNITY_MATRIX_V); // Needed for cluster Indexing.
    posInputs.positionNDC = float2(0, 0); // Not needed for cluster indexing.
    posInputs.deviceDepth = 0.0f; // Not needed for cluster indexing.


    return EvaluateProbeVolumesMaterialPass(posInputs, normalWS);
#else
    return float3(0, 0, 0);
#endif // endof SHADERPASS == SHADERPASS_GBUFFER || SHADERPASS == SHADERPASS_FORWARD
#endif // endof SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE == PROBEVOLUMESEVALUATIONMODES_MATERIALPASS
#endif // endof defined(SHADEROPTIONS_PROBE_VOLUMES_EVALUATION_MODE)

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

#else

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

#endif
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
