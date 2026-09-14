#ifndef UNIVERSAL_VOLUMETRIC_FOG_BLEND_MODE_INCLUDED
#define UNIVERSAL_VOLUMETRIC_FOG_BLEND_MODE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// Resolves the MixVolumetricFog blend mode for shaders that carry it as compile-time
// defines (ShaderGraph targets) rather than as the _Blend material float. Graphs whose
// blending is material-overridable expose only Multiply as a keyword, so their Premultiply
// and Additive materials fall back to the Alpha formula.

#if !defined(_ALPHAMODULATE_ON_KEYWORD_DECLARED)
    #if !defined(_ALPHAMODULATE_ON)
        #define _ALPHAMODULATE_ON 0
        #define _ALPHAMODULATE_ON_DEFINED_LOCALLY 1
    #elif DEFINED_NONZERO(_ALPHAMODULATE_ON)
        #undef _ALPHAMODULATE_ON
        #define _ALPHAMODULATE_ON 1
    #endif
#endif

half VolumetricFogBlendModeFromDefines()
{
#if defined(_BLENDMODE_ADDITIVE)
    return 2.0;
#else
    if (_ALPHAMODULATE_ON)
        return 3.0;
    #if defined(_BLENDMODE_PREMULTIPLY)
    return 1.0;
    #else
    return 0.0;
    #endif
#endif
}

// Prevents leaking the fallback keyword definition to shaders that include this file.
#if defined(_ALPHAMODULATE_ON_DEFINED_LOCALLY)
    #undef _ALPHAMODULATE_ON_DEFINED_LOCALLY
    #undef _ALPHAMODULATE_ON
#endif

#endif
