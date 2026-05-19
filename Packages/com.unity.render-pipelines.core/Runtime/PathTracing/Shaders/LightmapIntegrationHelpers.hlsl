#ifndef _PATHTRACING_LIGHTMAPINTEGRATIONHELPERS_HLSL_
#define _PATHTRACING_LIGHTMAPINTEGRATIONHELPERS_HLSL_

#include "PathTracingCommon.hlsl"

StructuredBuffer<HitEntry> g_GBuffer;
RWStructuredBuffer<uint> g_CompactedGBuffer;
RWStructuredBuffer<uint> g_CompactedGBufferLength; // This will contain the number of texels written.
int g_MaxLocalSampleCount; // Don't take more than this many samples per texel in this dispatch, sometimes the last dispatch will have fewer samples as the expanded sample count may not be a multiple of the total sample count
int g_ExpandedTexelSampleWidth; // The number of samples per texel in the expanded buffers
int g_InstanceWidth;
int g_ChunkOffsetX;
int g_ChunkOffsetY;
int g_InstanceGeometryIndex;
int g_TerrainIndex;
float4x4 g_ShaderLocalToWorld;
float4x4 g_ShaderLocalToWorldNormals;

#ifdef TERRAIN_RAY_MARCHING_ENABLED
bool GetExpandedSampleTerrain(uint dispatchIndex, out float3 worldPosition, out float3 worldNormal, out float3 worldFaceNormal, out float2 uv)
{
    if (!g_GBuffer[dispatchIndex].IsValid())
    {
        worldPosition = worldNormal = worldFaceNormal = 0;
        uv = 0;
        return false;
    }

    // Reconstruct UV from quad mesh barycentrics.
    // Quad: v0=(0,0), v1=(1,0), v2=(1,1), v3=(0,1)
    // Triangle 0: v0,v2,v1 (indices 0,2,1)  Triangle 1: v0,v3,v2 (indices 0,3,2)
    uint primIndex = g_GBuffer[dispatchIndex].primitiveIndex;
    float2 bary = g_GBuffer[dispatchIndex].barycentrics;
    float2 p0 = float2(0, 0);
    float2 p1 = primIndex == 0 ? float2(1, 1) : float2(0, 1);
    float2 p2 = primIndex == 0 ? float2(1, 0) : float2(1, 1);
    uv = p0 * (1.0 - bary.x - bary.y) + p1 * bary.x + p2 * bary.y;

    // Compute position and normal from heightmap using shared function.
    // Barycentrics give UVMesh-space [0,1]. Cell coordinates are [0, resolution-1].
    UnifiedRT::TerrainData terrainData = g_TerrainList[g_TerrainIndex];
    float numCells = 1.0 / terrainData.invHeightmapWidthInTexels - 1.0;
    float2 heightmapUV = uv * numCells;

    float3 localPos, localNormal;
    ComputeTerrainLocalPosAndNormal(terrainData, g_TerrainIndex, heightmapUV, localPos, localNormal);

    worldPosition = mul(g_ShaderLocalToWorld, float4(localPos, 1)).xyz;
    worldNormal = normalize(mul((float3x3)g_ShaderLocalToWorldNormals, localNormal));
    worldFaceNormal = worldNormal;
    return true;
}
#endif

bool GetExpandedSample(uint dispatchIndex, out uint localSampleOffset, out uint2 instanceTexelPos, inout float3 worldPosition, inout float3 worldNormal, inout float3 worldFaceNormal)
{
    localSampleOffset = dispatchIndex % g_ExpandedTexelSampleWidth;
    instanceTexelPos = 0;
    if (localSampleOffset >= (uint)g_MaxLocalSampleCount)
        return false; // no more samples to process

    const uint compactedTexelIndex = dispatchIndex / g_ExpandedTexelSampleWidth;
    const uint texelIndex = g_CompactedGBuffer[compactedTexelIndex];
    const uint linearChunkOffset = g_ChunkOffsetY * g_InstanceWidth + g_ChunkOffsetX;
    const uint linearTexelIndex = texelIndex + linearChunkOffset;
    instanceTexelPos = uint2(linearTexelIndex % g_InstanceWidth, linearTexelIndex / g_InstanceWidth);

#ifdef TERRAIN_RAY_MARCHING_ENABLED
    if (g_InstanceGeometryIndex == -1)
    {
        float2 uv;
        return GetExpandedSampleTerrain(dispatchIndex, worldPosition, worldNormal, worldFaceNormal, uv);
    }
#endif

    if (!g_GBuffer[dispatchIndex].IsValid())
        return false; // no intersection found, skip this sample

    UnifiedRT::Hit hit;
    hit.instanceID = g_GBuffer[dispatchIndex].instanceID;
    hit.primitiveIndex = g_GBuffer[dispatchIndex].primitiveIndex;
    hit.uvBarycentrics = g_GBuffer[dispatchIndex].barycentrics;

    FetchGeomAttributes(hit, g_InstanceGeometryIndex, worldPosition, worldNormal, worldFaceNormal);

    worldPosition = mul(g_ShaderLocalToWorld, float4(worldPosition, 1)).xyz;
    worldNormal = normalize(mul((float3x3)g_ShaderLocalToWorldNormals, worldNormal));
    worldFaceNormal = normalize(mul((float3x3)g_ShaderLocalToWorldNormals, worldFaceNormal));
    return true;
}

bool GetExpandedSample(uint dispatchIndex, inout float3 worldPosition, inout float3 worldNormal, inout float3 worldFaceNormal, inout float2 uv1)
{
#ifdef TERRAIN_RAY_MARCHING_ENABLED
    if (g_InstanceGeometryIndex == -1)
        return GetExpandedSampleTerrain(dispatchIndex, worldPosition, worldNormal, worldFaceNormal, uv1);
#endif

    if (!g_GBuffer[dispatchIndex].IsValid())
        return false; // no intersection found, skip this sample

    UnifiedRT::Hit hit;
    hit.instanceID = g_GBuffer[dispatchIndex].instanceID;
    hit.primitiveIndex = g_GBuffer[dispatchIndex].primitiveIndex;
    hit.uvBarycentrics = g_GBuffer[dispatchIndex].barycentrics;

    FetchGeomAttributes(hit, g_InstanceGeometryIndex, worldPosition, worldNormal, worldFaceNormal, uv1);

    worldPosition = mul(g_ShaderLocalToWorld, float4(worldPosition, 1)).xyz;
    worldNormal = normalize(mul((float3x3)g_ShaderLocalToWorldNormals, worldNormal));
    worldFaceNormal = normalize(mul((float3x3)g_ShaderLocalToWorldNormals, worldFaceNormal));
    return true;
}

#endif
