#define ENVMAP_FEATURE_PERFACEINFLUENCE
#define ENVMAP_FEATURE_INFLUENCENORMAL
#define ENVMAP_FEATURE_PERFACEFADE

// SurfaceData is define in Lit.cs which generate Lit.cs.hlsl
#include "Lit.cs.hlsl"
#include "../SubsurfaceScattering/SubsurfaceScattering.hlsl"

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
float4 _GaussianPyramidColorMipSize;
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

#define GBUFFER_LIT_SPECULAR_COLOR 15
#define GBUFFER_LIT_SSS_OR_TRANSMISSION 14
#define GBUFFER_LIT_IRIDESCENCE 13
#define GBUFFER_LIT_ANISOTROPIC_UPPER_BOUND 12

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

    // Standard with clear coat and Iridescence
    /* 21 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 22 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_AREA | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 23 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 24 */ LIGHTFEATUREFLAGS_SKY | LIGHTFEATUREFLAGS_DIRECTIONAL | LIGHTFEATUREFLAGS_PUNCTUAL | LIGHTFEATUREFLAGS_ENV | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,
    /* 25 */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIALFEATUREFLAGS_LIT_IRIDESCENCE | MATERIALFEATUREFLAGS_LIT_CLEAR_COAT | MATERIALFEATUREFLAGS_LIT_STANDARD,

    /* 26 */ LIGHT_FEATURE_MASK_FLAGS_OPAQUE | MATERIAL_FEATURE_MASK_FLAGS, // Catch all case with MATERIAL_FEATURE_MASK_FLAGS is needed in case we disable material classification
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

// This function need to return a compile time value, else there is no optimization
uint TileVariantToFeatureFlags(uint variant)
{
    return kFeatureVariantFlags[variant];
}

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

float3 ComputeDiffuseColor(float3 baseColor, float metallic)
{
    return baseColor * (1.0 - metallic);
}

float3 ComputeFresnel0(float3 baseColor, float metallic, float dielectricF0)
{
    return lerp(dielectricF0.xxx, baseColor, metallic);
}

// Assume that bsdfData.diffusionProfile is init
void FillMaterialSSS(float subsurfaceMask, inout BSDFData bsdfData)
{
    bsdfData.fresnel0 = _TransmissionTintsAndFresnel0[bsdfData.diffusionProfile].a;
    bsdfData.subsurfaceMask = subsurfaceMask;
    // Note: ApplySubsurfaceScatteringTexturingMode also test the diffusionProfile for updating diffuseColor based on SSS
}

// Assume that bsdfData.diffusionProfile is init
void FillMaterialTransmission(float thickness, inout BSDFData bsdfData)
{
    int diffusionProfile = bsdfData.diffusionProfile;

    bsdfData.thickness = _ThicknessRemaps[diffusionProfile].x + _ThicknessRemaps[diffusionProfile].y * thickness;
    uint transmissionMode = BitFieldExtract(asuint(_TransmissionFlags), 2u * diffusionProfile, 2u);
    bsdfData.useThickObjectMode = transmissionMode != TRANSMISSION_MODE_THIN;

#if SHADEROPTIONS_USE_DISNEY_SSS
    bsdfData.transmittance = ComputeTransmittanceDisney(    _ShapeParams[diffusionProfile].rgb,
                                                            _TransmissionTintsAndFresnel0[diffusionProfile].rgb,
                                                            bsdfData.thickness);
#else
    bsdfData.transmittance = ComputeTransmittanceJimenez(   _HalfRcpVariancesAndWeights[diffusionProfile][0].rgb,
                                                            _HalfRcpVariancesAndWeights[diffusionProfile][0].a,
                                                            _HalfRcpVariancesAndWeights[diffusionProfile][1].rgb,
                                                            _HalfRcpVariancesAndWeights[diffusionProfile][1].a,
                                                            _TransmissionTintsAndFresnel0[diffusionProfile].rgb,
                                                            bsdfData.thickness);
#endif
}

// Assume bsdfData.normalWS is init
void FillMaterialAnisotropy(float anisotropy, float3 tangentWS, inout BSDFData bsdfData)
{
    bsdfData.anisotropy     = anisotropy;
    bsdfData.tangentWS      = tangentWS;
    bsdfData.bitangentWS    = cross(bsdfData.normalWS, bsdfData.tangentWS);
}

void FillMaterialIridescence(float thickness, inout BSDFData bsdfData)
{
    bsdfData.thickness = thickness;
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
    // Fresnel0 is deduced from interface between air and material (Assume to be 1.5 in Unity, or a metal).
    // but here we go from clear coat (1.5) to material, we need to update fresnel0
    // Note: Schlick is a poor approximation of Fresnel when ieta is 1 (1.5 / 1.5), schlick target 1.4 to 2.2 IOR.
    bsdfData.fresnel0 = ConvertF0ForAirInterfaceToF0ForClearCoat15(bsdfData.fresnel0);
}

