//-----------------------------------------------------------------------------
// Includes
//-----------------------------------------------------------------------------

// SurfaceData is define in Lit.cs which generate Lit.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/Lit.cs.hlsl"
// Those define allow to include desired SSS/Transmission functions
#define MATERIAL_INCLUDE_SUBSURFACESCATTERING
#define MATERIAL_INCLUDE_TRANSMISSION
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinGIUtilities.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SubsurfaceScattering/SubsurfaceScattering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Decal/DecalPrepassBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------

// Choose between Lambert diffuse and Disney diffuse (enable only one of them)
// #define USE_DIFFUSE_LAMBERT_BRDF

#define LIT_USE_GGX_ENERGY_COMPENSATION

// Enable reference mode for IBL and area lights
// Both reference define below can be define only if LightLoop is present, else we get a compile error
#ifdef HAS_LIGHTLOOP
// #define LIT_DISPLAY_REFERENCE_AREA
// #define LIT_DISPLAY_REFERENCE_IBL
#endif

//-----------------------------------------------------------------------------
// Texture and constant buffer declaration
//-----------------------------------------------------------------------------

// GBuffer texture declaration
TEXTURE2D_X(_GBufferTexture0);
TEXTURE2D_X(_GBufferTexture1);
TEXTURE2D_X(_GBufferTexture2);
TEXTURE2D_X(_GBufferTexture3); // Bake lighting and/or emissive
TEXTURE2D_X(_GBufferTexture4); // VTFeedback or Rendering layer or shadow mask
TEXTURE2D_X(_GBufferTexture5); // Rendering layer or shadow mask
TEXTURE2D_X(_GBufferTexture6); // shadow mask


TEXTURE2D_X(_RenderingLayersTexture);
#ifdef SHADOWS_SHADOWMASK
TEXTURE2D_X(_ShadowMaskTexture); // Alias for shadow mask, so we don't need to know which gbuffer is used for shadow mask
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"

//-----------------------------------------------------------------------------
// Definition
//-----------------------------------------------------------------------------

#ifdef UNITY_VIRTUAL_TEXTURING
    #define OUT_GBUFFER_VTFEEDBACK outGBuffer4
    #define OUT_GBUFFER_OPTIONAL_SLOT_1 outGBuffer5
    #define OUT_GBUFFER_OPTIONAL_SLOT_2 outGBuffer6
    #if (SHADERPASS == SHADERPASS_GBUFFER)
        #if defined(SHADER_API_PSSL)
            //For exact packing on pssl, we want to write exact 16 bit unorm (respect exact bit packing).
            //In some sony platforms, the default is FMT_16_ABGR, which would incur in loss of precision.
            //Thus, when VT is enabled, we force FMT_32_ABGR
            #pragma PSSL_target_output_format(target 4 FMT_32_ABGR)
        #endif
    #endif
#else
    #define OUT_GBUFFER_OPTIONAL_SLOT_1 outGBuffer4
    #define OUT_GBUFFER_OPTIONAL_SLOT_2 outGBuffer5
#endif

#if defined(RENDERING_LAYERS) && defined(SHADOWS_SHADOWMASK)
#define OUT_GBUFFER_RENDERING_LAYERS OUT_GBUFFER_OPTIONAL_SLOT_1
#define OUT_GBUFFER_SHADOWMASK OUT_GBUFFER_OPTIONAL_SLOT_2
#elif defined(RENDERING_LAYERS)
#define OUT_GBUFFER_RENDERING_LAYERS OUT_GBUFFER_OPTIONAL_SLOT_1
#elif defined(SHADOWS_SHADOWMASK)
#define OUT_GBUFFER_SHADOWMASK OUT_GBUFFER_OPTIONAL_SLOT_1
#endif

#define HAS_REFRACTION (defined(_REFRACTION_PLANE) || defined(_REFRACTION_SPHERE) || defined(_REFRACTION_THIN))

// It is safe to include this file after the G-Buffer macros above.
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialGBufferMacros.hlsl"

//-----------------------------------------------------------------------------
// Light and material classification for the deferred rendering path
// Configure what kind of combination is supported
//-----------------------------------------------------------------------------

// Lighting architecture and material are suppose to be decoupled files.
// However as we use material classification it is hard to be fully separated
// the dependecy is define in this include where there is shared define for material and lighting in case of deferred material.
// If a user do a lighting architecture without material classification, this can be remove
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.cs.hlsl"

#define MATERIALFEATUREFLAGS_ANY_SSS (MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION | MATERIALFEATUREFLAGS_LIT_COLORED_TRANSMISSION)

// Combination need to be define in increasing "comlexity" order as define by FeatureFlagsToTileVariant
static const uint kFeatureVariantFlags[NUM_FEATURE_VARIANTS] =
{
    // Number in parenthesis is occupancy on PS4
    // Precomputed illumination (no dynamic lights) with standard
    /*  0 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SSREFLECTION | MATERIALFEATUREFLAGS_LIT_STANDARD,
    // Precomputed illumination (no dynamic lights) with standard, SSS and transmission
    /*  1 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SSREFLECTION | MATERIALFEATUREFLAGS_ANY_SSS | MATERIALFEATUREFLAGS_LIT_STANDARD,
    // Precomputed illumination (no dynamic lights) for all material types
    /*  2 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SSREFLECTION | MATERIAL_FEATURE_MASK_FLAGS,

    /*  3 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  4 (2) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  5 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SSREFLECTION | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  6 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SSREFLECTION | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  7 (2) */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIALFEATUREFLAGS_LIT_STANDARD,

    // Standard with SSS and Transmission
    /*  8 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_ANY_SSS | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  9 (2) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_ANY_SSS | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 10 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SSREFLECTION | MATERIALFEATUREFLAGS_ANY_SSS | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 11 (2) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SSREFLECTION | MATERIALFEATUREFLAGS_ANY_SSS | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 12 (2) */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIALFEATUREFLAGS_ANY_SSS | MATERIALFEATUREFLAGS_LIT_STANDARD,

    // Anisotropy
    /* 13 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_ANISOTROPY | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 14 (2) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_ANISOTROPY | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 15 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SSREFLECTION | MATERIALFEATUREFLAGS_LIT_ANISOTROPY | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 16 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SSREFLECTION | MATERIALFEATUREFLAGS_LIT_ANISOTROPY | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 17 (2) */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIALFEATUREFLAGS_LIT_ANISOTROPY | MATERIALFEATUREFLAGS_LIT_STANDARD,

    // Standard with clear coat
    /* 18 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 19 (2) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 20 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SSREFLECTION | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 21 (2) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SSREFLECTION | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 22 (2) */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,

    // Standard with Iridescence
    /* 23 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 24 (2) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 25 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SSREFLECTION | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 26 (3) */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | LIGHTFEATUREFLAGS_SSREFLECTION | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 27 (2) */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_STANDARD,

    /* 28 (2) */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIAL_FEATURE_MASK_FLAGS, // Catch all case with MATERIAL_FEATURE_MASK_FLAGS is needed in case we disable material classification
};

uint FeatureFlagsToTileVariant(uint featureFlags)
{
    for (int i = 0; i < NUM_FEATURE_VARIANTS; i++)
    {
        if ((featureFlags & kFeatureVariantFlags[i]) == featureFlags)
            return i;
    }
    return NUM_FEATURE_VARIANTS - 1;
}

#ifdef USE_INDIRECT

uint TileVariantToFeatureFlags(uint variant, uint tileIndex)
{
    if (variant == NUM_FEATURE_VARIANTS - 1)
    {
        // We don't have any compile-time feature information.
        // Therefore, we load the feature classification data at runtime to avoid
        // entering every single branch based on feature flags.
        return g_TileFeatureFlags[tileIndex];
    }
    else
    {
        // Return the compile-time feature flags.
        return kFeatureVariantFlags[variant];
    }
}

#endif // USE_INDIRECT

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

// This function return diffuse color or an equivalent color (in case of metal). Alpha channel is 0 is dieletric or 1 if metal, or in between value if it is in between
// This is use for MatCapView and reflection probe pass
// replace is 0.0 if we want diffuse color or 1.0 if we want default color
float4 GetDiffuseOrDefaultColor(BSDFData bsdfData, float replace)
{
    // Use frensel0 as mettalic weight. all value below 0.2 (ior of diamond) are dielectric
    // all value above 0.45 are metal, in between we lerp.
    float weight = saturate((Max3(bsdfData.fresnel0.r, bsdfData.fresnel0.g, bsdfData.fresnel0.b) - 0.2) / (0.45 - 0.2));

    return float4(lerp(bsdfData.diffuseColor, bsdfData.fresnel0, weight * replace), weight);
}

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    // In forward we can used geometric normal for shadow bias which improve quality
#if (SHADERPASS == SHADERPASS_FORWARD)
    return bsdfData.geomNormalWS;
#else
    return bsdfData.normalWS;
#endif
}

float GetAmbientOcclusionForMicroShadowing(BSDFData bsdfData)
{
    float sourceAO;
#if (SHADERPASS == SHADERPASS_DEFERRED_LIGHTING)
    // Note: In deferred pass we don't have space in GBuffer to store ambientOcclusion so we use specularOcclusion instead
    sourceAO = bsdfData.specularOcclusion;
#else
    sourceAO = bsdfData.ambientOcclusion;
#endif
    return sourceAO;
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Reflection/VolumeProjection.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceTracing.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceLighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Refraction.hlsl"

#if HAS_REFRACTION
    // Note that this option is referred as "Box" in the UI, we are keeping _REFRACTION_PLANE as shader define to avoid complication with already created materials.
    #if defined(_REFRACTION_PLANE)
    #define REFRACTION_MODEL(V, posInputs, bsdfData) RefractionModelBox(V, posInputs.positionWS, bsdfData.normalWS, bsdfData.ior, bsdfData.thickness)
    #elif defined(_REFRACTION_SPHERE)
    #define REFRACTION_MODEL(V, posInputs, bsdfData) RefractionModelSphere(V, posInputs.positionWS, bsdfData.normalWS, bsdfData.ior, bsdfData.thickness)
    #elif defined(_REFRACTION_THIN)
    #define REFRACTION_THIN_DISTANCE 0.005
    #define REFRACTION_MODEL(V, posInputs, bsdfData) RefractionModelBox(V, posInputs.positionWS, bsdfData.normalWS, bsdfData.ior, bsdfData.thickness)
    #endif
#endif

// Assume bsdfData.normalWS is init
void FillMaterialAnisotropy(float anisotropy, float3 tangentWS, float3 bitangentWS, inout BSDFData bsdfData)
{
    bsdfData.anisotropy  = anisotropy;
    bsdfData.tangentWS   = tangentWS;
    bsdfData.bitangentWS = bitangentWS;
}

void FillMaterialIridescence(float mask, float thickness, inout BSDFData bsdfData)
{
    bsdfData.iridescenceMask = mask;
    bsdfData.iridescenceThickness = thickness;
}

// Note: this modify the parameter perceptualRoughness and fresnel0, so they need to be setup
void FillMaterialClearCoatData(float coatMask, inout BSDFData bsdfData)
{
    bsdfData.coatMask = coatMask;
    float ieta = lerp(1.0, CLEAR_COAT_IETA, bsdfData.coatMask);
    bsdfData.coatRoughness = CLEAR_COAT_ROUGHNESS;

    // Approx to deal with roughness appearance of base layer (should appear rougher)
    float coatRoughnessScale = Sq(ieta);
    float sigma = RoughnessToVariance(PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness));
    bsdfData.perceptualRoughness = RoughnessToPerceptualRoughness(VarianceToRoughness(sigma * coatRoughnessScale));
}

void FillMaterialTransparencyData(float3 baseColor, float metallic, float ior, float3 transmittanceColor, float atDistance, float thickness, float transmittanceMask, inout BSDFData bsdfData)
{
    // Uses thickness from SSS's property set
    bsdfData.ior = ior;

    // IOR define the fresnel0 value, so update it also for consistency (and even if not physical we still need to take into account any metal mask)
    bsdfData.fresnel0 = lerp(IorToFresnel0(ior).xxx, baseColor, metallic);

    bsdfData.absorptionCoefficient = TransmittanceColorAtDistanceToAbsorption(transmittanceColor, atDistance);
    bsdfData.transmittanceMask = transmittanceMask;
    bsdfData.thickness = max(thickness, 0.0001);
}

