#ifndef __BUILTINGIUTILITIES_HLSL__
#define __BUILTINGIUTILITIES_HLSL__

// Include the IndirectDiffuseMode enum
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceGlobalIllumination.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceReflection.cs.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl"

real3 EvaluateLightProbe(real3 normalWS)
{
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

real3 EvaluateLightProbeL1(real3 normalWS)
{
    real4 SHCoefficients[3];
    SHCoefficients[0] = unity_SHAr;
    SHCoefficients[1] = unity_SHAg;
    SHCoefficients[2] = unity_SHAb;

    return SampleSH4_L1(SHCoefficients, normalWS);
}

real3 EvaluateLightProbeL0()
{
    return real3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
}

#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"
#endif

// Return camera relative probe volume world to object transformation
 // Note: Probe volume here refer to LPPV not APV
float4x4 GetProbeVolumeWorldToObject()
{
    return ApplyCameraTranslationToInverseMatrix(unity_ProbeVolumeWorldToObject);
}

void EvaluateLightmap(float3 positionRWS, float3 normalWS, float3 backNormalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap, inout float3 bakeDiffuseLighting, inout float3 backBakeDiffuseLighting)
{
#if defined(UNITY_DOTS_INSTANCING_ENABLED) && !defined(USE_LEGACY_LIGHTMAPS)
// ^ GPU-driven rendering is enabled, and we haven't opted-out from lightmap
// texture arrays. This minimizes batch breakages, but texture arrays aren't
// supported in a performant way on all GPUs.
#define LIGHTMAP_NAME unity_Lightmaps
#define LIGHTMAP_INDIRECTION_NAME unity_LightmapsInd
#define SHADOWMASK_NAME unity_ShadowMasks
#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmaps
#define SHADOWMASK_SAMPLER_NAME samplerunity_ShadowMasks
#define LIGHTMAP_SAMPLE_EXTRA_ARGS uvStaticLightmap, unity_LightmapIndex.x
#define SHADOWMASK_SAMPLE_EXTRA_ARGS uv, unity_LightmapIndex.x
#else
// ^ Lightmaps are not bound as texture arrays, but as individual textures. The
// batch is broken every time lightmaps are changed, but this is well-supported
// on all GPUs.
#define LIGHTMAP_NAME unity_Lightmap
#define LIGHTMAP_INDIRECTION_NAME unity_LightmapInd
#define SHADOWMASK_NAME unity_ShadowMask
#define LIGHTMAP_SAMPLER_NAME samplerunity_Lightmap
#define SHADOWMASK_SAMPLER_NAME samplerunity_ShadowMask
#define LIGHTMAP_SAMPLE_EXTRA_ARGS uvStaticLightmap
#define SHADOWMASK_SAMPLE_EXTRA_ARGS uv
#endif


#if defined(SHADER_STAGE_FRAGMENT) || defined(SHADER_STAGE_RAY_TRACING)

    #ifdef LIGHTMAP_ON
    {
    #ifdef DIRLIGHTMAP_COMBINED
        SampleDirectionalLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME),
            TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_INDIRECTION_NAME, LIGHTMAP_SAMPLER_NAME),
            LIGHTMAP_SAMPLE_EXTRA_ARGS, unity_LightmapST, normalWS, backNormalWS, true, bakeDiffuseLighting, backBakeDiffuseLighting);
    #else
        float3 illuminance = SampleSingleLightmap(TEXTURE2D_LIGHTMAP_ARGS(LIGHTMAP_NAME, LIGHTMAP_SAMPLER_NAME), LIGHTMAP_SAMPLE_EXTRA_ARGS, unity_LightmapST, true);
        bakeDiffuseLighting += illuminance;
        backBakeDiffuseLighting += illuminance;
    #endif
    }
    #endif

    #ifdef DYNAMICLIGHTMAP_ON
    {
    #ifdef DIRLIGHTMAP_COMBINED
        SampleDirectionalLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap),
            TEXTURE2D_ARGS(unity_DynamicDirectionality, samplerunity_DynamicLightmap),
            uvDynamicLightmap, unity_DynamicLightmapST, normalWS, backNormalWS, false, bakeDiffuseLighting, backBakeDiffuseLighting);
    #else
        float3 illuminance = SampleSingleLightmap(TEXTURE2D_ARGS(unity_DynamicLightmap, samplerunity_DynamicLightmap), uvDynamicLightmap, unity_DynamicLightmapST, false);
        bakeDiffuseLighting += illuminance;
        backBakeDiffuseLighting += illuminance;
    #endif
    }
    #endif

#endif
}

