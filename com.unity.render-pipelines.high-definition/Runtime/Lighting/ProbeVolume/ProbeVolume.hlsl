#ifndef __PROBEVOLUME_HLSL__
#define __PROBEVOLUME_HLSL__

#include "Packages/com.unity.render-pipelines.high-definition-config/Runtime/ShaderConfig.cs.hlsl"

#ifndef PROBE_VOLUMES_SAMPLING_MODE
// Default to sampling probe volumes at native atlas encoding mode.
// Users can override this by defining PROBE_VOLUMES_SAMPLING_MODE before including LightLoop.hlsl
// TODO: It's likely we will want to extend this out to simply be shader LOD quality levels,
// as there are other parameters such as bilateral filtering, additive blending, and normal bias
// that we will want to disable for a low quality high performance mode.
#define PROBE_VOLUMES_SAMPLING_MODE SHADEROPTIONS_PROBE_VOLUMES_ENCODING_MODE
#endif

#ifndef PROBE_VOLUMES_BILATERAL_FILTERING_MODE
// Default to filtering probe volumes with mode specified in ShaderConfig.cs
// Users can override this by defining PROBE_VOLUMES_BILATERAL_FILTERING_MODE before including LightLoop.hlsl
#define PROBE_VOLUMES_BILATERAL_FILTERING_MODE SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING_MODE
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl" // Needed for IsMatchingLightLayer().
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolume.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeLightLoopDef.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeAtlas.hlsl"

float ProbeVolumeGetReflectionProbeNormalizationEnabled()
{
    return _ProbeVolumeReflectionProbeNormalizationParameters.x > 0.0f;
}

float ProbeVolumeGetReflectionProbeNormalizationWeight()
{
    return _ProbeVolumeReflectionProbeNormalizationParameters.x;
}

float ProbeVolumeGetReflectionProbeNormalizationDirectionality()
{
    return _ProbeVolumeReflectionProbeNormalizationParameters.y;
}

float ProbeVolumeGetReflectionProbeNormalizationMin()
{
    return _ProbeVolumeReflectionProbeNormalizationParameters.z;
}

float ProbeVolumeGetReflectionProbeNormalizationMax()
{
    return _ProbeVolumeReflectionProbeNormalizationParameters.w;
}

// Copied from VolumeVoxelization.compute
float ProbeVolumeComputeFadeFactor(
    float3 samplePositionBoxNDC,
    float depthWS,
    float3 rcpPosFaceFade,
    float3 rcpNegFaceFade,
    float rcpDistFadeLen,
    float endTimesRcpDistFadeLen)
{
    float3 posF = Remap10(samplePositionBoxNDC, rcpPosFaceFade, rcpPosFaceFade);
    float3 negF = Remap01(samplePositionBoxNDC, rcpNegFaceFade, 0);
    float  dstF = Remap10(depthWS, rcpDistFadeLen, endTimesRcpDistFadeLen);
    float  fade = posF.x * posF.y * posF.z * negF.x * negF.y * negF.z;

    return dstF * fade;
}

#if PROBE_VOLUMES_BILATERAL_FILTERING_MODE != PROBEVOLUMESBILATERALFILTERINGMODES_DISABLED
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeBilateralFilter.hlsl"
#endif

void ProbeVolumeComputeOBBBoundsToFrame(OrientedBBox probeVolumeBounds, out float3x3 obbFrame, out float3 obbExtents, out float3 obbCenter)
{
    obbFrame = float3x3(probeVolumeBounds.right, probeVolumeBounds.up, cross(probeVolumeBounds.right, probeVolumeBounds.up));
    obbExtents = float3(probeVolumeBounds.extentX, probeVolumeBounds.extentY, probeVolumeBounds.extentZ);
    obbCenter = probeVolumeBounds.center; 
}