void FillMaterialTransparencyData(float3 baseColor, float metallic, float ior, float3 transmittanceColor, float atDistance, float thickness, float transmittanceMask, inout BSDFData bsdfData)
{
    // Uses thickness from SSS's property set
    bsdfData.ior = ior;

    // IOR define the fresnel0 value, so update it also for consistency (and even if not physical we still need to take into account any metal mask)
    bsdfData.fresnel0 = lerp(IORToFresnel0(ior).xxx, baseColor, metallic);

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

void ApplyDebugToSurfaceData(inout SurfaceData surfaceData)
{
#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_SPECULAR_LIGHTING)
    {
        bool overrideSmoothness = _DebugLightingSmoothness.x != 0.0;
        float overrideSmoothnessValue = _DebugLightingSmoothness.y;

        if (overrideSmoothness)
        {
            surfaceData.perceptualSmoothness = overrideSmoothnessValue;
        }
    }

    if (_DebugLightingMode == DEBUGLIGHTINGMODE_DIFFUSE_LIGHTING)
    {
        surfaceData.baseColor = _DebugLightingAlbedo.xyz;
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
    ApplyDebugToSurfaceData(surfaceData);

    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    // IMPORTANT: In case of foward or gbuffer pass all enable flags are statically know at compile time, so the compiler can do compile time optimization
    bsdfData.enableSpecularColor        = surfaceData.enableSpecularColor;
    bsdfData.enableSubsurfaceScattering = surfaceData.enableSubsurfaceScattering;
    bsdfData.enableTransmission         = surfaceData.enableTransmission;
    bsdfData.enableAnisotropy           = surfaceData.enableAnisotropy;
    bsdfData.enableIridescence          = surfaceData.enableIridescence;
    bsdfData.enableClearCoat            = surfaceData.enableClearCoat;

    // Standard material
    bsdfData.specularOcclusion      = surfaceData.specularOcclusion;
    bsdfData.normalWS               = surfaceData.normalWS;
    bsdfData.perceptualRoughness    = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);

    // There is no mettalic with SSS and specular color mode
    float metallic = (bsdfData.enableSpecularColor || bsdfData.enableSubsurfaceScattering || bsdfData.enableTransmission || bsdfData.enableIridescence) ? 0.0 : surfaceData.metallic;
    bsdfData.diffuseColor           = ComputeDiffuseColor(surfaceData.baseColor, metallic);
    bsdfData.fresnel0               = bsdfData.enableSpecularColor ? surfaceData.specularColor : ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, DEFAULT_SPECULAR_VALUE);

    // Always assign even if not used, DIFFUSION_PROFILE_NEUTRAL_ID is 0
    bsdfData.diffusionProfile       = surfaceData.diffusionProfile;
    // Note: we have ZERO_INITIALIZE the struct so bsdfData.anisotropy == 0.0

    // In forward everything is statically know and we could theorically cumulate all the material features. So the code reflect it.
    // However in practice we keep parity between deferred and forward, so we should contrain the various features.
    // The UI is in charge of setuping the constrain not the code, so if users is forward only and want full power, it is easy to unleash by some UI change

    if (bsdfData.enableSubsurfaceScattering)
    {
        // Modify fresnel0
        FillMaterialSSS(surfaceData.subsurfaceMask, bsdfData);
    }

    if (bsdfData.enableTransmission)
    {
        FillMaterialTransmission(surfaceData.thickness, bsdfData);
    }

    if (bsdfData.enableAnisotropy)
    {
        FillMaterialAnisotropy(surfaceData.anisotropy, surfaceData.tangentWS, bsdfData);
    }

    if (bsdfData.enableIridescence)
    {
        FillMaterialIridescence(surfaceData.thicknessIrid, bsdfData);
    }

    if (bsdfData.enableClearCoat)
    {
        // Modify fresnel0 and perceptualRoughness
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
    ApplyDebugToSurfaceData(surfaceData);

    // RT0 - 8:8:8:8 sRGB
    outGBuffer0 = float4(surfaceData.baseColor, surfaceData.specularOcclusion);

    // RT1 - 10:10:10:2
    // We store perceptualRoughness instead of roughness because it save a sqrt ALU when decoding
    // (as we want both perceptualRoughness and roughness for the lighting due to Disney Diffuse model)
    // Encode normal on 20bit with oct compression + 2bit of sign
    float2 octNormalWS = PackNormalOctEncode(surfaceData.normalWS);
    // To have more precision encode the sign of xy in a separate uint
    uint octNormalSign = (octNormalWS.x < 0.0 ? 1 : 0) | (octNormalWS.y < 0.0 ? 2 : 0);
    // Store octNormalSign on two bits with perceptualRoughness
    outGBuffer1 = float4(abs(octNormalWS), PackFloatInt10bit(PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness), octNormalSign, 4.0), 0.0);

    // RT2 - 8:8:8:8
    // mettalic will be store on 4 bit and store special value when not used
    int metallic15 = int(surfaceData.metallic * (GBUFFER_LIT_ANISOTROPIC_UPPER_BOUND + 0.5)); // Remap to [0..12] range. 13, 14, 15 are special value

    // IMPORTANT: In case of foward or gbuffer pass materialFeatures is statically know at compile time, so the compiler can do compile time optimization
    // Currently material features SpecularColor, Iridescence, SubsurfaceScattering/Transmission, Anisotropy are mutually exclusive due to Gbuffer constrain
    // The priority of feature is handled in the code here and reflect in the UI (see LitUI.cs)

    // Process SSS and Transmission together as they encode almost the same data, negligible cost
    if (surfaceData.enableSubsurfaceScattering || surfaceData.enableTransmission)
    {
        metallic15 = GBUFFER_LIT_SSS_OR_TRANSMISSION;
        // Special case: For SSS we will store the profile id and the subsurface radius at the location of the specular occlusion (in alpha channel of GBuffer0)
        // and we will move the specular occlusion in GBuffer2. This is an optimization for SSSSS and have no other side effect as specular occlusion is always used
        // during lighting pass when other buffer (Gbuffer0, 1, 2) and read anyway.
        EncodeIntoSSSBuffer(ConvertSurfaceDataToSSSData(surfaceData), positionSS, outGBuffer0);
        outGBuffer2.rgb = float3(surfaceData.specularOcclusion, surfaceData.thickness, surfaceData.enableSubsurfaceScattering ? 1.0 : 0.0); // thickness for Transmission
    }
    else
    {
        if (surfaceData.enableSpecularColor)
        {
            metallic15 = GBUFFER_LIT_SPECULAR_COLOR;
            outGBuffer2.rgb = LinearToGamma20(surfaceData.specularColor);
        }
        else if (surfaceData.enableAnisotropy)
        {
            // Encode tangent on 16bit with oct compression
            float2 octTangentWS = PackNormalOctEncode(surfaceData.tangentWS);
            outGBuffer2.rgb = float3(octTangentWS * 0.5 + 0.5, surfaceData.anisotropy * 0.5 + 0.5);
        }
        else if (surfaceData.enableIridescence)
        {
            metallic15 = GBUFFER_LIT_IRIDESCENCE;
            outGBuffer2.rgb = float3(0.0, surfaceData.thicknessIrid, 0.0);
        }
        else
        {
            // Caution: Neutral value for anisotropy is 0.5 not 0
            outGBuffer2.rgb = float3(0.0, 0.5, 0.0);
        }
    }

    // Encode coatMask (4bit) / mettalic (4bit)
    outGBuffer2.a = PackFloatInt8bit(surfaceData.enableClearCoat ? surfaceData.coatMask : 0.0, metallic15, 16.0);

    // Lighting: 11:11:10f
    outGBuffer3 = float4(bakeDiffuseLighting, 0.0);
}

