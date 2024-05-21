#ifndef VOLUMETRIC_CLOUDS_DENOISING_H
#define VOLUMETRIC_CLOUDS_DENOISING_H

// Need a max to avoid infinitely far away points
#define MAX_VOLUMETRIC_CLOUDS_DISTANCE 200000.0f

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
    float3 V = GetCloudViewDirWS(fullResCoord);

    float depth = min(DecodeInfiniteDepth(deviceDepth, _CloudNearPlane), MAX_VOLUMETRIC_CLOUDS_DISTANCE);
    float4 worldPos = float4(V * depth, positionFlag);
    float4 prevPos = worldPos;

    float4 curClipPos = mul(UNITY_MATRIX_UNJITTERED_VP, worldPos);
    float4 prevClipPos = mul(_CameraPrevViewProjection[unity_StereoEyeIndex], prevPos);

    float2 previousPositionCS = prevClipPos.xy / prevClipPos.w;
    float2 positionCS = curClipPos.xy / curClipPos.w;

    // Convert from Clip space (-1..1) to NDC 0..1 space
    float2 velocity = (positionCS - previousPositionCS) * 0.5;
#if UNITY_UV_STARTS_AT_TOP
    velocity.y = -velocity.y;
#endif
    return velocity;
}

bool EvaluateDepthDifference(float depthPrev, float depthCurr)
{
    if ((depthPrev == UNITY_RAW_FAR_CLIP_VALUE) != (depthCurr == UNITY_RAW_FAR_CLIP_VALUE))
        return false;
    else
    {
        float linearDepthP = Linear01Depth(depthPrev, _ZBufferParams);
        float linearDepthC = Linear01Depth(depthCurr, _ZBufferParams);
        //return abs(linearDepthP - linearDepthC) <= linearDepthC * 0.2;
        return abs(linearDepthP/linearDepthC - 1.0f) <= 0.2;
    }
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

float4 Linear01Depth(float4 depth, float4 zBufferParam)
{
    return float4(
        Linear01Depth(depth.x, zBufferParam),
        Linear01Depth(depth.y, zBufferParam),
        Linear01Depth(depth.z, zBufferParam),
        Linear01Depth(depth.w, zBufferParam));
}

#ifndef WITHOUT_LDS
// Our dispatch is a 8x8 tile. We can access up to 3x3 values at dispatch's half resolution
// around the center pixel which represents a total of 36 uniques values for the tile.
groupshared float4 gs_cacheRGBA[36];
groupshared float gs_cacheDP[36];
groupshared float gs_cacheDC[36];
groupshared float gs_cachePS[36];

// Init LDS
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
    int2 sampleCoord = clamp(originXY + int2(acessCoordX, acessCoordY), 0, _TraceScreenSize.xy - 1);

    // The representative coordinate to use depends if we are using the checkerboard integration pattern (or not)
    int2 representativeCoord = sampleCoord * 2;
    if (_EnableIntegration)
        representativeCoord += ComputeCheckerBoardOffset(sampleCoord, _SubPixelIndex);

    // Read the sample values
    float sampleDP = LOAD_TEXTURE2D_X(_CameraDepthTexture, _ReprojDepthMipOffset + representativeCoord).x;
    float3 sampleVal = LOAD_TEXTURE2D_X(_CloudsLightingTexture, sampleCoord).xyz;
    float2 sampleDC_TR = LOAD_TEXTURE2D_X(_CloudsDepthTexture, sampleCoord).xy;

    // Store into the LDS
    gs_cacheRGBA[groupIndex] = float4(sampleVal.rgb, sampleDC_TR.y);
    gs_cacheDP[groupIndex] = sampleDP;
    gs_cacheDC[groupIndex] = sampleDC_TR.x;
}

void FillLDSUpscale(uint groupIndex, uint2 groupOrigin)
{
    // Define which value we will be acessing with this worker thread
    int acessCoordX = groupIndex % 6;
    int acessCoordY = groupIndex / 6;

    // Everything we are accessing is in intermediate res (half rez).
    uint2 traceGroupOrigin = groupOrigin / 2;

    // The initial position of the access
    int2 originXY = traceGroupOrigin - int2(1, 1);

    // Compute the sample position
    int2 sampleCoord = clamp(originXY + int2(acessCoordX, acessCoordY), 0, _IntermediateScreenSize.xy - 1);

    // Read the sample value
    float3 sampleVal = LOAD_TEXTURE2D_X(_VolumetricCloudsTexture, sampleCoord).xyz;
    float4 depthStatusValue = LOAD_TEXTURE2D_X(_DepthStatusTexture, sampleCoord);

    // Store into the LDS
    gs_cacheRGBA[groupIndex] = float4(sampleVal.rgb, depthStatusValue.a);
    gs_cacheDP[groupIndex] = depthStatusValue.y;
    gs_cachePS[groupIndex] = saturate(depthStatusValue.x);
    gs_cacheDC[groupIndex] = depthStatusValue.z;
}

