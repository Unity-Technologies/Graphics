#ifndef UNIVERSAL_GLOBAL_ILLUMINATION_INCLUDED
#define UNIVERSAL_GLOBAL_ILLUMINATION_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lightmaps.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SampleScreenSpaceReflection.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareExposureTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDFData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.deprecated.hlsl"

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/AmbientProbe.hlsl"

#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"
#endif
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

#if defined(_SCREEN_SPACE_IRRADIANCE)
TEXTURE2D_X(_ScreenSpaceIrradiance);

URP_LIGHT_ACCUM3 SampleScreenSpaceGI(float2 pos, float invPreExposureMultiplier)
{
    // The irradiance texture is stored pre-exposed so its fp32 values fit fp16. Undo that here, as lighting inputs are un-exposed.
    URP_LIGHT_ACCUM3 irradiance = LOAD_TEXTURE2D_X(_ScreenSpaceIrradiance, pos).rgb * invPreExposureMultiplier;
#ifdef UNITY_COLORSPACE_GAMMA
    irradiance = LinearToSRGB(irradiance);
#endif
    return irradiance;
}
#endif

// If lightmap is not defined than we evaluate GI (ambient + probes) from SH

// Legacy: _MIXED_LIGHTING_SUBTRACTIVE was an alias for (LIGHTMAP_SHADOW_MIXING && !SHADOWS_SHADOWMASK).
// Derived here so shaders that still read it keep working; the reverse derive lives in Shadows.hlsl.
#if !defined(LIGHTMAP_SHADOW_MIXING_KEYWORD_DECLARED) && !defined(SHADOWS_SHADOWMASK_KEYWORD_DECLARED)
    #if !defined(_MIXED_LIGHTING_SUBTRACTIVE) && !defined(SHADOWS_SHADOWMASK) && DEFINED_NONZERO(LIGHTMAP_SHADOW_MIXING)
        #define _MIXED_LIGHTING_SUBTRACTIVE 1
    #endif
#endif

#if !defined(_REFLECTION_PROBE_BLENDING_KEYWORD_DECLARED)
    #if !defined(_REFLECTION_PROBE_BLENDING)
        static const bool _REFLECTION_PROBE_BLENDING = 0;
    #elif DEFINED_NONZERO(_REFLECTION_PROBE_BLENDING)
        #undef _REFLECTION_PROBE_BLENDING
        #define _REFLECTION_PROBE_BLENDING 1
    #endif
#endif

#if !defined(_REFLECTION_PROBE_BOX_PROJECTION_KEYWORD_DECLARED)
    #if !defined(_REFLECTION_PROBE_BOX_PROJECTION)
        static const bool _REFLECTION_PROBE_BOX_PROJECTION = 0;
    #elif DEFINED_NONZERO(_REFLECTION_PROBE_BOX_PROJECTION)
        #undef _REFLECTION_PROBE_BOX_PROJECTION
        #define _REFLECTION_PROBE_BOX_PROJECTION 1
    #endif
#endif

#if !defined(EVALUATE_SH_VERTEX_KEYWORD_DECLARED)
    #if !defined(EVALUATE_SH_VERTEX)
        static const bool EVALUATE_SH_VERTEX = 0;
    #elif DEFINED_NONZERO(EVALUATE_SH_VERTEX)
        #undef EVALUATE_SH_VERTEX
        #define EVALUATE_SH_VERTEX 1
    #endif
#endif

#if !defined(EVALUATE_SH_MIXED_KEYWORD_DECLARED)
    #if !defined(EVALUATE_SH_MIXED)
        static const bool EVALUATE_SH_MIXED = 0;
    #elif DEFINED_NONZERO(EVALUATE_SH_MIXED)
        #undef EVALUATE_SH_MIXED
        #define EVALUATE_SH_MIXED 1
    #endif
#endif

#if !defined(REFLECTION_PROBE_ROTATION_KEYWORD_DECLARED)
    #if !defined(REFLECTION_PROBE_ROTATION)
        static const bool REFLECTION_PROBE_ROTATION = 0;
    #elif DEFINED_NONZERO(REFLECTION_PROBE_ROTATION)
        #undef REFLECTION_PROBE_ROTATION
        #define REFLECTION_PROBE_ROTATION 1
    #endif
#endif

// SH Vertex Evaluation. Depending on target SH sampling might be
// done completely per vertex or mixed with L2 term per vertex and L0, L1
// per pixel. See SampleSHPixel
half3 SampleSHVertex(half3 normalWS)
{
    // Fully per-pixel: nothing to compute.
    half3 result = half3(0.0, 0.0, 0.0);

    if (EVALUATE_SH_VERTEX)
        result = EvaluateAmbientProbeSRGB(normalWS);
    else if (EVALUATE_SH_MIXED) // no max since this is only L2 contribution
        result = SHEvalLinearL2(normalWS, unity_SHBr, unity_SHBg, unity_SHBb, unity_SHC);

    return result;
}

