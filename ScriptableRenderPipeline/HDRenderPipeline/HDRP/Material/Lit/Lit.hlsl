// SurfaceData is define in Lit.cs which generate Lit.cs.hlsl
#include "Lit.cs.hlsl"
#include "../SubsurfaceScattering/SubsurfaceScattering.hlsl"
#include "CoreRP/ShaderLibrary/VolumeRendering.hlsl"

//-----------------------------------------------------------------------------
// Texture and constant buffer declaration
//-----------------------------------------------------------------------------

// GBuffer texture declaration
TEXTURE2D(_GBufferTexture0);
TEXTURE2D(_GBufferTexture1);
TEXTURE2D(_GBufferTexture2);
TEXTURE2D(_GBufferTexture3);

// Rough refraction texture
// Color pyramid (width, height, lodcount, Unused)
TEXTURE2D(_GaussianPyramidColorTexture);
// Depth pyramid (width, height, lodcount, Unused)
TEXTURE2D(_PyramidDepthTexture);

CBUFFER_START(UnityGaussianPyramidParameters)
float4 _GaussianPyramidColorMipSize; // (x,y) = PyramidToScreenScale, z = lodCount
float4 _PyramidDepthMipSize;
CBUFFER_END

// Ambient occlusion texture
TEXTURE2D(_AmbientOcclusionTexture);

CBUFFER_START(UnityAmbientOcclusionParameters)
float4 _AmbientOcclusionParam; // xyz occlusion color, w directLightStrenght
CBUFFER_END

// Area light textures
// TODO: This one should be set into a constant Buffer at pass frequency (with _Screensize)
TEXTURE2D(_PreIntegratedFGD);
TEXTURE2D_ARRAY(_LtcData); // We pack the 3 Ltc data inside a texture array
#define LTC_GGX_MATRIX_INDEX 0 // RGBA
#define LTC_DISNEY_DIFFUSE_MATRIX_INDEX 1 // RGBA
#define LTC_MULTI_GGX_FRESNEL_DISNEY_DIFFUSE_INDEX 2 // RGB, A unused
#define LTC_LUT_SIZE   64
#define LTC_LUT_SCALE  ((LTC_LUT_SIZE - 1) * rcp(LTC_LUT_SIZE))
#define LTC_LUT_OFFSET (0.5 * rcp(LTC_LUT_SIZE))

//-----------------------------------------------------------------------------
// Definition
//-----------------------------------------------------------------------------

#define GBufferType0 float4
#define GBufferType1 float4
#define GBufferType2 float4
#define GBufferType3 float4

#define HAS_REFRACTION (defined(_REFRACTION_PLANE) || defined(_REFRACTION_SPHERE))

#define DEFAULT_SPECULAR_VALUE 0.04

// Enum for materialFeatureId (only use for encode/decode GBuffer)
#define GBUFFER_LIT_STANDARD         0
// we have not enough space (3bit) to store mat feature to have SSS and Transmission as bitmask, such why we have all variant
#define GBUFFER_LIT_SSS              1
#define GBUFFER_LIT_TRANSMISSION     2
#define GBUFFER_LIT_TRANSMISSION_SSS 3
#define GBUFFER_LIT_ANISOTROPIC      4
#define GBUFFER_LIT_IRIDESCENCE      5 // TODO

#define CLEAR_COAT_IOR 1.5
#define CLEAR_COAT_IETA (1.0 / CLEAR_COAT_IOR) // IETA is the inverse eta which is the ratio of IOR of two interface
#define CLEAR_COAT_F0 0.04 // IORToFresnel0(CLEAR_COAT_IOR)
#define CLEAR_COAT_ROUGHNESS 0.001
#define CLEAR_COAT_PERCEPTUAL_ROUGHNESS RoughnessToPerceptualRoughness(CLEAR_COAT_ROUGHNESS)

//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------

// Choose between Lambert diffuse and Disney diffuse (enable only one of them)
// #define LIT_DIFFUSE_LAMBERT_BRDF
#define LIT_USE_GGX_ENERGY_COMPENSATION

// Enable reference mode for IBL and area lights
// Both reference define below can be define only if LightLoop is present, else we get a compile error
#ifdef HAS_LIGHTLOOP
// #define LIT_DISPLAY_REFERENCE_AREA
// #define LIT_DISPLAY_REFERENCE_IBL
#endif

//-----------------------------------------------------------------------------
// Ligth and material classification for the deferred rendering path
// Configure what kind of combination is supported
//-----------------------------------------------------------------------------

// Lighting architecture and material are suppose to be decoupled files.
// However as we use material classification it is hard to be fully separated
// the dependecy is define in this include where there is shared define for material and lighting in case of deferred material.
// If a user do a lighting architecture without material classification, this can be remove
#include "../../Lighting/LightLoop/LightLoop.cs.hlsl"

// Combination need to be define in increasing "comlexity" order as define by FeatureFlagsToTileVariant
static const uint kFeatureVariantFlags[NUM_FEATURE_VARIANTS] =
{
    // Precomputed illumination (no dynamic lights) for all material types
    /*  0 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_ENV | MATERIAL_FEATURE_MASK_FLAGS,

    /*  1 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  2 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  3 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  4 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  5 */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIALFEATUREFLAGS_LIT_STANDARD,

    // Standard with SSS and Transmission
    /*  6 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  7 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  8 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /*  9 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 10 */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION | MATERIALFEATUREFLAGS_LIT_STANDARD,

    // Anisotropy
    /* 11 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_ANISOTROPY | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 12 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_ANISOTROPY | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 13 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_ANISOTROPY | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 14 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_ANISOTROPY | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 15 */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIALFEATUREFLAGS_LIT_ANISOTROPY | MATERIALFEATUREFLAGS_LIT_STANDARD,

    // Standard with clear coat
    /* 16 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 17 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 18 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 19 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 20 */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,

    // Standard with Iridescence
    /* 21 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 22 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 23 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 24 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 25 */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_STANDARD,

    /* 26 */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIAL_FEATURE_MASK_FLAGS, // Catch all case with MATERIAL_FEATURE_MASK_FLAGS is needed in case we disable material classification
};

// Additional bits set in 'bsdfData.materialFeatures' to save registers and simplify feature tracking.
#define MATERIAL_FEATURE_FLAGS_SSS_OUTPUT_SPLIT_LIGHTING         ((MATERIAL_FEATURE_MASK_FLAGS + 1) << 0)
#define MATERIAL_FEATURE_FLAGS_SSS_TEXTURING_MODE_OFFSET FastLog2((MATERIAL_FEATURE_MASK_FLAGS + 1) << 1) // 2 bits
#define MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_AUTO_THICKNESS  ((MATERIAL_FEATURE_MASK_FLAGS + 1) << 3)

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

#if HAS_REFRACTION
# include "CoreRP/ShaderLibrary/Refraction.hlsl"

# if defined(_REFRACTION_PLANE)
#  define REFRACTION_MODEL(V, posInputs, bsdfData) RefractionModelPlane(V, posInputs.positionWS, bsdfData.normalWS, bsdfData.ior, bsdfData.thickness)
# elif defined(_REFRACTION_SPHERE)
#  define REFRACTION_MODEL(V, posInputs, bsdfData) RefractionModelSphere(V, posInputs.positionWS, bsdfData.normalWS, bsdfData.ior, bsdfData.thickness)
# endif
#endif

float3 EstimateRaycast(float3 V, PositionInputs posInputs, float3 positionWS, float3 rayWS)
{
    // For all refraction approximation, to calculate the refracted point in world space,
    //   we approximate the scene as a plane (back plane) with normal -V at the depth hit point.
    //   (We avoid to raymarch the depth texture to get the refracted point.)

    uint2 depthSize = uint2(_PyramidDepthMipSize.xy);

    // Get the depth of the approximated back plane
    float pyramidDepth = LOAD_TEXTURE2D_LOD(_PyramidDepthTexture, posInputs.positionNDC * (depthSize >> 2), 2).r;
    float depth = LinearEyeDepth(pyramidDepth, _ZBufferParams);

    // Distance from point to the back plane
    float depthFromPositionInput = depth - posInputs.linearDepth;

    float offset = dot(-V, positionWS - posInputs.positionWS);
    float depthFromPosition = depthFromPositionInput - offset;

    float hitDistanceFromPosition = depthFromPosition / dot(-V, rayWS);

    return positionWS + rayWS * hitDistanceFromPosition;
}

// This method allows us to know at compile time what material features should be removed from the code by Tile (Indepenently of the value of material feature flag per pixel).
// This is only useful for classification during lighting, so it's not needed in EncodeIntoGBuffer and ConvertSurfaceDataToBSDFData (where we always know exactly what the material feature is)
bool HasFeatureFlag(uint featureFlags, uint flag)
{
    return ((featureFlags & flag) != 0);
}

float3 ComputeDiffuseColor(float3 baseColor, float metallic)
{
    return baseColor * (1.0 - metallic);
}

float3 ComputeFresnel0(float3 baseColor, float metallic, float dielectricF0)
{
    return lerp(dielectricF0.xxx, baseColor, metallic);
}

// Assume that bsdfData.diffusionProfile is init
void FillMaterialSSS(uint diffusionProfile, float subsurfaceMask, inout BSDFData bsdfData)
{
    bsdfData.diffusionProfile = diffusionProfile;
    bsdfData.fresnel0 = _TransmissionTintsAndFresnel0[diffusionProfile].a;
    bsdfData.subsurfaceMask = subsurfaceMask;
    bsdfData.materialFeatures |= MATERIAL_FEATURE_FLAGS_SSS_OUTPUT_SPLIT_LIGHTING;
    bsdfData.materialFeatures |= GetSubsurfaceScatteringTexturingMode(bsdfData.diffusionProfile) << MATERIAL_FEATURE_FLAGS_SSS_TEXTURING_MODE_OFFSET;
}

