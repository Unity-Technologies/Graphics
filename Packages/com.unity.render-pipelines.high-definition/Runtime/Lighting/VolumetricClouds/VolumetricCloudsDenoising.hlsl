#ifndef VOLUMETRIC_CLOUDS_DENOISING_H
#define VOLUMETRIC_CLOUDS_DENOISING_H

// Half resolution volumetric cloud texture
TEXTURE2D_X(_VolumetricCloudsTexture);
TEXTURE2D_X(_DepthStatusTexture);

// Clouds data
TEXTURE2D_X(_CloudsLightingTexture);
TEXTURE2D_X(_CloudsDepthTexture);

// Half resolution depth buffer
TEXTURE2D_X(_HalfResDepthBuffer);

// Given that the sky is virtually a skybox, we cannot use the motion vector buffer
float2 EvaluateCloudMotionVectors(float2 fullResCoord, float deviceDepth, float positionFlag)
{
    PositionInputs posInput = GetPositionInput(fullResCoord, _ScreenSize.zw, deviceDepth, _IsPlanarReflection ? _CameraInverseViewProjection_NO : UNITY_MATRIX_I_VP, UNITY_MATRIX_V);
    float4 worldPos = float4(posInput.positionWS, positionFlag);
    float4 prevPos = worldPos;

    float4 prevClipPos = mul(_IsPlanarReflection ? _CameraPrevViewProjection_NO : UNITY_MATRIX_PREV_VP, prevPos);
    float4 curClipPos = mul(_IsPlanarReflection ?  _CameraViewProjection_NO: UNITY_MATRIX_UNJITTERED_VP, worldPos);

    float2 previousPositionCS = prevClipPos.xy / prevClipPos.w;
    float2 positionCS = curClipPos.xy / curClipPos.w;

    // Convert from Clip space (-1..1) to NDC 0..1 space
    float2 velocity = (positionCS - previousPositionCS) * 0.5;
#if UNITY_UV_STARTS_AT_TOP
    velocity.y = -velocity.y;
#endif
    return velocity;
}

// Our dispatch is a 8x8 tile. We can access up to 3x3 values at dispatch's half resolution
// around the center pixel which represents a total of 36 uniques values for the tile.
groupshared float gs_cacheR[36];
groupshared float gs_cacheG[36];
groupshared float gs_cacheB[36];
groupshared float gs_cacheA[36];
groupshared float gs_cacheDP[36];
groupshared float gs_cacheDC[36];
groupshared float gs_cachePS[36];

uint2 HalfResolutionIndexToOffset(uint index)
{
    return uint2(index & 0x1, index / 2);
}

void FillCloudReprojectionLDS(uint groupIndex, uint2 groupOrigin)
{
    // Define which value we will be acessing with this worker thread
    int acessCoordX = groupIndex % 6;
    int acessCoordY = groupIndex / 6;

    // Everything we are accessing is in trace res (quarter rez).
    uint2 traceGroupOrigin = groupOrigin / 2;

    // The initial position of the access
    int2 originXY = traceGroupOrigin - int2(1, 1);

    // Compute the sample position
    int2 sampleCoord = int2(clamp(originXY.x + acessCoordX, 0, _TraceScreenSize.x - 1), clamp(originXY.y + acessCoordY, 0, _TraceScreenSize.y - 1));

    // The representative coordinate to use depends if we are using the checkerboard integration pattern (or not)
    int checkerBoardIndex = ComputeCheckerBoardIndex(sampleCoord, _SubPixelIndex);
    int2 representativeCoord = sampleCoord * 2 + (_EnableIntegration ? (int2)HalfResolutionIndexToOffset(checkerBoardIndex) : int2(0, 0));

    // Read the sample values
    float sampleDP = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    float4 sampleVal = LOAD_TEXTURE2D_X(_CloudsLightingTexture, sampleCoord);
    float sampleDC = LOAD_TEXTURE2D_X(_CloudsDepthTexture, sampleCoord).x;

    // Store into the LDS
    gs_cacheR[groupIndex] = sampleVal.r;
    gs_cacheG[groupIndex] = sampleVal.g;
    gs_cacheB[groupIndex] = sampleVal.b;
    gs_cacheA[groupIndex] = sampleVal.a;
    gs_cacheDP[groupIndex] = sampleDP;
    gs_cacheDC[groupIndex] = sampleDC;
}