// Needs to be called after FillMaterialSSS
void FillMaterialAdvancedSSS(inout BSDFData bsdfData)
{
    // Setup for diffuse power and dual lobe
    // These features don't have a special deferred variant because they are cheap enough
    // but we still have feature flags to dynamically skip the code for materials that don't need it
    // Note that we don't store any paramteter in the bsdfData, they are loaded from the diffusion
    // profile everytime they are needed to save VGPRs

    if (GetDiffusePower(bsdfData.diffusionProfileIndex) != 0.0f)
        bsdfData.materialFeatures |= MATERIALFEATUREFLAGS_SSS_DIFFUSE_POWER;

    float lobeA, lobeB, lobeMix;
    if (GetDualLobeParameters(bsdfData.diffusionProfileIndex, lobeA, lobeB, lobeMix))
        bsdfData.materialFeatures |= MATERIALFEATUREFLAGS_SSS_DUAL_LOBE;
}

void FillMaterialColoredTranslucent(float3 transmissionMask, inout BSDFData bsdfData)
{
    bsdfData.diffusionProfileIndex = 0;
    bsdfData.fresnel0              = IorToFresnel0(1.4f);
    bsdfData.thickness             = 0.0f;
    bsdfData.transmittance         = transmissionMask;
}

// This function is use to help with debugging and must be implemented by any lit material
// Implementer must take into account what are the current override component and
// adjust SurfaceData properties accordingdly
void ApplyDebugToSurfaceData(float3x3 tangentToWorld, inout SurfaceData surfaceData)
{
#ifdef DEBUG_DISPLAY
    // Override value if requested by user
    // this can be use also in case of debug lighting mode like diffuse only
    bool overrideAlbedo = _DebugLightingAlbedo.x != 0.0;
    bool overrideSmoothness = _DebugLightingSmoothness.x != 0.0;
    bool overrideNormal = _DebugLightingNormal.x != 0.0;
    bool overrideAO = _DebugLightingAmbientOcclusion.x != 0.0;

    if (overrideAlbedo)
    {
        float3 overrideAlbedoValue = _DebugLightingAlbedo.yzw;
        surfaceData.baseColor = overrideAlbedoValue;
    }

    if (overrideSmoothness)
    {
        float overrideSmoothnessValue = _DebugLightingSmoothness.y;
        surfaceData.perceptualSmoothness = overrideSmoothnessValue;
    }

    if (overrideNormal)
    {
        surfaceData.normalWS = tangentToWorld[2];
    }

    if (overrideAO)
    {
        float overrideAOValue = _DebugLightingAmbientOcclusion.y;
        surfaceData.ambientOcclusion = overrideAOValue;
    }

    // There is no metallic with SSS and specular color mode
    float metallic = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR | MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION) ? 0.0 : surfaceData.metallic;

    float3 diffuseColor = ComputeDiffuseColor(surfaceData.baseColor, metallic);
    bool specularWorkflow = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR);
    float3 specularColor =  specularWorkflow ? surfaceData.specularColor : ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, DEFAULT_SPECULAR_VALUE);

    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_DIFFUSE_COLOR)
    {
        surfaceData.baseColor = pbrDiffuseColorValidate(diffuseColor, specularColor, metallic > 0.0, !specularWorkflow).xyz;
    }
    else if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_SPECULAR_COLOR)
    {
        surfaceData.baseColor = pbrSpecularColorValidate(diffuseColor, specularColor, metallic > 0.0, !specularWorkflow).xyz;
    }

#endif
}

// This function is similar to ApplyDebugToSurfaceData but for BSDFData
void ApplyDebugToBSDFData(inout BSDFData bsdfData)
{
#ifdef DEBUG_DISPLAY
    // Override value if requested by user
    // this can be use also in case of debug lighting mode like specular only
    bool overrideSpecularColor = _DebugLightingSpecularColor.x != 0.0;

    if (overrideSpecularColor)
    {
        float3 overrideSpecularColor = _DebugLightingSpecularColor.yzw;
        bsdfData.fresnel0 = overrideSpecularColor;
    }
#endif
}

SSSData ConvertSurfaceDataToSSSData(SurfaceData surfaceData)
{
    SSSData sssData;

    sssData.diffuseColor = surfaceData.baseColor;
    sssData.subsurfaceMask = surfaceData.subsurfaceMask;
    sssData.diffusionProfileIndex = FindDiffusionProfileIndex(surfaceData.diffusionProfileHash);

    return sssData;
}

NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    NormalData normalData;

    // Note: When we are in the prepass (depth only or motion vector) and we need to export the normal/roughness - Mean we are lit forward. In deferred the normal buffer will not be exported
    // If the fragment that we are processing has clear cloat, we want to export the clear coat's perceptual roughness and geometric normal
    // instead of the base layer's roughness and the shader normal to be use by SSR
    #if (SHADERPASS == SHADERPASS_DEPTH_ONLY) || (SHADERPASS == SHADERPASS_MOTION_VECTORS) || (SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS)
    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        normalData.normalWS = surfaceData.geomNormalWS;
        normalData.perceptualRoughness = CLEAR_COAT_SSR_PERCEPTUAL_ROUGHNESS;
    }
    else
    #endif
    {
        // Note: We can't handle clear coat material here, we have only one slot to store smoothness
        // and the buffer is the GBuffer1.
        normalData.normalWS = surfaceData.normalWS;
        normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    }

    return normalData;
}

void UpdateSurfaceDataFromNormalData(uint2 positionSS, inout BSDFData bsdfData)
{
    NormalData normalData;

    DecodeFromNormalBuffer(positionSS, normalData);

    bsdfData.normalWS = normalData.normalWS;
    bsdfData.perceptualRoughness = normalData.perceptualRoughness;
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    // IMPORTANT: In case of foward or gbuffer pass all enable flags are statically know at compile time, so the compiler can do compile time optimization
    bsdfData.materialFeatures    = surfaceData.materialFeatures;

    // Standard material
    bsdfData.ambientOcclusion    = surfaceData.ambientOcclusion;
    bsdfData.specularOcclusion   = surfaceData.specularOcclusion;
    bsdfData.normalWS            = surfaceData.normalWS;
    bsdfData.geomNormalWS        = surfaceData.geomNormalWS;
    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);

    // There is no metallic with SSS and specular color mode
    float metallic = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR | MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION) ? 0.0 : surfaceData.metallic;

    bsdfData.diffuseColor = ComputeDiffuseColor(surfaceData.baseColor, metallic);
    bsdfData.fresnel0     = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR) ? surfaceData.specularColor :
        ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, DEFAULT_SPECULAR_VALUE);
    bsdfData.fresnel90    = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION) ? 1.0f :
        ComputeF90(bsdfData.fresnel0);

    // Note: we have ZERO_INITIALIZE the struct so bsdfData.anisotropy == 0.0
    // Note: DIFFUSION_PROFILE_NEUTRAL_ID is 0

    // In forward everything is statically know and we could theorically cumulate all the material features. So the code reflect it.
    // However in practice we keep parity between deferred and forward, so we should constrain the various features.
    // The UI is in charge of setuping the constrain, not the code. So if users is forward only and want unleash power, it is easy to unleash by some UI change

    bsdfData.diffusionProfileIndex = FindDiffusionProfileIndex(surfaceData.diffusionProfileHash);

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING))
    {
        // Assign profile id and overwrite fresnel0
        FillMaterialSSS(bsdfData.diffusionProfileIndex, surfaceData.subsurfaceMask, bsdfData);
        FillMaterialAdvancedSSS(bsdfData);
    }

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        // Assign profile id and overwrite fresnel0
        if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_COLORED_TRANSMISSION))
            FillMaterialColoredTranslucent(surfaceData.transmissionMask, bsdfData);
        else
            FillMaterialTransmission(bsdfData.diffusionProfileIndex, surfaceData.thickness, surfaceData.transmissionMask, bsdfData);
    }

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
    {
        FillMaterialAnisotropy(surfaceData.anisotropy, surfaceData.tangentWS, cross(surfaceData.normalWS, surfaceData.tangentWS), bsdfData);
    }

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_IRIDESCENCE))
    {
        FillMaterialIridescence(surfaceData.iridescenceMask, surfaceData.iridescenceThickness, bsdfData);
    }

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        // Modify perceptualRoughness
        FillMaterialClearCoatData(surfaceData.coatMask, bsdfData);
    }

    // roughnessT and roughnessB are clamped, and are meant to be used with punctual and directional lights.
    // perceptualRoughness is not clamped, and is meant to be used for IBL.
    // perceptualRoughness can be modify by FillMaterialClearCoatData, so ConvertAnisotropyToClampRoughness must be call after
    ConvertAnisotropyToRoughness(bsdfData.perceptualRoughness, bsdfData.anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);

#if HAS_REFRACTION
    // Note: Reuse thickness of transmission's property set
    FillMaterialTransparencyData(surfaceData.baseColor, surfaceData.metallic, surfaceData.ior, surfaceData.transmittanceColor,
    #ifdef _REFRACTION_THIN
                                 // We set both atDistance and thickness to the same, small value
                                 REFRACTION_THIN_DISTANCE, REFRACTION_THIN_DISTANCE,
    #else
                                 surfaceData.atDistance, surfaceData.thickness,
    #endif
                                 surfaceData.transmittanceMask, bsdfData);
#endif

    ApplyDebugToBSDFData(bsdfData);

    return bsdfData;
}

//-----------------------------------------------------------------------------
// conversion function for deferred
//-----------------------------------------------------------------------------

// GBuffer layout.
// GBuffer2 and GBuffer0.a interpretation depends on material feature enabled

//GBuffer0      RGBA8 sRGB  Gbuffer0 encode baseColor and so is sRGB to save precision. Alpha is not affected.
//GBuffer1      RGBA8
//GBuffer2      RGBA8
//GBuffer3      RGBA8


//FeatureName   Standard
//GBuffer0      baseColor.r,    baseColor.g,    baseColor.b,    specularOcclusion(7) / IsLightmap(1)
//GBuffer1      normal.xy (1212),   perceptualRoughness
//GBuffer2      f0.r,   f0.g,   f0.b,   featureID(3) / coatMask(5)
//GBuffer3      bakedDiffuseLighting.rgb

//FeatureName   Subsurface Scattering + Transmission + Colored Transmission
//GBuffer0      baseColor.r,    baseColor.g,    baseColor.b,   diffusionProfile(4) / subsurfaceMask(4)
//GBuffer1      normal.xy (1212),   perceptualRoughness
//GBuffer2 SSS  specularOcclusion(7) / IsLightmap(1),  thickness,  diffusionProfile(4) / transmissionMask(4),  coatMask(5) / featureID(3)
//GBuffer2 Tra  specularOcclusion(7) / IsLightmap(1),  thickness,  diffusionProfile(4) / transmissionMask(4),  coatMask(4) / colored(1) / featureID(3)
//GBuffer2 ColT specularOcclusion(7) / IsLightmap(1),  transmissionTint.rgb (20) / colored(1) / featureID(3)
//GBuffer3      bakedDiffuseLighting.rgb

//FeatureName   Anisotropic
//GBuffer0      baseColor.r,    baseColor.g,    baseColor.b,    specularOcclusion(7) / IsLightmap(1)
//GBuffer1      normal.xy (1212),   perceptualRoughness
//GBuffer2      anisotropy, tangent.x,  tangent.sign(1) / metallic(5), featureID(3) / coatMask(5)
//GBuffer3      bakedDiffuseLighting.rgb

//FeatureName   Irridescence
//GBuffer0      baseColor.r,    baseColor.g,    baseColor.b,    specularOcclusion(7) / IsLightmap(1)
//GBuffer1      normal.xy (1212),   perceptualRoughness
//GBuffer2      IOR,    thickness,  unused(3bit) / metallic(5), featureID(3) / coatMask(5)
//GBuffer3      bakedDiffuseLighting.rgb

// Note:
// For standard we have chose to always encode fresnel0. Even when we use metal/baseColor parametrization. This avoid
// compiler optimization problem that was using VGPR to deal with the various combination of metal non metal.