// This method allows us to know at compile time what material features should be removed from the code by Tile (Indepenently of the value of material feature flag per pixel).
// This is only useful for classification during lighting, so it's not needed in EncodeIntoGBuffer and ConvertSurfaceDataToBSDFData (where we always know exactly what the material feature is)
bool HasMaterialFeatureFlag(uint featureFlags, uint flag)
{
    return ((featureFlags & flag) != 0);
}

void DecodeFromGBuffer(uint2 positionSS, uint featureFlags, out BSDFData bsdfData, out float3 bakeDiffuseLighting, out uint materialFeatures)
{
    GBufferType0 inGBuffer0 = LOAD_TEXTURE2D(_GBufferTexture0, positionSS);
    GBufferType1 inGBuffer1 = LOAD_TEXTURE2D(_GBufferTexture1, positionSS);
    GBufferType2 inGBuffer2 = LOAD_TEXTURE2D(_GBufferTexture2, positionSS);
    GBufferType3 inGBuffer3 = LOAD_TEXTURE2D(_GBufferTexture3, positionSS);

    ZERO_INITIALIZE(BSDFData, bsdfData);

    // Init all material flags from Gbuffer2
    float coatMask;
    int metallic15;
    UnpackFloatInt8bit(inGBuffer2.a, 16.0, coatMask, metallic15);

    // If any of the flags of g_MaterialFeatureFlags (tested with HasMaterialFeatureFlag) is not set, it mean we know statically that
    // the material feature is not used, so the compiler can optimize related code. Else we don't know if the material feature will be use, it is a dynamic condition.
    bsdfData.enableSpecularColor =  (metallic15 == GBUFFER_LIT_SPECULAR_COLOR); // This is always a dynamic test as it is very cheap
    bsdfData.enableTransmission =   (metallic15 == GBUFFER_LIT_SSS_OR_TRANSMISSION && inGBuffer2.g > 0.0) && // Thickness > 0
                                    HasMaterialFeatureFlag(featureFlags, MATERIALFEATUREFLAGS_LIT_TRANSMISSION);
    bsdfData.enableSubsurfaceScattering =   (metallic15 == GBUFFER_LIT_SSS_OR_TRANSMISSION && inGBuffer2.b > 0.0) && // SSS Flags > 0
                                            HasMaterialFeatureFlag(featureFlags, MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING);
    bsdfData.enableAnisotropy =     (metallic15 <= GBUFFER_LIT_ANISOTROPIC_UPPER_BOUND && abs(inGBuffer2.g * 2.0 - 1.0) > 0.004) && // Anisotropy > 0
                                    HasMaterialFeatureFlag(featureFlags, MATERIALFEATUREFLAGS_LIT_ANISOTROPY);
    bsdfData.enableIridescence =    (metallic15 == GBUFFER_LIT_IRIDESCENCE) &&
                                    HasMaterialFeatureFlag(featureFlags, MATERIALFEATUREFLAGS_LIT_IRIDESCENCE);
    bsdfData.enableClearCoat =      coatMask > 0.0 &&
                                    HasMaterialFeatureFlag(featureFlags, MATERIALFEATUREFLAGS_LIT_CLEAR_COAT);

    // Init material feature (will be optimize out in all pass but classification
    materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD; // It is mandatory to identity ourselve as standard shader, else we are consider as sky/background element
    materialFeatures |= bsdfData.enableTransmission ? MATERIALFEATUREFLAGS_LIT_TRANSMISSION : 0;
    materialFeatures |= bsdfData.enableSubsurfaceScattering ? MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING : 0;
    materialFeatures |= bsdfData.enableAnisotropy ? MATERIALFEATUREFLAGS_LIT_ANISOTROPY : 0;
    materialFeatures |= bsdfData.enableIridescence ? MATERIALFEATUREFLAGS_LIT_IRIDESCENCE : 0;
    materialFeatures |= bsdfData.enableClearCoat ? MATERIALFEATUREFLAGS_LIT_CLEAR_COAT : 0;

    // Start decompressing GBuffer
    float3 baseColor = inGBuffer0.rgb;
    bsdfData.specularOcclusion = inGBuffer0.a;

    int octNormalSign;
    UnpackFloatInt10bit(inGBuffer1.b, 4.0, bsdfData.perceptualRoughness, octNormalSign);
    inGBuffer1.r = (octNormalSign & 1) ? -inGBuffer1.r : inGBuffer1.r;
    inGBuffer1.g = (octNormalSign & 2) ? -inGBuffer1.g : inGBuffer1.g;

    bsdfData.normalWS = UnpackNormalOctEncode(float2(inGBuffer1.r, inGBuffer1.g));

    // metallic15 is range [0..12] if mettallic data is needed
    float metallic = (metallic15 <= GBUFFER_LIT_ANISOTROPIC_UPPER_BOUND) ? metallic15 * (1.0 / GBUFFER_LIT_ANISOTROPIC_UPPER_BOUND) : 0.0;
    bsdfData.diffuseColor = ComputeDiffuseColor(baseColor, metallic);
    bsdfData.fresnel0 = bsdfData.enableSpecularColor ? Gamma20ToLinear(inGBuffer2.rgb) : ComputeFresnel0(baseColor, metallic, DEFAULT_SPECULAR_VALUE);

    // Always assign even if not used, DIFFUSION_PROFILE_NEUTRAL_ID is 0
    // Note: we have ZERO_INITIALIZE the struct, so bsdfData.diffusionProfile == DIFFUSION_PROFILE_NEUTRAL_ID, bsdfData.anisotropy == 0.0, bsdfData.SubsurfaceMask == 0.0 etc...

    // Process SSS and Transmission together as they encode almost the same data
    if (bsdfData.enableSubsurfaceScattering || bsdfData.enableTransmission)
    {
        // First we must extract the diffusion profile

        // Reminder: when using SSS we exchange specular occlusion and subsurfaceMask/profileID
        bsdfData.specularOcclusion = inGBuffer2.r;

        SSSData sssData;
        DecodeFromSSSBuffer(inGBuffer0, positionSS, sssData);

        bsdfData.diffusionProfile = sssData.diffusionProfile;

        if (bsdfData.enableSubsurfaceScattering)
        {
            // Modify fresnel0
            FillMaterialSSS(sssData.subsurfaceMask, bsdfData);
        }

        if (bsdfData.enableTransmission)
        {
            FillMaterialTransmission(inGBuffer0.g, bsdfData);
        }
    }

    // Special handling for anisotropy: When anisotropy is present in a tile, the whole tile will use anisotropy to avoid divergent evaluation of GGX that increase the cost
    if (HasMaterialFeatureFlag(featureFlags, MATERIALFEATUREFLAGS_LIT_ANISOTROPY))
    {
        float anisotropy;
        float3 tangentWS;

        if (bsdfData.enableAnisotropy)
        {
            anisotropy = inGBuffer2.b * 2.0 - 1.0;
            tangentWS  = UnpackNormalOctEncode(inGBuffer2.rg * 2.0 - 1.0);
        }
        else
        {
            anisotropy = 0.0;
            tangentWS = GetLocalFrame(bsdfData.normalWS)[0];
        }

        FillMaterialAnisotropy(anisotropy, tangentWS, bsdfData);

        // Force the compiler to use the anisotropy path all the time
        bsdfData.enableAnisotropy = true;
    }

    if (bsdfData.enableIridescence)
    {
        FillMaterialIridescence(inGBuffer2.g, bsdfData);
    }

    if (bsdfData.enableClearCoat)
    {
        // Modify fresnel0 and perceptualRoughness
        FillMaterialClearCoatData(coatMask, bsdfData);
    }

    // Note: the full code below (for both roughness) only execute when we have enableAnisotropy == true, otherwise as we only use roughnessT compiler will optimize out
    // Mean that in the worst case we always execute it.

    // roughnessT and roughnessB are clamped, and are meant to be used with punctual and directional lights.
    // perceptualRoughness is not clamped, and is meant to be used for IBL.
    // perceptualRoughness can be modify by FillMaterialClearCoatData, so ConvertAnisotropyToClampRoughness must be call after
    ConvertAnisotropyToClampRoughness(bsdfData.perceptualRoughness, bsdfData.anisotropy, bsdfData.roughnessT, bsdfData.roughnessB);

    bakeDiffuseLighting = inGBuffer3.rgb;
}