void ProbeVolumeComputeTexel3DAndWeight(
    float weightHierarchy,
    ProbeVolumeEngineData probeVolumeData,
    float3x3 obbFrame,
    float3 obbExtents,
    float3 obbCenter,
    float3 samplePositionWS,
    float samplePositionLinearDepth,
    out float3 probeVolumeTexel3D,
    out float weight)
{
    float3 samplePositionBS = mul(obbFrame, samplePositionWS - obbCenter);
    float3 samplePositionBCS = samplePositionBS * rcp(obbExtents);
    float3 samplePositionBNDC = samplePositionBCS * 0.5 + 0.5;
    float3 probeVolumeUVW = clamp(samplePositionBNDC.xyz, 0.5 * probeVolumeData.resolutionInverse, 1.0 - probeVolumeData.resolutionInverse * 0.5);
    probeVolumeTexel3D = probeVolumeUVW * probeVolumeData.resolution;

    float fadeFactor = ProbeVolumeComputeFadeFactor(
        samplePositionBNDC,
        samplePositionLinearDepth,
        probeVolumeData.rcpPosFaceFade,
        probeVolumeData.rcpNegFaceFade,
        probeVolumeData.rcpDistFadeLen,
        probeVolumeData.endTimesRcpDistFadeLen
    );

    weight = fadeFactor * probeVolumeData.weight;

#if SHADEROPTIONS_PROBE_VOLUMES_ADDITIVE_BLENDING
    if (probeVolumeData.volumeBlendMode == VOLUMEBLENDMODE_ADDITIVE)
    {
        // Nothing to do.
    }
    else if (probeVolumeData.volumeBlendMode == VOLUMEBLENDMODE_SUBTRACTIVE)
    {
        weight = -weight;
    }
    else
#endif
    {
        // Alpha composite: weight = (1.0f - weightHierarchy) * weight;
        weight = weightHierarchy * -weight + weight;
    }
}

float3 ProbeVolumeEvaluateSphericalHarmonicsL0(float3 normalWS, ProbeVolumeSphericalHarmonicsL0 coefficients)
{

#ifdef DEBUG_DISPLAY
    if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS)
    {
        float3 debugColors = coefficients.data[0].rgb; 
        return debugColors;
    }
    else if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
    {
        float validity = coefficients.data[0].x;
        return lerp(float3(1, 0, 0), float3(0, 1, 0), validity);
    }
    else
#endif
    {
        float3 sampleOutgoingRadiance = coefficients.data[0].rgb;
        return sampleOutgoingRadiance;
    }
}

float3 ProbeVolumeEvaluateSphericalHarmonicsL1(float3 normalWS, ProbeVolumeSphericalHarmonicsL1 coefficients)
{

#ifdef DEBUG_DISPLAY
    if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS)
    {
        float3 debugColors = coefficients.data[0].rgb; 
        return debugColors;
    }
    else if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
    {
        float validity = coefficients.data[0].x;
        return lerp(float3(1, 0, 0), float3(0, 1, 0), validity);
    }
    else
#endif
    {
        float3 sampleOutgoingRadiance = SHEvalLinearL0L1(normalWS, coefficients.data[0], coefficients.data[1], coefficients.data[2]);

        // Ringing can cause negative values. Clip them to avoid inadvertently absorbing light from other sections of the light loop.
        return max(0.0f, sampleOutgoingRadiance);
    }
}

float3 ProbeVolumeEvaluateSphericalHarmonicsL2(float3 normalWS, ProbeVolumeSphericalHarmonicsL2 coefficients)
{

#ifdef DEBUG_DISPLAY
    if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS)
    {
        float3 debugColors = coefficients.data[0].rgb; 
        return debugColors;
    }
    else if (_DebugProbeVolumeMode == PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
    {
        float validity = coefficients.data[0].x;
        return lerp(float3(1, 0, 0), float3(0, 1, 0), validity);
    }
    else
#endif
    {
        float3 sampleOutgoingRadiance = SampleSH9(coefficients.data, normalWS);
        
        // Ringing can cause negative values. Clip them to avoid inadvertently absorbing light from other sections of the light loop.
        return max(0.0f, sampleOutgoingRadiance);
    }
}

float3 ProbeVolumeEvaluateSphericalHarmonicsL1(float3 normalWS, ProbeVolumeSphericalHarmonicsL1 coefficients, float directionality)
{
    coefficients.data[0].xyz *= directionality;
    coefficients.data[1].xyz *= directionality;
    coefficients.data[2].xyz *= directionality;

    return ProbeVolumeEvaluateSphericalHarmonicsL1(normalWS, coefficients);
}

