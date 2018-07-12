TEXTURE2D(_PreIntegratedFGD_GGXDisneyDiffuse);

// For image based lighting, a part of the BSDF is pre-integrated.
// This is done both for specular GGX height-correlated and DisneyDiffuse
// reflectivity is  Integral{(BSDF_GGX / F) - use for multiscattering
void GetPreIntegratedFGDGGXAndDisneyDiffuse(float NdotV, float perceptualRoughness, float3 fresnel0, out float3 GGXSpecularFGD, out float disneyDiffuseFGD, out float reflectivity)
{
    float3 preFGD = SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD_GGXDisneyDiffuse, s_linear_clamp_sampler, float2(NdotV, perceptualRoughness), 0).xyz;

    // Pre-integrate GGX FGD
    // Integral{BSDF * <N,L> dw} =
    // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
    // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} + F0 * Integral{(BSDF / F) * <N,L> dw}=
    // (1 - F0) * x + F0 * y = lerp(x, y, F0)
    GGXSpecularFGD = lerp(preFGD.xxx, preFGD.yyy, fresnel0);

    // Pre integrate DisneyDiffuse FGD:
    // z = DisneyDiffuse
    // Remap from the [0, 1] to the [0.5, 1.5] range.
    disneyDiffuseFGD = preFGD.z + 0.5;

    reflectivity = preFGD.y;
}

TEXTURE2D(_PreIntegratedFGD_CharlieAndCloth);

void GetPreIntegratedFGDCharlieAndClothLambert(float NdotV, float perceptualRoughness, float3 fresnel0, out float3 CharlieSpecularFGD, out float clothLambertDiffuseFGD, out float reflectivity)
{
    float3 preFGD = SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD_CharlieAndCloth, s_linear_clamp_sampler, float2(NdotV, perceptualRoughness), 0).xyz;

    CharlieSpecularFGD = lerp(preFGD.xxx, preFGD.yyy, fresnel0);
    // z = ClothLambert
    clothLambertDiffuseFGD = preFGD.z;

    reflectivity = preFGD.y;
}
