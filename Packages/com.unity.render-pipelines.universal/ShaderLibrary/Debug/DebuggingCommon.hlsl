
#ifndef UNIVERSAL_DEBUGGING_COMMON_INCLUDED
#define UNIVERSAL_DEBUGGING_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/DebugViewEnums.cs.hlsl"

#if defined(DEBUG_DISPLAY)

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreaming.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"

// Material settings...
int _DebugMaterialMode;
int _DebugVertexAttributeMode;
int _DebugMaterialValidationMode;

// Rendering settings...
int _DebugFullScreenMode;
int _DebugSceneOverrideMode;
int _DebugMipInfoMode;
int _DebugMipMapStatusMode;
int _DebugMipMapShowStatusCode;
half _DebugMipMapOpacity;
half _DebugMipMapRecentlyUpdatedCooldown;
int _DebugMipMapTerrainTextureMode;
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

// Not particular to any settings; used by MipMap debugging because we don't have reliable access to _Time.y
float _DebugCurrentRealTime;

// We commonly need to undo a previous TRANSFORM_TEX to get UV0s back, followed by a TRANSFORM_TEX with the UV0s / _ST of the StreamingDebugTex... hence, this macro.
#define UNDO_TRANSFORM_TEX(uv, undoTex)    ((uv - undoTex##_ST.zw) / undoTex##_ST.xy)

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