// For SSS, we move diffusionProfile(4) / subsurfaceMask(4) in GBuffer0.a so the forward SSS code only need to write into one RT
// and the SSS postprocess only need to read one RT
// We duplicate diffusionProfile in GBuffer2.b so the compiler doesn't need to read the GBuffer0 before PostEvaluateBSDF
// The lighting code have been adapted to only apply diffuseColor at the end.
// This save VGPR as we don't need to keep the GBuffer0 value in register.

// The layout is also design to only require one RT for the material classification. All the material feature flags are deduced from GBuffer2.

// Encode SurfaceData (BSDF parameters) into GBuffer
// Must be in sync with RT declared in HDRenderPipeline.cs ::Rebuild
void EncodeIntoGBuffer( SurfaceData surfaceData
                        , BuiltinData builtinData
                        , uint2 positionSS
                        , out GBufferType0 outGBuffer0
                        , out GBufferType1 outGBuffer1
                        , out GBufferType2 outGBuffer2
                        , out GBufferType3 outGBuffer3
#if GBUFFERMATERIAL_COUNT > 4
                        , out GBufferType4 outGBuffer4
#endif
#if GBUFFERMATERIAL_COUNT > 5
                        , out GBufferType5 outGBuffer5
#endif
#if GBUFFERMATERIAL_COUNT > 6
                        , out GBufferType5 outGBuffer6
#endif
                        )
{
    // When using APV we need to know if we have lightmaps or not.
    // we chose to encode this information into the specularOcclusion
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    float encodedSpecularOcclusion = PackFloatInt8bit(surfaceData.specularOcclusion, builtinData.isLightmap, 2);
#else
    float encodedSpecularOcclusion = surfaceData.specularOcclusion;
#endif

    // Ensure that surfaceData.coatMask is 0 if the feature is not enabled
    // Warning: overriden by Translucent if using a transmission tint
    float coatMask = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT) ? surfaceData.coatMask : 0.0;
    uint encodedCoatMask = PackFloatToUInt(surfaceData.coatMask, 3, 5);

    // RT0 - 8:8:8:8 sRGB
    // Warning: the contents are later overwritten for Standard and SSS!
    outGBuffer0 = float4(surfaceData.baseColor, encodedSpecularOcclusion);

    // This encode normalWS and PerceptualSmoothness into GBuffer1
    EncodeIntoNormalBuffer(ConvertSurfaceDataToNormalData(surfaceData), outGBuffer1);

    // RT2 - 8:8:8:8
    uint materialFeatureId;

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        // Reminder that during GBuffer pass we know statically material materialFeatures
        if ((surfaceData.materialFeatures & (MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION)) == (MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
            materialFeatureId = GBUFFER_LIT_TRANSMISSION_SSS;
        else if ((surfaceData.materialFeatures & MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING) == MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING)
            materialFeatureId = GBUFFER_LIT_SSS;
        else
            materialFeatureId = GBUFFER_LIT_TRANSMISSION;

        // We perform the same encoding for SSS and transmission even if not used as it is the same cost
        // Note that regarding EncodeIntoSSSBuffer, as the lit.shader IS the deferred shader (and the SSS fullscreen pass is based on deferred encoding),
        // it know the details of the encoding, so it is fine to assume here how SSSBuffer0 is encoded

        // For the SSS feature, the alpha channel is overwritten with (diffusionProfile | subsurfaceMask).
        // It is done so that the SSS pass only has to read a single G-Buffer 0.
        // We move specular occlusion to the red channel of the G-Buffer 2.
        SSSData sssData = ConvertSurfaceDataToSSSData(surfaceData);
        EncodeIntoSSSBuffer(sssData, positionSS, outGBuffer0);

        if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_COLORED_TRANSMISSION))
        {
            uint rgb20 = PackToR7G7B6(surfaceData.transmissionMask);
            outGBuffer2.rgb = float3(encodedSpecularOcclusion, PackByte((rgb20 >> 12) & 0xFF), PackByte((rgb20 >> 4) & 0xFF));
            encodedCoatMask = ((rgb20 & 0xF) << 4) | (1 << 3);
        }
        else
        {
            // We duplicate storage of diffusion profile in G-Buffer 2.
            // It allows us to delay reading the G-Buffer 0 until the end of the deferred lighting shader.
            float transmissionMaskProfile = PackFloatInt8bit(surfaceData.transmissionMask.x, sssData.diffusionProfileIndex, 16);
            outGBuffer2.rgb = float3(encodedSpecularOcclusion, surfaceData.thickness, transmissionMaskProfile);

            if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
                encodedCoatMask = PackFloatToUInt(coatMask, 4, 4) | (0 << 3);
        }
    }
    else if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
    {
        materialFeatureId = GBUFFER_LIT_ANISOTROPIC;

        // Reconstruct the default tangent frame.
        float3x3 frame = GetLocalFrame(surfaceData.normalWS);

        // Compute the rotation angle of the actual tangent frame with respect to the default one.
        float sinFrame = dot(surfaceData.tangentWS, frame[1]);
        float cosFrame = dot(surfaceData.tangentWS, frame[0]);

        // Define AnisoGGX(α, β, γ), where:
        // α is the roughness corresponding to the direction of the tangent;
        // β is the roughness corresponding to the direction of the bi-tangent;
        // γ is the angle of rotation of the tangent frame around the normal.
        //
        // The following symmetry relations exist:
        // 1st quadrant (Sin >= 0, Cos >  0): AnisoGGX(α, β, γ), where (0 <= γ < Pi/2)
        // 2nd quadrant (Sin >  0, Cos <= 0): AnisoGGX(α, β, γ) == AnisoGGX(β, α, γ + Pi * 1/2)
        // 3rd quadrant (Sin <= 0, Cos <  0): AnisoGGX(α, β, γ) == AnisoGGX(α, β, γ + Pi)
        // 4th quadrant (Sin <  0, Cos >= 0): AnisoGGX(α, β, γ) == AnisoGGX(β, α, γ + Pi * 3/2)
        // Handling of the interval end-points may be less rigorous to simplify programming.
        // The only requirement is that the handling is consistent throughout.
        bool quad2or4 = (sinFrame * cosFrame) < 0;

        // Anisotropy = (α - β) / (α + β).
        // Exchanging the roughness values α and β is equivalent to negating the value of anisotropy.
    #if 0
        // To avoid shading seams at the locations where anisotropy changes its sign,
        // its magnitude must be the same (on both sides) after reconstruction from the G-buffer.
        // This means that the hardware unit must perform rounding accurately (and consistently)
        // before storing the value in the G-buffer.
        float sfltAniso  = quad2or4 ? -surfaceData.anisotropy : surfaceData.anisotropy;
        float anisotropy = sfltAniso * 0.5 + 0.5;
    #else
        // It turns out, certain hardware has poor rounding behavior:
        // https://microsoft.github.io/DirectX-Specs/d3d/archive/D3D11_3_FunctionalSpec.htm#3.2.3.6%20FLOAT%20-%3E%20UNORM
        // Therefore, we must round manually to avoid the seams.
        float uintAniso  = round(surfaceData.anisotropy * 127.5 + 127.5);
              uintAniso  = quad2or4 ? 255 - uintAniso : uintAniso;
        // We cannot represent the anisotropy value of 0 exactly, but it is of little
        // importance since you can just use the isotropic material for that purpose.
        float anisotropy = uintAniso * rcp(255);
    #endif

        // We need to convert the values of Sin and Cos to those appropriate for the 1st quadrant.
        // To go from Q3 to Q1, we must rotate by Pi, so taking the absolute value suffices.
        // To go from Q2 or Q4 to Q1, we must rotate by ((N + 1/2) * Pi), so we must
        // take the absolute value and also swap Sin and Cos.
        bool  storeSin = (abs(sinFrame) < abs(cosFrame)) != quad2or4;
        // sin [and cos] are approximately linear up to [after] Pi/4 ± Pi.
        float sinOrCos = min(abs(sinFrame), abs(cosFrame));
        // To avoid storing redundant angles, we must convert from a node-centered representation
        // to a cell-centered one, e.i. remap: [0.5/256, 255.5/256] -> [0, 1].
        float remappedSinOrCos = Remap01(sinOrCos, sqrt(2) * 256.0/255.0, 0.5/255.0);

        outGBuffer2.rgb = float3(anisotropy,
                                 remappedSinOrCos,
                                 PackFloatInt8bit(surfaceData.metallic, storeSin ? 1 : 0, 8));
    }
    else if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_IRIDESCENCE))
    {
        materialFeatureId = GBUFFER_LIT_IRIDESCENCE;

        outGBuffer2.rgb = float3(surfaceData.iridescenceMask, surfaceData.iridescenceThickness,
                                 PackFloatInt8bit(surfaceData.metallic, 0, 8));
    }
    else // Standard
    {
        // In the case of standard or specular color we always convert to specular color parametrization before encoding,
        // so decoding is more efficient (it allow better optimization for the compiler and save VGPR)
        // This mean that on the decode side, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR doesn't exist anymore
        materialFeatureId = GBUFFER_LIT_STANDARD;

        float3 diffuseColor = surfaceData.baseColor;
        float3 fresnel0     = surfaceData.specularColor;

        if (!HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR))
        {
            // Convert from the metallic parametrization.
            diffuseColor = ComputeDiffuseColor(surfaceData.baseColor, surfaceData.metallic);
            fresnel0     = ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, DEFAULT_SPECULAR_VALUE);
        }

        outGBuffer0.rgb = diffuseColor;               // sRGB RT
        // outGBuffer2 is not sRGB, so use a fast encode/decode sRGB to keep precision
        outGBuffer2.rgb = FastLinearToSRGB(fresnel0); // TODO: optimize
    }

    // Note: no need to store MATERIALFEATUREFLAGS_LIT_STANDARD, always present
    outGBuffer2.a = PackByte(encodedCoatMask | materialFeatureId);

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode >= DEBUGLIGHTINGMODE_DIFFUSE_LIGHTING && _DebugLightingMode <= DEBUGLIGHTINGMODE_EMISSIVE_LIGHTING)
    {
        // With deferred, Emissive is store in builtinData.bakeDiffuseLighting. If we ask for emissive lighting only
        // then remove bakeDiffuseLighting part.
        if (_DebugLightingMode == DEBUGLIGHTINGMODE_EMISSIVE_LIGHTING)
        {
            builtinData.bakeDiffuseLighting = real3(0.0, 0.0, 0.0);
        }
        else
        {
            builtinData.emissiveColor = real3(0.0, 0.0, 0.0);
        }
    }
#endif

    // Random TAG which we expect will never match provided emissive or lightmap value
    #define AO_IN_GBUFFER3_TAG float3((1 << 11), 1, (1 << 10))

    // RT3 - 11f:11f:10f
    // In deferred we encode emissive color with bakeDiffuseLighting. We don't have the room to store emissiveColor separately.
    // It mean that any futher process that affect bakeDiffuseLighting in the lightloop will also affect emissiveColor, like SSAO for example.
    // For APV (non lightmap case) and SSGI/RTGI/Mixed bakeDiffuseLighting is 0 and below code will simply store emissiveColor
    // Extra hack: In this last case, if emissiveColor is 0 then we store AO inside the buffer with a tag.
    outGBuffer3 = float4(builtinData.bakeDiffuseLighting * surfaceData.ambientOcclusion + builtinData.emissiveColor, 0.0);
    // Pre-expose lighting buffer
    outGBuffer3.rgb *= GetCurrentExposureMultiplier();

    // If this is 0 it mean that both bakeDiffuseLighting and emissiveColor are 0 and we are potentially in case of one effect
    // so store AO instead. It doesn't matter if it is a false positive as result will be correct, this is to reduce code divergence
    // Note: We assume that having non black baseColor * AO * lighting + Emissive is uncommon / rare (We expect mostly baseColor * AO * lighting or Emissive or baseColor * lighting + Emissive)
    // and use this information as a tradeoff to improve quality in all others cases
    if (all(outGBuffer3.rgb == 0.0))
    {
        outGBuffer3.xz = AO_IN_GBUFFER3_TAG.xz;
        outGBuffer3.y = surfaceData.ambientOcclusion;
    }

