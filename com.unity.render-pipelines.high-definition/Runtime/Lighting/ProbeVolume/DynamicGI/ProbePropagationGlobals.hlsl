#ifndef PROBE_PROPAGATION_GLOBALS
#define PROBE_PROPAGATION_GLOBALS

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

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

void SetProbeDirty(RWStructuredBuffer<int> buffer, uint probeIndex)
{
    uint index = probeIndex >> 5;
    int bitmask = 1 << (probeIndex & 31);
    InterlockedOr(buffer[index], bitmask);
}

void ClearProbeDirty(RWStructuredBuffer<int> buffer, uint probeIndex)
{
    uint index = probeIndex >> 5;
    int bitmask = 1 << (probeIndex & 31);
    InterlockedAnd(buffer[index], ~bitmask);
}

bool IsProbeDirty(StructuredBuffer<int> buffer, uint probeIndex)
{
    uint index = probeIndex >> 5;
    int bitmask = 1 << (probeIndex & 31);
    return (buffer[index] & bitmask) != 0;
}

bool IsProbeDirty(RWStructuredBuffer<int> buffer, uint probeIndex)
{
    uint index = probeIndex >> 5;
    int bitmask = 1 << (probeIndex & 31);
    return (buffer[index] & bitmask) != 0;
}

#endif // endof PROBE_PROPAGATION_GLOBALS
