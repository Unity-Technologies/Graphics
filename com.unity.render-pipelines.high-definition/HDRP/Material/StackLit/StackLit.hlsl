//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in StackLit.cs which generates StackLit.cs.hlsl
#include "StackLit.cs.hlsl"
#include "../SubsurfaceScattering/SubsurfaceScattering.hlsl"
#include "../NormalBuffer.hlsl"
#include "CoreRP/ShaderLibrary/VolumeRendering.hlsl"

//NEWLITTODO : wireup CBUFFERs for uniforms and samplers used:
//
// We need this for AO, Depth/Color pyramids, LTC lights data, FGD pre-integrated data.
//
// Also add options at the top of this file, see Lit.hlsl.

//-----------------------------------------------------------------------------
// Texture and constant buffer declaration
//-----------------------------------------------------------------------------

// Required for SSS, GBuffer texture declaration
TEXTURE2D(_GBufferTexture0);

// Declare the BSDF specific FGD property and its fetching function
#include "../PreIntegratedFGD/PreIntegratedFGD.hlsl"

//-----------------------------------------------------------------------------
// Definition
//-----------------------------------------------------------------------------

// Needed for MATERIAL_FEATURE_MASK_FLAGS.
#include "../../Lighting/LightLoop/LightLoop.cs.hlsl"

// Additional bits set in 'bsdfData.materialFeatures' to save registers and simplify feature tracking.
#define MATERIAL_FEATURE_FLAGS_SSS_OUTPUT_SPLIT_LIGHTING         ((MATERIAL_FEATURE_MASK_FLAGS + 1) << 0)
#define MATERIAL_FEATURE_FLAGS_SSS_TEXTURING_MODE_OFFSET FastLog2((MATERIAL_FEATURE_MASK_FLAGS + 1) << 1) // 2 bits
#define MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_MIXED_THICKNESS ((MATERIAL_FEATURE_MASK_FLAGS + 1) << 3)

//
// Vertically Layered BSDF : "vlayering"
//


//#define _STACKLIT_DEBUG

#ifdef _STACKLIT_DEBUG
#    define IF_DEBUG(a) a
#    define VLAYERED_DEBUG
#else
#    define IF_DEBUG(a)
#endif

// Vlayer config options:

#ifdef _VLAYERED_RECOMPUTE_PERLIGHT
#define VLAYERED_RECOMPUTE_PERLIGHT
// Now a shader_features
// probably too slow but just to check the difference it makes
#endif

#ifdef _VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE
#define VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE
#endif

#define VLAYERED_DIFFUSE_ENERGY_HACKED_TERM
//#define VLAYERED_ANISOTROPY_IBL_DESTRETCH

#define VLAYERED_ANISOTROPY_SCALAR_ROUGHNESS
#define VLAYERED_ANISOTROPY_SCALAR_ROUGHNESS_CORRECTANISO

// Automatic:

// Mostly for struct array declarations, not really loops:
#ifdef _MATERIAL_FEATURE_COAT

#    define COAT_NB_LOBES 1
#    define COAT_LOBE_IDX 0
#    define BASE_LOBEA_IDX (COAT_LOBE_IDX+1)
#    define BASE_LOBEB_IDX (BASE_LOBEA_IDX+1)

#ifdef _MATERIAL_FEATURE_COAT_NORMALMAP
#    define NB_NORMALS 2 // NB of interfaces with different normals (for additional clear coat normal map)
#else
#    define NB_NORMALS 1
#endif

#if defined(VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE)
#    define NB_LV_DIR 2 // NB of interfaces with different L or V directions to consider (see BSDF( ) )
#else
#    define NB_LV_DIR 1
#endif

#    define IF_FEATURE_COAT(a) a

#else // ! _MATERIAL_FEATURE_COAT

#    undef VLAYERED_RECOMPUTE_PERLIGHT
#    undef VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE
#    undef _MATERIAL_FEATURE_COAT_NORMALMAP // enforce a "coat enabled subfeature" condition on this shader_feature

#    define COAT_NB_LOBES 0
#    define COAT_LOBE_IDX 0
#    define BASE_LOBEA_IDX 0
#    define BASE_LOBEB_IDX (BASE_LOBEA_IDX+1)
#    define NB_NORMALS 1
#    define NB_LV_DIR 1

#    define IF_FEATURE_COAT(a)

#endif // #ifdef _MATERIAL_FEATURE_COAT

// For NB_NORMALS arrays:
#define COAT_NORMAL_IDX 0
#define BASE_NORMAL_IDX (NB_NORMALS-1)
// For NB_LV_DIR arrays: make sure these indices match the above
#define TOP_DIR_IDX 0
#define BOTTOM_DIR_IDX (NB_LV_DIR-1)

// TODO: if dual lobe base
//#define BASE_NB_LOBES 1
#define BASE_NB_LOBES 2
#define TOTAL_NB_LOBES (BASE_NB_LOBES+COAT_NB_LOBES)


// TODO CLEANUP and put in proper define above
// Also, note that we have lobe-indexed arrays,
// and vlayer indexed for the generic vlayer
// ComputeAdding loop

#define NB_VLAYERS 3
// Use these to index vLayerEnergyCoeff[] !
// vLayer 1 is useless...
#define TOP_VLAYER_IDX 0
#define BOTTOM_VLAYER_IDX 2


//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------
#define LIT_DIFFUSE_LAMBERT_BRDF // TODO Disney Diffuse
#define LIT_USE_GGX_ENERGY_COMPENSATION



//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

// This method allows us to know at compile time what material features should be removed from the code by Tile (Indepenently of the value of material feature flag per pixel).
// This is only useful for classification during lighting, so it's not needed in EncodeIntoGBuffer and ConvertSurfaceDataToBSDFData (where we always know exactly what the material feature is)
bool HasFeatureFlag(uint featureFlags, uint flag)
{
    return ((featureFlags & flag) != 0);
}

// The only way to get Coat now is with vlayering
bool IsVLayeredEnabled(BSDFData bsdfData)
{
    return (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT));
}

bool IsCoatNormalMapEnabled(BSDFData bsdfData)
{
    return (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT_NORMAL_MAP));
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

    bsdfData.materialFeatures |= useThinObjectMode ? 0 : MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_MIXED_THICKNESS;

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

void FillMaterialCoatData(float coatPerceptualRoughness, float coatIor, float coatThickness, float3 coatExtinction, inout BSDFData bsdfData)
{
    bsdfData.coatPerceptualRoughness = coatPerceptualRoughness;
    bsdfData.coatIor        = coatIor;
    bsdfData.coatThickness  = coatThickness;
    bsdfData.coatExtinction = coatExtinction;
}

float GetCoatEta(in BSDFData bsdfData)
{
    float eta = bsdfData.coatIor / 1.0;
    //float eta = 1.5 / 1.0;
    //ieta = 1.0 / eta;
    return eta;
}

float3 ConvertF0ForAirInterfaceToF0ForNewTopIor(float3 fresnel0, float newTopIor)
{
    float3 ior = Fresnel0ToIor(fresnel0 + 0.0001); // guard against 1.0
    return IorToFresnel0(ior, newTopIor);
}

float CalculateEnergyCompensationFromSpecularReflectivity(float specularReflectivity)
{
    // Ref: Practical multiple scattering compensation for microfacet models.
    // We only apply the formulation for metals.
    // For dielectrics, the change of reflectance is negligible.
    // We deem the intensity difference of a couple of percent for high values of roughness
    // to not be worth the cost of another precomputed table.
    // Note: this formulation bakes the BSDF non-symmetric!

    // Note that using this factor for all specular lighting assumes all
    // BSDFs are from GGX.
    // (That's the FGD we use above to get integral[BSDF/F (N.w) dw] )

    // Make it roughly usable with a lerp factor with - 1.0, see ApplyEnergyCompensationToSpecularLighting()
    // The "lerp factor" will be fresnel0
    float energyCompensation = 1.0 / specularReflectivity - 1.0;
    return energyCompensation;
}

// Use fresnel0 as a lerp factor for energy compensation (if 0, none applied)
float3 ApplyEnergyCompensationToSpecularLighting(float3 specularLighting, float3 fresnel0, float energyCompensation)
{
    // Apply the fudge factor (boost) to compensate for multiple scattering not accounted for in the BSDF.
    // This assumes all spec comes from a GGX BSDF.
    specularLighting *= 1.0 + fresnel0 * energyCompensation;
    return specularLighting;
}

float3 GetEnergyCompensationFactor(float specularReflectivity, float3 fresnel0)
{
    float ec = CalculateEnergyCompensationFromSpecularReflectivity(specularReflectivity);
    return ApplyEnergyCompensationToSpecularLighting(float3(1.0, 1.0, 1.0), fresnel0, ec);
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
        surfaceData.perceptualSmoothnessA = overrideSmoothnessValue;
        surfaceData.perceptualSmoothnessB = overrideSmoothnessValue;
        surfaceData.coatPerceptualSmoothness = overrideSmoothnessValue;
    }

    if (overrideNormal)
    {
        surfaceData.normalWS = worldToTangent[2];
    }
#endif
}

// This function is similar to ApplyDebugToSurfaceData but for BSDFData
//
// NOTE:
//
// This will be available and used in ShaderPassForward.hlsl since in StackLit.shader,
// just before including the core code of the pass (ShaderPassForward.hlsl) we include
// Material.hlsl (or Lighting.hlsl which includes it) which in turn includes us,
// StackLit.shader, via the #if defined(UNITY_MATERIAL_*) glue mechanism.
//
void ApplyDebugToBSDFData(inout BSDFData bsdfData)
{
#ifdef DEBUG_DISPLAY
    // Override value if requested by user
    // this can be use also in case of debug lighting mode like specular only

    //NEWLITTODO
    //bool overrideSpecularColor = _DebugLightingSpecularColor.x != 0.0;

    //if (overrideSpecularColor)
    //{
    //   float3 overrideSpecularColor = _DebugLightingSpecularColor.yzw;
    //    bsdfData.fresnel0 = overrideSpecularColor;
    //}
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

NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    NormalData normalData;

    // When using clear cloat we want to use the coat normal for the various deferred effect
    // as it is the most dominant one
    if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT))
    {
        normalData.normalWS = surfaceData.coatNormalWS;
        normalData.perceptualRoughness = surfaceData.coatPerceptualSmoothness;
    }
    else
    {
        normalData.normalWS = surfaceData.normalWS;
        // Do average mix in case of dual lobe
        normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(lerp(surfaceData.perceptualSmoothnessA, surfaceData.perceptualSmoothnessB, surfaceData.lobeMix));
    }

    return normalData;
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    // IMPORTANT: In our forward only case, all enable flags are statically know at compile time, so the compiler can do compile time optimization
    bsdfData.materialFeatures = surfaceData.materialFeatures;

    bsdfData.geomNormalWS = surfaceData.geomNormalWS; // We should always have this whether we enable coat normals or not.

    // Two lobe base material
    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.perceptualRoughnessA = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothnessA);
    bsdfData.perceptualRoughnessB = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothnessB);

    bsdfData.lobeMix = surfaceData.lobeMix;

    // There is no metallic with SSS and specular color mode
    float metallic = HasFeatureFlag(surfaceData.materialFeatures, /*MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR |*/ MATERIALFEATUREFLAGS_STACK_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION) ? 0.0 : surfaceData.metallic;

    bsdfData.diffuseColor = ComputeDiffuseColor(surfaceData.baseColor, metallic);
    bsdfData.fresnel0 = ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, IorToFresnel0(surfaceData.dielectricIor));

    // Kind of obsolete without gbuffer, ie could use _MATERIAL_FEATURE* shader_features directly, but
    // if anything, makes the code more readable.
    if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_SUBSURFACE_SCATTERING))
    {
        // Assign profile id and overwrite fresnel0
        FillMaterialSSS(surfaceData.diffusionProfile, surfaceData.subsurfaceMask, bsdfData);
    }

    if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION))
    {
        // Assign profile id and overwrite fresnel0
        FillMaterialTransmission(surfaceData.diffusionProfile, surfaceData.thickness, bsdfData);
    }

    if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
    {
        FillMaterialAnisotropy(surfaceData.anisotropy, surfaceData.tangentWS, cross(surfaceData.normalWS, surfaceData.tangentWS), bsdfData);
    }

    if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT))
    {
        FillMaterialCoatData(PerceptualSmoothnessToPerceptualRoughness(surfaceData.coatPerceptualSmoothness),
                             surfaceData.coatIor, surfaceData.coatThickness, surfaceData.coatExtinction, bsdfData);

        if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT_NORMAL_MAP))
        {
            bsdfData.coatNormalWS = surfaceData.coatNormalWS;
        }

        // vlayering:
        // We can't calculate final roughnesses including anisotropy right away in this case: we will either do it
        // one time at GetPreLightData or for each light depending on the configuration for accuracy of BSDF
        // vlayering statistics (ie if VLAYERED_RECOMPUTE_PERLIGHT)

        // We have a coat top layer: change the base fresnel0 accordingdly:
        bsdfData.fresnel0 = ConvertF0ForAirInterfaceToF0ForNewTopIor(bsdfData.fresnel0, bsdfData.coatIor);

        // Dont clamp the roughnesses for now, ComputeAdding() will use those directly:
        // (don't forget to call ClampRoughnessForAnalyticalLights after though)
        ConvertAnisotropyToRoughness(bsdfData.perceptualRoughnessA, bsdfData.anisotropy, bsdfData.roughnessAT, bsdfData.roughnessAB);
        ConvertAnisotropyToRoughness(bsdfData.perceptualRoughnessB, bsdfData.anisotropy, bsdfData.roughnessBT, bsdfData.roughnessBB);
        bsdfData.coatRoughness = PerceptualRoughnessToRoughness(bsdfData.coatPerceptualRoughness);
    }
    else
    {
        // roughnessT and roughnessB are clamped, and are meant to be used with punctual and directional lights.
        // perceptualRoughness is not clamped, and is meant to be used for IBL.
        // TODO: add tangent map for anisotropy;
        ConvertAnisotropyToClampRoughness(bsdfData.perceptualRoughnessA, bsdfData.anisotropy, bsdfData.roughnessAT, bsdfData.roughnessAB);
        ConvertAnisotropyToClampRoughness(bsdfData.perceptualRoughnessB, bsdfData.anisotropy, bsdfData.roughnessBT, bsdfData.roughnessBB);
    }

    bsdfData.ambientOcclusion = surfaceData.ambientOcclusion;

    ApplyDebugToBSDFData(bsdfData);
    return bsdfData;
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
        case DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL_VIEW_SPACE:
            // Convert to view space
            result = TransformWorldToViewDir(surfaceData.normalWS) * 0.5 + 0.5;
            break;
    }
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
        case DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_VIEW_SPACE:
            // Convert to view space
            result = TransformWorldToViewDir(bsdfData.normalWS) * 0.5 + 0.5;
            break;
    }
}


