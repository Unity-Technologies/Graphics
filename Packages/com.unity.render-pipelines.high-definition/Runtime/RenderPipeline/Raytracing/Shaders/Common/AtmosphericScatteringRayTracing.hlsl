#ifndef UNITY_ATMOSPHERIC_SCATTERING_RAY_TRACING_INCLUDED
#define UNITY_ATMOSPHERIC_SCATTERING_RAY_TRACING_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

float GetHeightFogTransmittance(float3 origin, float3 direction, float t)
{
    return TransmittanceHeightFog(_HeightFogBaseExtinction, _HeightFogBaseHeight, _HeightFogExponents, direction.y, origin.y, min(t, _MaxFogDistance));
}

float3 GetHeightFogColor(float3 direction, float t)
{
    return GetFogColor(-direction, min(t, _MaxFogDistance)) * _HeightFogBaseScattering.xyz / _HeightFogBaseExtinction;
}

// Used on continuation rays
void ApplyFogAttenuation(float3 origin, float3 direction, float t, inout float3 value, inout float alpha, inout float3 throughput, bool useFogColor = true)
{
    if (_FogEnabled)
    {
        float fogTransmittance = GetHeightFogTransmittance(origin, direction, t);
        float3 fogColor = useFogColor? GetHeightFogColor(direction, t) : 0.0;

        value = lerp(fogColor, value, fogTransmittance);
        alpha = saturate(1.0 - fogTransmittance) + fogTransmittance * alpha;
        throughput *= fogTransmittance;
    }
}

// Used on transmission rays of local lights
void ApplyFogAttenuation(float3 origin, float3 direction, float t, inout float3 value, bool useFogColor = true)
{
    if (_FogEnabled)
    {
        float fogTransmittance = GetHeightFogTransmittance(origin, direction, t);
        float3 fogColor = useFogColor? GetHeightFogColor(direction, t) : 0.0;

        value = lerp(fogColor, value, fogTransmittance);
    }
}

// Used on transmission rays of distant lights
void ApplyFogAttenuation(float3 origin, float3 direction, inout float3 value)
{
    if (_FogEnabled)
    {
        float fogTransmittance = GetHeightFogTransmittance(origin, direction, _MipFogFar);
        float3 fogColor = GetHeightFogColor(direction, _MipFogFar);

        value = lerp(fogColor, value, fogTransmittance);
    }
}

// Used on camera rays
void ApplyFogAttenuation(float3 origin, float3 direction, inout float3 value, inout float alpha)
{
    if (_FogEnabled)
    {
        float fogTransmittance = GetHeightFogTransmittance(origin, direction, _MipFogFar);
        float3 fogColor = GetHeightFogColor(direction, _MipFogFar);

        value = lerp(fogColor, value, fogTransmittance);
        alpha = saturate(1.0 - fogTransmittance) + fogTransmittance * alpha;
    }
}

#endif // UNITY_ATMOSPHERIC_SCATTERING_RAY_TRACING_INCLUDED
