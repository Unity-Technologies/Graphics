
#ifndef UNIVERSAL_DEBUGGING_INCLUDED
#define UNIVERSAL_DEBUGGING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DebugViewEnums.cs.hlsl"

// Set of colors that should still provide contrast for the Color-blind
#define kPurpleColor float4(156.0 / 255.0, 79.0 / 255.0, 255.0 / 255.0, 1.0) // #9C4FFF
#define kRedColor float4(203.0 / 255.0, 48.0 / 255.0, 34.0 / 255.0, 1.0) // #CB3022
#define kGreenColor float4(8.0 / 255.0, 215.0 / 255.0, 139.0 / 255.0, 1.0) // #08D78B
#define kYellowGreenColor float4(151.0 / 255.0, 209.0 / 255.0, 61.0 / 255.0, 1.0) // #97D13D
#define kBlueColor float4(75.0 / 255.0, 146.0 / 255.0, 243.0 / 255.0, 1.0) // #4B92F3
#define kOrangeBrownColor float4(219.0 / 255.0, 119.0 / 255.0, 59.0 / 255.0, 1.0) // #4B92F3
#define kGrayColor float4(174.0 / 255.0, 174.0 / 255.0, 174.0 / 255.0, 1.0) // #AEAEAE

#if defined(_DEBUG_SHADER)

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"

// Material settings...
int _DebugMaterialMode;
int _DebugVertexAttributeMode;

// Rendering settings...
int _DebugFullScreenMode;
int _DebugSceneOverrideMode;
int _DebugMipInfoMode;

// Lighting settings...
int _DebugLightingMode;
int _DebugLightingFeatureFlags;

// Validation settings...
int _DebugValidationMode;
half _DebugValidateAlbedoMinLuminance = 0.01;
half _DebugValidateAlbedoMaxLuminance = 0.90;
half _DebugValidateAlbedoSaturationTolerance = 0.214;
half _DebugValidateAlbedoHueTolerance = 0.104;
half3 _DebugValidateAlbedoCompareColor = half3(0.5, 0.5, 0.5);

float4 _DebugColor;
sampler2D _DebugNumberTexture;

struct DebugData
{
    half3 brdfDiffuse;
    half3 brdfSpecular;
    float2 uv;

    float4 texelSize;   // 1 / width, 1 / height, width, height
    uint mipCount;
};

// TODO: Set of colors that should still provide contrast for the Color-blind
//#define kPurpleColor half4(156.0 / 255.0, 79.0 / 255.0, 255.0 / 255.0, 1.0) // #9C4FFF
//#define kRedColor half4(203.0 / 255.0, 48.0 / 255.0, 34.0 / 255.0, 1.0) // #CB3022
//#define kGreenColor = half4(8.0 / 255.0, 215.0 / 255.0, 139.0 / 255.0, 1.0) // #08D78B
//#define kYellowGreenColor = half4(151.0 / 255.0, 209.0 / 255.0, 61.0 / 255.0, 1.0) // #97D13D
//#define kBlueColor = half4(75.0 / 255.0, 146.0 / 255.0, 243.0 / 255.0, 1.0) // #4B92F3
//#define kOrangeBrownColor = half4(219.0 / 255.0, 119.0 / 255.0, 59.0 / 255.0, 1.0) // #4B92F3
//#define kGrayColor = half4(174.0 / 255.0, 174.0 / 255.0, 174.0 / 255.0, 1.0) // #AEAEAE

half4 GetShadowCascadeColor(float4 shadowCoord, float3 positionWS);

DebugData CreateDebugData(half3 brdfDiffuse, half3 brdfSpecular, float2 uv)
{
    DebugData debugData;

    debugData.brdfDiffuse = brdfDiffuse;
    debugData.brdfSpecular = brdfSpecular;
    debugData.uv = uv;

    // TODO: Pass the actual mipmap and texel data in here somehow, but we don't have access to textures here...
    const int textureWdith = 1024;
    const int textureHeight = 1024;
    debugData.texelSize = half4(1.0h / textureWdith, 1.0h / textureHeight, textureWdith, textureHeight);
    debugData.mipCount = 9;

    return debugData;
}