uint OffsetToLDSAdress(uint2 groupThreadId, int2 offset)
{
    // Compute the tap coordinate in the 6x6 grid
    uint2 tapAddress = (uint2)((int2)(groupThreadId / 2 + 1) + offset);
    return clamp((uint)(tapAddress.x) % 6 + tapAddress.y * 6, 0, 35);
}

float GetCloudDepth_LDS(uint2 groupThreadId, int2 offset)
{
    return gs_cacheDC[OffsetToLDSAdress(groupThreadId, offset)];
}

float4 GetCloudLighting_LDS(uint2 groupThreadId, int2 offset)
{
    uint ldsTapAddress = OffsetToLDSAdress(groupThreadId, offset);
    return float4(gs_cacheR[ldsTapAddress], gs_cacheG[ldsTapAddress], gs_cacheB[ldsTapAddress], gs_cacheA[ldsTapAddress]);
}

struct CloudReprojectionData
{
    float4 cloudLighting;
    float pixelDepth;
    float cloudDepth;
};

CloudReprojectionData GetCloudReprojectionDataSample(uint index)
{
    CloudReprojectionData outVal;
    outVal.cloudLighting.r = gs_cacheR[index];
    outVal.cloudLighting.g = gs_cacheG[index];
    outVal.cloudLighting.b = gs_cacheB[index];
    outVal.cloudLighting.a = gs_cacheA[index];
    outVal.pixelDepth = gs_cacheDP[index];
    outVal.cloudDepth = gs_cacheDC[index];
    return outVal;
}

CloudReprojectionData GetCloudReprojectionDataSample(uint2 groupThreadId, int2 offset)
{
    return GetCloudReprojectionDataSample(OffsetToLDSAdress(groupThreadId, offset));
}

float4 ClipCloudsToRegion(float4 history, float4 minimum, float4 maximum, inout float validityFactor)
{
    // The transmittance is overriden using a clamp
    float clampedTransmittance = clamp(history.w, minimum.w, maximum.w);

    // The lighting is overriden using a clip
    float3 center  = 0.5 * (maximum.xyz + minimum.xyz);
    float3 extents = 0.5 * (maximum.xyz - minimum.xyz);

    // This is actually `distance`, however the keyword is reserved
    float3 offset = history.xyz - center;
    float3 v_unit = offset.xyz / extents.xyz;
    float3 absUnit = abs(v_unit);
    float maxUnit = Max3(absUnit.x, absUnit.y, absUnit.z);

    // We make the history less valid if we had to clip it
    validityFactor *= maxUnit > 1.0 ? 0.5 : 1.0;

    if (maxUnit > 1.0)
        return float4(center + (offset / maxUnit), clampedTransmittance);
    else
        return float4(history.xyz, clampedTransmittance);
}

// Function that fills the struct as we cannot use arrays
void FillCloudReprojectionNeighborhoodData_NOLDS(int2 traceCoord, int subRegionIdx, out NeighborhoodUpsampleData3x3 neighborhoodData)
{
    // Fill the sample data
    neighborhoodData.lowValue0 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(-1, -1));
    neighborhoodData.lowValue1 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(0, -1));
    neighborhoodData.lowValue2 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(1, -1));

    neighborhoodData.lowValue3 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(-1, 0));
    neighborhoodData.lowValue4 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(0, 0));
    neighborhoodData.lowValue5 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(1, 0));

    neighborhoodData.lowValue6 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(-1, 1));
    neighborhoodData.lowValue7 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(0, 1));
    neighborhoodData.lowValue8 = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord + int2(1, 1));

    int2 traceTapCoord = traceCoord + int2(-1, -1);
    int checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    int2 representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthA.x = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightA.x = _DistanceBasedWeights[subRegionIdx * 3 + 0].x;

    traceTapCoord = traceCoord + int2(0, -1);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthA.y = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightA.y = _DistanceBasedWeights[subRegionIdx * 3 + 0].y;

    traceTapCoord = traceCoord + int2(1, -1);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthA.z = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightA.z = _DistanceBasedWeights[subRegionIdx * 3 + 0].z;

    traceTapCoord = traceCoord + int2(-1, 0);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthA.w = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightA.w = _DistanceBasedWeights[subRegionIdx * 3 + 0].w;

    traceTapCoord = traceCoord + int2(0, 0);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthB.x = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightB.x = _DistanceBasedWeights[subRegionIdx * 3 + 1].x;

    traceTapCoord = traceCoord + int2(1, 0);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthB.y = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightB.y = _DistanceBasedWeights[subRegionIdx * 3 + 1].y;

    traceTapCoord = traceCoord + int2(-1, 1);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthB.z = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightB.z = _DistanceBasedWeights[subRegionIdx * 3 + 1].z;

    traceTapCoord = traceCoord + int2(0, 1);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthB.w = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightB.w = _DistanceBasedWeights[subRegionIdx * 3 + 1].w;

    traceTapCoord = traceCoord + int2(1, 1);
    checkerBoardIndex = ComputeCheckerBoardIndex(traceTapCoord, _SubPixelIndex);
    representativeCoord = traceTapCoord * 2 + HalfResolutionIndexToOffset(checkerBoardIndex);
    neighborhoodData.lowDepthC = LOAD_TEXTURE2D_X(_HalfResDepthBuffer, representativeCoord).x;
    neighborhoodData.lowWeightC = _DistanceBasedWeights[subRegionIdx * 3 + 2].x;

    // In the reprojection case, all masks are valid
    neighborhoodData.lowMasksA = 1.0f;
    neighborhoodData.lowMasksB = 1.0f;
    neighborhoodData.lowMasksC = 1.0f;
}