// Assume that bsdfData.diffusionProfile is init
void FillMaterialTransmission(uint diffusionProfile, float thickness, inout BSDFData bsdfData)
{
    bsdfData.diffusionProfile = diffusionProfile;
    bsdfData.fresnel0 = _TransmissionTintsAndFresnel0[diffusionProfile].a;

    bsdfData.thickness = _ThicknessRemaps[diffusionProfile].x + _ThicknessRemaps[diffusionProfile].y * thickness;

    // The difference between the thin and the regular (a.k.a. auto-thickness) modes is the following:
    // * in the thin object mode, we assume that the geometry is thin enough for us to safely share
    // the shadowing information between the front and the back faces;
    // * the thin mode uses baked (textured) thickness for all transmission calculations;
    // * the thin mode uses wrapped diffuse lighting for the NdotL;
    // * the auto-thickness mode uses the baked (textured) thickness to compute transmission from
    // indirect lighting and non-shadow-casting lights; for shadowed lights, it calculates
    // the thickness using the distance to the closest occluder sampled from the shadow map.
    // If the distance is large, it may indicate that the closest occluder is not the back face of
    // the current object. That's not a problem, since large thickness will result in low intensity.
    bool useThinObjectMode = IsBitSet(asuint(_TransmissionFlags), diffusionProfile);

    bsdfData.materialFeatures |= useThinObjectMode ? 0 : MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_AUTO_THICKNESS;

    // Compute transmittance using baked thickness here. It may be overridden for direct lighting
    // in the auto-thickness mode (but is always be used for indirect lighting).
#if SHADEROPTIONS_USE_DISNEY_SSS
    bsdfData.transmittance = ComputeTransmittanceDisney(_ShapeParams[diffusionProfile].rgb,
                                                        _TransmissionTintsAndFresnel0[diffusionProfile].rgb,
                                                        bsdfData.thickness);
#else
    bsdfData.transmittance = ComputeTransmittanceJimenez(_HalfRcpVariancesAndWeights[diffusionProfile][0].rgb,
                                                         _HalfRcpVariancesAndWeights[diffusionProfile][0].a,
                                                         _HalfRcpVariancesAndWeights[diffusionProfile][1].rgb,
                                                         _HalfRcpVariancesAndWeights[diffusionProfile][1].a,
                                                         _TransmissionTintsAndFresnel0[diffusionProfile].rgb,
                                                         bsdfData.thickness);
#endif
}

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

// For image based lighting, a part of the BSDF is pre-integrated.
// This is done both for specular and diffuse (in case of DisneyDiffuse)
void GetPreIntegratedFGD(float NdotV, float perceptualRoughness, float3 fresnel0, out float3 specularFGD, out float diffuseFGD, out float reflectivity)
{
    // Pre-integrate GGX FGD
    // Integral{BSDF * <N,L> dw} =
    // Integral{(F0 + (1 - F0) * (1 - <V,H>)^5) * (BSDF / F) * <N,L> dw} =
    // (1 - F0) * Integral{(1 - <V,H>)^5 * (BSDF / F) * <N,L> dw} + F0 * Integral{(BSDF / F) * <N,L> dw}=
    // (1 - F0) * x + F0 * y = lerp(x, y, F0)
    // Pre integrate DisneyDiffuse FGD:
    // z = DisneyDiffuse
    float3 preFGD = SAMPLE_TEXTURE2D_LOD(_PreIntegratedFGD, s_linear_clamp_sampler, float2(NdotV, perceptualRoughness), 0).xyz;

    specularFGD = lerp(preFGD.xxx, preFGD.yyy, fresnel0);

#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    diffuseFGD = 1.0;
#else
    // Remap from the [0, 1] to the [0.5, 1.5] range.
    diffuseFGD = preFGD.z + 0.5;
#endif

    reflectivity = preFGD.y;
}

// This function is use to help with debugging and must be implemented by any lit material
// Implementer must take into account what are the current override component and
// adjust SurfaceData properties accordingdly
void ApplyDebugToSurfaceData(float3x3 worldToTangent, inout SurfaceData surfaceData)
{
#ifdef DEBUG_DISPLAY
    // Override value if requested by user
    // this can be use also in case of debug lighting mode like diffuse only
    bool overrideAlbedo = _DebugLightingAlbedo.x != 0.0;
    bool overrideSmoothness = _DebugLightingSmoothness.x != 0.0;
    bool overrideNormal = _DebugLightingNormal.x != 0.0;

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
        surfaceData.normalWS = worldToTangent[2];
    }
#endif
}

SSSData ConvertSurfaceDataToSSSData(SurfaceData surfaceData)
{
    SSSData sssData;

    sssData.diffuseColor = surfaceData.baseColor;
    sssData.subsurfaceMask = surfaceData.subsurfaceMask;
    sssData.diffusionProfile = surfaceData.diffusionProfile;

    return sssData;
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    // IMPORTANT: In case of foward or gbuffer pass all enable flags are statically know at compile time, so the compiler can do compile time optimization
    bsdfData.materialFeatures    = surfaceData.materialFeatures;

    // Standard material
    bsdfData.specularOcclusion   = surfaceData.specularOcclusion;
    bsdfData.normalWS            = surfaceData.normalWS;
    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);

    // There is no metallic with SSS and specular color mode
    float metallic = HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR | MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION) ? 0.0 : surfaceData.metallic;

    bsdfData.diffuseColor = ComputeDiffuseColor(surfaceData.baseColor, metallic);
    bsdfData.fresnel0     = HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR) ? surfaceData.specularColor : ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, DEFAULT_SPECULAR_VALUE);

    // Note: we have ZERO_INITIALIZE the struct so bsdfData.anisotropy == 0.0
    // Note: DIFFUSION_PROFILE_NEUTRAL_ID is 0

    // In forward everything is statically know and we could theorically cumulate all the material features. So the code reflect it.
    // However in practice we keep parity between deferred and forward, so we should constrain the various features.
    // The UI is in charge of setuping the constrain, not the code. So if users is forward only and want unleash power, it is easy to unleash by some UI change

    if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING))
    {
        // Assign profile id and overwrite fresnel0
        FillMaterialSSS(surfaceData.diffusionProfile, surfaceData.subsurfaceMask, bsdfData);
    }

    if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        // Assign profile id and overwrite fresnel0
        FillMaterialTransmission(surfaceData.diffusionProfile, surfaceData.thickness, bsdfData);
    }

    if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
    {
        FillMaterialAnisotropy(surfaceData.anisotropy, surfaceData.tangentWS, cross(surfaceData.normalWS, surfaceData.tangentWS), bsdfData);
    }

    if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_IRIDESCENCE))
    {
        FillMaterialIridescence(surfaceData.iridescenceMask, surfaceData.iridescenceThickness, bsdfData);
    }

    if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        // Modify perceptualRoughness
        FillMaterialClearCoatData(surfaceData.coatMask, bsdfData);
    }

    // roughnessT and roughnessB are clamped, and are meant to be used with punctual and directional lights.
    // perceptualRoughness is not clamped, and is meant to be used for IBL.
    // perceptualRoughness can be modify by FillMaterialClearCoatData, so ConvertAnisotropyToClampRoughness must be call after
    ConvertAnisotropyToClampRoughness(bsdfData.perceptualRoughness, bsdfData.anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);

#if HAS_REFRACTION
    // Note: Reuse thickness of transmission's property set
    FillMaterialTransparencyData( surfaceData.baseColor, surfaceData.metallic, surfaceData.ior, surfaceData.transmittanceColor, surfaceData.atDistance,
                                    surfaceData.thickness, surfaceData.transmittanceMask, bsdfData);
#endif

    return bsdfData;
}

//-----------------------------------------------------------------------------
// conversion function for deferred
//-----------------------------------------------------------------------------

// GBuffer layout.
// GBuffer2 and GBuffer0.a interpretation depends on material feature enabled

//GBuffer0  	RGBA8 sRGB  Gbuffer0 encode baseColor and so is sRGB to save precision. Alpha is not affected.
//GBuffer1  	RGBA8
//GBuffer2  	RGBA8
//GBuffer3  	RGBA8


//FeatureName   Standard
//GBuffer0  	baseColor.r,    baseColor.g,    baseColor.b,    specularOcclusion
//GBuffer1  	normal.xy (1212),   perceptualRoughness
//GBuffer2  	f0.r,   f0.g,   f0.b,   featureID(3) / coatMask(5)
//GBuffer3  	bakedDiffuseLighting.rgb

//FeatureName   Subsurface Scattering + Transmission
//GBuffer0  	baseColor.r,    baseColor.g,    baseColor.b,   diffusionProfile(4) / subsurfaceMask(4)
//GBuffer1  	normal.xy (1212),   perceptualRoughness
//GBuffer2  	specularOcclusion,  thickness,  diffusionProfile(4) / subsurfaceMask(4), featureID(3) / coatMask(5)
//GBuffer3  	bakedDiffuseLighting.rgb

//FeatureName   Anisotropic
//GBuffer0  	baseColor.r,    baseColor.g,    baseColor.b,    specularOcclusion
//GBuffer1  	normal.xy (1212),   perceptualRoughness
//GBuffer2  	anisotropy, tangent.x,  tangent.y(3) / metallic(5), featureID(3) / coatMask(5)
//GBuffer3  	bakedDiffuseLighting.rgb

//FeatureName   Irridescence
//GBuffer0  	baseColor.r,    baseColor.g,    baseColor.b,    specularOcclusion
//GBuffer1  	normal.xy (1212),   perceptualRoughness
//GBuffer2  	IOR,    thickness,  unused(3bit) / metallic(5), featureID(3) / coatMask(5)
//GBuffer3  	bakedDiffuseLighting.rgb

// Note:
// For standard we have chose to always encode fresnel0. Even when we use metal/baseColor parametrization. This avoid
// compiler optimization problem that was using VGPR to deal with the various combination of metal non metal.

// For SSS, we move diffusionProfile(4) / subsurfaceMask(4) in GBuffer0.a so the forward SSS code only need to write into one RT
// and the SSS postprocess only need to read one RT
// We duplicate diffusionProfile / subsurfaceMask in GBuffer2.b so the compiler don't need to read the GBuffer0 before PostEvaluateBSDF
// The lighting code have been adapted to only apply diffuseColor at the end.
// This save VGPR as we don' need to keep the GBuffer0 value in register.

// The layout is also design to only require one RT for the material classification. All the material feature flags are deduced from GBuffer2.

