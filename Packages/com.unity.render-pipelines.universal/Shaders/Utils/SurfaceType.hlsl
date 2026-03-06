#ifndef UNIVERSAL_SURFACE_TYPE_TRANSPARENT_INCLUDED
#define UNIVERSAL_SURFACE_TYPE_TRANSPARENT_INCLUDED
// Utility functionality for Universal RP materials that has the _SURFACE_TYPE_TRANSPARENT shader feature.

// The _Surface property has been removed, but we add this is a fallback.
#if defined(_SURFACE_TYPE_TRANSPARENT_KEYWORD_DECLARED) || defined(_SURFACE_TYPE_TRANSPARENT)
#if defined(_Surface)
#undef _Surface
#endif
// This property is deprecated. Use parameterless IsSurfaceTypeTransparent() instead.
#define _Surface _SURFACE_TYPE_TRANSPARENT
#elif defined(_Surface) // Some shaders hardcode the _Surface property
// Use IsSurfaceTypeTransparent() instead of checking this keyword directly.
#define _SURFACE_TYPE_TRANSPARENT (_Surface > 0)
#define _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY 1
#else
// Use IsSurfaceTypeTransparent() instead of checking this keyword directly.
#define _SURFACE_TYPE_TRANSPARENT 0
#define _SURFACE_TYPE_TRANSPARENT_DEFINED_LOCALLY 1
// This property is deprecated. Use parameterless IsSurfaceTypeTransparent() instead.
static const half _Surface = 0;
#endif

// Returns 'True' if the materials Surface Type is set to 'Transparent'.
inline bool IsSurfaceTypeTransparent()
{
    #if defined(_SURFACE_TYPE_TRANSPARENT_KEYWORD_DECLARED) || defined(_SURFACE_TYPE_TRANSPARENT)
    return _SURFACE_TYPE_TRANSPARENT;
    #else
    return false;
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
