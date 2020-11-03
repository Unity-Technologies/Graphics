#ifndef UNITY_RAY_TRACING_VOLUME_INCLUDED
#define UNITY_RAY_TRACING_VOLUME_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/AtmosphericScattering.hlsl"

void ApplyFogAttenuation(float3 origin, float3 direction, float t, inout float3 value, bool useFogColor = true)
{
    if (_FogEnabled)
    {
        float dist = min(t, _MaxFogDistance);
        float absFogBaseHeight = _HeightFogBaseHeight;
        float fogTransmittance = TransmittanceHeightFog(_HeightFogBaseExtinction, absFogBaseHeight, _HeightFogExponents, direction.y, origin.y, dist);
        float3 fogColor = useFogColor ? GetFogColor(-direction, dist) : 0.0;
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
        value = lerp(GetFogColor(-direction, dist), value, fogTransmittance);
    }
}

#endif // UNITY_RAY_TRACING_VOLUME_INCLUDED