//-----------------------------------------------------------------------------
// PreLightData
//
// Make sure we respect naming conventions to reuse ShaderPassForward as is,
// ie struct (even if opaque to the ShaderPassForward) name is PreLightData,
// GetPreLightData prototype.
//-----------------------------------------------------------------------------


// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    float NdotV[NB_NORMALS];                  // Could be negative due to normal mapping, use ClampNdotV()
    float geomNdotV;
    float bottomAngleFGD;
    float TdotV;                              // Stored only when VLAYERED_RECOMPUTE_PERLIGHT
    float BdotV;

    // IBL: we calculate and prefetch the pre-integrated split sum data for
    // all needed lobes
    float3 iblR[TOTAL_NB_LOBES];              // Dominant specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness[TOTAL_NB_LOBES];
    // IBL precalculation code modifies the perceptual roughnesses, so we need those too.
    // For analytical lights, clamping is needed (epsilon instead of 0 roughness, which
    // would be troublesome to use for perfectly smooth IBL reflections, and roughness
    // is split by anisotropy, while IBL anisotropy is delt with a hack on the used iblR
    // vector, with the non-anisotropic roughness).

    float3 specularFGD[TOTAL_NB_LOBES];       // Store preconvoled BSDF for both specular and diffuse

    float  diffuseFGD;

    // cf with Lit.hlsl: originally, it has a coat section with its own
    // iblR, iblF, PartLambdaV: this is due to the fact that it was the only
    // extra lobe, and simplified: we didn't want to pay the cost of an FGD fetch
    // for it (hence the name iblF in Lit). Here, we will fold all data into
    // lobe-indexed arrays.


    // For clarity, we will dump the base layer lobes roughnesses used by analytical lights
    // here, to avoid confusion with the per-vlayer (vs per lobe) vLayerPerceptualRoughness
    // (also, those don't need to be anisotropic for all lobes but the non-separated
    // original roughnesses are still useful for all lobes because of the IBL hack)
    //
    // We don't reuse the BSDFData roughnessAT/AB/BT/BB because we might need the original
    // values per light (ie not only once at GetPreLightData time) to recompute new roughnesses
    // if we use VLAYERED_RECOMPUTE_PERLIGHT.
    float  layeredRoughnessT[BASE_NB_LOBES];
    float  layeredRoughnessB[BASE_NB_LOBES];
    float  layeredCoatRoughness;
    // For consistency with nonperceptual anisotropic and clamped roughnessAT/AB/BT/BB
    // which are stored in BSDFData, coatRoughness (for analytical lights) will
    // also be stored in BSDFData.

    float  iblAnisotropy[BASE_NB_LOBES];


    // GGX
    float partLambdaV[TOTAL_NB_LOBES];        // Depends on N, V, roughness

    // TODO: If we use VLAYERED_RECOMPUTE_PERLIGHT, we need to recalculate those also.
    // (ComputeAdding changing roughness per light is what will make them change).
    //
    // This used to be strictly done in GetPreLightData, but since this is NOT useful
    // for IBLs, if vlayering is enabled and we want the vlayer stats recomputation
    // per analytical light, we must NOT do it in GetPreLightData (will be wasted) and
    // (in effect can't be precalculated for all analytical lights).
    //
    // In short: only valid and precalculated at GetPreLightData time if vlayering is disabled.
    //

    float coatIeta;

    // For IBLs (and analytical lights if approximation is used)

    float3 vLayerEnergyCoeff[NB_VLAYERS];
    //float vLayerPerceptualRoughness[NB_VLAYERS];

    // TODOENERGY:
    // For the vlayered case, fold compensation into FGD terms during ComputeAdding
    // (ie FGD becomes FGDinf) (but the approximation depends on f0, our FGD is scalar,
    // not rgb, see GetEnergyCompensationFactor.)

    // (see ApplyEnergyCompensationToSpecularLighting)
    // We will compute float3 energy factors per lobe.
    // We will duplicate one entry to simplify the IBL loop (In general it's either that or
    // we add branches (if lobe from bottom interface or top inteface)
    // (All our loops for lobes are static so either way the compiler should unroll and remove
    // either duplicated storage or the branch.)
    float3 energyCompensationFactor[TOTAL_NB_LOBES];


    //See VLAYERED_DIFFUSE_ENERGY_HACKED_TERM
    float3 diffuseEnergy;

};

//-----------------------------------------------------------------------------
//
// PreLightData: Vertically Layered BSDF Computations ("VLayering")
//
//-----------------------------------------------------------------------------

// Average of a float3
float mean(float3 a) { return (a.x+a.y+a.z)/3.0; }

// Linearized variance from roughness to be able to express an atomic
// adding operator on variance.
float RoughnessToLinearVariance(float a)
{
    a = clamp(a, 0.0, 0.9999);
    float a3 = pow(a, 1.1);
    return a3 / (1.0f - a3);
}

float PerceptualRoughnessToLinearVariance(float a)
{
    a = PerceptualRoughnessToRoughness(a);
    return RoughnessToLinearVariance(a);
}

float LinearVarianceToRoughness(float v)
{
    v = max(v, 0.0);
    float a = pow(v / (1.0 + v), 1.0/1.1);
    return a;
}

float LinearVarianceToPerceptualRoughness(float v)
{
    return RoughnessToPerceptualRoughness(LinearVarianceToRoughness(v));
}

// Return the unpolarized version of the complete dielectric Fresnel equations
// from `FresnelDielectric` without accounting for wave phase shift.
// TODO: verify we have in BSDF lib
float FresnelUnpolarized(in float ct1, in float n1, in float n2)
{
    float cti = ct1;
    float st2 = (1.0 - Sq(cti));
    float nr  = n2/n1;
    if(nr == 1.0) { return 0.0; }

    if(Sq(nr)*st2 <= 1.0) {
        float ctt = sqrt(1.0 - Sq(nr)*st2) ;
        float tpp = (nr*cti-ctt) / (nr*cti + ctt);
        float tps = (cti-nr*ctt) / (nr*ctt + cti);
        return 0.5 * (tpp*tpp + tps*tps);
    } else {
        return 0.0;
    }
}

float GetModifiedAnisotropy0(float anisotropy, float roughness, float newRoughness)
{
    float r = (roughness)/(newRoughness+FLT_EPS);
    r = sqrt(r);
    float newAniso = anisotropy * (r  + (1-r) * clamp(5*roughness*roughness,0,1));
    return newAniso;
}
float GetModifiedAnisotropy(float anisotropy, float perceptualRoughness, float roughness, float newPerceptualRoughness)
{
    float r = (perceptualRoughness)/(newPerceptualRoughness+FLT_MIN*2);
    //r = sqrt(r);
    float factor = 1000.0;
#ifdef VLAYERED_DEBUG
    factor = _DebugAniso.y;
#endif
    float newAniso = anisotropy * (r  + (1-r) * clamp(factor*roughness*roughness,0,1));

    return newAniso;
}

// Get the orthogonal component (or complement) of a vector V with regard to the vector N.
float3 GetOrthogonalComponent(float3 V, float3 N)
{
    // V and N are supposed to be unit vectors
    float VdotN = dot(V, N);
    float3 VOrtho = V - VdotN * N;
    float3 unitVOrtho = VOrtho * rsqrt(1.0 - Sq(VdotN));
    return unitVOrtho;
}

float3 GetDirFromAngleAndOrthoFrame(float3 V, float3 N, float newVdotN)
{
    float sintheta = sqrt(1.0 - Sq(newVdotN));
    float3 newV = newVdotN * N + sintheta * V;
    return newV;
}


void ComputeAdding_GetVOrthoGeomN(BSDFData bsdfData, float3 V, bool calledPerLight, out float3 vOrthoGeomN, out bool useGeomN)
{
    vOrthoGeomN = (float3)0;
    useGeomN = false;

    if( !calledPerLight && IsCoatNormalMapEnabled(bsdfData) )
    {
        // In that case, since we have 2 normal maps we need to decide on a common orientation
        // for our parallel interface model, otherwise the series expression doesn't make any
        // sense. We will settle on using the geometric normal. It will be used for
        // average mean propagation but all FGD or Fresnel terms will use the corresponding
        // interface's normal map's normal for calculation. IBL fetches and lighting
        // calculations (shading) for analytical lights should also use these.
        // The rational for the later is that the resulting stats are still a local model, so
        // all scattered rays should exit back up with the same normal as the ray that spawned
        // them had on entry on top. So we assume bending is cancelled out.
        //
        // Also, since we are using a fake (and adjusted / lerped depending on roughness)
        // refraction for the top (coat) interface, and this will be done, like stated,
        // using the geometric normal, we will reconstruct a direction for the bottom
        // interface using the "V and geomNormalWS" plane as a plane of incidence. So we
        // calculate a pseudo-refracted angle in this plane, and with an orthogonal basis
        // of it (2D basis embedded in 3D, ie basis formed by two 3D vectors)
        // (using the orthogonal complement of V vs geomNormalWS), we will reconstruct
        // the "V at the bottom interface". This V will then in turn be usable for further
        // FGD / Fresnel calculations with the bottom interface normal (from the bottom
        // normal map), which is not necessarily coplanar with V and geomNormalWS, hence
        // this method.
        //
        // In all other cases: we either don't have a dual normal map, or we recompute the
        // stats per light and in that case, the H vector serves as a common orientation
        // and we calculate everything with it anyway (symmetric parametrization), so no
        // normal map is involved.

        vOrthoGeomN = GetOrthogonalComponent(V, bsdfData.geomNormalWS);
        useGeomN = true;
    }
}


//-----------------------------------------------------------------------------
//  About layered BSDF statistical lobe data calculations:
//
//
// ComputeAdding summary notes:
// -----------------------------
//
// -(Point A) Any local BSDF function parameter that depends on an angle needs to
// use the angle of directions at the interface that generated the output lobe
// (either refracted for bottom interface, or normal elevation at top interface).
//
// -(Point B) By symmetry of refractions top/down -> bottom/up, lobe directional
// stats stay the same for lighting calculations (so use original top interface
// angles)
//
// -If we use energy coefficients for IBL, use FGD terms during ComputeAdding
// (FGDinf in fact, TODOENERGY) for all adding-equation operators. FGD is fetched
// during the algo, at the right angle.
//
// -If we will use energy coefficients for analytical lights, still use FGD (especially
// correct is a (1-FGD) term at the top), ie, for everything below the first interface,
// but use the actual Fresnel term for that light for R12 at the start
// of the algo (top interface) and not FGD.
// If you recompute everything per light, FGD fetches per light might be expensive,
// so could use the FGD used for IBLs, angle used will be more or less incorrect
// depending on light directions, but probably better than using F0 terms everywhere).
//
// -Right now the method uses Fresnel terms for everything.
//
// -When using Fresnel term formulation, FGD fetches are deferred, need to take care
// of points A and B. In particular, IBL fetches are light sample (ie don't refract
// directions of LD fetches). Also, while FGD table is meant to be used with F0,
// averages are used instead (notice the difference: a chain of Fresnel terms instead) of
// the F0 that modulates FGD(angle, roughness). This is another approximation, versus
// doing the FGD fetches directly during ComputeAdding. Then the output energy terms
// could be used directly as the "FGD" part of the split sum to multiply with IBL's LD.
//
// -This is still a local model (like BSDF), no SSS inferred, or inter-layer refractions
// that could change the actual spatially-varying (non-local) data parametrizing the
// BSDF.
//
//
// More details:
// -----------------------------
//
// There’s a couple of choices for formulating the adding equations in ComputeAdding( ).
// I discuss that here along with some information on the whole method.
//
// If the energy coefficients are intended to be used solely in a split sum IBL context,
// then it makes sense to always use FGD and actually fetch them here while doing adding.
//
// If they are going to be used for analytical lights also, this gets a bit more tricky
// (see details below), as the first R12 need not be an average but can be the actual
// Fresnel term of each light since there's a finite number of rays contributing.
// The application of analytical lights seems more natural as you could use your full
// BSDF with these energy coefficients.
// Even in that case though, past the first interface, you need  FGD when calculating
// the other terms (eg for transmission, past an interface, (1-FGD)).
//
// (Also ComputeAdding in that case (for every analytical lights) can be done with
// LdotH ie in a half-vector reference frame for the adding calculations but still use
// the original L direction and N when actually evaluating the BSDF corresponding to an
// output lobe. See below.)
//
// If in ComputeAdding() you use FGD for reflection, you need to be aware that you are
// targeting more a split sum formulation for eg IBL, and the other part of the sum is
// integral(L) (or importance sampled L, ie integral(LD), the later is the one we use).
// This would mean using D_GGX() (for the equivalent of preLD) directly in analytical
// light evaluations instead of the full BSDF. However, test empirically, might still be
// better to use the full BSDF even then.
// (Using FGD means accounting an average omnidirectional-outgoing / unidirectional-incoming
// energy transfer, “directional albedo” form. But in analytical lights, dirac vanishes
// the first FGD integral, and the "LD" part will be very sparse and punctual, hence this
// might justify using more than D_GGX() ?)
//
// Our current case:
//
// However, if like done now ComputeAdding uses Fresnel terms directly, then IBLs
// need to fetch FGD using the Fresnel term chain (energy coefficients) as an F0
// (approximation) *but with the angle the lobe propagation calculations in ComputeAdding
// (eg through fake refraction) would have computed when having reached the interface that
// generated that output lobe*
// The reason is that the FGD term would have been fetched at that point, and accounted
// with that direction. This has nothing to do with the actual orientation of the output
// lobe (and hence the direction that we must use for the LD fetch).
//
// Reference frame for the stats:
//
// Another point: since ComputeAdding( ) uses angles for Fresnel energy terms, if we
// recalculate per light, we must use a parametrization (reference frame) according to H
// and NOT N. This also means we are making different assumptions about the way the
// propagation operators work: eg see p9 the Symmetric Model: it is as if roughness is not
// "injected" in the direction of the macrosurface but in the H direction. Although
// different, empirical results show that it is as valid. Note however that again, this
// doesn’t change the lobe directions.
//
// Offspecular effects:
//
// Since the ComputeAdding() method doesn’t really track mean directions but assume symmetry in
// the incidence plane (perpendicular to the “up” vector of the parametrization - either N or H
// depending on the given cti param - cos theta incident), only elevation angles are used, but
// output lobe directions (by symmetry of reflection and symmetry of transmission top-to-bottom
// + bottom-to-top), since only reflection lobes are outputted, are all in the same direction
// and thus do not impose a deviation on the w_i_fake value we need to conceptually use when
// instantiating a BSDF from our statistical representation of a lobe (so we just need to use
// original w_i).
//
// Offspecular effects are also ignored in the computations (which would break symmetry of
// reflections especially at high roughness and further complicates the adding equations between
// interfaces of *different* roughnesses), but, in the end, it is assumed (and can be seen as an
// approximation to correct a bit for that) that the output lobes increase of roughness should
// indeed tilt the resulting instantiated BSDF lobe a bit toward the normal (ie an offspecular
// tilt still happens but after and based on the whole layered stack stats that have been
// computed).
//
// Again, since we don’t change w_i when instantiating an analytic BSDF, the change in roughness
// will incur that additional offspecular deviation naturally.
// For IBLs however, we take the resulting output (vlayer) roughness and calculate a correction
// to fetch through the dominant (central direction of the lobe) through GetSpecularDominantDir( )
// as usual, but using the refracted angle for the bottom interface because that is what specifies
// the "original" offspecular effect that the approximation uses to correct.
//
// (Note also that offspecular effects are also outside the plane of reflection, as the later is
// defined by coplanar L, N and V while the tilt of the lobe is towards N. This adds to the
// complexity of handling the effect.)
//
// TODOENERGY:
// EnergyCompensation: This term can no longer be used alone in our vlayered BSDF framework as it
// was applied only one time indiscriminately at PostEvaluateBSDF( ) on the specular lighting which
// would be wrong in our case, since the correction terms depend on the FGD of the lobe
// compensation must happen at each FGD use in ComputeAdding. However, our framework is exactly
// designed to handle that problem, in that if we calculate and apply proper energy coefficient
// terms (should be calculated from FGDinf) and modulate each specular calculations with them,
// this will actually do compensation. For now, since FGD fetches are done after ComputeAdding,
// we apply the factors on light samples, less correct, must be moved inside ComputeAdding.

