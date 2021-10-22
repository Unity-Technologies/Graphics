#ifndef UNITY_ATMOSPHERIC_SCATTERING_RAY_TRACING_INCLUDED
#define UNITY_ATMOSPHERIC_SCATTERING_RAY_TRACING_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

void ApplyFogAttenuation(float3 origin, float3 direction, float t, inout float3 value, bool useFogColor = true)
{
    if (_FogEnabled)
    {
        float dist = min(t, _MaxFogDistance);
        float absFogBaseHeight = _HeightFogBaseHeight;
        float fogTransmittance = TransmittanceHeightFog(_HeightFogBaseExtinction, absFogBaseHeight, _HeightFogExponents, direction.y, origin.y, dist);

        float3 fogColor = useFogColor? GetFogColor(-direction, dist) * _HeightFogBaseScattering.xyz / _HeightFogBaseExtinction : 0.0;
        value = lerp(fogColor, value, fogTransmittance);
    }
}

void ApplyFogAttenuation(float3 origin, float3 direction, inout float3 value)
{
    if (_FogEnabled)
    {
        float dist = min(_MipFogFar, _MaxFogDistance);
        float absFogBaseHeight = _HeightFogBaseHeight;
        float fogTransmittance = TransmittanceHeightFog(_HeightFogBaseExtinction, absFogBaseHeight, _HeightFogExponents, direction.y, origin.y, dist);

        float3 fogColor = GetFogColor(-direction, dist) * _HeightFogBaseScattering.xyz / _HeightFogBaseExtinction;
        value = lerp(fogColor, value, fogTransmittance);
    }
}

void ApplyFogAttenuation(float3 origin, float3 direction, inout float3 value, inout float alpha)
{
    if (_FogEnabled)
    {
        float dist = min(_MipFogFar, _MaxFogDistance);
        float absFogBaseHeight = _HeightFogBaseHeight;
        float fogTransmittance = TransmittanceHeightFog(_HeightFogBaseExtinction, absFogBaseHeight, _HeightFogExponents, direction.y, origin.y, dist);

        float3 fogColor = GetFogColor(-direction, dist) * _HeightFogBaseScattering.xyz / _HeightFogBaseExtinction;
        value = lerp(fogColor, value, fogTransmittance);
        alpha = saturate(1.0 - fogTransmittance);
    }
}

#endif // UNITY_ATMOSPHERIC_SCATTERING_RAY_TRACING_INCLUDED
