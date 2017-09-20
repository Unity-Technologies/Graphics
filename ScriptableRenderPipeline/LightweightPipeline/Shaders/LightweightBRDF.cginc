#ifndef LIGHTWEIGHT_BRDF_INCLUDED
#define LIGHTWEIGHT_BRDF_INCLUDED

#define PI 3.14159265359f

half MetallicSetup_Reflectivity()
{
    return 1.0h - OneMinusReflectivityFromMetallic(_Metallic);
}

half3 LightweightBRDFDirect(half3 diffColor, half3 specColor, half smoothness, half RdotL)
{
#ifndef _SPECULARHIGHLIGHTS_OFF
    half RdotLPow4 = Pow4(RdotL);
    half LUT_RANGE = 16.0; // must match range in NHxRoughness() function in GeneratedTextures.cpp
                           // Lookup texture to save instructions
    half perceptualRoughness = 1.0 - smoothness;
    half specular = tex2D(unity_NHxRoughness, half2(RdotLPow4, perceptualRoughness)).UNITY_ATTEN_CHANNEL * LUT_RANGE;
    return diffColor + specular * specColor;
#else
    return diffColor;
#endif
}

// Based on Minimalist CookTorrance BRDF
// Implementation is slightly different from original derivation: http://www.thetenthplanet.de/archives/255
//
// * NDF [Modified] GGX
// * Modified Kelemen and Szirmay-​Kalos for Visibility term
// * Fresnel approximated with 1/LdotH
half3 LightweightBDRF(half3 diffColor, half3 specColor, half oneMinusReflectivity, half perceptualRoughness, half3 normal, half3 lightDirection, half3 viewDir)
{
#ifndef _SPECULARHIGHLIGHTS_OFF
    half3 halfDir = Unity_SafeNormalize(lightDirection + viewDir);

    half NoH = saturate(dot(normal, halfDir));
    half LoH = saturate(dot(lightDirection, halfDir));

    // Specular term
    half roughness = perceptualRoughness * perceptualRoughness;

    // GGX Distribution multiplied by combined approximation of Visibility and Fresnel
    // See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
    // https://community.arm.com/events/1155
    half a2 = roughness * roughness;
    half d = NoH * NoH * (a2 - 1.h) + 1.00001h;

    half LoH2 = LoH * LoH;
    half specularTerm = a2 / ((d * d) * max(0.1h, LoH2) * (roughness + 0.5h) * 4);

    // on mobiles (where half actually means something) denominator have risk of overflow
    // clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
    // sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
#if defined (SHADER_API_MOBILE)
    specularTerm = specularTerm - 1e-4h;
#endif

#if defined (SHADER_API_MOBILE)
    specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
#endif

    return diffColor + specularTerm * specColor;
#else
    return diffColor;
#endif
}

half3 LightweightBRDFIndirect(half3 diffColor, half3 specColor, UnityIndirect indirect, float roughness, half grazingTerm, half fresnelTerm)
{
    half3 c = indirect.diffuse * diffColor;
    float surfaceReduction = 1.0 / (roughness * roughness + 1.0);
    c += surfaceReduction * indirect.specular * lerp(specColor, grazingTerm, fresnelTerm);
    return c;
}

#endif
