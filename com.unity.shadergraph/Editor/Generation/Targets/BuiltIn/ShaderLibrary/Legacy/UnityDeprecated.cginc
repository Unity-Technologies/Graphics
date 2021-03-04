//-----------------------------------------------------------------------------
// NOTICE:
// All functions in this file are deprecated and should not be use, they will be remove in a later version.
// They are let here for backward compatibility.
// This file gather several function related to different part of shader code like BRDF or image based lighting
// to avoid to create multiple deprecated file, this file include deprecated function based on a define
// to when including this file, it is expected that the caller define which deprecated function group he want to enable
// Example, following code will include all deprecated BRDF functions:
// #define INCLUDE_UNITY_STANDARD_BRDF_DEPRECATED
// #include "UnityDeprecated.cginc"
// #undef INCLUDE_UNITY_STANDARD_BRDF_DEPRECATED
//-----------------------------------------------------------------------------

#ifdef INCLUDE_UNITY_STANDARD_BRDF_DEPRECATED

inline half3 LazarovFresnelTerm (half3 F0, half roughness, half cosA)
{
    half t = Pow5 (1 - cosA);   // ala Schlick interpoliation
    t /= 4 - 3 * roughness;
    return F0 + (1-F0) * t;
}
inline half3 SebLagardeFresnelTerm (half3 F0, half roughness, half cosA)
{
    half t = Pow5 (1 - cosA);   // ala Schlick interpoliation
    return F0 + (max (F0, roughness) - F0) * t;
}

// Cook-Torrance visibility term, doesn't take roughness into account
inline half CookTorranceVisibilityTerm (half NdotL, half NdotV,  half NdotH, half VdotH)
{
    VdotH += 1e-5f;
    half G = min (1.0, min (
        (2.0 * NdotH * NdotV) / VdotH,
        (2.0 * NdotH * NdotL) / VdotH));
    return G / (NdotL * NdotV + 1e-4f);
}

// Kelemen-Szirmay-Kalos is an approximation to Cook-Torrance visibility term
// http://sirkan.iit.bme.hu/~szirmay/scook.pdf
inline half KelemenVisibilityTerm (half LdotH)
{
    return 1.0 / (LdotH * LdotH);
}

// Modified Kelemen-Szirmay-Kalos which takes roughness into account, based on: http://www.filmicworlds.com/2014/04/21/optimizing-ggx-shaders-with-dotlh/
inline half ModifiedKelemenVisibilityTerm (half LdotH, half perceptualRoughness)
{
    half c = 0.797884560802865h; // c = sqrt(2 / Pi)
    half k = PerceptualRoughnessToRoughness(perceptualRoughness) * c;
    half gH = LdotH * (1-k) + k;
    return 1.0 / (gH * gH);
}

// Smith-Schlick derived for GGX
inline half SmithGGXVisibilityTerm (half NdotL, half NdotV, half perceptualRoughness)
{
    half k = (PerceptualRoughnessToRoughness(perceptualRoughness)) / 2; // derived by B. Karis, http://graphicrants.blogspot.se/2013/08/specular-brdf-reference.html
    return SmithVisibilityTerm (NdotL, NdotV, k);
}

inline half ImplicitVisibilityTerm ()
{
    return 1;
}

// BlinnPhong normalized as reflection density­sity function (RDF)
// ready for use directly as specular: spec=D
// http://www.thetenthplanet.de/archives/255
inline half RDFBlinnPhongNormalizedTerm (half NdotH, half n)
{
    half normTerm = (n + 2.0) / (8.0 * UNITY_PI);
    half specTerm = pow (NdotH, n);
    return specTerm * normTerm;
}

// Decodes HDR textures
// sm 2.0 is no longer supported
inline half3 DecodeHDR_NoLinearSupportInSM2 (half4 data, half4 decodeInstructions)
{
    // If Linear mode is not supported we can skip exponent part
    // In Standard shader SM2.0 and SM3.0 paths are always using different shader variations
    // SM2.0: hardware does not support Linear, we can skip exponent part
#if defined(UNITY_COLORSPACE_GAMMA) && (SHADER_TARGET < 30)
    return (data.a * decodeInstructions.x) * data.rgb;
#else
    return DecodeHDR(data, decodeInstructions);
#endif
}

inline half DotClamped (half3 a, half3 b)
{
    #if (SHADER_TARGET < 30)
        return saturate(dot(a, b));
    #else
        return max(0.0h, dot(a, b));
    #endif
}

inline half LambertTerm (half3 normal, half3 lightDir)
{
    return DotClamped (normal, lightDir);
}

inline half BlinnTerm (half3 normal, half3 halfDir)
{
    return DotClamped (normal, halfDir);
}

half RoughnessToSpecPower (half roughness)
{
    return PerceptualRoughnessToSpecPower (roughness);
}

//-------------------------------------------------------------------------------------
// Legacy, to keep backwards compatibility for (pre Unity 5.3) custom user shaders:
#ifdef UNITY_COLORSPACE_GAMMA
#   define unity_LightGammaCorrectionConsts_PIDiv4 ((UNITY_PI/4)*(UNITY_PI/4))
#   define unity_LightGammaCorrectionConsts_HalfDivPI ((.5h/UNITY_PI)*(.5h/UNITY_PI))
#   define unity_LightGammaCorrectionConsts_8 (8*8)
#   define unity_LightGammaCorrectionConsts_SqrtHalfPI (2/UNITY_PI)
#else
#   define unity_LightGammaCorrectionConsts_PIDiv4 (UNITY_PI/4)
#   define unity_LightGammaCorrectionConsts_HalfDivPI (.5h/UNITY_PI)
#   define unity_LightGammaCorrectionConsts_8 (8)
#   define unity_LightGammaCorrectionConsts_SqrtHalfPI (0.79788)
#endif

#endif // INCLUDE_UNITY_STANDARD_BRDF_DEPRECATED

#ifdef INCLUDE_UNITY_IMAGE_BASED_LIGHTING_DEPRECATED

// Old Unity_GlossyEnvironment signature. Kept only for backward compatibility and will be removed soon
half3 Unity_GlossyEnvironment (UNITY_ARGS_TEXCUBE(tex), half4 hdr, half3 worldNormal, half perceptualRoughness)
{
    Unity_GlossyEnvironmentData g;
    g.roughness /* perceptualRoughness */ = perceptualRoughness;
    g.reflUVW   = worldNormal;

    return Unity_GlossyEnvironment (UNITY_PASS_TEXCUBE(tex), hdr, g);
}

#endif // INCLUDE_UNITY_IMAGE_BASED_LIGHTING_DEPRECATED
