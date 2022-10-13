#ifndef __MASKVOLUME_HLSL__
#define __MASKVOLUME_HLSL__

#include "Packages/com.unity.render-pipelines.high-definition-config/Runtime/ShaderConfig.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaskVolume/MaskVolume.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaskVolume/MaskVolumeLightLoopDef.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaskVolume/MaskVolumeAtlas.hlsl"

// Copied from VolumeVoxelization.compute
float MaskVolumeComputeFadeFactor(
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

void MaskVolumeComputeOBBBoundsToFrame(OrientedBBox maskVolumeBounds, out float3x3 obbFrame, out float3 obbExtents, out float3 obbCenter)
{
    obbFrame = float3x3(maskVolumeBounds.right, maskVolumeBounds.up, cross(maskVolumeBounds.right, maskVolumeBounds.up));
    obbExtents = float3(maskVolumeBounds.extentX, maskVolumeBounds.extentY, maskVolumeBounds.extentZ);
    obbCenter = maskVolumeBounds.center; 
}

void MaskVolumeComputeTexel3DAndWeight(
    float weightHierarchy,
    MaskVolumeEngineData maskVolumeData,
    float3x3 obbFrame,
    float3 obbExtents,
    float3 obbCenter,
    float3 samplePositionWS,
    float samplePositionLinearDepth,
    out float3 maskVolumeTexel3D,
    out float weight)
{
    float3 samplePositionBS = mul(obbFrame, samplePositionWS - obbCenter);
    float3 samplePositionBCS = samplePositionBS * rcp(obbExtents);
    float3 samplePositionBNDC = samplePositionBCS * 0.5 + 0.5;
    float3 maskVolumeUVW = clamp(samplePositionBNDC.xyz, 0.5 * maskVolumeData.resolutionInverse, 1.0 - maskVolumeData.resolutionInverse * 0.5);
    maskVolumeTexel3D = maskVolumeUVW * maskVolumeData.resolution;

    float fadeFactor = MaskVolumeComputeFadeFactor(
        samplePositionBNDC,
        samplePositionLinearDepth,
        maskVolumeData.rcpPosFaceFade,
        maskVolumeData.rcpNegFaceFade,
        maskVolumeData.rcpDistFadeLen,
        maskVolumeData.endTimesRcpDistFadeLen
    );

    weight = fadeFactor * maskVolumeData.weight;

    if (maskVolumeData.blendMode == MASKVOLUMEBLENDMODE_ADDITIVE)
        weight = fadeFactor;
    else if (maskVolumeData.blendMode == MASKVOLUMEBLENDMODE_SUBTRACTIVE)
        weight = -fadeFactor;
    else
    {
        // Alpha composite: weight = (1.0f - weightHierarchy) * fadeFactor;
        weight = weightHierarchy * -fadeFactor + fadeFactor;
    }
}

float3 MaskVolumeComputeTexel3DFromBilateralFilter(
    float3 maskVolumeTexel3D,
    MaskVolumeEngineData maskVolumeData,
    float3 positionUnbiasedWS,
    float3 positionBiasedWS,
    float3 normalWS,
    float3x3 obbFrame,
    float3 obbExtents,
    float3 obbCenter)
{
    return maskVolumeTexel3D;
}

float3 MaskVolumeEvaluateSphericalHarmonicsL0(float3 normalWS, MaskVolumeSphericalHarmonicsL0 coefficients)
{
    {
        float3 sampleOutgoingRadiance = coefficients.data[0].rgb;
        return sampleOutgoingRadiance;
    }
}

float3 MaskVolumeEvaluateSphericalHarmonicsL1(float3 normalWS, MaskVolumeSphericalHarmonicsL1 coefficients)
{
    {
        float3 sampleOutgoingRadiance = SHEvalLinearL0L1(normalWS, coefficients.data[0], coefficients.data[1], coefficients.data[2]);
        return sampleOutgoingRadiance;
    }
}

float3 MaskVolumeEvaluateSphericalHarmonicsL2(float3 normalWS, MaskVolumeSphericalHarmonicsL2 coefficients)
{
    {
        float3 sampleOutgoingRadiance = SampleSH9(coefficients.data, normalWS);
        return sampleOutgoingRadiance;
    }
}

// Generate MaskVolumeAccumulateSphericalHarmonicsL0 function:
#define MASK_VOLUMES_ACCUMULATE_MODE MASKVOLUMESENCODINGMODES_SPHERICAL_HARMONICS_L0
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaskVolume/MaskVolumeAccumulate.hlsl"
#undef MASK_VOLUMES_ACCUMULATE_MODE

#ifndef MASK_VOLUMES_SAMPLING_MODE
// Default to sampling mask volumes at native atlas encoding mode.
// Users can override this by defining MASK_VOLUMES_SAMPLING_MODE before including LightLoop.hlsl
// TODO: It's likely we will want to extend this out to simply be shader LOD quality levels,
// as there are other parameters such as bilateral filtering, additive blending, and normal bias
// that we will want to disable for a low quality high performance mode.
#define MASK_VOLUMES_SAMPLING_MODE SHADEROPTIONS_MASK_VOLUMES_ENCODING_MODE
#endif

void MaskVolumeEvaluateSphericalHarmonics(PositionInputs posInput, float3 normalWS, uint renderingLayers, float weightHierarchy, inout float3 mask)
{
    MaskVolumeSphericalHarmonicsL0 coefficients;
    MaskVolumeAccumulateSphericalHarmonicsL0(posInput, normalWS, renderingLayers, coefficients, weightHierarchy);
    mask += MaskVolumeEvaluateSphericalHarmonicsL0(normalWS, coefficients);
}

#endif // __MASKVOLUME_HLSL__
