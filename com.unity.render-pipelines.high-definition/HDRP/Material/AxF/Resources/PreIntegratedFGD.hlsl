///////////////////////////////////////////////////////////////////////////////////////////
// BMAYAUX (18/05/28) Ward BRDF used by the AxF shader

TEXTURE2D(_PreIntegratedFGD_WardLambert);

// For image based lighting, a part of the BSDF is pre-integrated.
// This is done both for specular Ward and Lambert
// reflectivity is  Integral{(BSDF_GGX / F) - used for multiscattering
//
void GetPreIntegratedFGDWardLambert( float NdotV, float perceptualRoughness, float3 F0, out float3 specularFGD, out float diffuseFGD, out float reflectivity ) {

    float3 preFGD = SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD_WardLambert, s_linear_clamp_sampler, float2(NdotV, perceptualRoughness), 0).xyz;

    // Pre-integrate Ward FGD
    // Integral{BSDF * <N,L> dw} =
    // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
    // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} + F0 * Integral{(BSDF / F) * <N,L> dw} =
    // (1 - F0) * x + F0 * y = lerp(x, y, F0)
    specularFGD = lerp(preFGD.xxx, preFGD.yyy, F0);

    // Pre integrate Lambert FGD:
    // z = Lambert
    diffuseFGD = preFGD.z;

    reflectivity = preFGD.y;
}

///////////////////////////////////////////////////////////////////////////////////////////
// BMAYAUX (18/05/28) Cook-Torrance BRDF used by the AxF shader

TEXTURE2D(_PreIntegratedFGD_CookTorranceLambert);

// For image based lighting, a part of the BSDF is pre-integrated.
// This is done both for specular Ward and Lambert
// reflectivity is  Integral{(BSDF_GGX / F) - used for multiscattering
//
void GetPreIntegratedFGDCookTorranceLambert( float NdotV, float perceptualRoughness, float3 F0, out float3 specularFGD, out float diffuseFGD, out float reflectivity ) {

    float3 preFGD = SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD_CookTorranceLambert, s_linear_clamp_sampler, float2(NdotV, perceptualRoughness), 0).xyz;

    // Pre-integrate Cook-Torrance FGD
    // Integral{BSDF * <N,L> dw} =
    // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
    // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} + F0 * Integral{(BSDF / F) * <N,L> dw} =
    // (1 - F0) * x + F0 * y = lerp(x, y, F0)
    specularFGD = lerp(preFGD.xxx, preFGD.yyy, F0);

    // Pre integrate Lambert FGD:
    // z = Lambert
    diffuseFGD = preFGD.z;

    reflectivity = preFGD.y;
}
