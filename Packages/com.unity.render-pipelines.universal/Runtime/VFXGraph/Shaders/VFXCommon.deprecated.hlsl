#ifndef UNIVERSAL_VFX_COMMON_DEPRECATED_INCLUDED
#define UNIVERSAL_VFX_COMMON_DEPRECATED_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// Deprecated keyword-reading wrappers, kept for external back-compat.
// New code passes the keyword state as parameters (VFXCommon.hlsl).

float4 VFXApplyAO(float4 color, float4 posCS, bool surfaceTypeTransparent);

#if !defined(_SURFACE_TYPE_TRANSPARENT_KEYWORD_DECLARED)
    #if !defined(_SURFACE_TYPE_TRANSPARENT)
        #define _SURFACE_TYPE_TRANSPARENT 0
        #define _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_SURFACE_TYPE_TRANSPARENT)
        #undef _SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT 1
    #endif
#endif

// Deprecated. Use VFXApplyAO(color, posCS, surfaceTypeTransparent).
float4 VFXApplyAO(float4 color, float4 posCS)
{
    return VFXApplyAO(color, posCS, _SURFACE_TYPE_TRANSPARENT);
}

// Prevents leaking the fallback keyword definition to shaders that include this file.
#if defined(_SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY)
    #undef _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY
    #undef _SURFACE_TYPE_TRANSPARENT
#endif

#endif // UNIVERSAL_VFX_COMMON_DEPRECATED_INCLUDED
