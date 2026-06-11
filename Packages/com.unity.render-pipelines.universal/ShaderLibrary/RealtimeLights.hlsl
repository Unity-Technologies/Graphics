
#ifndef UNIVERSAL_REALTIME_LIGHTS_INCLUDED
#define UNIVERSAL_REALTIME_LIGHTS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/AmbientOcclusion.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LightCookie/LightCookie.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Clustering.hlsl"

// Abstraction over Light shading data.
struct Light
{
    half3   direction;
    half3   color;
    float   distanceAttenuation; // full-float precision required on some platforms
    half    shadowAttenuation;
    uint    layerMask;
};

#if USE_CLUSTER_LIGHT_LOOP && defined(LIGHTMAP_ON) && defined(LIGHTMAP_SHADOW_MIXING)
#define CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK if (_AdditionalLightsColor[lightIndex].a > 0.0h) continue;
#else
#define CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
#endif


#if defined(UNITY_PLATFORM_META_QUEST) && META_QUEST_LIGHTUNROLL
	#define UNROLL_ONELIGHT [unroll(1)]
#else
	#define UNROLL_ONELIGHT
#endif

#if USE_CLUSTER_LIGHT_LOOP
    #define LIGHT_LOOP_BEGIN(lightCount) { \
    uint lightIndex; \
    ClusterIterator _urp_internal_clusterIterator = ClusterInit(inputData.normalizedScreenSpaceUV, inputData.positionWS, 0); \
    [loop] while (ClusterNext(_urp_internal_clusterIterator, lightIndex)) { \
        lightIndex += URP_FP_DIRECTIONAL_LIGHTS_COUNT; \
        CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
    #define LIGHT_LOOP_END } }
#else
    #define LIGHT_LOOP_BEGIN(lightCount) \
    UNROLL_ONELIGHT \
    for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex) {
    #define LIGHT_LOOP_END }
#endif

///////////////////////////////////////////////////////////////////////////////
//                        Attenuation Functions                               /
///////////////////////////////////////////////////////////////////////////////

// Matches Unity Vanilla HINT_NICE_QUALITY attenuation
// Attenuation smoothly decreases to light range.
#if (UNITY_PLATFORM_META_QUEST) // This is platform specific change targeting performance only
float DistanceAttenuation(float distanceSqr, half2 distanceAttenuation, float distRsqrt)
#else
float DistanceAttenuation(float distanceSqr, half2 distanceAttenuation)
#endif
{
    // We use a shared distance attenuation for additional directional and puctual lights
    // for directional lights attenuation will be 1
#if (UNITY_PLATFORM_META_QUEST) // This is platform specific change targeting performance only
    // distRsqrt is rsqrt(distanceSqr), already computed at the call site for light direction
    // normalization. We reuse it here: distRsqrt² = rsqrt(d²)² = 1/d², avoiding a separate
    // rcp(distanceSqr) call and saving one EFU (complex) instruction per light.
    float lightAtten = distRsqrt * distRsqrt;
#else
    float lightAtten = rcp(distanceSqr);
#endif
    float2 distanceAttenuationFloat = float2(distanceAttenuation);

    // Use the smoothing factor also used in the Unity lightmapper.
    half factor = half(distanceSqr * distanceAttenuationFloat.x);
    half smoothFactor = saturate(half(1.0) - factor * factor);
    smoothFactor = smoothFactor * smoothFactor;

    return lightAtten * smoothFactor;
}

half AngleAttenuation(half3 spotDirection, half3 lightDirection, half2 spotAttenuation)
{
    // Spot Attenuation with a linear falloff can be defined as
    // (SdotL - cosOuterAngle) / (cosInnerAngle - cosOuterAngle)
    // This can be rewritten as
    // invAngleRange = 1.0 / (cosInnerAngle - cosOuterAngle)
    // SdotL * invAngleRange + (-cosOuterAngle * invAngleRange)
    // SdotL * spotAttenuation.x + spotAttenuation.y

    // If we precompute the terms in a MAD instruction
    half SdotL = dot(spotDirection, lightDirection);
    half atten = saturate(SdotL * spotAttenuation.x + spotAttenuation.y);
    return atten * atten;
}

///////////////////////////////////////////////////////////////////////////////
//                      Light Abstraction                                    //
///////////////////////////////////////////////////////////////////////////////

Light GetMainLight()
{
    Light light;
    light.direction = half3(_MainLightPosition.xyz);
#if USE_CLUSTER_LIGHT_LOOP
#if defined(LIGHTMAP_ON) && defined(LIGHTMAP_SHADOW_MIXING)
    light.distanceAttenuation = _MainLightColor.a;
#else
    light.distanceAttenuation = 1.0;
#endif
#else
    light.distanceAttenuation = unity_LightData.z; // unity_LightData.z is 1 when not culled by the culling mask, otherwise 0.
#endif
    light.shadowAttenuation = 1.0;
    light.color = _MainLightColor.rgb;

    light.layerMask = _MainLightLayerMask;

    return light;
}

Light GetMainLight(float4 shadowCoord)
{
    Light light = GetMainLight();
    light.shadowAttenuation = MainLightRealtimeShadow(shadowCoord);
    return light;
}

Light GetMainLight(float4 shadowCoord, float3 positionWS, half4 shadowMask)
{
    Light light = GetMainLight();
    light.shadowAttenuation = MainLightShadow(shadowCoord, positionWS, shadowMask, _MainLightOcclusionProbes);

    #if defined(_LIGHT_COOKIES)
        real3 cookieColor = SampleMainLightCookie(positionWS);
        light.color *= cookieColor;
    #endif

    return light;
}

Light GetMainLight(InputData inputData, half4 shadowMask, AmbientOcclusionFactor aoFactor)
{
    Light light = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);

    #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_AMBIENT_OCCLUSION))
    {
        light.color *= aoFactor.directAmbientOcclusion;
    }
    #endif

    return light;
}