// Function that fills the struct as we cannot use arrays
void FillCloudReprojectionNeighborhoodData(int2 groupThreadId, int subRegionIdx, out NeighborhoodUpsampleData3x3 neighborhoodData)
{
    // Fill the sample data
    CloudReprojectionData data = GetCloudReprojectionDataSample(groupThreadId, int2(-1, -1));
    neighborhoodData.lowValue0 = data.cloudLighting;
    neighborhoodData.lowDepthA.x = data.pixelDepth;
    neighborhoodData.lowWeightA.x = _DistanceBasedWeights[subRegionIdx * 3 + 0].x;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(0, -1));
    neighborhoodData.lowValue1 = data.cloudLighting;
    neighborhoodData.lowDepthA.y = data.pixelDepth;
    neighborhoodData.lowWeightA.y = _DistanceBasedWeights[subRegionIdx * 3 + 0].y;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(1, -1));
    neighborhoodData.lowValue2 = data.cloudLighting;
    neighborhoodData.lowDepthA.z = data.pixelDepth;
    neighborhoodData.lowWeightA.z = _DistanceBasedWeights[subRegionIdx * 3 + 0].z;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(-1, 0));
    neighborhoodData.lowValue3 = data.cloudLighting;
    neighborhoodData.lowDepthA.w = data.pixelDepth;
    neighborhoodData.lowWeightA.w = _DistanceBasedWeights[subRegionIdx * 3 + 0].w;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(0, 0));
    neighborhoodData.lowValue4 = data.cloudLighting;
    neighborhoodData.lowDepthB.x = data.pixelDepth;
    neighborhoodData.lowWeightB.x = _DistanceBasedWeights[subRegionIdx * 3 + 1].x;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(1, 0));
    neighborhoodData.lowValue5 = data.cloudLighting;
    neighborhoodData.lowDepthB.y = data.pixelDepth;
    neighborhoodData.lowWeightB.y = _DistanceBasedWeights[subRegionIdx * 3 + 1].y;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(-1, 1));
    neighborhoodData.lowValue6 = data.cloudLighting;
    neighborhoodData.lowDepthB.z = data.pixelDepth;
    neighborhoodData.lowWeightB.z = _DistanceBasedWeights[subRegionIdx * 3 + 1].z;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(0, 1));
    neighborhoodData.lowValue7 = data.cloudLighting;
    neighborhoodData.lowDepthB.w = data.pixelDepth;
    neighborhoodData.lowWeightB.w = _DistanceBasedWeights[subRegionIdx * 3 + 1].w;

    data = GetCloudReprojectionDataSample(groupThreadId, int2(1, 1));
    neighborhoodData.lowValue8 = data.cloudLighting;
    neighborhoodData.lowDepthC = data.pixelDepth;
    neighborhoodData.lowWeightC = _DistanceBasedWeights[subRegionIdx * 3 + 2].x;

    // In the reprojection case, all masks are valid
    neighborhoodData.lowMasksA = 1.0f;
    neighborhoodData.lowMasksB = 1.0f;
    neighborhoodData.lowMasksC = 1.0f;
}

