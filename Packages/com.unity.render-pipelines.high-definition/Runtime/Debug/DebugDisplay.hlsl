#ifdef DEBUG_DISPLAY // Guard define here to be compliant with how shader graph generate code for include

#ifndef UNITY_DEBUG_DISPLAY_INCLUDED
#define UNITY_DEBUG_DISPLAY_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreaming.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Debug.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/MaterialDebug.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/LightingDebug.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/MipMapDebug.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/ColorPickerDebug.cs.hlsl"


// Local shader variables
static SHADOW_TYPE g_DebugShadowAttenuation = 0;

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/PBRValidator.hlsl"

// When displaying lux meter we compress the light in order to be able to display value higher than 65504
// The sun is between 100 000 and 150 000, so we use 4 to be able to cover such a range (4 * 65504)
#define LUXMETER_COMPRESSION_RATIO  4

TEXTURE2D(_DebugMatCapTexture);

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

        case DEBUGVIEWPROPERTIES_INSTANCING:
#if defined(UNITY_INSTANCING_ENABLED)
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

        case DEBUGVIEWPROPERTIES_DEFERRED_MATERIALS:
#ifdef _DEFERRED_CAPABLE_MATERIAL
            result = _DebugIsLitShaderModeDeferred ? float3(0.0, 1.0, 0.0) : float3(1.0, 0.0, 0.0);
#else
            result = float3(1.0, 0.0, 0.0);
#endif
            break;
    }
}

float3 BlitScreenSpaceDigit(float3 originalColor, uint2 screenSpaceCoords, int digit, uint spacing, bool invertColors)
{
    float3 outColor = originalColor;

    const uint2 pixCoord = screenSpaceCoords / 2;
    const uint2 tileSize = uint2(spacing, spacing);
    const int2 coord = (pixCoord & (tileSize - 1)) - int2(tileSize.x/4+1, tileSize.y/3-3);

    UNITY_LOOP for (int i = 0; i <= 1; ++i)
    {
        // 0 == shadow, 1 == text
        if (SampleDebugFontNumber2Digits(coord + i, digit))
        {
            outColor = (i == 0)
                ? (invertColors ? float3(1, 1, 1) : float3(0, 0, 0))
                : (invertColors ? float3(0, 0, 0) : float3(1, 1, 1));
        }
    }

    return outColor;
}

void GetHatchedColor(uint2 screenSpaceCoords, float3 hatchingColor, inout float3 debugColor)
{
    const uint spacing = 16; // increase spacing compared to the legend (easier on the eyes)
    const uint thickness = 3;
    if((screenSpaceCoords.x + screenSpaceCoords.y) % spacing < thickness)
        debugColor = hatchingColor;
}

void GetHatchedColor(uint2 screenSpaceCoords, inout float3 debugColor)
{
    GetHatchedColor(screenSpaceCoords, float3(0.1, 0.1, 0.1), debugColor);
}