// SH Pixel Evaluation. Depending on target SH sampling might be done
// mixed or fully in pixel. See SampleSHVertex
half3 SampleSHPixel(half3 L2Term, half3 normalWS)
{
    half3 result = half3(0.0, 0.0, 0.0);
    if (EVALUATE_SH_VERTEX)
    {
        result = L2Term;
    }
    else if (EVALUATE_SH_MIXED)
    {
        half3 res = L2Term + SHEvalLinearL0L1(normalWS, unity_SHAr, unity_SHAg, unity_SHAb);
        #ifdef UNITY_COLORSPACE_GAMMA
        res = LinearToSRGB(res);
        #endif
        result = max(half3(0, 0, 0), res);
    }
    else
    {
        // Default: Evaluate SH fully per-pixel
        result = EvaluateAmbientProbeSRGB(normalWS);
    }
    return result;
}

// APV Prove volume
// Vertex and Mixed both use Vertex sampling

// APV reads its data from StructuredBuffers (SSBOs). On GLES, vertex-stage SSBO support is device-dependent
// (GL_MAX_VERTEX_SHADER_STORAGE_BLOCKS may be 0), so sampling APV in the vertex shader can fail to link.
// Where unsupported, APV is evaluated per-pixel instead (see SampleProbeVolumePixel).
#if defined(SHADER_API_GLES3)
    #define APV_VERTEX_SAMPLING_SUPPORTED 0
#else
    #define APV_VERTEX_SAMPLING_SUPPORTED 1
#endif

half3 SampleProbeVolumeVertex(in float3 absolutePositionWS, in float3 normalWS, in float3 viewDir, out float4 probeOcclusion)
{
    probeOcclusion = 1.0;
    half3 result = half3(0, 0, 0);

#if (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)) && APV_VERTEX_SAMPLING_SUPPORTED

    if (EVALUATE_SH_VERTEX || EVALUATE_SH_MIXED)
    {
        half3 bakedGI;

        if (_EnableProbeVolumes && IsLightProbeSamplingEnabled())
        {
            EvaluateAdaptiveProbeVolume(absolutePositionWS, normalWS, viewDir, GetMeshRenderingLayer(), bakedGI, probeOcclusion);
        }
        else
        {
            bakedGI = EvaluateAmbientProbe(normalWS);
        }

        #ifdef UNITY_COLORSPACE_GAMMA
        bakedGI = LinearToSRGB(bakedGI);
        #endif

        result = bakedGI;
    }

#endif

    return result;
}

half3 SampleProbeVolumePixel(in half3 vertexValue, in float3 absolutePositionWS, in float3 normalWS, in float3 viewDir, in float2 positionSS, in float4 vertexProbeOcclusion, out float4 probeOcclusion)
{
    probeOcclusion = 1.0;

#if APV_VERTEX_SAMPLING_SUPPORTED
    if (EVALUATE_SH_VERTEX || EVALUATE_SH_MIXED)
    {
        probeOcclusion = vertexProbeOcclusion;
        return vertexValue;
    }
#endif

#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    half3 bakedGI;
    // APV per-renderer. When MeshRenderer.lightProbeUsage == Off, fall back to the ambient sky probe (matches LightProbeGroup behavior).
    if (_EnableProbeVolumes && IsLightProbeSamplingEnabled())
    {
        EvaluateAdaptiveProbeVolume(absolutePositionWS, normalWS, viewDir, positionSS, GetMeshRenderingLayer(), bakedGI, probeOcclusion);
    }
    else
    {
        bakedGI = EvaluateAmbientProbe(normalWS);
    }
#ifdef UNITY_COLORSPACE_GAMMA
    bakedGI = LinearToSRGB(bakedGI);
#endif
    return bakedGI;
#else
    return half3(0, 0, 0);
#endif
}

half3 SampleProbeVolumePixel(in half3 vertexValue, in float3 absolutePositionWS, in float3 normalWS, in float3 viewDir, in float2 positionSS)
{
    float4 unusedProbeOcclusion = 0;
    return SampleProbeVolumePixel(vertexValue, absolutePositionWS, normalWS, viewDir, positionSS, unusedProbeOcclusion, unusedProbeOcclusion);
}

half3 SampleProbeSHVertex(in float3 absolutePositionWS, in float3 normalWS, in float3 viewDir, out float4 probeOcclusion)
{
    probeOcclusion = 1.0;

#if (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    return SampleProbeVolumeVertex(absolutePositionWS, normalWS, viewDir, probeOcclusion);
#else
    return SampleSHVertex(normalWS);
#endif
}

