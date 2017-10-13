#ifndef UNITY_ATMOSPHERIC_SCATTERING_INCLUDED
#define UNITY_ATMOSPHERIC_SCATTERING_INCLUDED

#include "AtmosphericScattering.cs.hlsl"
#include "../../../Core/ShaderLibrary/VolumeRendering.hlsl"

uniform float _AtmosphericScatteringType;

// Common
uniform float   _FogColorMode;
uniform float4  _FogColor;
float4  _MipFogParameters;
#define _MipFogNear     _MipFogParameters.x
#define _MipFogFar      _MipFogParameters.y
#define _MipFogMaxMip   _MipFogParameters.z

TEXTURECUBE(_SkyTexture); // Global name defined in SkyManager
SAMPLERCUBE(sampler_SkyTexture);
float _SkyTextureMipCount;

// Linear fog
uniform float4 _LinearFogParameters;
#define _LinearFogStart     _LinearFogParameters.x
#define _LinearFogEnd       _LinearFogParameters.y
#define _LinearFogOoRange   _LinearFogParameters.z
#define _LinearFogDensity   _LinearFogParameters.w

// Exp fog
uniform float4 _ExpFogParameters;
#define _ExpFogDensity      _ExpFogParameters.x

float3 GetFogColor(PositionInputs posInput)
{
    if (_FogColorMode == FOGCOLORMODE_CONSTANT_COLOR)
    {
        return _FogColor.rgb;
    }
    else if (_FogColorMode == FOGCOLORMODE_SKY_COLOR)
    {
        // Based on Uncharted 4 "Mip Sky Fog" trick: http://advances.realtimerendering.com/other/2016/naughty_dog/NaughtyDog_TechArt_Final.pdf
        float mipLevel = (1.0 - _MipFogMaxMip * saturate((posInput.depthVS - _MipFogNear) / (_MipFogFar - _MipFogNear))) * _SkyTextureMipCount;
        float3 dir = normalize(posInput.positionWS - _WorldSpaceCameraPos);
        return SAMPLE_TEXTURECUBE_LOD(_SkyTexture, sampler_SkyTexture, dir, mipLevel).rgb;
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
        float fogFactor = 1.0f - Transmittance(OpticalDepthHomogeneous(1.0f / _ExpFogDensity, posInput.depthVS));
        return float4(fogColor, fogFactor);
    }
    else if (_AtmosphericScatteringType == FOGTYPE_LINEAR)
    {
        float3 fogColor = GetFogColor(posInput);
        float fogFactor = _LinearFogDensity * saturate((posInput.depthVS - _LinearFogStart) * _LinearFogOoRange);
        return float4(fogColor, fogFactor);
    }
    else // NONE
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }
}


#endif