#ifndef UNIVERSAL_DEFERRED_INCLUDED
#define UNIVERSAL_DEFERRED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

#define PREFERRED_CBUFFER_SIZE (64 * 1024)
#define SIZEOF_VEC4_TILEDATA 1 // uint4
#define SIZEOF_VEC4_POINTLIGHTDATA 4 // 4 * float4
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
    float radius; // TODO remove/replace?

    float4 color;

    float4 attenuation; // .xy are used by DistanceAttenuation - .zw are used by AngleAttenuation (for SpotLights)

    float3 spotDirection;   // spotLights support
    float padding0;  // TODO find something to put here? (or test other packing schemes?)
};

#define TEST_WIP_DEFERRED_POINT_LIGHTING 1

Light UnityLightFromPointLightDataAndWorldSpacePosition(PointLightData pointLightData, float3 positionWS)
{
    // Keep in sync with GetAdditionalPerObjectLight in Lighting.hlsl

    Light light;

    float3 lightVector = pointLightData.wsPos - positionWS.xyz;
    float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);

    half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));
    
    half attenuation = DistanceAttenuation(distanceSqr, pointLightData.attenuation.xy) * AngleAttenuation(pointLightData.spotDirection.xyz, lightDirection, pointLightData.attenuation.zw);

    light.direction = lightDirection;
    light.color = pointLightData.color.rgb;
    light.distanceAttenuation = attenuation;
    light.shadowAttenuation = 1.0;            // TODO fill with AdditionalLightRealtimeShadow
    return light;
}

#endif