#ifdef RENDERING_LAYERS
    // Output rendering layers to buffer. Also write geometric normal for use in lighting pass
    // We have to write it again in case the material has disabled decals and normal was not written during prepass
    DecalPrepassData decalPrepassData;
    decalPrepassData.geomNormalWS = surfaceData.geomNormalWS;
    decalPrepassData.renderingLayerMask = GetMeshRenderingLayerMask();
    EncodeIntoDecalPrepassBuffer(decalPrepassData, OUT_GBUFFER_RENDERING_LAYERS);
#endif

#ifdef SHADOWS_SHADOWMASK
    OUT_GBUFFER_SHADOWMASK = BUILTIN_DATA_SHADOW_MASK;
#endif

#ifdef UNITY_VIRTUAL_TEXTURING
    OUT_GBUFFER_VTFEEDBACK = PackVTFeedbackWithAlpha(builtinData.vtPackedFeedback, (float2)positionSS.xy, 1.0);
#endif
}

// Fills the BSDFData. Also returns the (per-pixel) material feature flags inferred
// from the contents of the G-buffer, which can be used by the feature classification system.
// Note that return type is not part of the MACRO DECODE_FROM_GBUFFER, so it is safe to use return value for our need
// 'tileFeatureFlags' are compile-time flags provided by the feature classification system.
// If you're not using the feature classification system, pass UINT_MAX.
// Also, see comment in TileVariantToFeatureFlags. When we are the worse case (i.e last variant), we read the featureflags
// from the structured buffer use to generate the indirect draw call. It allow to not go through all branch and the branch is scalar (not VGPR)
uint DecodeFromGBuffer(uint2 positionSS, uint tileFeatureFlags, out BSDFData bsdfData, out BuiltinData builtinData)
{
    // Note: we have ZERO_INITIALIZE the struct, so bsdfData.diffusionProfileIndex == DIFFUSION_PROFILE_NEUTRAL_ID,
    // bsdfData.anisotropy == 0, bsdfData.subsurfaceMask == 0, etc...
    ZERO_INITIALIZE(BSDFData, bsdfData);
    // Note: Some properties of builtinData are not used, just init all at 0 to silent the compiler
    ZERO_INITIALIZE(BuiltinData, builtinData);

    // Isolate material features.
    tileFeatureFlags &= MATERIAL_FEATURE_MASK_FLAGS;

    GBufferType0 inGBuffer0 = LOAD_TEXTURE2D_X(_GBufferTexture0, positionSS);
    GBufferType1 inGBuffer1 = LOAD_TEXTURE2D_X(_GBufferTexture1, positionSS);
    GBufferType2 inGBuffer2 = LOAD_TEXTURE2D_X(_GBufferTexture2, positionSS);

    // We know the GBufferType no need to use abstraction
#ifdef SHADOWS_SHADOWMASK
    float4 shadowMaskGbuffer = LOAD_TEXTURE2D_X(_ShadowMaskTexture, positionSS);
    builtinData.shadowMask0 = shadowMaskGbuffer.x;
    builtinData.shadowMask1 = shadowMaskGbuffer.y;
    builtinData.shadowMask2 = shadowMaskGbuffer.z;
    builtinData.shadowMask3 = shadowMaskGbuffer.w;
#else
    builtinData.shadowMask0 = 1.0;
    builtinData.shadowMask1 = 1.0;
    builtinData.shadowMask2 = 1.0;
    builtinData.shadowMask3 = 1.0;
#endif

    // SurfaceData

    // Material classification only uses the G-Buffer 2.
    uint materialFeatureId = UnpackByte(inGBuffer2.a) & 0x7;
    bool coloredTransmission = UnpackByte(inGBuffer2.a) & 0x8;
    float coatMask = UnpackCoatMask(inGBuffer2);

    uint pixelFeatureFlags    = MATERIALFEATUREFLAGS_LIT_STANDARD; // Only sky/background do not have the Standard flag.
    bool pixelHasSubsurface   = materialFeatureId == GBUFFER_LIT_TRANSMISSION_SSS || materialFeatureId == GBUFFER_LIT_SSS;
    bool pixelHasTransmission = materialFeatureId == GBUFFER_LIT_TRANSMISSION_SSS || materialFeatureId == GBUFFER_LIT_TRANSMISSION;
    bool pixelHasTransmiRGB   = materialFeatureId == GBUFFER_LIT_TRANSMISSION && coloredTransmission;
    bool pixelHasAnisotropy   = materialFeatureId == GBUFFER_LIT_ANISOTROPIC;
    bool pixelHasIridescence  = materialFeatureId == GBUFFER_LIT_IRIDESCENCE;
    bool pixelHasClearCoat    = coatMask > 0.0;

    // Disable pixel features disabled by the tile.
    pixelFeatureFlags |= tileFeatureFlags & (pixelHasSubsurface   ? MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING : 0);
    pixelFeatureFlags |= tileFeatureFlags & (pixelHasTransmission ? MATERIALFEATUREFLAGS_LIT_TRANSMISSION          : 0);
    pixelFeatureFlags |= tileFeatureFlags & (pixelHasTransmiRGB   ? MATERIALFEATUREFLAGS_LIT_COLORED_TRANSMISSION  : 0);
    pixelFeatureFlags |= tileFeatureFlags & (pixelHasAnisotropy   ? MATERIALFEATUREFLAGS_LIT_ANISOTROPY            : 0);
    pixelFeatureFlags |= tileFeatureFlags & (pixelHasIridescence  ? MATERIALFEATUREFLAGS_LIT_IRIDESCENCE           : 0);
    pixelFeatureFlags |= tileFeatureFlags & (pixelHasClearCoat    ? MATERIALFEATUREFLAGS_LIT_CLEAR_COAT            : 0);

    // In the case of material classification we assign tileFeatureFlags to bsdfData.materialFeatures
    // This mean that the branch inside the tile will be the same (coherency). Remember that a divergent branch
    // on AMD GCN mean we will execute both branch for all fragement. We setup at pixel level values
    // such that a particular branch will not have effect if it shouldn't. For example if SSS is enabled,
    // setup a sssMask of 0 don't have any effect and we can safely take the SSS branch for the tile.
    // Note that in the catch all variant of material classification we get the value from the structure buffer done
    // in the classification pass. Mean even in catch all, we it is high likely that we don't have tileFeatureFlags == MATERIAL_FEATURE_MASK_FLAGS case.

    // tileFeatureFlags == MATERIAL_FEATURE_MASK_FLAGS can appear in following situation
    // call from deferred.shader or other shader that doesn't peform material classification
    // call from last catch all variant in material classification, which mean we have all possible material inside a same tile (very rare)
    // call from a specific case in material classification (currently we have variant 0)
    // When this happen, we prefer to use the pixelFeatureFlags rather than the tileFeatureFlags as bsdfData.materialFeatures
    // because there is more likelihood to save performance (excep in the very rare case of catch all of material classification).
    // We can indeed have divergence inside a tile (like having aniso and not aniso)
    // but it is more likely that the whole time is convergent (like everything have SSS and clear coat).
    if (tileFeatureFlags == MATERIAL_FEATURE_MASK_FLAGS)
    {
        bsdfData.materialFeatures = pixelFeatureFlags;
        tileFeatureFlags = pixelFeatureFlags; // Required for the aniso test (see below)
    }
    else
    {
        bsdfData.materialFeatures = tileFeatureFlags;
    }

    // Decompress feature-agnostic data from the G-Buffer.
    float3 baseColor = inGBuffer0.rgb;

    NormalData normalData;
    DecodeFromNormalBuffer(inGBuffer1, normalData);
    bsdfData.normalWS = normalData.normalWS;
    bsdfData.geomNormalWS = bsdfData.normalWS; // If geometric normal is not available, fallback on normal map
    bsdfData.perceptualRoughness = normalData.perceptualRoughness;
    bsdfData.fresnel90    = 1.0f;

    // Avoid to introduce a new variant for light layer as it is already long to compile
    float4 inGBuffer4 = LOAD_TEXTURE2D_X(_RenderingLayersTexture, positionSS);
    builtinData.renderingLayers = _EnableLightLayers ? UnpackMeshRenderingLayerMask(inGBuffer4) : RENDERING_LAYERS_MASK;
    if (_EnableDecalLayers && (_EnableRenderingLayers || _EnableLightLayers))
        bsdfData.geomNormalWS = UnpackNormalOctQuadEncode(inGBuffer4.zw * 2.0 - 1.0);

    // Decompress feature-specific data from the G-Buffer.
    bool pixelHasMetallic = HasFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_ANISOTROPY | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE);

    if (pixelHasMetallic)
    {
        float metallic;
        uint unused;
        UnpackFloatInt8bit(inGBuffer2.b, 8, metallic, unused);

        bsdfData.diffuseColor = ComputeDiffuseColor(baseColor, metallic);
        bsdfData.fresnel0     = ComputeFresnel0(baseColor, metallic, DEFAULT_SPECULAR_VALUE);
    }
    else
    {
        bsdfData.diffuseColor = baseColor;
        bsdfData.fresnel0     = FastSRGBToLinear(inGBuffer2.rgb); // Later possibly overwritten by SSS
    }

    if (HasFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        SSSData sssData;
        float transmissionMask;

        #ifdef DEBUG_DISPLAY
        // Note that we don't use sssData.subsurfaceMask here. But it is still assign so we can have
        // the information in the material debug view.
        UnpackFloatInt8bit(inGBuffer0.a, 16, sssData.subsurfaceMask, sssData.diffusionProfileIndex);
        #else
        sssData.subsurfaceMask = 0.0f; // Initialize to prevent compiler error, but value is never used
        #endif

        // We read profile from G-Buffer 2 so the compiler can optimize away the read from the G-Buffer 0 to the very end (in PostEvaluateBSDF)
        // When using translucency, we exchange diffusion profile and coat mask
        UnpackFloatInt8bit(inGBuffer2.b, 16, transmissionMask, sssData.diffusionProfileIndex);

        // Reminder: when using SSS we exchange specular occlusion and subsurfaceMask/profileID
        bsdfData.specularOcclusion = inGBuffer2.r;

        // Note: both function assign profile and overwrite fresnel0 (both SSS and Transmission)
        // in case one feature is enabled and not the other.

        // The neutral value of subsurfaceMask is 0 (handled by ZERO_INITIALIZE).
        if (HasFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING))
        {
            FillMaterialSSS(sssData.diffusionProfileIndex, sssData.subsurfaceMask, bsdfData);
            FillMaterialAdvancedSSS(bsdfData);
        }

        // The neutral value of thickness and transmittance is 0 (handled by ZERO_INITIALIZE).
        if (HasFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
        {
            if (HasFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_COLORED_TRANSMISSION))
            {
                uint rgb20 = (UnpackByte(inGBuffer2.g) << 12) | (UnpackByte(inGBuffer2.b) << 4) | (UnpackByte(inGBuffer2.a) >> 4);
                FillMaterialColoredTranslucent(UnpackFromR7G7B6(rgb20), bsdfData);
            }
            else
                FillMaterialTransmission(sssData.diffusionProfileIndex, inGBuffer2.g, transmissionMask.xxx, bsdfData);
        }
    }
    else
    {
        bsdfData.specularOcclusion = inGBuffer0.a;
        bsdfData.fresnel90 = ComputeF90(bsdfData.fresnel0);
    }

    // Special handling for anisotropy: When anisotropy is present in a tile, the whole tile will use anisotropy to avoid divergent evaluation of GGX that increase the cost
    // Note that it mean that when we have the worse case, we always use Anisotropy and shader like deferred.shader are always the worst case (but only used for debugging)
    if (HasFlag(tileFeatureFlags, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
    {
        float anisotropy = 0;
        float3x3 frame = GetLocalFrame(bsdfData.normalWS);

        if (HasFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
        {
            anisotropy = inGBuffer2.r * 2.0 - 1.0;

            float unused;
            uint tangentFlags;
            UnpackFloatInt8bit(inGBuffer2.b, 8, unused, tangentFlags);

            // Get the rotation angle of the actual tangent frame with respect to the default one.
            float sinOrCos = (0.5/256.0 * rsqrt(2)) + (255.0/256.0 * rsqrt(2)) * inGBuffer2.g;
            float cosOrSin = sqrt(1 - sinOrCos * sinOrCos);
            bool  storeSin = tangentFlags != 0;
            float sinFrame = storeSin ? sinOrCos : cosOrSin;
            float cosFrame = storeSin ? cosOrSin : sinOrCos;

            // Rotate the reconstructed tangent around the normal.
            frame[0] = sinFrame * frame[1] + cosFrame * frame[0];
            frame[1] = cross(frame[2], frame[0]);
        }

        FillMaterialAnisotropy(anisotropy, frame[0], frame[1], bsdfData);
    }

    // The neutral value of iridescenceMask is 0 (handled by ZERO_INITIALIZE).
    if (HasFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_IRIDESCENCE))
    {
        FillMaterialIridescence(inGBuffer2.r, inGBuffer2.g, bsdfData);
    }

    // The neutral value of coatMask is 0 (handled by ZERO_INITIALIZE).
    if (HasFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        // Modify perceptualRoughness
        FillMaterialClearCoatData(coatMask, bsdfData);
    }

    // Note: the full code below (for both roughness) only execute when we have enableAnisotropy == true, otherwise as we only use roughnessT compiler will optimize out
    // Mean that in the worst case we always execute it.

    // roughnessT and roughnessB are clamped, and are meant to be used with punctual and directional lights.
    // perceptualRoughness is not clamped, and is meant to be used for IBL.
    // perceptualRoughness can be modify by FillMaterialClearCoatData, so ConvertAnisotropyToClampRoughness must be call after
    ConvertAnisotropyToRoughness(bsdfData.perceptualRoughness, bsdfData.anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);

#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    UnpackFloatInt8bit(bsdfData.specularOcclusion, 2, bsdfData.specularOcclusion, builtinData.isLightmap);
#endif

    // BuiltinData
    // _GBufferTexture3 contain lightmaps/lightprobe and emissive by default.
    // When any SSGI/RTGI/Mixed effect is enabled it contain emissive only
    // When APV is enabled it contain lightmaps or emissive. We use builtinData.isLightmap to know if we are emissive only
    // In the regular case the lightmaps/lightprobe are multiply by AO before adding emissive
    float3 gbuffer3 = LOAD_TEXTURE2D_X(_GBufferTexture3, positionSS).rgb;

    // In deferred case, AO is apply during the EncodeToGbuffer pass on bakeDiffuseLighting data but not emissive
    // This cause quality issue because it prevent us to combine it correctly with SSAO (i.e min(SSAO, AO)) + SSAO is apply on emissive
    // As explain in encoding step for SSGI/RTGI/Mixed and APV not using lightmap, we rely on a hack to retrieve AO
    // Then we could use the regular path (like in Forward) and get correct rendering.
    if (all(gbuffer3.xz == AO_IN_GBUFFER3_TAG.xz)) //Note: this check is duplicate in debugViewMaterialGBuffer.shader
        bsdfData.ambientOcclusion = gbuffer3.y;
    else
    {
        gbuffer3 *= GetInverseCurrentExposureMultiplier();
        bsdfData.ambientOcclusion = 1.0;

        // For SSGI/RTGI/Mixed and APV not using lightmap we load the content of gbuffer3 in emissive, otherwise it is lightmap/lightprobe + emissive
        if ((_IndirectDiffuseMode != INDIRECTDIFFUSEMODE_OFF && _IndirectDiffuseMode != INDIRECTDIFFUSEMODE_MIXED)
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
            || !builtinData.isLightmap
#endif
            )
        {
            builtinData.emissiveColor = gbuffer3;
        }
        else
        {
            builtinData.bakeDiffuseLighting = gbuffer3;
        }
    }

    ApplyDebugToBSDFData(bsdfData);

    return pixelFeatureFlags;
}

// Function call from the material classification compute shader
uint MaterialFeatureFlagsFromGBuffer(uint2 positionSS)
{
    BSDFData bsdfData;
    BuiltinData unused;
    // Call the regular function, compiler will optimized out everything not used.
    // Note that all material feature flag bellow are in the same GBuffer (inGBuffer2) and thus material classification only sample one Gbuffer
    return DecodeFromGBuffer(positionSS, UINT_MAX, bsdfData, unused);
}

//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------

void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
    case DEBUGVIEW_LIT_SURFACEDATA_NORMAL_VIEW_SPACE:
        {
            float3 vsNormal = TransformWorldToViewDir(surfaceData.normalWS);
            result = IsNormalized(vsNormal) ?  vsNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_LIT_SURFACEDATA_MATERIAL_FEATURES:
        result = (surfaceData.materialFeatures.xxx) / 255.0; // Aloow to read with color picker debug mode
        break;
    case DEBUGVIEW_LIT_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        {
            float3 vsGeomNormal = TransformWorldToViewDir(surfaceData.geomNormalWS);
            result = IsNormalized(vsGeomNormal) ?  vsGeomNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_LIT_SURFACEDATA_INDEX_OF_REFRACTION:
        result = saturate((surfaceData.ior - 1.0) / 1.5).xxx;
        break;
    case DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_COLOR:
        {
            if (!HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR))
            {
                // Derive the specular/fresnel0 term from the metallic parameter
                result = ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic.x, DEFAULT_SPECULAR_VALUE);
            }
            break;
        }
    }
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
    case DEBUGVIEW_LIT_BSDFDATA_NORMAL_VIEW_SPACE:
        // Convert to view space
        {
            float3 vsNormal = TransformWorldToViewDir(bsdfData.normalWS);
            result = IsNormalized(vsNormal) ?  vsNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_LIT_BSDFDATA_MATERIAL_FEATURES:
        result = (bsdfData.materialFeatures.xxx) / 255.0; // Aloow to read with color picker debug mode
        break;
    case DEBUGVIEW_LIT_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        {
            float3 vsGeomNormal = TransformWorldToViewDir(bsdfData.geomNormalWS);
            result = IsNormalized(vsGeomNormal) ?  vsGeomNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_LIT_BSDFDATA_IOR:
        result = saturate((bsdfData.ior - 1.0) / 1.5).xxx;
        break;
    }
}

