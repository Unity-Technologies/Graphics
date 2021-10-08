#ifndef PROBE_VOLUME_DYNAMIC_GI
#define PROBE_VOLUME_DYNAMIC_GI

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeShaderVariables.hlsl"

#define PROBE_VOLUME_NEIGHBOR_MAX_HIT_AXIS 16777215

struct PackedNeighborHit
{
    uint indexValidity;
    uint albedoDistance;
    uint normalAxis;
    uint emission;
};

struct PackedNeighborMiss
{
    uint indexValidity;
};

struct NeighborAxisLookup
{
    float3 neighborDirection;
    float sgWeight;
    int index;
};

struct NeighborAxis
{
    uint hitIndexValidity;
};

float4 UnpackAlbedoAndDistance(uint packedVal, float maxNeighborDistance)
{
    float4 outVal;
    outVal.r = ((packedVal >> 0) & 255) / 255.5f;
    outVal.g = ((packedVal >> 8) & 255) / 255.5f;
    outVal.b = ((packedVal >> 16) & 255) / 255.5f;

    outVal.a = ((packedVal >> 24) & 255) / 255.5f;
    outVal.a *= maxNeighborDistance * sqrt(3.0f);

    return outVal;
}

float3 UnpackEmission(uint packedVal)
{
    float3 outVal;
    outVal.r = ((packedVal >> 0) & 255) / 255.0f;
    outVal.g = ((packedVal >> 8) & 255) / 255.0f;
    outVal.b = ((packedVal >> 16) & 255) / 255.0f;

    float multiplier = ((packedVal >> 24) & 255) / 32.0f;

    return outVal * multiplier;
}

float3 UnpackNormal(uint packedVal)
{
    float2 N1212;
    N1212.r = ((packedVal >> 0) & 4095) / 4095.5f;
    N1212.g = ((packedVal >> 12) & 4095) / 4095.5f;

    return UnpackNormalOctQuadEncode(N1212 * 2.0 - 1.0);
}

float4 UnpackAxis(uint packedVal)
{
    // Info is in most significant 8 bit
    uint data = (packedVal >> 24);

    const float diagonalDist = sqrt(3.0f);
    const float diagonal = rcp(diagonalDist);
    const float diagonal2DDist = sqrt(2.0f);
    const float diagonal2D = rcp(diagonal2DDist);


    // Get if is diagonal or primary axis
    int axisType = (int)((data >> 6) & 3);
    int z = (int)((data >> 4) & 3);
    int y = (int)((data >> 2) & 3);
    int x = (int)(data & 3);

    const float channelVal = axisType == 0 ? 1 : axisType == 1 ? diagonal2D : diagonal;

    return float4((x - 1) * channelVal, (y - 1) * channelVal, (z - 1) * channelVal, axisType == 0 ? 1 : axisType == 1 ? diagonal2DDist : diagonalDist);
}

void UnpackIndicesAndValidity(uint packedVal, out uint probeIndex, out uint axisIndex, out float validity)
{
    // 5 bits for axis Index
    axisIndex = packedVal & 31;
    validity = ((packedVal >> 5) & 255) / 255.0f;
    probeIndex = (packedVal >> 13) & 524287;
}

void UnpackIndicesAndValidityOnly(uint packedVal, out uint hitIndex, out float validity)
{
    validity = (packedVal & 255) / 255.0f;
    hitIndex = (packedVal >> 8) & PROBE_VOLUME_NEIGHBOR_MAX_HIT_AXIS;
}

#endif // endof PROBE_VOLUME_DYNAMIC_GI