// TODO:
// This creates another performance option: when in VLAYERED_RECOMPUTE_PERLIGHT mode, we
// don’t recompute for IBLs, but the coefficients for energy compensation would need to get FGD,
// and will require FGD fetches for each analytical light. (ie ComputeAdding( ) ideally should
// always do calculations with FGD and do the fetches, so that even in GetPreLightData, nothing
// would be done there). For now, and for performance reasons, we don’t provide the option.
//
// However, when VLAYERED_RECOMPUTE_PERLIGHT is not used, we actually get usable terms that we
// will apply to the specular lighting, but these are different, we have one per real interface
// (so 2 vs the 3 “virtual” layer structure here).
// (FGDinf can be obtained from our FGD)
//



///Helper function that parses the BSDFData object to generate the current layer's
// statistics.
//
// TODO: R12 Should be replace by a fetch to FGD.
//       T12 should be multiplied by TIR.
//       (more like p8, T21 <- T21*TIR, R21 <- R21 + (1-TIR)*T21 )
//
//ComputeStatistics(cti, V, vOrthoGeomN, useGeomN, i, bsdfData, preLightData, ctt, R12, T12, R21, T21, s_r12, s_t12, j12, s_r21, s_t21, j21);
void ComputeStatistics(in  float  cti, in float3 V, in float3 vOrthoGeomN, in bool useGeomN, in int i, in BSDFData bsdfData,
                       inout PreLightData preLightData,
                       out float  ctt,
                       out float3 R12,   out float3 T12,   out float3 R21,   out float3 T21,
                       out float  s_r12, out float  s_t12, out float  j12,
                       out float  s_r21, out float  s_t21, out float  j21)
{

    // Case of the dielectric coating
    if( i == 0 )
    {
        // Update energy
        float R0, n12;

        n12 = GetCoatEta(bsdfData); //n2/n1;
        R0  = FresnelUnpolarized(cti, n12, 1.0);

        // At this point cti should be properly (coatNormalWS dot V) or NdotV or VdotH, see ComputeAdding.
        // In the special case where we do have a coat normal, we will propagate a different angle than
        // (coatNormalWS dot V) and vOrthoGeomN will be used.
        // vOrthoGeomN is the orthogonal complement of V wrt geomNormalWS.
        if (useGeomN)
        {
            cti = ClampNdotV(dot(bsdfData.geomNormalWS, V));
        }

        R12 = R0; // TODO: FGD
        T12 = 1.0 - R12;
        R21 = R12;
        T21 = T12;

        // Update mean
        float sti = sqrt(1.0 - Sq(cti));
        float stt = sti / n12;
        if( stt <= 1.0f )
        {
            // Hack: as roughness -> 1, remove the effect of changing angle also note: we never track means per se
            // because of symmetry, we have no azimuth, and don't consider offspecular effect as well as never
            // outputting final downward lobes anyway.
            // We just track cosines of angles for energy transfer calculations (should do with FGD but depends,
            // see comments above).
            const float alpha = bsdfData.coatRoughness;
            const float scale = clamp((1.0-alpha)*(sqrt(1.0-alpha) + alpha), 0.0, 1.0);
            //http://www.wolframalpha.com/input/?i=f(alpha)+%3D+(1.0-alpha)*(sqrt(1.0-alpha)+%2B+alpha)+alpha+%3D+0+to+1
            stt = scale*stt + (1.0-scale)*sti;
            ctt = sqrt(1.0 - stt*stt);
        }
        else
        {
            // TIR, flip sign: directions either reflected or transmitted always leave
            // the surface. So here we have ctt instead of cti, we reverse dir by flipping sign.
            // Not accounted for though check implications of ctt = -1.0
            // TODO
            ctt = -1.0;
        }

        // Update variance
        s_r12 = RoughnessToLinearVariance(bsdfData.coatRoughness);
        s_t12 = RoughnessToLinearVariance(bsdfData.coatRoughness * 0.5 * abs((ctt*n12 - cti)/(ctt*n12)));
        j12   = (ctt/cti)*n12;

        s_r21 = s_r12;
        s_t21 = RoughnessToLinearVariance(bsdfData.coatRoughness * 0.5 * abs((cti/n12 - ctt)/(cti/n12)));
        j21   = 1.0/j12;

    // Case of the media layer
    }
    else if(i == 1)
    {
        // Update energy
        R12 = float3(0.0, 0.0, 0.0);
        T12 = exp(- bsdfData.coatThickness * bsdfData.coatExtinction / cti);
        R21 = R12;
        T21 = T12;

        // Update mean
        ctt = cti;

        // Update variance
        s_r12 = 0.0;
        s_t12 = 0.0;
        j12   = 1.0;

        s_r21 = 0.0;
        s_t21 = 0.0;
        j21   = 1.0;

    // Case of the dielectric / conductor base
    }
    else
    {
        float ctiForFGD = cti;

        // If we use the geometric normal propagation hack, we want to calculate FGD / Fresnel with
        // an angle at the bottom interface between the average propagated direction and the normal from
        // the bottom normal map. For that, we will recover a direction from the angle we propagated in
        // the "V and geomNormalWS" plane of incidence. That direction will then serve to calculate an
        // angle with the non-coplanar bottom normal from the normal map.
        if (useGeomN)
        {
            float3 bottomDir = GetDirFromAngleAndOrthoFrame(vOrthoGeomN, bsdfData.geomNormalWS, cti);
            ctiForFGD = ClampNdotV(dot(bsdfData.normalWS, bottomDir));
        }
        // We will also save this average bottom angle:
        preLightData.bottomAngleFGD = ctiForFGD;

        // Update energy
        R12 = F_Schlick(bsdfData.fresnel0, ctiForFGD);
        T12 = 0.0;
#ifdef VLAYERED_DIFFUSE_ENERGY_HACKED_TERM
        // Still should use FGD!
        T12 = 1.0 - R12;
#endif
        R21 = R12;
        T21 = T12;

        // Update mean
        ctt = cti;

        // Update variance
        //
        // HACK: we will not propagate all needed last values, as we have 4,
        // but the adding cycle for the last layer can be shortcircuited for
        // the last lobes we need without computing the whole state of the
        // current stack (ie the i0 and 0i terms).
        //
        // We're only interested in _s_r0m and m_R0i.
        s_r12 = 0.0;
        //s_r12 = RoughnessToLinearVariance(bsdfData.roughnessAT);
        //s_r12_lobeB = RoughnessToLinearVariance(bsdfData.roughnessBT);
        // + anisotropic parts
        //

        s_t12 = 0.0;
        j12   = 1.0;

        s_r21 = s_r12;
        s_t21 = 0.0;
        j21   = 1.0;
    }

} //...ComputeStatistics()


void ComputeAdding(float _cti, float3 V, in BSDFData bsdfData, inout PreLightData preLightData, bool calledPerLight = false)
{
    // _cti should be LdotH or VdotH if calledPerLight == true (symmetric parametrization), V is unused in this case.
    // _cti should be NdotV if calledPerLight == false and no independent coat normal map is used (ie single normal map), V is unused in this case.
    // _cti should be (coatNormalWS dot V) if calledPerLight == false and we have a coat normal map. V is used in this case

#ifdef VLAYERED_DEBUG
    if( _DebugEnvLobeMask.w == 0.0)
    {
        preLightData.vLayerEnergyCoeff[COAT_LOBE_IDX] = 0.0 * F_Schlick(IorToFresnel0(bsdfData.coatIor), _cti);
        preLightData.iblPerceptualRoughness[COAT_LOBE_IDX] = bsdfData.coatPerceptualRoughness;
        preLightData.layeredCoatRoughness = ClampRoughnessForAnalyticalLights(bsdfData.coatRoughness);

        preLightData.vLayerEnergyCoeff[BASE_LOBEA_IDX] = 1.0 * F_Schlick(bsdfData.fresnel0, _cti);
        preLightData.vLayerEnergyCoeff[BASE_LOBEB_IDX] = 1.0 * F_Schlick(bsdfData.fresnel0, _cti);

        preLightData.layeredRoughnessT[0] = ClampRoughnessForAnalyticalLights(bsdfData.roughnessAT);
        preLightData.layeredRoughnessB[0] = ClampRoughnessForAnalyticalLights(bsdfData.roughnessAB);
        preLightData.layeredRoughnessT[1] = ClampRoughnessForAnalyticalLights(bsdfData.roughnessBT);
        preLightData.layeredRoughnessB[1] = ClampRoughnessForAnalyticalLights(bsdfData.roughnessBB);
        preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX] = bsdfData.perceptualRoughnessA;
        preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX] = bsdfData.perceptualRoughnessB;
        return;
    }
#endif

    // Global Variables

    // Decide if we need the special path/hack for the coat normal map mode:
    bool useGeomN;
    float3 vOrthoGeomN; // only valid if useGeomN == true
    ComputeAdding_GetVOrthoGeomN(bsdfData, V, calledPerLight, vOrthoGeomN, useGeomN);

    float  cti  = _cti;
    float3 R0i = float3(0.0, 0.0, 0.0), Ri0 = float3(0.0, 0.0, 0.0),
           T0i = float3(1.0, 1.0, 1.0), Ti0 = float3(1.0, 1.0, 1.0);
    float  s_r0i=0.0, s_ri0=0.0, s_t0i=0.0, s_ti0=0.0;
    float  j0i=1.0, ji0=1.0;

    float _s_r0m, s_r12, m_rr; // we will need these outside the loop for further calculations

    // Iterate over the layers
    for(int i = 0; i < NB_VLAYERS; ++i)
    {
        // Variables for the adding step
        float3 R12, T12, R21, T21;
        s_r12=0.0;
        float s_r21=0.0, s_t12=0.0, s_t21=0.0, j12=1.0, j21=1.0, ctt;

        // Layer specific evaluation of the transmittance, reflectance, variance
        ComputeStatistics(cti, V, vOrthoGeomN, useGeomN, i, bsdfData, preLightData, ctt, R12, T12, R21, T21, s_r12, s_t12, j12, s_r21, s_t21, j21);

        // Multiple scattering forms
        float3 denom = (float3(1.0, 1.0, 1.0) - Ri0*R12); //i = new layer, 0 = cumulative top (llab3.1 to 3.4)
        float3 m_R0i = (mean(denom) <= 0.0f)? float3(0.0, 0.0, 0.0) : (T0i*R12*Ti0) / denom; //(llab3.1)
        float3 m_Ri0 = (mean(denom) <= 0.0f)? float3(0.0, 0.0, 0.0) : (T21*Ri0*T12) / denom; //(llab3.2)
        float3 m_Rr  = (mean(denom) <= 0.0f)? float3(0.0, 0.0, 0.0) : (Ri0*R12) / denom;
        float  m_r0i = mean(m_R0i);
        float  m_ri0 = mean(m_Ri0);
        m_rr  = mean(m_Rr);

        // Evaluate the adding operator on the energy
        float3 e_R0i = R0i + m_R0i; //(llab3.1)
        float3 e_T0i = (T0i*T12) / denom; //(llab3.3)
        float3 e_Ri0 = R21 + (T21*Ri0*T12) / denom; //(llab3.2)
        float3 e_Ti0 = (T21*Ti0) / denom; //(llab3.4)

        // Scalar forms for the energy
        float r21   = mean(R21);
        float r0i   = mean(R0i);
        float e_r0i = mean(e_R0i);
        float e_ri0 = mean(e_Ri0);

        // Evaluate the adding operator on the normalized variance
        _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
        float _s_r0i = (r0i*s_r0i + m_r0i*_s_r0m) / e_r0i;
        float _s_t0i = j12*s_t0i + s_t12 + j12*(s_r12 + s_ri0)*m_rr;
        float _s_rim = s_t12 + j12*(s_t21 + s_ri0 + m_rr*(s_r12+s_ri0));
        float _s_ri0 = (r21*s_r21 + m_ri0*_s_rim) / e_ri0;
        float _s_ti0 = ji0*s_t21 + s_ti0 + ji0*(s_r12 + s_ri0)*m_rr;
        _s_r0i = (e_r0i > 0.0) ? _s_r0i/e_r0i : 0.0;
        _s_ri0 = (e_ri0 > 0.0) ? _s_ri0/e_ri0 : 0.0;

        // Store the coefficient and variance
        if(m_r0i > 0.0)
        {
            preLightData.vLayerEnergyCoeff[i] = m_R0i;
            //preLightData.vLayerPerceptualRoughness[i] = LinearVarianceToPerceptualRoughness(_s_r0m);
        }
        else
        {
            preLightData.vLayerEnergyCoeff[i] = float3(0.0, 0.0, 0.0);
            //preLightData.vLayerPerceptualRoughness[i] = 0.0;
        }

        // Update energy
        R0i = e_R0i;
        T0i = e_T0i;
        Ri0 = e_Ri0;
        Ti0 = e_Ti0; // upward transmittance: we need this fully computed "past" the last layer see below for diffuse


        // Update mean
        cti = ctt;

        // We need to escape this update on the last vlayer iteration,
        // as we will use a hack to compute all needed bottom layer
        // anisotropic roughnesses. The compiler should easily factor
        // this out when the loop is unrolled anyway
        if( i < (NB_VLAYERS-1) )
        {
            // Update variance
            s_r0i = _s_r0i;
            s_t0i = _s_t0i;
            s_ri0 = _s_ri0;
            s_ti0 = _s_ti0;

            // Update jacobian
            j0i *= j12;
            ji0 *= j21;
        }
    }

    //-------------------------------------------------------------
    // Post compute:
    //-------------------------------------------------------------
    // TODO: dual lobe feature option
    //
    // Works because we're the last "layer" and all variables touched
    // above are in a state where these calculations will be valid:
    //
    // We need both bottom lobes perceptualRoughnessA and
    // perceptualRoughnessB for IBLs (because anisotropy will use a hack)
    //
    // Then we need anisotropic roughness updates again for the 2
    // bottom lobes, for analytical lights.
    //

    // First, to be less messy, immediately transfer vLayerPerceptualRoughness
    // data into the iblPerceptualRoughness[] array
    // (note that vLayer*[0] and vLayer*[2] contains useful data,
    // but not vLayer*[1] - this is the media "layer")

    // Obviously coat roughness is given without ComputeAdding calculations (nothing on top)
    // ( preLightData.iblPerceptualRoughness[COAT_LOBE_IDX] = preLightData.vLayerPerceptualRoughness[TOP_VLAYER_IDX]; )

