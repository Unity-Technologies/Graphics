#ifndef LIGHTWEIGHT_BRDF_INCLUDED
#define LIGHTWEIGHT_BRDF_INCLUDED

half MetallicSetup_Reflectivity()
{
    return 1.0h - OneMinusReflectivityFromMetallic(_Metallic);
}

//sampler2D unity_NHxRoughness;
half3 LightweightBRDFDirect(half3 diffColor, half3 specColor, half smoothness, half RdotL)
{
#if SPECULAR_HIGHLIGHTS
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

half3 LightweightBRDFIndirect(half3 diffColor, half3 specColor, UnityIndirect indirect, half grazingTerm, half fresnelTerm)
{
    half3 c = indirect.diffuse * diffColor;
    c += indirect.specular * lerp(specColor, grazingTerm, fresnelTerm);
    return c;
}

#endif
