#ifndef VISIBILITY_OIT_RESOURCES
#define VISIBILITY_OIT_RESOURCES

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
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

#if defined(USE_TEXTURE2D_X_AS_ARRAY)
Texture2DArray<float2> _OITTileHiZ;
#else
Texture2D<float2> _OITTileHiZ;
#endif

TEXTURE2D_X_UINT4(_VisOITOffscreenGBuffer);
TEXTURE2D_X(_VisOITOffscreenLighting);

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

    float perc = float(accumulation);
    float maxVal = (float)_VisOITPrefixedHistogramBuffer.Load((DITHER_TILE_TOTAL_PIXELS - 1) << 2);
    return float4(sqrt(perc/maxVal).xxx, 1.0);
}

namespace VisibilityOIT
{

void PackVisibilityData(Visibility::VisibilityData data, uint2 texelCoordinate, float depth, out uint3 packedData)
{
    uint2 packedDataHalfs;
    Visibility::PackVisibilityData(data, packedData.x, packedDataHalfs);
    packedData.y = (packedDataHalfs.x & 0xFF) | (packedDataHalfs.y << 8) | ((uint)(depth * 0xFFFF) << 16);
    packedData.z = (texelCoordinate.x & 0xFFFF) | ((texelCoordinate.y & 0xFFFF) << 16);
}

void UnpackVisibilityData(uint3 packedData, out Visibility::VisibilityData data, out uint2 texelCoordinate, out float depth)
{
    uint2 packedDataHalfs = uint2(packedData.y & 0xFF, (packedData.y >> 8) & 0xFF);
    Visibility::UnpackVisibilityData(packedData.x, packedDataHalfs, data);
    texelCoordinate = uint2(packedData.z & 0xFFFF, (packedData.z >> 16) & 0xFFFF);
    depth = (packedData.y >> 16) * (1.0/(float)0xFFFF);
}

void PackOITGBufferData(float3 normal, float roughness, float3 diffuseAlbedo, out uint4 packedData)
{
    float2 oct = saturate(PackNormalOctRectEncode(normal) * 0.5f + 0.5f);

    packedData.r = PackFloatToUInt(oct.x, 0, 16);
    packedData.g = PackFloatToUInt(oct.y, 0, 16);
    packedData.b = PackFloatToUInt(diffuseAlbedo.r, 0, 8) | PackFloatToUInt(diffuseAlbedo.g, 8, 8);
    packedData.a = PackFloatToUInt(diffuseAlbedo.b, 0, 8) | PackFloatToUInt(roughness, 8, 8);
}

void UnpackOITGBufferData(uint4 packedData, out float3 normal, out float roughness, out float3 diffuseAlbedo)
{
    float2 oct;
    oct.x = UnpackUIntToFloat(packedData.x, 0, 16);
    oct.y = UnpackUIntToFloat(packedData.y, 0, 16);

    normal = normalize(UnpackNormalOctRectEncode(oct * 2.0f - 1.0f));

    diffuseAlbedo.r = UnpackUIntToFloat(packedData.b, 0, 8);
    diffuseAlbedo.g = UnpackUIntToFloat(packedData.b, 8, 8);
    diffuseAlbedo.b = UnpackUIntToFloat(packedData.a, 0, 8);

    roughness = UnpackUIntToFloat(packedData.a, 8, 8);
}

void GetPixelList(uint pixelOffset, out uint listCount, out uint listOffset)
{
    listCount = _VisOITSubListsCounts.Load(pixelOffset << 2);
    listOffset = _VisOITListsOffsets.Load(pixelOffset << 2);
}

void GetVisibilitySample(uint i, uint listOffset, out Visibility::VisibilityData data, out uint2 texelCoordinate, out float depthValue)
{
    uint3 packedData = _VisOITBuffer.Load3(((listOffset + i) * 3) << 2);
    VisibilityOIT::UnpackVisibilityData(packedData, data, texelCoordinate, depthValue);
}

}

#endif