#ifdef VLAYERED_RECOMPUTE_PERLIGHT
    bool perLightOption = true;
#else
    bool perLightOption = false;
#endif
    bool haveAnisotropy = HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY);

    if( !calledPerLight )
    {
        // First, if we're not called per light, always (regardless of perLightOption) calculate
        // roughness for top lobe: no adding( ) modifications, just conversion + clamping for
        // analytical lights. For these we also don't need to recompute these, but only the Fresnel
        // or FGD term are necessary in ComputeAdding, see BSDF().
        preLightData.iblPerceptualRoughness[COAT_LOBE_IDX] = bsdfData.coatPerceptualRoughness;
        preLightData.layeredCoatRoughness = ClampRoughnessForAnalyticalLights(bsdfData.coatRoughness);
    }

    // calledPerLight and all of the bools above are static time known.
    // What we have to calculate is a bit messy here.
    // Basically, If we're in a mode where we will compute the vlayer stats per analytical light,
    // and our calling context here is per light, we shouldn't deal with the perceptual roughnesses
    // for IBLs, nor with their anisotropy parameter recalculation. So we only deal with the roughness
    // used by analytical lights (no iblPerceptualRoughness)
    //
    // Otherwise, depending on if we have anisotropy or not, we might still have to deal with
    // the T and B terms to have isotropic modulation by the above layer and re-infer back a
    // a corrected anisotropy and scalar roughness for use with the IBL hack.
    // That hack adds complexity because IBLs can't use the T and B roughnesses but at the
    // same time we can't just update their scalar roughness because then it will only give them more
    // roughness in the anisotropic direction, which is incorrect and with the hack, will appear
    // even more so.

#ifdef VLAYERED_ANISOTROPY_SCALAR_ROUGHNESS

    // --------------------------------------------------------------------------------
    // Scalar treatment of anisotropy: Simple in this case, just modify the scalar
    // roughnesses, keep anisotropy the same. Whether we're being called per light or
    // not makes little difference on calculations so everything is made generic,
    // but if we have the perLightOption enabled, we don't finalize calculations for
    // analytical lights.
    // --------------------------------------------------------------------------------

    preLightData.iblAnisotropy[0] = bsdfData.anisotropy;
    preLightData.iblAnisotropy[1] = bsdfData.anisotropy;


    s_r12 = RoughnessToLinearVariance(PerceptualRoughnessToRoughness(bsdfData.perceptualRoughnessA));
    _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
    preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX] = LinearVarianceToPerceptualRoughness(_s_r0m);
    s_r12 = RoughnessToLinearVariance(PerceptualRoughnessToRoughness(bsdfData.perceptualRoughnessB));
    _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
    preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX] = LinearVarianceToPerceptualRoughness(_s_r0m);

#ifdef VLAYERED_ANISOTROPY_SCALAR_ROUGHNESS_CORRECTANISO
    // Try to correct stretching that happens when original roughness was low, but not too much
    // that destretching happens.
    IF_DEBUG( if( _DebugAniso.x == 1) )
    {
        preLightData.iblAnisotropy[0] = GetModifiedAnisotropy(bsdfData.anisotropy, bsdfData.perceptualRoughnessA,
                                                              PerceptualRoughnessToRoughness(bsdfData.perceptualRoughnessA),
                                                              preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX]);
        preLightData.iblAnisotropy[1] = GetModifiedAnisotropy(bsdfData.anisotropy, bsdfData.perceptualRoughnessB,
                                                              PerceptualRoughnessToRoughness(bsdfData.perceptualRoughnessB),
                                                              preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX]);
    }
#endif

    if( !perLightOption || calledPerLight)
    {
        ConvertAnisotropyToClampRoughness(preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX], preLightData.iblAnisotropy[0],
                                          preLightData.layeredRoughnessT[0], preLightData.layeredRoughnessB[0]);
        ConvertAnisotropyToClampRoughness(preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX], preLightData.iblAnisotropy[1],
                                          preLightData.layeredRoughnessT[1], preLightData.layeredRoughnessB[1]);
    }

#else
    // --------------------------------------------------------------------------------
    // Non scalar treatment of anisotropy to have the option to remove some anisotropy
    // --------------------------------------------------------------------------------
    if( !calledPerLight && !haveAnisotropy)
    {
        // Calculate modified base lobe roughnesses T (no anisotropy)

        // There's no anisotropy and we haven't clamped the roughness in the T and B fields, so
        // that we can use directly bsdfData.roughness?T == bsdfData.roughness?B
        // == PerceptualRoughnessToRoughness(bsdfData.perceptualRoughnessA)) :
        //s_r12 = RoughnessToLinearVariance(PerceptualRoughnessToRoughness(bsdfData.perceptualRoughnessA));
        s_r12 = RoughnessToLinearVariance(bsdfData.roughnessAT);
        _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
        float varianceLobeA = _s_r0m;
        preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX] = LinearVarianceToPerceptualRoughness(_s_r0m);

        //s_r12 = RoughnessToLinearVariance(PerceptualRoughnessToRoughness(bsdfData.perceptualRoughnessB));
        s_r12 = RoughnessToLinearVariance(bsdfData.roughnessBT);
        _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
        float varianceLobeB = _s_r0m;
        preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX] = LinearVarianceToPerceptualRoughness(_s_r0m);

        if( !perLightOption )
        {
            // We're not going to get called again per analytical light so store the result needed and used by them:
            // LOBEA and LOBEB but only the T part...
            preLightData.layeredRoughnessT[0] = ClampRoughnessForAnalyticalLights(LinearVarianceToRoughness(varianceLobeA));
            preLightData.layeredRoughnessT[1] = ClampRoughnessForAnalyticalLights(LinearVarianceToRoughness(varianceLobeB));
        }
    }

    if( !calledPerLight && haveAnisotropy)
    {
        // We're in GetPreLightData context so we need to deal with IBL precalc, and
        // regardless of if we had the VLAYERED_RECOMPUTE_PERLIGHT option or not, we
        // still need to compute the full anistropic modification of variances.

        // We proceed as follow: Convert T & B roughnesses to variance, propagate the effect of layers,
        // infer back a new anisotropy parameter and roughness from them:
        // TODOANISOTROPY

        // LOBEA roughness for analytical lights (T part)
        s_r12 = RoughnessToLinearVariance(bsdfData.roughnessAT);
        _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
        float roughnessT = LinearVarianceToRoughness(_s_r0m);

        // LOBEA roughness for analytical (B part)
        s_r12 = RoughnessToLinearVariance(bsdfData.roughnessAB);
        _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
        float roughnessB = LinearVarianceToRoughness(_s_r0m);

#ifdef VLAYERED_ANISOTROPY_IBL_DESTRETCH
        // TODOANISOTROPY
        ConvertRoughnessToAnisotropy(roughnessT, roughnessB, preLightData.iblAnisotropy[0]);
#else
        preLightData.iblAnisotropy[0] = bsdfData.anisotropy;
#endif
        preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX] = RoughnessToPerceptualRoughness((roughnessT + roughnessB)/2.0);

        if( !perLightOption )
        {
            // We're not going to get called again per analytical light so store the result needed and used by them:
            // LOBEA T and B part:
            preLightData.layeredRoughnessT[0] = ClampRoughnessForAnalyticalLights(roughnessT);
            preLightData.layeredRoughnessB[0] = ClampRoughnessForAnalyticalLights(roughnessB);
        }


        // LOBEB roughness for analytical lights (T part)
        s_r12 = RoughnessToLinearVariance(bsdfData.roughnessBT);
        _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
        roughnessT = LinearVarianceToRoughness(_s_r0m);

        // LOBEB roughness for analytical (B part)
        s_r12 = RoughnessToLinearVariance(bsdfData.roughnessBB);
        _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
        roughnessB = LinearVarianceToRoughness(_s_r0m);

#ifdef VLAYERED_ANISOTROPY_IBL_DESTRETCH
        // TODOANISOTROPY
        ConvertRoughnessToAnisotropy(roughnessT, roughnessB, preLightData.iblAnisotropy[1]);
#else
        preLightData.iblAnisotropy[1] = bsdfData.anisotropy;
#endif
        preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX] = RoughnessToPerceptualRoughness((roughnessT + roughnessB)/2.0);

        if( !perLightOption )
        {
            // We're not going to get called again per analytical light so store the result needed and used by them:
            // LOBEB T and B part:
            preLightData.layeredRoughnessT[1] = ClampRoughnessForAnalyticalLights(roughnessT);
            preLightData.layeredRoughnessB[1] = ClampRoughnessForAnalyticalLights(roughnessB);
        }

    } // if( !calledPerLight && haveAnisotropy)

    if( calledPerLight )
    {
#ifndef VLAYERED_RECOMPUTE_PERLIGHT
    //error
#endif
        // Finally, if we're computing all this for one light, first the option should have been declared,
        // and we don't compute anything IBL related, already done in GetPreLightData's context.
        // We just need to propagate variance for LOBEA and LOBEB and clamp.

        // LOBEA roughness for analytical lights (T part)
        s_r12 = RoughnessToLinearVariance(bsdfData.roughnessAT);
        _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
        preLightData.layeredRoughnessT[0] = ClampRoughnessForAnalyticalLights(LinearVarianceToRoughness(_s_r0m));

        // LOBEB roughness for analytical lights (T part)
        s_r12 = RoughnessToLinearVariance(bsdfData.roughnessBT);
        _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
        preLightData.layeredRoughnessT[1] = ClampRoughnessForAnalyticalLights(LinearVarianceToRoughness(_s_r0m));

        if ( haveAnisotropy )
        {
            // LOBEA roughness for analytical lights (B part)
            s_r12 = RoughnessToLinearVariance(bsdfData.roughnessAB);
            _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
            preLightData.layeredRoughnessB[0] = ClampRoughnessForAnalyticalLights(LinearVarianceToRoughness(_s_r0m));

            // LOBEB roughness for analytical lights (B part)
            s_r12 = RoughnessToLinearVariance(bsdfData.roughnessBB);
            _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
            preLightData.layeredRoughnessB[1] = ClampRoughnessForAnalyticalLights(LinearVarianceToRoughness(_s_r0m));
        }
    }

#endif // #ifdef VLAYERED_ANISOTROPY_SCALAR_ROUGHNESS


#ifdef VLAYERED_DIFFUSE_ENERGY_HACKED_TERM
    // TODO
    preLightData.diffuseEnergy = Ti0;
    // Not correct since these stats are still directional probably too much
    // removed, but with a non FGD term, could actually balance out (as using
    // FGD would lower this)
#else
    preLightData.diffuseEnergy = float3(1.0, 1.0, 1.0);
#endif


} //... ComputeAdding()



float PreLightData_GetBaseNdotVForFGD(BSDFData bsdfData, PreLightData preLightData, float NdotV[NB_NORMALS])
{
    float baseLayerNdotV;
    if ( IsCoatNormalMapEnabled(bsdfData) )
    {
        baseLayerNdotV = preLightData.bottomAngleFGD;
    }
    else
    {
        //slnote: TODO TOTEST
        //baseLayerNdotV = preLightData.bottomAngleFGD;
        baseLayerNdotV = sqrt(1 + Sq(preLightData.coatIeta)*(Sq(NdotV[0]) - 1));
        //TODO refactor with EvalIridescence, Lit::GetPreLightData
    }
    return baseLayerNdotV;
}

