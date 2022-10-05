#ifndef PROBE_PROPAGATION_GLOBALS
#define PROBE_PROPAGATION_GLOBALS

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

float4 _ProbeVolumeResolution;
float4 _ProbeVolumeBlockResolution;

#define BLOCK_SIZE 4

// TODO: Generate from C#
#define NEIGHBOR_AXIS_COUNT     26
#define NEIGHBOR_AXIS_DECLARATION  \
    const int3 NeighborAxisOffset[NEIGHBOR_AXIS_COUNT] = {    \
        int3(1, 0, 0),                 \
        int3(1, 0, 1),                 \
        int3(1, 0, -1),                 \
        int3(-1, 0, 0),                \
        int3(-1, 0, 1),                 \
        int3(-1, 0, -1),                \
        int3(0, 0, 1),                \
        int3(0, 0, -1),                \
        \
        int3(0, 1, 0),                 \
        int3(1, 1, 0),                \
        int3(1, 1, 1),                \
        int3(1, 1, -1),               \
        int3(-1, 1, 0),               \
        int3(-1, 1, 1),               \
        int3(-1, 1, -1),               \
        int3(0, 1, 1),               \
        int3(0, 1, -1),               \
        \
        int3(0, -1, 0),                \
        int3(1, -1, 0),               \
        int3(1, -1, 1),               \
        int3(1, -1, -1),               \
        int3(-1, -1, 0),               \
        int3(-1, -1, 1),               \
        int3(-1, -1, -1),               \
        int3(0, -1, 1),               \
        int3(0, -1, -1)               \
    };

int3 GetNeighborAxisOffset(int i)
{
    NEIGHBOR_AXIS_DECLARATION;
    return NeighborAxisOffset[i];
}

void SetProbeDirty(RWStructuredBuffer<int> buffer, uint paddedProbeIndex)
{
    uint index = paddedProbeIndex >> 5;
    int bitmask = 1 << (paddedProbeIndex & 31);
    InterlockedOr(buffer[index], bitmask);
}

void ClearProbeDirty(RWStructuredBuffer<int> buffer, uint paddedProbeIndex)
{
    uint index = paddedProbeIndex >> 5;
    int bitmask = 1 << (paddedProbeIndex & 31);
    InterlockedAnd(buffer[index], ~bitmask);
}

bool IsProbeDirty(StructuredBuffer<int> buffer, uint paddedProbeIndex)
{
    uint index = paddedProbeIndex >> 5;
    int bitmask = 1 << (paddedProbeIndex & 31);
    return (buffer[index] & bitmask) != 0;
}

bool IsProbeDirty(RWStructuredBuffer<int> buffer, uint paddedProbeIndex)
{
    uint index = paddedProbeIndex >> 5;
    int bitmask = 1 << (paddedProbeIndex & 31);
    return (buffer[index] & bitmask) != 0;
}

bool IsBoundaryProbe(uint3 probeCoordinate)
{
    return any(probeCoordinate == 0) || any(probeCoordinate + 1 == (uint3)_ProbeVolumeResolution);
}

uint3 ProbeIndexToProbeCoordinates(uint probeIndex)
{
    const uint2 resolution = (uint2)_ProbeVolumeResolution.xy;
    uint probeZ = probeIndex / (resolution.x * resolution.y);
    probeIndex -= probeZ * (resolution.x * resolution.y);

    uint probeY = probeIndex / resolution.x;
    uint probeX = probeIndex % resolution.x;

    return uint3(probeX, probeY, probeZ);
}

uint CoordinateToIndex(uint3 coordinate, uint2 resolution)
{
    return coordinate.z * (resolution.x * resolution.y) + coordinate.y * resolution.x + coordinate.x;
}

uint GroupAndThreadToPaddedProbeIndex(uint3 group, uint3 thread)
{
    const uint blockIndex = CoordinateToIndex(group, (uint2)_ProbeVolumeBlockResolution.xy);

    return blockIndex * (BLOCK_SIZE * BLOCK_SIZE * BLOCK_SIZE) + CoordinateToIndex(thread, BLOCK_SIZE);
}

uint GroupAndIndexToPaddedProbeIndex(uint3 group, uint threadIndex)
{
    const uint blockIndex = CoordinateToIndex(group, (uint2)_ProbeVolumeBlockResolution.xy);

    return blockIndex * (BLOCK_SIZE * BLOCK_SIZE * BLOCK_SIZE) + threadIndex;
}

uint ProbeCoordinateToPaddedProbeIndex(uint3 probeCoordinate)
{
    const uint3 group = probeCoordinate / BLOCK_SIZE;
    const uint3 thread = probeCoordinate - group * BLOCK_SIZE;

    return GroupAndThreadToPaddedProbeIndex(group, thread);
}

uint ProbeCoordinateToProbeIndex(uint3 probeCoordinate)
{
    return CoordinateToIndex(probeCoordinate, (uint2)_ProbeVolumeResolution.xy);
}

uint PaddedProbeCount()
{
    return (uint)_ProbeVolumeBlockResolution.w * (BLOCK_SIZE * BLOCK_SIZE * BLOCK_SIZE);
}

uint ProbeCount()
{
    return (uint)_ProbeVolumeResolution.w;
}

#endif // endof PROBE_PROPAGATION_GLOBALS
