#ifndef LIGHTWEIGHT_PIPELINE_CORE_INCLUDED
#define LIGHTWEIGHT_PIPELINE_CORE_INCLUDED

#include "LightweightInput.cginc"
#include "LightweightLighting.cginc"
#include "LightweightBRDF.cginc"

#if defined(_HARD_SHADOWS) || defined(_SOFT_SHADOWS) || defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOWS
#endif

#if defined(_HARD_SHADOWS_CASCADES) || defined(_SOFT_SHADOWS_CASCADES)
#define _SHADOW_CASCADES
#endif

#ifdef _SHADOWS
#include "LightweightShadows.cginc"
#endif

#if defined(_SPECGLOSSMAP_BASE_ALPHA) || defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
#define LIGHTWEIGHT_SPECULAR_HIGHLIGHTS
#endif

half SpecularReflectivity(half3 specular)
{
#if (SHADER_TARGET < 30)
    // SM2.0: instruction count limitation
    // SM2.0: simplified SpecularStrength
    return specular.r; // Red channel - because most metals are either monocrhome or with redish/yellowish tint
#else
    return max(max(specular.r, specular.g), specular.b);
#endif
}

half4 OutputColor(half3 color, half alpha)
{
#if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
    return LIGHTWEIGHT_LINEAR_TO_GAMMA(half4(color, alpha));
#else
    return half4(LIGHTWEIGHT_LINEAR_TO_GAMMA(color), 1);
#endif
}


#endif