void PreLightData_SetupNormals(BSDFData bsdfData, inout PreLightData preLightData, float3 V, out float3 N[NB_NORMALS], out float NdotV[NB_NORMALS])
{
    N[BASE_NORMAL_IDX] = bsdfData.normalWS;
    preLightData.NdotV[BASE_NORMAL_IDX] = dot(N[BASE_NORMAL_IDX], V);
    NdotV[BASE_NORMAL_IDX] = ClampNdotV(preLightData.NdotV[BASE_NORMAL_IDX]);

#ifdef _MATERIAL_FEATURE_COAT_NORMALMAP
    if ( IsCoatNormalMapEnabled(bsdfData) )
    {
        N[COAT_NORMAL_IDX] = bsdfData.coatNormalWS;
        preLightData.NdotV[COAT_NORMAL_IDX] = dot(N[COAT_NORMAL_IDX], V);
        NdotV[COAT_NORMAL_IDX] = ClampNdotV(preLightData.NdotV[COAT_NORMAL_IDX]);

        preLightData.geomNdotV = dot(bsdfData.geomNormalWS, V);
    }
#endif
}

PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    ZERO_INITIALIZE(PreLightData, preLightData);

    float3 N[NB_NORMALS];
    float NdotV[NB_NORMALS];

    PreLightData_SetupNormals(bsdfData, preLightData, V, N, NdotV);

    preLightData.diffuseEnergy = float3(1.0, 1.0, 1.0);

    // For eval IBL lights, we need:
    //
    // iblPerceptualRoughness (for FGD, mip, etc)
    // iblR                   (fetch direction in dominant spec direction / compensated for offspecular effect)
    // specularFGD            (coatIblF is now in there too)
    // energyCompensation     (to apply for each light sample since with multiple roughnesses, it becomes lobe specific)
    //
    // We also need for analytical lights:
    //
    // coatRoughness, roughnessAT/AB/BT/BB (anisotropic, all are nonperceptual and *clamped*)
    // partLambdaV
    //
    // The later are done in ComputeAdding at GetPreLightData time only if we're not using
    // VLAYERED_RECOMPUTE_PERLIGHT.

    // TODO this can now be refactored instead of having mostly duped code down here,
    // Use loops and special case with IsVLayeredEnabled(bsdfData) which is statically known.

    // We will need hacked N for the stretch anisotropic hack later.
    float3 iblN[TOTAL_NB_LOBES]; //ZERO_INITIALIZE(float3[TOTAL_NB_LOBES], iblN);
    float3 iblR[TOTAL_NB_LOBES];
    float specularReflectivity[TOTAL_NB_LOBES];
    float diffuseFGD[BASE_NB_LOBES];

    if( IsVLayeredEnabled(bsdfData) )
    {
#ifdef _MATERIAL_FEATURE_COAT
        // --------------------------------------------------------------------
        // VLAYERING:
        // --------------------------------------------------------------------

        // A secondary coat normal map is possible here, NdotV[] and N[] are sized
        // accordingly and are accessed by COAT|BASE_NORMAL_IDX

        preLightData.coatIeta = 1.0 / GetCoatEta(bsdfData);

        // First thing we need is compute the energy coefficients and new roughnesses.
        // Even if configured to do it also per analytical light, we need it for IBLs too.
        ComputeAdding(NdotV[COAT_NORMAL_IDX], V, bsdfData, preLightData, false);

        // After ComputeAdding, these are done for all lobes:
        //
        //   preLightData.iblPerceptualRoughness[]
        //   preLightData.vLayerEnergyCoeff[]
        //   preLightData.iblAnisotropy[]           (only if anisotropy is enabled)

        // If we're not using VLAYERED_RECOMPUTE_PERLIGHT we also have calculated
        //   preLightData.layeredRoughnessT and B[],
        //   preLightData.layeredCoatRoughness
        // Otherwise, the calculation of these is done for each light
        //

        if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
        {
            // Note: there's no anisotropy possible on coat.
            float TdotV = dot(bsdfData.tangentWS,   V);
            float BdotV = dot(bsdfData.bitangentWS, V);

#ifndef VLAYERED_RECOMPUTE_PERLIGHT
            // We can precalculate lambdaVs for all lights here since we're not doing ComputeAdding per light
            preLightData.partLambdaV[COAT_LOBE_IDX] = GetSmithJointGGXPartLambdaV(NdotV[COAT_NORMAL_IDX], preLightData.layeredCoatRoughness);
            preLightData.partLambdaV[BASE_LOBEA_IDX] = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV[BASE_NORMAL_IDX],
                                                                                        preLightData.layeredRoughnessT[0], preLightData.layeredRoughnessB[0]);
            preLightData.partLambdaV[BASE_LOBEB_IDX] = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV[BASE_NORMAL_IDX],
                                                                                        preLightData.layeredRoughnessT[1], preLightData.layeredRoughnessB[1]);
#else
            // Store those for eval analytical lights since we're going to
            // recalculate lambdaV after each ComputeAdding for each light
            preLightData.TdotV = TdotV;
            preLightData.BdotV = BdotV;
#endif
            // For GGX aniso and IBL we have done an empirical (eye balled) approximation compare to the reference.
            // We use a single fetch, and we stretch the normal to use based on various criteria.
            // result are far away from the reference but better than nothing
            // For positive anisotropy values: tangent = highlight stretch (anisotropy) direction, bitangent = grain (brush) direction.
            float3 grainDirWS[2];
            //grainDirWS[0] = (bsdfData.anisotropy >= 0.0) ? bsdfData.bitangentWS : bsdfData.tangentWS;
            grainDirWS[0] = (preLightData.iblAnisotropy[0] >= 0.0) ? bsdfData.bitangentWS : bsdfData.tangentWS;
            grainDirWS[1] = (preLightData.iblAnisotropy[1] >= 0.0) ? bsdfData.bitangentWS : bsdfData.tangentWS;

            // Reduce stretching for (perceptualRoughness < 0.2).
            float stretch[2];
            stretch[0] = abs(preLightData.iblAnisotropy[0]) * saturate(5 * preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX]);
            stretch[1] = abs(preLightData.iblAnisotropy[1]) * saturate(5 * preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX]);

            iblN[COAT_LOBE_IDX] = N[COAT_NORMAL_IDX]; // no anisotropy for coat.
            iblN[BASE_LOBEA_IDX] = GetAnisotropicModifiedNormal(grainDirWS[0], N[BASE_NORMAL_IDX], V, stretch[0]);
            iblN[BASE_LOBEB_IDX] = GetAnisotropicModifiedNormal(grainDirWS[1], N[BASE_NORMAL_IDX], V, stretch[1]);

        }
        else
        {

#ifndef VLAYERED_RECOMPUTE_PERLIGHT
            // We can precalculate lambdaVs for all lights here since we're not doing ComputeAdding per light
            preLightData.partLambdaV[COAT_LOBE_IDX] = GetSmithJointGGXPartLambdaV(NdotV[COAT_NORMAL_IDX], preLightData.layeredCoatRoughness);
            preLightData.partLambdaV[BASE_LOBEA_IDX] = GetSmithJointGGXPartLambdaV(NdotV[BASE_NORMAL_IDX], preLightData.layeredRoughnessT[0]);
            preLightData.partLambdaV[BASE_LOBEB_IDX] = GetSmithJointGGXPartLambdaV(NdotV[BASE_NORMAL_IDX], preLightData.layeredRoughnessT[1]);
#endif
            iblN[COAT_LOBE_IDX] = N[COAT_NORMAL_IDX];
            iblN[BASE_LOBEA_IDX] = iblN[BASE_LOBEB_IDX] = N[BASE_NORMAL_IDX];
        } // anisotropy

        // IBL
        // Handle IBL pre calculated data + GGX multiscattering energy loss compensation term

        // Here, we will fetch our actual FGD terms, see ComputeAdding for details: the F0 params
        // will be replaced by our energy coefficients. Note that the way to do it depends on the
        // formulation of ComputeAdding (with FGD fetches or only Fresnel terms).

        // Also note that while the fetch directions for the light samples (IBL) are the ones
        // at the top interface, for the FGD terms (in fact, for all angle dependent BSDF
        // parametrization data), we need to use the actual interface angle a propagated direction
        // would have. So, for the base layer, this is a refracted direction through the coat.
        // Same for the top, but this is just NdotV.
        // This is because we should really have fetched FGD with the tracked cti (cos theta incoming)
        // at the bottom layer or top layer during ComputeAdding itself. We delayed the fetch after,
        // because our ComputeAdding formulation is with "energy" coefficients calculated with a
        // chain of Fresnel terms instead of a correct chain computed with the true FGD.

        float baseLayerNdotV = PreLightData_GetBaseNdotVForFGD(bsdfData, preLightData, NdotV);


        float diffuseFGDTmp; // unused, for coat layer FGD fetch

        GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV[COAT_NORMAL_IDX],
                                               preLightData.iblPerceptualRoughness[COAT_LOBE_IDX],
                                               preLightData.vLayerEnergyCoeff[TOP_VLAYER_IDX],
                                               preLightData.specularFGD[COAT_LOBE_IDX],
                                               diffuseFGDTmp,
                                               specularReflectivity[COAT_LOBE_IDX]);

        GetPreIntegratedFGDGGXAndDisneyDiffuse(baseLayerNdotV,
                                               preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX],
                                               preLightData.vLayerEnergyCoeff[BOTTOM_VLAYER_IDX],
                                               preLightData.specularFGD[BASE_LOBEA_IDX],
                                               diffuseFGD[0],
                                               specularReflectivity[BASE_LOBEA_IDX]);

        GetPreIntegratedFGDGGXAndDisneyDiffuse(baseLayerNdotV,
                                               preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX],
                                               preLightData.vLayerEnergyCoeff[BOTTOM_VLAYER_IDX],
                                               preLightData.specularFGD[BASE_LOBEB_IDX],
                                               diffuseFGD[1],
                                               specularReflectivity[BASE_LOBEB_IDX]);

        iblR[0] = reflect(-V, iblN[0]);
        iblR[1] = reflect(-V, iblN[1]);
        iblR[2] = reflect(-V, iblN[2]);
        // This is a ad-hoc tweak to better match reference of anisotropic GGX.
        // TODO: We need a better hack.
        preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX] *= saturate(1.2 - abs(preLightData.iblAnisotropy[0]));
        preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX] *= saturate(1.2 - abs(preLightData.iblAnisotropy[1]));

        // Correction of reflected direction for better handling of rough material

        // Notice again that the roughness and iblR properly use the output lobe statistics, but baseLayerNdotV
        // is used for the offspecular correction because the true original offspecular tilt is parametrized by
        // the angle at the base layer and the correction itself is influenced by that. See comments above.
        preLightData.iblR[COAT_LOBE_IDX] = GetSpecularDominantDir(N[COAT_NORMAL_IDX], iblR[COAT_LOBE_IDX], preLightData.iblPerceptualRoughness[COAT_LOBE_IDX], NdotV[COAT_NORMAL_IDX]);
        preLightData.iblR[BASE_LOBEA_IDX] = GetSpecularDominantDir(N[BASE_NORMAL_IDX], iblR[BASE_LOBEA_IDX], preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX], baseLayerNdotV);
        preLightData.iblR[BASE_LOBEB_IDX] = GetSpecularDominantDir(N[BASE_NORMAL_IDX], iblR[BASE_LOBEB_IDX], preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX], baseLayerNdotV);

#ifdef LIT_USE_GGX_ENERGY_COMPENSATION
        // TODOENERGY:
        // This is actually changing FGD to FGDinf in the vlayering framework but this needs to be folded in the energy calculations
        // during ComputeAdding. Also even analytical lights will need the energy terms, not just IBL.
        // (See also CalculateEnergyCompensationFromSpecularReflectivity and ApplyEnergyCompensationToSpecularLighting)

        // Wrong to apply these here vs in ComputeAdding as compensation means replacing FGD with FGDinf but we have a chain of
        // them built by ComputeAdding with terms like (FGDinf), (1-FGDinf). Also, the compensation approximation depends on f0.
        // See TODOENERGY [eg:  (a1*ef)(1-(a2*ef)) != ef (a1)(1-a2) ]

        preLightData.energyCompensationFactor[BASE_LOBEA_IDX] = GetEnergyCompensationFactor(specularReflectivity[BASE_LOBEA_IDX], bsdfData.fresnel0);
        preLightData.energyCompensationFactor[BASE_LOBEB_IDX] = GetEnergyCompensationFactor(specularReflectivity[BASE_LOBEB_IDX], bsdfData.fresnel0);
        preLightData.energyCompensationFactor[COAT_LOBE_IDX] = GetEnergyCompensationFactor(specularReflectivity[COAT_LOBE_IDX], IorToFresnel0(bsdfData.coatIor));


#else
        preLightData.energyCompensationFactor[BASE_LOBEA_IDX] =
        preLightData.energyCompensationFactor[BASE_LOBEB_IDX] =
        preLightData.energyCompensationFactor[COAT_LOBE_IDX] = 1.0;
#endif

