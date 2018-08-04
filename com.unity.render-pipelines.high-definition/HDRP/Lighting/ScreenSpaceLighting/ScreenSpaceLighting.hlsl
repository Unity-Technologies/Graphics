#ifndef UNITY_SCREENSPACELIGHTING_INCLUDED
 #define UNITY_SCREENSPACELIGHTING_INCLUDED

#include "HDRP/Lighting/ScreenSpaceLighting/ScreenSpaceLighting.cs.hlsl"
#include "HDRP/Lighting/Reflection/VolumeProjection.hlsl"

#define SSRTID Reflection
#include "HDRP/Lighting/ScreenSpaceLighting/ScreenSpaceTracing.hlsl"
#undef SSRTID

#include "CoreRP/ShaderLibrary/Refraction.hlsl"
#define SSRTID Refraction
#include "HDRP/Lighting/ScreenSpaceLighting/ScreenSpaceTracing.hlsl"
#undef SSRTID

#endif
