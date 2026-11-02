#ifndef UNIVERSAL_METALLIC_SPEC_GLOSS_INCLUDED
#define UNIVERSAL_METALLIC_SPEC_GLOSS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// FEATURES_METALLICSPECGLOSSMAP allows the preprocessor to know if the _METALLICSPECGLOSSMAP might be used.
// Dynamic branch mode: always true if _METALLICSPECGLOSSMAP feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _METALLICSPECGLOSSMAP is active.
#if (_METALLICSPECGLOSSMAP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_METALLICSPECGLOSSMAP
    #define FEATURES_METALLICSPECGLOSSMAP 1
#elif !defined(_METALLICSPECGLOSSMAP_KEYWORD_DECLARED) || (_METALLICSPECGLOSSMAP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_METALLICSPECGLOSSMAP)
        #undef FEATURES_METALLICSPECGLOSSMAP
        #define FEATURES_METALLICSPECGLOSSMAP 1
    #endif
#endif

// FEATURES_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A allows the preprocessor to know if the _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A might be used.
// Dynamic branch mode: always true if _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A is active.
#if (_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
    #define FEATURES_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1
#elif !defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A_KEYWORD_DECLARED) || (_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
        #undef FEATURES_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        #define FEATURES_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A 1
    #endif
#endif

// FEATURES_SPECULAR_SETUP allows the preprocessor to know if the _SPECULAR_SETUP might be used.
// Dynamic branch mode: always true if _SPECULAR_SETUP feature exists; query the runtime state with the Available() helper.
// Specialized variants mode: only true when _SPECULAR_SETUP is active.
#if (_SPECULAR_SETUP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_RUNTIME_BRANCHING)
    #undef FEATURES_SPECULAR_SETUP
    #define FEATURES_SPECULAR_SETUP 1
#elif !defined(_SPECULAR_SETUP_KEYWORD_DECLARED) || (_SPECULAR_SETUP_KEYWORD_DECLARED & KEYWORD_TYPE_FLAG_SPECIALIZED_VARIANTS)
    #if DEFINED_NONZERO(_SPECULAR_SETUP)
        #undef FEATURES_SPECULAR_SETUP
        #define FEATURES_SPECULAR_SETUP 1
    #endif
#endif


// Runtime queries for the Lit-family metallic/specular gloss shader features, safe whether or
// not the keywords are declared in the current pass. These three act as one unit:
// _SPECULAR_SETUP selects the workflow, _METALLICSPECGLOSSMAP binds the workflow's mask
// texture, and _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A redirects that sample's smoothness source.

// The DEFINED_NONZERO arm keeps a leaked `#define _KW 0` fallback from reading as enabled.

// Returns true if the _SPECULAR_SETUP variant is active.
bool IsSpecularSetup()
{
#if defined(_SPECULAR_SETUP_KEYWORD_DECLARED)
    return _SPECULAR_SETUP;
#elif DEFINED_NONZERO(_SPECULAR_SETUP)
    return true;
#else
    return false;
#endif
}

// Returns true if the _METALLICSPECGLOSSMAP variant is active.
bool UseMetallicSpecGlossMap()
{
#if defined(_METALLICSPECGLOSSMAP_KEYWORD_DECLARED)
    return _METALLICSPECGLOSSMAP;
#elif DEFINED_NONZERO(_METALLICSPECGLOSSMAP)
    return true;
#else
    return false;
#endif
}

// Returns true if the _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A variant is active.
bool UseSmoothnessTextureAlbedoChannelA()
{
#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A_KEYWORD_DECLARED)
    return _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A;
#elif DEFINED_NONZERO(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
    return true;
#else
    return false;
#endif
}

// Simple getters the including material's input file must define.
half4 SampleMetallicGlossMap(float2 uv);
half4 SampleSpecGlossMap(float2 uv);
half4 GetSpecColor();
half GetMetallic();
half GetSmoothness();

// Samples the active workflow's reflectivity/smoothness data:
// .rgb = specular color (specular workflow) or metallic in .r (metallic workflow), .a = smoothness.
half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
{
    half smoothness = GetSmoothness();
    half4 specGloss;

    if (UseMetallicSpecGlossMap())
    {
        specGloss = IsSpecularSetup() ? SampleSpecGlossMap(uv) : SampleMetallicGlossMap(uv);
        specGloss.a = UseSmoothnessTextureAlbedoChannelA() ? albedoAlpha * smoothness : specGloss.a * smoothness;
    }
    else
    {
        half metallic = GetMetallic();
        specGloss.rgb = IsSpecularSetup() ? GetSpecColor().rgb : metallic.rrr;
        specGloss.a = UseSmoothnessTextureAlbedoChannelA() ? albedoAlpha * smoothness : smoothness;
    }

    return specGloss;
}

#endif // UNIVERSAL_METALLIC_SPEC_GLOSS_INCLUDED