// Fills a light struct given a perObjectLightIndex
Light GetAdditionalPerObjectLight(int perObjectLightIndex, float3 positionWS)
{
    // Abstraction over Light input constants
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    float4 lightPositionWS = _AdditionalLightsBuffer[perObjectLightIndex].position;
    half3 color = _AdditionalLightsBuffer[perObjectLightIndex].color.rgb;
    half4 distanceAndSpotAttenuation = _AdditionalLightsBuffer[perObjectLightIndex].attenuation;
    half4 spotDirection = _AdditionalLightsBuffer[perObjectLightIndex].spotDirection;
    uint lightLayerMask = _AdditionalLightsBuffer[perObjectLightIndex].layerMask;
#else
    float4 lightPositionWS = _AdditionalLightsPosition[perObjectLightIndex];
    half3 color = _AdditionalLightsColor[perObjectLightIndex].rgb;
    half4 distanceAndSpotAttenuation = _AdditionalLightsAttenuation[perObjectLightIndex];
    half4 spotDirection = _AdditionalLightsSpotDir[perObjectLightIndex];
    uint lightLayerMask = asuint(_AdditionalLightsLayerMasks[perObjectLightIndex]);
#endif

    // Directional lights store direction in lightPosition.xyz and have .w set to 0.0.
    // This way the following code will work for both directional and punctual lights.
    float3 lightVector = lightPositionWS.xyz - positionWS * lightPositionWS.w;
    float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);

#if (UNITY_PLATFORM_META_QUEST) // This is platform specific change targeting performance only
    float distRsqrt = rsqrt(distanceSqr);
    half3 lightDirection = half3(lightVector * distRsqrt);
    float distAtten = DistanceAttenuation(distanceSqr, distanceAndSpotAttenuation.xy, distRsqrt);
#else
    half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));
    float distAtten = DistanceAttenuation(distanceSqr, distanceAndSpotAttenuation.xy);
#endif

#if (META_QUEST_NO_SPOTLIGHTS_LIGHT_LOOP)
    // full-float precision required on some platforms
    float attenuation = distAtten;
#else
    // full-float precision required on some platforms
    float attenuation = distAtten * AngleAttenuation(spotDirection.xyz, lightDirection, distanceAndSpotAttenuation.zw);
#endif

    Light light;
    light.direction = lightDirection;
    light.distanceAttenuation = attenuation;
    light.shadowAttenuation = 1.0; // This value can later be overridden in GetAdditionalLight(uint i, float3 positionWS, half4 shadowMask)
    light.color = color;
    light.layerMask = lightLayerMask;

    return light;
}

uint GetPerObjectLightIndexOffset()
{
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    return uint(unity_LightData.x);
#else
    return 0;
#endif
}