// Keep in sync with CalculateColorForDebugMipmapStreaming in URP's ShaderLibrary/Debug/DebuggingCommon.hlsl
#define TERRAIN_STREAM_INFO float4(0.0f, 0.0f, float(6 | (4 << 4)), 0.0f) // 0-15 are reserved for per-texture codes (use "6" to indicate terrain); per-material code "4" signifies "warnings/issues"
#define GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(positionSS, streamingUv, tex) GetTextureDataDebug(_DebugMipMapMode, positionSS, streamingUv, tex, tex##_TexelSize, tex##_MipInfo, TERRAIN_STREAM_INFO)
#define GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_NO_TEX(positionSS, streamingUv) GetTextureDataDebug(_DebugMipMapMode, positionSS, streamingUv, float3(1.0f, 1.0f, 1.0f), 0, float4(0.0f, 0.0f, 0.0f, 0.0f), float4(0.0f, 0.0f, 0.0f, 0.0f), TERRAIN_STREAM_INFO) // Used exclusively for layers 4-7 debug when terrain only has 4 layers
#define GET_TEXTURE_STREAMING_DEBUG(positionSS, uv) GetTextureDataDebug(_DebugMipMapMode, positionSS, TRANSFORM_TEX(uv.xy, unity_MipmapStreaming_DebugTex), unity_MipmapStreaming_DebugTex, unity_MipmapStreaming_DebugTex_TexelSize, unity_MipmapStreaming_DebugTex_MipInfo, unity_MipmapStreaming_DebugTex_StreamInfo)
#define GET_TEXTURE_STREAMING_DEBUG_NO_UV(positionSS) GetTextureDataDebug(_DebugMipMapMode, positionSS, float2(0.0f, 0.0f), unity_MipmapStreaming_DebugTex, unity_MipmapStreaming_DebugTex_TexelSize, unity_MipmapStreaming_DebugTex_MipInfo, unity_MipmapStreaming_DebugTex_StreamInfo)
float3 GetTextureDataDebug(uint paramId, uint2 screenSpaceCoords, float2 uv, float3 originalColor, uint mipCount, float4 texelSize, float4 mipInfo, float4 streamInfo)
{
    float3 outColor = originalColor;

    bool needsHatching;
    switch (paramId)
    {
    case DEBUGMIPMAPMODE_MIP_RATIO:
        outColor = GetDebugMipColorIncludingMipReduction(originalColor, mipCount, texelSize, uv, mipInfo);
        break;
    case DEBUGMIPMAPMODE_MIP_COUNT:
        outColor = GetDebugMipCountColor(mipCount, needsHatching);
        if (needsHatching)
            GetHatchedColor(screenSpaceCoords, outColor);

        if (mipCount > 0 && mipCount <= 14)
        {
            outColor = BlitScreenSpaceDigit(outColor, screenSpaceCoords, mipCount, 32, true);
        }
        
        break;
    case DEBUGMIPMAPMODE_MIP_STREAMING_PERFORMANCE:
        outColor = GetDebugStreamingMipColor(mipCount, mipInfo, streamInfo, needsHatching);
        if (needsHatching)
        {
            float3 hatchingColor = GetDebugMipCountHatchingColor(mipCount);
            GetHatchedColor(screenSpaceCoords, hatchingColor, outColor);
        };
        break;
    case DEBUGMIPMAPMODE_MIP_STREAMING_STATUS:
        if (_DebugMipMapStatusMode == DEBUGMIPMAPSTATUSMODE_TEXTURE)
            outColor = GetDebugStreamingStatusColor(streamInfo, needsHatching);
        else
            outColor = GetDebugPerMaterialStreamingStatusColor(streamInfo, needsHatching);

        if (needsHatching)
            GetHatchedColor(screenSpaceCoords, outColor);

        if (_DebugMipMapShowStatusCode && _DebugMipMapStatusMode == DEBUGMIPMAPSTATUSMODE_TEXTURE && !IsStreaming(streamInfo))
        {
            if (GetStatusCode(streamInfo, false) != kMipmapDebugStatusCodeNotSet && GetStatusCode(streamInfo, false) != kMipmapDebugStatusCodeNoTexture) // we're ignoring these because there's just one status anyway (so the color itself is enough)
                outColor = BlitScreenSpaceDigit(outColor, screenSpaceCoords, GetStatusCode(streamInfo, false), 16, false);
        }

        break;
    case DEBUGMIPMAPMODE_MIP_STREAMING_PRIORITY:
        outColor = GetDebugStreamingPriorityColor(streamInfo);
        break;
    case DEBUGMIPMAPMODE_MIP_STREAMING_ACTIVITY:
        outColor = GetDebugStreamingRecentlyUpdatedColor(_DebugCurrentRealTime, _DebugMipMapRecentlyUpdatedCooldown, _DebugMipMapStatusMode == DEBUGMIPMAPSTATUSMODE_MATERIAL, streamInfo);
        break;
    }

    return lerp(originalColor, outColor, _DebugMipMapOpacity);
}

float3 GetTextureDataDebug(uint paramId, uint2 screenSpaceCoords, float2 uv, Texture2D tex, float4 texelSize, float4 mipInfo, float4 streamInfo)
{
    const float3 originalColor = SAMPLE_TEXTURE2D(tex, s_linear_repeat_sampler, uv).xyz;
    const uint mipCount = GetMipCount(TEXTURE2D_ARGS(tex, s_point_clamp_sampler));

    return GetTextureDataDebug(paramId, screenSpaceCoords, uv, originalColor, mipCount, texelSize, mipInfo, streamInfo);
}

