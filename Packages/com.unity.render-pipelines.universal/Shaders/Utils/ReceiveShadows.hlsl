#ifndef UNIVERSAL_RECEIVE_SHADOWS_INCLUDED
#define UNIVERSAL_RECEIVE_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// FEATURES_RECEIVE_SHADOWS_OFF allows the preprocessor to know if the _RECEIVE_SHADOWS_OFF might be used.
// Dynamic branch mode: always true if _RECEIVE_SHADOWS_OFF feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _RECEIVE_SHADOWS_OFF is active.
#if (_RECEIVE_SHADOWS_OFF_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_RECEIVE_SHADOWS_OFF
    #define FEATURES_RECEIVE_SHADOWS_OFF 1
#elif !defined(_RECEIVE_SHADOWS_OFF_KEYWORD_DECLARED) || (_RECEIVE_SHADOWS_OFF_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_RECEIVE_SHADOWS_OFF)
        #undef FEATURES_RECEIVE_SHADOWS_OFF
        #define FEATURES_RECEIVE_SHADOWS_OFF 1
    #endif
#endif

// True only when _RECEIVE_SHADOWS_OFF is enabled at compile time.
#if !defined(_RECEIVE_SHADOWS_OFF_KEYWORD_DECLARED) || (_RECEIVE_SHADOWS_OFF_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_RECEIVE_SHADOWS_OFF)
        #undef _RECEIVE_SHADOWS_OFF_STATICALLY_ENABLED
        #define _RECEIVE_SHADOWS_OFF_STATICALLY_ENABLED 1
    #endif
#endif


// Runtime queries for the _RECEIVE_SHADOWS_OFF shader feature state, safe whether or not the
// keyword is declared in the current pass.

// The DEFINED_NONZERO arm keeps a leaked `#define _KW 0` fallback from reading as enabled.

// True when the material receives shadows (inverts the _RECEIVE_SHADOWS_OFF keyword).
bool ReceiveShadows()
{
#if defined(_RECEIVE_SHADOWS_OFF_KEYWORD_DECLARED)
    return !_RECEIVE_SHADOWS_OFF;
#elif DEFINED_NONZERO(_RECEIVE_SHADOWS_OFF)
    return false;
#else
    return true;
#endif
}

#endif // UNIVERSAL_RECEIVE_SHADOWS_INCLUDED
