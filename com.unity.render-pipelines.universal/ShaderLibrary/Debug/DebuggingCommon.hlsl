
#ifndef UNIVERSAL_DEBUGGING_COMMON_INCLUDED
#define UNIVERSAL_DEBUGGING_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebugViewEnums.cs.hlsl"

#if defined(DEBUG_DISPLAY)

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"

// Material settings...
int _DebugMaterialMode;
int _DebugVertexAttributeMode;
int _DebugMaterialValidationMode;

// Rendering settings...
int _DebugFullScreenMode;
int _DebugSceneOverrideMode;
int _DebugMipInfoMode;
int _DebugValidationMode;

// Lighting settings...
int _DebugLightingMode;
int _DebugLightingFeatureFlags;

half _DebugValidateAlbedoMinLuminance = 0.01;
half _DebugValidateAlbedoMaxLuminance = 0.90;
half _DebugValidateAlbedoSaturationTolerance = 0.214;
half _DebugValidateAlbedoHueTolerance = 0.104;
half3 _DebugValidateAlbedoCompareColor = half3(0.5, 0.5, 0.5);

half _DebugValidateMetallicMinValue = 0;
half _DebugValidateMetallicMaxValue = 0.9;

float4 _DebugColor;
float4 _DebugColorInvalidMode;
float4 _DebugValidateBelowMinThresholdColor;
float4 _DebugValidateAboveMaxThresholdColor;
sampler2D _DebugNumberTexture;

half3 GetDebugColor(uint index)
{
    uint clampedIndex = clamp(index, 0, DEBUG_COLORS_COUNT-1);
    return kDebugColorGradient[clampedIndex].rgb;
}

bool TryGetDebugColorInvalidMode(out half4 debugColor)
{
    // Depending upon how we want to deal with invalid modes, this code may need to change,
    // for now we'll simply make each pixel use "_DebugColorInvalidMode"...
    debugColor = _DebugColorInvalidMode;
    return true;
}

half4 GetTextNumber(uint numberValue, float3 positionWS)
{
    float4 clipPos = TransformWorldToHClip(positionWS);
    float2 ndc = saturate((clipPos.xy / clipPos.w) * 0.5 + 0.5);

#if UNITY_UV_STARTS_AT_TOP
    if (_ProjectionParams.x < 0)
        ndc.y = 1.0 - ndc.y;
#endif

    // There are currently 10 characters in the font texture, 0-9.
    const int numChar = 10;
    const float invNumChar = 1.0 / numChar;
    numberValue = clamp(0, numChar - 1, numberValue);

    // The following are hardcoded scales that make the font size readable.
    ndc.x *= 5.0;
    ndc.y *= 15.0;
    ndc.x = fmod(ndc.x, invNumChar) + (numberValue * invNumChar);

    return tex2D(_DebugNumberTexture, ndc.xy);
}

half4 CalculateDebugColorWithNumber(float3 positionWS, half3 albedo, uint index)
{
    const float opacity = 0.8f; // TODO: Opacity could be user-defined.

    const half3 debugColor = GetDebugColor(index);
    const half3 fc = lerp(albedo, debugColor, opacity);
    const half4 textColor = GetTextNumber(index, positionWS);

    return textColor * half4(fc, 1);
}

uint GetMipMapLevel(float2 nonNormalizedUVCoordinate)
{
    // The OpenGL Graphics System: A Specification 4.2
    //  - chapter 3.9.11, equation 3.21

    float2  dx_vtc = ddx(nonNormalizedUVCoordinate);
    float2  dy_vtc = ddy(nonNormalizedUVCoordinate);
    float delta_max_sqr = max(dot(dx_vtc, dx_vtc), dot(dy_vtc, dy_vtc));

    return (uint)(0.5 * log2(delta_max_sqr));
}

half4 GetMipLevelDebugColor(float3 positionWS, half3 albedo, float2 uv, float4 texelSize)
{
    uint mipLevel = GetMipMapLevel(uv * texelSize.zw);

    return CalculateDebugColorWithNumber(positionWS, albedo, mipLevel);
}

half4 GetMipCountDebugColor(float3 positionWS, half3 albedo, uint mipCount)
{
    return CalculateDebugColorWithNumber(positionWS, albedo, mipCount);
}

bool CalculateValidationAlbedo(half3 albedo, out half4 color)
{
    half luminance = Luminance(albedo);

    if (luminance < _DebugValidateAlbedoMinLuminance)
    {
        color = _DebugValidateBelowMinThresholdColor;
    }
    else if (luminance > _DebugValidateAlbedoMaxLuminance)
    {
        color = _DebugValidateAboveMaxThresholdColor;
    }
    else
    {
        half3 hsv = RgbToHsv(albedo);
        half hue = hsv.r;
        half sat = hsv.g;

        half3 compHSV = RgbToHsv(_DebugValidateAlbedoCompareColor.rgb);
        half compHue = compHSV.r;
        half compSat = compHSV.g;

        if ((compSat - _DebugValidateAlbedoSaturationTolerance > sat) || ((compHue - _DebugValidateAlbedoHueTolerance > hue) && (compHue - _DebugValidateAlbedoHueTolerance + 1.0 > hue)))
        {
            color = _DebugValidateBelowMinThresholdColor;
        }
        else if ((sat > compSat + _DebugValidateAlbedoSaturationTolerance) || ((hue > compHue + _DebugValidateAlbedoHueTolerance) && (hue > compHue + _DebugValidateAlbedoHueTolerance - 1.0)))
        {
            color = _DebugValidateAboveMaxThresholdColor;
        }
        else
        {
            color = half4(luminance, luminance, luminance, 1.0);
        }
    }
    return true;
}

bool CalculateColorForDebugSceneOverride(out half4 color)
{
    if (_DebugSceneOverrideMode == DEBUGSCENEOVERRIDEMODE_NONE)
    {
        color = 0;
        return false;
    }
    else
    {
        color = _DebugColor;
        return true;
    }
}

#endif

bool IsAlphaDiscardEnabled()
{
    #if defined(DEBUG_DISPLAY)
    return (_DebugSceneOverrideMode == DEBUGSCENEOVERRIDEMODE_NONE);
    #else
    return true;
    #endif
}

bool IsFogEnabled()
{
    #if defined(DEBUG_DISPLAY)
    return (_DebugMaterialMode == DEBUGMATERIALMODE_NONE) &&
           (_DebugVertexAttributeMode == DEBUGVERTEXATTRIBUTEMODE_NONE) &&
           (_DebugMaterialValidationMode == DEBUGMATERIALVALIDATIONMODE_NONE) &&
           (_DebugSceneOverrideMode == DEBUGSCENEOVERRIDEMODE_NONE) &&
           (_DebugMipInfoMode == DEBUGMIPINFOMODE_NONE) &&
           (_DebugLightingMode == DEBUGLIGHTINGMODE_NONE) &&
           (_DebugLightingFeatureFlags == 0) &&
           (_DebugValidationMode == DEBUGVALIDATIONMODE_NONE);
    #else
    return true;
    #endif
}

bool IsLightingFeatureEnabled(uint bitMask)
{
    #if defined(DEBUG_DISPLAY)
    return (_DebugLightingFeatureFlags == 0) || ((_DebugLightingFeatureFlags & bitMask) != 0);
    #else
    return true;
    #endif
}

#endif
