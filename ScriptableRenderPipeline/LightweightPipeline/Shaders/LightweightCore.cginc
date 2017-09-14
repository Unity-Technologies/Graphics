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

#define _DieletricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)

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

half3 MetallicSetup(float2 uv, half3 albedo, half albedoAlpha, out half3 specular, out half smoothness, out half oneMinusReflectivity)
{
    half2 metallicGloss = MetallicSpecGloss(uv, albedoAlpha).ra;
    half metallic = metallicGloss.r;
    smoothness = metallicGloss.g;

    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in unity_ColorSpaceDielectricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = _DieletricSpec.a;
    oneMinusReflectivity = oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
    specular = lerp(_DieletricSpec.rgb, albedo, metallic);

    return albedo * oneMinusReflectivity;
}

half3 SpecularSetup(float2 uv, half3 albedo, half albedoAlpha, out half3 specular, out half smoothness, out half oneMinusReflectivity)
{
    half4 specGloss = MetallicSpecGloss(uv, albedoAlpha);
    specular = specGloss.rgb;
    smoothness = specGloss.a;
    oneMinusReflectivity = 1.0h - SpecularReflectivity(specular);
    return albedo * (half3(1, 1, 1) - specular);
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
