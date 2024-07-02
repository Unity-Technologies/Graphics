#ifndef WATER_CURRENT_UTILITIES_H
#define WATER_CURRENT_UTILITIES_H

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Water/ShaderVariablesWater.cs.hlsl"

// Water Current
// NOTE: This should be a structured buffer, however, due to a metal shader translation bug,
// we have to make it a texture. Which sucks, but we don't have a choice.
Texture2D<float4> _WaterSectorData;

// Current map of the first group
TEXTURE2D(_Group0CurrentMap);
SAMPLER(sampler_Group0CurrentMap);

// Current map of the second group
TEXTURE2D(_Group1CurrentMap);
SAMPLER(sampler_Group1CurrentMap);

// Quadrant description
#define NUM_SECTORS 8u
#define SECTOR_SIZE ((2.0 * PI) / NUM_SECTORS)
#define SECTOR_DATA_SAMPLING_OFFSET 0
#define SECTOR_DATA_OTHER_OFFSET 8

// This define uses a LOD sample for vertex and compute
#ifdef SHADER_STAGE_FRAGMENT
#define SAMPLE_TEX2D(textureName, samplerName, coord2) SAMPLE_TEXTURE2D(textureName, samplerName, coord2)
#else
#define SAMPLE_TEX2D(textureName, samplerName, coord2) SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, 0)
#endif

float ConvertAngle_0_2PI(float angle)
{
    angle = angle % (2.0 * PI);
    return angle < 0.0 ? angle + 2.0 * PI : angle;
}

float ConvertAngle_NPI_PPI(float angle)
{
    angle = angle % (2.0 * PI);
    return angle > PI ? angle - 2.0 * PI : (angle < -PI ? angle + 2.0 * PI : angle);
}

float EvaluateAngle(float3 cmpDir, float orientation, float influence, float2 flipDir)
{
    float3 dir = float3((cmpDir.xy * 2.0 - 1.0) * flipDir, cmpDir.z);
    float angle = ConvertAngle_NPI_PPI(atan2(dir.y, dir.x) - orientation);
    if (dir.x == 0 && dir.y == 0) angle = 0;
    return angle * influence * dir.z;
}

struct CurrentData
{
    uint quadrant;
    float proportion;
    float angle;
};

void DecompressDirection(float3 cmpDir, float orientation, float influence, float2 flipDir, out CurrentData currentData)
{
    float angle = EvaluateAngle(cmpDir, orientation, influence, flipDir);
    angle = (angle < 0.0 ? angle + 2.0 * PI : angle);
    float data = angle / SECTOR_SIZE;
    float relativeAngle = frac(data);
    // NOTE: We need to do it this way otherwise metal doesn't compile silently.
    uint uintData = (uint)data;
    currentData.angle = angle;
    currentData.quadrant = uintData > NUM_SECTORS ? uintData - NUM_SECTORS : uintData;
    currentData.proportion = PositivePow((currentData.quadrant % 2u == 0) ? relativeAngle : 1.0 - relativeAngle, 0.75);
}

float2 EvaluateDecalUV(float3 transformedPositionAWS);

float2 EvaluateWaterCurrentUV(float2 currentUV)
{
    // In decal workflow, both current maps share the same region
    float3 positionRWS = mul(ApplyCameraTranslationToMatrix(_WaterSurfaceTransform), float4(currentUV.x, 0, currentUV.y, 1.0)).xyz;
    return EvaluateDecalUV(GetAbsolutePositionWS(positionRWS));
}

float2 EvaluateWaterGroup0CurrentUV(float2 currentUV)
{
#ifdef WATER_DECAL_COMPLETE
    return EvaluateWaterCurrentUV(currentUV);
#else
    return float2(currentUV.x - _Group0CurrentRegionScaleOffset.z, currentUV.y + _Group0CurrentRegionScaleOffset.w) * _Group0CurrentRegionScaleOffset.xy + float2(0.5f, 0.5f);
#endif
}

float2 EvaluateWaterGroup1CurrentUV(float2 currentUV)
{
#ifdef WATER_DECAL_COMPLETE
    return EvaluateWaterCurrentUV(currentUV);
#else
    return float2(currentUV.x - _Group1CurrentRegionScaleOffset.z, currentUV.y + _Group1CurrentRegionScaleOffset.w) * _Group1CurrentRegionScaleOffset.xy + float2(0.5f, 0.5f);
#endif
}