// Draw a signed integer
// Can't display more than 16 digit
// The two following parameter are for float representation
// leading0 is used when drawing frac part of a float to draw the leading 0 (call is in charge of it)
// forceNegativeSign is used to force to display a negative sign as -0 is not recognize
void DrawInteger(int intValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, inout float3 color, int leading0, bool forceNegativeSign)
{
    const uint maxStringSize = 16;

    uint absIntValue = abs(intValue);

    // 1. Get size of the number of display
    int numEntries = min((intValue == 0 ? 0 : log10(absIntValue)) + ((intValue < 0 || forceNegativeSign) ? 1 : 0) + leading0, maxStringSize);

    // 2. Shift curseur to last location as we will go reverse
    fixedUnormCoord.x += numEntries * DEBUG_FONT_TEXT_SCALE_WIDTH;

    // 3. Display the number
    bool drawCharacter = true; // bit weird, but it is to appease the compiler.
    for (uint j = 0; j < maxStringSize; ++j)
    {
        // Numeric value incurrent font start on the second row at 0
        if(drawCharacter)
            DrawCharacter((absIntValue % 10) + '0', fontColor, currentUnormCoord, fixedUnormCoord, color, -1);

        if (absIntValue  < 10)
            drawCharacter = false;

        absIntValue /= 10;
    }

    // 4. Display leading 0
    if (leading0 > 0)
    {
        for (int i = 0; i < leading0; ++i)
        {
            DrawCharacter('0', fontColor, currentUnormCoord, fixedUnormCoord, color, -1);
        }
    }

    // 5. Display sign
    if (intValue < 0 || forceNegativeSign)
    {
        DrawCharacter('-', fontColor, currentUnormCoord, fixedUnormCoord, color, -1);
    }

    // 6. Reset cursor at end location
    fixedUnormCoord.x += (numEntries + 2) * DEBUG_FONT_TEXT_SCALE_WIDTH;
}

void DrawInteger(int intValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, inout float3 color)
{
    DrawInteger(intValue, fontColor, currentUnormCoord, fixedUnormCoord, color, 0, false);
}

void DrawFloatExplicitPrecision(float floatValue, float3 fontColor, uint2 currentUnormCoord, uint digitCount, inout uint2 fixedUnormCoord, inout float3 color)
{
    if (IsNaN(floatValue))
    {
        DrawCharacter('N', fontColor, currentUnormCoord, fixedUnormCoord, color);
        DrawCharacter('a', fontColor, currentUnormCoord, fixedUnormCoord, color);
        DrawCharacter('N', fontColor, currentUnormCoord, fixedUnormCoord, color);
    }
    else
    {
        int intValue = int(floatValue);
        bool forceNegativeSign = floatValue >= 0.0f ? false : true;
        DrawInteger(intValue, fontColor, currentUnormCoord, fixedUnormCoord, color, 0, forceNegativeSign);
        DrawCharacter('.', fontColor, currentUnormCoord, fixedUnormCoord, color);
        int fracValue = int(frac(abs(floatValue)) * pow(10, digitCount));
        int leading0 = digitCount - (int(log10(fracValue)) + 1); // Counting leading0 to add in front of the float
        DrawInteger(fracValue, fontColor, currentUnormCoord, fixedUnormCoord, color, leading0, false);
    }
}

void DrawFloat(float floatValue, float3 fontColor, uint2 currentUnormCoord, inout uint2 fixedUnormCoord, inout float3 color)
{
    DrawFloatExplicitPrecision(floatValue, fontColor, currentUnormCoord, 6, fixedUnormCoord, color);
}

// Debug rendering is performed at the end of the frame (after post-processing).
// Debug textures are never flipped upside-down automatically. Therefore, we must always flip manually.
bool ShouldFlipDebugTexture()
{
    #if UNITY_UV_STARTS_AT_TOP
        return (_ProjectionParams.x > 0);
    #else
        return (_ProjectionParams.x < 0);
    #endif
}

#endif

#endif // DEBUG_DISPLAY
