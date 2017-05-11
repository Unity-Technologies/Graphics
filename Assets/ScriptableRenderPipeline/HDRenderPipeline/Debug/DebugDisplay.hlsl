#ifndef UNITY_DEBUG_DISPLAY_INCLUDED
#define UNITY_DEBUG_DISPLAY_INCLUDED

#include "DebugDisplay.cs.hlsl"

// Set of parameters available when switching to debug shader mode
int _DebugLightingMode; // Match enum DebugLightingMode
int _DebugViewMaterial; // Contain the id (define in various materialXXX.cs.hlsl) of the property to display
float4 _DebugLightingAlbedo; // xyz = albedo for diffuse, w unused
float4 _DebugLightingSmoothness; // x == bool override, y == override value

#endif