void GetPBRValidatorDebug(SurfaceData surfaceData, inout float3 result)
{
    result = surfaceData.baseColor;
}

//-----------------------------------------------------------------------------
// PreLightData
//-----------------------------------------------------------------------------

// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    float NdotV;                     // Could be negative due to normal mapping, use ClampNdotV()

    // GGX
    float partLambdaV;
    float energyCompensation;

    // IBL
    float3 iblR;                     // Reflected specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness;

    float3 specularFGD;              // Store preintegrated BSDF for both specular and diffuse
    float  diffuseFGD;

    // Area lights
    // TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.
    float3x3 orthoBasisViewNormal;   // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
    // Warning: these matrices are transposed! They are designed to transform row vectors via mul(V, M).
    float3x3 ltcTransformDiffuse;    // Inverse transformation for Lambertian or Disney Diffuse        (4x VGPRs)
    float3x3 ltcTransformSpecular[2];// Inverse transformation for GGX - 2 specular lobes              (4x VGPRs * 2)
    float    ltcLobeMix;             // We store it only for area lights to save the vgpr otherwise    (1x VGPR)

    // Clear coat
    float    coatPartLambdaV;
    float3   coatIblR;
    float    coatIblF;               // Fresnel term for view vector
    float    coatReflectionWeight;   // like reflectionHierarchyWeight but used to distinguish coat contribution between SSR/IBL lighting
    float3x3 ltcTransformCoat;       // Inverse transformation for GGX                                 (4x VGPRs)

#if HAS_REFRACTION
    // Refraction
    float3 transparentRefractV;      // refracted view vector after exiting the shape
    float3 transparentPositionWS;    // start of the refracted ray after exiting the shape
    float3 transparentTransmittance; // transmittance due to absorption
    float transparentSSMipLevel;     // mip level of the screen space gaussian pyramid for rough refraction
#endif
};

//
// ClampRoughness helper specific to this material
//
void ClampRoughness(inout PreLightData preLightData, inout BSDFData bsdfData, float minRoughness)
{
    bsdfData.roughnessT    = max(minRoughness, bsdfData.roughnessT);
    bsdfData.roughnessB    = max(minRoughness, bsdfData.roughnessB);
    bsdfData.coatRoughness = max(minRoughness, bsdfData.coatRoughness);
}

PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    ZERO_INITIALIZE(PreLightData, preLightData);

    float3 N = bsdfData.normalWS;
    preLightData.NdotV = dot(N, V);
    preLightData.iblPerceptualRoughness = bsdfData.perceptualRoughness;

    float clampedNdotV = ClampNdotV(preLightData.NdotV);

    // We modify the bsdfData.fresnel0 here for iridescence
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_IRIDESCENCE))
    {
        float viewAngle = clampedNdotV;
        float topIor = 1.0; // Default is air
        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
        {
            topIor = lerp(1.0, CLEAR_COAT_IOR, bsdfData.coatMask);
            // HACK: Use the reflected direction to specify the Fresnel coefficient for pre-convolved envmaps
            if (bsdfData.coatMask != 0.0f) // We must make sure that effect is neutral when coatMask == 0
                viewAngle = sqrt(1.0 + Sq(1.0 / topIor) * (Sq(dot(bsdfData.normalWS, V)) - 1.0));
        }

        if (bsdfData.iridescenceMask > 0.0)
        {
            bsdfData.fresnel0 = lerp(bsdfData.fresnel0, EvalIridescence(topIor, viewAngle, bsdfData.iridescenceThickness, bsdfData.fresnel0), bsdfData.iridescenceMask);
        }
    }

    // We modify the bsdfData.fresnel0 here for clearCoat
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        // Fresnel0 is deduced from interface between air and material (Assume to be 1.5 in Unity, or a metal).
        // but here we go from clear coat (1.5) to material, we need to update fresnel0
        // Note: Schlick is a poor approximation of Fresnel when ieta is 1 (1.5 / 1.5), schlick target 1.4 to 2.2 IOR.
        bsdfData.fresnel0 = lerp(bsdfData.fresnel0, ConvertF0ForAirInterfaceToF0ForClearCoat15(bsdfData.fresnel0), bsdfData.coatMask);

        preLightData.coatPartLambdaV = GetSmithJointGGXPartLambdaV(clampedNdotV, CLEAR_COAT_ROUGHNESS);
        preLightData.coatIblR = reflect(-V, N);
        preLightData.coatIblF = F_Schlick(CLEAR_COAT_F0, clampedNdotV) * bsdfData.coatMask;
        preLightData.coatReflectionWeight = 0.0;
    }

    // Handle IBL + area light + multiscattering.
    // Note: use the not modified by anisotropy iblPerceptualRoughness here.
    float specularReflectivity;
    GetPreIntegratedFGDGGXAndDisneyDiffuse(clampedNdotV, preLightData.iblPerceptualRoughness, bsdfData.fresnel0, bsdfData.fresnel90, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
#ifdef USE_DIFFUSE_LAMBERT_BRDF
    preLightData.diffuseFGD = 1.0;
#endif

#ifdef LIT_USE_GGX_ENERGY_COMPENSATION
    // Ref: Practical multiple scattering compensation for microfacet models.
    // We only apply the formulation for metals.
    // For dielectrics, the change of reflectance is negligible.
    // We deem the intensity difference of a couple of percent for high values of roughness
    // to not be worth the cost of another precomputed table.
    // Note: this formulation bakes the BSDF non-symmetric!
    preLightData.energyCompensation = 1.0 / specularReflectivity - 1.0;
#else
    preLightData.energyCompensation = 0.0;
#endif // LIT_USE_GGX_ENERGY_COMPENSATION

    float3 iblN;

    // We avoid divergent evaluation of the GGX, as that nearly doubles the cost.
    // If the tile has anisotropy, all the pixels within the tile are evaluated as anisotropic.
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
    {
        float TdotV = dot(bsdfData.tangentWS,   V);
        float BdotV = dot(bsdfData.bitangentWS, V);

        preLightData.partLambdaV = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, clampedNdotV, bsdfData.roughnessT, bsdfData.roughnessB);

        // perceptualRoughness is use as input and output here
        GetGGXAnisotropicModifiedNormalAndRoughness(bsdfData.bitangentWS, bsdfData.tangentWS, N, V, bsdfData.anisotropy, preLightData.iblPerceptualRoughness, iblN, preLightData.iblPerceptualRoughness);
    }
    else
    {
        preLightData.partLambdaV = GetSmithJointGGXPartLambdaV(clampedNdotV, bsdfData.roughnessT);
        iblN = N;
    }

    preLightData.iblR = reflect(-V, iblN);

    // Area light