void EvaluateGroup0CurrentData(float2 currentUV, out CurrentData currentData)
{
    float2 uv = EvaluateWaterGroup0CurrentUV(currentUV);

#ifdef WATER_DECAL_COMPLETE
    float3 cmpDir = all(saturate(uv) == uv) ? SAMPLE_TEX2D(_Group0CurrentMap, s_linear_clamp_sampler, uv).xyz : 0;
    DecompressDirection(cmpDir, _GroupOrientation.x, 1, 1, currentData);
#else
    float2 flipDir = sign(_Group0CurrentRegionScaleOffset.xy);
    float3 cmpDir = SAMPLE_TEX2D(_Group0CurrentMap, sampler_Group0CurrentMap, uv).xyz;
    DecompressDirection(cmpDir, _GroupOrientation.x, _CurrentMapInfluence.x, flipDir, currentData);
#endif
}

void EvaluateGroup1CurrentData(float2 currentUV, out CurrentData currentData)
{
    float2 uv = EvaluateWaterGroup1CurrentUV(currentUV);

#ifdef WATER_DECAL_COMPLETE
    float3 cmpDir = all(saturate(uv) == uv) ? SAMPLE_TEX2D(_Group1CurrentMap, s_linear_clamp_sampler, uv).xyz : 0;
    DecompressDirection(cmpDir, _GroupOrientation.y, 1, 1, currentData);
#else
    float2 flipDir = sign(_Group1CurrentRegionScaleOffset.xy);
    float3 cmpDir = SAMPLE_TEX2D(_Group1CurrentMap, sampler_Group1CurrentMap, uv).xyz;
    DecompressDirection(cmpDir, _GroupOrientation.y, _CurrentMapInfluence.y, flipDir, currentData);
#endif
}

float2 SampleWaterGroup0CurrentMap(float2 currentUV)
{
    float2 uv = EvaluateWaterGroup0CurrentUV(currentUV);

#ifdef WATER_DECAL_COMPLETE
    float3 cmpDir = all(saturate(uv) == uv) ? SAMPLE_TEX2D(_Group0CurrentMap, s_linear_clamp_sampler, uv).xyz : 0;
    float angle = EvaluateAngle(cmpDir, _GroupOrientation.x, 1, 1);
#else
    float2 flipDir = sign(_Group0CurrentRegionScaleOffset.xy);
    float3 cmpDir = SAMPLE_TEX2D(_Group0CurrentMap, sampler_Group0CurrentMap, uv).xyz;
    float angle = EvaluateAngle(cmpDir, _GroupOrientation.x, _CurrentMapInfluence.x, flipDir);
#endif

    return float2(cos(angle), sin(angle));
}

float2 SampleWaterGroup1CurrentMap(float2 currentUV)
{
    float2 uv = EvaluateWaterGroup1CurrentUV(currentUV);

#ifdef WATER_DECAL_COMPLETE
    float3 cmpDir = all(saturate(uv) == uv) ? SAMPLE_TEXTURE2D_LOD(_Group1CurrentMap, s_linear_clamp_sampler, uv, 0).xyz : 0;
    float angle = EvaluateAngle(cmpDir, _GroupOrientation.y, 1, 1);
#else
    float2 flipDir = sign(_Group1CurrentRegionScaleOffset.xy);
    float3 cmpDir = SAMPLE_TEX2D(_Group1CurrentMap, sampler_Group1CurrentMap, uv).xyz;
    float angle = EvaluateAngle(cmpDir, _GroupOrientation.y, _CurrentMapInfluence.y, flipDir);
#endif

    return float2(cos(angle), sin(angle));
}

void SwizzleSamplingCoordinates(float2 coord, uint quadrant, out float4 tapCoord)
{
    int tapIndex = quadrant + SECTOR_DATA_SAMPLING_OFFSET;
    float4 dir = _WaterSectorData[int2(tapIndex, 0)];
    tapCoord.xy = float2(dot(coord.xy, dir.xy), dot(coord.xy, float2(-dir.y, dir.x)));
    tapCoord.zw = float2(dot(coord.xy, dir.zw), dot(coord.xy, float2(-dir.w, dir.z)));
}

// Current debug
#define ARROW_TILE_SIZE 4.0

float EvaluateArrow(float2 positionAWS, float2 dir, float2 tileSize)
{
    // Tile coordinate
    float2 tileCoord = frac(positionAWS / tileSize) * 2.0 - 1.0;

    float x = dot(dir, tileCoord);
    float y = dot(float2(dir.y, -dir.x), tileCoord);
    float mask = 0.0;
    // Arrow body
    if (y > -0.1 && y < 0.1 && x > -0.9 && x < 0.5)
        mask = 1.0;
    // Arrow head
    else if (y > -0.4 && y < 0.4 && x > 0.5 && x < 0.9 && (0.4 - (x - 0.5)) / abs(y) > 1)
        mask = 1.0;
    return mask;
}

#endif // WATER_CURRENT_UTILITIES_H
