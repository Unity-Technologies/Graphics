#ifndef UNIVERSAL_PARALLAXMAP_INCLUDED
#define UNIVERSAL_PARALLAXMAP_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
// Utility functionality for Universal RP materials that has the _PARALLAXMAP shader feature.

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"

// FEATURES_PARALLAXMAP allows the preprocessor to know if the _PARALLAXMAP might be used.
// Dynamic branch mode: always true if _PARALLAXMAP feature exists; use UseParallaxMap() to query runtime usage.
// Specialized variants mode: only true when _PARALLAXMAP is active.
#if (_PARALLAXMAP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_PARALLAXMAP
    #define FEATURES_PARALLAXMAP 1
#elif !defined(_PARALLAXMAP_KEYWORD_DECLARED) || (_PARALLAXMAP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_PARALLAXMAP)
        #undef FEATURES_PARALLAXMAP
        #define FEATURES_PARALLAXMAP 1
    #endif
#endif

// Returns true if the _PARALLAXMAP shader feature is enabled, otherwise false.
bool UseParallaxMap()
{
    #if defined(_PARALLAXMAP_KEYWORD_DECLARED)
        return _PARALLAXMAP;
    #else
        #if defined(_PARALLAXMAP)
            return _PARALLAXMAP;
        #else
            return false;
        #endif
    #endif
}

// Simple getters the including material's input file must define.
half4 SampleParallaxMap(float2 uv);
half GetParallax();

#ifndef BUILTIN_TARGET_API
// Offsets the uv by the parallax displacement when the material has a parallax (height) map.
void ApplyPerPixelDisplacement(half3 viewDirTS, inout float2 uv)
{
    if (UseParallaxMap())
        uv += ParallaxOffset1Step(SampleParallaxMap(uv).g, GetParallax(), viewDirTS);
}
#endif

#endif