half3 GetDebugColor(uint index)
{
    // TODO: Make these colors colorblind friendly...
    const uint maxColors = 10;
    float4 lut[maxColors] = {
        kPurpleColor,
        kRedColor,
        kGreenColor,
        kYellowGreenColor,
        kBlueColor,
        kOrangeBrownColor,
        kGrayColor,
        float4(1, 1, 1, 0),
        float4(0.8, 0.3, 0.7, 0),
        float4(0.8, 0.7, 0.3, 0),
    };
    uint clammpedIndex = clamp(index, 0, maxColors - 1);

    return lut[clammpedIndex].rgb;
}

half3 GetLODDebugColor()
{
    if (IsBitSet(unity_LODFade.z, 0))
        return GetDebugColor(0);
    else if (IsBitSet(unity_LODFade.z, 1))
        return GetDebugColor(1);
    else if (IsBitSet(unity_LODFade.z, 2))
        return GetDebugColor(2);
    else if (IsBitSet(unity_LODFade.z, 3))
        return GetDebugColor(3);
    else if (IsBitSet(unity_LODFade.z, 4))
        return GetDebugColor(4);
    else if (IsBitSet(unity_LODFade.z, 5))
        return GetDebugColor(5);
    else if (IsBitSet(unity_LODFade.z, 6))
        return GetDebugColor(6);
    else if (IsBitSet(unity_LODFade.z, 7))
        return GetDebugColor(7);
    else
        return GetDebugColor(8);
}

// Convert rgb to luminance
// with rgb in linear space with sRGB primaries and D65 white point
half LinearRgbToLuminance(half3 linearRgb)
{
    return dot(linearRgb, half3(0.2126729f, 0.7151522f, 0.0721750f));
}

half3 UnityMeta_RGBToHSVHelper(float offset, half dominantColor, half colorone, half colortwo)
{
    half H, S, V;
    V = dominantColor;

    if (V != 0.0)
    {
        half small = 0.0;
        if (colorone > colortwo)
            small = colortwo;
        else
            small = colorone;

        half diff = V - small;

        if (diff != 0)
        {
            S = diff / V;
            H = offset + ((colorone - colortwo) / diff);
        }
        else
        {
            S = 0;
            H = offset + (colorone - colortwo);
        }

        H /= 6.0;

        if (H < 6.0)
        {
            H += 1.0;
        }
    }
    else
    {
        S = 0;
        H = 0;
    }
    return half3(H, S, V);
}

half3 UnityMeta_RGBToHSV(half3 rgbColor)
{
    // when blue is highest valued
    if ((rgbColor.b > rgbColor.g) && (rgbColor.b > rgbColor.r))
        return UnityMeta_RGBToHSVHelper(4.0, rgbColor.b, rgbColor.r, rgbColor.g);
    //when green is highest valued
    else if (rgbColor.g > rgbColor.r)
        return UnityMeta_RGBToHSVHelper(2.0, rgbColor.g, rgbColor.b, rgbColor.r);
    //when red is highest valued
    else
        return UnityMeta_RGBToHSVHelper(0.0, rgbColor.r, rgbColor.g, rgbColor.b);
}

