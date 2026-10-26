#ifndef UNIVERSAL_OCCLUSION_MAP_INCLUDED
#define UNIVERSAL_OCCLUSION_MAP_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// FEATURES_OCCLUSIONMAP allows the preprocessor to know if the _OCCLUSIONMAP might be used.
// Dynamic branch mode: always true if _OCCLUSIONMAP feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _OCCLUSIONMAP is active.
#if (_OCCLUSIONMAP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_OCCLUSIONMAP
    #define FEATURES_OCCLUSIONMAP 1
#elif !defined(_OCCLUSIONMAP_KEYWORD_DECLARED) || (_OCCLUSIONMAP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_OCCLUSIONMAP)
        #undef FEATURES_OCCLUSIONMAP
        #define FEATURES_OCCLUSIONMAP 1
    #endif
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

// Runtime queries for the _OCCLUSIONMAP shader feature state, safe whether or not the
// keyword is declared in the current pass.

// The DEFINED_NONZERO arm keeps a leaked `#define _KW 0` fallback from reading as enabled.

// Returns true if the _OCCLUSIONMAP variant is active.
bool UseOcclusionMap()
{
#if defined(_OCCLUSIONMAP_KEYWORD_DECLARED)
    return _OCCLUSIONMAP;
#elif DEFINED_NONZERO(_OCCLUSIONMAP)
    return true;
#else
    return false;
#endif
}

// Simple getters the including material's input file must define.
half4 SampleOcclusionMap(float2 uv);
half GetOcclusionStrength();

// Returns the occlusion factor, or 1 when the material has no occlusion map.
half SampleOcclusion(float2 uv)
{
    return UseOcclusionMap() ? LerpWhiteTo(SampleOcclusionMap(uv).g, GetOcclusionStrength()) : half(1.0);
}

#endif // UNIVERSAL_OCCLUSION_MAP_INCLUDED
