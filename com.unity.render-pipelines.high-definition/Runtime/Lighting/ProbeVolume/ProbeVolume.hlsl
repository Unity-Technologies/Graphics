#ifndef __PROBEVOLUME_HLSL__
#define __PROBEVOLUME_HLSL__

#include "Packages/com.unity.render-pipelines.high-definition-config/Runtime/ShaderConfig.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolume.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeLightLoopDef.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeAtlas.hlsl"

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

#if SHADEROPTIONS_PROBE_VOLUMES_BILATERAL_FILTERING_MODE != PROBEVOLUMESBILATERALFILTERINGMODES_NONE
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
        weight = fadeFactor;
    else if (probeVolumeData.volumeBlendMode == VOLUMEBLENDMODE_SUBTRACTIVE)
        weight = -fadeFactor;
    else
#endif
    {
        // Alpha composite: weight = (1.0f - weightHierarchy) * fadeFactor;
        weight = weightHierarchy * -fadeFactor + fadeFactor;
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
        return sampleOutgoingRadiance;
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
        return sampleOutgoingRadiance;
    }
}

// Fallback to global ambient probe lighting when probe volume lighting weight is not fully saturated.
float3 ProbeVolumeEvaluateAmbientProbeFallback(float3 normalWS, float weightHierarchy)
{
    float3 sampleAmbientProbeOutgoingRadiance = float3(0.0, 0.0, 0.0);
    if (weightHierarchy < 1.0
#ifdef DEBUG_DISPLAY
        && (_DebugProbeVolumeMode != PROBEVOLUMEDEBUGMODE_VISUALIZE_DEBUG_COLORS)
        && (_DebugProbeVolumeMode != PROBEVOLUMEDEBUGMODE_VISUALIZE_VALIDITY)
#endif
    )
    {

        sampleAmbientProbeOutgoingRadiance = SampleSH9(_ProbeVolumeAmbientProbeFallbackPackedCoeffs, normalWS) * (1.0 - weightHierarchy);
    }

    return sampleAmbientProbeOutgoingRadiance;
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

#ifndef PROBE_VOLUMES_SAMPLING_MODE
// Default to sampling probe volumes at native atlas encoding mode.
// Users can override this by defining PROBE_VOLUMES_SAMPLING_MODE before including LightLoop.hlsl
// TODO: It's likely we will want to extend this out to simply be shader LOD quality levels,
// as there are other parameters such as bilateral filtering, additive blending, and normal bias
// that we will want to disable for a low quality high performance mode.
#define PROBE_VOLUMES_SAMPLING_MODE SHADEROPTIONS_PROBE_VOLUMES_ENCODING_MODE
#endif

void ProbeVolumeEvaluateSphericalHarmonics(PositionInputs posInput, float3 normalWS, float3 backNormalWS, uint renderingLayers, float weightHierarchy, inout float3 bakeDiffuseLighting, inout float3 backBakeDiffuseLighting)
{
#if PROBE_VOLUMES_SAMPLING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L0
        ProbeVolumeSphericalHarmonicsL0 coefficients;
        ProbeVolumeAccumulateSphericalHarmonicsL0(posInput, normalWS, renderingLayers, coefficients, weightHierarchy);
        bakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL0(normalWS, coefficients);
        backBakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL0(backNormalWS, coefficients);

#elif PROBE_VOLUMES_SAMPLING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L1
        ProbeVolumeSphericalHarmonicsL1 coefficients;
        ProbeVolumeAccumulateSphericalHarmonicsL1(posInput, normalWS, renderingLayers, coefficients, weightHierarchy);
        bakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL1(normalWS, coefficients);
        backBakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL1(backNormalWS, coefficients);

#elif PROBE_VOLUMES_SAMPLING_MODE == PROBEVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L2
        ProbeVolumeSphericalHarmonicsL2 coefficients;
        ProbeVolumeAccumulateSphericalHarmonicsL2(posInput, normalWS, renderingLayers, coefficients, weightHierarchy);
        bakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL2(normalWS, coefficients);
        backBakeDiffuseLighting += ProbeVolumeEvaluateSphericalHarmonicsL2(backNormalWS, coefficients);

#endif

        bakeDiffuseLighting += ProbeVolumeEvaluateAmbientProbeFallback(normalWS, weightHierarchy);
        backBakeDiffuseLighting += ProbeVolumeEvaluateAmbientProbeFallback(backNormalWS, weightHierarchy);
}

#endif // __PROBEVOLUME_HLSL__
