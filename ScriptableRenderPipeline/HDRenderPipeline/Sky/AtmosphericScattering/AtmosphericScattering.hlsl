#ifndef UNITY_ATMOSPHERIC_SCATTERING_INCLUDED
#define UNITY_ATMOSPHERIC_SCATTERING_INCLUDED

#include "ShaderLibrary/VolumeRendering.hlsl"

#include "AtmosphericScattering.cs.hlsl"
#include "../SkyVariables.hlsl"
#include "../../ShaderVariables.hlsl"

#ifdef VOLUMETRIC_LIGHTING_ENABLED
TEXTURE3D(_VBufferLighting);
#endif

CBUFFER_START(AtmosphericScattering)
float   _AtmosphericScatteringType;
// Common
float   _FogColorMode;
float4  _FogColorDensity; // color in rgb, density in alpha
float4  _MipFogParameters;
// Linear fog
float4  _LinearFogParameters;
// Exp fog
float4  _ExpFogParameters;
CBUFFER_END

#define _MipFogNear             _MipFogParameters.x
#define _MipFogFar              _MipFogParameters.y
#define _MipFogMaxMip           _MipFogParameters.z

#define _FogDensity             _FogColorDensity.w
#define _FogColor               _FogColorDensity

#define _LinearFogStart         _LinearFogParameters.x
#define _LinearFogEnd           _LinearFogParameters.y
#define _LinearFogOneOverRange  _LinearFogParameters.z

#define _ExpFogDistance         _ExpFogParameters.x

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
#ifdef VOLUMETRIC_LIGHTING_ENABLED
    return SampleInScatteredRadianceAndTransmittance(TEXTURE3D_PARAM(_VBufferLighting, s_linear_clamp_sampler),
                                                     posInput.positionNDC, posInput.linearDepth,
                                                     _VBufferResolutionAndScale.zw,
                                                     _VBufferDepthEncodingParams);
#endif

    float3 fogColor; float fogFactor;

    if (_AtmosphericScatteringType == FOGTYPE_EXPONENTIAL)
    {
        fogColor  = GetFogColor(posInput);
        fogFactor = _FogDensity * (1.0f - TransmittanceHomogeneousMedium(1.0f / _ExpFogDistance, posInput.linearDepth));
    }
    else if (_AtmosphericScatteringType == FOGTYPE_LINEAR)
    {
        fogColor  = GetFogColor(posInput);
        fogFactor = _FogDensity * saturate((posInput.linearDepth - _LinearFogStart) * _LinearFogOneOverRange);
        return float4(fogColor, fogFactor);
    }
    else // NONE
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    return float4(fogColor * fogFactor, fogFactor); // Premultiplied alpha
}


#endif
