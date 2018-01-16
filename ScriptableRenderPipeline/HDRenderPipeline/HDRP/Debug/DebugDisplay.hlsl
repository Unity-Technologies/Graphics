#ifndef UNITY_DEBUG_DISPLAY_INCLUDED
#define UNITY_DEBUG_DISPLAY_INCLUDED

#include "CoreRP/ShaderLibrary/Debug.hlsl"
#include "DebugDisplay.cs.hlsl"
#include "MaterialDebug.cs.hlsl"
#include "LightingDebug.cs.hlsl"
#include "MipMapDebug.cs.hlsl"

// Set of parameters available when switching to debug shader mode
int _DebugLightingMode; // Match enum DebugLightingMode
int _DebugViewMaterial; // Contain the id (define in various materialXXX.cs.hlsl) of the property to display
int _DebugMipMapMode; // Match enum DebugMipMapMode
float4 _DebugLightingAlbedo; // xyz = albedo for diffuse, w unused
float4 _DebugLuxMeterParam; // 4 increasing threshold
float4 _DebugLightingSmoothness; // x == bool override, y == override value
float4 _MousePixelCoord;  // xy unorm, zw norm

TEXTURE2D(_DebugFont); // Debug font to write string in shader

void GetPropertiesDataDebug(uint paramId, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEWPROPERTIES_TESSELLATION:
#ifdef TESSELLATION_ON
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

        case DEBUGVIEWPROPERTIES_PIXEL_DISPLACEMENT:
#ifdef _PIXEL_DISPLACEMENT // Caution: This define is related to a shader features (But it may become a standard features for HD)
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

        case DEBUGVIEWPROPERTIES_VERTEX_DISPLACEMENT:
#ifdef _VERTEX_DISPLACEMENT // Caution: This define is related to a shader features (But it may become a standard features for HD)
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

        case DEBUGVIEWPROPERTIES_TESSELLATION_DISPLACEMENT:
#ifdef _TESSELLATION_DISPLACEMENT // Caution: This define is related to a shader features (But it may become a standard features for HD)
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

        case DEBUGVIEWPROPERTIES_DEPTH_OFFSET:
#ifdef _DEPTHOFFSET_ON  // Caution: This define is related to a shader features (But it may become a standard features for HD)
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

        case DEBUGVIEWPROPERTIES_LIGHTMAP:
#if defined(LIGHTMAP_ON) || defined (DIRLIGHTMAP_COMBINED) || defined(DYNAMICLIGHTMAP_ON)
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

    }
}

float3 GetTextureDataDebug(uint paramId, float2 uv, Texture2D tex, float4 texelSize, float4 mipInfo, float3 originalColor)
{
    switch (paramId)
    {
    case DEBUGMIPMAPMODE_MIP_RATIO:
        return GetDebugMipColorIncludingMipReduction(originalColor, tex, texelSize, uv, mipInfo);
    case DEBUGMIPMAPMODE_MIP_COUNT:
        return GetDebugMipCountColor(originalColor, tex);
    case DEBUGMIPMAPMODE_MIP_COUNT_REDUCTION:
        return GetDebugMipReductionColor(tex, mipInfo);
    case DEBUGMIPMAPMODE_STREAMING_MIP_BUDGET:
        return GetDebugStreamingMipColor(tex, mipInfo);
    case DEBUGMIPMAPMODE_STREAMING_MIP:
        return GetDebugStreamingMipColorBlended(originalColor, tex, mipInfo);
    }

    return originalColor;
}

// font texture is 256x128
#define DEBUG_FONT_TEXT_WIDTH	16
#define DEBUG_FONT_TEXT_HEIGHT	16
#define DEBUG_FONT_TEXT_SIZE	1.0f
#define DEBUG_FONT_TEXT_COUNT_X	16
#define DEBUG_FONT_TEXT_COUNT_Y	8
#define DEBUG_FONT_TEXT_SCALE	0.7f

float3 DrawCharacter(uint asciiValue, float2 currentAbsCoords, inout float2 referenceAbsCoord, float fontSize, float incrementSign)
{
    uint2 asciiCoord = uint2(asciiValue % DEBUG_FONT_TEXT_COUNT_X, asciiValue / DEBUG_FONT_TEXT_COUNT_Y);
    const float charWidth = DEBUG_FONT_TEXT_WIDTH * fontSize;
    const float charHeight = DEBUG_FONT_TEXT_HEIGHT * fontSize;
    const float2 localCoords = currentAbsCoords - referenceAbsCoord;

    float3 output = float3(0, 0, 0);
    if (localCoords.x >= 0 && localCoords.x < charWidth && localCoords.y >= 0 && localCoords.y < charHeight)
    {
        float2 texOffset = float2(asciiCoord) / float2(DEBUG_FONT_TEXT_COUNT_X, DEBUG_FONT_TEXT_COUNT_Y);
        float2 texCoord = localCoords / float2(charWidth * DEBUG_FONT_TEXT_COUNT_X, charHeight * DEBUG_FONT_TEXT_COUNT_Y);
        output = SAMPLE_TEXTURE2D_LOD(_DebugFont, s_linear_clamp_sampler, texCoord + texOffset, 0).xyz;
    }

    referenceAbsCoord.x += charWidth * incrementSign * DEBUG_FONT_TEXT_SCALE;

    return output;
}

float3 DrawCharacter(uint asciiValue, float2 currentAbsCoords, inout float2 referenceAbsCoord, float fontSize)
{
    return DrawCharacter(asciiValue, currentAbsCoords, referenceAbsCoord, fontSize, 1.0f);
}

float GetTextSize(uint intValue, float fontSize)
{
    const uint charWidth = DEBUG_FONT_TEXT_WIDTH * fontSize * DEBUG_FONT_TEXT_SCALE;
    const uint maxCharCount = 16;
    uint charCount = 0;

    for (uint charIt = 0; charIt < maxCharCount; ++charIt)
    {
        ++charCount;
        if (intValue  < 10)
            break;
        intValue /= 10;
    }
    return charCount * charWidth;
}

float3 DrawInteger(in uint intValue, in float2 coords, inout float2 referenceCoord, in float fontSize)
{
    const uint charWidth = DEBUG_FONT_TEXT_WIDTH * fontSize * DEBUG_FONT_TEXT_SCALE;
    const uint maxCharCount = 16;
    float2 localRefCoord = referenceCoord + float2(GetTextSize(intValue, fontSize), 0);
    referenceCoord = localRefCoord + float2(charWidth, 0);

    float3 output = float3(0, 0, 0);
    for (uint charIt = 0; charIt < maxCharCount; ++charIt)
    {
        output += DrawCharacter((intValue % 10) + 48, coords, localRefCoord, fontSize, -1.0f).xyz;
        if (intValue  < 10)
            break;
        intValue /= 10;
    }

    return output;
}

#endif
