#ifndef UNIVERSAL_ENVIRONMENT_REFLECTIONS_INCLUDED
#define UNIVERSAL_ENVIRONMENT_REFLECTIONS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// FEATURES_ENVIRONMENTREFLECTIONS_OFF allows the preprocessor to know if the _ENVIRONMENTREFLECTIONS_OFF might be used.
// Dynamic branch mode: always true if _ENVIRONMENTREFLECTIONS_OFF feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _ENVIRONMENTREFLECTIONS_OFF is active.
#if (_ENVIRONMENTREFLECTIONS_OFF_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_ENVIRONMENTREFLECTIONS_OFF
    #define FEATURES_ENVIRONMENTREFLECTIONS_OFF 1
#elif !defined(_ENVIRONMENTREFLECTIONS_OFF_KEYWORD_DECLARED) || (_ENVIRONMENTREFLECTIONS_OFF_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_ENVIRONMENTREFLECTIONS_OFF)
        #undef FEATURES_ENVIRONMENTREFLECTIONS_OFF
        #define FEATURES_ENVIRONMENTREFLECTIONS_OFF 1
    #endif
#endif


// Runtime query for the _ENVIRONMENTREFLECTIONS_OFF shader feature state, safe whether or not
// the keyword is declared in the current pass.

// The DEFINED_NONZERO arm keeps a leaked `#define _KW 0` fallback from reading as enabled.

// True when environment reflections are enabled (inverts the _ENVIRONMENTREFLECTIONS_OFF keyword).
bool UseEnvironmentReflections()
{
#if defined(_ENVIRONMENTREFLECTIONS_OFF_KEYWORD_DECLARED)
    return !_ENVIRONMENTREFLECTIONS_OFF;
#elif DEFINED_NONZERO(_ENVIRONMENTREFLECTIONS_OFF)
    return false;
#else
    return true;
#endif
}

#endif // UNIVERSAL_ENVIRONMENT_REFLECTIONS_INCLUDED
