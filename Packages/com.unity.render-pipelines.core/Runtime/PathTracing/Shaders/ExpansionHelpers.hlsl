#ifndef _PATHTRACING_EXPANSIONHELPERS_HLSL_
#define _PATHTRACING_EXPANSIONHELPERS_HLSL_

#include "PathTracingCommon.hlsl"

RWStructuredBuffer<uint> g_CompactedGBuffer;
RWStructuredBuffer<uint> g_CompactedGBufferLength; // This will contain the number of texels written.
int g_ChunkOffsetX;
int g_ChunkOffsetY;
int g_ChunkSize;
int g_InstanceWidth;
Texture2D<half2> g_UvFallback;

void CompactGBufferInternal(uint index)
{
    // The dispatch domain is [0; g_ChunkSize-1] in X
    if (index >= (uint)g_ChunkSize)
        return;

    const uint linearChunkOffset = g_ChunkOffsetX + g_ChunkOffsetY * g_InstanceWidth;
    const uint linearDispatch = linearChunkOffset + index;
    const uint2 instanceTexelPos = uint2(linearDispatch % g_InstanceWidth, linearDispatch / g_InstanceWidth);

    if (g_UvFallback[instanceTexelPos].x < 0)
        return;

    uint destinationIndex = 0;
    InterlockedAdd(g_CompactedGBufferLength[0], 1, destinationIndex);
    g_CompactedGBuffer[destinationIndex] = index;
}

#endif
