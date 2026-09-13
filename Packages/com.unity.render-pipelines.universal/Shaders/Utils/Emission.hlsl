#ifndef UNIVERSAL_EMISSION_INCLUDED
#define UNIVERSAL_EMISSION_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// FEATURES_EMISSION allows the preprocessor to know if the _EMISSION might be used.
// Dynamic branch mode: always true if _EMISSION feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _EMISSION is active.
#if (_EMISSION_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_EMISSION
    #define FEATURES_EMISSION 1
#elif !defined(_EMISSION_KEYWORD_DECLARED) || (_EMISSION_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_EMISSION)
        #undef FEATURES_EMISSION
        #define FEATURES_EMISSION 1
    #endif
#endif


// Runtime queries for the _EMISSION shader feature state, safe whether or not the
// keyword is declared in the current pass.

// The DEFINED_NONZERO arm keeps a leaked `#define _KW 0` fallback from reading as enabled.

// Returns true if the _EMISSION variant is active.
bool UseEmission()
{
#if defined(_EMISSION_KEYWORD_DECLARED)
    return _EMISSION;
#elif DEFINED_NONZERO(_EMISSION)
    return true;
#else
    return false;
#endif
}

// Simple getters the including material's input file must define.
half4 SampleEmissionMap(float2 uv);
half4 GetEmissionColor();

// Returns the tinted emission color, or black when emission is disabled.
half3 SampleEmission(float2 uv, bool useEmission)
{
    return useEmission ? SampleEmissionMap(uv).rgb * GetEmissionColor().rgb : half3(0.0h, 0.0h, 0.0h);
}

half3 SampleEmission(float2 uv)
{
    return SampleEmission(uv, UseEmission());
}

#endif // UNIVERSAL_EMISSION_INCLUDED
