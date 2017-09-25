#ifndef LIGHTWEIGHT_PIPELINE_CORE_INCLUDED
#define LIGHTWEIGHT_PIPELINE_CORE_INCLUDED

#include "LightweightInput.cginc"
#include "LightweightLighting.cginc"
#include "LightweightShadows.cginc"

#if defined(_SPECGLOSSMAP) || defined(_SPECULAR_COLOR)
#define LIGHTWEIGHT_SPECULAR_HIGHLIGHTS
#endif

#define kDieletricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)

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

inline void InitializeSurfaceData(LightweightVertexOutput i, out SurfaceData outSurfaceData)
{
    float2 uv = i.uv01.xy;
    half4 albedoAlpha = tex2D(_MainTex, uv);

    outSurfaceData.albedo = LIGHTWEIGHT_GAMMA_TO_LINEAR(albedoAlpha.rgb) * _Color.rgb;
    outSurfaceData.alpha = Alpha(albedoAlpha.a);
    outSurfaceData.metallicSpecGloss = MetallicSpecGloss(uv, albedoAlpha);
    outSurfaceData.normalWorld = Normal(i);
    outSurfaceData.ao = OcclusionLW(uv);
    outSurfaceData.emission = EmissionLW(uv);
}

inline void InitializeBRDFData(SurfaceData surfaceData, out BRDFData outBRDFData)
{
    // BRDF SETUP
#ifdef _METALLIC_SETUP
    half2 metallicGloss = surfaceData.metallicSpecGloss.ra;
    half metallic = metallicGloss.r;
    half smoothness = metallicGloss.g;

    // We'll need oneMinusReflectivity, so
    //   1-reflectivity = 1-lerp(dielectricSpec, 1, metallic) = lerp(1-dielectricSpec, 0, metallic)
    // store (1-dielectricSpec) in kDieletricSpec.a, then
    //   1-reflectivity = lerp(alpha, 0, metallic) = alpha + metallic*(0 - alpha) =
    //                  = alpha - metallic * alpha
    half oneMinusDielectricSpec = kDieletricSpec.a;
    half oneMinusReflectivity = oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
    half reflectivity = 1.0 - oneMinusReflectivity;

    outBRDFData.diffuse = surfaceData.albedo * oneMinusReflectivity;
    outBRDFData.specular = lerp(kDieletricSpec.rgb, surfaceData.albedo, metallic);

#else
    half3 specular = surfaceData.metallicSpecGloss.rgb;
    half smoothness = surfaceData.metallicSpecGloss.a;
    half reflectivity = SpecularReflectivity(specular);

    outBRDFData.diffuse = surfaceData.albedo * (half3(1.0h, 1.0h, 1.0h) - specular);
    outBRDFData.specular = specular;
#endif

    outBRDFData.grazingTerm = saturate(smoothness + reflectivity);
    outBRDFData.perceptualRoughness = 1.0h - smoothness;
    outBRDFData.roughness = outBRDFData.perceptualRoughness * outBRDFData.perceptualRoughness;

#ifdef _ALPHAPREMULTIPLY_ON
    half alpha = surfaceData.alpha;
    outBRDFData.diffuse *= alpha;
    surfaceData.alpha = reflectivity + alpha * (1.0 - reflectivity);
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