#endif //ifdef _MATERIAL_FEATURE_COAT
    } //...if( IsVLayeredEnabled(BSDFData bsdfData) )
    else
    {
        // --------------------------------------------------------------------
        // NO VLAYERING:
        // --------------------------------------------------------------------

        // Only a single normal map possible here, NdotV[] and N[] are sized to 1

        // See ConvertSurfaceDataToBSDFData : The later are already clamped if
        // vlayering is disabled, so could be used directly, but for later
        // refactoring (instead of BSDFdata A and B values, we should really
        // permit array definitions in the shader include attributes TODOTODO)
        // no coat here: preLightData.layeredCoatRoughness = bsdfData.coatRoughness;
        preLightData.layeredRoughnessT[0] = bsdfData.roughnessAT;
        preLightData.layeredRoughnessB[0] = bsdfData.roughnessAB;
        preLightData.layeredRoughnessT[1] = bsdfData.roughnessBT;
        preLightData.layeredRoughnessB[1] = bsdfData.roughnessBB;

        preLightData.iblPerceptualRoughness[0] = bsdfData.perceptualRoughnessA;
        preLightData.iblPerceptualRoughness[1] = bsdfData.perceptualRoughnessB;

        if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
        {
            float TdotV = dot(bsdfData.tangentWS,   V);
            float BdotV = dot(bsdfData.bitangentWS, V);

            preLightData.partLambdaV[0] = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV[0], preLightData.layeredRoughnessT[0], preLightData.layeredRoughnessB[0]);
            preLightData.partLambdaV[1] = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV[0], preLightData.layeredRoughnessT[1], preLightData.layeredRoughnessB[1]);

            // For GGX aniso and IBL we have done an empirical (eye balled) approximation compare to the reference.
            // We use a single fetch, and we stretch the normal to use based on various criteria.
            // result are far away from the reference but better than nothing
            // For positive anisotropy values: tangent = highlight stretch (anisotropy) direction, bitangent = grain (brush) direction.
            float3 grainDirWS = (bsdfData.anisotropy >= 0.0) ? bsdfData.bitangentWS : bsdfData.tangentWS;

            // Reduce stretching for (perceptualRoughness < 0.2).
            float stretch[2];
            stretch[0] = abs(bsdfData.anisotropy) * saturate(5 * preLightData.iblPerceptualRoughness[0]);
            stretch[1] = abs(bsdfData.anisotropy) * saturate(5 * preLightData.iblPerceptualRoughness[1]);
            iblN[0] = GetAnisotropicModifiedNormal(grainDirWS, N[0], V, stretch[0]);
            iblN[1] = GetAnisotropicModifiedNormal(grainDirWS, N[0], V, stretch[1]);
        }
        else
        {
            preLightData.partLambdaV[0] = GetSmithJointGGXPartLambdaV(NdotV[0], preLightData.layeredRoughnessT[0]);
            preLightData.partLambdaV[1] = GetSmithJointGGXPartLambdaV(NdotV[0], preLightData.layeredRoughnessT[1]);
            iblN[0] = iblN[1] = N[0];
        } // ...no anisotropy


        // IBL
        // Handle IBL pre calculated data + GGX multiscattering energy loss compensation term

        GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV[0],
                                               preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX],
                                               bsdfData.fresnel0,
                                               preLightData.specularFGD[BASE_LOBEA_IDX],
                                               diffuseFGD[0],
                                               specularReflectivity[BASE_LOBEA_IDX]);

        GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV[0],
                                               preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX],
                                               bsdfData.fresnel0,
                                               preLightData.specularFGD[BASE_LOBEB_IDX],
                                               diffuseFGD[1],
                                               specularReflectivity[BASE_LOBEB_IDX]);


        iblR[0] = reflect(-V, iblN[0]);
        iblR[1] = reflect(-V, iblN[1]);
        // This is a ad-hoc tweak to better match reference of anisotropic GGX.
        // TODO: We need a better hack.
        float fact = saturate(1.2 - abs(bsdfData.anisotropy));
        preLightData.iblPerceptualRoughness[0] *= fact;
        preLightData.iblPerceptualRoughness[1] *= fact;
        // Correction of reflected direction for better handling of rough material
        preLightData.iblR[0] = GetSpecularDominantDir(N[0], iblR[0], preLightData.iblPerceptualRoughness[0], NdotV[0]);
        preLightData.iblR[1] = GetSpecularDominantDir(N[0], iblR[1], preLightData.iblPerceptualRoughness[1], NdotV[0]);

#ifdef LIT_USE_GGX_ENERGY_COMPENSATION
        // Here, since this compensation term is already an average applied to a sum
        // (akin to a "split sum" approximation) we will just lerp our two "specularReflectivities".
        // When in vlayering, the same split approximation idea is embedded in the whole aggregate statistical
        // formulation. ie Compensation corresponds to using FGDinf instead of FGD.
        preLightData.energyCompensationFactor[BASE_LOBEA_IDX] = GetEnergyCompensationFactor(specularReflectivity[BASE_LOBEA_IDX], bsdfData.fresnel0);
        preLightData.energyCompensationFactor[BASE_LOBEB_IDX] = GetEnergyCompensationFactor(specularReflectivity[BASE_LOBEB_IDX], bsdfData.fresnel0);


#else
        preLightData.energyCompensationFactor[BASE_LOBEA_IDX] =
        preLightData.energyCompensationFactor[BASE_LOBEB_IDX] = 1.0;
#endif

    } //...else !IsVLayeredEnabled

    // Apply  * (1-bsdfData.lobeMix) and * (bsdfData.lobeMix) to the FGD themselves
    // so we don't need to mix elsewhere (makes sense also in the context of what is FGD)
    // (todo check we dont mix again elsewhere)
    preLightData.specularFGD[BASE_LOBEA_IDX] *= (1-bsdfData.lobeMix);
    preLightData.specularFGD[BASE_LOBEB_IDX] *= (bsdfData.lobeMix);

    preLightData.diffuseFGD = lerp(diffuseFGD[0], diffuseFGD[1], bsdfData.lobeMix);

#ifdef LIT_DIFFUSE_LAMBERT_BRDF
    // TODO: DiffuseFGD not done anyway, applied on bakeddiffuse:
    preLightData.diffuseFGD = 1.0;
#endif


    return preLightData;
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

//
// GetBakedDiffuseLighting will be called from ShaderPassForward.hlsl.
//
// GetBakedDiffuseLighting function compute the bake lighting + emissive color to be store in emissive buffer (Deferred case)
// In forward it must be add to the final contribution.
// This function require the 3 structure surfaceData, builtinData, bsdfData because it may require both the engine side data, and data that will not be store inside the gbuffer.
float3 GetBakedDiffuseLighting(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData, PreLightData preLightData)
{
    // TODO: Handle SSS

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // The lighting in SH or lightmap is assume to contain bounced light only (i.e no direct lighting), and is divide by PI (i.e Lambert is apply), so multiply by PI here to get back the illuminance
        return builtinData.bakeDiffuseLighting * PI;
    }
#endif

    // Premultiply bake diffuse lighting information
    return builtinData.bakeDiffuseLighting * surfaceData.ambientOcclusion * bsdfData.diffuseColor + builtinData.emissiveColor;
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

    //NEWLITTODO
    //float roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
    //lightTransportData.diffuseColor = bsdfData.diffuseColor + bsdfData.fresnel0 * roughness * 0.5 * surfaceData.metallic;
    lightTransportData.diffuseColor = bsdfData.diffuseColor;
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
// For /Lighting/LightEvaluation.hlsl:
#define USE_DEFERRED_DIRECTIONAL_SHADOWS // Deferred shadows are always enabled for opaque objects
#endif

#include "HDRP/Material/MaterialEvaluation.hlsl"
#include "HDRP/Lighting/LightEvaluation.hlsl"

#include "HDRP/Lighting/Reflection/VolumeProjection.hlsl"

//-----------------------------------------------------------------------------
// BSDF share between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

// BSDF helpers

//
// The following is to streamline the usage (in the BSDF evaluations) of NdotL[] and NdotV[]
// regardless of the reason why we need [2] sized arrays:
//
#if defined(VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE) || defined(_MATERIAL_FEATURE_COAT_NORMALMAP)
#define DNLV_COAT_IDX 0
#define DNLV_BASE_IDX 1
#else
#define DNLV_COAT_IDX 0
#define DNLV_BASE_IDX 0
#endif
#if ((DNLV_BASE_IDX != BASE_NORMAL_IDX) && (DNLV_BASE_IDX != BOTTOM_DIR_IDX)) || ((DNLV_COAT_IDX != COAT_NORMAL_IDX) && (DNLV_BASE_IDX != COAT_DIR_IDX))
#error "DIR and NORMAL indices should match"
#endif

#if defined(VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE)
    // static assert:
#if defined(_MATERIAL_FEATURE_COAT_NORMALMAP) && ((NB_LV_DIR != 2) || ( NB_NORMALS != 2) || (BASE_NORMAL_IDX != BOTTOM_DIR_IDX) || (COAT_NORMAL_IDX != TOP_DIR_IDX))
#error "Unexpected NB_NORMALS and/or NB_LV_DIR, should be 2 or unmatching indices between DIR_IDX vs NORMAL_IDX"
#endif
#define NDOTLV_SIZE NB_LV_DIR
#else
#define NDOTLV_SIZE NB_NORMALS
#endif //...defined(VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE)



//BSDF_SetupNormalsAndAngles(bsdfData, preLightData, inNdotL,
//                           L, V, N, NdotL,
//                           H, NdotH, savedLdotH, NdotV);

void BSDF_SetupNormalsAndAngles(BSDFData bsdfData, inout PreLightData preLightData, float inNdotL,
                                inout float3 L[NB_LV_DIR], inout float3 V[NB_LV_DIR], out float3 N[NB_NORMALS], out float NdotL[NDOTLV_SIZE],
                                out float3 H, out float NdotH[NB_NORMALS], out float savedLdotH, out float NdotV[NDOTLV_SIZE])
{
    H = float3(0.0, 0.0, 0.0);
    N[BASE_NORMAL_IDX] = bsdfData.normalWS;

#if defined(_MATERIAL_FEATURE_COAT_NORMALMAP)
    if ( IsCoatNormalMapEnabled(bsdfData) )
    {
        N[COAT_NORMAL_IDX] = bsdfData.coatNormalWS;
    }
#endif

    // preLightData.NdotV is sized with NB_NORMALS, not NDOTLV_SIZE, because the only useful
    // precalculation that is not per light is NdotV with the coat normal (if it exists) and
    // NdotV with the base normal (if coat normal exists), with the same V.
    // But if VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE is used, V is different at the bottom,
    // and we will recalculate a refracted (along H) V here, per light, and the resulting base
    // NdotV.
    float unclampedNdotV[NB_NORMALS] = preLightData.NdotV;

    // static assert (to simplify code, COAT_NORMAL_IDX == BASE_NORMAL_IDX when no coat normal):
#if !defined(_MATERIAL_FEATURE_COAT_NORMALMAP) && (BASE_NORMAL_IDX != COAT_NORMAL_IDX)
#error "COAT_NORMAL_IDX shoud equal BASE_NORMAL_IDX when no coat normal map"
#endif

    // We will compute everything needed for top interface lighting, and this will alias to the
    // (only) bottom interface when we're not vlayered (if we are and have a coat normal, we will
    // use it as we transfered coatNormalWS above and if we don't, again, the only existing
    // normal will be used)

    // todo: inNdotL is with geometric N for now, so we don't use it.
    NdotL[DNLV_COAT_IDX] = dot(N[COAT_NORMAL_IDX], L[TOP_DIR_IDX]);

    // Optimized math. Ref: PBR Diffuse Lighting for GGX + Smith Microsurfaces (slide 114).
    float LdotV = dot(L[TOP_DIR_IDX], V[TOP_DIR_IDX]); // note: LdotV isn't reused elsewhere, just here.
    float invLenLV = rsqrt(max(2.0 * LdotV + 2.0, FLT_EPS)); // invLenLV = rcp(length(L + V)), clamp to avoid rsqrt(0) = NaN
    savedLdotH = saturate(invLenLV * LdotV + invLenLV);
    NdotH[COAT_NORMAL_IDX] = saturate((NdotL[DNLV_COAT_IDX] + unclampedNdotV[COAT_NORMAL_IDX]) * invLenLV); // Do not clamp NdotV here
    NdotV[DNLV_COAT_IDX] = ClampNdotV(unclampedNdotV[COAT_NORMAL_IDX]);

    if( IsVLayeredEnabled(bsdfData) )
    {
#ifdef _MATERIAL_FEATURE_COAT

        // NdotH[] is size 2 only if we have two normal maps, but it doesn't change if we refract along
        // H itself, so refracted angles considerations don't matter for it. Still we need to calculate
        // it if we have two normal maps. Note we don't even test _MATERIAL_FEATURE_COAT_NORMALMAP, as
        // BASE_NORMAL_IDX should alias COAT_NORMAL_IDX if there's no coat normalmap, and this line
        // becomes a nop.
        // We still reuse the top directions to be able to reuse computations above, regardless of
        // refracted angle option since NdotH is invariant to it.
        NdotH[BASE_NORMAL_IDX] = saturate((dot(N[BASE_NORMAL_IDX], L[TOP_DIR_IDX]) + unclampedNdotV[BASE_NORMAL_IDX]) * invLenLV); // Do not clamp NdotV here

#if defined(VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE) || defined(_MATERIAL_FEATURE_ANISOTROPY)
        // In both of these cases, we need H, so get this out of the way now:
        H = (L[TOP_DIR_IDX] + V[TOP_DIR_IDX]) * invLenLV; // H stays the same so calculate it one time
#endif

#ifdef VLAYERED_RECOMPUTE_PERLIGHT
        // TODOWIP
        // Must call ComputeAdding and update partLambdaV
        ComputeAdding(savedLdotH, V[TOP_DIR_IDX], bsdfData, preLightData, true);
        // Notice the top LdotH as interface angle, symmetric model parametrization (see paper sec. 6 and comments
        // on ComputeAdding)
        // layered*Roughness* and vLayerEnergyCoeff are now updated for the proper light direction.

        // !Updates to PartLambdaV are now needed, will be done later when we consider anisotropy.

        // Note (see p9 eq(39)): if we don't recompute per light, we just reuse the IBL energy terms as the fresnel
        // terms for our LdotH, too bad (similar to what we do with iridescence), along with the "wrongly" calculated
        // energy.
        // In any case, we should have used FGD terms (except for R12 at the start of the process) for the analytical
        // light case, see comments at the top of ComputeAdding
#endif //...VLAYERED_RECOMPUTE_PERLIGHT


#ifdef VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE

        // Also, we will use the base normal map automatically if we have dual normal maps (coat normals)
        // since we use generically N[BASE_NORMAL_IDX]

        // TODOWIP

        // Use the refracted angle at the bottom interface for BSDF calculations:
        // Seems like the more correct ones to use, but not obvious as we have the energy
        // coefficients already (vLayerEnergyCoeff), which are like FGD (but no deferred
        // FGD fetch to do here for analytical lights), so normally, we should use
        // an output lobe parametrization, but multiple-scattering is not accounted fully
        // by ComputeAdding (for anisotropy by deriving azimuth dependent covariance operators).
        // In the IBL case, we don't have a specific incoming light direction so the light
        // representation matches more the correct context of a split sum approximation,
        // though we don't handle anisotropy correctly either anyway.
        // In both cases we need to work around it.
        //
        // Using refracted angles for BSDF eval for the base in the case of analytical lights
        // must be seen as a hack on top of the ComputeAdding method in which we consider that
        // even though the output (energy coefficients) of the method are statistical averages
        // over all incoming light directions and so to be used in the context of a split sum
        // approximation, the analytical lights have all their energy in a specific ray direction
        // and moreover, currently, the output coefficients of the method are formed from straight
        // Fresnel terms, and not FGD terms.
        //
        V[BOTTOM_DIR_IDX] = CoatRefract(V[TOP_DIR_IDX], H, preLightData.coatIeta);
        L[BOTTOM_DIR_IDX] = reflect(-V[BOTTOM_DIR_IDX], H);
        NdotL[DNLV_BASE_IDX] = dot(N[BASE_NORMAL_IDX], L[BOTTOM_DIR_IDX]);

        float unclampedBaseNdotV = dot(N[BASE_NORMAL_IDX], V[BOTTOM_DIR_IDX]);
        NdotV[DNLV_BASE_IDX] = ClampNdotV(unclampedBaseNdotV);

        //NdotH[BASE_NORMAL_IDX] = already calculated, see above

#else //, don't use refracted angles for bottom interface:

#if defined(_MATERIAL_FEATURE_COAT_NORMALMAP)
        if ( IsCoatNormalMapEnabled(bsdfData) )
        {
            // Just to be clean we test the above, but since BASE_NORMAL_IDX should alias COAT_NORMAL_IDX
            // if we don't have coat normals and no refracted angle to account, this is already computed
            // and the compiler would remove this.
            NdotL[DNLV_BASE_IDX] = dot(N[BASE_NORMAL_IDX], L[BOTTOM_DIR_IDX]);
            //NdotH[BASE_NORMAL_IDX] = saturate((NdotL[DNLV_BASE_IDX] + unclampedNdotV[BASE_NORMAL_IDX]) * invLenLV); // Do not clamp NdotV here
            NdotV[DNLV_BASE_IDX] = ClampNdotV(unclampedNdotV[BASE_NORMAL_IDX]);
        }
#endif

#endif // #ifdef VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE


        // Finally, we will update the partLambdaV if we did ComputeAdding per light, having the proper
        // angles wrt to refraction option and/or dual normal maps and considering anisotropy:

        if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
        {
#ifdef VLAYERED_RECOMPUTE_PERLIGHT
#ifdef VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE
            // we changed V, update those:
            preLightData.TdotV = dot(bsdfData.tangentWS,   V[BOTTOM_DIR_IDX]);
            preLightData.BdotV = dot(bsdfData.bitangentWS, V[BOTTOM_DIR_IDX]);
#endif
            // we need to update partLambdaV as we recomputed the layering for this light (these depend on roughness):
            preLightData.partLambdaV[COAT_LOBE_IDX] = GetSmithJointGGXPartLambdaV(NdotV[DNLV_COAT_IDX], preLightData.layeredCoatRoughness);
            preLightData.partLambdaV[BASE_LOBEA_IDX] = GetSmithJointGGXAnisoPartLambdaV(preLightData.TdotV, preLightData.BdotV, NdotV[DNLV_BASE_IDX],
                                                                                        preLightData.layeredRoughnessT[0], preLightData.layeredRoughnessB[0]);
            preLightData.partLambdaV[BASE_LOBEB_IDX] = GetSmithJointGGXAnisoPartLambdaV(preLightData.TdotV, preLightData.BdotV, NdotV[DNLV_BASE_IDX],
                                                                                        preLightData.layeredRoughnessT[1], preLightData.layeredRoughnessB[1]);
#endif
        } // anisotropy
        else
        {
#ifdef VLAYERED_RECOMPUTE_PERLIGHT
            // we need to update partLambdaV as we recomputed the layering for this light (these depend on roughness):
            preLightData.partLambdaV[COAT_LOBE_IDX] = GetSmithJointGGXPartLambdaV(NdotV[DNLV_COAT_IDX], preLightData.layeredCoatRoughness);
            preLightData.partLambdaV[BASE_LOBEA_IDX] = GetSmithJointGGXPartLambdaV(NdotV[DNLV_BASE_IDX], preLightData.layeredRoughnessT[0]);
            preLightData.partLambdaV[BASE_LOBEB_IDX] = GetSmithJointGGXPartLambdaV(NdotV[DNLV_BASE_IDX], preLightData.layeredRoughnessT[1]);
#endif
        } //...no anisotropy

#endif // _MATERIAL_FEATURE_COAT
    }
    else
    {
        // No vlayering, just check if we need H:
        if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
        {
            H = (L[0] + V[0]) * invLenLV;
        }
    }

} //...BSDF_SetupNormalsAndAngles

