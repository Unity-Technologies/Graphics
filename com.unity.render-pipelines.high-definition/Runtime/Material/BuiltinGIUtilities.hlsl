#ifndef __BUILTINGIUTILITIES_HLSL__
#define __BUILTINGIUTILITIES_HLSL__

// Include the IndirectDiffuseMode enum
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceGlobalIllumination.cs.hlsl"

#ifdef SHADERPASS
#if ((SHADEROPTIONS_ENABLE_PROBE_VOLUMES == 1) && (SHADERPASS == SHADERPASS_DEFERRED_LIGHTING || SHADERPASS == SHADERPASS_FORWARD))
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"
#endif
#endif // #ifdef SHADERPASS

#if SHADEROPTIONS_ENABLE_PROBE_VOLUMES == 1

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

void EvaluateLightmap(float3 positionRWS, float3 normalWS, float3 backNormalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap, inout float3 bakeDiffuseLighting, inout float3 backBakeDiffuseLighting)
{
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

#if defined(UNITY_DOTS_INSTANCING_ENABLED)
#define LIGHTMAP_NAME unity_Lightmaps
#define LIGHTMAP_INDIRECTION_NAME unity_LightmapsInd
#define SHADOWMASK_NAME unity_ShadowMasks
#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmaps
#define SHADOWMASK_SAMPLER_NAME samplerunity_ShadowMasks
#define LIGHTMAP_SAMPLE_EXTRA_ARGS uvStaticLightmap, unity_LightmapIndex.x
#define SHADOWMASK_SAMPLE_EXTRA_ARGS uv, unity_LightmapIndex.x
#else
#define LIGHTMAP_NAME unity_Lightmap
#define LIGHTMAP_INDIRECTION_NAME unity_LightmapInd
#define SHADOWMASK_NAME unity_ShadowMask
#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmap
#define SHADOWMASK_SAMPLER_NAME samplerunity_ShadowMask
#define LIGHTMAP_SAMPLE_EXTRA_ARGS uvStaticLightmap
#define SHADOWMASK_SAMPLE_EXTRA_ARGS uv
#endif

#ifdef LIGHTMAP_ON
#ifdef DIRLIGHTMAP_COMBINED
    SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME),
            TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_INDIRECTION_NAME, LIGHTMAP_SAMPLER_NAME),
            LIGHTMAP_SAMPLE_EXTRA_ARGS, unity_LightmapST, normalWS, backNormalWS, useRGBMLightmap, decodeInstructions, bakeDiffuseLighting, backBakeDiffuseLighting);
#else
    float3 illuminance = SampleSingleLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME), LIGHTMAP_SAMPLE_EXTRA_ARGS, unity_LightmapST, useRGBMLightmap, decodeInstructions);
    bakeDiffuseLighting += illuminance;
    backBakeDiffuseLighting += illuminance;
#endif
#endif

#ifdef DYNAMICLIGHTMAP_ON
#ifdef DIRLIGHTMAP_COMBINED
    SampleDirectionalLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap),
        TEXTURE2D_ARGS(unity_DynamicDirectionality, samplerunity_DynamicLightmap),
        uvDynamicLightmap, unity_DynamicLightmapST, normalWS, backNormalWS, false, decodeInstructions, bakeDiffuseLighting, backBakeDiffuseLighting);
#else
    float3 illuminance = SampleSingleLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap), uvDynamicLightmap, unity_DynamicLightmapST, false, decodeInstructions);
    bakeDiffuseLighting += illuminance;
    backBakeDiffuseLighting += illuminance;
#endif
#endif
}

void EvaluateLightProbeBuiltin(float3 positionRWS, float3 normalWS, float3 backNormalWS, inout float3 bakeDiffuseLighting, inout float3 backBakeDiffuseLighting)
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

        bakeDiffuseLighting += SampleSH9(SHCoefficients, normalWS);
        backBakeDiffuseLighting += SampleSH9(SHCoefficients, backNormalWS);
    }
    else
    {
        SampleProbeVolumeSH4(TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionRWS, normalWS, backNormalWS, GetProbeVolumeWorldToObject(),
            unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z, unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz, bakeDiffuseLighting, backBakeDiffuseLighting);
    }
}

