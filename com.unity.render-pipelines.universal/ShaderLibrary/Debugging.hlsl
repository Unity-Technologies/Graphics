
#ifndef UNIVERSAL_DEBUGGING_INCLUDED
#define UNIVERSAL_DEBUGGING_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"

float4 _BaseMap_TexelSize;
float4 _BaseMap_MipInfo;

#define DEBUG_UNLIT 1
#define DEBUG_DIFFUSE 2
#define DEBUG_SPECULAR 3
#define DEBUG_ALPHA 4
#define DEBUG_SMOOTHNESS 5
#define DEBUG_OCCLUSION 6
#define DEBUG_EMISSION 7
#define DEBUG_NORMAL_WORLD_SPACE 8
#define DEBUG_NORMAL_TANGENT_SPACE 9
#define DEBUG_LIGHTING_COMPLEXITY 10
#define DEBUG_LOD 11
#define DEBUG_METALLIC 12
int _DebugMaterialIndex;

#define DEBUG_LIGHTING_SHADOW_CASCADES 1
#define DEBUG_LIGHTING_LIGHT_ONLY 2
#define DEBUG_LIGHTING_LIGHT_DETAIL 3
#define DEBUG_LIGHTING_REFLECTIONS 4
#define DEBUG_LIGHTING_REFLECTIONS_WITH_SMOOTHNESS 5 

int _DebugLightingIndex;

#define DEBUG_ATTRIBUTE_TEXCOORD0 1
#define DEBUG_ATTRIBUTE_TEXCOORD1 2
#define DEBUG_ATTRIBUTE_TEXCOORD2 3
#define DEBUG_ATTRIBUTE_TEXCOORD3 4
#define DEBUG_ATTRIBUTE_COLOR     5
#define DEBUG_ATTRIBUTE_TANGENT   6
#define DEBUG_ATTRIBUTE_NORMAL    7
int _DebugAttributesIndex;

#define DEBUG_PBR_LIGHTING_ENABLE_GI 0
#define DEBUG_PBR_LIGHTING_ENABLE_PBR_LIGHTING 1
#define DEBUG_PBR_LIGHTING_ENABLE_ADDITIONAL_LIGHTS 2
#define DEBUG_PBR_LIGHTING_ENABLE_VERTEX_LIGHTING 3
#define DEBUG_PBR_LIGHTING_ENABLE_EMISSION 4
int _DebugPBRLightingMask;

#define DEBUG_MIPMAPMODE_NONE 0
#define DEBUG_MIPMAPMODE_MIP_LEVEL 1
#define DEBUG_MIPMAPMODE_MIP_COUNT 2
#define DEBUG_MIPMAPMODE_MIP_COUNT_REDUCTION 3
//#define DEBUG_MIPMAPMODE_STREAMING_MIP_BUDGET 4
//#define DEBUG_MIPMAPMODE_STREAMING_MIP 5
int _DebugMipIndex;

sampler2D _DebugNumberTexture;

#define DEBUG_VALIDATION_NONE 0
//#define DEBUG_VALIDATION_HIGHLIGHT_NAN_INFINITE_NEGATIVE 1
//#define DEBUG_VALIDATION_HIGHLIGHT_OUTSIDE_RANGE 2
#define DEBUG_VALIDATION_ALBEDO 3
#define DEBUG_VALIDATION_METAL 4
int _DebugValidationIndex;
half _AlbedoMinLuminance = 0.01;
half _AlbedoMaxLuminance = 0.90;
half _AlbedoSaturationTolerance = 0.214;
half _AlbedoHueTolerance = 0.104;
half3 _AlbedoCompareColor = half3(0.5, 0.5, 0.5);

struct DebugData
{
    half3 brdfDiffuse;
    half3 brdfSpecular;
    float2 uv;
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

    return debugData;
}

// Set of colors that should still provide contrast for the Color-blind
#define kPurpleColor float4(156.0 / 255.0, 79.0 / 255.0, 255.0 / 255.0, 1.0) // #9C4FFF
#define kRedColor float4(203.0 / 255.0, 48.0 / 255.0, 34.0 / 255.0, 1.0) // #CB3022
#define kGreenColor float4(8.0 / 255.0, 215.0 / 255.0, 139.0 / 255.0, 1.0) // #08D78B
#define kYellowGreenColor float4(151.0 / 255.0, 209.0 / 255.0, 61.0 / 255.0, 1.0) // #97D13D
#define kBlueColor float4(75.0 / 255.0, 146.0 / 255.0, 243.0 / 255.0, 1.0) // #4B92F3
#define kOrangeBrownColor float4(219.0 / 255.0, 119.0 / 255.0, 59.0 / 255.0, 1.0) // #4B92F3
#define kGrayColor float4(174.0 / 255.0, 174.0 / 255.0, 174.0 / 255.0, 1.0) // #AEAEAE

