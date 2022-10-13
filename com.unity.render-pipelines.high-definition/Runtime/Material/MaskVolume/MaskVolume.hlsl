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

    // Alpha composite: weight = (1.0f - weightHierarchy) * weight;
    weight = weightHierarchy * -weight + weight;
}

float3 MaskVolumeEvaluate(float3 normalWS, MaskVolumeData coefficients)
{
    float3 mask = coefficients.data[0].rgb;
    return mask;
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaskVolume/MaskVolumeAccumulate.hlsl"

void MaskVolumeEvaluate(PositionInputs posInput, float3 normalWS, uint renderingLayers, float weightHierarchy, inout float3 mask)
{
    MaskVolumeData coefficients;
    MaskVolumeAccumulate(posInput, normalWS, renderingLayers, coefficients, weightHierarchy);
    mask += MaskVolumeEvaluate(normalWS, coefficients);
}

#endif // __MASKVOLUME_HLSL__
