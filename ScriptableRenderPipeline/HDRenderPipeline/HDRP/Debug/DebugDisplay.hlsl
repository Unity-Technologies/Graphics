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

#endif