// Encode SurfaceData (BSDF parameters) into GBuffer
// Must be in sync with RT declared in HDRenderPipeline.cs ::Rebuild
void EncodeIntoGBuffer( SurfaceData surfaceData,
                        float3 bakeDiffuseLighting,
                        uint2 positionSS,
                        out GBufferType0 outGBuffer0,
                        out GBufferType1 outGBuffer1,
                        out GBufferType2 outGBuffer2,
                        out GBufferType3 outGBuffer3
                        )
{
    // RT0 - 8:8:8:8 sRGB
    // Warning: the contents are later overwritten for Standard and SSS!
    outGBuffer0 = float4(surfaceData.baseColor, surfaceData.specularOcclusion);

    // RT1 - 8:8:8:8
    // Our tangent encoding is based on our normal.
    // With octahedral quad packing we get an artifact for reconstructed tangent at the center of this quad. We use rect packing instead to avoid it.
    float2 octNormalWS = PackNormalOctRectEncode(surfaceData.normalWS);
    float3 packNormalWS = PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5));
    // We store perceptualRoughness instead of roughness because it is perceptually linear.
    outGBuffer1 = float4(packNormalWS, PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness));

    // RT2 - 8:8:8:8
    uint materialFeatureId;

    if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
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
        EncodeIntoSSSBuffer(ConvertSurfaceDataToSSSData(surfaceData), positionSS, outGBuffer0);

        // We duplicate the alpha channel of the G-Buffer 0 (for diffusion profile).
        // It allows us to delay reading the G-Buffer 0 until the end of the deferred lighting shader.
        outGBuffer2.rgb = float3(surfaceData.specularOcclusion, surfaceData.thickness, outGBuffer0.a);
    }
    else if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
    {
        materialFeatureId = GBUFFER_LIT_ANISOTROPIC;

        // Reconstruct the default tangent frame.
        float3x3 frame = GetLocalFrame(surfaceData.normalWS);

        // Compute the rotation angle of the actual tangent frame with respect to the default one.
        float sinFrame = dot(surfaceData.tangentWS, frame[1]);
        float cosFrame = dot(surfaceData.tangentWS, frame[0]);
        uint  storeSin = abs(sinFrame) < abs(cosFrame) ? 4 : 0;
        uint  quadrant = ((sinFrame < 0) ? 1 : 0) | ((cosFrame < 0) ? 2 : 0);

        // sin [and cos] are approximately linear up to [after] 45 degrees.
        float sinOrCos = min(abs(sinFrame), abs(cosFrame)) * sqrt(2);

        outGBuffer2.rgb = float3(surfaceData.anisotropy * 0.5 + 0.5,
                                 sinOrCos,
                                 PackFloatInt8bit(surfaceData.metallic, storeSin | quadrant, 8));
    }
    else if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_IRIDESCENCE))
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

        if (!HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR))
        {
            // Convert from the metallic parametrization.
            diffuseColor = ComputeDiffuseColor(surfaceData.baseColor, surfaceData.metallic);
            fresnel0     = ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, DEFAULT_SPECULAR_VALUE);
        }

        outGBuffer0.rgb = diffuseColor;               // sRGB RT
        // outGBuffer2 is not sRGB, so use a fast encode/decode sRGB to keep precision
        outGBuffer2.rgb = FastLinearToSRGB(fresnel0); // TODO: optimize
    }

    // Ensure that surfaceData.coatMask is 0 if the feature is not enabled
    float coatMask = HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT) ? surfaceData.coatMask : 0.0;
    // Note: no need to store MATERIALFEATUREFLAGS_LIT_STANDARD, always present
    outGBuffer2.a  = PackFloatInt8bit(coatMask, materialFeatureId, 8);

    // RT3 - 11f:11f:10f
    outGBuffer3 = float4(bakeDiffuseLighting, 0.0);
}

