#ifndef UNIVERSAL_SURFACE_TYPE_TRANSPARENT_INCLUDED
#define UNIVERSAL_SURFACE_TYPE_TRANSPARENT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// FEATURES_SURFACE_TYPE_TRANSPARENT allows the preprocessor to know if the _SURFACE_TYPE_TRANSPARENT might be used.
// Dynamic branch mode: always true if _SURFACE_TYPE_TRANSPARENT feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _SURFACE_TYPE_TRANSPARENT is active.
#if (_SURFACE_TYPE_TRANSPARENT_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_SURFACE_TYPE_TRANSPARENT
    #define FEATURES_SURFACE_TYPE_TRANSPARENT 1
#elif !defined(_SURFACE_TYPE_TRANSPARENT_KEYWORD_DECLARED) || (_SURFACE_TYPE_TRANSPARENT_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_SURFACE_TYPE_TRANSPARENT)
        #undef FEATURES_SURFACE_TYPE_TRANSPARENT
        #define FEATURES_SURFACE_TYPE_TRANSPARENT 1
    #endif
#endif
// Utility functionality for Universal RP materials that has the _SURFACE_TYPE_TRANSPARENT shader feature.

// The _Surface property has been removed, but we add this is a fallback.
#if defined(_SURFACE_TYPE_TRANSPARENT_KEYWORD_DECLARED)
    #if defined(_Surface)
        #undef _Surface
    #endif

    // This property is deprecated. Use parameterless IsSurfaceTypeTransparent() instead.
    static const half _Surface = (half)_SURFACE_TYPE_TRANSPARENT;
#elif defined(_Surface) // Some shaders hardcode the _Surface property
    #if !defined(_SURFACE_TYPE_TRANSPARENT)
        // Use IsSurfaceTypeTransparent() instead of checking this keyword directly.
        #define _SURFACE_TYPE_TRANSPARENT (_Surface > 0)
        #define _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY 1
    #endif
#else
    #if !defined(_SURFACE_TYPE_TRANSPARENT)
        // Use IsSurfaceTypeTransparent() instead of checking this keyword directly.
        #define _SURFACE_TYPE_TRANSPARENT 0
        #define _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY 1
    #endif

    // This property is deprecated. Use parameterless IsSurfaceTypeTransparent() instead.
    static const half _Surface = 0;
#endif

// Returns 'True' if the materials Surface Type is set to 'Transparent'.
inline bool IsSurfaceTypeTransparent()
{
    #if defined(_SURFACE_TYPE_TRANSPARENT_KEYWORD_DECLARED)
        return _SURFACE_TYPE_TRANSPARENT;
    #else
        #if defined(_SURFACE_TYPE_TRANSPARENT)
            return _SURFACE_TYPE_TRANSPARENT;
        #else
            return false;
        #endif
    #endif
}

// Returns 'True' if the materials Surface Type is set to 'Opaque'.
inline bool IsSurfaceTypeOpaque()
{
    return !IsSurfaceTypeTransparent();
}

// Prevents leaking _SURFACE_TYPE_TRANSPARENT fallback definition.
// This makes sure #if defined(_SURFACE_TYPE_TRANSPARENT) doesn't suddenly return true for shaders that include this file.
#if defined(_SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY)
    #undef _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY
    #undef _SURFACE_TYPE_TRANSPARENT
#endif

#endif