half3 SampleProbeSHVertex(in float3 absolutePositionWS, in float3 normalWS, in float3 viewDir)
{
    float4 unusedProbeOcclusion = 0;
    return SampleProbeSHVertex(absolutePositionWS, normalWS, viewDir, unusedProbeOcclusion);
}

half3 SampleLightmapOrSH(float2 staticLightmapUV, float2 dynamicLightmapUV, half3 vertexSH, half3 normalWS)
{
    if (LightmapAvailable() || DynamicLightmapAvailable())
        return SampleLightmap(staticLightmapUV, dynamicLightmapUV, normalWS);
    return SampleSHPixel(vertexSH, normalWS);
}

// SAMPLE_GI exists for backward compatibility with custom shaders and ShaderGraph;
// pipeline passes call InitializeBakedGI instead.
// The shName argument may reference a struct field that only exists when the SH interpolator is
// live, so the lightmap arms may only expand it under USE_VERTEX_SH_INTERPOLATOR. That is exactly
// the runtime-branching case, where the SH fallback is needed because lightmap availability is a
// runtime property; specialized variants keep the direct SampleLightmap expansion.
#if defined(_SCREEN_SPACE_IRRADIANCE)
    // Transparent surfaces are not in the surface cache; they fall back to the ambient probe.
    #define SAMPLE_GI(irradianceTex, pos, normal, invPreExposureMultiplier) (IsSurfaceTypeTransparent() ? URP_LIGHT_ACCUM3(EvaluateAmbientProbe(normal)) : SampleScreenSpaceGI(pos, invPreExposureMultiplier))
#elif USE_LIGHTMAP_UV_INTERPOLATOR && USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR
    #if USE_VERTEX_SH_INTERPOLATOR
        #define SAMPLE_GI(staticLmName, dynamicLmName, shName, normalWSName) SampleLightmapOrSH(staticLmName, dynamicLmName, shName, normalWSName)
    #else
        #define SAMPLE_GI(staticLmName, dynamicLmName, shName, normalWSName) SampleLightmap(staticLmName, dynamicLmName, normalWSName)
    #endif
#elif USE_DYNAMICLIGHTMAP_UV_INTERPOLATOR
    #if USE_VERTEX_SH_INTERPOLATOR
        #define SAMPLE_GI(staticLmName, dynamicLmName, shName, normalWSName) SampleLightmapOrSH(0, dynamicLmName, shName, normalWSName)
    #else
        #define SAMPLE_GI(staticLmName, dynamicLmName, shName, normalWSName) SampleLightmap(0, dynamicLmName, normalWSName)
    #endif
#elif USE_LIGHTMAP_UV_INTERPOLATOR
    #if USE_VERTEX_SH_INTERPOLATOR
        #define SAMPLE_GI(staticLmName, shName, normalWSName) SampleLightmapOrSH(staticLmName, 0, shName, normalWSName)
    #else
        #define SAMPLE_GI(staticLmName, shName, normalWSName) SampleLightmap(staticLmName, 0, normalWSName)
    #endif
#elif defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    #ifdef USE_APV_PROBE_OCCLUSION
        #define SAMPLE_GI(shName, absolutePositionWS, normalWS, viewDir, positionSS, vertexProbeOcclusion, probeOcclusion) SampleProbeVolumePixel(shName, absolutePositionWS, normalWS, viewDir, positionSS, vertexProbeOcclusion, probeOcclusion)
    #else
        #define SAMPLE_GI(shName, absolutePositionWS, normalWS, viewDir, positionSS, vertexProbeOcclusion, probeOcclusion) SampleProbeVolumePixel(shName, absolutePositionWS, normalWS, viewDir, positionSS)
    #endif
#else
#define SAMPLE_GI(staticLmName, shName, normalWSName) SampleSHPixel(shName, normalWSName)
#endif

// Inputs for InitializeBakedGI. Zero-initialize with (GIParams)0 and fill the fields the pass
// has data for; fields left zero select the corresponding neutral behavior.
struct GIParams
{
    float2 staticLightmapUV;
    float2 dynamicLightmapUV;
    half3 vertexSH;
    float3 positionWS;
    half3 normalWS;
    float3 viewDirWS;
    float2 positionSS;
    float4 vertexProbeOcclusion;
    bool isSurfaceTypeTransparent;
};