#ifdef USE_DIFFUSE_LAMBERT_BRDF
    preLightData.ltcTransformDiffuse = k_identity3x3;

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_SSS_DIFFUSE_POWER))
        ModifyLambertLTCTransformForDiffusePower(preLightData.ltcTransformDiffuse, GetDiffusePower(bsdfData.diffusionProfileIndex));
#else
    preLightData.ltcTransformDiffuse = SampleLtcMatrix(bsdfData.perceptualRoughness, clampedNdotV, LTCLIGHTINGMODEL_DISNEY_DIFFUSE);

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_SSS_DIFFUSE_POWER))
        ModifyDisneyLTCTransformForDiffusePower(preLightData.ltcTransformDiffuse, GetDiffusePower(bsdfData.diffusionProfileIndex), bsdfData.perceptualRoughness, clampedNdotV);
#endif

    float perceptualRoughnessA = bsdfData.perceptualRoughness;

    // This is a dynamic branch if the MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING flag is enabled.
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_SSS_DUAL_LOBE))
    {
        float lobeA, lobeB, perceptualRoughnessB;
        GetDualLobeParameters(bsdfData.diffusionProfileIndex, lobeA, lobeB, preLightData.ltcLobeMix);

        perceptualRoughnessA = PerceptualSmoothnessToPerceptualRoughness(saturate((1.0f - bsdfData.perceptualRoughness) * lobeA));
        perceptualRoughnessB = PerceptualSmoothnessToPerceptualRoughness(saturate((1.0f - bsdfData.perceptualRoughness) * lobeB));

        preLightData.ltcTransformSpecular[1] = SampleLtcMatrix(perceptualRoughnessB, clampedNdotV, LTCLIGHTINGMODEL_GGX);
    }

    preLightData.ltcTransformSpecular[0] = SampleLtcMatrix(perceptualRoughnessA, clampedNdotV, LTCLIGHTINGMODEL_GGX);

    // Construct a right-handed view-dependent orthogonal basis around the normal
    preLightData.orthoBasisViewNormal = GetOrthoBasisViewNormal(V, N, preLightData.NdotV);

    preLightData.ltcTransformCoat = 0.0;
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        preLightData.ltcTransformCoat = SampleLtcMatrix(CLEAR_COAT_PERCEPTUAL_ROUGHNESS, clampedNdotV, LTCLIGHTINGMODEL_GGX);
    }

    // refraction (forward only)
#if HAS_REFRACTION
    RefractionModelResult refraction = REFRACTION_MODEL(V, posInput, bsdfData);
    preLightData.transparentRefractV = refraction.rayWS;
    preLightData.transparentPositionWS = refraction.positionWS;
    preLightData.transparentTransmittance = exp(-bsdfData.absorptionCoefficient * refraction.dist);

    // Empirical remap to try to match a bit the refraction probe blurring for the fallback
    // Use IblPerceptualRoughness so we can handle approx of clear coat.
    preLightData.transparentSSMipLevel = PositivePow(preLightData.iblPerceptualRoughness, 1.3) * uint(max(_ColorPyramidLodCount - 1, 0));
#endif

    return preLightData;
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

// This define allow to say that we implement a ModifyBakedDiffuseLighting function to be call in PostInitBuiltinData
#define MODIFY_BAKED_DIFFUSE_LIGHTING

// This function allow to modify the content of (back) baked diffuse lighting when we gather builtinData
// This is use to apply lighting model specific code, like pre-integration, transmission etc...
// It is up to the lighting model implementer to chose if the modification are apply here or in PostEvaluateBSDF
void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, inout BuiltinData builtinData)
{
    // In case of deferred, all lighting model operation are done before storage in GBuffer, as we store emissive with bakeDiffuseLighting

    // Add GI transmission contribution to bakeDiffuseLighting, we then drop backBakeDiffuseLighting (i.e it is not used anymore, this save VGPR in forward and in deferred we can't store it anyway)
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        builtinData.bakeDiffuseLighting += builtinData.backBakeDiffuseLighting * bsdfData.transmittance;
    }

    // For SSS we need to take into account the state of diffuseColor
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING))
    {
        bsdfData.diffuseColor = GetModifiedDiffuseColorForSSS(bsdfData);
    }

    // Premultiply (back) bake diffuse lighting information with DisneyDiffuse pre-integration
    // Note: When baking reflection probes, we approximate the diffuse with the fresnel0
    builtinData.bakeDiffuseLighting *= preLightData.diffuseFGD * GetDiffuseOrDefaultColor(bsdfData, _ReplaceDiffuseForIndirect).rgb;
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------

LightTransportData GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LightTransportData lightTransportData;

    // diffuseColor for lightmapping should basically be diffuse color.
    // But rough metals (black diffuse) still scatter quite a lot of light around, so
    // we want to take some of that into account too.

    float roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
    lightTransportData.diffuseColor = bsdfData.diffuseColor + bsdfData.fresnel0 * roughness * 0.5 * surfaceData.metallic;
    lightTransportData.emissiveColor = builtinData.emissiveColor;

    return lightTransportData;
}

//-----------------------------------------------------------------------------
// LightLoop related function (Only include if required)
// HAS_LIGHTLOOP is define in Lighting.hlsl
//-----------------------------------------------------------------------------

#ifdef HAS_LIGHTLOOP

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

bool IsNonZeroBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    float NdotL = dot(bsdfData.normalWS, L);

    return HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION) || (NdotL > 0.0);
}

CBSDF EvaluateBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    float3 N = bsdfData.normalWS;

    float NdotV = preLightData.NdotV;
    float NdotL = dot(N, L);
    float clampedNdotV = ClampNdotV(NdotV);
    float clampedNdotL = saturate(NdotL);
    float flippedNdotL = ComputeWrappedDiffuseLighting(-NdotL, TRANSMISSION_WRAP_LIGHT);
    float diffuseNdotL = clampedNdotL;

    float LdotV, NdotH, LdotH, invLenLV;
    GetBSDFAngle(V, L, NdotL, NdotV, LdotV, NdotH, LdotH, invLenLV);

    // This F90 term can be used as a way to suppress completely specular when using the specular workflow.
    float3 F = F_Schlick(bsdfData.fresnel0, bsdfData.fresnel90, LdotH);
    // Remark: Fresnel must be use with LdotH angle. But Fresnel for iridescence is expensive to compute at each light.
    // Instead we use the incorrect angle NdotV as an approximation for LdotH for Fresnel evaluation.
    // The Fresnel with iridescence and NDotV angle is precomputed ahead and here we jsut reuse the result.
    // Thus why we shouldn't apply a second time Fresnel on the value if iridescence is enabled.
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_IRIDESCENCE))
    {
        F = lerp(F, bsdfData.fresnel0, bsdfData.iridescenceMask);
    }

    float DV;
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
    {
        float3 H = (L + V) * invLenLV;

        // For anisotropy we must not saturate these values
        float TdotH = dot(bsdfData.tangentWS, H);
        float TdotL = dot(bsdfData.tangentWS, L);
        float BdotH = dot(bsdfData.bitangentWS, H);
        float BdotL = dot(bsdfData.bitangentWS, L);

        // TODO: Do comparison between this correct version and the one from isotropic and see if there is any visual difference
        // We use abs(NdotL) to handle the none case of double sided
        DV = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH, clampedNdotV, TdotL, BdotL, abs(NdotL),
                                   bsdfData.roughnessT, bsdfData.roughnessB, preLightData.partLambdaV);
    }
    else if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_SSS_DUAL_LOBE))
    {
        // We reload roughnesses from diffusion profile to save VGPRs. As a result we don't have correct ClampRoughness but it's ok
        float lobeA, lobeB, lobeMix;
        GetDualLobeParameters(bsdfData.diffusionProfileIndex, lobeA, lobeB, lobeMix);

        // We have to inline the call to PerceptualRoughnessToPerceptualSmoothness for better codegen
        float roughnessL = PerceptualSmoothnessToRoughness(saturate(lobeA - bsdfData.perceptualRoughness * lobeA));
        float roughnessH = PerceptualSmoothnessToRoughness(saturate(lobeB - bsdfData.perceptualRoughness * lobeB));

        DV = lerp(DV_SmithJointGGX(NdotH, abs(NdotL), clampedNdotV, roughnessL, preLightData.partLambdaV),
                  DV_SmithJointGGX(NdotH, abs(NdotL), clampedNdotV, roughnessH, preLightData.partLambdaV),
                  lobeMix);
    }
    else
    {
        // We use abs(NdotL) to handle the none case of double sided
        DV = DV_SmithJointGGX(NdotH, abs(NdotL), clampedNdotV, bsdfData.roughnessT, preLightData.partLambdaV);
    }

    float3 specTerm = F * DV;

#ifdef USE_DIFFUSE_LAMBERT_BRDF
    float diffTerm = Lambert();
#else
    // A note on subsurface scattering: [SSS-NOTE-TRSM]
    // The correct way to handle SSS is to transmit light inside the surface, perform SSS,
    // and then transmit it outside towards the viewer.
    // Transmit(X) = F_Transm_Schlick(F0, F90, NdotX), where F0 = 0, F90 = 1.
    // Therefore, the diffuse BSDF should be decomposed as follows:
    // f_d = A / Pi * F_Transm_Schlick(0, 1, NdotL) * F_Transm_Schlick(0, 1, NdotV) + f_d_reflection,
    // with F_Transm_Schlick(0, 1, NdotV) applied after the SSS pass.
    // The alternative (artistic) formulation of Disney is to set F90 = 0.5:
    // f_d = A / Pi * F_Transm_Schlick(0, 0.5, NdotL) * F_Transm_Schlick(0, 0.5, NdotV) + f_retro_reflection.
    // That way, darkening at grading angles is reduced to 0.5.
    // In practice, applying F_Transm_Schlick(F0, F90, NdotV) after the SSS pass is expensive,
    // as it forces us to read the normal buffer at the end of the SSS pass.
    // Separating f_retro_reflection also has a small cost (mostly due to energy compensation
    // for multi-bounce GGX), and the visual difference is negligible.
    // Therefore, we choose not to separate diffuse lighting into reflected and transmitted.

    // Use abs NdotL to evaluate diffuse term also for transmission
    // TODO: See with Evgenii about the clampedNdotV here. This is what we use before the refactor
    // but now maybe we want to revisit it for transmission
    float diffTerm = DisneyDiffuse(clampedNdotV, abs(NdotL), LdotV, bsdfData.perceptualRoughness);
#endif

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        // Apply isotropic GGX for clear coat
        // Note: coat F is scalar as it is a dieletric
        float coatF = F_Schlick(CLEAR_COAT_F0, LdotH) * bsdfData.coatMask;
        // Scale base specular
        specTerm *= Sq(1.0 - coatF);

        // Add top specular
        // TODO: Should we call just D_GGX here ?
        // We use abs(NdotL) to handle the none case of double sided
        float DV = DV_SmithJointGGX(NdotH, abs(NdotL), clampedNdotV, bsdfData.coatRoughness, preLightData.coatPartLambdaV);
        specTerm += coatF * DV;

        // Note: The modification of the base roughness and fresnel0 by the clear coat is already handled in FillMaterialClearCoatData

        // Very coarse attempt at doing energy conservation for the diffuse layer based on NdotL. No science.
        diffTerm *= lerp(1, 1.0 - coatF, bsdfData.coatMask);
    }

    // Diffuse power modification
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_SSS_DIFFUSE_POWER))
    {
        float power = GetDiffusePower(bsdfData.diffusionProfileIndex);
        diffuseNdotL = pow(diffuseNdotL, max(power + 1, 1.0f));
        diffuseNdotL *= power * 0.5 + 1; // normalize
    }

    // The compiler should optimize these. Can revisit later if necessary.
    cbsdf.diffR = diffTerm * diffuseNdotL;
    cbsdf.diffT = diffTerm * flippedNdotL;

    // Probably worth branching here for perf reasons.
    // This branch will be optimized away if there's no transmission.
    if (NdotL > 0)
    {
        cbsdf.specR = specTerm * clampedNdotL;
    }

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    return cbsdf;
}

