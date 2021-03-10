#ifndef UNITY_SHIMS_INCLUDED
#define UNITY_SHIMS_INCLUDED

#define BUILTIN_TARGET_API

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#include "HLSLSupportShim.hlsl"
#include "InputsShim.hlsl"
#include "SurfaceShaderProxy.hlsl"

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Legacy/UnityShaderVariables.cginc"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Legacy/UnityShaderUtilities.cginc"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Legacy/UnityCG.cginc"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Legacy/Lighting.cginc"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Legacy/UnityPBSLighting.cginc"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Legacy/AutoLight.cginc"


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