uint GetMipMapLevel(float2 nonNormalizedUVCoordinate)
{
    // The OpenGL Graphics System: A Specification 4.2
    //  - chapter 3.9.11, equation 3.21

    float2  dx_vtc = ddx(nonNormalizedUVCoordinate);
    float2  dy_vtc = ddy(nonNormalizedUVCoordinate);
    float delta_max_sqr = max(dot(dx_vtc, dx_vtc), dot(dy_vtc, dy_vtc));

    return (uint)(0.5 * log2(delta_max_sqr));
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

half4 BlitScreenSpaceDigit(half4 originalColor, uint2 screenSpaceCoords, int digit, uint spacing, bool invertColors)
{
    half4 outColor = originalColor;

    const uint2 pixCoord = screenSpaceCoords / 2;
    const uint2 tileSize = uint2(spacing, spacing);
    const int2 coord = (pixCoord & (tileSize - 1)) - int2(tileSize.x/4+1, tileSize.y/3-3);

    UNITY_LOOP for (int i = 0; i <= 1; ++i)
    {
        // 0 == shadow, 1 == text
        if (SampleDebugFontNumber2Digits(coord + i, digit))
        {
            outColor = (i == 0)
                ? (invertColors ? half4(1, 1, 1, 1) : half4(0, 0, 0, 1))
                : (invertColors ? half4(0, 0, 0, 1) : half4(1, 1, 1, 1));
        }
    }

    return outColor;
}

void GetHatchedColor(uint2 screenSpaceCoords, half4 hatchingColor, inout half4 debugColor)
{
    const uint spacing = 16; // increase spacing compared to the legend (easier on the eyes)
    const uint thickness = 3;
    if((screenSpaceCoords.x + screenSpaceCoords.y) % spacing < thickness)
        debugColor = hatchingColor;
}

void GetHatchedColor(uint2 screenSpaceCoords, inout half4 debugColor)
{
    GetHatchedColor(screenSpaceCoords, half4(0.1, 0.1, 0.1, 1), debugColor);
}

// Keep in sync with GetTextureDataDebug in HDRP's Runtime/Debug/DebugDisplay.hlsl
bool CalculateColorForDebugMipmapStreaming(in uint mipCount, uint2 screenSpaceCoords, in float4 texelSize, in float2 uv, in float4 mipInfo, in float4 streamInfo, in half3 originalColor, inout half4 debugColor)
{
    bool hasDebugColor = false;
    bool needsHatching;

    switch (_DebugMipInfoMode)
    {
        case DEBUGMIPINFOMODE_NONE:
            hasDebugColor = false;
            break;

        case DEBUGMIPINFOMODE_MIP_COUNT:
            debugColor = half4(GetDebugMipCountColor(mipCount, needsHatching), 1);
            if (needsHatching)
            {
                half4 hatchingColor = half4(GetDebugMipCountHatchingColor(mipCount), 1);
                GetHatchedColor(screenSpaceCoords, hatchingColor, debugColor);
            }

            if (mipCount > 0 && mipCount <= 14)
                debugColor = BlitScreenSpaceDigit(debugColor, screenSpaceCoords, mipCount, 32, true);

            hasDebugColor = true;
            break;

        case DEBUGMIPINFOMODE_MIP_RATIO:
            debugColor = half4(GetDebugMipColorIncludingMipReduction(originalColor, mipCount, texelSize, uv, mipInfo), 1);
            hasDebugColor = true;
            break;

        case DEBUGMIPINFOMODE_MIP_STREAMING_PERFORMANCE:
            debugColor = half4(GetDebugStreamingMipColor(mipCount, mipInfo, streamInfo, needsHatching), 1);
            if (needsHatching)
                GetHatchedColor(screenSpaceCoords, debugColor);

            hasDebugColor = true;
            break;

        case DEBUGMIPINFOMODE_MIP_STREAMING_STATUS:
            if(_DebugMipMapStatusMode == DEBUGMIPMAPSTATUSMODE_TEXTURE)
                debugColor = half4(GetDebugStreamingStatusColor(streamInfo, needsHatching), 1);
            else
                debugColor = half4(GetDebugPerMaterialStreamingStatusColor(streamInfo, needsHatching), 1);
            if (needsHatching)
                GetHatchedColor(screenSpaceCoords, debugColor);

            if (_DebugMipMapShowStatusCode && _DebugMipMapStatusMode == DEBUGMIPMAPSTATUSMODE_TEXTURE && !IsStreaming(streamInfo))
            {
                if (GetStatusCode(streamInfo, false) != kMipmapDebugStatusCodeNotSet && GetStatusCode(streamInfo, false) != kMipmapDebugStatusCodeNoTexture) // we're ignoring these because there's just one status anyway (so the color itself is enough)
                    debugColor = BlitScreenSpaceDigit(debugColor, screenSpaceCoords, GetStatusCode(streamInfo, false), 16, false);
            }

            hasDebugColor = true;
            break;

        case DEBUGMIPINFOMODE_MIP_STREAMING_PRIORITY:
            debugColor = half4(GetDebugStreamingPriorityColor(streamInfo), 1);
            hasDebugColor = true;
            break;

        case DEBUGMIPINFOMODE_MIP_STREAMING_ACTIVITY:
            debugColor = half4(GetDebugStreamingRecentlyUpdatedColor(_DebugCurrentRealTime, _DebugMipMapRecentlyUpdatedCooldown, _DebugMipMapStatusMode == DEBUGMIPMAPSTATUSMODE_MATERIAL, streamInfo), 1);
            hasDebugColor = true;
            break;

        default:
            hasDebugColor = TryGetDebugColorInvalidMode(debugColor);
            break;
    }

    // Blend the original color with the debug color
    if(hasDebugColor)
        debugColor = lerp(half4(originalColor, 1), debugColor, _DebugMipMapOpacity);

    return hasDebugColor;
}

#else

// When "DEBUG_DISPLAY" isn't defined this macro just returns the original UVs.
#define UNDO_TRANSFORM_TEX(uv, undoTex)    uv

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

bool IsOnlyAOLightingFeatureEnabled()
{
    #if defined(DEBUG_DISPLAY)
    return _DebugLightingFeatureFlags == DEBUGLIGHTINGFEATUREFLAGS_AMBIENT_OCCLUSION;
    #else
    return false;
    #endif
}

#endif