void DecodeFromGBuffer(uint2 positionSS, uint featureFlags, out BSDFData bsdfData, out float3 bakeDiffuseLighting)
{
    uint unsused;
    DecodeFromGBuffer(positionSS, featureFlags, bsdfData, bakeDiffuseLighting, unsused);
}

// Function call from the material classification compute shader
uint MaterialFeatureFlagsFromGBuffer(uint2 positionSS)
{
    BSDFData bsdfData;
    float3 unused;
    // Call the regular function, compiler will optimized out everything not used.
    // Note that all material feature flag bellow are in the same GBuffer (inGBuffer2) and thus material classification only sample one Gbuffer
    uint materialFeatures;
    DecodeFromGBuffer(positionSS, MATERIAL_FEATURE_MASK_FLAGS, bsdfData, unused, materialFeatures);

    return materialFeatures;
}


//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------

void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);
}

//-----------------------------------------------------------------------------
// PreLightData
//-----------------------------------------------------------------------------

// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    // General
    float clampNdotV; // clamped NdotV

    // GGX
    float partLambdaV;
    float energyCompensation;

    // IBL
    float3 iblR;                     // Dominant specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness;

    float3 specularFGD;                  // Store preconvoled BRDF for both specular and diffuse
    float diffuseFGD;

    // Area lights (17 VGPRs)
    // TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.
    float3x3 orthoBasisViewNormal; // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
    float3x3 ltcTransformDiffuse;  // Inverse transformation for Lambertian or Disney Diffuse        (4x VGPRs)
    float3x3 ltcTransformSpecular; // Inverse transformation for GGX                                 (4x VGPRs)
    float    ltcMagnitudeDiffuse;
    float3   ltcMagnitudeFresnel;

    // Clear coat
    float    coatPartLambdaV;
    float3   coatIblR;
    float    coatIblF;                 // Fresnel term for view vector
    float3x3 ltcTransformCoat;  // Inverse transformation for GGX                                 (4x VGPRs)
    float    ltcMagnitudeCoatFresnel;

    // Refraction
    float3 transparentRefractV;            // refracted view vector after exiting the shape
    float3 transparentPositionWS;          // start of the refracted ray after exiting the shape
    float3 transparentTransmittance;       // transmittance due to absorption
    float transparentSSMipLevel;           // mip level of the screen space gaussian pyramid for rough refraction
};