// Samples baked GI (lightmaps, probe volumes, SH, or screen-space irradiance, depending on the
// active configuration) and the shadow mask for a surface point.
void InitializeBakedGI(GIParams giParams, out URP_LIGHT_ACCUM3 bakedGI, out half4 shadowMask)
{
    bakedGI = 0.0;
    shadowMask = half4(1, 1, 1, 1);

#if defined(_SCREEN_SPACE_IRRADIANCE)
    if (!giParams.isSurfaceTypeTransparent)
    {
        bakedGI = SampleScreenSpaceGI(giParams.positionSS, GetInvPreExposureMultiplier());
    }
    else
    {
        bakedGI = EvaluateAmbientProbe(giParams.normalWS);
    }
#else
    if (LightmapAvailable() || DynamicLightmapAvailable())
    {
        bakedGI = SampleLightmap(giParams.staticLightmapUV, giParams.dynamicLightmapUV, giParams.normalWS);
        shadowMask = SampleShadowMask(giParams.staticLightmapUV);
    }
    #if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    else
    {
        #ifdef USE_APV_PROBE_OCCLUSION
        bakedGI = SampleProbeVolumePixel(giParams.vertexSH, GetAbsolutePositionWS(giParams.positionWS), giParams.normalWS, giParams.viewDirWS, giParams.positionSS, giParams.vertexProbeOcclusion, shadowMask);
        #else
        bakedGI = SampleProbeVolumePixel(giParams.vertexSH, GetAbsolutePositionWS(giParams.positionWS), giParams.normalWS, giParams.viewDirWS, giParams.positionSS);
        #endif
    }
    #else
    else
    {
        bakedGI = SampleSHPixel(giParams.vertexSH, giParams.normalWS);
        shadowMask = SampleShadowMask(giParams.staticLightmapUV);
    }
    #endif
#endif
}

float3 GetReflectionProbeCenter(float4 boxMin, float4 boxMax)
{
    return boxMin.xyz + (boxMax.xyz - boxMin.xyz) / 2;
}

float3 GetRotatedPoint(float3 centerPosition, float4 quaternion, float3 pointToRotate)
{
    return RotateVectorByQuat(quaternion, pointToRotate - centerPosition) + centerPosition;
}

half3 BoxProjectedCubemapDirection(half3 reflectionWS, float3 positionWS, float4 cubemapPositionWS, float4 boxMin, float4 boxMax)
{
    // Is this probe using box projection?
    if (cubemapPositionWS.w > 0.0f)
    {
        float3 boxMinMax = (reflectionWS > 0.0f) ? boxMax.xyz : boxMin.xyz;
        half3 rbMinMax = half3(boxMinMax - positionWS) / reflectionWS;

        half fa = half(min(min(rbMinMax.x, rbMinMax.y), rbMinMax.z));

        half3 worldPos = half3(positionWS - cubemapPositionWS.xyz);

        half3 result = worldPos + reflectionWS * fa;
        return result;
    }
    else
    {
        return reflectionWS;
    }
}

half3 BoxProjectedCubemapDirection(float4 rotation, half3 reflectionWS, float3 positionWS, float4 cubemapPositionWS, float4 boxMin, float4 boxMax)
{
    half3 rotReflectVector = RotateVectorByQuat(rotation, reflectionWS);
    float4 inverseRotation = -rotation;
    inverseRotation.w = -inverseRotation.w;

    half3 dir = BoxProjectedCubemapDirection(rotReflectVector, positionWS, cubemapPositionWS, boxMin, boxMax);

    half3 rotatedDir = RotateVectorByQuat(inverseRotation, dir);

    return rotatedDir;

}

float CalculateProbeWeight(float3 positionWS, float4 probeBoxMin, float4 probeBoxMax)
{
    float blendDistance = probeBoxMax.w;
    float3 weightDir = min(positionWS - probeBoxMin.xyz, probeBoxMax.xyz - positionWS) / blendDistance;
    return saturate(min(weightDir.x, min(weightDir.y, weightDir.z)));
}

half CalculateProbeVolumeSqrMagnitude(float4 probeBoxMin, float4 probeBoxMax)
{
    half3 maxToMin = half3(probeBoxMax.xyz - probeBoxMin.xyz);
    return dot(maxToMin, maxToMin);
}

half3 CalculateIrradianceFromReflectionProbes(half3 reflectVector, float3 positionWS, half perceptualRoughness, float2 normalizedScreenSpaceUV)
{
    half3 irradiance = half3(0.0h, 0.0h, 0.0h);
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);

