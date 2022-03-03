#ifndef VISIBILITY_HLSL
#define VISIBILITY_HLSL

#include "Packages/com.unity.render-pipelines.core/Runtime/GeometryPool/Resources/GeometryPool.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.cs.hlsl"

TEXTURE2D_X_UINT(_VisBufferTexture0);
TEXTURE2D_X_UINT2(_VisBufferTexture1);

TEXTURE2D_X_UINT(_VisBufferFeatureTiles);
TEXTURE2D_X_UINT2(_VisBufferMaterialTiles);
TEXTURE2D_X_UINT(_VisBufferBucketTiles);

#define VIS_BUFFER_TILE_LOG2 6
#define VIS_BUFFER_TILE_SIZE (1 <<  VIS_BUFFER_TILE_LOG2) //64

namespace Visibility
{

#define InvalidVisibilityData 0

struct VisibilityData
{
    bool valid;
    uint DOTSInstanceIndex;
    uint primitiveID;
    uint batchID;
};

float3 DebugVisIndexToRGB(uint index, uint maxCol = 512)
{
    if (index == 0)
        return float3(0, 0, 0);

    float indexf = sin(816.0f * (index % maxCol)) * 2.0;
    {
        indexf = frac(indexf * 0.011);
        indexf *= indexf + 7.5;
        indexf *= indexf + indexf;
        indexf = frac(indexf);
    }

    float H = indexf;

    //standard hue to HSV
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R,G,B));
}



void PackVisibilityData(in VisibilityData data, out uint packedData0, out uint2 packedData1)
{
    packedData0 = 0;
    packedData0 |= (data.DOTSInstanceIndex & 0xffff);
    packedData0 |= (data.primitiveID & 0x7fff) << 16;
    packedData0 |= (data.valid ? 1 : 0) << 31;
    packedData1.x = data.batchID;
    packedData1.y = (data.primitiveID >> 15) & 0xFF;
}

void UnpackVisibilityData(uint packedData0, uint2 packedData1, out VisibilityData data)
{
    data.valid = (packedData0 >> 31) != 0;
    data.DOTSInstanceIndex = (packedData0 & 0xffff);
    data.primitiveID = ((packedData0 >> 16) & 0x7fff ) | (packedData1.y << 15);
    data.batchID = packedData1.x;

}

uint GetMaterialKey(in VisibilityData visData, out GeoPoolMetadataEntry metadataEntry)
{
    ZERO_INITIALIZE(GeoPoolMetadataEntry, metadataEntry);
    if (visData.valid)
        metadataEntry = GeometryPool::GetMetadataEntry(visData.DOTSInstanceIndex, visData.batchID);

    return visData.valid ? GeometryPool::GetMaterialKey(metadataEntry, visData.primitiveID) : 0;
}

VisibilityData LoadVisibilityData(uint2 coord)
{
    uint value0 = LOAD_TEXTURE2D_X(_VisBufferTexture0, (uint2)coord.xy).x;
    uint2 value1 = LOAD_TEXTURE2D_X(_VisBufferTexture1, (uint2)coord.xy).xy;
    VisibilityData visData;
    ZERO_INITIALIZE(VisibilityData, visData);
    Visibility::UnpackVisibilityData(value0, value1, visData);
    return visData;
}

uint GetMaterialKey(in VisibilityData visData)
{
    GeoPoolMetadataEntry unused;
    ZERO_INITIALIZE(GeoPoolMetadataEntry, unused);
    return GetMaterialKey(visData, unused);
}

uint2 GetTileCoord(uint2 coord)
{
    return coord >> VIS_BUFFER_TILE_LOG2;
}

uint LoadFeatureTile(uint2 tileCoord)
{
    return LOAD_TEXTURE2D_X(_VisBufferFeatureTiles, tileCoord).x;
}

uint2 LoadMaterialTile(uint2 tileCoord)
{
    return LOAD_TEXTURE2D_X(_VisBufferMaterialTiles, tileCoord).xy;
}

uint LoadBucketTile(uint2 tileCoord)
{
    return LOAD_TEXTURE2D_X(_VisBufferBucketTiles, tileCoord).x;
}

float PackDepthMaterialKey(uint materialGPUBatchKey)
{
    return float(materialGPUBatchKey & 0xffffff) / (float)0xffffff;
}

uint GetLightTileCategory(uint featureFlags)
{
    if ((featureFlags & VBUFFER_LIGHTING_FEATURES_ENV) !=0 && (featureFlags & ~VBUFFER_LIGHTING_FEATURES_ENV) == 0)
        return LIGHTVBUFFERTILECATEGORY_ENV;
    if ((featureFlags & VBUFFER_LIGHTING_FEATURES_ENV_PUNCTUAL) !=0 && (featureFlags & ~VBUFFER_LIGHTING_FEATURES_ENV_PUNCTUAL) == 0)
        return LIGHTVBUFFERTILECATEGORY_ENV_PUNCTUAL;
    if ((featureFlags & VBUFFER_LIGHTING_FEATURES_EVERYTHING) !=0 && (featureFlags & ~VBUFFER_LIGHTING_FEATURES_EVERYTHING) == 0)
        return LIGHTVBUFFERTILECATEGORY_EVERYTHING;
    return LIGHTVBUFFERTILECATEGORY_UNKNOWN;
}

}

#endif
