#ifndef LIGHTWEIGHT_PIPELINE_CORE_INCLUDED
#define LIGHTWEIGHT_PIPELINE_CORE_INCLUDED

#include "LightweightPipelineInput.cginc"

#if defined(_HARD_SHADOWS) || defined(_SOFT_SHADOWS) || defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOWS
#endif

#if defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOW_CASCADES
#endif

#ifdef _SHADOWS
#include "LightweightPipelineShadows.cginc"
#endif

#if defined(_SPECGLOSSMAP_BASE_ALPHA) || defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
#define LIGHTWEIGHT_SPECULAR_HIGHLIGHTS
#endif

//  Does not support: _PARALLAXMAP, DIRLIGHTMAP_COMBINED
#define GLOSSMAP (defined(_SPECGLOSSMAP) || defined(_METALLICGLOSSMAP))

#ifndef SPECULAR_HIGHLIGHTS
#define SPECULAR_HIGHLIGHTS (!defined(_SPECULAR_HIGHLIGHTS_OFF))
#endif

half4 OutputColor(half3 color, half alpha)
{
#ifdef _ALPHABLEND_ON
    return LIGHTWEIGHT_LINEAR_TO_GAMMA(half4(color, alpha));
#else
    return half4(LIGHTWEIGHT_LINEAR_TO_GAMMA(color), 1);
#endif
}

#endif