uint OffsetToLDSAdress(uint2 groupThreadId, int2 offset)
{
    // Compute the tap coordinate in the 6x6 grid
    uint2 tapAddress = (uint2)((int2)(groupThreadId / 2 + 1) + offset);
    return clamp((uint)(tapAddress.x) % 6 + tapAddress.y * 6, 0, 35);
}
#endif

#ifdef WITHOUT_LDS
float GetCloudDepth(int2 traceCoord, int2 offset)
{
    traceCoord = clamp(traceCoord + offset, 0, _TraceScreenSize.xy - 1);
    return LOAD_TEXTURE2D_X(_CloudsDepthTexture, traceCoord).x;
}

float4 GetCloudLighting(int2 traceCoord, int2 offset)
{
    float4 cloudLighting;
    traceCoord = clamp(traceCoord + offset, 0, _TraceScreenSize.xy - 1);

    cloudLighting.xyz = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord).xyz;
    cloudLighting.a = LOAD_TEXTURE2D_X(_CloudsDepthTexture, traceCoord).y;
    return cloudLighting;
}
#else
float GetCloudDepth(uint2 groupThreadId, int2 offset)
{
    return gs_cacheDC[OffsetToLDSAdress(groupThreadId, offset)];
}

float4 GetCloudLighting(uint2 groupThreadId, int2 offset)
{
    uint ldsTapAddress = OffsetToLDSAdress(groupThreadId, offset);
    return gs_cacheRGBA[ldsTapAddress];
}
#endif

struct CloudReprojectionData
{
    float4 cloudLighting;
    float pixelDepth;
    float cloudDepth;
};

#ifdef WITHOUT_LDS
CloudReprojectionData GetCloudReprojectionDataSample(int2 traceCoord, int2 offset)
{
    traceCoord = clamp(traceCoord + offset, 0, _TraceScreenSize.xy - 1);

    float3 color = LOAD_TEXTURE2D_X(_CloudsLightingTexture, traceCoord).xyz;
    float2 depthTransmittance = LOAD_TEXTURE2D_X(_CloudsDepthTexture, traceCoord).xy;

    int2 representativeCoord = traceCoord * 2 + ComputeCheckerBoardOffset(traceCoord, _SubPixelIndex);

    CloudReprojectionData outVal;
    outVal.cloudLighting.rgb = color;
    outVal.cloudLighting.a = depthTransmittance.y;
    outVal.cloudDepth = depthTransmittance.x;
    outVal.pixelDepth = LOAD_TEXTURE2D_X(_CameraDepthTexture, _ReprojDepthMipOffset + representativeCoord).x;
    return outVal;
}
#else
CloudReprojectionData GetCloudReprojectionDataSample(uint index)
{
    CloudReprojectionData outVal;
    outVal.cloudLighting = gs_cacheRGBA[index];
    outVal.pixelDepth = gs_cacheDP[index];
    outVal.cloudDepth = gs_cacheDC[index];
    return outVal;
}

CloudReprojectionData GetCloudReprojectionDataSample(uint2 groupThreadId, int2 offset)
{
    return GetCloudReprojectionDataSample(OffsetToLDSAdress(groupThreadId, offset));
}
#endif