// Fills the BSDFData. Also returns the (per-pixel) material feature flags inferred
// from the contents of the G-buffer, which can be used by the feature classification system.
// Note that return type is not part of the MACRO DECODE_FROM_GBUFFER, so it is safe to use return value for our need
// 'tileFeatureFlags' are compile-time flags provided by the feature classification system.
// If you're not using the feature classification system, pass UINT_MAX.
// Also, see comment in TileVariantToFeatureFlags. When we are the worse case (i.e last variant), we read the featureflags
// from the structured buffer use to generate the indirect draw call. It allow to not go through all branch and the branch is scalar (not VGPR)
uint DecodeFromGBuffer(uint2 positionSS, uint tileFeatureFlags, out BSDFData bsdfData, out float3 bakeDiffuseLighting)
{
    // Note: we have ZERO_INITIALIZE the struct, so bsdfData.diffusionProfile == DIFFUSION_PROFILE_NEUTRAL_ID,
    // bsdfData.anisotropy == 0, bsdfData.subsurfaceMask == 0, etc...
    ZERO_INITIALIZE(BSDFData, bsdfData);

    // Isolate material features.
    tileFeatureFlags &= MATERIAL_FEATURE_MASK_FLAGS;

    GBufferType0 inGBuffer0 = LOAD_TEXTURE2D(_GBufferTexture0, positionSS);
    GBufferType1 inGBuffer1 = LOAD_TEXTURE2D(_GBufferTexture1, positionSS);
    GBufferType2 inGBuffer2 = LOAD_TEXTURE2D(_GBufferTexture2, positionSS);
    GBufferType3 inGBuffer3 = LOAD_TEXTURE2D(_GBufferTexture3, positionSS);

    // Material classification only uses the G-Buffer 2.
    float coatMask;
    uint materialFeatureId;
    UnpackFloatInt8bit(inGBuffer2.a, 8, coatMask, materialFeatureId);

    uint pixelFeatureFlags    = MATERIALFEATUREFLAGS_LIT_STANDARD; // Only sky/background do not have the Standard flag.
    bool pixelHasSubsurface   = materialFeatureId == GBUFFER_LIT_TRANSMISSION_SSS || materialFeatureId == GBUFFER_LIT_SSS;
    bool pixelHasTransmission = materialFeatureId == GBUFFER_LIT_TRANSMISSION_SSS || materialFeatureId == GBUFFER_LIT_TRANSMISSION;
    bool pixelHasAnisotropy   = materialFeatureId == GBUFFER_LIT_ANISOTROPIC;
    bool pixelHasIridescence  = materialFeatureId == GBUFFER_LIT_IRIDESCENCE;
    bool pixelHasClearCoat    = coatMask > 0.0;

    // Disable pixel features disabled by the tile.
    pixelFeatureFlags |= tileFeatureFlags & (pixelHasSubsurface   ? MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING : 0);
    pixelFeatureFlags |= tileFeatureFlags & (pixelHasTransmission ? MATERIALFEATUREFLAGS_LIT_TRANSMISSION          : 0);
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

    bsdfData.specularOcclusion   = inGBuffer0.a; // Later possibly overwritten by SSS
    bsdfData.perceptualRoughness = inGBuffer1.a;

    float3 packNormalWS = inGBuffer1.rgb;
    float2 octNormalWS = Unpack888ToFloat2(packNormalWS);
    bsdfData.normalWS = UnpackNormalOctRectEncode(octNormalWS * 2.0 - 1.0);

    bakeDiffuseLighting = inGBuffer3.rgb;

    // Decompress feature-specific data from the G-Buffer.
    bool pixelHasMetallic = HasFeatureFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_ANISOTROPY | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE);

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

    if (HasFeatureFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        SSSData sssData;

        // We don't need to do this call, see comment below
        // DecodeFromSSSBuffer(inGBuffer0, positionSS, sssData);

        // Overwrite the diffusion profile/subsurfaceMask extracted by DecodeFromSSSBuffer().
        // We must do this so the compiler can optimize away the read from the G-Buffer 0 to the very end (in PostEvaluateBSDF)
        // Note that we don't use sssData.subsurfaceMask here. But it is still assign so we can have the information in the
        // material debug view + If we require it in the future.
        UnpackFloatInt8bit(inGBuffer2.b, 16, sssData.subsurfaceMask, sssData.diffusionProfile);

        // Reminder: when using SSS we exchange specular occlusion and subsurfaceMask/profileID
        bsdfData.specularOcclusion = inGBuffer2.r;

        // Note: both function assign profile and overwrite fresnel0 (both SSS and Transmission)
        // in case one feature is enabled and not the other.

        // The neutral value of subsurfaceMask is 0 (handled by ZERO_INITIALIZE).
        if (HasFeatureFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING))
        {
            FillMaterialSSS(sssData.diffusionProfile, sssData.subsurfaceMask, bsdfData);
        }

        // The neutral value of thickness and transmittance is 0 (handled by ZERO_INITIALIZE).
        if (HasFeatureFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
        {
            FillMaterialTransmission(sssData.diffusionProfile, inGBuffer2.g, bsdfData);
        }
    }

    // Special handling for anisotropy: When anisotropy is present in a tile, the whole tile will use anisotropy to avoid divergent evaluation of GGX that increase the cost
    // Note that it mean that when we have the worse case, we always use Anisotropy and shader like deferred.shader are always the worst case (but only used for debugging)
    if (HasFeatureFlag(tileFeatureFlags, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
    {
        float anisotropy = 0;
        float3x3 frame = GetLocalFrame(bsdfData.normalWS);

        if (HasFeatureFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
        {
            anisotropy = inGBuffer2.r * 2.0 - 1.0;

            float unused;
            uint tangentFlags;
            UnpackFloatInt8bit(inGBuffer2.b, 8, unused, tangentFlags);

            // Get the rotation angle of the actual tangent frame with respect to the default one.
            uint  quadrant = tangentFlags;
            uint  storeSin = tangentFlags & 4;
            float sinOrCos = inGBuffer2.g * rsqrt(2);
            float cosOrSin = sqrt(1 - sinOrCos * sinOrCos);
            float sinFrame = storeSin ? sinOrCos : cosOrSin;
            float cosFrame = storeSin ? cosOrSin : sinOrCos;
                  sinFrame = (quadrant & 1) ? -sinFrame : sinFrame;
                  cosFrame = (quadrant & 2) ? -cosFrame : cosFrame;

            // Rotate the reconstructed tangent around the normal.
            frame[0] = sinFrame * frame[1] + cosFrame * frame[0];
            frame[1] = cross(frame[2], frame[0]);
        }

        FillMaterialAnisotropy(anisotropy, frame[0], frame[1], bsdfData);
    }

    // The neutral value of iridescenceMask is 0 (handled by ZERO_INITIALIZE).
    if (HasFeatureFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_IRIDESCENCE))
    {
        FillMaterialIridescence(inGBuffer2.r, inGBuffer2.g, bsdfData);
    }

    // The neutral value of coatMask is 0 (handled by ZERO_INITIALIZE).
    if (HasFeatureFlag(pixelFeatureFlags, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        // Modify perceptualRoughness
        FillMaterialClearCoatData(coatMask, bsdfData);
    }

    // Note: the full code below (for both roughness) only execute when we have enableAnisotropy == true, otherwise as we only use roughnessT compiler will optimize out
    // Mean that in the worst case we always execute it.

    // roughnessT and roughnessB are clamped, and are meant to be used with punctual and directional lights.
    // perceptualRoughness is not clamped, and is meant to be used for IBL.
    // perceptualRoughness can be modify by FillMaterialClearCoatData, so ConvertAnisotropyToClampRoughness must be call after
    ConvertAnisotropyToClampRoughness(bsdfData.perceptualRoughness, bsdfData.anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);

    return pixelFeatureFlags;
}

// Function call from the material classification compute shader
uint MaterialFeatureFlagsFromGBuffer(uint2 positionSS)
{
    BSDFData bsdfData;
    float3 unused;
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
        // Convert to view space
        result = TransformWorldToViewDir(surfaceData.normalWS) * 0.5 + 0.5;
        break;
    case DEBUGVIEW_LIT_SURFACEDATA_MATERIAL_FEATURES:
        result = (surfaceData.materialFeatures.xxx) / 255.0; // Aloow to read with color picker debug mode
        break;
    case DEBUGVIEW_LIT_SURFACEDATA_INDEX_OF_REFRACTION:
        result = saturate((surfaceData.ior - 1.0) / 1.5).xxx;
        break;
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
        result = TransformWorldToViewDir(bsdfData.normalWS) * 0.5 + 0.5;
        break;
    case DEBUGVIEW_LIT_BSDFDATA_MATERIAL_FEATURES:
        result = (bsdfData.materialFeatures.xxx) / 255.0; // Aloow to read with color picker debug mode
        break;
    case DEBUGVIEW_LIT_BSDFDATA_IOR:
        result = saturate((bsdfData.ior - 1.0) / 1.5).xxx;
        break;
    }
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
    float3 iblR;                     // Dominant specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness;

    float3 specularFGD;              // Store preconvoled BRDF for both specular and diffuse
    float  diffuseFGD;

    // Area lights (17 VGPRs)
    // TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.
    float3x3 orthoBasisViewNormal;   // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
    float3x3 ltcTransformDiffuse;    // Inverse transformation for Lambertian or Disney Diffuse        (4x VGPRs)
    float3x3 ltcTransformSpecular;   // Inverse transformation for GGX                                 (4x VGPRs)
    float    ltcMagnitudeDiffuse;
    float3   ltcMagnitudeFresnel;

    // Clear coat
    float    coatPartLambdaV;
    float3   coatIblR;
    float    coatIblF;               // Fresnel term for view vector
    float3x3 ltcTransformCoat;       // Inverse transformation for GGX                                 (4x VGPRs)
    float    ltcMagnitudeCoatFresnel;

    // Refraction
    float3 transparentRefractV;      // refracted view vector after exiting the shape
    float3 transparentPositionWS;    // start of the refracted ray after exiting the shape
    float3 transparentTransmittance; // transmittance due to absorption
    float transparentSSMipLevel;     // mip level of the screen space gaussian pyramid for rough refraction
};

PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    ZERO_INITIALIZE(PreLightData, preLightData);

    float3 N = bsdfData.normalWS;
    preLightData.NdotV = dot(N, V);
    preLightData.iblPerceptualRoughness = bsdfData.perceptualRoughness;

    float NdotV = ClampNdotV(preLightData.NdotV);

    // We modify the bsdfData.fresnel0 here for iridescence
    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_IRIDESCENCE))
    {
        float viewAngle = NdotV;
        float topIor = 1.0; // Default is air
        if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
        {
            topIor = lerp(1.0, CLEAR_COAT_IOR, bsdfData.coatMask);
            // HACK: Use the reflected direction to specify the Fresnel coefficient for pre-convolved envmaps
            viewAngle = sqrt(1.0 + Sq(1.0 / topIor) * (Sq(dot(bsdfData.normalWS, V)) - 1.0));
        }

        if (bsdfData.iridescenceMask > 0.0)
        {
            bsdfData.fresnel0 = lerp(bsdfData.fresnel0, EvalIridescence(topIor, viewAngle, bsdfData.iridescenceThickness, bsdfData.fresnel0), bsdfData.iridescenceMask);
        }
    }

    // We modify the bsdfData.fresnel0 here for clearCoat
    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        // Fresnel0 is deduced from interface between air and material (Assume to be 1.5 in Unity, or a metal).
        // but here we go from clear coat (1.5) to material, we need to update fresnel0
        // Note: Schlick is a poor approximation of Fresnel when ieta is 1 (1.5 / 1.5), schlick target 1.4 to 2.2 IOR.
        bsdfData.fresnel0 = lerp(bsdfData.fresnel0, ConvertF0ForAirInterfaceToF0ForClearCoat15(bsdfData.fresnel0), bsdfData.coatMask);

        preLightData.coatPartLambdaV = GetSmithJointGGXPartLambdaV(NdotV, CLEAR_COAT_ROUGHNESS);
        preLightData.coatIblR = reflect(-V, N);
        preLightData.coatIblF = F_Schlick(CLEAR_COAT_F0, NdotV) * bsdfData.coatMask;
    }

    float3 iblN, iblR;

    // We avoid divergent evaluation of the GGX, as that nearly doubles the cost.
    // If the tile has anisotropy, all the pixels within the tile are evaluated as anisotropic.
    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
    {
        float TdotV = dot(bsdfData.tangentWS,   V);
        float BdotV = dot(bsdfData.bitangentWS, V);

        preLightData.partLambdaV = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV, bsdfData.roughnessT, bsdfData.roughnessB);

        // For GGX aniso and IBL we have done an empirical (eye balled) approximation compare to the reference.
        // We use a single fetch, and we stretch the normal to use based on various criteria.
        // result are far away from the reference but better than nothing
        // For positive anisotropy values: tangent = highlight stretch (anisotropy) direction, bitangent = grain (brush) direction.
        float3 grainDirWS = (bsdfData.anisotropy >= 0.0) ? bsdfData.bitangentWS : bsdfData.tangentWS;
        // Reduce stretching for (perceptualRoughness < 0.2).
        float stretch = abs(bsdfData.anisotropy) * saturate(5 * preLightData.iblPerceptualRoughness);
        // NOTE: If we follow the theory we should use the modified normal for the different calculation implying a normal (like NdotV) and use 'anisoIblNormalWS'
        // into function like GetSpecularDominantDir(). However modified normal is just a hack. The goal is just to stretch a cubemap, no accuracy here.
        // With this in mind and for performance reasons we chose to only use modified normal to calculate R.
        iblN = GetAnisotropicModifiedNormal(grainDirWS, N, V, stretch);
    }
    else
    {
        preLightData.partLambdaV = GetSmithJointGGXPartLambdaV(NdotV, bsdfData.roughnessT);
        iblN = N;
    }

    // IBL

    // Handle IBL +  multiscattering
    float reflectivity;
    GetPreIntegratedFGD(NdotV, preLightData.iblPerceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD, reflectivity);

    iblR = reflect(-V, iblN);
    // This is a ad-hoc tweak to better match reference of anisotropic GGX.
    // TODO: We need a better hack.
    preLightData.iblPerceptualRoughness *= saturate(1.2 - abs(bsdfData.anisotropy));
    // Corretion of reflected direction for better handling of rough material
    preLightData.iblR = GetSpecularDominantDir(N, iblR, preLightData.iblPerceptualRoughness, NdotV);

#ifdef LIT_USE_GGX_ENERGY_COMPENSATION
    // Ref: Practical multiple scattering compensation for microfacet models.
    // We only apply the formulation for metals.
    // For dielectrics, the change of reflectance is negligible.
    // We deem the intensity difference of a couple of percent for high values of roughness
    // to not be worth the cost of another precomputed table.
    // Note: this formulation bakes the BSDF non-symmetric!
    preLightData.energyCompensation = 1.0 / reflectivity - 1.0;
#else
    preLightData.energyCompensation = 0.0;
#endif // LIT_USE_GGX_ENERGY_COMPENSATION

    // Area light
    // UVs for sampling the LUTs
    float theta = FastACosPos(NdotV); // For Area light - UVs for sampling the LUTs
    float2 uv = LTC_LUT_OFFSET + LTC_LUT_SCALE * float2(bsdfData.perceptualRoughness, theta * INV_HALF_PI);

    // Note we load the matrix transpose (avoid to have to transpose it in shader)
#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    preLightData.ltcTransformDiffuse = k_identity3x3;
#else
    // Get the inverse LTC matrix for Disney Diffuse
    preLightData.ltcTransformDiffuse      = 0.0;
    preLightData.ltcTransformDiffuse._m22 = 1.0;
    preLightData.ltcTransformDiffuse._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTC_DISNEY_DIFFUSE_MATRIX_INDEX, 0);
#endif

    // Get the inverse LTC matrix for GGX
    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcTransformSpecular      = 0.0;
    preLightData.ltcTransformSpecular._m22 = 1.0;
    preLightData.ltcTransformSpecular._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTC_GGX_MATRIX_INDEX, 0);

    // Construct a right-handed view-dependent orthogonal basis around the normal
    preLightData.orthoBasisViewNormal[0] = normalize(V - N * preLightData.NdotV); // Do not clamp NdotV here
    preLightData.orthoBasisViewNormal[2] = N;
    preLightData.orthoBasisViewNormal[1] = cross(preLightData.orthoBasisViewNormal[2], preLightData.orthoBasisViewNormal[0]);

    float3 ltcMagnitude = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTC_MULTI_GGX_FRESNEL_DISNEY_DIFFUSE_INDEX, 0).rgb;
    float  ltcGGXFresnelMagnitudeDiff = ltcMagnitude.r; // The difference of magnitudes of GGX and Fresnel
    float  ltcGGXFresnelMagnitude     = ltcMagnitude.g;
    float  ltcDisneyDiffuseMagnitude  = ltcMagnitude.b;

#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    preLightData.ltcMagnitudeDiffuse = 1;
#else
    preLightData.ltcMagnitudeDiffuse = ltcDisneyDiffuseMagnitude;