#if USE_CLUSTER_LIGHT_LOOP && CLUSTER_HAS_REFLECTION_PROBES

    float totalWeight = 0.0f;
    uint probeIndex;
    float3 rotPosWS = positionWS;
    ClusterIterator it = ClusterInit(normalizedScreenSpaceUV, positionWS, 1);
    [loop] while (ClusterNext(it, probeIndex) && totalWeight < 0.99f)
    {
        probeIndex -= URP_FP_PROBES_BEGIN;

        float4 probeRotation = urp_ReflProbes_Rotation[probeIndex];
        float4 probePosition = urp_ReflProbes_ProbePosition[probeIndex];
        float4 probeBoxMin = urp_ReflProbes_BoxMin[probeIndex];
        float4 probeBoxMax = urp_ReflProbes_BoxMax[probeIndex];

        if (REFLECTION_PROBE_ROTATION)
        {
            // We need to rotate positionWS such that we can assume the influence volumes to be axis aligned
            // when calculating the weight and box projection.
            float3 probeCenterPosWS = GetReflectionProbeCenter(probeBoxMin, probeBoxMax);
            rotPosWS = GetRotatedPoint(probeCenterPosWS, probeRotation, positionWS);
        }

        float weight = CalculateProbeWeight(rotPosWS, probeBoxMin, probeBoxMax);
        weight = min(weight, 1.0f - totalWeight);

        half3 sampleVector = reflectVector;
        if (_REFLECTION_PROBE_BOX_PROJECTION)
        {
            if (REFLECTION_PROBE_ROTATION)
                sampleVector = BoxProjectedCubemapDirection(probeRotation, reflectVector, rotPosWS, probePosition, probeBoxMin, probeBoxMax);
            else
                sampleVector = BoxProjectedCubemapDirection(reflectVector, rotPosWS, probePosition, probeBoxMin, probeBoxMax);
        }

        uint maxMip = (uint)abs(probePosition.w) - 1;
        half probeMip = min(mip, maxMip);
        float2 uv = saturate(PackNormalOctQuadEncode(sampleVector) * 0.5 + 0.5);

        float mip0 = floor(probeMip);
        float mip1 = mip0 + 1;
        float mipBlend = probeMip - mip0;
        float4 scaleOffset0 = urp_ReflProbes_MipScaleOffset[probeIndex * 7 + (uint)mip0];
        float4 scaleOffset1 = urp_ReflProbes_MipScaleOffset[probeIndex * 7 + (uint)mip1];

        float preExposure = urp_ReflProbes_ExposureMultipliers[probeIndex].x;
        half3 irradiance0 = half4(SAMPLE_TEXTURE2D_LOD(urp_ReflProbes_Atlas, sampler_LinearClamp, uv * scaleOffset0.xy + scaleOffset0.zw, 0.0)).rgb;
        half3 irradiance1 = half4(SAMPLE_TEXTURE2D_LOD(urp_ReflProbes_Atlas, sampler_LinearClamp, uv * scaleOffset1.xy + scaleOffset1.zw, 0.0)).rgb;
        irradiance += preExposure * weight * lerp(irradiance0, irradiance1, mipBlend);
        totalWeight += weight;
    }

