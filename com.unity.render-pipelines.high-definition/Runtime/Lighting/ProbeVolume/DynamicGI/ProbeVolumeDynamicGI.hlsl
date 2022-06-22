#ifndef PROBE_VOLUME_DYNAMIC_GI
#define PROBE_VOLUME_DYNAMIC_GI

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/ProbeVolumeShaderVariables.hlsl"

#define PROBE_VOLUME_NEIGHBOR_MAX_HIT_AXIS 16777215

struct PackedNeighborHit
{
    uint indexValidity;
    uint albedoDistance;
    uint normalAxis;
    uint emission;
    uint mixedLighting;
};

struct PackedNeighborMiss
{
    uint indexValidity;
};

struct NeighborAxisLookup
{
    float3 neighborDirection;
    float hitWeight;
    float propagationWeight;
    int index;
};

struct NeighborAxis
{
    uint hitIndexValidity;
};

float4 UnpackAlbedoAndDistance(uint packedVal, float maxNeighborDistance)
{
    float4 outVal;
    outVal.r = ((packedVal >> 0) & 255) / 255.0f;
    outVal.g = ((packedVal >> 8) & 255) / 255.0f;
    outVal.b = ((packedVal >> 16) & 255) / 255.0f;

    outVal.a = ((packedVal >> 24) & 255) / 255.0f;
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

// LogLUV

const static float3x3 LOG_LUV_ENCODE_MAT = float3x3(
    0.2209, 0.1138, 0.0102,
    0.3390, 0.6780, 0.1130,
    0.4184, 0.7319, 0.2969);

const static float3x3 LOG_LUV_DECODE_MAT = float3x3(
     6.0014, -1.3320,  0.3008,
    -2.7008,  3.1029, -1.0882,
    -1.7996, -5.7721,  5.6268);

float3 LogluvFromRgb(float3 vRGB)
{
    float3 vResult; 
    float3 Xp_Y_XYZp = mul(LOG_LUV_ENCODE_MAT, vRGB);
    Xp_Y_XYZp = max(Xp_Y_XYZp, float3(1e-6, 1e-6, 1e-6));
    vResult.xy = Xp_Y_XYZp.xy / Xp_Y_XYZp.z;
    // float Le = log2(Xp_Y_XYZp.y) * (2.0 / 255.0) + (127.0 / 255.0); // original super large range
    float Le = log2(Xp_Y_XYZp.y) * (1.0 / (20.0 + 16.61)) + (16.61 / (20.0 + 16.61)); // map ~[1e-5, 1M] to [0, 1] range
    vResult.z = Le;
    return vResult;
}

float3 RgbFromLogluv(float3 vLogLuv)
{
    float3 Xp_Y_XYZp;
    // Xp_Y_XYZp.y = exp2(vLogLuv.z * 127.5 - 63.5); // original super large range
    Xp_Y_XYZp.y = exp2(vLogLuv.z * (20.0 + 16.61) - 16.61); // map [0, 1] to ~[1e-5, 1M] range
    Xp_Y_XYZp.z = Xp_Y_XYZp.y / vLogLuv.y;
    Xp_Y_XYZp.x = vLogLuv.x * Xp_Y_XYZp.z;
    float3 vRGB = mul(LOG_LUV_DECODE_MAT, Xp_Y_XYZp);
    return max(vRGB, 0.0);
}

// Radiance encoding

#ifdef RADIANCE_ENCODING_LOGLUV
    #define RADIANCE uint
#else
    #define RADIANCE float3
#endif

RADIANCE ZeroRadiance()
{
    return 0;
}

RADIANCE EncodeRadiance(float3 color)
{
#ifdef RADIANCE_ENCODING_LOGLUV
    RADIANCE packedOutput = 0;
    float3 logLuv = LogluvFromRgb(color);
    packedOutput |= min(255, (uint)round(logLuv.x * 255)) << 0;
    packedOutput |= min(255, (uint)round(logLuv.y * 255)) << 8;
    packedOutput |= min(65535, (uint)round(logLuv.z * 65535)) << 16;
    return packedOutput;
#else
    return color;
#endif
}

float3 DecodeRadiance(RADIANCE packedVal)
{
#ifdef RADIANCE_ENCODING_LOGLUV
    float3 outVal;
    outVal.x = ((packedVal >> 0) & 255) / 255.0f;
    outVal.y = ((packedVal >> 8) & 255) / 255.0f;
    outVal.z = ((packedVal >> 16) & 65535) / 65535.0f;
    return RgbFromLogluv(outVal);
#else
    return packedVal;
#endif
}

#endif // endof PROBE_VOLUME_DYNAMIC_GI
