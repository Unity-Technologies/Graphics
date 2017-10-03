#ifndef UNITY_DEBUG_DISPLAY_INCLUDED
#define UNITY_DEBUG_DISPLAY_INCLUDED

#include "DebugDisplay.cs.hlsl"

// Set of parameters available when switching to debug shader mode
int _DebugLightingMode; // Match enum DebugLightingMode
int _DebugViewMaterial; // Contain the id (define in various materialXXX.cs.hlsl) of the property to display
float4 _DebugLightingAlbedo; // xyz = albedo for diffuse, w unused
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

        case DEBUGVIEWPROPERTIES_PER_PIXEL_DISPLACEMENT:
#ifdef _PER_PIXEL_DISPLACEMENT // Caution: This define is related to a shader features (But it may become a standard features for HD
            result = float3(1.0, 0.0, 0.0);
#else
            result = float3(0.0, 0.0, 0.0);
#endif
            break;

        case DEBUGVIEWPROPERTIES_DEPTH_OFFSET:
#ifdef _DEPTHOFFSET_ON  // Caution: This define is related to a shader features (But it may become a standard features for HD
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

#endif
