#ifndef UNITY_ATMOSPHERIC_SCATTERING_INCLUDED
#define UNITY_ATMOSPHERIC_SCATTERING_INCLUDED

#include "CoreRP/ShaderLibrary/VolumeRendering.hlsl"
#include "CoreRP/ShaderLibrary/Filtering.hlsl"

#include "AtmosphericScattering.cs.hlsl"
#include "../SkyVariables.hlsl"
#include "../../ShaderVariables.hlsl"
#include "../../Lighting/Volumetrics/VBuffer.hlsl"

TEXTURE3D(_VBufferLighting);

#ifdef DEBUG_DISPLAY
#include "../../Debug/DebugDisplay.hlsl"
#endif

CBUFFER_START(AtmosphericScattering)
int     _AtmosphericScatteringType;
// Common
float   _FogColorMode;
float4  _FogColorDensity; // color in rgb, density in alpha
float4  _MipFogParameters;
// Linear fog
float4  _LinearFogParameters;
// Exp fog
float4  _ExpFogParameters;
CBUFFER_END

#define _MipFogNear                     _MipFogParameters.x
#define _MipFogFar                      _MipFogParameters.y
#define _MipFogMaxMip                   _MipFogParameters.z

#define _FogDensity                     _FogColorDensity.w
#define _FogColor                       _FogColorDensity

#define _LinearFogStart                 _LinearFogParameters.x
#define _LinearFogOneOverRange          _LinearFogParameters.y
#define _LinearFogHeightEnd             _LinearFogParameters.z
#define _LinearFogHeightOneOverRange    _LinearFogParameters.w

#define _ExpFogDistance                 _ExpFogParameters.x
#define _ExpFogBaseHeight               _ExpFogParameters.y
#define _ExpFogHeightAttenuation        _ExpFogParameters.z

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
        float3 dir = -GetWorldSpaceNormalizeViewDir(posInput.positionWS);
        return SampleSkyTexture(dir, mipLevel).rgb;
    }
    else // Should not be possible.
        return  float3(0.0, 0.0, 0.0);
}

// Returns fog color in rgb and fog factor in alpha.
float4 EvaluateAtmosphericScattering(PositionInputs posInput)
{
    float3 fogColor = 0;
    float  fogFactor = 0;

#ifdef DEBUG_DISPLAY
    // Don't sample atmospheric scattering when lighting debug more are enabled so fog is not visible
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_DIFFUSE_LIGHTING || _DebugLightingMode == DEBUGLIGHTINGMODE_SPECULAR_LIGHTING || _DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
        return float4(0, 0, 0, 0);
#endif

    switch (_AtmosphericScatteringType)
    {
        case FOGTYPE_LINEAR:
        {
            fogColor = GetFogColor(posInput);
            fogFactor = _FogDensity * saturate((posInput.linearDepth - _LinearFogStart) * _LinearFogOneOverRange) * saturate((_LinearFogHeightEnd - GetAbsolutePositionWS(posInput.positionWS).y) * _LinearFogHeightOneOverRange);
            break;
        }
        case FOGTYPE_EXPONENTIAL:
        {
            fogColor = GetFogColor(posInput);
            float distance = length(GetWorldSpaceViewDir(posInput.positionWS));
            float fogHeight = max(0.0, GetAbsolutePositionWS(posInput.positionWS).y - _ExpFogBaseHeight);
            fogFactor = _FogDensity * TransmittanceHomogeneousMedium(_ExpFogHeightAttenuation, fogHeight) * (1.0f - TransmittanceHomogeneousMedium(1.0f / _ExpFogDistance, distance));
            break;
        }
        case FOGTYPE_VOLUMETRIC:
        {
            float4 volFog = SampleVolumetricLighting(TEXTURE3D_PARAM(_VBufferLighting, s_linear_clamp_sampler),
                                                     posInput.positionNDC,
                                                     posInput.linearDepth,
                                                     _VBufferResolution,
                                                     _VBufferSliceCount.xy,
                                                     _VBufferUvScaleAndLimit.xy,
                                                     _VBufferUvScaleAndLimit.zw,
                                                     _VBufferDepthEncodingParams,
                                                     _VBufferDepthDecodingParams,
                                                     true, true);

            fogFactor = 1 - volFog.a;                              // Opacity from transmittance
            fogColor  = volFog.rgb * min(rcp(fogFactor), FLT_MAX); // Un-premultiply, clamp to avoid (0 * INF = NaN)
            break;
        }
    }

    return float4(fogColor, fogFactor);
}

#endif