// No need to initialize bakeDiffuseLighting and backBakeDiffuseLighting must be initialize outside the function
void SampleBakedGI(
    PositionInputs posInputs,
    float3 normalWS,
    float3 backNormalWS,
    uint renderingLayers,
    float2 uvStaticLightmap,
    float2 uvDynamicLightmap,
    out float3 bakeDiffuseLighting,
    out float3 backBakeDiffuseLighting)
{
    bakeDiffuseLighting = float3(0, 0, 0);
    backBakeDiffuseLighting = float3(0, 0, 0);

    // Check if we are RTGI in which case we don't want to read GI at all (We rely fully on the raytrace effect)
    // The check need to be here to work with both regular shader and shader graph
    // Note: with Probe volume it will prevent to add the UNINITIALIZED_GI tag and
    // the ProbeVolume will not be evaluate in the lightloop which is the desired behavior
    // Also this code only needs to be executed in the rasterization pipeline, otherwise it will lead to udnefined behaviors in ray tracing
#if !defined(_SURFACE_TYPE_TRANSPARENT) && (SHADERPASS != SHADERPASS_RAYTRACING_INDIRECT) && (SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER)
    if (_IndirectDiffuseMode == INDIRECTDIFFUSEMODE_RAYTRACE)
        return;
#endif

    float3 positionRWS = posInputs.positionWS;

#define SAMPLE_LIGHTMAP (defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON))
#define SAMPLE_PROBEVOLUME_BUILTIN (!SAMPLE_LIGHTMAP)

#if SAMPLE_LIGHTMAP
    EvaluateLightmap(positionRWS, normalWS, backNormalWS, uvStaticLightmap, uvDynamicLightmap, bakeDiffuseLighting, backBakeDiffuseLighting);
#endif

#if SHADEROPTIONS_ENABLE_PROBE_VOLUMES == 1
    // If probe volumes are evaluated in the lightloop, we place a sentinel value to detect that no lightmap data is present at the current pixel,
    // and we can safely overwrite baked data value with value from probe volume evaluation in light loop.
#if !SAMPLE_LIGHTMAP
    bakeDiffuseLighting = UNINITIALIZED_GI;
    return;
#endif

#elif SAMPLE_PROBEVOLUME_BUILTIN // SAMPLE_PROBEVOLUME_BUILTIN && SHADEROPTIONS_ENABLE_PROBE_VOLUMES == 0

    EvaluateLightProbeBuiltin(positionRWS, normalWS, backNormalWS, bakeDiffuseLighting, backBakeDiffuseLighting);

#endif

#undef SAMPLE_LIGHTMAP
#undef SAMPLE_PROBEVOLUME_BUILTIN
}

// Function signature exposed in a shader graph node, to keep
float3 SampleBakedGI(float3 positionRWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap)
{
    // Need PositionInputs for indexing probe volume clusters, but they are not availbile from the current SampleBakedGI() function signature.
    // Reconstruct.
    uint renderingLayers = 0;
    PositionInputs posInputs;
    ZERO_INITIALIZE(PositionInputs, posInputs);
    posInputs.positionWS = positionRWS;

    const float3 backNormalWSUnused = 0.0;
    float3 bakeDiffuseLighting;
    float3 backBakeDiffuseLightingUnused;
    SampleBakedGI(posInputs, normalWS, backNormalWSUnused, renderingLayers, uvStaticLightmap, uvDynamicLightmap, bakeDiffuseLighting, backBakeDiffuseLightingUnused);

    return bakeDiffuseLighting;
}


float4 SampleShadowMask(float3 positionRWS, float2 uvStaticLightmap) // normalWS not use for now
{
#if defined(LIGHTMAP_ON)
    float2 uv = uvStaticLightmap * unity_LightmapST.xy + unity_LightmapST.zw;
    float4 rawOcclusionMask = SAMPLE_TEXTURE2D_LIGHTMAP(SHADOWMASK_NAME, SHADOWMASK_SAMPLER_NAME, SHADOWMASK_SAMPLE_EXTRA_ARGS); // Can't reuse sampler from Lightmap because with shader graph, the compile could optimize out the lightmaps if metal is 1
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