#endif

    // TODO: the fit seems rather poor. The scaling factor of 0.5 allows us
    // to match the reference for rough metals, but further darkens dielectrics.
    preLightData.ltcMagnitudeFresnel = bsdfData.fresnel0 * ltcGGXFresnelMagnitudeDiff + (float3)ltcGGXFresnelMagnitude;

    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        float2 uv = LTC_LUT_OFFSET + LTC_LUT_SCALE * float2(CLEAR_COAT_PERCEPTUAL_ROUGHNESS, theta * INV_HALF_PI);

        // Get the inverse LTC matrix for GGX
        // Note we load the matrix transpose (avoid to have to transpose it in shader)
        preLightData.ltcTransformCoat = 0.0;
        preLightData.ltcTransformCoat._m22 = 1.0;
        preLightData.ltcTransformCoat._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTC_GGX_MATRIX_INDEX, 0);

        ltcMagnitude = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTC_MULTI_GGX_FRESNEL_DISNEY_DIFFUSE_INDEX, 0).rgb;
        ltcGGXFresnelMagnitudeDiff  = ltcMagnitude.r; // The difference of magnitudes of GGX and Fresnel
        ltcGGXFresnelMagnitude      = ltcMagnitude.g;
        preLightData.ltcMagnitudeCoatFresnel = (CLEAR_COAT_F0 * ltcGGXFresnelMagnitudeDiff + ltcGGXFresnelMagnitude) * bsdfData.coatMask;
    }

    // refraction (forward only)
#ifdef REFRACTION_MODEL
    RefractionModelResult refraction = REFRACTION_MODEL(V, posInput, bsdfData);
    preLightData.transparentRefractV = refraction.rayWS;
    preLightData.transparentPositionWS = refraction.positionWS;
    preLightData.transparentTransmittance = exp(-bsdfData.absorptionCoefficient * refraction.dist);
    // Empirical remap to try to match a bit the refraction probe blurring for the fallback
    // Use IblPerceptualRoughness so we can handle approx of clear coat.
    preLightData.transparentSSMipLevel = sqrt(preLightData.iblPerceptualRoughness) * uint(_GaussianPyramidColorMipSize.z);
#endif

    return preLightData;
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

// GetBakedDiffuseLigthing function compute the bake lighting + emissive color to be store in emissive buffer (Deferred case)
// In forward it must be add to the final contribution.
// This function require the 3 structure surfaceData, builtinData, bsdfData because it may require both the engine side data, and data that will not be store inside the gbuffer.
float3 GetBakedDiffuseLigthing(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData, PreLightData preLightData)
{
    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING))
    {
        uint   texturingMode  = (bsdfData.materialFeatures >> MATERIAL_FEATURE_FLAGS_SSS_TEXTURING_MODE_OFFSET) & 3;
        bsdfData.diffuseColor = ApplySubsurfaceScatteringTexturingMode(texturingMode, bsdfData.diffuseColor);
    }

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // The lighting in SH or lightmap is assume to contain bounced light only (i.e no direct lighting), and is divide by PI (i.e Lambert is apply), so multiply by PI here to get back the illuminance
        return builtinData.bakeDiffuseLighting * PI;
    }
#endif

    // Premultiply bake diffuse lighting information with DisneyDiffuse pre-integration
    return builtinData.bakeDiffuseLighting * preLightData.diffuseFGD * surfaceData.ambientOcclusion * bsdfData.diffuseColor + builtinData.emissiveColor;
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
// Subsurface Scattering functions
//-----------------------------------------------------------------------------

bool ShouldOutputSplitLighting(BSDFData bsdfData)
{
    return HasFeatureFlag(bsdfData.materialFeatures, MATERIAL_FEATURE_FLAGS_SSS_OUTPUT_SPLIT_LIGHTING);
}

//-----------------------------------------------------------------------------
// LightLoop related function (Only include if required)
// HAS_LIGHTLOOP is define in Lighting.hlsl
//-----------------------------------------------------------------------------

#ifdef HAS_LIGHTLOOP

#ifndef _SURFACE_TYPE_TRANSPARENT
#define USE_DEFERRED_DIRECTIONAL_SHADOWS // Deferred shadows are always enabled for opaque objects
#endif

#include "../../Lighting/LightEvaluation.hlsl"
#include "../../Lighting/Reflection/VolumeProjection.hlsl"

//-----------------------------------------------------------------------------
// Lighting structure for light accumulation
//-----------------------------------------------------------------------------

// These structure allow to accumulate lighting accross the Lit material
// AggregateLighting is init to zero and transfer to EvaluateBSDF, but the LightLoop can't access its content.
struct DirectLighting
{
    float3 diffuse;
    float3 specular;
};

struct IndirectLighting
{
    float3 specularReflected;
    float3 specularTransmitted;
};

struct AggregateLighting
{
    DirectLighting   direct;
    IndirectLighting indirect;
};

void AccumulateDirectLighting(DirectLighting src, inout AggregateLighting dst)
{
    dst.direct.diffuse += src.diffuse;
    dst.direct.specular += src.specular;
}

void AccumulateIndirectLighting(IndirectLighting src, inout AggregateLighting dst)
{
    dst.indirect.specularReflected += src.specularReflected;
    dst.indirect.specularTransmitted += src.specularTransmitted;
}

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

// This function apply BSDF. Assumes that NdotL is positive.
void BSDF(  float3 V, float3 L, float NdotL, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
            out float3 diffuseLighting,
            out float3 specularLighting)
{
    float3 N = bsdfData.normalWS;

    // Optimized math. Ref: PBR Diffuse Lighting for GGX + Smith Microsurfaces (slide 114).
    float LdotV    = dot(L, V);
    float invLenLV = rsqrt(max(2.0 * LdotV + 2.0, FLT_EPS));            // invLenLV = rcp(length(L + V)), clamp to avoid rsqrt(0) = NaN
    float NdotH    = saturate((NdotL + preLightData.NdotV) * invLenLV); // Do not clamp NdotV here
    float LdotH    = saturate(invLenLV * LdotV + invLenLV);
    float NdotV    = ClampNdotV(preLightData.NdotV);

    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);
    // Remark: Fresnel must be use with LdotH angle. But Fresnel for iridescence is expensive to compute at each light.
    // Instead we use the incorrect angle NdotV as an approximation for LdotH for Fresnel evaluation.
    // The Fresnel with iridescence and NDotV angle is precomputed ahead and here we jsut reuse the result.
    // Thus why we shouldn't apply a second time Fresnel on the value if iridescence is enabled.
    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_IRIDESCENCE))
    {
        F = lerp(F, bsdfData.fresnel0, bsdfData.iridescenceMask);
    }

    float DV;
    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
    {
        float3 H = (L + V) * invLenLV;

        // For anisotropy we must not saturate these values
        float TdotH = dot(bsdfData.tangentWS, H);
        float TdotL = dot(bsdfData.tangentWS, L);
        float BdotH = dot(bsdfData.bitangentWS, H);
        float BdotL = dot(bsdfData.bitangentWS, L);

        // TODO: Do comparison between this correct version and the one from isotropic and see if there is any visual difference
        DV = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH, NdotV, TdotL, BdotL, NdotL,
                                   bsdfData.roughnessT, bsdfData.roughnessB, preLightData.partLambdaV);
    }
    else
    {
        DV = DV_SmithJointGGX(NdotH, NdotL, NdotV, bsdfData.roughnessT, preLightData.partLambdaV);
    }
    specularLighting = F * DV;

#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    float  diffuseTerm = Lambert();
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
    float diffuseTerm = DisneyDiffuse(NdotV, NdotL, LdotV, bsdfData.perceptualRoughness);
#endif

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    diffuseLighting = diffuseTerm;

    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        // Apply isotropic GGX for clear coat
        // Note: coat F is scalar as it is a dieletric
        float coatF = F_Schlick(CLEAR_COAT_F0, LdotH) * bsdfData.coatMask;
        // Scale base specular
        specularLighting *= Sq(1.0 - coatF);

        // Add top specular
        // TODO: Should we call just D_GGX here ?
        float DV = DV_SmithJointGGX(NdotH, NdotL, NdotV, bsdfData.coatRoughness, preLightData.coatPartLambdaV);
        specularLighting += coatF * DV;

        // Note: The modification of the base roughness and fresnel0 by the clear coat is already handled in FillMaterialClearCoatData

        // Very coarse attempt at doing energy conservation for the diffuse layer based on NdotL. No science.
        diffuseLighting *= lerp(1, 1.0 - coatF, bsdfData.coatMask);
    }
}

// In the "thin object" mode (for cards), we assume that the geometry is very thin.
// We apply wrapped lighting to compensate for that, and do not modify the shading position.
// Otherwise, in the "thick object" mode, we can have EITHER reflected (front) lighting
// OR transmitted (back) lighting, never both at the same time. For transmitted lighting,
// we need to push the shading position back to avoid self-shadowing problems.
// Note: 'bsdfData.thickness' is in world units, and already accounts for the transmission mode.
float3 ComputeThicknessDisplacement(BSDFData bsdfData, float3 L, float NdotL)
{
    // Compute the thickness in world units along the light vector.
    // We need a max(x, 0) here, but the saturate() is free,
    // and we don't expect the total displacement of over 1 meter.
    float displacement = saturate(bsdfData.thickness / -NdotL);

    return displacement * L;
}