//-----------------------------------------------------------------------------
// Surface shading (all light types) below
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/SurfaceShading.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                                        float3 V, PositionInputs posInput,
                                        PreLightData preLightData, DirectionalLightData lightData,
                                        BSDFData bsdfData, BuiltinData builtinData)
{
    return ShadeSurface_Directional(lightLoopContext, posInput, builtinData, preLightData, lightData, bsdfData, V);
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData,
                                     BSDFData bsdfData, BuiltinData builtinData)
{
    return ShadeSurface_Punctual(lightLoopContext, posInput, builtinData, preLightData, lightData, bsdfData, V);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitReference.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    const bool isRectLight = lightData.lightType == GPULIGHTTYPE_RECTANGLE; // static

#if SHADEROPTIONS_BARN_DOOR
    if (isRectLight)
    {
        RectangularLightApplyBarnDoor(lightData, posInput.positionWS);
    }
#endif

#ifdef LIT_DISPLAY_REFERENCE_AREA
    if (posInput.positionNDC.x < 0.5)
    {
        if (isRectLight)
        {
            IntegrateBSDF_AreaRef(V, positionWS, preLightData, lightData, bsdfData,
                                  lighting.diffuse, lighting.specular);
        }
        else
        {
            IntegrateBSDF_LineRef(V, positionWS, preLightData, lightData, bsdfData,
                                  lighting.diffuse, lighting.specular);
        }
    }
    else
    {
#endif // LIT_DISPLAY_REFERENCE_AREA

    // Translate the light s.t. the shaded point is at the origin of the coordinate system.
    float3 unL = lightData.positionRWS - posInput.positionWS;

    // These values could be precomputed on CPU to save VGPR or ALU.
    float halfLength = lightData.size.x * 0.5;
    float halfHeight = lightData.size.y * 0.5; // = 0 for a line light

    float intensity = PillowWindowing(unL, lightData.right, lightData.up, halfLength, halfHeight,
                                      lightData.rangeAttenuationScale, lightData.rangeAttenuationBias);

    // Make sure the light is front-facing (and has a non-zero effective area).
    intensity *= (isRectLight && dot(unL, lightData.forward) >= 0) ? 0 : 1;

    bool isVisible = true;

    // Raytracing shadow algorithm require to evaluate lighting without shadow, so it defined SKIP_RASTERIZED_AREA_SHADOWS
    // This is only present in Lit Material as it is the only one using the improved shadow algorithm.
#ifndef SKIP_RASTERIZED_AREA_SHADOWS
    if (isRectLight && intensity > 0)
    {
        SHADOW_TYPE shadow = EvaluateShadow_RectArea(lightLoopContext, posInput, lightData, builtinData, bsdfData.normalWS, normalize(lightData.positionRWS), length(lightData.positionRWS));
        lightData.color.rgb *= ComputeShadowColor(shadow, lightData.shadowTint, lightData.penumbraTint);

        isVisible = Max3(lightData.color.r, lightData.color.g, lightData.color.b) > 0;
    }
#endif

    // Terminate if the shaded point is occluded or is too far away.
    if (isVisible && intensity > 0)
    {
        // Rotate the light vectors into the local coordinate system.
        float3 center = mul(preLightData.orthoBasisViewNormal, unL);
        float3 right  = mul(preLightData.orthoBasisViewNormal, lightData.right);
        float3 up     = mul(preLightData.orthoBasisViewNormal, lightData.up);

        float4 ltcValue;

        // ----- 1. Evaluate the diffuse part -----

        ltcValue = EvaluateLTC_Area(isRectLight, center, right, up, halfLength, halfHeight,
                                #ifdef USE_DIFFUSE_LAMBERT_BRDF
                                    k_identity3x3, 1.0f,
                                #else
                                    // LTC light cookies appear broken unless diffuse roughness is set to 1.
                                    transpose(preLightData.ltcTransformDiffuse), /*bsdfData.perceptualRoughness*/ 1.0f,
                                #endif
                                    lightData.cookieMode, lightData.cookieScaleOffset);

        ltcValue.a *= intensity * lightData.diffuseDimmer;

        // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
        lighting.diffuse += ltcValue.rgb * ltcValue.a;

        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
        {
            // Flip the surface while maintaining the view direction.
            float3x3 flipMatrix = float3x3(1,  0,  0,
                                           0, -1,  0,
                                           0,  0, -1);

            // Transform the vectors instead of transforming the basis.
            // Use the Lambertian approximation for performance reasons.
            // TODO: performing the evaluation twice is very inefficient!
            ltcValue = EvaluateLTC_Area(isRectLight, mul(flipMatrix, center), mul(flipMatrix, right), mul(flipMatrix, up), halfLength, halfHeight,
                                        k_identity3x3, 1.0f,
                                        lightData.cookieMode, lightData.cookieScaleOffset);

            ltcValue.a *= intensity * lightData.diffuseDimmer;

            // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
            // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            lighting.diffuse += bsdfData.transmittance * ltcValue.rgb * ltcValue.a;
        }

        // ----- 2. Evaluate the specular part -----

        float perceptualRoughnessA = bsdfData.perceptualRoughness;
        float perceptualRoughnessB = 0; // Set it to 0 just to avoid a warning from the compiler

        // This is a dynamic branch if the MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING flag is enabled.
        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_SSS_DUAL_LOBE))
        {
            float lobeA, lobeB;
            GetDualLobeParameters(bsdfData.diffusionProfileIndex, lobeA, lobeB, preLightData.ltcLobeMix);

            perceptualRoughnessA = PerceptualSmoothnessToPerceptualRoughness(saturate((1.0f - bsdfData.perceptualRoughness) * lobeA));
            perceptualRoughnessB = PerceptualSmoothnessToPerceptualRoughness(saturate((1.0f - bsdfData.perceptualRoughness) * lobeB));
        }

        // First lobe
        ltcValue = EvaluateLTC_Area(isRectLight, center, right, up, halfLength, halfHeight,
                                    transpose(preLightData.ltcTransformSpecular[0]), perceptualRoughnessA,
                                    lightData.cookieMode, lightData.cookieScaleOffset);

        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_SSS_DUAL_LOBE))
        {
            // Second lobe
            float4 ltcValue1 = EvaluateLTC_Area(isRectLight, center, right, up, halfLength, halfHeight,
                                                transpose(preLightData.ltcTransformSpecular[1]), perceptualRoughnessB,
                                                lightData.cookieMode, lightData.cookieScaleOffset);

            // Mix the lobes
            ltcValue = lerp(ltcValue, ltcValue1, preLightData.ltcLobeMix);
        }

        ltcValue.a *= intensity * lightData.specularDimmer;

        // We need to multiply by the magnitude of the integral of the BRDF
        // ref: http://advances.realtimerendering.com/s2016/s2016_ltc_fresnel.pdf
        lighting.specular += preLightData.specularFGD * ltcValue.rgb * ltcValue.a;

        // ----- 3. Evaluate the clear coat part -----

        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
        {
            ltcValue = EvaluateLTC_Area(isRectLight, center, right, up, halfLength, halfHeight,
                                        transpose(preLightData.ltcTransformCoat), RoughnessToPerceptualRoughness(bsdfData.coatRoughness),
                                        lightData.cookieMode, lightData.cookieScaleOffset);

            ltcValue.a *= intensity * lightData.specularDimmer;

            // For clear coat we don't fetch specularFGD we can use directly the perfect fresnel coatIblF
            lighting.diffuse *= 1.0 - preLightData.coatIblF;
            lighting.specular = lerp(lighting.specular, ltcValue.rgb * ltcValue.a, preLightData.coatIblF);
        }

        // We need to multiply by the magnitude of the integral of the BRDF
        // ref: http://advances.realtimerendering.com/s2016/s2016_ltc_fresnel.pdf
        lighting.diffuse  *= lightData.color * preLightData.diffuseFGD;
        lighting.specular *= lightData.color;

        // ----- 4. Debug display -----

    #ifdef DEBUG_DISPLAY
        if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
        {
            ltcValue = EvaluateLTC_Area(isRectLight, center, right, up, halfLength, halfHeight,
                                        k_identity3x3, 1.0f,
                                        lightData.cookieMode, lightData.cookieScaleOffset);

            ltcValue.a *= intensity * lightData.diffuseDimmer;

            // Only lighting, not BSDF
            lighting.diffuse  = lightData.color * ltcValue.rgb * ltcValue.a;
            // Apply area light on Lambert then multiply by PI to cancel Lambert
            lighting.diffuse *= PI;
        }
    #endif
    }

#ifdef LIT_DISPLAY_REFERENCE_AREA
    }
#endif // LIT_DISPLAY_REFERENCE_AREA

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_SSLighting for screen space lighting
// ----------------------------------------------------------------------------

IndirectLighting EvaluateBSDF_ScreenSpaceReflection(PositionInputs posInput,
                                                    // Note: We use inout here with PreLightData to track an extra reflectionHierarchyWeight for the coat, but it should be avoided otherwise
                                                    inout PreLightData preLightData,
                                                    BSDFData       bsdfData,
                                                    inout float    reflectionHierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    // TODO: this texture is sparse (mostly black). Can we avoid reading every texel? How about using Hi-S?
    float4 ssrLighting = LOAD_TEXTURE2D_X(_SsrLightingTexture, posInput.positionSS);
    InversePreExposeSsrLighting(ssrLighting);

    // Apply the weight on the ssr contribution (if required)
    ApplyScreenSpaceReflectionWeight(ssrLighting);

    // TODO: we should multiply all indirect lighting by the FGD value only ONCE.

    // When this material has a clear coat, we should not be using specularFGD (used for bottom layer lobe) to modulate the coat traced light but coatIblF.
    // The condition for it is a combination of a material feature and the coat mask.

    // Without coat we use the SSR lighting (traced with coat parameters) and fallback on reflection probes (EvaluateBSDF_Env())
    // if there's still room in reflectionHierarchyWeight (ie if reflectionHierarchyWeight < 1 in the light loop).
    //
    // With the clear coat, the coat-traced SSR light can't be used to contribute for the bottom lobe in general and we still want to use the probe lighting
    // as a fallback. This requires us to return a reflectionHierarchyWeight < 1 (ie 0 if we didnt add any light for the bottom lobe yet) to the lightloop
    // regardless of what we consumed for the coat. In turn, in EvaluateBSDF_Env(), we need to track what weight we already used up for the coat lobe via the
    // current SSR callback to avoid double coat lighting contributions (which would otherwise come from both the SSR and from reflection probes called to
    // contribute mainly to the bottom lobe). We use a separate coatReflectionWeight for that which we hold in preLightData
    //
    // Note that the SSR with clear coat is a binary state, which means we should never enter the if condition if we don't have an active
    // clear coat (which is not guaranteed by the HasFlag condition in deferred mode in some cases). We then need to make sure that coatMask is actually non zero.
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT) && bsdfData.coatMask >= 0.0)
    {
        // We use the coat-traced light according to how similar the base lobe roughness is to the coat roughness
        // (we can assume the coat is always smoother):
        //
        // - The smoothness is equal to CLEAR_COAT_PERCEPTUAL_SMOOTHNESS  = (1-CLEAR_COAT_PERCEPTUAL_ROUGHNESS) ie to 0.9,
        //   We use the fact that the clear coat and base layer have the same roughness and use the SSR as the indirect specular signal.
        // - The smoothness is inferior to CLEAR_COAT_PERCEPTUAL_SMOOTHNESS - 0.2 (ie 0.7).
        //   We cannot use the SSR for the base layer.
        // - The smooothness is within <= 0.2 away (ie 0.7 to 0.9) of CLEAR_COAT_PERCEPTUAL_SMOOTHNESS, we lerp between the two behaviors.
        float coatSSRLightOnBottomLayerBlendingFactor = lerp(1.0, 0.0, saturate( (bsdfData.perceptualRoughness - CLEAR_COAT_PERCEPTUAL_ROUGHNESS) / 0.2 ) );

        // Use the coat-traced SSR lighting on the bottom layer lobe according to the above and also for the coat itself:
        lighting.specularReflected = ssrLighting.rgb * (coatSSRLightOnBottomLayerBlendingFactor * preLightData.specularFGD * (1.0 - preLightData.coatIblF) + preLightData.coatIblF);
        // Note: (1.0 - preLightData.coatIblF) is used like in EvaluateBSDF_ScreenspaceRefraction(), but IBL uses Sq().

        // Important: EvaluateBSDF_SSLighting() assumes it is the first light loop callback that contributes lighting,
        // we can thus directly set the reflectionHierarchyWeight instead of using UpdateLightingHierarchyWeights().

        // We initialize and keep track of the separate light reflection hierarchy weights but since only reflectionHierarchyWeight is known to the light loop,
        // normally a min() of both should be returned, but here, we know the coat "consumes" at least as much than the bottom lobe, so the coatReflectionWeight
        // dont interfere with the reflectionHierarchyWeight value returned:
        reflectionHierarchyWeight = ssrLighting.a * coatSSRLightOnBottomLayerBlendingFactor;
        preLightData.coatReflectionWeight = ssrLighting.a;
    }
    else
    {
        // Set the default weight value
        reflectionHierarchyWeight = ssrLighting.a;
        lighting.specularReflected = ssrLighting.rgb * preLightData.specularFGD;
    }

    return lighting;
}

