#ifndef UNIVERSAL_NORMALMAP_INCLUDED
#define UNIVERSAL_NORMALMAP_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
// Utility functionality for Universal RP materials that has the _NORMALMAP shader feature.

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

// FEATURES_NORMALMAP allows the preprocessor to know if the _NORMALMAP might be used.
// Dynamic branch mode: always true if _NORMALMAP feature exists; use UseNormalMap() to query runtime usage.
// Specialized variants mode: only true when _NORMALMAP is active.
#if (_NORMALMAP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_NORMALMAP
    #define FEATURES_NORMALMAP 1
#elif !defined(_NORMALMAP_KEYWORD_DECLARED) || (_NORMALMAP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_NORMALMAP)
        #undef FEATURES_NORMALMAP
        #define FEATURES_NORMALMAP 1
    #endif
#endif

// Returns true if the _NORMALMAP shader feature is enabled, otherwise false.
bool UseNormalMap()
{
    #if defined(_NORMALMAP_KEYWORD_DECLARED)
        return _NORMALMAP;
    #else
        #if defined(_NORMALMAP)
            return _NORMALMAP;
        #else
            return false;
        #endif
    #endif
}

// Simple getters the including material's input file must define.
half4 SampleBumpMap(float2 uv);
half GetBumpScale();

// Returns the unpacked tangent-space normal, or the flat normal when the normal map is disabled.
half3 SampleNormal(float2 uv, bool useNormalMap)
{
    half3 normalTS = half3(0.0h, 0.0h, 1.0h);
    if (useNormalMap)
    {
        half4 n = SampleBumpMap(uv);
        #if BUMP_SCALE_NOT_SUPPORTED
            normalTS = UnpackNormal(n);
        #else
            normalTS = UnpackNormalScale(n, GetBumpScale());
        #endif
    }
    return normalTS;
}

half3 SampleNormal(float2 uv)
{
    return SampleNormal(uv, UseNormalMap());
}

#endif
