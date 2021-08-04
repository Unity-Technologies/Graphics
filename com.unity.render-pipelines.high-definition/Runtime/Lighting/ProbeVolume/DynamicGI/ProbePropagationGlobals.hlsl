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

#endif // endof PROBE_PROPAGATION_GLOBALS
