#ifndef UNIVERSAL_DEFERRED_INCLUDED
#define UNIVERSAL_DEFERRED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

#define PREFERRED_CBUFFER_SIZE (64 * 1024)
#define SIZEOF_VEC4_TILEDATA 1 // uint4
#define SIZEOF_VEC4_PUNCTUALLIGHTDATA 5 // 5 * float4
#define MAX_DEPTHRANGE_PER_CBUFFER_BATCH (PREFERRED_CBUFFER_SIZE / 4) // Should be ushort, but extra unpacking code is "too expensive"
#define MAX_TILES_PER_CBUFFER_PATCH (PREFERRED_CBUFFER_SIZE / (16 * SIZEOF_VEC4_TILEDATA))
#define MAX_PUNCTUALLIGHT_PER_CBUFFER_BATCH (PREFERRED_CBUFFER_SIZE / (16 * SIZEOF_VEC4_PUNCTUALLIGHTDATA))
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
#elif defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
#define USE_CBUFFER_FOR_DEPTHRANGE 1
#define USE_CBUFFER_FOR_TILELIST 1
#define USE_CBUFFER_FOR_LIGHTDATA 1
#define USE_CBUFFER_FOR_LIGHTLIST 1
#else
#define USE_CBUFFER_FOR_DEPTHRANGE 0
#define USE_CBUFFER_FOR_TILELIST 0
#define USE_CBUFFER_FOR_LIGHTDATA 1
#define USE_CBUFFER_FOR_LIGHTLIST 0
#endif

struct PunctualLightData
{
    float3 posWS;
    float radius2;           // squared radius
    half4 color;
    half4 attenuation;       // .xy are used by DistanceAttenuation - .zw are used by AngleAttenuation (for SpotLights)
    half3 spotDirection;     // spotLights support
    int flags;               // Light flags (enum kLightFlags and LightFlag in C# code)
    half4 occlusionProbeInfo;
    int shadowLightIndex;
};

Light UnityLightFromPunctualLightDataAndWorldSpacePosition(PunctualLightData punctualLightData, float3 positionWS, half4 shadowMask, bool materialFlagReceiveShadowsOff)
{
    // Keep in sync with GetAdditionalPerObjectLight in Lighting.hlsl

    half4 probesOcclusion = shadowMask;

    Light light;

    float3 lightVector = punctualLightData.posWS - positionWS.xyz;
    float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);

    half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));

    half attenuation = DistanceAttenuation(distanceSqr, punctualLightData.attenuation.xy) * AngleAttenuation(punctualLightData.spotDirection.xyz, lightDirection, punctualLightData.attenuation.zw);

    light.direction = lightDirection;
    light.color = punctualLightData.color.rgb;

    light.distanceAttenuation = attenuation;

    // Baked lighting has been set to subtractive, which means:
    // -static geometry do not receive any realtime lighting
    // -dynamic geometry receive realtime lighting and any baked shadows are approximated using occlusion probes.
    #if defined(_DEFERRED_SUBTRACTIVE_LIGHTING)
        // First find the probe channel from the light.
        // Then sample `unity_ProbesOcclusion` for the baked occlusion.
        // If the light is not baked, the channel is -1, and we need to apply no occlusion.

        // probeChannel is the index in 'unity_ProbesOcclusion' that holds the proper occlusion value.
        int probeChannel = punctualLightData.occlusionProbeInfo.x;

        // lightProbeContribution is set to 0 if we are indeed using a probe, otherwise set to 1.
        half lightProbeContribution = punctualLightData.occlusionProbeInfo.y;

        half probeOcclusionValue = probesOcclusion[probeChannel];
        light.distanceAttenuation *= max(probeOcclusionValue, lightProbeContribution);
    #endif

    [branch] if (materialFlagReceiveShadowsOff)
        light.shadowAttenuation = 1.0;
    else
    {
        light.shadowAttenuation = AdditionalLightRealtimeShadow(punctualLightData.shadowLightIndex, positionWS);
        light.shadowAttenuation = ApplyShadowFade(light.shadowAttenuation, positionWS);
    }
    return light;
}

#endif