// Currently, we only model diffuse transmission. Specular transmission is not yet supported.
// Transmitted lighting is computed as follows:
// - we assume that the object is a thick plane (slab);
// - we reverse the front-facing normal for the back of the object;
// - we assume that the incoming radiance is constant along the entire back surface;
// - we apply BSDF-specific diffuse transmission to transmit the light subsurface and back;
// - we integrate the diffuse reflectance profile w.r.t. the radius (while also accounting
//   for the thickness) to compute the transmittance;
// - we multiply the transmitted radiance by the transmittance.
float3 EvaluateTransmission(BSDFData bsdfData, float3 transmittance, float NdotL, float NdotV, float attenuation)
{
    float wrappedNdotL = ComputeWrappedDiffuseLighting(-NdotL, SSS_WRAP_LIGHT);
    float negatedNdotL = -NdotL;

    // Apply wrapped lighting to better handle thin objects (cards) at grazing angles.
    bool  autoThicknessMode = HasFeatureFlag(bsdfData.materialFeatures, MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_AUTO_THICKNESS);
    float backNdotL         = autoThicknessMode ? negatedNdotL : wrappedNdotL;

    // Apply BSDF-specific diffuse transmission to attenuation. See also: [SSS-NOTE-TRSM]
    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    attenuation *= Lambert();
#else
    attenuation *= INV_PI * F_Transm_Schlick(0, 0.5, NdotV) * F_Transm_Schlick(0, 0.5, abs(backNdotL));
#endif

    float intensity = max(0, attenuation * backNdotL); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    return intensity * transmittance;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                                        float3 V, PositionInputs posInput, PreLightData preLightData,
                                        DirectionalLightData lightData, BSDFData bsdfData,
                                        BakeLightingData bakeLightingData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 N     = bsdfData.normalWS;
    float3 L     = -lightData.forward; // Lights point backward in Unity
    float  NdotL = dot(N, L); // Note: Ideally this N here should be vertex normal - use for transmisison

    float3 transmittance     = bsdfData.transmittance;
    bool   autoThicknessMode = HasFeatureFlag(bsdfData.materialFeatures, MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_AUTO_THICKNESS);

    UNITY_BRANCH
    if (autoThicknessMode && NdotL < 0 && lightData.shadowIndex >= 0)
    {
        // TODO: perform bilinear filtering of the shadow map.
        // Recompute transmittance using the thickness value computed from the shadow map.
    #if 0
        // Does not work, I get a compiler crash...
        float3 occluderPosWS = EvalShadow_GetClosestSample_Cascade(lightLoopContext.shadowContext, posInput.positionWS, bsdfData.normalWS, lightData.shadowIndex, float4(L, 0));
    #else
        #define SHADOW_DISPATCH_DIR_TEX 3 // Manually keep it in sync with Shadow.hlsl...
        float3 occluderPosWS = EvalShadow_GetClosestSample_Cascade(lightLoopContext.shadowContext, lightLoopContext.shadowContext.tex2DArray[SHADOW_DISPATCH_DIR_TEX], posInput.positionWS, bsdfData.normalWS, lightData.shadowIndex, float4(L, 0));
    #endif

        float thicknessInUnits       = distance(posInput.positionWS, occluderPosWS);
        float thicknessInMeters      = thicknessInUnits * _WorldScales[bsdfData.diffusionProfile].x;
        float thicknessInMillimeters = thicknessInMeters * MILLIMETERS_PER_METER;

        // TODO: optimize.
    #if SHADEROPTIONS_USE_DISNEY_SSS
        transmittance = ComputeTransmittanceDisney(_ShapeParams[bsdfData.diffusionProfile].rgb,
                                                   _TransmissionTintsAndFresnel0[bsdfData.diffusionProfile].rgb,
                                                   thicknessInMillimeters);
    #else
        transmittance = ComputeTransmittanceJimenez(_HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][0].rgb,
                                                    _HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][0].a,
                                                    _HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][1].rgb,
                                                    _HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][1].a,
                                                    _TransmissionTintsAndFresnel0[bsdfData.diffusionProfile].rgb,
                                                    thicknessInMillimeters);
    #endif

        // Make sure we do not sample the shadow map twice.
        lightData.shadowIndex = -1;
        // Note: we do not modify the distance to the light, or the light angle for the back face.
        // This is a performance-saving optimization which makes sense as long as the thickness is small.
    }

    float3 color;
    float attenuation;
    EvaluateLight_Directional(lightLoopContext, posInput, lightData, bakeLightingData, N, L, color, attenuation);

    float intensity = max(0, attenuation * NdotL); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are ok with this
    UNITY_BRANCH if (intensity > 0.0)
    {
        BSDF(V, L, NdotL, posInput.positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * lightData.diffuseScale;
        lighting.specular *= intensity * lightData.specularScale;
    }

    // TODO: move this before BSDF() to save VGPRs.
    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        lighting.diffuse += EvaluateTransmission(bsdfData, transmittance, NdotL, ClampNdotV(preLightData.NdotV), attenuation * lightData.diffuseScale);
    }

    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        lighting.diffuse = color * intensity * lightData.diffuseScale;
    }
#endif

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData, BSDFData bsdfData, BakeLightingData bakeLightingData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 lightToSample = posInput.positionWS - lightData.positionWS;
    int    lightType     = lightData.lightType;

    float3 L;
    float4 distances; // {d, d^2, 1/d, d_proj}
    distances.w = dot(lightToSample, lightData.forward);

    if (lightType == GPULIGHTTYPE_PROJECTOR_BOX)
    {
        L = -lightData.forward;
        distances.xyz = 1; // No distance or angle attenuation
    }
    else
    {
        float3 unL     = -lightToSample;
        float  distSq  = dot(unL, unL);
        float  distRcp = rsqrt(distSq);
        float  dist    = distSq * distRcp;

        L = unL * distRcp;
        distances.xyz = float3(dist, distSq, distRcp);
    }

    float3 N     = bsdfData.normalWS;
    float  NdotL = dot(N, L);

    float3 transmittance     = bsdfData.transmittance;
    bool   autoThicknessMode = HasFeatureFlag(bsdfData.materialFeatures, MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_AUTO_THICKNESS);

    UNITY_BRANCH
    if (autoThicknessMode && NdotL < 0 && lightData.shadowIndex >= 0)
    {
        // TODO: perform bilinear filtering of the shadow map.
        // Recompute transmittance using the thickness value computed from the shadow map.
    #if 0
        // Does not work, I get a compiler crash...
        float3 occluderPosWS = EvalShadow_GetClosestSample_Punctual(lightLoopContext.shadowContext, posInput.positionWS, lightData.shadowIndex, L);
    #else
        #define SHADOW_DISPATCH_PUNC_TEX 3 // Manually keep it in sync with Shadow.hlsl...
        float3 occluderPosWS = EvalShadow_GetClosestSample_Punctual(lightLoopContext.shadowContext, lightLoopContext.shadowContext.tex2DArray[SHADOW_DISPATCH_PUNC_TEX], posInput.positionWS, lightData.shadowIndex, L);
    #endif

        float thicknessInUnits       = distance(posInput.positionWS, occluderPosWS);
        float thicknessInMeters      = thicknessInUnits * _WorldScales[bsdfData.diffusionProfile].x;
        float thicknessInMillimeters = thicknessInMeters * MILLIMETERS_PER_METER;

        // TODO: optimize.
    #if SHADEROPTIONS_USE_DISNEY_SSS
        transmittance = ComputeTransmittanceDisney(_ShapeParams[bsdfData.diffusionProfile].rgb,
                                                   _TransmissionTintsAndFresnel0[bsdfData.diffusionProfile].rgb,
                                                   thicknessInMillimeters);
    #else
        transmittance = ComputeTransmittanceJimenez(_HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][0].rgb,
                                                    _HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][0].a,
                                                    _HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][1].rgb,
                                                    _HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][1].a,
                                                    _TransmissionTintsAndFresnel0[bsdfData.diffusionProfile].rgb,
                                                    thicknessInMillimeters);
    #endif

        // Make sure we do not sample the shadow map twice.
        lightData.shadowIndex = -1;
        // Note: we do not modify the distance to the light, or the light angle for the back face.
        // This is a performance-saving optimization which makes sense as long as the thickness is small.
    }

    float3 color;
    float attenuation;
    EvaluateLight_Punctual(lightLoopContext, posInput, lightData, bakeLightingData, N, L,
                           lightToSample, distances, color, attenuation);

    float intensity = max(0, attenuation * NdotL); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are ok with this
    UNITY_BRANCH if (intensity > 0.0)
    {
        // Simulate a sphere light with this hack
        // Note that it is not correct with our pre-computation of PartLambdaV (mean if we disable the optimization we will not have the
        // same result) but we don't care as it is a hack anyway
        bsdfData.coatRoughness = max(bsdfData.coatRoughness, lightData.minRoughness);
        bsdfData.roughnessT = max(bsdfData.roughnessT, lightData.minRoughness);
        bsdfData.roughnessB = max(bsdfData.roughnessB, lightData.minRoughness);

        BSDF(V, L, NdotL, posInput.positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * lightData.diffuseScale;
        lighting.specular *= intensity * lightData.specularScale;
    }

    // TODO: move this before BSDF() to save VGPRs.
    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        lighting.diffuse += EvaluateTransmission(bsdfData, transmittance, NdotL, ClampNdotV(preLightData.NdotV), attenuation * lightData.diffuseScale);
    }

    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        lighting.diffuse = color * intensity * lightData.diffuseScale;
    }
#endif

    return lighting;
}

#include "LitReference.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Line(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BakeLightingData bakeLightingData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 positionWS = posInput.positionWS;

#ifdef LIT_DISPLAY_REFERENCE_AREA
    IntegrateBSDF_LineRef(V, positionWS, preLightData, lightData, bsdfData,
                          lighting.diffuse, lighting.specular);