struct CloudUpscaleData
{
    float4 cloudLighting;
    float pixelDepth;
    float pixelStatus;
    float cloudDepth;
};

CloudUpscaleData GetCloudUpscaleDataSample(uint index)
{
    CloudUpscaleData outVal;
    outVal.cloudLighting.r = gs_cacheR[index];
    outVal.cloudLighting.g = gs_cacheG[index];
    outVal.cloudLighting.b = gs_cacheB[index];
    outVal.cloudLighting.a = gs_cacheA[index];
    outVal.pixelDepth = gs_cacheDP[index];
    outVal.pixelStatus = gs_cachePS[index];
    outVal.cloudDepth = gs_cacheDC[index];
    return outVal;
}

CloudUpscaleData GetCloudUpscaleDataSample(uint2 groupThreadId, int2 offset)
{
    return GetCloudUpscaleDataSample(OffsetToLDSAdress(groupThreadId, offset));
}

// Function that fills the struct as we cannot use arrays
void FillCloudUpscaleNeighborhoodData(int2 groupThreadId, int subRegionIdx, out NeighborhoodUpsampleData3x3 neighborhoodData)
{
    // Fill the sample data
    CloudUpscaleData data = GetCloudUpscaleDataSample(groupThreadId, int2(-1, -1));
    neighborhoodData.lowValue0 = data.cloudLighting;
    neighborhoodData.lowDepthA.x = data.pixelDepth;
    neighborhoodData.lowMasksA.x = data.pixelStatus;
    neighborhoodData.lowWeightA.x = _DistanceBasedWeights[subRegionIdx * 3 + 0].x;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(0, -1));
    neighborhoodData.lowValue1 = data.cloudLighting;
    neighborhoodData.lowDepthA.y = data.pixelDepth;
    neighborhoodData.lowMasksA.y = data.pixelStatus;
    neighborhoodData.lowWeightA.y = _DistanceBasedWeights[subRegionIdx * 3 + 0].y;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(1, -1));
    neighborhoodData.lowValue2 = data.cloudLighting;
    neighborhoodData.lowDepthA.z = data.pixelDepth;
    neighborhoodData.lowMasksA.z = data.pixelStatus;
    neighborhoodData.lowWeightA.z = _DistanceBasedWeights[subRegionIdx * 3 + 0].z;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(-1, 0));
    neighborhoodData.lowValue3 = data.cloudLighting;
    neighborhoodData.lowDepthA.w = data.pixelDepth;
    neighborhoodData.lowMasksA.w = data.pixelStatus;
    neighborhoodData.lowWeightA.w = _DistanceBasedWeights[subRegionIdx * 3 + 0].w;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(0, 0));
    neighborhoodData.lowValue4 = data.cloudLighting;
    neighborhoodData.lowDepthB.x = data.pixelDepth;
    neighborhoodData.lowMasksB.x = data.pixelStatus;
    neighborhoodData.lowWeightB.x = _DistanceBasedWeights[subRegionIdx * 3 + 1].x;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(1, 0));
    neighborhoodData.lowValue5 = data.cloudLighting;
    neighborhoodData.lowDepthB.y = data.pixelDepth;
    neighborhoodData.lowMasksB.y = data.pixelStatus;
    neighborhoodData.lowWeightB.y = _DistanceBasedWeights[subRegionIdx * 3 + 1].y;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(-1, 1));
    neighborhoodData.lowValue6 = data.cloudLighting;
    neighborhoodData.lowDepthB.z = data.pixelDepth;
    neighborhoodData.lowMasksB.z = data.pixelStatus;
    neighborhoodData.lowWeightB.z = _DistanceBasedWeights[subRegionIdx * 3 + 1].z;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(0, 1));
    neighborhoodData.lowValue7 = data.cloudLighting;
    neighborhoodData.lowDepthB.w = data.pixelDepth;
    neighborhoodData.lowMasksB.w = data.pixelStatus;
    neighborhoodData.lowWeightB.w = _DistanceBasedWeights[subRegionIdx * 3 + 1].w;

    data = GetCloudUpscaleDataSample(groupThreadId, int2(1, 1));
    neighborhoodData.lowValue8 = data.cloudLighting;
    neighborhoodData.lowDepthC = data.pixelDepth;
    neighborhoodData.lowMasksC = data.pixelStatus;
    neighborhoodData.lowWeightC = _DistanceBasedWeights[subRegionIdx * 3 + 2].x;
}