#else

    float4 boxMin0 = unity_SpecCube0_BoxMin;
    float4 boxMax0 = unity_SpecCube0_BoxMax;
    float4 boxMin1 = unity_SpecCube1_BoxMin;
    float4 boxMax1 = unity_SpecCube1_BoxMax;

    float3 rotPosWS0 = positionWS;
    float3 rotPosWS1 = positionWS;

    if (REFLECTION_PROBE_ROTATION)
    {
        // We need to rotate positionWS such that we can assume the influence volumes to be axis aligned
        // when calculating the weight and box projection.
        float3 probeCenterPosWS0 = GetReflectionProbeCenter(boxMin0, boxMax0);
        rotPosWS0 = GetRotatedPoint(probeCenterPosWS0, unity_SpecCube0_Rotation, positionWS);
        float3 probeCenterPosWS1 = GetReflectionProbeCenter(boxMin1, boxMax1);
        rotPosWS1 = GetRotatedPoint(probeCenterPosWS1, unity_SpecCube1_Rotation, positionWS);
    }

    half probe0Volume = CalculateProbeVolumeSqrMagnitude(boxMin0, boxMax0);
    half probe1Volume = CalculateProbeVolumeSqrMagnitude(boxMin1, boxMax1);

    half volumeDiff = probe0Volume - probe1Volume;
    float importanceSign = boxMin1.w;

    // A probe is dominant if its importance is higher
    // Or have equal importance but smaller volume
    bool probe0Dominant = importanceSign > 0.0f || (importanceSign == 0.0f && volumeDiff < -0.0001h);
    bool probe1Dominant = importanceSign < 0.0f || (importanceSign == 0.0f && volumeDiff > 0.0001h);

    float desiredWeightProbe0 = CalculateProbeWeight(rotPosWS0, boxMin0, boxMax0);
    float desiredWeightProbe1 = CalculateProbeWeight(rotPosWS1, boxMin1, boxMax1);

    // Subject the probes weight if the other probe is dominant
    float weightProbe0 = probe1Dominant ? min(desiredWeightProbe0, 1.0f - desiredWeightProbe1) : desiredWeightProbe0;
    float weightProbe1 = probe0Dominant ? min(desiredWeightProbe1, 1.0f - desiredWeightProbe0) : desiredWeightProbe1;

    float totalWeight = weightProbe0 + weightProbe1;

    // If either probe 0 or probe 1 is dominant the sum of weights is guaranteed to be 1.
    // If neither is dominant this is not guaranteed - only normalize weights if totalweight exceeds 1.
    weightProbe0 /= max(totalWeight, 1.0f);
    weightProbe1 /= max(totalWeight, 1.0f);

    // Sample the first reflection probe
    if (weightProbe0 > 0.01f)
    {
        half3 reflectVector0 = reflectVector;
        if (_REFLECTION_PROBE_BOX_PROJECTION)
        {
            if (REFLECTION_PROBE_ROTATION)
                reflectVector0 = BoxProjectedCubemapDirection(unity_SpecCube0_Rotation, reflectVector, rotPosWS0, unity_SpecCube0_ProbePosition, boxMin0, boxMax0);
            else
                reflectVector0 = BoxProjectedCubemapDirection(reflectVector, rotPosWS0, unity_SpecCube0_ProbePosition, boxMin0, boxMax0);
        }

        half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector0, mip));

        irradiance += weightProbe0 * SelectProbeExposureMultiplier(unity_SpecCube0_Exposure.x) * DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
    }

    // Sample the second reflection probe
    if (weightProbe1 > 0.01f)
    {
        half3 reflectVector1 = reflectVector;
        if (_REFLECTION_PROBE_BOX_PROJECTION)
        {
            if (REFLECTION_PROBE_ROTATION)
                reflectVector1 = BoxProjectedCubemapDirection(unity_SpecCube1_Rotation, reflectVector, rotPosWS1, unity_SpecCube1_ProbePosition, boxMin1, boxMax1);
            else
                reflectVector1 = BoxProjectedCubemapDirection(reflectVector, rotPosWS1, unity_SpecCube1_ProbePosition, boxMin1, boxMax1);
        }
        half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube1, samplerunity_SpecCube1, reflectVector1, mip));

        irradiance += weightProbe1 * SelectProbeExposureMultiplier(unity_SpecCube1_Exposure.x) * DecodeHDREnvironment(encodedIrradiance, unity_SpecCube1_HDR);
    }
#endif

    // Use any remaining weight to blend to environment reflection cube map
    if (totalWeight < 0.99f)
    {
        half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(_GlossyEnvironmentCubeMap, sampler_GlossyEnvironmentCubeMap, reflectVector, mip));

        irradiance += (1.0f - totalWeight) * DecodeHDREnvironment(encodedIrradiance, _GlossyEnvironmentCubeMap_HDR);
    }

    return irradiance;
}

half3 GlossyEnvironmentReflection(half3 reflectVector, float3 positionWS, half perceptualRoughness, half occlusion, float2 normalizedScreenSpaceUV, bool useEnvironmentReflections)
{
    half3 irradiance = half3(0, 0, 0);

    #if defined(_SCREENSPACEREFLECTIONS_OFF)
    half4 ssrColor = 0;
    #else
    half4 ssrColor = GetScreenSpaceReflection(normalizedScreenSpaceUV, positionWS, perceptualRoughness);
    // The SSR texture is resolved from the pre-exposed camera color; undo that, as lighting inputs are un-exposed.
    ssrColor.rgb *= GetInvPreExposureMultiplier();
    #endif

// Skip the reflection-probe work when SSR fully covers the pixel (the lerp below discards it).
#if !defined(STEREO_INSTANCING_ON) || defined(UNITY_COMPILER_DXC)
    if (ssrColor.a < 1.0)
#endif
    {
        if (!useEnvironmentReflections)
        {
            irradiance = _GlossyEnvironmentColor.rgb;
        }
        else if (_REFLECTION_PROBE_BLENDING)
        {
            irradiance = CalculateIrradianceFromReflectionProbes(reflectVector, positionWS, perceptualRoughness, normalizedScreenSpaceUV);
        }
        else
        {
            if (_REFLECTION_PROBE_BOX_PROJECTION)
            {
                if (REFLECTION_PROBE_ROTATION)
                {
                    float3 probeCenterPosWS0 = unity_SpecCube0_BoxMin.xyz + (unity_SpecCube0_BoxMax.xyz - unity_SpecCube0_BoxMin.xyz) / 2;
                    float3 rotPosWS0 = RotateVectorByQuat(unity_SpecCube0_Rotation, positionWS - probeCenterPosWS0) + probeCenterPosWS0;
                    half3 rotReflectVector0 = RotateVectorByQuat(unity_SpecCube0_Rotation, reflectVector);
                    float4 inverseRotation0 = -unity_SpecCube0_Rotation;
                    inverseRotation0.w = -inverseRotation0.w;
                    reflectVector = BoxProjectedCubemapDirection(rotReflectVector0, rotPosWS0, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
                    reflectVector = RotateVectorByQuat(inverseRotation0, reflectVector);
                }
                else
                {
                    reflectVector = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
                }
            }

            half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
            half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip));

            irradiance = SelectProbeExposureMultiplier(unity_SpecCube0_Exposure.x) * DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
        }
    }

    irradiance = lerp(irradiance.rgb, ssrColor.rgb, ssrColor.a);

    return irradiance * occlusion;
}