bool UpdateSurfaceAndInputDataForDebug(inout SurfaceData surfaceData, inout InputData inputData)
{
    bool changed = false;

    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LIGHT_ONLY || _DebugLightingMode == DEBUGLIGHTINGMODE_LIGHT_DETAIL)
    {
        surfaceData.albedo = half3(1, 1, 1);
        surfaceData.emission = half3(0, 0, 0);
        surfaceData.specular = half3(0, 0, 0);
        surfaceData.occlusion = 1;
        surfaceData.clearCoatMask = 0;
        surfaceData.clearCoatSmoothness = 1;
        surfaceData.metallic = 0;
        surfaceData.smoothness = 0;
        changed = true;
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS || _DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS_WITH_SMOOTHNESS)
    {
        surfaceData.albedo = half3(0, 0, 0);
        surfaceData.emission = half3(0, 0, 0);
        surfaceData.occlusion = 1;
        surfaceData.clearCoatMask = 0;
        surfaceData.clearCoatSmoothness = 1;
        if (_DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS)
        {
            surfaceData.specular = half3(1, 1, 1);
            surfaceData.metallic = 0;
            surfaceData.smoothness = 1;
        }
        else if (_DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS_WITH_SMOOTHNESS)
        {
            surfaceData.specular = half3(0, 0, 0);
            surfaceData.metallic = 1;
            surfaceData.smoothness = 0;
        }
        changed = true;
    }

    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LIGHT_ONLY || _DebugLightingMode == DEBUGLIGHTINGMODE_REFLECTIONS)
    {
        half3 normalTS = half3(0, 0, 1);

        #if defined(_NORMALMAP)
        inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentMatrixWS);
        #else
        inputData.normalWS = inputData.normalWS;
        #endif
        inputData.normalTS = normalTS;
        surfaceData.normalTS = normalTS;
        changed = true;
    }

    return changed;
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
    const float invNumChar = 1.0 / 10.0f;
    // The following are hardcoded scales that make the font size readable.
    ndc.x *= 5.0;
    ndc.y *= 15.0;
    ndc.x = fmod(ndc.x, invNumChar) + (numberValue * invNumChar);

    return tex2D(_DebugNumberTexture, ndc.xy);
}

