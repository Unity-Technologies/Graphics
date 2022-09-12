#ifndef PROBE_PROPAGATION_GLOBALS
#define PROBE_PROPAGATION_GLOBALS

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

float3 _ProbeVolumeResolution;

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

uint CoordinateToIndex(uint3 coordinate, uint3 resolution)
{
    return coordinate.z * (resolution.x * resolution.y) + coordinate.y * resolution.x + coordinate.x;
}

uint ProbeCoordinateToProbeIndex(uint3 probeCoordinate)
{
    const uint3 resolution = (uint3)_ProbeVolumeResolution;
    return CoordinateToIndex(probeCoordinate, resolution);
}

uint ProbeCoordinateToPaddedProbeIndex(uint3 probeCoordinate)
{
    const uint3 resolution = (uint3)_ProbeVolumeResolution;
    const uint3 blockCoordinate = probeCoordinate / BLOCK_SIZE;
    const uint3 blockCounts = (resolution + (BLOCK_SIZE - 1)) / BLOCK_SIZE;
    const uint3 inBlockCoordinate = probeCoordinate - blockCoordinate * BLOCK_SIZE;
    const uint3 blockStart = CoordinateToIndex(blockCoordinate, blockCounts) * (BLOCK_SIZE * BLOCK_SIZE * BLOCK_SIZE);
    
    return blockStart + CoordinateToIndex(inBlockCoordinate, BLOCK_SIZE);
}

uint3 ProbeIndexToProbeCoordinates(uint probeIndex)
{
    uint probeZ = probeIndex / (_ProbeVolumeResolution.x * _ProbeVolumeResolution.y);
    probeIndex -= probeZ * (_ProbeVolumeResolution.x * _ProbeVolumeResolution.y);

    uint probeY = probeIndex / _ProbeVolumeResolution.x;
    uint probeX = probeIndex % _ProbeVolumeResolution.x;

    return uint3(probeX, probeY, probeZ);
}

#endif // endof PROBE_PROPAGATION_GLOBALS
