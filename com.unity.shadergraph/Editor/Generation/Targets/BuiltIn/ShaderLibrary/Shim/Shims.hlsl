#ifndef UNITY_SHIMS_INCLUDED
#define UNITY_SHIMS_INCLUDED

#define BUILTIN_TARGET_API

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// Duplicate define in Macros.hlsl
#if defined (TRANSFORM_TEX)
#undef TRANSFORM_TEX
#endif

#include "HLSLSupportShim.hlsl"
#include "InputsShim.hlsl"
#include "SurfaceShaderProxy.hlsl"

#include "UnityShaderVariables.cginc"
#include "UnityShaderUtilities.cginc"
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"


#ifdef POINT
#   define COPY_FROM_LIGHT_COORDS(dest, src) dest = src._LightCoord
#   define COPY_TO_LIGHT_COORDS(dest, src) dest._LightCoord.xyz = src.xyz
#endif

#ifdef SPOT
#   define COPY_FROM_LIGHT_COORDS(dest, src) dest = src._LightCoord.xyz
#   define COPY_TO_LIGHT_COORDS(dest, src) dest._LightCoord.xyz = src.xyz
#endif

#ifdef DIRECTIONAL
#   define COPY_FROM_LIGHT_COORDS(dest, src)
#   define COPY_TO_LIGHT_COORDS(dest, src)
#endif

#ifdef POINT_COOKIE
#   define COPY_FROM_LIGHT_COORDS(dest, src) dest = src._LightCoord.xyz
#   define COPY_TO_LIGHT_COORDS(dest, src) dest._LightCoord.xyz = src.xyz
#endif

#ifdef DIRECTIONAL_COOKIE
#   define COPY_FROM_LIGHT_COORDS(dest, src) dest = float3(src._LightCoord.xy, 1)
#   define COPY_TO_LIGHT_COORDS(dest, src) dest._LightCoord.xy = src.xy
#endif

#endif // UNITY_SHIMS_INCLUDED