#else
    float  len = lightData.size.x;
    float3 T   = lightData.right;

    float3 unL = lightData.positionWS - positionWS;

    // Pick the major axis of the ellipsoid.
    float3 axis = lightData.right;

    // We define the ellipsoid s.t. r1 = (r + len / 2), r2 = r3 = r.
    // TODO: This could be precomputed.
    float radius         = rsqrt(lightData.invSqrAttenuationRadius);
    float invAspectRatio = saturate(radius / (radius + (0.5 * len)));

    // Compute the light attenuation.
    float intensity = EllipsoidalDistanceAttenuation(unL, lightData.invSqrAttenuationRadius,
                                                     axis, invAspectRatio);

    // Terminate if the shaded point is too far away.
    if (intensity == 0.0)
        return lighting;

    lightData.diffuseScale  *= intensity;
    lightData.specularScale *= intensity;

    // Translate the light s.t. the shaded point is at the origin of the coordinate system.
    lightData.positionWS -= positionWS;

    // TODO: some of this could be precomputed.
    float3 P1 = lightData.positionWS - T * (0.5 * len);
    float3 P2 = lightData.positionWS + T * (0.5 * len);

    // Rotate the endpoints into the local coordinate system.
    P1 = mul(P1, transpose(preLightData.orthoBasisViewNormal));
    P2 = mul(P2, transpose(preLightData.orthoBasisViewNormal));

    // Compute the binormal in the local coordinate system.
    float3 B = normalize(cross(P1, P2));

    float ltcValue;

    // Evaluate the diffuse part
    ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformDiffuse);
    ltcValue *= lightData.diffuseScale;
    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    lighting.diffuse = preLightData.ltcMagnitudeDiffuse * ltcValue;

    UNITY_BRANCH if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        // Flip the view vector and the normal. The bitangent stays the same.
        float3x3 flipMatrix = float3x3(-1,  0,  0,
                                        0,  1,  0,
                                        0,  0, -1);

        // Use the Lambertian approximation for performance reasons.
        // The matrix multiplication should not generate any extra ALU on GCN.
        // TODO: double evaluation is very inefficient! This is a temporary solution.
        ltcValue  = LTCEvaluate(P1, P2, B, mul(flipMatrix, k_identity3x3));
        ltcValue *= lightData.diffuseScale;
        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
        lighting.diffuse += bsdfData.transmittance * ltcValue;
    }

    // Evaluate the specular part
    ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformSpecular);
    ltcValue *= lightData.specularScale;
    lighting.specular = preLightData.ltcMagnitudeFresnel * ltcValue;

    // Evaluate the coat part
    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        lighting.diffuse *= (1.0 - preLightData.ltcMagnitudeCoatFresnel);
        lighting.specular *= (1.0 - preLightData.ltcMagnitudeCoatFresnel);
        ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformCoat);
        ltcValue *= lightData.specularScale;
        lighting.specular += preLightData.ltcMagnitudeCoatFresnel * ltcValue;
    }

    // Save ALU by applying 'lightData.color' only once.
    lighting.diffuse *= lightData.color;
    lighting.specular *= lightData.color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        // Apply area light on lambert then multiply by PI to cancel Lambert
        lighting.diffuse = LTCEvaluate(P1, P2, B, k_identity3x3);
        lighting.diffuse *= PI * lightData.diffuseScale;
    }
#endif

#endif // LIT_DISPLAY_REFERENCE_AREA

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

// #define ELLIPSOIDAL_ATTENUATION

DirectLighting EvaluateBSDF_Rect(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BakeLightingData bakeLightingData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 positionWS = posInput.positionWS;

#ifdef LIT_DISPLAY_REFERENCE_AREA
    IntegrateBSDF_AreaRef(V, positionWS, preLightData, lightData, bsdfData,
                          lighting.diffuse, lighting.specular);
#else
    float3 unL = lightData.positionWS - positionWS;

    if (dot(lightData.forward, unL) >= 0.0001)
    {
        // The light is back-facing.
        return lighting;
    }

    // Rotate the light direction into the light space.
    float3x3 lightToWorld = float3x3(lightData.right, lightData.up, -lightData.forward);
    unL = mul(unL, transpose(lightToWorld));

    // TODO: This could be precomputed.
    float halfWidth  = lightData.size.x * 0.5;
    float halfHeight = lightData.size.y * 0.5;

    // Define the dimensions of the attenuation volume.
    // TODO: This could be precomputed.
    float  radius     = rsqrt(lightData.invSqrAttenuationRadius);
    float3 invHalfDim = rcp(float3(radius + halfWidth,
                                   radius + halfHeight,
                                   radius));

    // Compute the light attenuation.
#ifdef ELLIPSOIDAL_ATTENUATION
    // The attenuation volume is an axis-aligned ellipsoid s.t.
    // r1 = (r + w / 2), r2 = (r + h / 2), r3 = r.
    float intensity = EllipsoidalDistanceAttenuation(unL, invHalfDim);
#else
    // The attenuation volume is an axis-aligned box s.t.
    // hX = (r + w / 2), hY = (r + h / 2), hZ = r.
    float intensity = BoxDistanceAttenuation(unL, invHalfDim);
#endif

    // Terminate if the shaded point is too far away.
    if (intensity == 0.0)
        return lighting;

    lightData.diffuseScale  *= intensity;
    lightData.specularScale *= intensity;

    // Translate the light s.t. the shaded point is at the origin of the coordinate system.
    lightData.positionWS -= positionWS;

    float4x3 lightVerts;

    // TODO: some of this could be precomputed.
    lightVerts[0] = lightData.positionWS + lightData.right *  halfWidth + lightData.up *  halfHeight;
    lightVerts[1] = lightData.positionWS + lightData.right *  halfWidth + lightData.up * -halfHeight;
    lightVerts[2] = lightData.positionWS + lightData.right * -halfWidth + lightData.up * -halfHeight;
    lightVerts[3] = lightData.positionWS + lightData.right * -halfWidth + lightData.up *  halfHeight;

    // Rotate the endpoints into the local coordinate system.
    lightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal));

    float ltcValue;

    // Evaluate the diffuse part
    // Polygon irradiance in the transformed configuration.
    ltcValue  = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformDiffuse));
    ltcValue *= lightData.diffuseScale;
    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    lighting.diffuse = preLightData.ltcMagnitudeDiffuse * ltcValue;

    UNITY_BRANCH if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_TRANSMISSION))
    {
        // Flip the view vector and the normal. The bitangent stays the same.
        float3x3 flipMatrix = float3x3(-1,  0,  0,
                                        0,  1,  0,
                                        0,  0, -1);

        // Use the Lambertian approximation for performance reasons.
        // The matrix multiplication should not generate any extra ALU on GCN.
        float3x3 ltcTransform = mul(flipMatrix, k_identity3x3);

        // Polygon irradiance in the transformed configuration.
        // TODO: double evaluation is very inefficient! This is a temporary solution.
        ltcValue  = PolygonIrradiance(mul(lightVerts, ltcTransform));
        ltcValue *= lightData.diffuseScale;
        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
        lighting.diffuse += bsdfData.transmittance * ltcValue;
    }

    // Evaluate the specular part
    // Polygon irradiance in the transformed configuration.
    ltcValue  = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformSpecular));
    ltcValue *= lightData.specularScale;
    lighting.specular += preLightData.ltcMagnitudeFresnel * ltcValue;

    // Evaluate the coat part
    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
    {
        lighting.diffuse *= (1.0 - preLightData.ltcMagnitudeCoatFresnel);
        lighting.specular *= (1.0 - preLightData.ltcMagnitudeCoatFresnel);
        ltcValue = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformCoat));
        ltcValue *= lightData.specularScale;
        lighting.specular += preLightData.ltcMagnitudeCoatFresnel * ltcValue;
    }

    // Save ALU by applying 'lightData.color' only once.
    lighting.diffuse *= lightData.color;
    lighting.specular *= lightData.color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        // Apply area light on lambert then multiply by PI to cancel Lambert
        lighting.diffuse = PolygonIrradiance(mul(lightVerts, k_identity3x3));
        lighting.diffuse *= PI * lightData.diffuseScale;
    }
#endif

#endif // LIT_DISPLAY_REFERENCE_AREA

    return lighting;
}

