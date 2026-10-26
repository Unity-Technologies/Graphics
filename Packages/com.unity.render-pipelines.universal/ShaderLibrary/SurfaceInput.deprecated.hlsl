
#ifndef UNIVERSAL_INPUT_SURFACE_DEPRECATED_INCLUDED
#define UNIVERSAL_INPUT_SURFACE_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_BaseMap);
TEXTURE2D(_BumpMap);
SAMPLER(sampler_BumpMap);
TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInputFunctions.deprecated.hlsl"

#endif
