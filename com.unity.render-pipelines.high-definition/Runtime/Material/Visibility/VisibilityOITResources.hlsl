#ifndef VISIBILITY_OIT_RESOURCES
#define VISIBILITY_OIT_RESOURCES

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VisibilityCommon.hlsl"

#define DITHER_TILE_SIZE 128
#define DITHER_TILE_TOTAL_PIXELS (DITHER_TILE_SIZE * DITHER_TILE_SIZE)

TEXTURE2D_X_UINT2(_VisOITCount);
ByteAddressBuffer _VisOITBuffer;
ByteAddressBuffer _VisOITHistogramBuffer;
ByteAddressBuffer _VisOITPrefixedHistogramBuffer;
ByteAddressBuffer _VisOITListsCounts;
ByteAddressBuffer _VisOITListsOffsets;
ByteAddressBuffer _VisOITSubListsCounts;

float4 DebugDrawOITHistogram(float2 sampleUV, float2 screenSize)
{
    float2 workOffset = float2(0.02,0.02);
    float2 workSize = float2(0.3, 0.25);

    sampleUV = (sampleUV - workOffset) / workSize;
    if (any(sampleUV.xy < float2(0.0, 0.0)) || any(sampleUV.xy > float2(1.0, 1.0)))
        return float4(0,0,0,0);

    float samplePixelSize = rcp(screenSize.x * workSize.x);
    float barsPerPixel = (float)DITHER_TILE_TOTAL_PIXELS * samplePixelSize;
    uint offset = floor(sampleUV.x * DITHER_TILE_TOTAL_PIXELS);

    uint accumulation = 0;

    [loop]
    for (int i = -0.5*round(barsPerPixel); i < 0.5*round(barsPerPixel); ++i)
    {
        accumulation += _VisOITHistogramBuffer.Load(clamp((offset + i), 0, DITHER_TILE_TOTAL_PIXELS - 1) << 2);
    }

    float perc = float(accumulation);;
    float maxVal = (float)_VisOITPrefixedHistogramBuffer.Load((DITHER_TILE_TOTAL_PIXELS - 1) << 2);
    return float4(sqrt(perc/maxVal).xxx, 1.0);
}

namespace VisibilityOIT
{

void PackVisibilityData(Visibility::VisibilityData data, uint2 texelCoordinate, out uint3 packedData)
{
    uint2 packedDataHalfs;
    Visibility::PackVisibilityData(data, packedData.x, packedDataHalfs);
    packedData.y = (packedDataHalfs.x & 0xFF) | (packedDataHalfs.y << 8);
    packedData.z = (texelCoordinate.x & 0xFFFF) | ((texelCoordinate.y << 16) & 0xFFFF);
}

void UnpackVisibilityData(uint3 packedData, out Visibility::VisibilityData data, out uint2 texelCoordinate)
{
    uint2 packedDataHalfs = uint2(packedData.y & 0xFF, packedData.y >> 8);
    Visibility::UnpackVisibilityData(packedData.x, packedDataHalfs, data);
    texelCoordinate = uint2(packedData.z & 0xFFFF, (packedData.z >> 16) & 0xFFFF);
}

void GetPixelList(uint pixelOffset, out uint listCount, out uint listOffset)
{
    listCount = _VisOITSubListsCounts.Load(pixelOffset << 2);
    listOffset = _VisOITListsOffsets.Load(pixelOffset << 2);
}

void GetVisibilitySample(uint i, uint listOffset, out Visibility::VisibilityData data, out uint2 texelCoordinate)
{
    uint3 packedData = _VisOITBuffer.Load3(((listOffset + i) * 3) << 2);
    VisibilityOIT::UnpackVisibilityData(packedData, data, texelCoordinate);
} 

}

#endif