#if !USE_CLUSTER_LIGHT_LOOP
half3 GlossyEnvironmentReflection(half3 reflectVector, float3 positionWS, half perceptualRoughness, half occlusion, bool useEnvironmentReflections)
{
    return GlossyEnvironmentReflection(reflectVector, positionWS, perceptualRoughness, occlusion, float2(0.0f, 0.0f), useEnvironmentReflections);
}
#endif

half3 GlossyEnvironmentReflection(half3 reflectVector, half perceptualRoughness, half occlusion, bool useEnvironmentReflections)
{
    half3 irradiance = _GlossyEnvironmentColor.rgb;

    if (useEnvironmentReflections)
    {
        half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
        half4 encodedIrradiance = half4(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip));

        irradiance = SelectProbeExposureMultiplier(unity_SpecCube0_Exposure.x) * DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
    }

    return irradiance * occlusion;
}

URP_LIGHT_ACCUM3 SubtractDirectMainLightFromLightmap(Light mainLight, half3 normalWS, URP_LIGHT_ACCUM3 bakedGI)
{
    // Let's try to make realtime shadows work on a surface, which already contains
    // baked lighting and shadowing from the main sun light.
    // Summary:
    // 1) Calculate possible value in the shadow by subtracting estimated light contribution from the places occluded by realtime shadow:
    //      a) preserves other baked lights and light bounces
    //      b) eliminates shadows on the geometry facing away from the light
    // 2) Clamp against user defined ShadowColor.
    // 3) Pick original lightmap value, if it is the darkest one.


    // 1) Gives good estimate of illumination as if light would've been shadowed during the bake.
    // We only subtract the main direction light. This is accounted in the contribution term below.
    half shadowStrength = GetMainLightShadowStrength();
    half contributionTerm = saturate(dot(mainLight.direction, normalWS));
    half3 lambert = mainLight.color * contributionTerm;
    half3 estimatedLightContributionMaskedByInverseOfShadow = lambert * (1.0 - mainLight.shadowAttenuation);
    URP_LIGHT_ACCUM3 subtractedLightmap = bakedGI - estimatedLightContributionMaskedByInverseOfShadow;

    // 2) Allows user to define overall ambient of the scene and control situation when realtime shadow becomes too dark.
    URP_LIGHT_ACCUM3 realtimeShadow = max(subtractedLightmap, _SubtractiveShadowColor.xyz);
    realtimeShadow = lerp(bakedGI, realtimeShadow, shadowStrength);

    // 3) Pick darkest color
    return min(bakedGI, realtimeShadow);
}

URP_LIGHT_ACCUM3 GlobalIllumination(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    URP_LIGHT_ACCUM3 bakedGI, half occlusion, float3 positionWS,
    half3 normalWS, half3 viewDirectionWS, float2 normalizedScreenSpaceUV, bool useClearCoat, bool useEnvironmentReflections)
{
// Prevent calling 'reflect' from causing NdotV to be computed twice on Adreno GPUs
#if (UNITY_PLATFORM_META_QUEST) // This is platform specific change targeting performance only
    half NdotV = dot(normalWS, viewDirectionWS);
    half NdotV2 = NdotV + NdotV;    // 2.0h * normalWS * NdotV was resulting in promotion to 32 bit precision
    half3 reflectVector = normalWS * NdotV2 - viewDirectionWS;    // reflect(i,n) = i - 2 * n * dot(i n)
                                                                  // reflect(-viewDirection, normalWS) = -viewDirection - 2 * normalWS * dot(-viewDirection, normalWS)
                                                                  //                                   = -viewDirection + 2 * normalWS * dot(viewDirection, normalWS)
                                                                  //                                   = 2 * normalWS * dot(viewDirection, normalWS) - viewDirection
    half NoV = saturate(NdotV);
#else
    half3 reflectVector = reflect(-viewDirectionWS, normalWS);
    half NoV = saturate(dot(normalWS, viewDirectionWS));
#endif
    half fresnelTerm = Pow4(1.0 - NoV);

    URP_LIGHT_ACCUM3 indirectDiffuse = bakedGI;
    half3 indirectSpecular = GlossyEnvironmentReflection(reflectVector, positionWS, brdfData.perceptualRoughness, 1.0h, normalizedScreenSpaceUV, useEnvironmentReflections);

    URP_LIGHT_ACCUM3 color = EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);

    if (IsOnlyAOLightingFeatureEnabled())
    {
        color = half3(1,1,1); // "Base white" for AO debug lighting mode
    }

    if (useClearCoat)
    {
        half3 coatIndirectSpecular = GlossyEnvironmentReflection(reflectVector, positionWS, brdfDataClearCoat.perceptualRoughness, 1.0h, normalizedScreenSpaceUV, useEnvironmentReflections);
        // TODO: "grazing term" causes problems on full roughness
        half3 coatColor = EnvironmentBRDFClearCoat(brdfDataClearCoat, clearCoatMask, coatIndirectSpecular, fresnelTerm);

        // Blend with base layer using khronos glTF recommended way using NoV
        // Smooth surface & "ambiguous" lighting
        // NOTE: fresnelTerm (above) is pow4 instead of pow5, but should be ok as blend weight.
        half coatFresnel = kDielectricSpec.x + kDielectricSpec.a * fresnelTerm;
        color = color * (1.0 - coatFresnel * clearCoatMask) + coatColor;
    }
    return color * occlusion;
}