float3 ProbeVolumeEvaluateSphericalHarmonicsL2(float3 normalWS, ProbeVolumeSphericalHarmonicsL2 coefficients, float directionality)
{
    // ProbeVolumeSphericalHarmonicsL2 is already pre-swizzled + normalized into the format that SampleSH9() expects, which is
    // outlined here:
    // https://www.ppsloan.org/publications/StupidSH36.pdf
    // Appendix A10 Shader/CPU code for Irradiance Environment Maps
    // 
    // Of note, is the folding of the constant portion of the SH[6].rgb terms into the DC term (which are packed in [3].z, [4].z, and [5].z)
    // We need to remove those constant factors to recover the raw DC term.
    coefficients.data[0].w += coefficients.data[3].z * (1.0 / 3.0) * (1.0 - directionality);
    coefficients.data[1].w += coefficients.data[4].z * (1.0 / 3.0) * (1.0 - directionality);
    coefficients.data[2].w += coefficients.data[5].z * (1.0 / 3.0) * (1.0 - directionality);

    coefficients.data[0].xyz *= directionality;
    coefficients.data[1].xyz *= directionality;
    coefficients.data[2].xyz *= directionality;
    coefficients.data[3] *= directionality;
    coefficients.data[4] *= directionality;
    coefficients.data[5] *= directionality;
    coefficients.data[6] *= directionality;

    return ProbeVolumeEvaluateSphericalHarmonicsL2(normalWS, coefficients);
}

// Generate ProbeVolumeAccumulateSphericalHarmonicsL0 function:
#define PROBE_VOLUMES_ACCUMULATE_MODE PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L0
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeAccumulate.hlsl"
#undef PROBE_VOLUMES_ACCUMULATE_MODE

// Generate ProbeVolumeAccumulateSphericalHarmonicsL1 function:
#define PROBE_VOLUMES_ACCUMULATE_MODE PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeAccumulate.hlsl"
#undef PROBE_VOLUMES_ACCUMULATE_MODE

// Generate ProbeVolumeAccumulateSphericalHarmonicsL2 function:
#define PROBE_VOLUMES_ACCUMULATE_MODE PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeAccumulate.hlsl"
#undef PROBE_VOLUMES_ACCUMULATE_MODE

void ProbeVolumeEvaluateSphericalHarmonics(PositionInputs posInput, float3 normalWS, float3 backNormalWS, float3 reflectionDirectionWS, float3 viewDirectionWS, uint renderingLayers, float weightHierarchy, inout float3 bakeDiffuseLighting, inout float3 backBakeDiffuseLighting, inout float3 reflectionProbeNormalizationLighting, out float reflectionProbeNormalizationWeight)
{
#if PROBE_VOLUMES_SAMPLING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L0
    ProbeVolumeSphericalHarmonicsL0 coefficients;
    ProbeVolumeAccumulateSphericalHarmonicsL0(posInput, normalWS, viewDirectionWS, renderingLayers, coefficients, weightHierarchy);
    bakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL0(normalWS, coefficients);
    backBakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL0(backNormalWS, coefficients);
    reflectionProbeNormalizationLighting += ProbeVolumeEvaluateSphericalHarmonicsL0(reflectionDirectionWS, coefficients);

#elif PROBE_VOLUMES_SAMPLING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
    ProbeVolumeSphericalHarmonicsL1 coefficients;
    ProbeVolumeAccumulateSphericalHarmonicsL1(posInput, normalWS, viewDirectionWS, renderingLayers, coefficients, weightHierarchy);
    bakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL1(normalWS, coefficients);
    backBakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL1(backNormalWS, coefficients);
    reflectionProbeNormalizationLighting += ProbeVolumeEvaluateSphericalHarmonicsL1(reflectionDirectionWS, coefficients, ProbeVolumeGetReflectionProbeNormalizationDirectionality());

#elif PROBE_VOLUMES_SAMPLING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
    ProbeVolumeSphericalHarmonicsL2 coefficients;
    ProbeVolumeAccumulateSphericalHarmonicsL2(posInput, normalWS, viewDirectionWS, renderingLayers, coefficients, weightHierarchy);
    bakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL2(normalWS, coefficients);
    backBakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL2(backNormalWS, coefficients);
    reflectionProbeNormalizationLighting += ProbeVolumeEvaluateSphericalHarmonicsL2(reflectionDirectionWS, coefficients, ProbeVolumeGetReflectionProbeNormalizationDirectionality());

#endif

    reflectionProbeNormalizationWeight = weightHierarchy * ProbeVolumeGetReflectionProbeNormalizationWeight();
}

#endif // __PROBEVOLUME_HLSL__