// Returns a per-object index given a loop index.
// This abstract the underlying data implementation for storing lights/light indices
int GetPerObjectLightIndex(uint index)
{
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
/////////////////////////////////////////////////////////////////////////////////////////////
// Structured Buffer Path                                                                   /
//                                                                                          /
// Lights and light indices are stored in StructuredBuffer. We can just index them.         /
// Currently all non-mobile platforms take this path :(                                     /
// There are limitation in mobile GPUs to use SSBO (performance / no vertex shader support) /
/////////////////////////////////////////////////////////////////////////////////////////////
    uint offset = uint(unity_LightData.x);
    return _AdditionalLightsIndices[offset + index];
#else
/////////////////////////////////////////////////////////////////////////////////////////////
// UBO path                                                                                 /
//                                                                                          /
// We pack 8 x 16bit uint light indices into float4 unity_PackedLightIndices;               /
// light index 0 is packed into lower 16 bits of unity_PackedLightIndices.x,                /
// light index 1 is packed into high 16 bits of unity_PackedLightIndices.x and so on        /
/////////////////////////////////////////////////////////////////////////////////////////////
    uint4 packed4 = asuint(unity_PackedLightIndices);
    uint2 pair = index >= 4 ? packed4.zw : packed4.xy;
    uint word = (index & 2) ? pair.y : pair.x;
    return (word >> ((index & 1) << 4)) & 0xFFFF;    
#endif
}

// Fills a light struct given a loop i index. This will convert the i
// index to a perObjectLightIndex
Light GetAdditionalLight(uint i, float3 positionWS)
{
#if USE_CLUSTER_LIGHT_LOOP
    int lightIndex = i;
#else
    int lightIndex = GetPerObjectLightIndex(i);
#endif
    return GetAdditionalPerObjectLight(lightIndex, positionWS);
}

Light GetAdditionalLight(uint i, float3 positionWS, half4 shadowMask)
{
#if USE_CLUSTER_LIGHT_LOOP
    int lightIndex = i;
#else
    int lightIndex = GetPerObjectLightIndex(i);
#endif
    Light light = GetAdditionalPerObjectLight(lightIndex, positionWS);

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    half4 occlusionProbeChannels = _AdditionalLightsBuffer[lightIndex].occlusionProbeChannels;
#else
    half4 occlusionProbeChannels = _AdditionalLightsOcclusionProbes[lightIndex];
#endif
    light.shadowAttenuation = AdditionalLightShadow(lightIndex, positionWS, light.direction, shadowMask, occlusionProbeChannels);
#if defined(_LIGHT_COOKIES)
    real3 cookieColor = SampleAdditionalLightCookie(lightIndex, positionWS);
    light.color *= cookieColor;
#endif

    return light;
}

Light GetAdditionalLight(uint i, InputData inputData, half4 shadowMask, AmbientOcclusionFactor aoFactor)
{
    Light light = GetAdditionalLight(i, inputData.positionWS, shadowMask);

    #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
    if (IsLightingFeatureEnabled(DEBUGLIGHTINGFEATUREFLAGS_AMBIENT_OCCLUSION))
    {
        light.color *= aoFactor.directAmbientOcclusion;
    }
    #endif

    return light;
}

int GetAdditionalLightsCount()
{
#if USE_CLUSTER_LIGHT_LOOP
    // Counting the number of lights in clustered requires traversing the bit list, and is not needed up front.
    return 0;
#else
    // TODO: we need to expose in SRP api an ability for the pipeline cap the amount of lights
    // in the culling. This way we could do the loop branch with an uniform
    // This would be helpful to support baking exceeding lights in SH as well
    return int(min(_AdditionalLightsCount.x, unity_LightData.y));
#endif
}

half4 CalculateShadowMask(InputData inputData)
{
    // To ensure backward compatibility we have to avoid using shadowMask input, as it is not present in older shaders
    #if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
    half4 shadowMask = inputData.shadowMask; // Shadowmask was sampled from lightmap
    #elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    half4 shadowMask = inputData.shadowMask; // Shadowmask (probe occlusion) was sampled from APV
    #elif !defined (LIGHTMAP_ON)
    half4 shadowMask = unity_ProbesOcclusion; // Sample shadowmask (probe occlusion) from legacy probes
    #else
    half4 shadowMask = half4(1, 1, 1, 1); // Fallback shadowmask, fully unoccluded
    #endif

    return shadowMask;
}

#endif