// Function that fills the struct as we cannot use arrays
void FillCloudUpscaleNeighborhoodData_NOLDS(int2 traceCoord, int subRegionIdx, out NeighborhoodUpsampleData3x3 neighborhoodData)
{
    // Fill the sample data (TOP LEFT)
    float4 lightingVal = LOAD_TEXTURE2D_X(_VolumetricCloudsTexture, traceCoord + int2(-1, -1));
    float3 depthStatusValue = LOAD_TEXTURE2D_X(_DepthStatusTexture, traceCoord + int2(-1, -1)).xyz;
    neighborhoodData.lowValue0 = lightingVal;
    neighborhoodData.lowDepthA.x = depthStatusValue.y;
    neighborhoodData.lowMasksA.x = saturate(depthStatusValue.x);
    neighborhoodData.lowWeightA.x = _DistanceBasedWeights[subRegionIdx * 3 + 0].x;

    // Fill the sample data (TOP CENTER)
    lightingVal = LOAD_TEXTURE2D_X(_VolumetricCloudsTexture, traceCoord + int2(0, -1));
    depthStatusValue = LOAD_TEXTURE2D_X(_DepthStatusTexture, traceCoord + int2(0, -1)).xyz;
    neighborhoodData.lowValue1 = lightingVal;
    neighborhoodData.lowDepthA.y = depthStatusValue.y;
    neighborhoodData.lowMasksA.y = saturate(depthStatusValue.x);
    neighborhoodData.lowWeightA.y = _DistanceBasedWeights[subRegionIdx * 3 + 0].y;

    // Fill the sample data (TOP RIGHT)
    lightingVal = LOAD_TEXTURE2D_X(_VolumetricCloudsTexture, traceCoord + int2(1, -1));
    depthStatusValue = LOAD_TEXTURE2D_X(_DepthStatusTexture, traceCoord + int2(1, -1)).xyz;
    neighborhoodData.lowValue2 = lightingVal;
    neighborhoodData.lowDepthA.z = depthStatusValue.y;
    neighborhoodData.lowMasksA.z = saturate(depthStatusValue.x);
    neighborhoodData.lowWeightA.z = depthStatusValue.z;
    neighborhoodData.lowWeightA.z = _DistanceBasedWeights[subRegionIdx * 3 + 0].z;

    // Fill the sample data (MID LEFT)
    lightingVal = LOAD_TEXTURE2D_X(_VolumetricCloudsTexture, traceCoord + int2(-1, 0));
    depthStatusValue = LOAD_TEXTURE2D_X(_DepthStatusTexture, traceCoord + int2(-1, 0)).xyz;
    neighborhoodData.lowValue3 = lightingVal;
    neighborhoodData.lowDepthA.w = depthStatusValue.y;
    neighborhoodData.lowMasksA.w = saturate(depthStatusValue.x);
    neighborhoodData.lowWeightA.w = _DistanceBasedWeights[subRegionIdx * 3 + 0].w;

    // Fill the sample data (MID CENTER)
    lightingVal = LOAD_TEXTURE2D_X(_VolumetricCloudsTexture, traceCoord + int2(0, 0));
    depthStatusValue = LOAD_TEXTURE2D_X(_DepthStatusTexture, traceCoord + int2(0, 0)).xyz;
    neighborhoodData.lowValue4 = lightingVal;
    neighborhoodData.lowDepthB.x = depthStatusValue.y;
    neighborhoodData.lowMasksB.x = saturate(depthStatusValue.x);
    neighborhoodData.lowWeightB.x = _DistanceBasedWeights[subRegionIdx * 3 + 1].x;

    // Fill the sample data (MID RIGHT)
    lightingVal = LOAD_TEXTURE2D_X(_VolumetricCloudsTexture, traceCoord + int2(1, 0));
    depthStatusValue = LOAD_TEXTURE2D_X(_DepthStatusTexture, traceCoord + int2(1, 0)).xyz;
    neighborhoodData.lowValue5 = lightingVal;
    neighborhoodData.lowDepthB.y = depthStatusValue.y;
    neighborhoodData.lowMasksB.y = saturate(depthStatusValue.x);
    neighborhoodData.lowWeightB.y = _DistanceBasedWeights[subRegionIdx * 3 + 1].y;

    // Fill the sample data (BOTTOM LEFT)
    lightingVal = LOAD_TEXTURE2D_X(_VolumetricCloudsTexture, traceCoord + int2(-1, 1));
    depthStatusValue = LOAD_TEXTURE2D_X(_DepthStatusTexture, traceCoord + int2(-1, 1)).xyz;
    neighborhoodData.lowValue6 = lightingVal;
    neighborhoodData.lowDepthB.z = depthStatusValue.y;
    neighborhoodData.lowMasksB.z = saturate(depthStatusValue.x);
    neighborhoodData.lowWeightB.z = depthStatusValue.z;
    neighborhoodData.lowWeightB.z = _DistanceBasedWeights[subRegionIdx * 3 + 1].z;

    // Fill the sample data (BOTTOM CENTER)
    lightingVal = LOAD_TEXTURE2D_X(_VolumetricCloudsTexture, traceCoord + int2(0, 1));
    depthStatusValue = LOAD_TEXTURE2D_X(_DepthStatusTexture, traceCoord + int2(0, 1)).xyz;
    neighborhoodData.lowValue7 = lightingVal;
    neighborhoodData.lowDepthB.w = depthStatusValue.y;
    neighborhoodData.lowMasksB.w = saturate(depthStatusValue.x);
    neighborhoodData.lowWeightB.w = depthStatusValue.z;
    neighborhoodData.lowWeightB.w = _DistanceBasedWeights[subRegionIdx * 3 + 1].w;

    // Fill the sample data (BOTTOM CENTER)
    lightingVal = LOAD_TEXTURE2D_X(_VolumetricCloudsTexture, traceCoord + int2(1, 1));
    depthStatusValue = LOAD_TEXTURE2D_X(_DepthStatusTexture, traceCoord + int2(1, 1)).xyz;
    neighborhoodData.lowValue8 = lightingVal;
    neighborhoodData.lowDepthC = depthStatusValue.y;
    neighborhoodData.lowMasksC = saturate(depthStatusValue.x);
    neighborhoodData.lowWeightC = _DistanceBasedWeights[subRegionIdx * 3 + 2].x;
}

