#ifndef UNIVERSAL_SPEC_GLOSS_INCLUDED
#define UNIVERSAL_SPEC_GLOSS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// FEATURES_GLOSSINESS_FROM_BASE_ALPHA allows the preprocessor to know if the _GLOSSINESS_FROM_BASE_ALPHA might be used.
// Dynamic branch mode: always true if _GLOSSINESS_FROM_BASE_ALPHA feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _GLOSSINESS_FROM_BASE_ALPHA is active.
#if (_GLOSSINESS_FROM_BASE_ALPHA_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_GLOSSINESS_FROM_BASE_ALPHA
    #define FEATURES_GLOSSINESS_FROM_BASE_ALPHA 1
#elif !defined(_GLOSSINESS_FROM_BASE_ALPHA_KEYWORD_DECLARED) || (_GLOSSINESS_FROM_BASE_ALPHA_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_GLOSSINESS_FROM_BASE_ALPHA)
        #undef FEATURES_GLOSSINESS_FROM_BASE_ALPHA
        #define FEATURES_GLOSSINESS_FROM_BASE_ALPHA 1
    #endif
#endif

// FEATURES_SPECGLOSSMAP allows the preprocessor to know if the _SPECGLOSSMAP might be used.
// Dynamic branch mode: always true if _SPECGLOSSMAP feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _SPECGLOSSMAP is active.
#if (_SPECGLOSSMAP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_SPECGLOSSMAP
    #define FEATURES_SPECGLOSSMAP 1
#elif !defined(_SPECGLOSSMAP_KEYWORD_DECLARED) || (_SPECGLOSSMAP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_SPECGLOSSMAP)
        #undef FEATURES_SPECGLOSSMAP
        #define FEATURES_SPECGLOSSMAP 1
    #endif
#endif

// FEATURES_SPECULAR_COLOR allows the preprocessor to know if the _SPECULAR_COLOR might be used.
// Dynamic branch mode: always true if _SPECULAR_COLOR feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _SPECULAR_COLOR is active.
#if (_SPECULAR_COLOR_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_SPECULAR_COLOR
    #define FEATURES_SPECULAR_COLOR 1
#elif !defined(_SPECULAR_COLOR_KEYWORD_DECLARED) || (_SPECULAR_COLOR_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_SPECULAR_COLOR)
        #undef FEATURES_SPECULAR_COLOR
        #define FEATURES_SPECULAR_COLOR 1
    #endif
#endif


// Runtime queries for the SimpleLit-family specular gloss shader features, safe whether or
// not the keywords are declared in the current pass. These act as one unit: the
// _SPECGLOSSMAP / _SPECULAR_COLOR axis selects the specular source (the empty state means
// no specular), and _GLOSSINESS_FROM_BASE_ALPHA redirects the source's smoothness channel.

// The DEFINED_NONZERO arm keeps a leaked `#define _KW 0` fallback from reading as enabled.

// Returns true if the _SPECGLOSSMAP variant is active.
bool UseSpecGlossMap()
{
#if defined(_SPECGLOSSMAP_KEYWORD_DECLARED)
    return _SPECGLOSSMAP;
#elif DEFINED_NONZERO(_SPECGLOSSMAP)
    return true;
#else
    return false;
#endif
}

// Returns true if the _SPECULAR_COLOR variant is active.
bool UseSpecularColor()
{
#if defined(_SPECULAR_COLOR_KEYWORD_DECLARED)
    return _SPECULAR_COLOR;
#elif DEFINED_NONZERO(_SPECULAR_COLOR)
    return true;
#else
    return false;
#endif
}

// Returns true if the _GLOSSINESS_FROM_BASE_ALPHA variant is active.
bool UseGlossinessFromBaseAlpha()
{
#if defined(_GLOSSINESS_FROM_BASE_ALPHA_KEYWORD_DECLARED)
    return _GLOSSINESS_FROM_BASE_ALPHA;
#elif DEFINED_NONZERO(_GLOSSINESS_FROM_BASE_ALPHA)
    return true;
#else
    return false;
#endif
}

#endif // UNIVERSAL_SPEC_GLOSS_INCLUDED
