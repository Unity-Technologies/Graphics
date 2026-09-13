#ifndef UNIVERSAL_CLEAR_COAT_INCLUDED
#define UNIVERSAL_CLEAR_COAT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// FEATURES_CLEARCOAT allows the preprocessor to know if the _CLEARCOAT might be used.
// Dynamic branch mode: always true if _CLEARCOAT feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _CLEARCOAT is active.
#if (_CLEARCOAT_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_CLEARCOAT
    #define FEATURES_CLEARCOAT 1
#elif !defined(_CLEARCOAT_KEYWORD_DECLARED) || (_CLEARCOAT_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_CLEARCOAT)
        #undef FEATURES_CLEARCOAT
        #define FEATURES_CLEARCOAT 1
    #endif
#endif

// FEATURES_CLEARCOATMAP allows the preprocessor to know if the _CLEARCOATMAP might be used.
// Dynamic branch mode: always true if _CLEARCOATMAP feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _CLEARCOATMAP is active.
#if (_CLEARCOATMAP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_CLEARCOATMAP
    #define FEATURES_CLEARCOATMAP 1
#elif !defined(_CLEARCOATMAP_KEYWORD_DECLARED) || (_CLEARCOATMAP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_CLEARCOATMAP)
        #undef FEATURES_CLEARCOATMAP
        #define FEATURES_CLEARCOATMAP 1
    #endif
#endif


// Runtime queries for the _CLEARCOAT / _CLEARCOATMAP shader feature state, safe whether or not the
// keyword is declared in the current pass.

// The DEFINED_NONZERO arm keeps a leaked `#define _KW 0` fallback from reading as enabled.

// Returns true if the _CLEARCOAT variant is active.
bool UseClearCoat()
{
#if defined(_CLEARCOAT_KEYWORD_DECLARED)
    return _CLEARCOAT;
#elif DEFINED_NONZERO(_CLEARCOAT)
    return true;
#else
    return false;
#endif
}

// Returns true if the _CLEARCOATMAP variant is active.
bool UseClearCoatMap()
{
#if defined(_CLEARCOATMAP_KEYWORD_DECLARED)
    return _CLEARCOATMAP;
#elif DEFINED_NONZERO(_CLEARCOATMAP)
    return true;
#else
    return false;
#endif
}

// Simple getters the including material's input file must define.
half4 SampleClearCoatMap(float2 uv);
half GetClearCoatMask();
half GetClearCoatSmoothness();

// Returns the clear coat parameters: .r = mask, .g = smoothness.
half2 SampleClearCoat(float2 uv)
{
    half2 clearCoatMaskSmoothness = half2(0.0, 1.0);
    if (UseClearCoat() || UseClearCoatMap())
    {
        clearCoatMaskSmoothness = half2(GetClearCoatMask(), GetClearCoatSmoothness());

        if (UseClearCoatMap())
            clearCoatMaskSmoothness *= SampleClearCoatMap(uv).rg;
    }
    return clearCoatMaskSmoothness;
}

#endif // UNIVERSAL_CLEAR_COAT_INCLUDED