IndirectLighting EvaluateBSDF_ScreenspaceRefraction(LightLoopContext lightLoopContext,
                                                    float3 V, PositionInputs posInput,
                                                    PreLightData preLightData, BSDFData bsdfData,
                                                    EnvLightData envLightData,
                                                    inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

#if HAS_REFRACTION
    // Refraction process:
    //  1. Depending on the shape model, we calculate the refracted point in world space and the optical depth
    //  2. We calculate the screen space position of the refracted point
    //  3. If this point is available (ie: in color buffer and point is not in front of the object)
    //    a. Get the corresponding color depending on the roughness from the gaussian pyramid of the color buffer
    //    b. Multiply by the transmittance for absorption (depends on the optical depth)

    // Proxy raycasting
    ScreenSpaceProxyRaycastInput ssRayInput;
    ZERO_INITIALIZE(ScreenSpaceProxyRaycastInput, ssRayInput);

    ssRayInput.rayOriginWS = preLightData.transparentPositionWS;
    ssRayInput.rayDirWS = preLightData.transparentRefractV;
    ssRayInput.proxyData = envLightData;

    ScreenSpaceRayHit hit;
    ZERO_INITIALIZE(ScreenSpaceRayHit, hit);
    bool hitSuccessful = false;
    float hitWeight = 1;
    hitSuccessful = ScreenSpaceProxyRaycastRefraction(ssRayInput, hit);

    if (!hitSuccessful)
        return lighting;

    // Resolve weight and color

    // Fade pixels near the texture buffers' borders
    float weight = EdgeOfScreenFade(hit.positionNDC, _SSRefractionInvScreenWeightDistance) * hitWeight;

    // Exit if texel is discarded
    if (weight == 0)
        // Do nothing and don't update the hierarchy weight so we can fall back on refraction probe
        return lighting;

    // This is an empirically set hack/modifier to reduce haloes of objects visible in the refraction.
    float refractionOffsetMultiplier = max(0.0f, 1.0f - preLightData.transparentSSMipLevel * 0.08f);

    // using LoadCameraDepth() here instead of hit.hitLinearDepth allow to fix an issue with VR single path instancing
    // as it use the macro LOAD_TEXTURE2D_X_LOD
    float hitDeviceDepth = LoadCameraDepth(hit.positionSS);
    float hitLinearDepth = LinearEyeDepth(hitDeviceDepth, _ZBufferParams);

    // If the hit object is in front of the refracting object, we use posInput.positionNDC to sample the color pyramid
    // This is equivalent of setting samplingPositionNDC = posInput.positionNDC when hitLinearDepth <= posInput.linearDepth
    refractionOffsetMultiplier *= (hitLinearDepth > posInput.linearDepth);

    float2 samplingPositionNDC = lerp(posInput.positionNDC, hit.positionNDC, refractionOffsetMultiplier);
    float2 samplingUV = samplingPositionNDC * _RTHandleScaleHistory.xy;
    float mipLevel = preLightData.transparentSSMipLevel;

    // Clamp to avoid potential leaks around the edges when the dynamic resolution is set to low and the smoothness too.
    float2 diffLimit = _ColorPyramidUvScaleAndLimitCurrentFrame.xy - _ColorPyramidUvScaleAndLimitCurrentFrame.zw;
    float2 diffLimitMipAdjusted = diffLimit * pow(2.0,2.0 + ceil(abs(mipLevel)));
    float2 limit = _ColorPyramidUvScaleAndLimitCurrentFrame.xy - diffLimitMipAdjusted;

    samplingUV.xy = min(samplingUV.xy, limit);

    float3 preLD = SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, s_trilinear_clamp_sampler, samplingUV, mipLevel).rgb;

    // Inverse pre-exposure
    preLD *= GetInverseCurrentExposureMultiplier();

    // We use specularFGD as an approximation of the fresnel effect (that also handle smoothness)
    float3 F = preLightData.specularFGD;
    lighting.specularTransmitted = (1.0 - F) * preLD.rgb * preLightData.transparentTransmittance * weight;

    UpdateLightingHierarchyWeights(hierarchyWeight, weight); // Shouldn't be needed, but safer in case we decide to change hierarchy priority

#else // HAS_REFRACTION
    // No refraction, no need to go further
    hierarchyWeight = 1.0;
#endif

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------
// _preIntegratedFGD and _CubemapLD are unique for each BRDF
IndirectLighting EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    inout PreLightData preLightData, // inout, see preLightData.coatReflectionWeight
                                    EnvLightData lightData, BSDFData bsdfData,
                                    int influenceShapeType, int GPUImageBasedLightingType,
                                    inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);
#if !HAS_REFRACTION
    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION)
        return lighting;
#endif

    float3 envLighting;
    float3 positionWS = posInput.positionWS;
    float weight = 1.0;

#ifdef LIT_DISPLAY_REFERENCE_IBL

    envLighting = IntegrateSpecularGGXIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);

    // TODO: Do refraction reference (is it even possible ?)
    // TODO: handle clear coat


//    #ifdef USE_DIFFUSE_LAMBERT_BRDF
//    envLighting += IntegrateLambertIBLRef(lightData, V, bsdfData);
//    #else
//    envLighting += IntegrateDisneyDiffuseIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);
//    #endif

#else

    float3 R = preLightData.iblR;

#if HAS_REFRACTION
    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION)
    {
        positionWS = preLightData.transparentPositionWS;
        R = preLightData.transparentRefractV;
    }
    else
#endif
    {
        if (!IsEnvIndexTexture2D(lightData.envIndex)) // ENVCACHETYPE_CUBEMAP
        {
            R = GetSpecularDominantDir(bsdfData.normalWS, R, preLightData.iblPerceptualRoughness, ClampNdotV(preLightData.NdotV));
            // When we are rough, we tend to see outward shifting of the reflection when at the boundary of the projection volume
            // Also it appear like more sharp. To avoid these artifact and at the same time get better match to reference we lerp to original unmodified reflection.
            // Formula is empirical.
            float roughness = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness);
            R = lerp(R, preLightData.iblR, saturate(smoothstep(0, 1, roughness * roughness)));
        }
    }

    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    float intersectionDistance = EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, R, weight);

    // Don't do clear coating for refraction
    float3 coatR = preLightData.coatIblR;
    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION && HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        float unusedWeight = 0.0;
        EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, coatR, unusedWeight);
    }

    float3 F = preLightData.specularFGD;

    float4 preLD = SampleEnvWithDistanceBaseRoughness(lightLoopContext, posInput, lightData, R, preLightData.iblPerceptualRoughness, intersectionDistance);
    weight *= preLD.a; // Used by planar reflection to discard pixel

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
    {
        envLighting = F * preLD.rgb;

        // Note: we have the same EnvIntersection weight used for the coat, but NOT the same headroom left to be used in the
        // hierarchy, so we saved the intersection weight here:
        float coatWeight = weight;

        // Apply the main lobe weight and update main reflection hierarchyWeight:
        UpdateLightingHierarchyWeights(hierarchyWeight, weight);
        envLighting *= weight;

        // Evaluate the Clear Coat component if needed
        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
        {
            // No correction needed for coatR as it is smooth
            // Note: coat F is scalar as it is a dieletric
            envLighting *= Sq(1.0 - preLightData.coatIblF);

            // Evaluate the Clear Coat color
            float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, coatR, 0.0, lightData.rangeCompressionFactorCompensation, posInput.positionNDC);

            // We adjust the EnvIntersection weight according to "headroom" left < 1 in the coatReflectionWeight and use that weight for the
            // additionnal (to SSR) coat contribution, if any:
            UpdateLightingHierarchyWeights(preLightData.coatReflectionWeight, coatWeight);
            // Note: PreLightData is made inout because of this update to preLightData.coatReflectionWeight.
            // This is because of an edge case when we mix eg two probes for the main hierarchyWeight, we will be called back again
            // with another probe after the first one has contributed, and we must thus keep track of the updated coatReflectionWeight too.

            envLighting += preLightData.coatIblF * preLD.rgb * coatWeight;

            // Can't attenuate diffuse lighting here, may try to apply something on bakeLighting in PostEvaluateBSDF
        }
    }
#if HAS_REFRACTION
    else
    {
        // No clear coat support with refraction

        // specular transmisted lighting is the remaining of the reflection (let's use this approx)
        // With refraction, we don't care about the clear coat value, only about the Fresnel, thus why we use 'envLighting ='
        envLighting = (1.0 - F) * preLD.rgb * preLightData.transparentTransmittance;

        // Apply the main lobe weight and update reflection hierarchyWeight:
        UpdateLightingHierarchyWeights(hierarchyWeight, weight);
        envLighting *= weight;
    }
#endif

#endif // LIT_DISPLAY_REFERENCE_IBL

    envLighting *= lightData.multiplier;

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
        lighting.specularReflected = envLighting;
#if HAS_REFRACTION
    else
        lighting.specularTransmitted = envLighting;
#endif

    return lighting;
}

//-----------------------------------------------------------------------------
// PostEvaluateBSDF
// ----------------------------------------------------------------------------

void PostEvaluateBSDF(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput,
                        PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, AggregateLighting lighting,
                        out LightLoopOutput lightLoopOutput)
{
    AmbientOcclusionFactor aoFactor;
    // Use GTAOMultiBounce approximation for ambient occlusion (allow to get a tint from the baseColor)
#if 0
    GetScreenSpaceAmbientOcclusion(posInput.positionSS, preLightData.NdotV, bsdfData.perceptualRoughness, bsdfData.ambientOcclusion, bsdfData.specularOcclusion, aoFactor);
#else
    GetScreenSpaceAmbientOcclusionMultibounce(posInput.positionSS, preLightData.NdotV, bsdfData.perceptualRoughness, bsdfData.ambientOcclusion, bsdfData.specularOcclusion, bsdfData.diffuseColor, bsdfData.fresnel0, aoFactor);
#endif

    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);

    // Subsurface scattering mode
    float3 modifiedDiffuseColor = GetModifiedDiffuseColorForSSS(bsdfData);

    // Apply the albedo to the direct diffuse lighting (only once). The indirect (baked)
    // diffuse lighting has already multiply the albedo in ModifyBakedDiffuseLighting().
    // Note: In deferred bakeDiffuseLighting also contain emissive and in this case emissiveColor is 0
    lightLoopOutput.diffuseLighting = modifiedDiffuseColor * lighting.direct.diffuse + builtinData.bakeDiffuseLighting + builtinData.emissiveColor;

    // If refraction is enable we use the transmittanceMask to lerp between current diffuse lighting and refraction value
    // Physically speaking, transmittanceMask should be 1, but for artistic reasons, we let the value vary
    //
    // Note we also transfer the refracted light (lighting.indirect.specularTransmitted) into diffuseLighting
    // since we know it won't be further processed: it is called at the end of the LightLoop(), but doing this
    // enables opacity to affect it (in ApplyBlendMode()) while the rest of specularLighting escapes it.
#if HAS_REFRACTION
    lightLoopOutput.diffuseLighting = lerp(lightLoopOutput.diffuseLighting, lighting.indirect.specularTransmitted, bsdfData.transmittanceMask * _EnableSSRefraction);
#endif

    lightLoopOutput.specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;
    // Rescale the GGX to account for the multiple scattering.
    lightLoopOutput.specularLighting *= 1.0 + bsdfData.fresnel0 * preLightData.energyCompensation;

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, lightLoopOutput);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