PreLightData GetPreLightData(float3 V, PositionInputs posInput, BSDFData bsdfData)
{
    PreLightData preLightData;
    ZERO_INITIALIZE(PreLightData, preLightData);

    float3 N = bsdfData.normalWS;
    float  NdotV = saturate(dot(N, V));
    preLightData.clampNdotV = NdotV; // Caution: The handling of edge cases where N is directed away from the screen is handled during Gbuffer/forward pass, so here do nothing
    preLightData.iblPerceptualRoughness = bsdfData.perceptualRoughness;

    if (bsdfData.enableClearCoat)
    {
        preLightData.coatPartLambdaV = GetSmithJointGGXPartLambdaV(NdotV, CLEAR_COAT_ROUGHNESS);
        preLightData.coatIblR = reflect(-V, N);
        preLightData.coatIblF = F_Schlick(CLEAR_COAT_F0, NdotV) * bsdfData.coatMask;
    }

    float3 iblN, iblR;

    // We avoid divergent evaluation of the GGX, as that nearly doubles the cost.
    // If the tile has anisotropy, all the pixels within the tile are evaluated as anisotropic.
    if (bsdfData.enableAnisotropy)
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
    float iblRoughness = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness);
    // Corretion of reflected direction for better handling of rough material
    preLightData.iblR = GetSpecularDominantDir(N, iblR, iblRoughness, NdotV);

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
    preLightData.orthoBasisViewNormal[0] = normalize(V - N * NdotV);
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

    if (bsdfData.enableClearCoat)
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
    if (bsdfData.enableSubsurfaceScattering)
    {
        bsdfData.diffuseColor = ApplySubsurfaceScatteringTexturingMode(bsdfData.diffuseColor, bsdfData.diffusionProfile);
    }

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

