#ifndef UNIVERSAL_DEFERRED_INCLUDED
#define UNIVERSAL_DEFERRED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

#define PREFERRED_CBUFFER_SIZE (64 * 1024)
#define SIZEOF_VEC4_TILEDATA 1 // uint4
#define SIZEOF_VEC4_POINTLIGHTDATA 2 // 2 float4
#define MAX_DEPTHRANGE_PER_CBUFFER_BATCH (PREFERRED_CBUFFER_SIZE / 4) // Should be ushort, but extra unpacking code is "too expensive"
#define MAX_TILES_PER_CBUFFER_PATCH (PREFERRED_CBUFFER_SIZE / (16 * SIZEOF_VEC4_TILEDATA))
#define MAX_POINTLIGHT_PER_CBUFFER_BATCH (PREFERRED_CBUFFER_SIZE / (16 * SIZEOF_VEC4_POINTLIGHTDATA))
#define MAX_REL_LIGHT_INDICES_PER_CBUFFER_BATCH (PREFERRED_CBUFFER_SIZE / 4) // Should be ushort, but extra unpacking code is "too expensive"

// Keep in sync with kUseCBufferForDepthRange.
// Keep in sync with kUseCBufferForTileData.
// Keep in sync with kUseCBufferForLightData.
// Keep in sync with kUseCBufferForLightList.
#if defined(SHADER_API_SWITCH)
#define USE_CBUFFER_FOR_DEPTHRANGE 0
#define USE_CBUFFER_FOR_TILELIST 0
#define USE_CBUFFER_FOR_LIGHTDATA 1
#define USE_CBUFFER_FOR_LIGHTLIST 0
#else
#define USE_CBUFFER_FOR_DEPTHRANGE 0
#define USE_CBUFFER_FOR_TILELIST 0
#define USE_CBUFFER_FOR_LIGHTDATA 1
#define USE_CBUFFER_FOR_LIGHTLIST 0
#endif

struct PointLightData
{
    float3 wsPos;
    float radius;
    float4 color;
};

#define TEST_WIP_DEFERRED_POINT_LIGHTING 0

Light UnityLightFromPointLightDataAndWorldSpacePosition(PointLightData pointLightData, float3 wsPos)
{
    Light light;
    light.direction = pointLightData.wsPos - wsPos.xyz; // TODO adjust direction
    light.color = pointLightData.color.rgb;             // TODO adjust color
    light.distanceAttenuation = pointLightData.radius;  // TODO adjust attenuation
    light.shadowAttenuation = 0.1;                      // TODO adjust shadowAttenuation
    return light;
}

#endif