half4 GetLODDebugColor()
{
    if (IsBitSet(unity_LODFade.z, 0))
        return half4(0.4831376f, 0.6211768f, 0.0219608f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 1))
        return half4(0.2792160f, 0.4078432f, 0.5835296f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 2))
        return half4(0.2070592f, 0.5333336f, 0.6556864f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 3))
        return half4(0.5333336f, 0.1600000f, 0.0282352f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 4))
        return half4(0.3827448f, 0.2886272f, 0.5239216f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 5))
        return half4(0.8000000f, 0.4423528f, 0.0000000f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 6))
        return half4(0.4486272f, 0.4078432f, 0.0501960f, 1.0f);
    if (IsBitSet(unity_LODFade.z, 7))
        return half4(0.7749016f, 0.6368624f, 0.0250984f, 1.0f);
    return half4(0.2,0.2,0.2,1);
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

    if (_DebugLightingIndex == DEBUG_LIGHTING_LIGHT_ONLY || _DebugLightingIndex == DEBUG_LIGHTING_LIGHT_DETAIL)
    {
        surfaceData.albedo = half3(1.0h, 1.0h, 1.0h);
        surfaceData.metallic = 0.0;
        surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
        surfaceData.smoothness = 0.0;
        surfaceData.occlusion = 0.0;
        surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
        changed = true;
    }

    if (_DebugLightingIndex == DEBUG_LIGHTING_LIGHT_ONLY || _DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS)
    {
        half3 normalTS = half3(0.0h, 0.0h, 1.0h);
        #if defined(_NORMALMAP)
        inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentMatrixWS);
        #else
        inputData.normalWS = TransformObjectToWorldDir(normalTS);
        #endif
        inputData.normalTS = normalTS;
        surfaceData.normalTS = normalTS;
        changed = true;
    }

    if (_DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS)
    {
        surfaceData.albedo = half3(0.0h, 0.0h, 0.0h);
        surfaceData.smoothness = 1.0;
        surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
        changed = true;
    }

    if (_DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS_WITH_SMOOTHNESS)
    {
        surfaceData.albedo = half3(0.0h, 0.0h, 0.0h);
        surfaceData.metallic = 1.0;
        surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
        changed = true;
    }
    return changed;
}

//sampler2D _DebugNumberTexture;
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

float GetMipMapLevel(float2 nonNormalizedUVCoordinate)
{
    // The OpenGL Graphics System: A Specification 4.2
    //  - chapter 3.9.11, equation 3.21

    float2  dx_vtc = ddx(nonNormalizedUVCoordinate);
    float2  dy_vtc = ddy(nonNormalizedUVCoordinate);
    float delta_max_sqr = max(dot(dx_vtc, dx_vtc), dot(dy_vtc, dy_vtc));

    return 0.5 * log2(delta_max_sqr);
}