// Function that fills the struct as we cannot use arrays
void FillCloudReprojectionNeighborhoodData(int2 threadCoord, int subRegionIdx, out NeighborhoodUpsampleData3x3 neighborhoodData)
{
    // Fill the sample data
    CloudReprojectionData data = GetCloudReprojectionDataSample(threadCoord, int2(-1, -1));
    neighborhoodData.lowValue0 = data.cloudLighting;
    neighborhoodData.lowDepthValueA.x = data.cloudDepth;
    neighborhoodData.lowDepthA.x = data.pixelDepth;
    neighborhoodData.lowWeightA.x = _DistanceBasedWeights[subRegionIdx * 3 + 0].x;

    data = GetCloudReprojectionDataSample(threadCoord, int2(0, -1));
    neighborhoodData.lowValue1 = data.cloudLighting;
    neighborhoodData.lowDepthValueA.y = data.cloudDepth;
    neighborhoodData.lowDepthA.y = data.pixelDepth;
    neighborhoodData.lowWeightA.y = _DistanceBasedWeights[subRegionIdx * 3 + 0].y;

    data = GetCloudReprojectionDataSample(threadCoord, int2(1, -1));
    neighborhoodData.lowValue2 = data.cloudLighting;
    neighborhoodData.lowDepthValueA.z = data.cloudDepth;
    neighborhoodData.lowDepthA.z = data.pixelDepth;
    neighborhoodData.lowWeightA.z = _DistanceBasedWeights[subRegionIdx * 3 + 0].z;

    data = GetCloudReprojectionDataSample(threadCoord, int2(-1, 0));
    neighborhoodData.lowValue3 = data.cloudLighting;
    neighborhoodData.lowDepthValueA.w = data.cloudDepth;
    neighborhoodData.lowDepthA.w = data.pixelDepth;
    neighborhoodData.lowWeightA.w = _DistanceBasedWeights[subRegionIdx * 3 + 0].w;

    data = GetCloudReprojectionDataSample(threadCoord, int2(0, 0));
    neighborhoodData.lowValue4 = data.cloudLighting;
    neighborhoodData.lowDepthValueB.x = data.cloudDepth;
    neighborhoodData.lowDepthB.x = data.pixelDepth;
    neighborhoodData.lowWeightB.x = _DistanceBasedWeights[subRegionIdx * 3 + 1].x;

    data = GetCloudReprojectionDataSample(threadCoord, int2(1, 0));
    neighborhoodData.lowValue5 = data.cloudLighting;
    neighborhoodData.lowDepthValueB.y = data.cloudDepth;
    neighborhoodData.lowDepthB.y = data.pixelDepth;
    neighborhoodData.lowWeightB.y = _DistanceBasedWeights[subRegionIdx * 3 + 1].y;

    data = GetCloudReprojectionDataSample(threadCoord, int2(-1, 1));
    neighborhoodData.lowValue6 = data.cloudLighting;
    neighborhoodData.lowDepthValueB.z = data.cloudDepth;
    neighborhoodData.lowDepthB.z = data.pixelDepth;
    neighborhoodData.lowWeightB.z = _DistanceBasedWeights[subRegionIdx * 3 + 1].z;

    data = GetCloudReprojectionDataSample(threadCoord, int2(0, 1));
    neighborhoodData.lowValue7 = data.cloudLighting;
    neighborhoodData.lowDepthValueB.w = data.cloudDepth;
    neighborhoodData.lowDepthB.w = data.pixelDepth;
    neighborhoodData.lowWeightB.w = _DistanceBasedWeights[subRegionIdx * 3 + 1].w;

    data = GetCloudReprojectionDataSample(threadCoord, int2(1, 1));
    neighborhoodData.lowValue8 = data.cloudLighting;
    neighborhoodData.lowDepthValueC = data.cloudDepth;
    neighborhoodData.lowDepthC = data.pixelDepth;
    neighborhoodData.lowWeightC = _DistanceBasedWeights[subRegionIdx * 3 + 2].x;
}

struct CloudUpscaleData
{
    float4 cloudLighting;
    float pixelDepth;
    float pixelStatus;
    float cloudDepth;
};

#ifdef WITHOUT_LDS
CloudUpscaleData GetCloudUpscaleDataSample(int2 intermediateCoord, int2 offset)
{
    intermediateCoord = clamp(intermediateCoord + offset, 0, _IntermediateScreenSize.xy - 1);

    float3 lightingVal = LOAD_TEXTURE2D_X(_VolumetricCloudsTexture, intermediateCoord).xyz;
    float4 depthStatusValue = LOAD_TEXTURE2D_X(_DepthStatusTexture, intermediateCoord);

    CloudUpscaleData outVal;
    outVal.cloudLighting.rgb = lightingVal;
    outVal.cloudLighting.a = depthStatusValue.a;
    outVal.pixelDepth = depthStatusValue.y;
    outVal.pixelStatus = saturate(depthStatusValue.x);
    outVal.cloudDepth = depthStatusValue.z;
    return outVal;
}
#else
CloudUpscaleData GetCloudUpscaleDataSample(uint index)
{
    CloudUpscaleData outVal;
    outVal.cloudLighting = gs_cacheRGBA[index];
    outVal.pixelDepth = gs_cacheDP[index];
    outVal.pixelStatus = gs_cachePS[index];
    outVal.cloudDepth = gs_cacheDC[index];
    return outVal;
}