half4 CalculateDebugColorWithNumber(in InputData inputData, in SurfaceData surfaceData, uint index)
{
    // TODO: Opacity could be user-defined...
    const float opacity = 0.8f;
    half3 debugColor = GetDebugColor(index);
    half3 fc = lerp(surfaceData.albedo, debugColor, opacity);
    half4 textColor = GetTextNumber(index, inputData.positionWS);

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

half4 GetMipLevelDebugColor(in InputData inputData, in SurfaceData surfaceData, in DebugData debugData)
{
    uint mipLevel = GetMipMapLevel(debugData.uv * debugData.texelSize.zw);

    return CalculateDebugColorWithNumber(inputData, surfaceData, mipLevel);
}

half4 GetMipCountDebugColor(in InputData inputData, in SurfaceData surfaceData, in DebugData debugData)
{
    uint mipCount = debugData.mipCount;

    return CalculateDebugColorWithNumber(inputData, surfaceData, mipCount);
}

bool CalculateValidationColorForDebug(in InputData inputData, in SurfaceData surfaceData, in DebugData debugData, out half4 color)
{
    if (_DebugValidationMode == DEBUGVALIDATIONMODE_VALIDATE_ALBEDO)
    {
        half value = LinearRgbToLuminance(surfaceData.albedo);
        if (_DebugValidateAlbedoMinLuminance > value)
        {
             color = half4(1.0f, 0.0f, 0.0f, 1.0f);
        }
        else if (_DebugValidateAlbedoMaxLuminance < value)
        {
             color = half4(0.0f, 1.0f, 0.0f, 1.0f);
        }
        else
        {
            half3 hsv = UnityMeta_RGBToHSV(surfaceData.albedo);
            half hue = hsv.r;
            half sat = hsv.g;

            half3 compHSV = UnityMeta_RGBToHSV(_DebugValidateAlbedoCompareColor.rgb);
            half compHue = compHSV.r;
            half compSat = compHSV.g;

            if ((compSat - _DebugValidateAlbedoSaturationTolerance > sat) || ((compHue - _DebugValidateAlbedoHueTolerance > hue) && (compHue - _DebugValidateAlbedoHueTolerance + 1.0 > hue)))
            {
                color = half4(1.0f, 0.0f, 0.0f, 1.0f);
            }
            else if ((sat > compSat + _DebugValidateAlbedoSaturationTolerance) || ((hue > compHue + _DebugValidateAlbedoHueTolerance) && (hue > compHue + _DebugValidateAlbedoHueTolerance - 1.0)))
            {
                color = half4(0.0f, 1.0f, 0.0f, 1.0f);
            }
            else
            {
                color = half4(value, value, value, 1.0);
            }
        }
        return true;
    }
    else
    {
        color = half4(0, 0, 0, 1);
        return false;
    }
}

bool CalculateValidationColorForMipMaps(in InputData inputData, in SurfaceData surfaceData, in DebugData debugData, out half4 color)
{
    switch (_DebugMipInfoMode)
    {
        case DEBUGMIPINFOMODE_LEVEL:
            color = GetMipLevelDebugColor(inputData, surfaceData, debugData);
            return true;

        case DEBUGMIPINFOMODE_COUNT:
            color = GetMipCountDebugColor(inputData, surfaceData, debugData);
            return true;

        default:
            color = half4(0, 0, 0, 1);
            return false;
    }
}

bool CalculateColorForDebugMaterial(in InputData inputData, in SurfaceData surfaceData, in DebugData debugData, out half4 color)
{
    // Debug materials...
    switch(_DebugMaterialMode)
    {
        case DEBUGMATERIALMODE_UNLIT:
            color = half4(surfaceData.albedo, 1);
            return true;

        case DEBUGMATERIALMODE_DIFFUSE:
            color = half4(debugData.brdfDiffuse, 1);
            return true;

        case DEBUGMATERIALMODE_SPECULAR:
            color = half4(debugData.brdfSpecular, 1);
            return true;

        case DEBUGMATERIALMODE_ALPHA:
            color = half4(surfaceData.alpha.rrr, 1);
            return true;

        case DEBUGMATERIALMODE_SMOOTHNESS:
            color = half4(surfaceData.smoothness.rrr, 1);
            return true;

        case DEBUGMATERIALMODE_AMBIENT_OCCLUSION:
            color = half4(surfaceData.occlusion.rrr, 1);
            return true;

        case DEBUGMATERIALMODE_EMISSION:
            color = half4(surfaceData.emission, 1);
            return true;

        case DEBUGMATERIALMODE_NORMAL_WORLD_SPACE:
            color = half4(inputData.normalWS.xyz * 0.5 + 0.5, 1);
            return true;

        case DEBUGMATERIALMODE_NORMAL_TANGENT_SPACE:
            color = half4(surfaceData.normalTS.xyz * 0.5 + 0.5, 1);
            return true;

        case DEBUGMATERIALMODE_LOD:
            color = half4(GetLODDebugColor(), 1);
            return true;

        case DEBUGMATERIALMODE_METALLIC:
            color = half4(surfaceData.metallic.rrr, 1);
            return true;

        default:
            color = half4(0, 0, 0, 1);
            return false;
    }
}

bool CalculateColorForDebugSceneOverride(in InputData inputData, in SurfaceData surfaceData, in DebugData debugData, out half4 color)
{
    if(_DebugSceneOverrideMode == DEBUGSCENEOVERRIDEMODE_NONE)
    {
        color = half4(0, 0, 0, 1);
        return false;
    }
    else
    {
        color = _DebugColor;
        return true;
    }
}

bool CalculateColorForDebug(in InputData inputData, in SurfaceData surfaceData, in DebugData debugData, out half4 color)
{
    if(CalculateColorForDebugSceneOverride(inputData, surfaceData, debugData, color))
    {
        return true;
    }
    else if(CalculateColorForDebugMaterial(inputData, surfaceData, debugData, color))
    {
        return true;
    }
    else if(CalculateValidationColorForDebug(inputData, surfaceData, debugData, color))
    {
        return true;
    }
    else if(CalculateValidationColorForMipMaps(inputData, surfaceData, debugData, color))
    {
        return true;
    }
    else
    {
        color = half4(0, 0, 0, 1);
        return false;
    }
}

#endif

bool IsLightingFeatureEnabled(uint bitMask)
{
    #if defined(_DEBUG_SHADER)
    return (_DebugLightingFeatureFlags == 0) || ((_DebugLightingFeatureFlags & bitMask) != 0);
    #else
    return true;
    #endif
}

#endif
