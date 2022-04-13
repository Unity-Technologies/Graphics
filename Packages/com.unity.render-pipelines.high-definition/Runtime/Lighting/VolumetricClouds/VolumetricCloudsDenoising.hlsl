#ifndef VOLUMETRIC_CLOUDS_DENOISING_H
#define VOLUMETRIC_CLOUDS_DENOISING_H

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

#endif // VOLUMETRIC_CLOUDS_DENOISING_H