half4 GetMipLevelDebugColor(InputData inputData, float2 texelSize, float2 uv)
{
    float4 lut[10] = {
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

    uint mipLevel = clamp(GetMipMapLevel(uv * texelSize), 0, 9);
    half4 fc = (half4)(lut[mipLevel]) * 0.8;
    fc *= GetTextNumber(mipLevel, inputData.positionWS);

    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * 0.2 + fc;
}

half4 GetMipCountDebugColor(InputData inputData, float2 uv)
{
    const uint maxMips = 9;
    uint mipCount = clamp(GetMipCount(_BaseMap), 0, maxMips);
    half4 fc = lerp(half4(1, 0, 0, 1), half4(1,1,1,1), mipCount / maxMips) * 0.8;
    fc *= GetTextNumber(mipCount, inputData.positionWS) * 2.0;

    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * 0.2 + fc;
    return fc;
}

float3 GetTextureDataDebug(InputData inputData, uint paramId, float2 uv, float4 texelSize, float4 mipInfo, float3 originalColor)
{
    float3 outColor = originalColor;

    switch (paramId)
    {
    case DEBUG_MIPMAPMODE_MIP_LEVEL:
        outColor = GetMipLevelDebugColor(inputData, texelSize.zw, uv);
        break;
    case DEBUG_MIPMAPMODE_MIP_COUNT:
        outColor = GetMipCountDebugColor(inputData, uv);
        break;
    case DEBUG_MIPMAPMODE_MIP_COUNT_REDUCTION:
        //outColor = GetDebugMipReductionColor(_BaseMap, mipInfo);
        break;
    //case DEBUG_MIPMAPMODE_STREAMING_MIP_BUDGET:
    //    outColor = GetDebugStreamingMipColor(tex, mipInfo);
    //    break;
    //case DEBUG_MIPMAPMODE_STREAMING_MIP:
    //    outColor = GetDebugStreamingMipColorBlended(originalColor, tex, mipInfo);
    //    break;
    }

    return outColor;
}


bool CalculateValidationColorForDebug(InputData inputData, SurfaceData surfaceData, DebugData debugData, out half4 color)
{
    if (_DebugValidationIndex == DEBUG_VALIDATION_ALBEDO)
    {
        half value = LinearRgbToLuminance(surfaceData.albedo);
        if (_AlbedoMinLuminance > value)
        {
             color = half4(1.0f, 0.0f, 0.0f, 1.0f);
        }
        else if (_AlbedoMaxLuminance < value)
        {
             color = half4(0.0f, 1.0f, 0.0f, 1.0f);
        }
        else
        {
            half3 hsv = UnityMeta_RGBToHSV(surfaceData.albedo);
            half hue = hsv.r;
            half sat = hsv.g;

            half3 compHSV = UnityMeta_RGBToHSV(_AlbedoCompareColor.rgb);
            half compHue = compHSV.r;
            half compSat = compHSV.g;

            if ((compSat - _AlbedoSaturationTolerance > sat) || ((compHue - _AlbedoHueTolerance > hue) && (compHue - _AlbedoHueTolerance + 1.0 > hue)))
            {
                color = half4(1.0f, 0.0f, 0.0f, 1.0f);
            }
            else if ((sat > compSat + _AlbedoSaturationTolerance) || ((hue > compHue + _AlbedoHueTolerance) && (hue > compHue + _AlbedoHueTolerance - 1.0)))
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
    
    return false;
}

bool CalculateValidationColorForMipMaps(InputData inputData, SurfaceData surfaceData, DebugData debugData, out half4 color)
{
    if (_DebugMipIndex > 0)
    {
        half3 debugCol = GetTextureDataDebug(inputData, _DebugMipIndex, debugData.uv, _BaseMap_TexelSize, _BaseMap_MipInfo, surfaceData.albedo);
        color = half4(debugCol, 1.0f);
        return true;
    }
    
    return false;
}

bool CalculateColorForDebugMaterial(InputData inputData, SurfaceData surfaceData, DebugData debugData, out half4 color)
{
    color = half4(0.0, 0.0, 0.0, 1.0);

    // Debug materials...
    switch(_DebugMaterialIndex)
    {
        case DEBUG_UNLIT:
            color.rgb = surfaceData.albedo;
            return true;

        case DEBUG_DIFFUSE:
            color.rgb = debugData.brdfDiffuse;
            return true;

        case DEBUG_SPECULAR:
            color.rgb = debugData.brdfSpecular;
            return true;

        case DEBUG_ALPHA:
            color.rgb = (1.0 - surfaceData.alpha).xxx;
            return true;

        case DEBUG_SMOOTHNESS:
            color.rgb = surfaceData.smoothness.xxx;
            return true;

        case DEBUG_OCCLUSION:
            color.rgb = surfaceData.occlusion.xxx;
            return true;

        case DEBUG_EMISSION:
            color.rgb = surfaceData.emission;
            return true;

        case DEBUG_NORMAL_WORLD_SPACE:
            color.rgb = inputData.normalWS.xyz * 0.5 + 0.5;
            return true;

        case DEBUG_NORMAL_TANGENT_SPACE:
            color.rgb = surfaceData.normalTS.xyz * 0.5 + 0.5;
            return true;
        case DEBUG_LOD:
            color.rgb = GetLODDebugColor().rgb;
            return true;
        case DEBUG_METALLIC:
            color.rgb = surfaceData.metallic.xxx;
            return true;

        default:
            return false;
    }
}

bool CalculateColorForDebug(InputData inputData, SurfaceData surfaceData, DebugData debugData, out half4 color)
{
    if(CalculateColorForDebugMaterial(inputData, surfaceData, debugData, color))
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
        return false;
    }
}

half4 CalculateDebugShadowCascadeColor(InputData inputData)
{
    float4 shadowCoord = inputData.shadowCoord;
    float3 positionWS = inputData.positionWS;
    Light mainLight = GetMainLight(shadowCoord);
    half cascadeIndex = ComputeCascadeIndex(positionWS);

    float4 cascadeColors[] =
    {
        kBlueColor,
        kGreenColor,
        kYellowGreenColor,
        kRedColor,
    };

    return (half4)(cascadeColors[cascadeIndex]);
}

half4 CalculateDebugLightingComplexityColor(InputData inputData)
{
    float4 lut[5] = {
            float4(0, 1, 0, 0),
            float4(0.25, 0.75, 0, 0),
            float4(0.498, 0.5019, 0.0039, 0),
            float4(0.749, 0.247, 0, 0),
            float4(1, 0, 0, 0)
    };

    // Assume a main light and add 1 to the additional lights.
    unsigned int numLights = clamp(GetAdditionalLightsCount()+1, 0, 4);
    half4 fc = (half4)(lut[numLights]);

    float4 clipPos = TransformWorldToHClip(inputData.positionWS);
    float2 ndc = saturate((clipPos.xy / clipPos.w) * 0.5 + 0.5);

#if UNITY_UV_STARTS_AT_TOP
    if(_ProjectionParams.x < 0)
        ndc.y = 1.0 - ndc.y;
#endif

    const float invNumChar = 1.0 / 10.0f;
    ndc.x *= 5.0;
    ndc.y *= 15.0;
    ndc.x = fmod(ndc.x, invNumChar) + (numLights * invNumChar);

    fc *= tex2D(_DebugNumberTexture, ndc.xy);

    return fc;
}

bool IsLightingFeatureEnabled(uint bitIndex)
{
    return (_DebugPBRLightingMask == 0) || IsBitSet(_DebugPBRLightingMask, bitIndex);
}

#endif
