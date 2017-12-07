#ifndef UNITY_ATMOSPHERIC_SCATTERING_INCLUDED
#define UNITY_ATMOSPHERIC_SCATTERING_INCLUDED

#include "ShaderLibrary/VolumeRendering.hlsl"

#include "AtmosphericScattering.cs.hlsl"
#include "../SkyVariables.hlsl"
#include "../../ShaderVariables.hlsl"

CBUFFER_START(AtmosphericScattering)
float   _AtmosphericScatteringType;
// Common
float   _FogColorMode;
float4  _FogColor;
float4  _MipFogParameters;
// Linear fog
float4  _LinearFogParameters;
// Exp fog
float4  _ExpFogParameters;
CBUFFER_END

#define _MipFogNear             _MipFogParameters.x
#define _MipFogFar              _MipFogParameters.y
#define _MipFogMaxMip           _MipFogParameters.z

#define _LinearFogStart         _LinearFogParameters.x
#define _LinearFogEnd           _LinearFogParameters.y
#define _LinearFogOneOverRange  _LinearFogParameters.z
#define _LinearFogDensity       _LinearFogParameters.w

#define _ExpFogDistance         _ExpFogParameters.x
#define _ExpFogDensity          _ExpFogParameters.y

float3 GetFogColor(PositionInputs posInput)
{
    if (_FogColorMode == FOGCOLORMODE_CONSTANT_COLOR)
    {
        return _FogColor.rgb;
    }
    else if (_FogColorMode == FOGCOLORMODE_SKY_COLOR)
    {
        // Based on Uncharted 4 "Mip Sky Fog" trick: http://advances.realtimerendering.com/other/2016/naughty_dog/NaughtyDog_TechArt_Final.pdf
        float mipLevel = (1.0 - _MipFogMaxMip * saturate((posInput.linearDepth - _MipFogNear) / (_MipFogFar - _MipFogNear))) * _SkyTextureMipCount;
        float3 dir = normalize(posInput.positionWS - GetPrimaryCameraPosition());
        return SampleSkyTexture(dir, mipLevel).rgb;
    }
    else // Should not be possible.
        return  float3(0.0, 0.0, 0.0);
}

// Returns fog color in rgb and fog factor in alpha.
float4 EvaluateAtmosphericScattering(PositionInputs posInput)
{
    if (_AtmosphericScatteringType == FOGTYPE_EXPONENTIAL)
    {
        float3 fogColor = GetFogColor(posInput);
        float fogFactor = _ExpFogDensity * (1.0f - Transmittance(OpticalDepthHomogeneous(1.0f / _ExpFogDistance, posInput.linearDepth)));
        return float4(fogColor, fogFactor);
    }
    else if (_AtmosphericScatteringType == FOGTYPE_LINEAR)
    {
        float3 fogColor = GetFogColor(posInput);
        float fogFactor = _LinearFogDensity * saturate((posInput.linearDepth - _LinearFogStart) * _LinearFogOneOverRange);
        return float4(fogColor, fogFactor);
    }
    else // NONE
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }
}


#endif
