#ifndef LIGHTWEIGHT_BRDF_INCLUDED
#define LIGHTWEIGHT_BRDF_INCLUDED

#define PI 3.14159265359f

// Based on Minimalist CookTorrance BRDF
// Implementation is slightly different from original derivation: http://www.thetenthplanet.de/archives/255
//
// * NDF [Modified] GGX
// * Modified Kelemen and Szirmay-​Kalos for Visibility term
// * Fresnel approximated with 1/LdotH
half3 LightweightBDRF(BRDFData brdfData, half roughness2, half3 normal, half3 lightDirection, half3 viewDir)
{
#ifndef _SPECULARHIGHLIGHTS_OFF
    half3 halfDir = Unity_SafeNormalize(lightDirection + viewDir);

    half NoH = saturate(dot(normal, halfDir));
    half LoH = saturate(dot(lightDirection, halfDir));

    // GGX Distribution multiplied by combined approximation of Visibility and Fresnel
    // See "Optimizing PBR for Mobile" from Siggraph 2015 moving mobile graphics course
    // https://community.arm.com/events/1155
    half d = NoH * NoH * (roughness2 - 1.h) + 1.00001h;

    half LoH2 = LoH * LoH;
    half specularTerm = roughness2 / ((d * d) * max(0.1h, LoH2) * (brdfData.roughness + 0.5h) * 4);

    // on mobiles (where half actually means something) denominator have risk of overflow
    // clamp below was added specifically to "fix" that, but dx compiler (we convert bytecode to metal/gles)
    // sees that specularTerm have only non-negative terms, so it skips max(0,..) in clamp (leaving only min(100,...))
#if defined (SHADER_API_MOBILE)
    specularTerm = specularTerm - 1e-4h;
#endif

#if defined (SHADER_API_MOBILE)
    specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
#endif

    half3 color = specularTerm * brdfData.specular + brdfData.diffuse;
    return color;
#else
    return diffColor;
#endif
}

half3 LightweightBRDFIndirect(BRDFData brdfData, UnityIndirect indirect, half roughness2, half fresnelTerm)
{
    half3 c = indirect.diffuse * brdfData.diffuse;
    float surfaceReduction = 1.0 / (roughness2 + 1.0);
    c += surfaceReduction * indirect.specular * lerp(brdfData.specular, brdfData.grazingTerm, fresnelTerm);
    return c;
}

#endif
