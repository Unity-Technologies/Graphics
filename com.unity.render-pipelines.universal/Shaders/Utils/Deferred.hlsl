#ifndef UNIVERSAL_DEFERRED_INCLUDED
#define UNIVERSAL_DEFERRED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

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