URP_LIGHT_ACCUM3 GlobalIllumination(BRDFData brdfData, BRDFData brdfDataClearCoat, float clearCoatMask,
    URP_LIGHT_ACCUM3 bakedGI, half occlusion,
    half3 normalWS, half3 viewDirectionWS, bool useClearCoat, bool useEnvironmentReflections)
{
// Prevent calling 'reflect' from causing NdotV to be computed twice on Adreno GPUs
#if (UNITY_PLATFORM_META_QUEST) // This is platform specific change targeting performance only
    half NdotV = dot(normalWS, viewDirectionWS);
    half NdotV2 = NdotV + NdotV;    // 2.0h * normalWS * NdotV was resulting in promotion to 32 bit precision
    half3 reflectVector = normalWS * NdotV2 - viewDirectionWS;    // reflect(i,n) = i - 2 * n * dot(i n)
                                                                  // reflect(-viewDirection, normalWS) = -viewDirection - 2 * normalWS * dot(-viewDirection, normalWS)
                                                                  //                                   = -viewDirection + 2 * normalWS * dot(viewDirection, normalWS)
                                                                  //                                   = 2 * normalWS * dot(viewDirection, normalWS) - viewDirection
    half NoV = saturate(NdotV);
#else
    half3 reflectVector = reflect(-viewDirectionWS, normalWS);
    half NoV = saturate(dot(normalWS, viewDirectionWS));
#endif
    half fresnelTerm = Pow4(1.0 - NoV);

    URP_LIGHT_ACCUM3 indirectDiffuse = bakedGI;
    half3 indirectSpecular = GlossyEnvironmentReflection(reflectVector, brdfData.perceptualRoughness, half(1.0), useEnvironmentReflections);

    URP_LIGHT_ACCUM3 color = EnvironmentBRDF(brdfData, indirectDiffuse, indirectSpecular, fresnelTerm);

    if (useClearCoat)
    {
        half3 coatIndirectSpecular = GlossyEnvironmentReflection(reflectVector, brdfDataClearCoat.perceptualRoughness, half(1.0), useEnvironmentReflections);
        // TODO: "grazing term" causes problems on full roughness
        half3 coatColor = EnvironmentBRDFClearCoat(brdfDataClearCoat, clearCoatMask, coatIndirectSpecular, fresnelTerm);

        // Blend with base layer using khronos glTF recommended way using NoV
        // Smooth surface & "ambiguous" lighting
        // NOTE: fresnelTerm (above) is pow4 instead of pow5, but should be ok as blend weight.
        half coatFresnel = kDielectricSpec.x + kDielectricSpec.a * fresnelTerm;
        color = color * (1.0 - coatFresnel * clearCoatMask) + coatColor;
    }
    return color * occlusion;
}

void MixRealtimeAndBakedGI(inout Light light, half3 normalWS, inout URP_LIGHT_ACCUM3 bakedGI)
{
    if (LightmapAvailable() && MixedLightingSubtractive())
        bakedGI = SubtractDirectMainLightFromLightmap(light, normalWS, bakedGI);
}

void MixRealtimeAndBakedGI(inout Light light, half3 normalWS, inout URP_LIGHT_ACCUM3 bakedGI, AmbientOcclusionFactor aoFactor)
{
    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_AMBIENT_OCCLUSION))
    {
        bakedGI *= aoFactor.indirectAmbientOcclusion;
    }

    MixRealtimeAndBakedGI(light, normalWS, bakedGI);
}

#endif
