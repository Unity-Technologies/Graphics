#ifndef UNIVERSAL_ALPHA_BLEND_INCLUDED
#define UNIVERSAL_ALPHA_BLEND_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// FEATURES_ALPHAMODULATE_ON allows the preprocessor to know if the _ALPHAMODULATE_ON might be used.
// Dynamic branch mode: always true if _ALPHAMODULATE_ON feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _ALPHAMODULATE_ON is active.
#if (_ALPHAMODULATE_ON_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_ALPHAMODULATE_ON
    #define FEATURES_ALPHAMODULATE_ON 1
#elif !defined(_ALPHAMODULATE_ON_KEYWORD_DECLARED) || (_ALPHAMODULATE_ON_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_ALPHAMODULATE_ON)
        #undef FEATURES_ALPHAMODULATE_ON
        #define FEATURES_ALPHAMODULATE_ON 1
    #endif
#endif

// FEATURES_ALPHAPREMULTIPLY_ON allows the preprocessor to know if the _ALPHAPREMULTIPLY_ON might be used.
// Dynamic branch mode: always true if _ALPHAPREMULTIPLY_ON feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _ALPHAPREMULTIPLY_ON is active.
#if (_ALPHAPREMULTIPLY_ON_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_ALPHAPREMULTIPLY_ON
    #define FEATURES_ALPHAPREMULTIPLY_ON 1
#elif !defined(_ALPHAPREMULTIPLY_ON_KEYWORD_DECLARED) || (_ALPHAPREMULTIPLY_ON_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_ALPHAPREMULTIPLY_ON)
        #undef FEATURES_ALPHAPREMULTIPLY_ON
        #define FEATURES_ALPHAPREMULTIPLY_ON 1
    #endif
#endif


// Runtime queries for the _ALPHAPREMULTIPLY_ON / _ALPHAMODULATE_ON shader feature state, safe whether or not the
// keyword is declared in the current pass.

// The DEFINED_NONZERO arm keeps a leaked `#define _KW 0` fallback from reading as enabled.

// Returns true if the _ALPHAPREMULTIPLY_ON variant is active.
bool UseAlphaPremultiply()
{
#if defined(_ALPHAPREMULTIPLY_ON_KEYWORD_DECLARED)
    return _ALPHAPREMULTIPLY_ON;
#elif DEFINED_NONZERO(_ALPHAPREMULTIPLY_ON)
    return true;
#else
    return false;
#endif
}

// Returns true if the _ALPHAMODULATE_ON variant is active.
bool UseAlphaModulate()
{
#if defined(_ALPHAMODULATE_ON_KEYWORD_DECLARED)
    return _ALPHAMODULATE_ON;
#elif DEFINED_NONZERO(_ALPHAMODULATE_ON)
    return true;
#else
    return false;
#endif
}

#endif // UNIVERSAL_ALPHA_BLEND_INCLUDED