DirectLighting EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BakeLightingData bakeLightingData)
{
    if (lightData.lightType == GPULIGHTTYPE_LINE)
    {
        return EvaluateBSDF_Line(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, bakeLightingData);
    }
    else
    {
        return EvaluateBSDF_Rect(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, bakeLightingData);
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_SSLighting for screen space lighting
// ----------------------------------------------------------------------------

IndirectLighting EvaluateBSDF_SSLighting(LightLoopContext lightLoopContext,
                                            float3 V, PositionInputs posInput,
                                            PreLightData preLightData, BSDFData bsdfData,
                                            int GPUImageBasedLightingType,
                                            inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    switch (GPUImageBasedLightingType)
    {
        case GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION:
        {
#if HAS_REFRACTION
            // Refraction process:
            //  1. Depending on the shape model, we calculate the refracted point in world space and the optical depth
            //  2. We calculate the screen space position of the refracted point
            //  3. If this point is available (ie: in color buffer and point is not in front of the object)
            //    a. Get the corresponding color depending on the roughness from the gaussian pyramid of the color buffer
            //    b. Multiply by the transmittance for absorption (depends on the optical depth)

            float3 refractedBackPointWS = EstimateRaycast(V, posInput, preLightData.transparentPositionWS, preLightData.transparentRefractV);

            // Calculate screen space coordinates of refracted point in back plane
            float2 refractedBackPointNDC = ComputeNormalizedDeviceCoordinates(refractedBackPointWS, UNITY_MATRIX_VP);
            uint2 depthSize = uint2(_PyramidDepthMipSize.xy);
            float refractedBackPointDepth = LinearEyeDepth(LOAD_TEXTURE2D_LOD(_PyramidDepthTexture, refractedBackPointNDC * depthSize, 0).r, _ZBufferParams);

            // Exit if texel is out of color buffer
            // Or if the texel is from an object in front of the object
            if (refractedBackPointDepth < posInput.linearDepth
                || any(refractedBackPointNDC < 0.0)
                || any(refractedBackPointNDC > 1.0))
            {
                // Do nothing and don't update the hierarchy weight so we can fall back on refraction probe
                return lighting;
            }

            // Map the roughness to the correct mip map level of the color pyramid
            lighting.specularTransmitted = SAMPLE_TEXTURE2D_LOD(_GaussianPyramidColorTexture, s_trilinear_clamp_sampler, refractedBackPointNDC * _GaussianPyramidColorMipSize.xy, preLightData.transparentSSMipLevel).rgb;

            // Beer-Lamber law for absorption
            lighting.specularTransmitted *= preLightData.transparentTransmittance;

            float weight = 1.0;
            UpdateLightingHierarchyWeights(hierarchyWeight, weight); // Shouldn't be needed, but safer in case we decide to change hierarchy priority
                                                                     // We use specularFGD as an approximation of the fresnel effect (that also handle smoothness), so take the remaining for transmission
            lighting.specularTransmitted *= (1.0 - preLightData.specularFGD) * weight;
#else
            // No refraction, no need to go further
            hierarchyWeight = 1.0;
#endif
            break;
        }
        case GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION:
        {
            break;
        }
    }

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
IndirectLighting EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
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


//    #ifdef LIT_DIFFUSE_LAMBERT_BRDF
//    envLighting += IntegrateLambertIBLRef(lightData, V, bsdfData);
//    #else
//    envLighting += IntegrateDisneyDiffuseIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);
//    #endif

#else

    // TODO: factor this code in common, so other material authoring don't require to rewrite everything,
    // TODO: test the strech from Tomasz
    // float roughness       = PerceptualRoughnessToRoughness(preLightData.IblPerceptualRoughness);
    // float shrunkRoughness = AnisotropicStrechAtGrazingAngle(roughness, roughness, NdotV);

    // Guideline for reflection volume: In HDRenderPipeline we separate the projection volume (the proxy of the scene) from the influence volume (what pixel on the screen is affected)
    // However we add the constrain that the shape of the projection and influence volume is the same (i.e if we have a sphere shape projection volume, we have a shape influence).
    // It allow to have more coherence for the dynamic if in shader code.
    // Users can also chose to not have any projection, in this case we use the property minProjectionDistance to minimize code change. minProjectionDistance is set to huge number
    // that simulate effect of no shape projection

    float3 R = preLightData.iblR;
    float3 coatR = preLightData.coatIblR;

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION)
    {
        positionWS = preLightData.transparentPositionWS;
        R = preLightData.transparentRefractV;
    }

    // In Unity the cubemaps are capture with the localToWorld transform of the component.
    // This mean that location and orientation matter. So after intersection of proxy volume we need to convert back to world.

    float3x3 worldToIS = WorldToInfluenceSpace(lightData);
    float3 positionIS = WorldToInfluencePosition(lightData, worldToIS, positionWS);
    float3 dirIS = mul(R, worldToIS);

    float3x3 worldToPS = WorldToProxySpace(lightData);
    float3 positionPS = WorldToProxyPosition(lightData, worldToPS, positionWS);
    float3 dirPS = mul(R, worldToPS);

    float projectionDistance = 0;
    // 1. First process the projection
    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    if (influenceShapeType == ENVSHAPETYPE_SPHERE)
    {
        projectionDistance = IntersectSphereProxy(lightData, dirPS, positionPS);
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in lightData.capturePositionWS
        float3 capturePositionWS = GetCapturePositionWS(lightData);
        R = (positionWS + projectionDistance * R) - capturePositionWS;

        // Test again for clear coat
        if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION && HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
        {
            dirPS = mul(coatR, worldToPS);
            projectionDistance = IntersectSphereProxy(lightData, dirPS, positionPS);
            coatR = (positionWS + projectionDistance * coatR) - capturePositionWS;
        }
        weight = InfluenceSphereWeight(lightData, bsdfData, positionWS, positionIS, dirIS);
    }
    else if (influenceShapeType == ENVSHAPETYPE_BOX)
    {
        projectionDistance = IntersectBoxProxy(lightData, dirPS, positionPS);
        // No need to normalize for fetching cubemap
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in lightData.capturePositionWS
        float3 capturePositionWS = GetCapturePositionWS(lightData);
        R = (positionWS + projectionDistance * R) - capturePositionWS;

        // TODO: add distance based roughness

        // Test again for clear coat
        if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION && HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
        {
            dirPS = mul(coatR, worldToPS);
            projectionDistance = IntersectBoxProxy(lightData, dirPS, positionPS);
            coatR = (positionWS + projectionDistance * coatR) - capturePositionWS;
        }
        weight = InfluenceBoxWeight(lightData, bsdfData, positionWS, positionIS, dirIS);
    }

#ifdef DEBUG_DISPLAY
    float3 radiusToProxy = R;
#endif

    // When we are rough, we tend to see outward shifting of the reflection when at the boundary of the projection volume
    // Also it appear like more sharp. To avoid these artifact and at the same time get better match to reference we lerp to original unmodified reflection.
    // Formula is empirical.
    float roughness = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness);
    R = lerp(R, preLightData.iblR, saturate(smoothstep(0, 1, roughness * roughness)));

    float3 sampleDirectionDiscardWS = GetSampleDirectionDiscardWS(lightData);
    if (dot(sampleDirectionDiscardWS, R) < 0)
        return lighting;

    float3 F = preLightData.specularFGD;
    float iblMipLevel = PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness);


    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, iblMipLevel);
    weight *= preLD.a;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_ENVIRONMENT_PROXY_VOLUME)
        preLD = ApplyDebugProjectionVolume(preLD, radiusToProxy, _DebugEnvironmentProxyDepthScale);
#endif

    // Smooth weighting
    weight = Smoothstep01(weight);

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
    {
        envLighting = F * preLD.rgb;

        // Evaluate the Clear Coat component if needed
        if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT))
        {
            // No correction needed for coatR as it is smooth
            // Note: coat F is scalar as it is a dieletric
            envLighting *= Sq(1.0 - preLightData.coatIblF);

            // Evaluate the Clear Coat color
            float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, coatR, 0.0);
            envLighting += preLightData.coatIblF * preLD.rgb;

            // Can't attenuate diffuse lighting here, may try to apply something on bakeLighting in PostEvaluateBSDF
        }
    }
    else
    {
        // No clear coat support with refraction

        // specular transmisted lighting is the remaining of the reflection (let's use this approx)
        // With refraction, we don't care about the clear coat value, only about the Fresnel, thus why we use 'envLighting ='
        envLighting = (1.0 - F) * preLD.rgb * preLightData.transparentTransmittance;
    }

#endif // LIT_DISPLAY_REFERENCE_IBL

    UpdateLightingHierarchyWeights(hierarchyWeight, weight);
    envLighting *= weight * lightData.dimmer;

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
        lighting.specularReflected = envLighting;
    else
        lighting.specularTransmitted = envLighting * preLightData.transparentTransmittance;

    return lighting;
}

//-----------------------------------------------------------------------------
// PostEvaluateBSDF
// ----------------------------------------------------------------------------

void PostEvaluateBSDF(  LightLoopContext lightLoopContext,
                        float3 V, PositionInputs posInput,
                        PreLightData preLightData, BSDFData bsdfData, BakeLightingData bakeLightingData, AggregateLighting lighting,
                        out float3 diffuseLighting, out float3 specularLighting)
{
    float3 bakeDiffuseLighting = bakeLightingData.bakeDiffuseLighting;

    // Use GTAOMultiBounce approximation for ambient occlusion (allow to get a tint from the baseColor)
#define GTAO_MULTIBOUNCE_APPROX 1

    // Note: When we ImageLoad outside of texture size, the value returned by Load is 0 (Note: On Metal maybe it clamp to value of texture which is also fine)
    // We use this property to have a neutral value for AO that doesn't consume a sampler and work also with compute shader (i.e use ImageLoad)
    // We store inverse AO so neutral is black. So either we sample inside or outside the texture it return 0 in case of neutral

    // Ambient occlusion use for indirect lighting (reflection probe, baked diffuse lighting)
#ifndef _SURFACE_TYPE_TRANSPARENT
    float indirectAmbientOcclusion = 1.0 - LOAD_TEXTURE2D(_AmbientOcclusionTexture, posInput.positionSS).x;
    // Ambient occlusion use for direct lighting (directional, punctual, area)
    float directAmbientOcclusion = lerp(1.0, indirectAmbientOcclusion, _AmbientOcclusionParam.w);
#else
    float indirectAmbientOcclusion = 1.0;
    float directAmbientOcclusion = 1.0;
#endif

    // Add indirect diffuse + emissive (if any) - Ambient occlusion is multiply by emissive which is wrong but not a big deal
#if GTAO_MULTIBOUNCE_APPROX
    bakeDiffuseLighting *= GTAOMultiBounce(indirectAmbientOcclusion, bsdfData.diffuseColor);
#else
    bakeDiffuseLighting *= lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), indirectAmbientOcclusion);
#endif

    float roughness         = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
    float specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(ClampNdotV(preLightData.NdotV), indirectAmbientOcclusion, roughness);
    // Try to mimic multibounce with specular color. Not the point of the original formula but ok result.
    // Take the min of screenspace specular occlusion and visibility cone specular occlusion
#if GTAO_MULTIBOUNCE_APPROX
    lighting.indirect.specularReflected *= GTAOMultiBounce(min(bsdfData.specularOcclusion, specularOcclusion), bsdfData.fresnel0);
#else
    lighting.indirect.specularReflected *= lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), min(bsdfData.specularOcclusion, specularOcclusion));
#endif

    lighting.direct.diffuse *=
#if GTAO_MULTIBOUNCE_APPROX
                                GTAOMultiBounce(directAmbientOcclusion, bsdfData.diffuseColor);
#else
                                lerp(_AmbientOcclusionParam.rgb, float3(1.0, 1.0, 1.0), directAmbientOcclusion);
#endif

    uint   texturingMode        = (bsdfData.materialFeatures >> MATERIAL_FEATURE_FLAGS_SSS_TEXTURING_MODE_OFFSET) & 3;
    float3 modifiedDiffuseColor = ApplySubsurfaceScatteringTexturingMode(texturingMode, bsdfData.diffuseColor);

    // Apply the albedo to the direct diffuse lighting (only once). The indirect (baked)
    // diffuse lighting has already had the albedo applied in GetBakedDiffuseLigthing().
    diffuseLighting = modifiedDiffuseColor * lighting.direct.diffuse + bakeDiffuseLighting;

    // If refraction is enable we use the transmittanceMask to lerp between current diffuse lighting and refraction value
    // Physically speaking, it should be transmittanceMask should be 1, but for artistic reasons, we let the value vary
#if HAS_REFRACTION
    diffuseLighting = lerp(diffuseLighting, lighting.indirect.specularTransmitted, bsdfData.transmittanceMask);
#endif

    specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;
    // Rescale the GGX to account for the multiple scattering.
    specularLighting *= 1.0 + bsdfData.fresnel0 * preLightData.energyCompensation;

#ifdef DEBUG_DISPLAY

    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        diffuseLighting = lighting.direct.diffuse + bakeLightingData.bakeDiffuseLighting;
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_INDIRECT_DIFFUSE_OCCLUSION_FROM_SSAO)
    {
        diffuseLighting = indirectAmbientOcclusion;
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_INDIRECT_SPECULAR_OCCLUSION_FROM_SSAO)
    {
        diffuseLighting = specularOcclusion;
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
    #if GTAO_MULTIBOUNCE_APPROX
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_INDIRECT_DIFFUSE_GTAO_FROM_SSAO)
    {
        diffuseLighting = GTAOMultiBounce(indirectAmbientOcclusion, bsdfData.diffuseColor);
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
    else if (_DebugLightingMode == DEBUGLIGHTINGMODE_INDIRECT_SPECULAR_GTAO_FROM_SSAO)
    {
        diffuseLighting = GTAOMultiBounce(specularOcclusion, bsdfData.fresnel0);
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
    #endif
    else if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        diffuseLighting = bsdfData.diffuseColor;
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
