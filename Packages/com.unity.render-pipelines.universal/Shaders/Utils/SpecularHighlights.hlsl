#ifndef UNIVERSAL_SPECULAR_HIGHLIGHTS_INCLUDED
#define UNIVERSAL_SPECULAR_HIGHLIGHTS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// FEATURES_SPECULARHIGHLIGHTS_OFF allows the preprocessor to know if the _SPECULARHIGHLIGHTS_OFF might be used.
// Dynamic branch mode: always true if _SPECULARHIGHLIGHTS_OFF feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _SPECULARHIGHLIGHTS_OFF is active.
#if (_SPECULARHIGHLIGHTS_OFF_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_SPECULARHIGHLIGHTS_OFF
    #define FEATURES_SPECULARHIGHLIGHTS_OFF 1
#elif !defined(_SPECULARHIGHLIGHTS_OFF_KEYWORD_DECLARED) || (_SPECULARHIGHLIGHTS_OFF_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_SPECULARHIGHLIGHTS_OFF)
        #undef FEATURES_SPECULARHIGHLIGHTS_OFF
        #define FEATURES_SPECULARHIGHLIGHTS_OFF 1
    #endif
#endif


// Runtime query for the _SPECULARHIGHLIGHTS_OFF shader feature state, safe whether or not the
// keyword is declared in the current pass.

// SimpleLit-family shaders have no _SPECULARHIGHLIGHTS_OFF; they express disabled highlights
// as the empty state of their specular source axis (SpecGloss.hlsl).

// The DEFINED_NONZERO arm keeps a leaked `#define _KW 0` fallback from reading as enabled.

// True when specular highlights are enabled (inverts the _SPECULARHIGHLIGHTS_OFF keyword).
bool UseSpecularHighlights()
{
#if defined(_SPECULARHIGHLIGHTS_OFF_KEYWORD_DECLARED)
    return !_SPECULARHIGHLIGHTS_OFF;
#elif DEFINED_NONZERO(_SPECULARHIGHLIGHTS_OFF)
    return false;
#else
    return true;
#endif
}

#endif // UNIVERSAL_SPECULAR_HIGHLIGHTS_INCLUDED