bool HaveSubsurfaceScattering(BSDFData bsdfData)
{
    return (_EnableSubsurfaceScattering != 0) && bsdfData.enableSubsurfaceScattering;
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

// This function apply BSDF
void BSDF(  float3 V, float3 L, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
            out float3 diffuseLighting,
            out float3 specularLighting)
{
    float3 N = bsdfData.normalWS;

    float NdotV = preLightData.clampNdotV;
    // Optimized math. Ref: PBR Diffuse Lighting for GGX + Smith Microsurfaces (slide 114).
    float NdotL = saturate(dot(N, L)); // Must have the same value without the clamp

    float LdotV = dot(L, V);
    float invLenLV = rsqrt(max(2.0 * LdotV + 2.0, FLT_EPS));  // invLenLV = rcp(length(L + V)) - caution about the case where V and L are opposite, it can happen, use max to avoid this
    float NdotH = saturate((NdotL + NdotV) * invLenLV);
    float LdotH = saturate(invLenLV * LdotV + invLenLV);

    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);
    float DV;

    if (bsdfData.enableAnisotropy)
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

    if (bsdfData.enableClearCoat)
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
        diffuseLighting *= F_Schlick(CLEAR_COAT_F0, NdotL);
    }
}

// In the "thin object" mode (for cards), we assume that the geometry is very thin.
// We apply wrapped lighting to compensate for that, and do not modify the shading position.
// Otherwise, in the "thick object" mode, we can have EITHER reflected (front) lighting
// OR transmitted (back) lighting, never both at the same time. For transmitted lighting,
// we need to push the shading position back to avoid self-shadowing problems.
float3 ComputeThicknessDisplacement(BSDFData bsdfData, float3 L, float NdotL)
{
    float displacement = 0;

    [flatten] if (bsdfData.useThickObjectMode && NdotL < 0)
    {
        // Compute the thickness in world units along the normal.
        float thicknessInMeters = bsdfData.thickness * METERS_PER_MILLIMETER;
        float thicknessInUnits  = thicknessInMeters * _WorldScales[bsdfData.diffusionProfile].y;

        // Compute the thickness in world units along the light vector.
        displacement = thicknessInUnits / -NdotL;
    }

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
float3 EvaluateTransmission(BSDFData bsdfData, float NdotL, float NdotV, float attenuation)
{
    float wrappedNdotL = ComputeWrappedDiffuseLighting(-NdotL, SSS_WRAP_LIGHT);
    float negatedNdotL = saturate(-NdotL);

    // Apply wrapped lighting to better handle thin objects (cards) at grazing angles.
    float backNdotL = bsdfData.useThickObjectMode ? negatedNdotL : wrappedNdotL;

    // Apply BSDF-specific diffuse transmission to attenuation. See also: [SSS-NOTE-TRSM]
    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    attenuation *= Lambert();
#else
    attenuation *= INV_PI * F_Transm_Schlick(0, 0.5, NdotV) * F_Transm_Schlick(0, 0.5, backNdotL);
#endif

    float intensity = attenuation * backNdotL;

    return intensity * bsdfData.transmittance;
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

    // Compute displacement for fake thickObject transmission
    posInput.positionWS += ComputeThicknessDisplacement(bsdfData, L, NdotL);

    float3 color;
    float attenuation;
    EvaluateLight_Directional(lightLoopContext, posInput, lightData, bakeLightingData, N, L, color, attenuation);

    float intensity = attenuation * saturate(NdotL);

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are ok with this
    [branch] if (intensity > 0.0)
    {
        BSDF(V, L, posInput.positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * lightData.diffuseScale;
        lighting.specular *= intensity * lightData.specularScale;
    }

    [branch] if (bsdfData.enableTransmission)
    {
        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        lighting.diffuse += EvaluateTransmission(bsdfData, NdotL, preLightData.clampNdotV, attenuation * lightData.diffuseScale);
    }

    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

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

    float3 unL     = (lightType != GPULIGHTTYPE_PROJECTOR_BOX) ? -lightToSample : -lightData.forward;
    float  distSq  = dot(unL, unL);
    float  distRcp = rsqrt(distSq);
    float  dist    = distSq * distRcp;
    float3 N       = bsdfData.normalWS;
    float3 L       = unL * distRcp;
    float  NdotL   = dot(N, L);

    // Compute displacement for fake thickObject transmission
    posInput.positionWS += ComputeThicknessDisplacement(bsdfData, L, NdotL);

    float3 color;
    float attenuation;
    EvaluateLight_Punctual(lightLoopContext, posInput, lightData, bakeLightingData, N, L, dist, distSq, color, attenuation);

    float intensity = attenuation * saturate(NdotL);

    // Note: We use NdotL here to early out, but in case of clear coat this is not correct. But we are ok with this
    [branch] if (intensity > 0.0)
    {
        // Simulate a sphere light with this hack
        // Note that it is not correct with our pre-computation of PartLambdaV (mean if we disable the optimization we will not have the
        // same result) but we don't care as it is a hack anyway
        bsdfData.coatRoughness = max(bsdfData.coatRoughness, lightData.minRoughness);
        bsdfData.roughnessT = max(bsdfData.roughnessT, lightData.minRoughness);
        bsdfData.roughnessB = max(bsdfData.roughnessB, lightData.minRoughness);

        BSDF(V, L, posInput.positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * lightData.diffuseScale;
        lighting.specular *= intensity * lightData.specularScale;
    }

    [branch] if (bsdfData.enableTransmission)
    {
        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        lighting.diffuse += EvaluateTransmission(bsdfData, NdotL, preLightData.clampNdotV, attenuation * lightData.diffuseScale);
    }

    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

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
    float invAspectRatio = radius / (radius + (0.5 * len));

    // Compute the light attenuation.
    float intensity = GetEllipsoidalDistanceAttenuation(unL,  lightData.invSqrAttenuationRadius,
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

    [branch] if (bsdfData.enableTransmission)
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
    if (bsdfData.enableClearCoat)
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
    float intensity = GetEllipsoidalDistanceAttenuation(unL, invHalfDim);
#else
    // The attenuation volume is an axis-aligned box s.t.
    // hX = (r + w / 2), hY = (r + h / 2), hZ = r.
    float intensity = GetBoxDistanceAttenuation(unL, invHalfDim);
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

    [branch] if (bsdfData.enableTransmission)
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
    if (bsdfData.enableClearCoat)
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
#endif // LIT_DISPLAY_REFERENCE_AREA

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_SSLighting for screen space lighting
// ----------------------------------------------------------------------------

IndirectLighting EvaluateBSDF_SSReflection(LightLoopContext lightLoopContext,
                                            float3 V, PositionInputs posInput,
                                            PreLightData preLightData, BSDFData bsdfData,
                                            inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    // TODO

    return lighting;
}

IndirectLighting EvaluateBSDF_SSRefraction(LightLoopContext lightLoopContext,
                                            float3 V, PositionInputs posInput,
                                            PreLightData preLightData, BSDFData bsdfData,
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
    lighting.specularTransmitted = SAMPLE_TEXTURE2D_LOD(_GaussianPyramidColorTexture, s_trilinear_clamp_sampler, refractedBackPointNDC, preLightData.transparentSSMipLevel).rgb;

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
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
IndirectLighting EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData, int envShapeType, int GPUImageBasedLightingType,
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

    // CAUTION: localToWorld is the transform use to convert the cubemap capture point to world space (mean it include the offset)
    // the center of the bounding box is thus in locals space: positionLS - offsetLS
    // We use this formulation as it is the one of legacy unity that was using only AABB box.
    float3x3 worldToLocal = transpose(float3x3(lightData.right, lightData.up, lightData.forward)); // worldToLocal assume no scaling
    float3 positionLS = positionWS - lightData.positionWS;
    positionLS = mul(positionLS, worldToLocal).xyz - lightData.offsetLS; // We want to calculate the intersection from the center of the bounding box.

    // Note: using envShapeType instead of lightData.envShapeType allow to make compiler optimization in case the type is know (like for sky)
    if (envShapeType == ENVSHAPETYPE_SPHERE)
    {
        // 1. First process the projection
        float3 dirLS = mul(R, worldToLocal);
        float sphereOuterDistance = lightData.influenceExtents.x;

        float projectionDistance = SphereRayIntersectSimple(positionLS, dirLS, sphereOuterDistance);
        projectionDistance = max(projectionDistance, lightData.minProjectionDistance); // Setup projection to infinite if requested (mean no projection shape)
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in lightData.positionWS
        R = (positionWS + projectionDistance * R) - lightData.positionWS;

        // Test again for clear code
        if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION && bsdfData.enableClearCoat)
        {
            dirLS = mul(coatR, worldToLocal);
            projectionDistance = SphereRayIntersectSimple(positionLS, dirLS, sphereOuterDistance);
            projectionDistance = max(projectionDistance, lightData.minProjectionDistance); // Setup projection to infinite if requested (mean no projection shape)
            coatR = (positionWS + projectionDistance * coatR) - lightData.positionWS;
        }

        // 2. Process the position influence
        float lengthPositionLS = length(positionLS);
        float sphereInfluenceDistance = lightData.influenceExtents.x - lightData.blendDistancePositive.x;
        float distFade = max(lengthPositionLS - sphereInfluenceDistance, 0.0);
        float alpha = saturate(1.0 - distFade / max(lightData.blendDistancePositive.x, 0.0001)); // avoid divide by zero

#if defined(ENVMAP_FEATURE_INFLUENCENORMAL)
        // 3. Process the normal influence
        float insideInfluenceNormalVolume = lengthPositionLS <= (lightData.influenceExtents.x - lightData.blendNormalDistancePositive.x) ? 1.0 : 0.0;
        float insideWeight = InfluenceFadeNormalWeight(bsdfData.normalWS, normalize(positionWS - lightData.positionWS));
        alpha *= insideInfluenceNormalVolume ? 1.0 : insideWeight;
#endif

        weight = alpha;
    }
    else if (envShapeType == ENVSHAPETYPE_BOX)
    {
        // 1. First process the projection
        float3 dirLS = mul(R, worldToLocal);
        float3 boxOuterDistance = lightData.influenceExtents;
        float projectionDistance = BoxRayIntersectSimple(positionLS, dirLS, -boxOuterDistance, boxOuterDistance);
        projectionDistance = max(projectionDistance, lightData.minProjectionDistance); // Setup projection to infinite if requested (mean no projection shape)

        // No need to normalize for fetching cubemap
        // We can reuse dist calculate in LS directly in WS as there is no scaling. Also the offset is already include in lightData.positionWS
        R = (positionWS + projectionDistance * R) - lightData.positionWS;

        // TODO: add distance based roughness

        // Test again for clear code
        if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION && bsdfData.enableClearCoat)
        {
            dirLS = mul(coatR, worldToLocal);
            projectionDistance = BoxRayIntersectSimple(positionLS, dirLS, -boxOuterDistance, boxOuterDistance);
            projectionDistance = max(projectionDistance, lightData.minProjectionDistance); // Setup projection to infinite if requested (mean no projection shape)
            coatR = (positionWS + projectionDistance * coatR) - lightData.positionWS;
        }

        // 2. Process the position influence
        // Calculate falloff value, so reflections on the edges of the volume would gradually blend to previous reflection.

#if defined(ENVMAP_FEATURE_PERFACEINFLUENCE) || defined(ENVMAP_FEATURE_INFLUENCENORMAL) || defined(ENVMAP_FEATURE_PERFACEFADE)
        // Distance to each cube face
        float3 negativeDistance = boxOuterDistance + positionLS;
        float3 positiveDistance = boxOuterDistance - positionLS;
#endif

#if defined(ENVMAP_FEATURE_PERFACEINFLUENCE)
        // Influence falloff for each face
        float3 negativeFalloff = negativeDistance / max(0.0001, lightData.blendDistanceNegative);
        float3 positiveFalloff = positiveDistance / max(0.0001, lightData.blendDistancePositive);

        // Fallof is the min for all faces
        float influenceFalloff = min(
            min(min(negativeFalloff.x, negativeFalloff.y), negativeFalloff.z),
            min(min(positiveFalloff.x, positiveFalloff.y), positiveFalloff.z));

        float alpha = saturate(influenceFalloff);
#else
        float distFace = DistancePointBox(positionLS, -lightData.influenceExtents + lightData.blendDistancePositive.x, lightData.influenceExtents - lightData.blendDistancePositive.x);
        float alpha = saturate(1.0 - distFace / max(lightData.blendDistancePositive.x, 0.0001));
#endif

#if defined(ENVMAP_FEATURE_INFLUENCENORMAL)
        // 3. Process the normal influence
        // Calculate a falloff value to discard normals pointing outward the center of the environment light
        float3 belowPositiveInfluenceNormalVolume = positiveDistance / max(0.0001, lightData.blendNormalDistancePositive);
        float3 aboveNegativeInfluenceNormalVolume = negativeDistance / max(0.0001, lightData.blendNormalDistanceNegative);
        float insideInfluenceNormalVolume = all(belowPositiveInfluenceNormalVolume >= 1.0) && all(aboveNegativeInfluenceNormalVolume >= 1.0) ? 1.0 : 0;
        float insideWeight = InfluenceFadeNormalWeight(bsdfData.normalWS, normalize(positionWS - lightData.positionWS));
        alpha *= insideInfluenceNormalVolume ? 1.0 : insideWeight;
#endif

#if defined(ENVMAP_FEATURE_PERFACEFADE)
        // 4. Fade specific cubemap faces
        // For each axes (both positive and negative ones), we want to fade from the center of one face to another
        // So we normalized the sample direction (R) and use its component to fade for each axis
        // We consider R.x as cos(X) and then fade as angle from 60°(=acos(1/2)) to 75°(=acos(1/4))
        // For positive axes: axisFade = (R - 1/4) / (1/2 - 1/4)
        // <=> axisFace = 4 * R - 1;
        R = normalize(R);
        float3 faceFade = saturate((4 * R - 1) * lightData.boxSideFadePositive) + saturate((-4 * R - 1) * lightData.boxSideFadeNegative);
        alpha *= saturate(faceFade.x + faceFade.y + faceFade.z);
#endif

        weight = alpha;
    }

    // Smooth weighting
    weight = Smoothstep01(weight);

    // When we are rough, we tend to see outward shifting of the reflection when at the boundary of the projection volume
    // Also it appear like more sharp. To avoid these artifact and at the same time get better match to reference we lerp to original unmodified reflection.
    // Formula is empirical.
    float roughness = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness);
    R = lerp(R, preLightData.iblR, saturate(smoothstep(0, 1, roughness * roughness)));

    float3 F = preLightData.specularFGD;
    float iblMipLevel = PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness);
    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, iblMipLevel);

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
    {
        envLighting = F * preLD.rgb;

        // Evaluate the Clear Coat component if needed
        if (bsdfData.enableClearCoat)
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
    float specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(preLightData.clampNdotV, indirectAmbientOcclusion, roughness);
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

    float3 modifiedDiffuseColor;
    if (bsdfData.enableSubsurfaceScattering)
        modifiedDiffuseColor = ApplySubsurfaceScatteringTexturingMode(bsdfData.diffuseColor, bsdfData.diffusionProfile);
    else
        modifiedDiffuseColor = bsdfData.diffuseColor;

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
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_INDIRECT_DIFFUSE_OCCLUSION_FROM_SSAO)
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
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