void CalculateAnisoAngles(BSDFData bsdfData, float3 H, float3 L, float3 V, out float TdotH, out float TdotL, out float BdotH, out float BdotL)
{
    // For anisotropy we must not saturate these values
    TdotH = dot(bsdfData.tangentWS, H);
    TdotL = dot(bsdfData.tangentWS, L);
    BdotH = dot(bsdfData.bitangentWS, H);
    BdotL = dot(bsdfData.bitangentWS, L);
}


// This function apply BSDF. Assumes that NdotL is positive.
void BSDF(float3 inV, float3 inL, float inNdotL, float3 positionWS, PreLightData preLightData, BSDFData bsdfData,
          out float3 diffuseLighting,
          out float3 specularLighting)
{
    float NdotL[NDOTLV_SIZE];
    float NdotV[NDOTLV_SIZE];
    // IMPORTANT: use DNLV_COAT_IDX and DNLV_BASE_IDX to index NdotL and NdotV since they can be sized 2
    // either if we have to deal with refraction or if we use dual normal maps: NB_LV_DIR is for L or V,
    // while NB_NORMALS is for N, but in the cases of NdotL, NdotV, they will be sized
    // max(NB_LV_DIR, NB_NORMALS)

    float3 L[NB_LV_DIR], V[NB_LV_DIR];
    float3 N[NB_NORMALS];

    float savedLdotH;
    // ...only one needed: when vlayered, only top needed for input to ComputeAdding and even then, only if we recompute per light,
    // otherwise, no vlayered and base one is used for a standard Fresnel calculation.
    float3 H = (float3)0; // might not be needed if no refracted_angles option and no anisotropy...

    float NdotH[NB_NORMALS]; // NdotH[NB_LV_DIR] not needed since it stays the same wrt a refraction along H itself.

    float3 DV[TOTAL_NB_LOBES]; // BSDF results per lobe

    // Note that we're never missing an initialization for the following as the arrays are sized 1 in the cases
    // we use only the "bottom" parts (which are the same as the top: ie in case we have dual normal maps but no
    // refracted angles to account for).
    L[TOP_DIR_IDX] = inL;
    V[TOP_DIR_IDX] = inV;

    BSDF_SetupNormalsAndAngles(bsdfData, preLightData, inNdotL, L, V, N, NdotL, H, NdotH, savedLdotH, NdotV);


    // TODO: with iridescence, will be optionally per light sample

    if( IsVLayeredEnabled(bsdfData) )
    {
#ifdef _MATERIAL_FEATURE_COAT
        // --------------------------------------------------------------------
        // VLAYERING:
        // --------------------------------------------------------------------

        if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
        {

            float TdotH, TdotL, BdotH, BdotL;
            CalculateAnisoAngles(bsdfData, H, L[BOTTOM_DIR_IDX], V[BOTTOM_DIR_IDX], TdotH, TdotL, BdotH, BdotL);

            DV[BASE_LOBEA_IDX] = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH[BASE_NORMAL_IDX], NdotV[DNLV_BASE_IDX], TdotL, BdotL, NdotL[DNLV_BASE_IDX],
                                                      preLightData.layeredRoughnessT[0], preLightData.layeredRoughnessB[0],
                                                      preLightData.partLambdaV[BASE_LOBEA_IDX]);

            DV[BASE_LOBEB_IDX] = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH[BASE_NORMAL_IDX], NdotV[DNLV_BASE_IDX], TdotL, BdotL, NdotL[DNLV_BASE_IDX],
                                                      preLightData.layeredRoughnessT[1], preLightData.layeredRoughnessB[1],
                                                      preLightData.partLambdaV[BASE_LOBEB_IDX]);

            DV[COAT_LOBE_IDX] = DV_SmithJointGGX(NdotH[COAT_NORMAL_IDX], NdotL[DNLV_COAT_IDX], NdotV[DNLV_COAT_IDX], preLightData.layeredCoatRoughness, preLightData.partLambdaV[COAT_LOBE_IDX]);

        }
        else
        {
            DV[COAT_LOBE_IDX] = DV_SmithJointGGX(NdotH[COAT_NORMAL_IDX], NdotL[DNLV_COAT_IDX], NdotV[DNLV_COAT_IDX], preLightData.layeredCoatRoughness, preLightData.partLambdaV[COAT_LOBE_IDX]);
            DV[BASE_LOBEA_IDX] = DV_SmithJointGGX(NdotH[BASE_NORMAL_IDX], NdotL[DNLV_BASE_IDX], NdotV[DNLV_BASE_IDX],
                                                  preLightData.layeredRoughnessT[0], preLightData.partLambdaV[BASE_LOBEA_IDX]);
            DV[BASE_LOBEB_IDX] = DV_SmithJointGGX(NdotH[BASE_NORMAL_IDX], NdotL[DNLV_BASE_IDX], NdotV[DNLV_BASE_IDX],
                                                  preLightData.layeredRoughnessT[1], preLightData.partLambdaV[BASE_LOBEB_IDX]);
        }

        IF_DEBUG( if(_DebugLobeMask.x == 0.0) DV[COAT_LOBE_IDX] = (float3)0; )
        IF_DEBUG( if(_DebugLobeMask.y == 0.0) DV[BASE_LOBEA_IDX] = (float3)0; )
        IF_DEBUG( if(_DebugLobeMask.z == 0.0) DV[BASE_LOBEB_IDX] = (float3)0; )

        specularLighting =  max(0, NdotL[DNLV_BASE_IDX]) * preLightData.vLayerEnergyCoeff[BOTTOM_VLAYER_IDX]
                          * lerp(DV[BASE_LOBEA_IDX] * preLightData.energyCompensationFactor[BASE_LOBEA_IDX],
                                 DV[BASE_LOBEB_IDX] * preLightData.energyCompensationFactor[BASE_LOBEB_IDX],
                                 bsdfData.lobeMix);

        specularLighting +=  max(0, NdotL[DNLV_COAT_IDX]) * preLightData.vLayerEnergyCoeff[TOP_VLAYER_IDX]
                           * preLightData.energyCompensationFactor[COAT_LOBE_IDX]
                           * DV[COAT_LOBE_IDX];

#endif // ..._MATERIAL_FEATURE_COAT
    } // if( IsVLayeredEnabled(bsdfData) )
    else
    {
        // --------------------------------------------------------------------
        // NO VLAYERING:
        // --------------------------------------------------------------------
        // TODO: Proper Fresnel
        float3 F = F_Schlick(bsdfData.fresnel0, savedLdotH);

        if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
        {
            float TdotH, TdotL, BdotH, BdotL;
            CalculateAnisoAngles(bsdfData, H, L[0], V[0], TdotH, TdotL, BdotH, BdotL);
            DV[0] = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH[0], NdotV[0], TdotL, BdotL, NdotL[0],
                                          bsdfData.roughnessAT, bsdfData.roughnessAB,
                                          preLightData.partLambdaV[0]);
            DV[1] = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH[0], NdotV[0], TdotL, BdotL, NdotL[0],
                                          bsdfData.roughnessBT, bsdfData.roughnessBB,
                                          preLightData.partLambdaV[1]);
        }
        else
        {
            DV[0] = DV_SmithJointGGX(NdotH[0], NdotL[0], NdotV[0], bsdfData.roughnessAT, preLightData.partLambdaV[0]);
            DV[1] = DV_SmithJointGGX(NdotH[0], NdotL[0], NdotV[0], bsdfData.roughnessBT, preLightData.partLambdaV[1]);
        }

        IF_DEBUG( if(_DebugLobeMask.y == 0.0) DV[BASE_LOBEA_IDX] = (float3)0; )
        IF_DEBUG( if(_DebugLobeMask.z == 0.0) DV[BASE_LOBEB_IDX] = (float3)0; )

        specularLighting = max(0, NdotL[0]) * F * lerp(DV[0]*preLightData.energyCompensationFactor[BASE_LOBEA_IDX],
                                                       DV[1]*preLightData.energyCompensationFactor[BASE_LOBEB_IDX],
                                                       bsdfData.lobeMix);
        //...and energy compensation is applied at PostEvaluateBSDF when no vlayering.
    }


    // TODO: config option + diffuse GGX
    float3 diffuseTerm = Lambert() * max(0, NdotL[DNLV_BASE_IDX]);

#ifdef VLAYERED_DIFFUSE_ENERGY_HACKED_TERM
    // TODOTODO: Energy when vlayered.
    if( IsVLayeredEnabled(bsdfData) )
    {
        diffuseTerm *= preLightData.diffuseEnergy;
    }
#endif

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    diffuseLighting = diffuseTerm;

}//...BSDF


float3 EvaluateTransmission(BSDFData bsdfData, float3 transmittance, float NdotL, float NdotV, float LdotV, float attenuation)
{
    // Apply wrapped lighting to better handle thin objects at grazing angles.
    float wrappedNdotL = ComputeWrappedDiffuseLighting(-NdotL, SSS_WRAP_LIGHT);

    // Apply BSDF-specific diffuse transmission to attenuation. See also: [SSS-NOTE-TRSM]
    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    attenuation *= Lambert();

    float intensity = attenuation * wrappedNdotL;
    return intensity * transmittance;
}

