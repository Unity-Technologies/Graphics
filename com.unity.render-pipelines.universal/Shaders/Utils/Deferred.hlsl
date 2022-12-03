#ifndef UNIVERSAL_DEFERRED_INCLUDED
#define UNIVERSAL_DEFERRED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

// This structure is used in StructuredBuffer.
// TODO move some of the properties to half storage (color, attenuation, spotDirection, flag to 16bits, occlusionProbeInfo)
struct PunctualLightData
{
    float3 posWS;
    float radius2;              // squared radius
    float4 color;
    float4 attenuation;         // .xy are used by DistanceAttenuation - .zw are used by AngleAttenuation (for SpotLights)
    float3 spotDirection;       // spotLights support
    int flags;                  // Light flags (enum kLightFlags and LightFlag in C# code)
    float4 occlusionProbeInfo;
    uint layerMask;             // Optional light layer mask
};

Light UnityLightFromPunctualLightDataAndWorldSpacePosition(PunctualLightData punctualLightData, float3 positionWS, half4 shadowMask, int shadowLightIndex, bool materialFlagReceiveShadowsOff)
{
    // Keep in sync with GetAdditionalPerObjectLight in Lighting.hlsl

    half4 probesOcclusion = shadowMask;

    Light light;

    float3 lightVector = punctualLightData.posWS - positionWS.xyz;
    float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);

    half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));

    // full-float precision required on some platforms
    float attenuation = DistanceAttenuation(distanceSqr, punctualLightData.attenuation.xy) * AngleAttenuation(punctualLightData.spotDirection.xyz, lightDirection, punctualLightData.attenuation.zw);

    light.direction = lightDirection;
    light.color = punctualLightData.color.rgb;

    light.distanceAttenuation = attenuation;

    [branch] if (materialFlagReceiveShadowsOff)
        light.shadowAttenuation = 1.0;
    else
    {
        light.shadowAttenuation = AdditionalLightShadow(shadowLightIndex, positionWS, lightDirection, shadowMask, punctualLightData.occlusionProbeInfo);
    }

    light.layerMask = punctualLightData.layerMask;

    return light;
}

#endif