void EvaluateLightProbeBuiltin(float3 positionRWS, float3 normalWS, float3 backNormalWS, inout float3 bakeDiffuseLighting, inout float3 backBakeDiffuseLighting)
{
    if (unity_ProbeVolumeParams.x == 0.0)
    {
        bakeDiffuseLighting += EvaluateLightProbe(normalWS);
        backBakeDiffuseLighting += EvaluateLightProbe(backNormalWS);
    }
    else
    {
        // Note: Probe volume here refer to LPPV not APV
#if SHADEROPTIONS_CAMERA_RELATIVE_RENDERING == 1
        if (unity_ProbeVolumeParams.y == 0.0)
            positionRWS += _WorldSpaceCameraPos;
#endif
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
    bool needToIncludeAPV,
    out float3 bakeDiffuseLighting,
    out float3 backBakeDiffuseLighting)
{
    bakeDiffuseLighting = float3(0, 0, 0);
    backBakeDiffuseLighting = float3(0, 0, 0);

    // If we have SSGI/RTGI enabled in which case we don't want to read Lightmaps/Lightprobe at all.
    // If we have Mixed RTR or GI enabled, we need to have the lightmap exported in the case of the gbuffer as they will be consumed and will be used if the pixel is ray marched.
    // This behavior only applies to opaque Materials as Transparent one don't receive SSGI/RTGI/Mixed lighting.
    // The check need to be here to work with both regular shader and shader graph
    // Note: With Probe volume the code is skip in the lightloop if any of those effects is enabled
    // We prevent to read GI only if we are not raytrace pass that are used to fill the RTGI/Mixed buffer need to be executed normaly
#if !defined(_SURFACE_TYPE_TRANSPARENT) && (SHADERPASS != SHADERPASS_RAYTRACING_INDIRECT) && (SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER)
    if (_IndirectDiffuseMode != INDIRECTDIFFUSEMODE_OFF
#if (SHADERPASS == SHADERPASS_GBUFFER)
        && _IndirectDiffuseMode != INDIRECTDIFFUSEMODE_MIXED && _ReflectionsMode != REFLECTIONSMODE_MIXED
#endif
        )
        return;
#endif

    float3 positionRWS = posInputs.positionWS;

#if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
    EvaluateLightmap(positionRWS, normalWS, backNormalWS, uvStaticLightmap, uvDynamicLightmap, bakeDiffuseLighting, backBakeDiffuseLighting);
#elif (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    if (needToIncludeAPV)
    {
        EvaluateAdaptiveProbeVolume(GetAbsolutePositionWS(posInputs.positionWS),
            normalWS,
            backNormalWS,
            GetWorldSpaceNormalizeViewDir(posInputs.positionWS),
            posInputs.positionSS,
            renderingLayers,
            bakeDiffuseLighting,
            backBakeDiffuseLighting);
    }
#elif !(defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)) // With APV if we aren't a lightmap we do nothing. We will default to Ambient Probe in lightloop code if APV is disabled
    EvaluateLightProbeBuiltin(positionRWS, normalWS, backNormalWS, bakeDiffuseLighting, backBakeDiffuseLighting);

    // We only want to apply the ray tracing ambient probe dimmer on the ambient probe
    // and legacy light probes (and obviously only in the ray tracing shaders).
#if defined(SHADER_STAGE_RAY_TRACING)
    bakeDiffuseLighting *= _RayTracingAmbientProbeDimmer;
    backBakeDiffuseLighting *= _RayTracingAmbientProbeDimmer;
#endif
#endif
}

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
    bool needToIncludeAPV = false;
    SampleBakedGI(posInputs, normalWS, backNormalWS, renderingLayers, uvStaticLightmap, uvDynamicLightmap, needToIncludeAPV, bakeDiffuseLighting, backBakeDiffuseLighting);
}

float3 SampleBakedGI(float3 positionRWS, float3 normalWS, uint2 positionSS, float2 uvStaticLightmap, float2 uvDynamicLightmap, bool needToIncludeAPV = false)
{
    // Need PositionInputs for indexing probe volume clusters, but they are not available from the current SampleBakedGI() function signature.
    // Reconstruct.
    uint renderingLayers = 0;
    PositionInputs posInputs;
    ZERO_INITIALIZE(PositionInputs, posInputs);
    posInputs.positionWS = positionRWS;
    posInputs.positionSS = positionSS;

    const float3 backNormalWSUnused = 0.0;
    float3 bakeDiffuseLighting;
    float3 backBakeDiffuseLightingUnused;
    SampleBakedGI(posInputs, normalWS, backNormalWSUnused, renderingLayers, uvStaticLightmap, uvDynamicLightmap, needToIncludeAPV, bakeDiffuseLighting, backBakeDiffuseLightingUnused);

    return bakeDiffuseLighting;
}


float4 SampleShadowMask(float3 positionRWS, float2 uvStaticLightmap) // normalWS not use for now
{
#if defined(LIGHTMAP_ON)
    float2 uv = uvStaticLightmap * unity_LightmapST.xy + unity_LightmapST.zw;
    return SAMPLE_TEXTURE2D_LIGHTMAP(SHADOWMASK_NAME, SHADOWMASK_SAMPLER_NAME, SHADOWMASK_SAMPLE_EXTRA_ARGS); // Can't reuse sampler from Lightmap because with shader graph, the compile could optimize out the lightmaps if metal is 1
#elif (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    return 1;
#else
    float4 rawOcclusionMask;
    if (unity_ProbeVolumeParams.x == 1.0)
    {
#if SHADEROPTIONS_CAMERA_RELATIVE_RENDERING == 1
        if (unity_ProbeVolumeParams.y == 0.0)
            positionRWS += _WorldSpaceCameraPos;
#endif
        rawOcclusionMask = SampleProbeOcclusion(TEXTURE3D_ARGS(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionRWS, GetProbeVolumeWorldToObject(),
            unity_ProbeVolumeParams.y, unity_ProbeVolumeParams.z, unity_ProbeVolumeMin.xyz, unity_ProbeVolumeSizeInv.xyz);
    }
    else
    {
        // Note: Default value when the feature is not enabled is float(1.0, 1.0, 1.0, 1.0) in C++
        rawOcclusionMask = unity_ProbesOcclusion;
    }

    return rawOcclusionMask;
#endif
}

#endif
