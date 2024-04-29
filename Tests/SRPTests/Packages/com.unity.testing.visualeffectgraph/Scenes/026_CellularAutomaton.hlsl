#ifndef CELLULARAUTOMATON_H
#define CELLULARAUTOMATON_H
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceFillingCurves.hlsl"

bool TryGetLinearAddress(uint2 index, out uint address)
{
    address = EncodeMorton2D(index);
    return address < 4096;
}

uint2 GetCoordinates(float2 position)
{
    position *= 64.0f;
    position = round(position);
    return (uint2)position;
}

void StoreState(RWStructuredBuffer<float> buffer, float2 position, float state)
{
    uint2 coord = GetCoordinates(position);
    uint address;
    if (TryGetLinearAddress(coord, address))
    {
        buffer[address] = state;
    }
}

uint GetNeighborState(RWStructuredBuffer<float> buffer, float2 position)
{
    int2 coord = GetCoordinates(position);

    int2 north = coord + int2(0u, 1u);
    int2 south = coord - int2(0u, 1u);
    int2 east  = coord + int2(1u, 0u);
    int2 west  = coord - int2(1u, 0u);

    uint count = 0u;

    uint address;
    [branch] if (TryGetLinearAddress((uint2)coord, address)) count += buffer[address] > 0.5f;
    [branch] if (TryGetLinearAddress((uint2)north, address)) count += buffer[address] > 0.5f;
    [branch] if (TryGetLinearAddress((uint2)south, address)) count += buffer[address] > 0.5f;
    [branch] if (TryGetLinearAddress((uint2)east,  address)) count += buffer[address] > 0.5f;
    [branch] if (TryGetLinearAddress((uint2)west,  address)) count += buffer[address] > 0.5f;

    return count;
}


#endif