CloudUpscaleData GetCloudUpscaleDataSample(uint2 groupThreadId, int2 offset)
{
    return GetCloudUpscaleDataSample(OffsetToLDSAdress(groupThreadId, offset));
}
#endif

// Function that fills the struct as we cannot use arrays
void FillCloudUpscaleNeighborhoodData(int2 threadCoord, int subRegionIdx, out NeighborhoodUpsampleData3x3 neighborhoodData)
{
    // Fill the sample data
    CloudUpscaleData data = GetCloudUpscaleDataSample(threadCoord, int2(-1, -1));
    neighborhoodData.lowValue0 = data.cloudLighting;
    neighborhoodData.lowDepthValueA.x = data.cloudDepth;
    neighborhoodData.lowDepthA.x = data.pixelDepth;
    neighborhoodData.lowWeightA.x = data.pixelStatus * _DistanceBasedWeights[subRegionIdx * 3 + 0].x;

    data = GetCloudUpscaleDataSample(threadCoord, int2(0, -1));
    neighborhoodData.lowValue1 = data.cloudLighting;
    neighborhoodData.lowDepthValueA.y = data.cloudDepth;
    neighborhoodData.lowDepthA.y = data.pixelDepth;
    neighborhoodData.lowWeightA.y = data.pixelStatus * _DistanceBasedWeights[subRegionIdx * 3 + 0].y;

    data = GetCloudUpscaleDataSample(threadCoord, int2(1, -1));
    neighborhoodData.lowValue2 = data.cloudLighting;
    neighborhoodData.lowDepthValueA.z = data.cloudDepth;
    neighborhoodData.lowDepthA.z = data.pixelDepth;
    neighborhoodData.lowWeightA.z = data.pixelStatus * _DistanceBasedWeights[subRegionIdx * 3 + 0].z;

    data = GetCloudUpscaleDataSample(threadCoord, int2(-1, 0));
    neighborhoodData.lowValue3 = data.cloudLighting;
    neighborhoodData.lowDepthValueA.w = data.cloudDepth;
    neighborhoodData.lowDepthA.w = data.pixelDepth;
    neighborhoodData.lowWeightA.w = data.pixelStatus * _DistanceBasedWeights[subRegionIdx * 3 + 0].w;

    data = GetCloudUpscaleDataSample(threadCoord, int2(0, 0));
    neighborhoodData.lowValue4 = data.cloudLighting;
    neighborhoodData.lowDepthValueB.x = data.cloudDepth;
    neighborhoodData.lowDepthB.x = data.pixelDepth;
    neighborhoodData.lowWeightB.x = data.pixelStatus * _DistanceBasedWeights[subRegionIdx * 3 + 1].x;

    data = GetCloudUpscaleDataSample(threadCoord, int2(1, 0));
    neighborhoodData.lowValue5 = data.cloudLighting;
    neighborhoodData.lowDepthValueB.y = data.cloudDepth;
    neighborhoodData.lowDepthB.y = data.pixelDepth;
    neighborhoodData.lowWeightB.y = data.pixelStatus * _DistanceBasedWeights[subRegionIdx * 3 + 1].y;

    data = GetCloudUpscaleDataSample(threadCoord, int2(-1, 1));
    neighborhoodData.lowValue6 = data.cloudLighting;
    neighborhoodData.lowDepthValueB.z = data.cloudDepth;
    neighborhoodData.lowDepthB.z = data.pixelDepth;
    neighborhoodData.lowWeightB.z = data.pixelStatus * _DistanceBasedWeights[subRegionIdx * 3 + 1].z;

    data = GetCloudUpscaleDataSample(threadCoord, int2(0, 1));
    neighborhoodData.lowValue7 = data.cloudLighting;
    neighborhoodData.lowDepthValueB.w = data.cloudDepth;
    neighborhoodData.lowDepthB.w = data.pixelDepth;
    neighborhoodData.lowWeightB.w = data.pixelStatus * _DistanceBasedWeights[subRegionIdx * 3 + 1].w;

    data = GetCloudUpscaleDataSample(threadCoord, int2(1, 1));
    neighborhoodData.lowValue8 = data.cloudLighting;
    neighborhoodData.lowDepthValueC = data.cloudDepth;
    neighborhoodData.lowDepthC = data.pixelDepth;
    neighborhoodData.lowWeightC = data.pixelStatus * _DistanceBasedWeights[subRegionIdx * 3 + 2].x;
}

#endif // VOLUMETRIC_CLOUDS_DENOISING_H