void EvaluateBSDF_GetNormalUnclampedNdotV(BSDFData bsdfData, PreLightData preLightData, float3 V, out float3 N, out float unclampedNdotV)
{
    //TODO: This affects transmission and SSS, choose the normal the use when we have
    // both. For now, just use the base:
    N = bsdfData.normalWS;
    unclampedNdotV = preLightData.NdotV[BASE_NORMAL_IDX];

#ifdef _MATERIAL_FEATURE_COAT_NORMALMAP
    if ( IsCoatNormalMapEnabled(bsdfData) )
    {
#ifdef _STACKLIT_DEBUG
        if(_DebugLobeMask.w == 2.0)
        {
            N = bsdfData.coatNormalWS;
            unclampedNdotV = preLightData.NdotV[COAT_NORMAL_IDX];
        }
        else if(_DebugLobeMask.w == 3.0)
        {
            N = bsdfData.geomNormalWS;
            unclampedNdotV = preLightData.geomNdotV;
        }
#endif
    }
#endif // _MATERIAL_FEATURE_COAT_NORMALMAP
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

    float3 N; float unclampedNdotV;
    EvaluateBSDF_GetNormalUnclampedNdotV(bsdfData, preLightData, V, N, unclampedNdotV);

    float3 L     = -lightData.forward; // Lights point backward in Unity
    float  NdotV = ClampNdotV(unclampedNdotV);
    float  NdotL = dot(N, L);
    float  LdotV = dot(L, V);

    // color and attenuation are outputted  by EvaluateLight:
    float3 color;
    float attenuation;

    // For shadow attenuation (ie receiver bias), always use the geometric normal:
    EvaluateLight_Directional(lightLoopContext, posInput, lightData, bakeLightingData, bsdfData.geomNormalWS, L, color, attenuation);

    float intensity = max(0, attenuation); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: the NdotL term is now applied in the BSDF() eval itself to account for different normals.
    UNITY_BRANCH if (intensity > 0.0)
    {
        BSDF(V, L, NdotL, posInput.positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * lightData.diffuseScale;
        lighting.specular *= intensity * lightData.specularScale;
    }

    // The mixed thickness mode is not supported by directional lights due to poor quality and high performance impact.
    bool mixedThicknessMode = HasFeatureFlag(bsdfData.materialFeatures, MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_MIXED_THICKNESS);

    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION) && !mixedThicknessMode)
    {
        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        lighting.diffuse += EvaluateTransmission(bsdfData, bsdfData.transmittance, NdotL, NdotV, LdotV, attenuation * lightData.diffuseScale);
    }

    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        intensity = max(0, attenuation * NdotL);
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

    float3 N; float unclampedNdotV;
    EvaluateBSDF_GetNormalUnclampedNdotV(bsdfData, preLightData, V, N, unclampedNdotV);

    float  NdotV = ClampNdotV(unclampedNdotV);
    float  NdotL = dot(N, L);
    float  LdotV = dot(L, V);

    bool mixedThicknessMode = HasFeatureFlag(bsdfData.materialFeatures, MATERIAL_FEATURE_FLAGS_TRANSMISSION_MODE_MIXED_THICKNESS)
                              && NdotL < 0 && lightData.shadowIndex >= 0;

    // Save the original version for the transmission code below.
    int originalShadowIndex = lightData.shadowIndex;

    if (mixedThicknessMode)
    {
        // Make sure we do not sample the shadow map twice.
        lightData.shadowIndex = -1;
    }

    float3 color;
    float attenuation;

    // For shadow attenuation (ie receiver bias), always use the geometric normal:
    EvaluateLight_Punctual(lightLoopContext, posInput, lightData, bakeLightingData, bsdfData.geomNormalWS, L,
                           lightToSample, distances, color, attenuation);

    // Restore the original shadow index.
    lightData.shadowIndex = originalShadowIndex;

    float intensity = max(0, attenuation); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: the NdotL term is now applied in the BSDF() eval itself to account for different normals.
    UNITY_BRANCH if (intensity > 0.0)
    {
        // Simulate a sphere light with this hack
        // Note that it is not correct with our pre-computation of PartLambdaV (mean if we disable the optimization we will not have the
        // same result) but we don't care as it is a hack anyway

        //NEWLITTODO: Do we want this hack in stacklit ? Yes we have area lights, but cheap and not much maintenance to leave it here.
        // For now no roughness anyways.

        //bsdfData.coatRoughness = max(bsdfData.coatRoughness, lightData.minRoughness);
        //bsdfData.roughnessT = max(bsdfData.roughnessT, lightData.minRoughness);
        //bsdfData.roughnessB = max(bsdfData.roughnessB, lightData.minRoughness);

        BSDF(V, L, NdotL, posInput.positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * lightData.diffuseScale;
        lighting.specular *= intensity * lightData.specularScale;
    }

    if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION))
    {
        float3 transmittance = bsdfData.transmittance;

        if (mixedThicknessMode)
        {
            // Recompute transmittance using the thickness value computed from the shadow map.

            // Compute the distance from the light to the back face of the object along the light direction.
            float distBackFaceToLight = GetPunctualShadowClosestDistance(lightLoopContext.shadowContext, s_linear_clamp_sampler,
                                                                         posInput.positionWS, lightData.shadowIndex, L, lightData.positionWS);

            // Our subsurface scattering models use the semi-infinite planar slab assumption.
            // Therefore, we need to find the thickness along the normal.
            float distFrontFaceToLight   = distances.x;
            float thicknessInUnits       = (distFrontFaceToLight - distBackFaceToLight) * -NdotL;
            float thicknessInMeters      = thicknessInUnits * _WorldScales[bsdfData.diffusionProfile].x;
            float thicknessInMillimeters = thicknessInMeters * MILLIMETERS_PER_METER;

        #if SHADEROPTIONS_USE_DISNEY_SSS
            // We need to make sure it's not less than the baked thickness to minimize light leaking.
            float thicknessDelta = max(0, thicknessInMillimeters - bsdfData.thickness);

            float3 S = _ShapeParams[bsdfData.diffusionProfile].rgb;

            // Approximate the decrease of transmittance by e^(-1/3 * dt * S).
        #if 0
            float3 expOneThird = exp(((-1.0 / 3.0) * thicknessDelta) * S);
        #else
            // Help the compiler.
            float  k = (-1.0 / 3.0) * LOG2_E;
            float3 p = (k * thicknessDelta) * S;
            float3 expOneThird = exp2(p);
        #endif

            transmittance *= expOneThird;

        #else // SHADEROPTIONS_USE_DISNEY_SSS

            // We need to make sure it's not less than the baked thickness to minimize light leaking.
            thicknessInMillimeters = max(thicknessInMillimeters, bsdfData.thickness);

            transmittance = ComputeTransmittanceJimenez(_HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][0].rgb,
                                                        _HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][0].a,
                                                        _HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][1].rgb,
                                                        _HalfRcpVariancesAndWeights[bsdfData.diffusionProfile][1].a,
                                                        _TransmissionTintsAndFresnel0[bsdfData.diffusionProfile].rgb,
                                                        thicknessInMillimeters);
        #endif // SHADEROPTIONS_USE_DISNEY_SSS
        }

        // Note: we do not modify the distance to the light, or the light angle for the back face.
        // This is a performance-saving optimization which makes sense as long as the thickness is small.
        // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
        lighting.diffuse += EvaluateTransmission(bsdfData, transmittance, NdotL, NdotV, LdotV, attenuation * lightData.diffuseScale);
    }

    // Save ALU by applying light and cookie colors only once.
    lighting.diffuse  *= color;
    lighting.specular *= color;

#ifdef DEBUG_DISPLAY
    if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
    {
        // Only lighting, not BSDF
        intensity = max(0, attenuation * NdotL);
        lighting.diffuse = color * intensity * lightData.diffuseScale;
    }
#endif

    return lighting;
}

// NEWLITTODO: For a refence rendering option for area light, like LIT_DISPLAY_REFERENCE_AREA option in eg EvaluateBSDF_<area light type> :
//#include "LitReference.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Line(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BakeLightingData bakeLightingData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    //NEWLITTODO

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

    //NEWLITTODO

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
                                            EnvLightData envLightData,
                                            int GPUImageBasedLightingType,
                                            inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    //NEWLITTODO

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
    //return lighting;
    // TODO: Refraction
    // There is no coat handling in Lit for refractions.
    // Here handle one lobe instead of all the others basically, or we could want it all.
    // Could use proper transmission term T0i when vlayered and a total refraction lobe
    // variance (need to get it in ComputeAdding, TODOTODO)

#if !HAS_REFRACTION
    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION)
        return lighting;
#endif

    float3 envLighting = float3(0.0, 0.0, 0.0);
    float3 positionWS = posInput.positionWS;
    float weight = 0.0;

#ifdef LIT_DISPLAY_REFERENCE_IBL

    envLighting = IntegrateSpecularGGXIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);

    // TODO: Do refraction reference (is it even possible ?)
    // TODO: handle coat

    // TODO: Handle all lobes in reference


//    #ifdef LIT_DIFFUSE_LAMBERT_BRDF
//    envLighting += IntegrateLambertIBLRef(lightData, V, bsdfData);
//    #else
//    envLighting += IntegrateDisneyDiffuseIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);
//    #endif

#else

    float3 R[TOTAL_NB_LOBES];
    float tempWeight[TOTAL_NB_LOBES];
    int i;

    for( i = 0; i < TOTAL_NB_LOBES; ++i)
    {
       R[i] = preLightData.iblR[i];
       tempWeight[i] = 1.0;
    }


    // We will sample one time for each lobe the environment.
    // Steps are:

    // -Calculate influence weights from intersection with the proxies.
    // Since the weights are influence blending weights, we can correctly
    // use our lobe weight and mix them.
    // -Fudge the sampling direction to dampen boundary artefacts.
    // -Fetch samples of preintegrated environment lighting
    // (see preLD, first part of the split-sum approx.)
    // -Use the BSDF preintegration terms we pre-fetched in preLightData
    // (second part of the split-sum approx.,
    //  and common to all Env. Lights. using the same BSDF and
    //  we only have GGX thus only one FGD map for now)
    // -Multiply the two split sum terms together for each lobe
    // and lerp them and/or add them.

    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)

    //slnote dual map
    float3 influenceNormal; float unclampedNdotV;
    EvaluateBSDF_GetNormalUnclampedNdotV(bsdfData, preLightData, V, influenceNormal, unclampedNdotV);

    for ( i = 0; i < TOTAL_NB_LOBES; ++i)
    {
        float3 L;

#ifdef VLAYERED_DEBUG
        IF_FEATURE_COAT( if( (i == 0) && _DebugEnvLobeMask.x == 0.0) continue; )
        if( (i == (0 IF_FEATURE_COAT(+1))) && _DebugEnvLobeMask.y == 0.0) continue;
        if( (i == (1 IF_FEATURE_COAT(+1))) && _DebugEnvLobeMask.z == 0.0) continue;
#endif
        EvaluateLight_EnvIntersection(positionWS, influenceNormal, lightData, influenceShapeType, R[i], tempWeight[i]);

        // When we are rough, we tend to see outward shifting of the reflection when at the boundary of the projection volume
        // Also it appear like more sharp. To avoid these artifact and at the same time get better match to reference we lerp to original unmodified reflection.
        // Formula is empirical.
        float roughness = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness[i]);
        R[i] = lerp(R[i], preLightData.iblR[i], saturate(smoothstep(0, 1, roughness * roughness)));


        float iblMipLevel;
        // TODO: We need to match the PerceptualRoughnessToMipmapLevel formula for planar, so we don't do this test (which is specific to our current lightloop)
        // Specific case for Texture2Ds, their convolution is a gaussian one and not a GGX one - So we use another roughness mip mapping.
        if (IsEnvIndexTexture2D(lightData.envIndex))
        {
            // Empirical remapping
            iblMipLevel = PositivePow(preLightData.iblPerceptualRoughness[i], 0.8) * uint(max(_ColorPyramidScale.z - 1, 0));
        }
        else
        {
            iblMipLevel = PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness[i]);
        }

        float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R[i], iblMipLevel);

        // Used by planar reflection to discard pixel:
        tempWeight[i] *= preLD.a;

        L = preLD.rgb * preLightData.specularFGD[i];
        if( IsVLayeredEnabled(bsdfData) )
        {
            // TODOENERGY: should be done in ComputeAdding with FGD formulation for IBL.
            // Note that when we're not vlayered, we apply it not at each light sample but at the end,
            // at PostEvaluateBSDF.
            // Incorrect, but just for now:
            L *= preLightData.energyCompensationFactor[i];
        }
        envLighting += L;
    }

    // TODO: to combine influence weights, mean or max or ... ?
    for( i = 0; i < TOTAL_NB_LOBES; ++i)
    {
       weight += tempWeight[i];
    }
    weight /= TOTAL_NB_LOBES;
    weight = tempWeight[1];

#endif // LIT_DISPLAY_REFERENCE_IBL

    UpdateLightingHierarchyWeights(hierarchyWeight, weight);
    envLighting *= weight * lightData.multiplier;

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
        lighting.specularReflected = envLighting;
    //TODO refraction:
    //else
    //    lighting.specularTransmitted = envLighting * preLightData.transparentTransmittance;

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
    float3 N;
    float unclampedNdotV;
    EvaluateBSDF_GetNormalUnclampedNdotV(bsdfData, preLightData, V, N, unclampedNdotV);

    AmbientOcclusionFactor aoFactor;
    // Use GTAOMultiBounce approximation for ambient occlusion (allow to get a tint from the baseColor)
    //GetScreenSpaceAmbientOcclusionMultibounce(posInput.positionSS, preLightData.NdotV, lerp(bsdfData.perceptualRoughnessA, bsdfData.perceptualRoughnessB, bsdfData.lobeMix), bsdfData.ambientOcclusion, 1.0, bsdfData.diffuseColor, bsdfData.fresnel0, aoFactor);
    GetScreenSpaceAmbientOcclusionMultibounce(posInput.positionSS, unclampedNdotV, lerp(bsdfData.perceptualRoughnessA, bsdfData.perceptualRoughnessB, bsdfData.lobeMix), bsdfData.ambientOcclusion, 1.0, bsdfData.diffuseColor, bsdfData.fresnel0, aoFactor);

    ApplyAmbientOcclusionFactor(aoFactor, bakeLightingData, lighting);

    // Subsurface scattering mdoe
    uint   texturingMode = (bsdfData.materialFeatures >> MATERIAL_FEATURE_FLAGS_SSS_TEXTURING_MODE_OFFSET) & 3;
    float3 modifiedDiffuseColor = ApplySubsurfaceScatteringTexturingMode(texturingMode, bsdfData.diffuseColor);

    // Apply the albedo to the direct diffuse lighting (only once). The indirect (baked)
    // diffuse lighting has already had the albedo applied in GetBakedDiffuseLighting().
    diffuseLighting = modifiedDiffuseColor * lighting.direct.diffuse + bakeLightingData.bakeDiffuseLighting;

    specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, bakeLightingData, lighting, bsdfData.diffuseColor, diffuseLighting, specularLighting);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