float EvaluateUpscaledCloudDepth(int2 groupThreadId, NeighborhoodUpsampleData3x3 nhd)
{
    // There are some cases where we need to provide a depth value for the volumetric clouds (mainly for the fog now, but this may change in the future)
    // Given that the cloud value for a final pixel doesn't come from a single half resolution pixel (it is interpolated), we also need to interpolate
    // the depth. That said, we cannot interpolate cloud values with non-cloudy pixels values and we need to exclude them from the evaluation
    // Also, we should be doing the interpolation in linear space to be accurate, but it becomes more expensive and experimentally I didn't find
    // any artifact of doing it in logarithmic space.
    float finalDepth = 0.0f;
    float sumWeight = 0.0f;

    // Top left
    float weight = (nhd.lowValue0.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksA.x;
    finalDepth += weight * GetCloudDepth_LDS(groupThreadId, int2(-1, -1));
    sumWeight += weight;

    // Top center
    weight = (nhd.lowValue1.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksA.y;
    finalDepth += weight * GetCloudDepth_LDS(groupThreadId, int2(0, -1));
    sumWeight += weight;

    // Top right
    weight = (nhd.lowValue2.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksA.z;
    finalDepth += weight * GetCloudDepth_LDS(groupThreadId, int2(1, -1));
    sumWeight += weight;

    // Mid left
    weight = (nhd.lowValue3.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksA.w;
    finalDepth += weight * GetCloudDepth_LDS(groupThreadId, int2(-1, 0));
    sumWeight += weight;

    // Mid center
    weight = (nhd.lowValue4.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksB.x;
    finalDepth += weight * GetCloudDepth_LDS(groupThreadId, int2(0, 0));
    sumWeight += weight;

    // Mid right
    weight = (nhd.lowValue5.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksB.y;
    finalDepth += weight * GetCloudDepth_LDS(groupThreadId, int2(1, 0));
    sumWeight += weight;

    // Bottom left
    weight = (nhd.lowValue6.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksB.z;
    finalDepth += weight * GetCloudDepth_LDS(groupThreadId, int2(-1, 1));
    sumWeight += weight;

    // Bottom mid
    weight = (nhd.lowValue7.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksB.w;
    finalDepth += weight * GetCloudDepth_LDS(groupThreadId, int2(0, 1));
    sumWeight += weight;

    // Bottom mid
    weight = (nhd.lowValue8.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksC;
    finalDepth += weight * GetCloudDepth_LDS(groupThreadId, int2(1, 1));
    sumWeight += weight;

    return finalDepth / sumWeight;
}

float EvaluateUpscaledCloudDepth_NOLDS(int2 halfResCoord, NeighborhoodUpsampleData3x3 nhd)
{
    float finalDepth = 0.0f;
    float sumWeight = 0.0f;

    // Top left
    float weight = (nhd.lowValue0.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksA.x;
    finalDepth += weight * LOAD_TEXTURE2D_X(_DepthStatusTexture, halfResCoord + int2(-1, -1)).z;
    sumWeight += weight;

    // Top center
    weight = (nhd.lowValue1.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksA.y;
    finalDepth += weight * LOAD_TEXTURE2D_X(_DepthStatusTexture, halfResCoord + int2(0, -1)).z;
    sumWeight += weight;

    // Top right
    weight = (nhd.lowValue2.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksA.z;
    finalDepth += weight * LOAD_TEXTURE2D_X(_DepthStatusTexture, halfResCoord + int2(1, -1)).z;
    sumWeight += weight;

    // Mid left
    weight = (nhd.lowValue3.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksA.w;
    finalDepth += weight * LOAD_TEXTURE2D_X(_DepthStatusTexture, halfResCoord + int2(-1, 0)).z;
    sumWeight += weight;

    // Mid center
    weight = (nhd.lowValue4.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksB.x;
    finalDepth += weight * LOAD_TEXTURE2D_X(_DepthStatusTexture, halfResCoord + int2(0, 0)).z;
    sumWeight += weight;

    // Mid right
    weight = (nhd.lowValue5.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksB.y;
    finalDepth += weight * LOAD_TEXTURE2D_X(_DepthStatusTexture, halfResCoord + int2(1, 0)).z;
    sumWeight += weight;

    // Bottom left
    weight = (nhd.lowValue6.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksB.z;
    finalDepth += weight * LOAD_TEXTURE2D_X(_DepthStatusTexture, halfResCoord + int2(-1, 1)).z;
    sumWeight += weight;

    // Bottom mid
    weight = (nhd.lowValue7.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksB.w;
    finalDepth += weight * LOAD_TEXTURE2D_X(_DepthStatusTexture, halfResCoord + int2(0, 1)).z;
    sumWeight += weight;

    // Bottom mid
    weight = (nhd.lowValue8.w != 1.0 ? 1.0 : 0.0) * nhd.lowMasksC;
    finalDepth += weight * LOAD_TEXTURE2D_X(_DepthStatusTexture, halfResCoord + int2(1, 1)).z;
    sumWeight += weight;

    return finalDepth / sumWeight;
}

// This function will return something strictly smaller than 0 if any of the lower res pixels
// have some amound of clouds.
float EvaluateRegionEmptiness(NeighborhoodUpsampleData3x3 data)
{
    float emptyRegionFlag = 1.0f;
    emptyRegionFlag *= lerp(1.0, data.lowValue0.w, data.lowWeightA.x != 0.0 ? 1.0 : 0.0);
    emptyRegionFlag *= lerp(1.0, data.lowValue1.w, data.lowWeightA.y != 0.0 ? 1.0 : 0.0);
    emptyRegionFlag *= lerp(1.0, data.lowValue2.w, data.lowWeightA.z != 0.0 ? 1.0 : 0.0);
    emptyRegionFlag *= lerp(1.0, data.lowValue3.w, data.lowWeightA.w != 0.0 ? 1.0 : 0.0);
    emptyRegionFlag *= lerp(1.0, data.lowValue4.w, data.lowWeightB.x != 0.0 ? 1.0 : 0.0);
    emptyRegionFlag *= lerp(1.0, data.lowValue5.w, data.lowWeightB.y != 0.0 ? 1.0 : 0.0);
    emptyRegionFlag *= lerp(1.0, data.lowValue6.w, data.lowWeightB.z != 0.0 ? 1.0 : 0.0);
    emptyRegionFlag *= lerp(1.0, data.lowValue7.w, data.lowWeightB.w != 0.0 ? 1.0 : 0.0);
    emptyRegionFlag *= lerp(1.0, data.lowValue8.w, data.lowWeightC != 0.0 ? 1.0 : 0.0);
    return emptyRegionFlag;
}
#endif // VOLUMETRIC_CLOUDS_DENOISING_H
