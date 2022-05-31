//-----------------------------------------------------------------------------
// Includes
//-----------------------------------------------------------------------------

// SurfaceData is defined in StackLit.cs which generates StackLit.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StackLit/StackLit.cs.hlsl"
// Those define allow to include desired SSS/Transmission functions
#define MATERIAL_INCLUDE_SUBSURFACESCATTERING
#define MATERIAL_INCLUDE_TRANSMISSION
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SubsurfaceScattering/SubsurfaceScattering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

//-----------------------------------------------------------------------------
// Configuration
//-----------------------------------------------------------------------------
#define _VLAYERED_DUAL_NORMALS_TOP_FIX_FLIP // Artistic choice, but this first is better as it tries to keep Fresnel influence
//#define _VLAYERED_DUAL_NORMALS_TOP_FIX_GEOM

// Choose between Lambert diffuse and Disney diffuse (enable only one of them)
// TODO Disney Diffuse
#define USE_DIFFUSE_LAMBERT_BRDF // For transmission

#define STACK_LIT_USE_GGX_ENERGY_COMPENSATION

// Enable reference mode for IBL and area lights
// Both reference defined below can be defined only if LightLoop is present, else we get a compile error
#ifdef HAS_LIGHTLOOP
// TODO for stacklit
// #define STACK_LIT_DISPLAY_REFERENCE_AREA
// #define STACK_LIT_DISPLAY_REFERENCE_IBL
#endif

//-----------------------------------------------------------------------------
// Texture and constant buffer declaration
//-----------------------------------------------------------------------------

//NEWLITTODO : wireup CBUFFERs for uniforms and samplers used:
//
// We need this for AO, Depth/Color pyramids, LTC lights data, FGD pre-integrated data.
//
// Also add options at the top of this file, see Lit.hlsl.

// Required for SSS, GBuffer texture declaration
TEXTURE2D_X(_GBufferTexture0);

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SphericalCapPivot/SPTDistribution.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"

// Get ambient occlusion (indirect) from given data and screenspace one using the high quality GTAOMultiBounce version
float3 GetDiffuseOcclusionGTAOMultiBounce(float diffuseOcclusionFromData, float screenSpaceDiffuseOcclusion, float3 diffuseColor)
{
    return GTAOMultiBounce(min(diffuseOcclusionFromData, screenSpaceDiffuseOcclusion), diffuseColor);
}
// Get and Apply screen space diffuse occlusion only from SSAO on direct lighting
// (Note: mostly artistic parameter, also no data-based AO is used):
void GetApplyScreenSpaceDiffuseOcclusionForDirect(float3 diffuseColor, float screenSpaceDiffuseOcclusion, out float3 directAmbientOcclusion, inout AggregateLighting lighting)
{
    // Ambient occlusion use for direct lighting (directional, punctual, area)
    float directDiffuseOcclusion = lerp(1.0, screenSpaceDiffuseOcclusion, _AmbientOcclusionParam.w);
    directAmbientOcclusion = GTAOMultiBounce(directDiffuseOcclusion, diffuseColor);
    lighting.direct.diffuse *= directAmbientOcclusion;
}
// Get/fill aoFactor from field values
void GetAmbientOcclusionFactor(float3 indirectAmbientOcclusion, float3 indirectSpecularOcclusion, float3 directAmbientOcclusion, out AmbientOcclusionFactor aoFactor)
{
    aoFactor.indirectAmbientOcclusion = indirectAmbientOcclusion;
    aoFactor.indirectSpecularOcclusion = indirectSpecularOcclusion;
    aoFactor.directAmbientOcclusion = directAmbientOcclusion;
    aoFactor.directSpecularOcclusion = float3(1.0, 1.0, 1.0);
}

//...MaterialEvaluation is needed earlier in StackLit for occlusion handling (see PreLightData_SetupOcclusion)

//-----------------------------------------------------------------------------
// Definition
//-----------------------------------------------------------------------------

// Needed for MATERIAL_FEATURE_MASK_FLAGS.
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoop.cs.hlsl"

#if defined(_MATERIAL_FEATURE_COAT) && defined(_MATERIAL_FEATURE_COAT_NORMALMAP)
#   if defined(_VLAYERED_DUAL_NORMALS_TOP_FIX_FLIP) // calculate as if normal was flip, up to the geometric normal
#   define VLAYERED_DUAL_NORMALS_TOP_FIX_FLIP
#   elif defined(_VLAYERED_DUAL_NORMALS_TOP_FIX_GEOM) // use geom normal directly
#   define VLAYERED_DUAL_NORMALS_TOP_FIX_GEOM
#   endif
#endif

// Anisotropic Hack for Area Lights
#ifdef _ANISOTROPY_FOR_AREA_LIGHTS // shader_feature
#define AREA_LIGHTS_SUPPORT_ANISOTROPY // internal to this file
#endif

// Vertically Layered BSDF : "vlayering"

//#define _STACKLIT_DEBUG

#ifdef _STACKLIT_DEBUG
#    define IF_DEBUG(a) a
#    define STACKLIT_DEBUG
#else
#    define IF_DEBUG(a)
#endif

// Vlayer config options:

#ifdef _VLAYERED_RECOMPUTE_PERLIGHT // Now a shader_features
#define VLAYERED_RECOMPUTE_PERLIGHT
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
#    define COAT_LOBE_IDX 0 // Leave coat index == 0, otherwise change IF_FEATURE_COAT etc
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

    // For iridescence, we will reuse the recompute per light keyword even when not vlayered.
    // When vlayered and iridescence is on, iridescence is also automatically recomputed per light too,
    // so the following gives the recompute option for iridescence when NOT vlayered:
#ifdef VLAYERED_RECOMPUTE_PERLIGHT
#    ifdef _MATERIAL_FEATURE_IRIDESCENCE
#    define IRIDESCENCE_RECOMPUTE_PERLIGHT
#    endif
#endif
#    undef VLAYERED_RECOMPUTE_PERLIGHT
#    undef VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE
#    undef _MATERIAL_FEATURE_COAT_NORMALMAP // enforce a "coat enabled subfeature" condition on this shader_feature

#    define COAT_NB_LOBES 0
#    define COAT_LOBE_IDX 0
#    define BASE_LOBEA_IDX 0
#    define BASE_LOBEB_IDX (BASE_LOBEA_IDX+1) // (make sure B is always #A+1)
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

// BASE_NB_LOBES will never be 1, we let the compiler optimize
// everything out from bsdfData.lobeMix = 0;
#define BASE_NB_LOBES 2 // use numeric indices for these arrays
#define TOTAL_NB_LOBES (BASE_NB_LOBES+COAT_NB_LOBES) // use *_LOBE?_IDX for these arrays.

// We have lobe-indexed arrays, and vlayer indexed for the generic vlayer
// ComputeAdding loop:
#define NB_VLAYERS 3
// Use these to index vLayerEnergyCoeff[] !
// vLayer 1 is useless...
#define TOP_VLAYER_IDX 0
#define BOTTOM_VLAYER_IDX 2

//
// Area Lights + Anisotropy
//
#if defined(AREA_LIGHTS_SUPPORT_ANISOTROPY) && defined(_MATERIAL_FEATURE_ANISOTROPY)
#define AREA_LIGHTS_ANISOTROPY_ENABLED (true)
// By using the following indices alias, we avoid using preprocessor #if AREA_LIGHTS_SUPPORT_ANISOTROPY guards
// in accessing orthoBasisViewNormal[] and can use static code branches "if (AREA_LIGHTS_ANISOTROPY_ENABLED)"
#define NB_ORTHOBASISVIEWNORMAL TOTAL_NB_LOBES
#define ORTHOBASIS_VN_COAT_LOBE_IDX COAT_LOBE_IDX
#define ORTHOBASIS_VN_BASE_LOBEA_IDX BASE_LOBEA_IDX
#define ORTHOBASIS_VN_BASE_LOBEB_IDX BASE_LOBEB_IDX

#else

#define AREA_LIGHTS_ANISOTROPY_ENABLED (false)
#define NB_ORTHOBASISVIEWNORMAL NB_NORMALS
#define ORTHOBASIS_VN_COAT_LOBE_IDX COAT_NORMAL_IDX
#define ORTHOBASIS_VN_BASE_LOBEA_IDX BASE_NORMAL_IDX
#define ORTHOBASIS_VN_BASE_LOBEB_IDX BASE_NORMAL_IDX

#endif // #if defined(AREA_LIGHTS_SUPPORT_ANISOTROPY) && defined(_MATERIAL_FEATURE_ANISOTROPY)

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    return bsdfData.geomNormalWS;
}

float GetAmbientOcclusionForMicroShadowing(BSDFData bsdfData)
{
    return bsdfData.ambientOcclusion;
}

// SSReflection
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightDefinition.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Reflection/VolumeProjection.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ScreenSpaceLighting/ScreenSpaceLighting.hlsl"

// The only way to get Coat now is with vlayering
bool IsVLayeredEnabled(BSDFData bsdfData)
{
    return (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT));
}

bool IsCoatNormalMapEnabled(BSDFData bsdfData)
{
    return (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT_NORMAL_MAP));
}

// When no preprocessor exclusions are required, can use these:

// When computing the stack equivalent lobe characteristics when having dual normal
// maps, we have to make a contradictory assumption on the presence of parallel surfaces
// at some level. We then calculate some energy reaching the bottom layer of the stack
// based on Fresnel terms (hack to reduce pre-integrated FGD fetches TODOENERGY).
//
// Normally when shading with normal maps, we clamp / saturate diverse values
// (eg see here BSDF_SetupNormalsAndAngles or CommonLighting's GetBSDFAngle) to avoid
// special casing the BSDF evaluation but still shade according to the normal maps.
// Fresnel is normally evaluated with the LdotH angle, but this normally never "clips"
// to the hemisphere (oriented on the normal) the complete BSDF evaluation as LdotH is
// never negative (H is at most 90 degrees away from L and V).
//
// If the surface is away from us, it makes no sense to have reflections from it but then
// again it should not obstruct other visible surfaces as a complete black point, but
// this is an understood problem with normal maps (cf displacement maps).
//
// For computing the stack, it would be nice to cheat a little if the bottom surface is
// to be visible, and that's what we already intrinsically do when the "recompute per light"
// option is used for dirac (directional|puntual) lights: we use LdotH systematically,
// regardless of the orientation of the normal, and since it is never negative, it won't
// force regions where the energy stats of the stack are zero for the bottom layer when
// the top normals are facing away. We let shading computations take care of darkening.
// Obviously this is still a hack as stated but is more pleasing and is roughly akin to
// having the top layer "folds" as dual-faced.
//
// When no recompute per light is done or we are doing ComputeAdding in the first call in
// GetPreLightData for split-sum type of lights (non dirac), we don't have a particular L
// to use and use clamped NdotV. In that case, the normal is taken as the H vector, and
// there will be regions where NdotV can be negative so is clamped near zero. In that case,
// being in the "grazing angle region", integrated FGD or Fresnel would yeld reflectance
// operators that yield zero energy transmitted to the bottom layer, and everything reflected
// by the top - which will eventually shade to nothing since the top normal is facing away.
//
// To reproduce the effect cheaply without using the "recompute per light" option,
// but also for all split sum lights, we add options to "fixup" the top reflectance to never
// be clamped but act as if it is dual faced. There are two options available: simply use
// the geometric normal on the top since it should not be back facing to begin the
// computations - in that case, we lose the Fresnel variations induced by the top normal map
// and it only affects other parts of BSDF evaluations later for all types of lights.
//
// This is VLAYERED_DUAL_NORMALS_TOP_FIX_GEOM_NORMAL.
//
// Otherwise, we also provide a behavior similar to flipping of the normal, and we even
// saturate a bit less close to zero (than ClampNdotV) to remove the effect of the grazing
// angle.
//
// This is VLAYERED_DUAL_NORMALS_TOP_FIX_FLIP_NORMAL
//
#define VLAYERED_DUAL_NORMALS_TOP_FIX_DEFAULT 0  // do nothing
#define VLAYERED_DUAL_NORMALS_TOP_FIX_GEOM_NORMAL 1
#define VLAYERED_DUAL_NORMALS_TOP_FIX_FLIP_NORMAL 2

int ComputeAdding_GetDualNormalsTopHandlingMethod()
{
    int method = VLAYERED_DUAL_NORMALS_TOP_FIX_DEFAULT;

#if defined(VLAYERED_DUAL_NORMALS_TOP_FIX_FLIP) // this is done up to the geometric normal
    method = VLAYERED_DUAL_NORMALS_TOP_FIX_FLIP_NORMAL;
#elif defined(VLAYERED_DUAL_NORMALS_TOP_FIX_GEOM)
    method = VLAYERED_DUAL_NORMALS_TOP_FIX_GEOM_NORMAL;
#endif

    return method;
}

bool GetRecomputeStackPerLightOption()
{
    bool perLightOption = false;

#ifdef VLAYERED_RECOMPUTE_PERLIGHT
#ifndef _MATERIAL_FEATURE_COAT
#error "Recompute stack per light option but without coat feature enabled?"
#endif
    perLightOption = true;
#endif
    return perLightOption;
}

// Handling of ShaderGraph configured specular occlusion options:
//
// _SCREENSPACE_SPECULAROCCLUSION_METHOD, _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT, _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_DIR
// _DATABASED_SPECULAROCCLUSION_METHOD, _DATABASED_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT

int GetScreenSpaceSpecularOcclusionMethod()
{
    int method = SPECULAR_OCCLUSION_DISABLED;
#ifdef _SCREENSPACE_SPECULAROCCLUSION_METHOD
    method = _SCREENSPACE_SPECULAROCCLUSION_METHOD;
#endif
    return method;
}

int GetScreenSpaceSpecularOcclusionVisibilityFromAoWeight()
{
    int method = BENT_VISIBILITY_FROM_AO_COS;
#ifdef _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT
    method = _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT;
#endif
    return method;
}

// For screenspace SO, if a bent algorithm is used, the SS algorithm should also give the direction.
// It is not the case now, so we have an option to use either the more neutral geometric normal vs
// the baked bent normal (which is more appropriate for the data-based SO).
float3 GetScreenSpaceSpecularOcclusionVisibilityDir(BSDFData bsdfData, float3 interfaceShadingNormal)
{
    int source = BENT_VISIBILITY_DIR_BENT_NORMAL;
    float3 dir = bsdfData.bentNormalWS;

#ifdef _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_DIR
    source = _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_DIR;
#endif

    switch (source)
    {
        case BENT_VISIBILITY_DIR_GEOM_NORMAL:
            dir = bsdfData.geomNormalWS;
            break;
        case BENT_VISIBILITY_DIR_BENT_NORMAL:
            dir = bsdfData.bentNormalWS;
            break;
        case BENT_VISIBILITY_DIR_SHADING_NORMAL:
            dir = interfaceShadingNormal;
            break;
    }

    return dir;
}

int GetDataBasedSpecularOcclusionMethod()
{
    int method = SPECULAR_OCCLUSION_DISABLED;
    //int method = SPECULAR_OCCLUSION_SPTD;
#ifdef _DATABASED_SPECULAROCCLUSION_METHOD
    method = _DATABASED_SPECULAROCCLUSION_METHOD;
#endif
    return method;
}

int GetDataBasedSpecularOcclusionVisibilityFromAoWeight()
{
    int method = BENT_VISIBILITY_FROM_AO_COS_BENT_CORRECTION;
#ifdef _DATABASED_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT
    method = _DATABASED_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT;
#endif
    return method;
}

uint GetSpecularOcclusionBentVisibilityFixupFlags()
{
    uint method = BENT_VISIBILITY_FIXUP_FLAGS_NONE;
#ifdef _BENT_VISIBILITY_FIXUP_FLAGS
    method = _BENT_VISIBILITY_FIXUP_FLAGS;
#endif
    return method;
}

bool GetHonorPerLightMinRoughness()
{
#ifdef _STACK_LIT_HONORS_LIGHT_MIN_ROUGHNESS
    return true;
#else
    return false;
#endif
}

bool GetPerLightMinRoughnessAffectsOnlyCoat()
{
    return false;
}

//
// This is used to have a default clamping behavior of roughness with ClampRoughnessForAnalyticalLights
// if we don't enable the clamping control coming from the light data (GetHonorPerLightMinRoughness == FALSE).
//
// If the option is ON though, this does nothing.
//
// (Note that ClampRoughnessForAnalyticalLights() is still always used in HazeMapping for numerical stability)
float ClampRoughnessForDiracLightsByDefault(float roughness)
{
    if (GetHonorPerLightMinRoughness() == false)
    {
        roughness = ClampRoughnessForAnalyticalLights(roughness);
    }
    return roughness;
}

float ClampRoughnessIfHonorLightMinRoughness(float roughness, float minRoughness = 0.0)
{
    if (GetHonorPerLightMinRoughness())
    {
        roughness = max(roughness, minRoughness);
    }
    return roughness;
}

// Assume bsdfData.normalWS is init
void FillMaterialAnisotropy(float anisotropyA, float anisotropyB, float3 tangentWS, float3 bitangentWS, inout BSDFData bsdfData)
{
    bsdfData.anisotropyA  = anisotropyA;
    bsdfData.anisotropyB  = anisotropyB;
    bsdfData.tangentWS   = tangentWS;
    bsdfData.bitangentWS = bitangentWS;
}

void FillMaterialIridescence(float mask, float thickness, float ior, float iridescenceCoatFixupTIR, float iridescenceCoatFixupTIRClamp, inout BSDFData bsdfData)
{
    bsdfData.iridescenceMask = mask;
    bsdfData.iridescenceThickness = thickness;
    bsdfData.iridescenceIor = ior;
    bsdfData.iridescenceCoatFixupTIR = iridescenceCoatFixupTIR;
    bsdfData.iridescenceCoatFixupTIRClamp = iridescenceCoatFixupTIRClamp;
}

void FillMaterialCoatData(float coatPerceptualRoughness, float coatMask, float coatIor, float coatThickness, float3 coatExtinction, inout BSDFData bsdfData)
{
    bsdfData.coatPerceptualRoughness = coatPerceptualRoughness;
    // Hack: A true coatMask would lead to complications like additionnal base lobes unmodified by the coat,
    // in proportion to 1-coatMask. We lerp the ior with 1 here along with thickness to 0 so that Beer-Lambert attenuation
    // is removed as coatMask -> 0. Some series term in ComputeStatistics are also lerped with their proper neutral values, along
    // with pre-integrated FGD terms (to lerp them to 0).
    bsdfData.coatMask       = coatMask;
    bsdfData.coatIor        = lerp(1.0, coatIor, bsdfData.coatMask);
    bsdfData.coatThickness  = coatThickness * bsdfData.coatMask;
    bsdfData.coatExtinction = coatExtinction;
}

float GetCoatEta(in BSDFData bsdfData)
{
    float eta = bsdfData.coatIor / 1.0;
    //ieta = 1.0 / eta;
    return eta;
}

bool MaterialSupportsTransmission(BSDFData bsdfData)
{
    return HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION);
}

float3 ConvertF0ForAirInterfaceToF0ForNewTopIor(float3 fresnel0, float newTopIor)
{
    float3 ior = Fresnel0ToIor(min(fresnel0, float3(1,1,1)*0.999)); // guard against 1.0
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
        surfaceData.perceptualSmoothnessA = overrideSmoothnessValue;
        surfaceData.perceptualSmoothnessB = overrideSmoothnessValue;
        surfaceData.coatPerceptualSmoothness = overrideSmoothnessValue;
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
    float metallic = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_SPECULAR_COLOR | MATERIALFEATUREFLAGS_STACK_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION) ? 0.0 : surfaceData.metallic;

    float3 diffuseColor = ComputeDiffuseColor(surfaceData.baseColor, metallic);
    bool specularWorkflow = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_SPECULAR_COLOR);
    float3 specularColor = specularWorkflow ? surfaceData.specularColor : ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, IorToFresnel0(surfaceData.dielectricIor));

    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_DIFFUSE_COLOR)
    {
        surfaceData.baseColor = pbrDiffuseColorValidate(diffuseColor, specularColor, surfaceData.metallic > 0.0, !specularWorkflow).xyz;
    }
    else if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_SPECULAR_COLOR)
    {
        surfaceData.baseColor = pbrSpecularColorValidate(diffuseColor, specularColor, surfaceData.metallic > 0.0, !specularWorkflow).xyz;
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

// Warning:
//
// Keep StackLitSubShader.cs in synch with what we do here (see AddPixelShaderSlotsForWriteNormalBufferPasses)
// along with EvaluateBSDF_ScreenSpaceReflection()
NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    NormalData normalData;

    // When using clear cloat we want to use the coat normal for the various deferred effect
    // as it is the most dominant one
    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT) && (surfaceData.coatMask > 0))
    {
        normalData.normalWS = surfaceData.coatNormalWS;
        normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.coatPerceptualSmoothness);
    }
    else
    {
        // In HazyGloss mode. ConvertSurfaceDataToNormalData() would need positionSS and to call
        // ConvertSurfaceDataToBSDFData, might be too heavy for a prepass, maybe find a lightweight approximation
        // of HazeMapping.
        // This is a moot point though: mixing two roughnesses directly in one is already a hack, the
        // resulting lobe isn't representative of this. But for what ConvertSurfaceDataToNormalData() influences
        // (like SSR and shadows), it might be sufficient.
        // Ignoring the case of hazy gloss doesn't seem a bad solution either: lobeMix will be 0 and we will use
        // smoothnessA, which is "incomplete" but since smoothnessA >= smoothnessB, it at least bounds the
        // differing behavior vs the hack with the direct input mode.

        normalData.normalWS = surfaceData.normalWS;
        // Do average mix in case of dual lobe
        normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(lerp(surfaceData.perceptualSmoothnessA, surfaceData.perceptualSmoothnessB, surfaceData.lobeMix));
    }

    return normalData;
}

//-----------------------------------------------------------------------------
// Conversion function for hazy gloss parametrization
//-----------------------------------------------------------------------------

//
// Outputs:
//
//    bsdfData.fresnel0
//    bsdfData.lobeMix
//
//    (and these that only depend on roughnessA/B, hazeExtent and bsdfData.anisotropyB:)
//
//    bsdfData.anisotropyB
//    bsdfData.perceptualRoughnessB
//    bsdfData.roughnessBT
//    bsdfData.roughnessBB
//
//
void HazeMapping(float3 fresnel0, float roughnessAT, float roughnessAB, float haziness, float hazeExtent, float hazeExtentAnisotropy, float3 hazyGlossMaxf0, inout BSDFData bsdfData)
{
    float w = 10.0; // interpolation steepness weight (Bezier weight of central point)
    bool useBezierToMapKh = true;

    float3 r_c = fresnel0;
    // We can use clamping of roughnessA here to avoid a "p == 0/0" case if roughnessA == 0.
    // Also note that if hazeExtent == 0, p will == 1, which will end up posing lobeMix = haziness.
    // Thus, we could also test (alpha_w_xy <= FLT_EPS) below (it will be such if hazeExtent == 0 or rouhnessA == 0)
    // to directly return lobeMix = haziness.
    //
    // But since hazeExtent is variable, this doesn't yield the same limit behaviour for a general hazeExtent though:
    // Clamping roughness will allow p to approach a non unit (p != 1) value as input roughness goes to 0 and
    // hazeExtent is non null, while using (alpha_w_xy <= FLT_EPS) makes us use the limit of p = 1 as soon as
    // alpha_w_xy is small enough.
    // The general limit of p = alpha_n/[alpha_n(1+lambda_h)] is 1/(1+lambda_h) for all paths where alpha_n > 0.
    // So both methods are arbitrary since whatever clamp value we use as eps in max(roughness, eps) for clamping
    // will determine at what value of "hazeExtent" there's a rapid change (high sensitivity) of p when roughness is
    // close to 0. (With the (alpha_w_xy <= FLT_EPS), there is also such a point).
    // We choose to streamline with just clamp, and arbitrarily with the value used when clamping for analytical lights.

    //float2 alpha_n = float2(roughnessAT, roughnessAB);
    float2 alpha_n = float2(ClampRoughnessForAnalyticalLights(roughnessAT), ClampRoughnessForAnalyticalLights(roughnessAB));
    // ...see above, we always use that clamp for numerical stability
    float alpha_n_xy = alpha_n.x * alpha_n.y;
    float beta_h = haziness;
    float2 lambda_h;
    ConvertValueAnisotropyToValueTB(hazeExtent, hazeExtentAnisotropy, lambda_h.x, lambda_h.y);
    //ConvertValueAnisotropyToValueTB(hazeExtent, 0.0, lambda_h.x, lambda_h.y);

    float2 alpha_w = saturate(alpha_n + lambda_h * sqrt(alpha_n_xy)); // wide lobe (haze) roughness
    float alpha_w_xy = alpha_w.x * alpha_w.y;
    // We saturate here as hazeExtent is scaled arbitrarily (Ideally, a max scale depends on the
    // maximum core roughness and since this primary roughness (of lobe A) can be textured, we
    // don't know it).
    float p = alpha_n_xy/alpha_w_xy; // peak ratio formula at theta_d = 0 (ie p is in the paper := P(0))

    float r_c_max = Max3(r_c.r, r_c.g, r_c.b);
    float k_h_max = 0.0;

    if (r_c_max <= FLT_EPS)
    {
        bsdfData.lobeMix = 0.0;
    }
    //else if (alpha_w_xy <= FLT_EPS) { bsdfData.lobeMix = beta_h; }
    else
    {
        if (useBezierToMapKh)
        {
            // Smooth out C1 discontinuity at k_h = p with a Bezier curve
            // (loose some hazeExtent in the process).

            float b = 2*(r_c_max*(1-w)+w*p);
            float u; // parametric coordinate for rational Bezier curve
            if (abs(2*(b-1)) <= FLT_EPS)
            {
                // use Taylor expansion around singularity
                u = (2*w*p-1- (4*Sq(w)*Sq(p)-4*Sq(w)*p+1)*(r_c_max-(0.5-w*p)/(1-w)) ) / (2*(w-1));
            }
            else
            {
                float D = Sq(b) - 4*(b-1)*r_c_max;
                u = (b-sqrt(D)) / (2*(b-1));
            }
            k_h_max =  (2*(1-u)*u*w*beta_h)/(Sq(1-u)+2*(1-u)*u*w+Sq(u)); // rational Bezier curve
        }
        else
        {
            // Interpolation between 0 and positivity and energy constraints: these are lines
            // but form a triangle so there's a discontinuity at k_h := K_h(0) = p, hence the
            // branch here:
            k_h_max = (r_c_max > p) ? beta_h*(1-r_c_max)/(1-p) : beta_h*r_c_max/p;
        }

        float r_max = r_c_max + (1-p)*k_h_max; // compound reflectivity (max color channel)
        float3 chromaVec = r_c/r_c_max;

        bsdfData.fresnel0 = r_max*chromaVec;
        bsdfData.fresnel0 = min(bsdfData.fresnel0, hazyGlossMaxf0);
        bsdfData.lobeMix = k_h_max / r_max;
        //bsdfData.lobeMix = 0.5;

        // For IBL, convert back to the scalar roughness + anisotropy parametrization for the
        // secondary lobe:
        float anisotropyB;
        float roughnessB;
        ConvertRoughnessToAnisotropy(alpha_w.x, alpha_w.y, anisotropyB);
        ConvertRoughnessTAndAnisotropyToRoughness(alpha_w.x, anisotropyB, roughnessB);

        bsdfData.anisotropyB = anisotropyB;
        bsdfData.perceptualRoughnessB = RoughnessToPerceptualRoughness(roughnessB);
        bsdfData.roughnessBT = alpha_w.x;
        bsdfData.roughnessBB = alpha_w.y;
    }
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

    // Blindly copy: will never be used if custom (ext) input is not used and selected as the SO method for baked/data-based specular occlusion.
    bsdfData.specularOcclusionCustomInput = surfaceData.specularOcclusionCustomInput;

    bsdfData.geomNormalWS = surfaceData.geomNormalWS; // We should always have this whether we enable coat normals or not.

    // Two lobe base material
    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.bentNormalWS = surfaceData.bentNormalWS;
    bsdfData.perceptualRoughnessA = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothnessA);
    bsdfData.perceptualRoughnessB = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothnessB);

    // We set metallic to 0 with SSS and specular color mode
    float metallic = HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_SPECULAR_COLOR | MATERIALFEATUREFLAGS_STACK_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION) ? 0.0 : surfaceData.metallic;

    bsdfData.diffuseColor = ComputeDiffuseColor(surfaceData.baseColor, metallic);

    bsdfData.diffusionProfileIndex = FindDiffusionProfileIndex(surfaceData.diffusionProfileHash);

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_SUBSURFACE_SCATTERING))
    {
        // Assign profile id and fresnel0
        FillMaterialSSS(bsdfData.diffusionProfileIndex, surfaceData.subsurfaceMask, bsdfData);
    }

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION))
    {
        // Assign profile id and fresnel0
        FillMaterialTransmission(bsdfData.diffusionProfileIndex, surfaceData.thickness, surfaceData.transmissionMask, bsdfData);
    }

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
    {
        FillMaterialAnisotropy(surfaceData.anisotropyA, surfaceData.anisotropyB, surfaceData.tangentWS, cross(surfaceData.normalWS, surfaceData.tangentWS), bsdfData);
    }

    // Overwrite fresnel0 if requested
    if (!surfaceData.useProfileIor)
    {
        if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_SPECULAR_COLOR))
            bsdfData.fresnel0 = surfaceData.specularColor;
        else
            bsdfData.fresnel0 = ComputeFresnel0(surfaceData.baseColor, metallic, IorToFresnel0(surfaceData.dielectricIor));
    }

    // Extract T & B anisotropies
    ConvertAnisotropyToRoughness(bsdfData.perceptualRoughnessA, bsdfData.anisotropyA, bsdfData.roughnessAT, bsdfData.roughnessAB);
    ConvertAnisotropyToRoughness(bsdfData.perceptualRoughnessB, bsdfData.anisotropyB, bsdfData.roughnessBT, bsdfData.roughnessBB);
    bsdfData.lobeMix = surfaceData.lobeMix;

    // Note that if we're using the hazy gloss parametrization, these will all be changed again:
    // fresnel0, lobeMix, perceptualRoughnessB, roughnessBT, roughnessBB.
    //
    // The fresnel0 is the only one used and needed in that case but it's ok, since materialFeatures are
    // statically known, the compiler will prune the useless computations for the rest of the terms above
    // when MATERIALFEATUREFLAGS_STACK_LIT_HAZY_GLOSS is set.
    //
    // It is important to deal with the hazy gloss parametrization after we have fresnel0 for the base but
    // before the effect of the coat is applied on it. When hazy gloss is used, the current fresnel0 at this
    // point is reinterpreted as a pseudo-f0 ("core lobe reflectivity" or Fc(0) or r_c in the paper)
    //
    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_HAZY_GLOSS))
    {
        // reminder: ComputeFresnel0 lerps from last param to first param using middle param as lerp factor.
        float3 hazyGlossMaxf0 = ComputeFresnel0(float3(1.0, 1.0, 1.0), surfaceData.metallic, surfaceData.hazyGlossMaxDielectricF0);
        HazeMapping(bsdfData.fresnel0, bsdfData.roughnessAT, bsdfData.roughnessAB, surfaceData.haziness, surfaceData.hazeExtent, bsdfData.anisotropyB, hazyGlossMaxf0, bsdfData);
    }

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_IRIDESCENCE))
    {
        FillMaterialIridescence(surfaceData.iridescenceMask, surfaceData.iridescenceThickness, surfaceData.iridescenceIor,
                                surfaceData.iridescenceCoatFixupTIR, surfaceData.iridescenceCoatFixupTIRClamp, bsdfData);
    }

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT))
    {
        FillMaterialCoatData(PerceptualSmoothnessToPerceptualRoughness(surfaceData.coatPerceptualSmoothness),
                             surfaceData.coatMask, surfaceData.coatIor, surfaceData.coatThickness, surfaceData.coatExtinction, bsdfData);

        if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT_NORMAL_MAP))
        {
            bsdfData.coatNormalWS = surfaceData.coatNormalWS;
        }

        // vlayering:
        // We can't calculate final roughnesses including anisotropy right away in this case: we will either do it
        // one time at GetPreLightData or for each light depending on the configuration for accuracy of BSDF
        // vlayering statistics (ie if VLAYERED_RECOMPUTE_PERLIGHT)

        // We have a coat top layer: change the base fresnel0 accordingdly:
        bsdfData.fresnel0 = ConvertF0ForAirInterfaceToF0ForNewTopIor(bsdfData.fresnel0, bsdfData.coatIor);

        // We dont clamp the roughnesses for now, ComputeAdding() will use those directly, unclamped.
        // (don't forget to call ClampRoughnessForDiracLightsByDefault after though)
        bsdfData.coatRoughness = PerceptualRoughnessToRoughness(bsdfData.coatPerceptualRoughness);
    }
    else
    {
        // roughnessT and roughnessB are clamped, and are meant to be used with punctual and directional lights.
        // perceptualRoughness is not clamped, and is meant to be used for IBL.
        bsdfData.roughnessAT = ClampRoughnessForDiracLightsByDefault(bsdfData.roughnessAT);
        bsdfData.roughnessAB = ClampRoughnessForDiracLightsByDefault(bsdfData.roughnessAB);
        bsdfData.roughnessBT = ClampRoughnessForDiracLightsByDefault(bsdfData.roughnessBT);
        bsdfData.roughnessBB = ClampRoughnessForDiracLightsByDefault(bsdfData.roughnessBB);
    }

    bsdfData.ambientOcclusion = surfaceData.ambientOcclusion;

    bsdfData.soFixupVisibilityRatioThreshold = surfaceData.soFixupVisibilityRatioThreshold;
    bsdfData.soFixupStrengthFactor = surfaceData.soFixupStrengthFactor;
    bsdfData.soFixupMaxAddedRoughness = surfaceData.soFixupMaxAddedRoughness;

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
        {
            float3 vsNormal = TransformWorldToViewDir(surfaceData.normalWS);
            result = IsNormalized(vsNormal) ?  vsNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
        case DEBUGVIEW_STACKLIT_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        {
            float3 vsGeomNormal = TransformWorldToViewDir(surfaceData.geomNormalWS);
            result = IsNormalized(vsGeomNormal) ?  vsGeomNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
        case DEBUGVIEW_STACKLIT_SURFACEDATA_SPECULAR_COLOR:
        {
            if (!HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_SPECULAR_COLOR))
            {
                // Derive the specular/fresnel0 term from the metallic parameter
                result = ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic.x, IorToFresnel0(surfaceData.dielectricIor));
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
        case DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_VIEW_SPACE:
            // Convert to view space
        {
            float3 vsNormal = TransformWorldToViewDir(bsdfData.normalWS);
            result = IsNormalized(vsNormal) ?  vsNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
        case DEBUGVIEW_STACKLIT_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        {
            float3 vsGeomNormal = TransformWorldToViewDir(bsdfData.geomNormalWS);
            result = IsNormalized(vsGeomNormal) ?  vsGeomNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    }
}

void GetPBRValidatorDebug(SurfaceData surfaceData, inout float3 result)
{
    result = surfaceData.baseColor;
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
    float bottomAngleFGD;                     // Only used when dual normal maps are enabled, set by ComputeAdding()
    float baseLayerNdotV;                     // Already had ClampNdotV() applied; for GetSpecularDominantDir (see EvaluateBSDF_Env)
                                              // (should be merged with the above, compiler should deal with that)
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
    float  unmodifiedIblPerceptualRoughness[BASE_NB_LOBES]; // see GetVLayeredBottomLobesPerceptualRoughnesses: this wont use more VGPR

    float  iblAnisotropy[BASE_NB_LOBES];      // bsdfData original anisotropies can change from stack computations
                                              // again, no register pressure added, bsdfData originals aren't live (used further)
                                              // once these are calculated, we just put them here to keep usage context clearer
                                              // (ie same semantic as the rest of PreLightData fields)

    float3 specularFGD[TOTAL_NB_LOBES];       // Store preintegrated BSDF for both specular and diffuse
    float  diffuseFGD;

    float lobeReflectionWeight[TOTAL_NB_LOBES]; // Extra light reflection hierarchy weights to avoid double coat lighting contribution with SSR-RTR

    // cf with Lit.hlsl: originally, it has a coat section with its own
    // iblR, iblF, PartLambdaV: this is due to the fact that it was the only
    // extra lobe, and simplified: we didn't want to pay the cost of an FGD fetch
    // for it (hence the name iblF in Lit). Here, we will fold all data into
    // lobe-indexed arrays.

    // For iridescence, to avoid recalculation per analytical light, we store the calculated
    // iridescence reflectance that was used (as an approximation to "iridescent pre-integrated" FGD)
    // to calculate FGD with the precalculated table:
    float3 fresnelIridforCalculatingFGD;

    // For clarity, we will dump the base layer lobes roughnesses used by analytical lights
    // here, to avoid confusion with the per-vlayer (vs per lobe) vLayerPerceptualRoughness
    // (also, those don't need to be anisotropic for all lobes but the non-separated
    // original roughnesses are still useful for all lobes because of the IBL hack)
    //
    // Again layered* are used by analytical dirac (non area, ie point and directional) lights
    // and are thus clamped.
    // We don't reuse the BSDFData roughnessAT/AB/BT/BB because we might need these original
    // values per light (ie not only once at GetPreLightData time) to recompute new roughnesses
    // if we use VLAYERED_RECOMPUTE_PERLIGHT.
    // This happens in the context of EvaluateBSDF_* calls though, and BSDFData is "input only"
    // in those, so not really needed, kept just for clarity, though this shouldn't affect compiler
    // register allocation.
    float  layeredRoughnessT[BASE_NB_LOBES];
    float  layeredRoughnessB[BASE_NB_LOBES];
    float  layeredCoatRoughness;
    // For consistency with these nonperceptual anisotropic and clamped roughnessAT/AB/BT/BB,
    // coatRoughness for analytical dirac lights will also be stored here;
    // These should always be:
    // preLightData.iblPerceptualRoughness[COAT_LOBE_IDX] = bsdfData.coatPerceptualRoughness;
    // preLightData.layeredCoatRoughness = ClampRoughnessForDiracLightsByDefault(bsdfData.coatRoughness);

    float minRoughness; // hack to pass per-light minRoughness to EvaluateBSDF when needed (GetHonorPerLightMinRoughness)
                        // (either that or TODO, modify EvaluateBSDF() callback signature to include lightData but
                        // the struct type changes depending on the type of light.)



    // GGX
    float partLambdaV[TOTAL_NB_LOBES];        // Depends on N, V, roughness
    // If we use VLAYERED_RECOMPUTE_PERLIGHT, we will recalculate those also.
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
    // TODOENERGY
    // For now since FGD fetches aren't used in compute adding (instead we do non integrated
    // Fresnel( ) evaluations and 1 - Fresnel( ) which is wrong, the former only ok for analytical
    // lights for the top interface for R12), we will use these for FGD fetches but keep them
    // for BSDF( ) eval for analytical lights since the later don't use FGD terms.


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
    float3 diffuseEnergy; // We don't fold into diffuseFGD because of analytical lights that require it separately.

    //
    // Area lights
    // TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.

    //#if !defined(AREA_LIGHTS_SUPPORT_ANISOTROPY) || !defined(_MATERIAL_FEATURE_ANISOTROPY)
    //float3x3 orthoBasisViewNormal[NB_NORMALS];       // Right-handed view-dependent orthogonal basis around the normal
    //#else
    // compiler should be ok to handle pruning if there's no AREA_LIGHTS_SUPPORT_ANISOTROPY or _MATERIAL_FEATURE_ANISOTROPY,
    // but we use defined sizes and indices like all other arrays here to be clean.
    // Use ORTHOBASIS_VN_*_IDX indices, they alias NB_NORMALS or TOTAL_NB_LOBES indices.
    float3x3 orthoBasisViewNormal[NB_ORTHOBASISVIEWNORMAL];   // for AREA_LIGHTS_SUPPORT_ANISOTROPY + _MATERIAL_FEATURE_ANISOTROPY
    float3x3 orthoBasisViewNormalDiffuse;            // for AREA_LIGHTS_SUPPORT_ANISOTROPY + _MATERIAL_FEATURE_ANISOTROPY,
                                                     // used by diffuse component (base and back lighted with _MATERIAL_FEATURE_TRANSMISSION)
    //#endif
    float3x3 ltcTransformDiffuse;                    // Inverse transformation for Lambertian or Disney Diffuse
    float3x3 ltcTransformSpecular[TOTAL_NB_LOBES];   // Inverse transformation for GGX

    // Occlusion data:
    float screenSpaceAmbientOcclusion;              // Keep a copy of the screen space occlusion texture fetch between
                                                    // PreLightData and PostEvaluateBSDF.
    float3 hemiSpecularOcclusion[TOTAL_NB_LOBES];   // Specular occlusion calculated from roughness and for an unknown
                                                    // (the less sparse / more uniform the better) light structure
                                                    // potentially covering the whole hemisphere.
};

//
// ClampRoughness helper specific to this material (PreLightData dependent)
//
//
// This is called for each directional and punctual light evaluation,
// in the context of callbacks from ShadeSurface_* calls.
//
// Only clamps if the shader is configured with the option to honor the per-light min roughness.
// Otherwise, the clamping is done earlier with a default ClampRoughnessForDiracLightsByDefault.
//
// Also, this is called before EvaluateBSDF, so if the option to recompute the stack per light is set,
// the ComputeAdding will be done at EvaluateBSDF time (once LdotH is known) and the modified values here would
// be overwritten, so we escape the clamps in that case too.
//
// -> There are additional options possible we could provide to control the interpretation of the per light
// minRoughness, in the case we recompute the stack per light:
//
// 1a) To clamp final roughnesses of the computed (top-of-stack equivalent) lobes, post computation (ie exactly as if
// we didn't recompute the stack per light wrt to the per light minRoughness setting).
// 1b) Clamp input roughnesses before the stack computations, so that the new top roughness also impacts the bottom.
//
// 2) For 1b), we also could interpret the minRoughness as clamping the coat only: since the bottom will get the
// impact of the clamp indirectly, this could suffice.
//
// As the light.minRoughness is a hack that can be used to simulate a sphere light from a point light, all options
// can be valid, it depends on what appearance the user wants.
//
// For 1), if the symmetric parameterization of the stack is desired and was the reason to have the recompute per
// light option, we currently don't offer to escape the influence of per light roughness applied to the coat in the
// stack recomputation.
//
// For 1b) and 2), in theory as soon as we touch base roughnesses interpreted as pre-stack computation clamps, this also
// means (if we want to respect the semantics of that clamp) the HazeMapping of lobe B (in the dual specular lobe case)
// should be redone, affecting bsdfData.fresnel0, .lobeMix, and all properties of lobe B. In that case, the HazeMapping
// dependencies should still stay the same (as computed from ConvertSurfaceDataToBSDFData) for non-dirac lights
// (IBL, LTC). In practice, the final fields used for those lights are preLightData.ibl*, the FGD terms and
// associated energyCompensationFactor, some of those are not touched in ComputeAdding. In any case, the per light stack
// recomputation happens in the context of EvaluateBSDF_* calls where preLightData and bsdfData are restored per light
// anyway.
//
// -> For now we ignore redoing HazeMapping and tie the "recompute stack option" to also mean to clamp with per light
// minRoughness pre-stack computation. We also clamp both the coat and the base inputs (see ComputeAdding) instead of
// just the coat.
//
void ClampRoughness(inout PreLightData preLightData, inout BSDFData bsdfData, float minRoughness)
{
    if (GetHonorPerLightMinRoughness())
    {
        preLightData.minRoughness = minRoughness;
    }

    // See option for 2) above: in that case, ComputeAdding() should just be called with a minRoughness == 0.
    if (GetPerLightMinRoughnessAffectsOnlyCoat())
    {
        if (GetHonorPerLightMinRoughness())
        {
            if (IsVLayeredEnabled(bsdfData))
            {
                if (!GetRecomputeStackPerLightOption())
                {
                    // If we don't recompute the stack per light, we clamp the final roughness used by
                    // dirac lights (preLightData.layered*: for the coat this is the same as bsdfData.coatRoughness)
                    preLightData.layeredCoatRoughness = max(minRoughness, preLightData.layeredCoatRoughness);
                }
                else
                {
                    // Otherwise, since the base roughness is amplified by the coat roughness, we clamp
                    // the roughness of the coat only, the base should get an effect too.
                    bsdfData.coatRoughness = max(minRoughness, bsdfData.coatRoughness);
                    // we don't update this, no need to: bsdfData.coatPerceptualRoughness = RoughnessToPerceptualRoughness(bsdfData.coatRoughness);
                }
            }

            if (!GetRecomputeStackPerLightOption())
            {
                preLightData.layeredRoughnessT[0] = max(minRoughness, preLightData.layeredRoughnessT[0]);
                preLightData.layeredRoughnessT[1] = max(minRoughness, preLightData.layeredRoughnessT[1]);
                preLightData.layeredRoughnessB[0] = max(minRoughness, preLightData.layeredRoughnessB[0]);
                preLightData.layeredRoughnessB[1] = max(minRoughness, preLightData.layeredRoughnessB[1]);
            }
        }
    }
    else
    {
        if (GetHonorPerLightMinRoughness() && !GetRecomputeStackPerLightOption())
        {
            if (IsVLayeredEnabled(bsdfData))
            {
                preLightData.layeredCoatRoughness = max(minRoughness, preLightData.layeredCoatRoughness);
            }

            preLightData.layeredRoughnessT[0] = max(minRoughness, preLightData.layeredRoughnessT[0]);
            preLightData.layeredRoughnessT[1] = max(minRoughness, preLightData.layeredRoughnessT[1]);
            preLightData.layeredRoughnessB[0] = max(minRoughness, preLightData.layeredRoughnessB[0]);
            preLightData.layeredRoughnessB[1] = max(minRoughness, preLightData.layeredRoughnessB[1]);
        }
    }
}

void GetVLayeredBottomLobesPerceptualRoughnesses(BSDFData bsdfData, PreLightData preLightData, out float basePerceptualRoughness[BASE_NB_LOBES])
{
    bool perLightOption = GetRecomputeStackPerLightOption();
    bool haveAnisotropy = HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY);

    if (perLightOption && haveAnisotropy)
    {
        basePerceptualRoughness[0] = preLightData.unmodifiedIblPerceptualRoughness[0];
        basePerceptualRoughness[1] = preLightData.unmodifiedIblPerceptualRoughness[1];
    }
    else
    {
        // ie !perLightOption || !haveAnisotropy so, if we haveAnisotropy, we don't have perLightOption and layeredRoughness* will be calculated,
        // otherwise, iblPerceptualRoughness[] is safe to use:
        basePerceptualRoughness[0] = haveAnisotropy ? ConvertRoughnessTAndBToRoughness(preLightData.layeredRoughnessT[0], preLightData.layeredRoughnessB[0]) : preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX];
        basePerceptualRoughness[1] = haveAnisotropy ? ConvertRoughnessTAndBToRoughness(preLightData.layeredRoughnessT[1], preLightData.layeredRoughnessB[1]) : preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX];
    }
}

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
#ifdef STACKLIT_DEBUG
    factor = _DebugAniso.w;
#endif
    //float newAniso = anisotropy * (r  + (1-r) * (1-newPerceptualRoughness)*(1-newPerceptualRoughness)
    //                                          * clamp(factor*roughness*roughness,0,1));
    //float newAniso = anisotropy * (r  + (1-r) * clamp(factor*roughness*roughness,0,1));
    float newAniso = anisotropy * r;
    return newAniso;
}

// Get the orthogonal component (or complement) of a vector V with regard to the vector N.
float3 GetOrthogonalComponent(float3 V, float3 N, bool testSingularity = false)
{
    // V and N are supposed to be unit vectors
    float VdotN = dot(V, N);
    float3 unitVOrtho;

    if (testSingularity && (abs(1.0 - VdotN) <= FLT_EPS))
    {
        // In this case N == V, and azimuth orientation around N shouldn't matter for the caller,
        // we can use any quaternion-based method, like Frisvad or Reynold's (Pixar):
        float3x3 orthoBasis = GetLocalFrame(N);
        unitVOrtho = orthoBasis[0]; // we pick any axis, compiler should optimize out calculation of [1]
    }
    else
    {
        float3 VOrtho = V - VdotN * N;
        // Instead of unitVOrtho = VOrtho * rsqrt(1.0 - Sq(VdotN)),
        // we will still clamp to avoid NaN and a warning if we don't care to testSingularity
        // But see CommonLighting.hlsl:GetOrthoBasisViewNormal(): this one doesn't seem to yield a warning
        unitVOrtho = VOrtho * rsqrt(max(1.0 - Sq(VdotN), FLT_EPS));
    }
    return unitVOrtho;
}

float3 GetDirFromAngleAndOrthoFrame(float3 V, float3 N, float newVdotN)
{
    float sintheta = sqrt(1.0 - Sq(newVdotN));
    float3 newV = newVdotN * N + sintheta * V;
    return newV;
}


void ComputeAdding_GetVOrthoGeomN(BSDFData bsdfData, float3 V, bool calledPerLight, out float3 vOrthoGeomN, out bool useGeomN, bool testSingularity = false)
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

        vOrthoGeomN = GetOrthogonalComponent(V, bsdfData.geomNormalWS, testSingularity);
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
//
// Interaction and performance considerations with the recompute per light option:
//
// If you recompute everything per light, FGD fetches per light might be expensive,
// so could use the FGD used for IBLs, angle used will be more or less incorrect
// depending on light directions, but probably better than using fresnel terms everywhere).
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
// There's a couple of choices for formulating the adding equations in ComputeAdding( ).
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
// energy transfer, "directional albedo" form. But in analytical lights, dirac vanishes
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
// doesn't change the lobe directions.
//
// Offspecular effects:
//
// Since the ComputeAdding() method doesn't really track mean directions but assume symmetry in
// the incidence plane (perpendicular to the "up" vector of the parametrization - either N or H
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
// Again, since we don't change w_i when instantiating an analytic BSDF, the change in roughness
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
// don't recompute for IBLs, but the coefficients for energy compensation would need to get FGD,
// and will require FGD fetches for each analytical light. (ie ComputeAdding( ) ideally should
// always do calculations with FGD and do the fetches, so that even in GetPreLightData, nothing
// would be done there). For now, and for performance reasons, we don't provide the option.
//
// However, when VLAYERED_RECOMPUTE_PERLIGHT is not used, we actually get usable terms that we
// will apply to the specular lighting, but these are different, we have one per real interface
// (so 2 vs the 3 "virtual" layer structure here).
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
void ComputeStatistics(in  float  cti, in float3 V, in float3 vOrthoGeomN, in bool useGeomN, in int i, in BSDFData bsdfData, in float minRoughness,
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
            // See p5 fig5 a) vs b) : using a refraction as a peak mean is the dotted line, while the ref is the solid line.
            // The scale is a hack to reproduce this effect:
            // As roughness -> 1, remove the effect of changing angle of entry.
            // Note that we never track complete means per se because of symmetry, we have no azimuth, so the "space" of the
            // means sin(theta) (here sti and stt) is just a line perpendicular to the normal in the plane of incidence.
            // Moreover, we don't consider offspecular effect as well as never outputting final downward lobes anyway, so we
            // never output the means but just track them as cosines of angles (cti) for energy transfer calculations
            // (should do with FGD but depends, see comments above).

            const float alpha = ClampRoughnessIfHonorLightMinRoughness(bsdfData.coatRoughness, minRoughness);
            const float scale = clamp((1.0-alpha)*(sqrt(1.0-alpha) + alpha), 0.0, 1.0);
            //http://www.wolframalpha.com/input/?i=f(alpha)+%3D+(1.0-alpha)*(sqrt(1.0-alpha)+%2B+alpha)+alpha+%3D+0+to+1
            stt = scale*stt + (1.0-scale)*sti;
            ctt = sqrt(1.0 - stt*stt);
        }
        else
        {
            // Even though we really track stats for a lobe, we decide on average we have TIR
            // (since we can't propagate anyway, we have no direction to use)
            // TODO: Right now, we block the UI from using coat IOR < 1.0, so can't happen.
            ctt = -1.0;
        }

        // Update variance
        // coatMask hack: See FillMaterialCoatData.
        // Here we just lerp linearized variance to 0 here.
        // Note that ctt -> cti when coatMask -> 0, so s_t12 is left as is. Same for jacobian terms
        s_r12 = RoughnessToLinearVariance(ClampRoughnessIfHonorLightMinRoughness(bsdfData.coatRoughness, minRoughness)) * bsdfData.coatMask;
        s_t12 = RoughnessToLinearVariance(ClampRoughnessIfHonorLightMinRoughness(bsdfData.coatRoughness, minRoughness) * 0.5 * abs((ctt*n12 - cti)/(ctt*n12)));
        j12   = (ctt/cti)*n12;

        s_r21 = s_r12;
        s_t21 = RoughnessToLinearVariance(ClampRoughnessIfHonorLightMinRoughness(bsdfData.coatRoughness, minRoughness) * 0.5 * abs((cti/n12 - ctt)/(cti/n12)));
        j21   = 1.0/j12;

    // Case of the media layer
    }
    else if(i == 1)
    {
        // TODO: if TIR is permitted by UI, do this, or early out
        //float stopFactor = float(cti > 0.0);
        //T12 = stopFactor * exp(- bsdfData.coatThickness * bsdfData.coatExtinction / cti);

        // coatMask hack: See FillMaterialCoatData. We already have modified coatThickness
        // and this will reduce Beer-Lambert attenuation here when the mask goes to 0

        // Update energy:
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

        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_IRIDESCENCE))
        {
            if (bsdfData.iridescenceMask > 0.0)
            {
                // float topIor = bsdfData.coatIor;
                //
                // Hack: (see also the last parameter of EvalIridescence() that we don't set)
                //
                // We will avoid using coatIor directly as with the refraction, it can cause TIR
                // which even when handled in EvalIridescence (tested), doesn't look pleasing and
                // creates a discontinuity.
                //
                // This fixup is configurable and applied via two terms:
                //
                // k1:
                //
                // First, note our average refracted lobe direction stat is modulated by the roughness:
                // The more roughness the coat has, the less we make our average lobe direction stat be
                // affected by refraction. Since this will make the incoming directions less compressed
                // wrt to the normal, TIR in iridescence due to the coatIor is more likely to happen.
                //
                // We noted the lerpfactor curve to make the iridescence effect cover the whole 0-90 degrees
                // angular range with 90-eps degrees just short of causing TIR vs the input coatPerceptualRoughness.
                // We fitted this curve with a cubic polynomial.
                // This gives us our lerpfactor to lerp down the coatIor closer to air.
                //
                // The first fixup is configured to keep TIR just out of the whole possible NdotV range,
                // but this is also dependent on iridescenceThickness. The first factor works for
                // iridescenceThickness = 1, but lowering iridescenceThickness will push the TIR border further
                // out toward the grazing angle.
                // A second fixup can be used to clamp the TIR border fixed by the first fixup factor,
                // regardless of iridescenceThickness.
                // Both are handy artistic options when wanting to animate or map (or anything else in a
                // shadergraph) coatIor, coatPerceptualSmoothness and iridescenceThickness.
                float k1 = 1.0;
                float k2 = 1.0;
                float scale = bsdfData.coatPerceptualRoughness;
                scale = RoughnessToPerceptualRoughness(ClampRoughnessIfHonorLightMinRoughness(bsdfData.coatRoughness, minRoughness));
                k1 = bsdfData.iridescenceCoatFixupTIR * saturate(0.003333915 - 0.1675957*scale + 2.571426*scale*scale - 1.406338*scale*scale*scale);
                k2 = lerp(1.0, (2.0 - bsdfData.iridescenceThickness), bsdfData.iridescenceCoatFixupTIRClamp); // See EvalIridescence in BSDF.hlsl for why "2-iridescenceThickness"
                float topIor = k2 * lerp(bsdfData.coatIor, 1.0001, k1);
                // Because of the coatMask, we also store independently the iridescence evaluation in preLightData for lights to be evaluated
                // with BSDF(), as normally without coat we do not use the NdotV-based terms (see R12 F_Schlick evaluation with ctiForFGD just
                // above) calculated in ComputeAdding for fresnel but the true LdotH. So for the coatMask hack, we need a separate iridescence
                // fresnel (stored here), combined with an F_Schlick(bsdfData.fresnel0, LdotH) term, the later evaluated in BSDF().
                //
                // We also need to store the iridescent fresnel term for the split-sum lights for the coatMask = 0 endpoint, since in
                // GetPreLightData, we assume that when we have the coat feature enabled, EvalIridescence is done here (avoid double computation).
                preLightData.fresnelIridforCalculatingFGD = EvalIridescence(topIor, ctiForFGD, bsdfData.iridescenceThickness, bsdfData.fresnel0);
                R12 = lerp(R12, preLightData.fresnelIridforCalculatingFGD, bsdfData.iridescenceMask);
            }
        }

        T12 = 0.0;
#ifdef VLAYERED_DIFFUSE_ENERGY_HACKED_TERM
        // Still should use FGD!
        // (note that although statistics T12 == T21 for the interface, the stack has terms e_T0i != e_Ti0)
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


void ComputeAdding(float _cti, float3 V, in BSDFData bsdfData, inout PreLightData preLightData,
                   bool calledPerLight = false, bool testSingularity = false, float minRoughness = 0.0)
{
    if (calledPerLight == false)
    {
        // Just a precaution
        minRoughness = 0.0;
        // ie We will only take it into account if called per light.
        // If GetHonorPerLightMinRoughness(), we will still escape the default clamp of ClampRoughnessForDiracLightsByDefault() though.
        // The net result if we're never recomputing the stack per light but signal we honor the per-light minRoughness is that we
        // won't clamp anything in ComputeAdding and just late clamp the resulting roughnesses at each light evaluation via ClampRoughness().
        // The change in coat roughness will obviously not affect the bottom roughness in that case and the results will be wrong, but
        // depending on the scene setup, could be acceptable.
    }
    // Like stated above, minRoughness is only useful if calledPerLight == true and GetHonorPerLightMinRoughness() == true:
    //
    // In that case, ClampRoughnessForDiracLightsByDefault() will not clamp anything but ClampRoughnessIfHonorLightMinRoughness()
    // used here and in ComputeStatistics will (provided calledPerLight == true and thus minRoughness is not 0) and we're passing
    // bsdfData's roughnesses through that function before using them in light evaluation (via ClampRoughness()),
    // so the net result will be a per light (ie light specific - see the call to ComputeAdding() in BSDF_SetupNormalsAndAngles)
    // minimum roughness value enforced on the input roughness values used before computing the stack statistics.

    // _cti should be LdotH or VdotH if calledPerLight == true (symmetric parametrization), V is unused in this case.
    // _cti should be NdotV if calledPerLight == false and no independent coat normal map is used (ie single normal map), V is unused in this case.
    // _cti should be (coatNormalWS dot V) if calledPerLight == false and we have a coat normal map. V is used in this case

#ifdef STACKLIT_DEBUG
    if( _DebugEnvLobeMask.w == 0.0)
    {
        preLightData.vLayerEnergyCoeff[TOP_VLAYER_IDX] = 0.0 * F_Schlick(IorToFresnel0(bsdfData.coatIor), _cti);
        preLightData.iblPerceptualRoughness[COAT_LOBE_IDX] = bsdfData.coatPerceptualRoughness;

        // This debug scope is used to bypass stack computing effects, so we use ClampRoughnessForDiracLightsByDefault()
        // to compute one time the default clamp if we dont honor the per light one, otherwise, ClampRoughness() will finalize the
        // per-light minRoughness clamping in the per light BSDF evaluations.
        preLightData.layeredCoatRoughness = ClampRoughnessForDiracLightsByDefault(bsdfData.coatRoughness);

        preLightData.vLayerEnergyCoeff[BOTTOM_VLAYER_IDX] = 1.0 * F_Schlick(bsdfData.fresnel0, _cti);

        preLightData.layeredRoughnessT[0] = ClampRoughnessForDiracLightsByDefault(bsdfData.roughnessAT);
        preLightData.layeredRoughnessB[0] = ClampRoughnessForDiracLightsByDefault(bsdfData.roughnessAB);
        preLightData.layeredRoughnessT[1] = ClampRoughnessForDiracLightsByDefault(bsdfData.roughnessBT);
        preLightData.layeredRoughnessB[1] = ClampRoughnessForDiracLightsByDefault(bsdfData.roughnessBB);
        preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX] = bsdfData.perceptualRoughnessA;
        preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX] = bsdfData.perceptualRoughnessB;
        return;
    }
#endif

    // Global Variables

    // Decide if we need the special path/hack for the coat normal map mode:
    bool useGeomN;
    float3 vOrthoGeomN; // only valid if useGeomN == true
    ComputeAdding_GetVOrthoGeomN(bsdfData, V, calledPerLight, vOrthoGeomN, useGeomN, testSingularity);
    if (useGeomN)
    {
        // We have dual normal maps and we are not called per light, so we aren't in the
        // symmetric model parametrization (with _cti = LdotH always positive).
        // Handle an option to fix the coat layer behavior to avoid regions where the top
        // normal map could completely block bottom shading.
        int method = ComputeAdding_GetDualNormalsTopHandlingMethod();

        if (method == VLAYERED_DUAL_NORMALS_TOP_FIX_GEOM_NORMAL)
        {
            _cti = ClampNdotV(preLightData.geomNdotV);
        }
        else if(method == VLAYERED_DUAL_NORMALS_TOP_FIX_FLIP_NORMAL)
        {
            // To still have the top normal influence the Fresnel term,
            // we could do
            // _cti = ClampNdotV(abs(dot(bsdfData.coatNormalWS, V)));
            // or use an even coarser clamp to remove more of the dark regions
            // around the lines where the normal flip,
            _cti = max(abs(dot(bsdfData.coatNormalWS, V)), 0.1);
            // and try not to go beyond the geometric normal:
            // This is important to avoid outer rim boost (on the base layer energy, and dimming for the coat,
            // as when cti -> 0 - which we prevent - , coat reflections get more energy but there's also less
            // transmission) from that large angular range (from the grazing limit of PI/2 up to acos(0.1))
            // due to use of a clamp with a large value of 0.1
            _cti = min(_cti, ClampNdotV(preLightData.geomNdotV));
        }
    }

    float  cti  = _cti;
    float3 R0i = float3(0.0, 0.0, 0.0), Ri0 = float3(0.0, 0.0, 0.0),
           T0i = float3(1.0, 1.0, 1.0), Ti0 = float3(1.0, 1.0, 1.0);
    float  s_r0i=0.0, s_ri0=0.0, s_t0i=0.0, s_ti0=0.0;
    float  j0i=1.0, ji0=1.0;

    float _s_r0m, s_r12, m_rr; // we will need these outside the loop for further calculations

    // HACK: If we don't use a local table and write the result directly in preLightData.vLayerEnergyCoeff
    // we get a warning 'array reference cannot be used as an l-value; not natively addressable, forcing loop to unroll'
    // Caution: be sure NB_VLAYERS == 3 and complete the code after
    float3 localvLayerEnergyCoeff[NB_VLAYERS];

    // Iterate over the layers
    UNITY_UNROLL
    for(int i = 0; i < NB_VLAYERS; ++i)
    {
        // Variables for the adding step
        float3 R12, T12, R21, T21;
        s_r12=0.0;
        float s_r21=0.0, s_t12=0.0, s_t21=0.0, j12=1.0, j21=1.0, ctt;

        // Layer specific evaluation of the transmittance, reflectance, variance
        ComputeStatistics(cti, V, vOrthoGeomN, useGeomN, i, bsdfData, minRoughness, preLightData, ctt, R12, T12, R21, T21, s_r12, s_t12, j12, s_r21, s_t21, j21);

        // Multiple scattering forms
        float3 denom = max(0.0, (float3(1.0, 1.0, 1.0) - Ri0*R12)); //i = new layer, 0 = cumulative top (llab3.1 to 3.4)

        // (mean(denom) <= 0.0f)? is not enough to prevent division by 0, see below
        //
        // float3 m_R0i = (mean(denom) <= 0.0f)? float3(0.0, 0.0, 0.0) : (T0i*R12*Ti0) / denom; //(llab3.1)
        // float3 m_Ri0 = (mean(denom) <= 0.0f)? float3(0.0, 0.0, 0.0) : (T21*Ri0*T12) / denom; //(llab3.2)
        // float3 m_Rr  = (mean(denom) <= 0.0f)? float3(0.0, 0.0, 0.0) : (Ri0*R12) / denom;
        float3 m_R0i = float3(0.0, 0.0, 0.0);
        float3 m_Ri0 = float3(0.0, 0.0, 0.0);
        float3 m_Rr  = float3(0.0, 0.0, 0.0);

        // Energy for transmission terms:
        float3 e_T0i = float3(0.0, 0.0, 0.0);
        float3 e_Ti0 = float3(0.0, 0.0, 0.0);

        // If the denominator of any component of the multiple scattering forms reaches 0, the series becomes meaningless
        // as bounces between the interfaces become "trapped" there in that limit, so we leave the terms to 0.
        UNITY_UNROLL
        for(int j = 0; j < 3; ++j)
        {
            if (denom[j] > 0.0)
            {
                m_R0i[j] = (T0i[j]*R12[j]*Ti0[j]) / denom[j]; //(llab3.1)
                m_Ri0[j] = (T21[j]*Ri0[j]*T12[j]) / denom[j]; //(llab3.2)
                m_Rr[j]  = (Ri0[j]*R12[j]) / denom[j];
                // Evaluate the adding operator on the energy for transmission terms:
                e_T0i[j] = (T0i[j]*T12[j]) / denom[j]; //(llab3.3)
                e_Ti0[j] = (T21[j]*Ti0[j]) / denom[j]; //(llab3.4)
            }
        }
        float  m_r0i = mean(m_R0i);
        float  m_ri0 = mean(m_Ri0);
        m_rr  = mean(m_Rr);

        // Evaluate the adding operator on the energy for reflection terms:
        float3 e_R0i = R0i + m_R0i; //(llab3.1)
        float3 e_Ri0 = R21 + m_Ri0; //(llab3.2)
        // Transmission energy is computed above already:
        // float3 e_T0i = (T0i*T12) / denom; //(llab3.3)
        // float3 e_Ti0 = (T21*Ti0) / denom; //(llab3.4)

        // Scalar forms for the energy
        float r21   = mean(R21);
        float r0i   = mean(R0i);
        float e_r0i = mean(e_R0i);
        float e_ri0 = mean(e_Ri0);

        // Evaluate the adding operator on the normalized variance
        _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
        float _s_r0i = (r0i*s_r0i + m_r0i*_s_r0m); // e_r0i; -> _s_r0i normalization is done below
        float _s_t0i = j12*s_t0i + s_t12 + j12*(s_r12 + s_ri0)*m_rr;
        float _s_rim = s_t12 + j12*(s_t21 + s_ri0 + m_rr*(s_r12+s_ri0));
        float _s_ri0 = (r21*s_r21 + m_ri0*_s_rim); // e_ri0; -> _s_ri0 normalization is done below
        float _s_ti0 = ji0*s_t21 + s_ti0 + ji0*(s_r12 + s_ri0)*m_rr;
        _s_r0i = (e_r0i > 0.0) ? _s_r0i/e_r0i : 0.0;
        _s_ri0 = (e_ri0 > 0.0) ? _s_ri0/e_ri0 : 0.0;

        // Store the coefficient and variance
        localvLayerEnergyCoeff[i] = (m_r0i > 0.0) ? m_R0i : float3(0.0, 0.0, 0.0);
        //preLightData.vLayerPerceptualRoughness[i] = (m_r0i > 0.0) ? LinearVarianceToPerceptualRoughness(_s_r0m) : 0.0;

        // Update energy
        R0i = e_R0i;
        T0i = e_T0i;
        Ri0 = e_Ri0;
        Ti0 = e_Ti0; // upward transmittance: we need this fully computed "past" the last layer see below for diffuse


        // Update mean
        // Avoid grazing angle black artefacts and instead of
        // cti = ctt;
        cti = ClampNdotV(ctt);

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

    // HACK: See note above why we need to do this
    preLightData.vLayerEnergyCoeff[0] = localvLayerEnergyCoeff[0];
    preLightData.vLayerEnergyCoeff[1] = localvLayerEnergyCoeff[1];
    preLightData.vLayerEnergyCoeff[2] = localvLayerEnergyCoeff[2];

    //-------------------------------------------------------------
    // Post compute:
    //-------------------------------------------------------------
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

    bool perLightOption = GetRecomputeStackPerLightOption();
    bool haveAnisotropy = HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY);

    // calledPerLight case:
    //
    // preLightData.ibl* fields are updated even if calledPerLight (ie for dirac lights) as they
    // can also be used to hold intermediate calculations for anisotropy used by dirac lights.
    // Normally we don't want these field updated per light, but we only call ComputeAdding
    // per light in the context of EvaluateBSDF_* calls, which passes preLightData not as "inout" but just
    // as input, so non-dirac lights will not be affected, neither should register pressure: the overwritten
    // values here shouldn't need to be held long before being restored as the new values are not used
    // beyond a few lines in this function.
    //
    // So to not further clutter the code here, we wont use extra locals copied to preLightData if (!calledPerLight).

    if( !calledPerLight )
    {
        // First, if we're not called per light, always (regardless of perLightOption) calculate
        // roughness for top lobe: no adding( ) modifications, just conversion + clamping for
        // analytical lights. For these we also don't need to recompute the following, but only the Fresnel
        // or FGD term are necessary in ComputeAdding, see BSDF().
        preLightData.iblPerceptualRoughness[COAT_LOBE_IDX] = bsdfData.coatPerceptualRoughness;
        preLightData.layeredCoatRoughness = ClampRoughnessForDiracLightsByDefault(bsdfData.coatRoughness);
    }
    else
    {
        preLightData.layeredCoatRoughness = ClampRoughnessIfHonorLightMinRoughness(bsdfData.coatRoughness, minRoughness);
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

    preLightData.iblAnisotropy[0] = bsdfData.anisotropyA;
    preLightData.iblAnisotropy[1] = bsdfData.anisotropyB;


    s_r12 = RoughnessToLinearVariance(ClampRoughnessIfHonorLightMinRoughness(PerceptualRoughnessToRoughness(bsdfData.perceptualRoughnessA), minRoughness));
    _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
    preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX] = LinearVarianceToPerceptualRoughness(_s_r0m);
    s_r12 = RoughnessToLinearVariance(ClampRoughnessIfHonorLightMinRoughness(PerceptualRoughnessToRoughness(bsdfData.perceptualRoughnessB), minRoughness));
    _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
    preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX] = LinearVarianceToPerceptualRoughness(_s_r0m);

#ifdef VLAYERED_ANISOTROPY_SCALAR_ROUGHNESS_CORRECTANISO
    // Try to correct stretching that happens when original roughness was low, but not too much
    // that destretching happens.
    IF_DEBUG( if( _DebugAniso.x == 1) )
    {
        // To be consistent with ClampRoughnessIfHonorLightMinRoughness() here, we should do:
        float originalPerceptualRoughness = RoughnessToPerceptualRoughness(ClampRoughnessIfHonorLightMinRoughness(PerceptualRoughnessToRoughness(bsdfData.perceptualRoughnessA), minRoughness));
        // compiler should optimize out all of this when not required (eg if GetHonorPerLightMinRoughness() == false) and fold subexpressions.

        preLightData.iblAnisotropy[0] = GetModifiedAnisotropy(bsdfData.anisotropyA, /*bsdfData.perceptualRoughnessA*/originalPerceptualRoughness,
                                                              PerceptualRoughnessToRoughness(/*bsdfData.perceptualRoughnessA*/originalPerceptualRoughness),
                                                              preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX]);

        originalPerceptualRoughness = RoughnessToPerceptualRoughness(ClampRoughnessIfHonorLightMinRoughness(PerceptualRoughnessToRoughness(bsdfData.perceptualRoughnessB), minRoughness));

        preLightData.iblAnisotropy[1] = GetModifiedAnisotropy(bsdfData.anisotropyB, /*bsdfData.perceptualRoughnessB*/originalPerceptualRoughness,
                                                              PerceptualRoughnessToRoughness(/*bsdfData.perceptualRoughnessB*/originalPerceptualRoughness),
                                                              preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX]);
    }
#endif

    if( !perLightOption || calledPerLight)
    {
        ConvertAnisotropyToRoughness(preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX], preLightData.iblAnisotropy[0],
                                     preLightData.layeredRoughnessT[0], preLightData.layeredRoughnessB[0]);
        ConvertAnisotropyToRoughness(preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX], preLightData.iblAnisotropy[1],
                                     preLightData.layeredRoughnessT[1], preLightData.layeredRoughnessB[1]);
        preLightData.layeredRoughnessT[0] = ClampRoughnessForDiracLightsByDefault(preLightData.layeredRoughnessT[0]);
        preLightData.layeredRoughnessB[0] = ClampRoughnessForDiracLightsByDefault(preLightData.layeredRoughnessB[0]);
        preLightData.layeredRoughnessT[1] = ClampRoughnessForDiracLightsByDefault(preLightData.layeredRoughnessT[1]);
        preLightData.layeredRoughnessB[1] = ClampRoughnessForDiracLightsByDefault(preLightData.layeredRoughnessB[1]);
    }
    else if (haveAnisotropy)
    {
        // For coat-traced SSR-RTR:
        //
        // If we're not calledPerLight AND we do have the option enabled, preLightData.layeredRoughness*[*] won't be filled
        // until the first shading call to dirac delta lights.
        // Since we're not calledPerLight, this is in the context of GetPreLightData and is our last chance to save the
        // stack-calculated base lobe roughnesses before they are modified by the anisotropic hack.
        //
        // Unfortunately, we'd like to have those scalar perceptualRoughnessA and B but unmodified by the IBL anisotropic hack:
        // this is for the EvaluateBSDF_ScreenSpaceReflection() hack where coat-traced SSR-RTR light might be re-used by
        // a bottom layer lobe (A or B) if the roughness is similar enough to that of the coat.
        //
        // If there's no perLightOption, we can use preLightData.layeredRoughness*[*], otherwise, we save the "still unmodified
        // by anisotropy" preLightData.iblPerceptualRoughness[*].
        // This is to help the compiler not use 2 more VGPRs.
        // See GetVLayeredBottomLobesPerceptualRoughnesses().
        preLightData.unmodifiedIblPerceptualRoughness[0] = preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX];
        preLightData.unmodifiedIblPerceptualRoughness[1] = preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX];
    }


#else
    // --------------------------------------------------------------------------------
    // Non scalar treatment of anisotropy to have the option to remove some anisotropy
    // --------------------------------------------------------------------------------
    if( !calledPerLight && !haveAnisotropy)
    {
        // We're not called per light, so don't do anything with ClampRoughnessIfHonorLightMinRoughness().

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
            preLightData.layeredRoughnessT[0] = ClampRoughnessForDiracLightsByDefault(LinearVarianceToRoughness(varianceLobeA));
            preLightData.layeredRoughnessT[1] = ClampRoughnessForDiracLightsByDefault(LinearVarianceToRoughness(varianceLobeB));
        }
    }

    if( !calledPerLight && haveAnisotropy)
    {
        // We're not called per light, so don't do anything with ClampRoughnessIfHonorLightMinRoughness().

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
        preLightData.iblAnisotropy[0] = bsdfData.anisotropyA;
#endif
        preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX] = RoughnessToPerceptualRoughness((roughnessT + roughnessB)/2.0);

        if( !perLightOption )
        {
            // We're not going to get called again per analytical light so store the result needed and used by them:
            // LOBEA T and B part:
            preLightData.layeredRoughnessT[0] = ClampRoughnessForDiracLightsByDefault(roughnessT);
            preLightData.layeredRoughnessB[0] = ClampRoughnessForDiracLightsByDefault(roughnessB);
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
        preLightData.iblAnisotropy[1] = bsdfData.anisotropyB;
#endif
        preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX] = RoughnessToPerceptualRoughness((roughnessT + roughnessB)/2.0);

        if( !perLightOption )
        {
            // We're not going to get called again per analytical light so store the result needed and used by them:
            // LOBEB T and B part:
            preLightData.layeredRoughnessT[1] = ClampRoughnessForDiracLightsByDefault(roughnessT);
            preLightData.layeredRoughnessB[1] = ClampRoughnessForDiracLightsByDefault(roughnessB);
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
        //s_r12 = RoughnessToLinearVariance(bsdfData.roughnessAT);
        s_r12 = RoughnessToLinearVariance(ClampRoughnessIfHonorLightMinRoughness(bsdfData.roughnessAT, minRoughness));
        _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
        preLightData.layeredRoughnessT[0] = ClampRoughnessForDiracLightsByDefault(LinearVarianceToRoughness(_s_r0m));

        // LOBEB roughness for analytical lights (T part)
        //s_r12 = RoughnessToLinearVariance(bsdfData.roughnessBT);
        s_r12 = RoughnessToLinearVariance(ClampRoughnessIfHonorLightMinRoughness(bsdfData.roughnessBT, minRoughness));
        _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
        preLightData.layeredRoughnessT[1] = ClampRoughnessForDiracLightsByDefault(LinearVarianceToRoughness(_s_r0m));

        if ( haveAnisotropy )
        {
            // LOBEA roughness for analytical lights (B part)
            //s_r12 = RoughnessToLinearVariance(bsdfData.roughnessAB);
            s_r12 = RoughnessToLinearVariance(ClampRoughnessIfHonorLightMinRoughness(bsdfData.roughnessAB, minRoughness));
            _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
            preLightData.layeredRoughnessB[0] = ClampRoughnessForDiracLightsByDefault(LinearVarianceToRoughness(_s_r0m));

            // LOBEB roughness for analytical lights (B part)
            //s_r12 = RoughnessToLinearVariance(bsdfData.roughnessBB);
            s_r12 = RoughnessToLinearVariance(ClampRoughnessIfHonorLightMinRoughness(bsdfData.roughnessBB, minRoughness));
            _s_r0m = s_ti0 + j0i*(s_t0i + s_r12 + m_rr*(s_r12+s_ri0));
            preLightData.layeredRoughnessB[1] = ClampRoughnessForDiracLightsByDefault(LinearVarianceToRoughness(_s_r0m));
        }
    }
    else if (perLightOption && haveAnisotropy)
    {
        // For coat-traced SSR-RTR:
        //
        // If we're not calledPerLight AND we do have the option enabled, preLightData.layeredRoughness*[*] won't be filled
        // until the first shading call to dirac delta lights.
        // Since we're not calledPerLight, this is in the context of GetPreLightData and is our last chance to save the
        // stack-calculated base lobe roughnesses before they are modified by the anisotropic hack.
        //
        // Unfortunately, we'd like to have those scalar perceptualRoughnessA and B but unmodified by the IBL anisotropic hack:
        // this is for the EvaluateBSDF_ScreenSpaceReflection() hack where coat-traced SSR-RTR light might be re-used by
        // a bottom layer lobe (A or B) if the roughness is similar enough to that of the coat.
        //
        // If there's no perLightOption, we can use preLightData.layeredRoughness*[*], otherwise, we save the "still unmodified
        // by anisotropy" preLightData.iblPerceptualRoughness[*].
        // This is to help the compiler not use 2 more VGPRs.
        // See GetVLayeredBottomLobesPerceptualRoughnesses().
        preLightData.unmodifiedIblPerceptualRoughness[0] = preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX];
        preLightData.unmodifiedIblPerceptualRoughness[1] = preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX];
    }
    // ...Non scalar treatment of anisotropy to have the option to remove some anisotropy
    // --------------------------------------------------------------------------------
#endif // #ifdef VLAYERED_ANISOTROPY_SCALAR_ROUGHNESS


#ifdef VLAYERED_DIFFUSE_ENERGY_HACKED_TERM
    // TODO
    // coatMask hack: See FillMaterialCoatData.
    preLightData.diffuseEnergy = lerp(float3(1.0, 1.0, 1.0), Ti0, bsdfData.coatMask);
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
        //TODO TOTEST
        //baseLayerNdotV = preLightData.bottomAngleFGD;
        baseLayerNdotV = sqrt(1 + Sq(preLightData.coatIeta)*(Sq(NdotV[0]) - 1));
        //TODO refactor with EvalIridescence, Lit::GetPreLightData
    }
    return ClampNdotV(baseLayerNdotV);
}

void PreLightData_SetupNormals(BSDFData bsdfData, inout PreLightData preLightData, float3 V, out float3 N[NB_NORMALS], out float NdotV[NB_NORMALS] /* clamped */)
{
    N[BASE_NORMAL_IDX] = bsdfData.normalWS;
    preLightData.NdotV[BASE_NORMAL_IDX] = dot(N[BASE_NORMAL_IDX], V);
    NdotV[BASE_NORMAL_IDX] = ClampNdotV(preLightData.NdotV[BASE_NORMAL_IDX]);

    if ( IsCoatNormalMapEnabled(bsdfData) )
    {
        N[COAT_NORMAL_IDX] = bsdfData.coatNormalWS;
        preLightData.NdotV[COAT_NORMAL_IDX] = dot(N[COAT_NORMAL_IDX], V);
        NdotV[COAT_NORMAL_IDX] = ClampNdotV(preLightData.NdotV[COAT_NORMAL_IDX]);

        preLightData.geomNdotV = dot(bsdfData.geomNormalWS, V);
    }
}

void PreLightData_LoadLtcTransformSpecular(float2 uv, int lobeIdx, inout PreLightData preLightData)
{
    // Get the inverse LTC matrix for GGX
    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcTransformSpecular[lobeIdx]      = 0.0;
    preLightData.ltcTransformSpecular[lobeIdx]._m22 = 1.0;
    preLightData.ltcTransformSpecular[lobeIdx]._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTCLIGHTINGMODEL_GGX, 0);
}

void PreLightData_SetupAreaLights(BSDFData bsdfData, float3 V, float3 N[NB_NORMALS], float NdotV[NB_NORMALS] /* clamped */, inout PreLightData preLightData)
{
    // For sampling the LUTs
    float cosTheta[NB_NORMALS];
    float2 uv[TOTAL_NB_LOBES];

    // These 2 cases will generate the same code when no dual normal maps since COAT_NORMAL_IDX == BASE_NORMAL_IDX == 0,
    // and one will be pruned out:
    cosTheta[COAT_NORMAL_IDX] = sqrt(1 - NdotV[COAT_NORMAL_IDX]);
    cosTheta[BASE_NORMAL_IDX] = sqrt(1 - NdotV[BASE_NORMAL_IDX]);

    // NB_NORMALS is always <= TOTAL_NB_LOBES, so we are safe to always be able to compile this even if preLightData.orthoBasisViewNormal[]
    // is sized with NB_ORTHOBASISVIEWNORMAL, and we use _NORMAL_IDX for clarity instead of ORTHOBASIS_VN_*_IDX aliases
    // (this function should never be called anyways when NB_ORTHOBASISVIEWNORMAL == TOTAL_NB_LOBES,
    // as PreLightData_SetupAreaLightsAniso is used in that case, but it must still compile)
    preLightData.orthoBasisViewNormal[COAT_NORMAL_IDX] = GetOrthoBasisViewNormal(V, N[COAT_NORMAL_IDX], preLightData.NdotV[COAT_NORMAL_IDX]);
    preLightData.orthoBasisViewNormal[BASE_NORMAL_IDX] = GetOrthoBasisViewNormal(V, N[BASE_NORMAL_IDX], preLightData.NdotV[BASE_NORMAL_IDX]);

    if( IsVLayeredEnabled(bsdfData) )
    {
        uv[COAT_LOBE_IDX] = LTC_LUT_OFFSET + LTC_LUT_SCALE * float2(bsdfData.coatPerceptualRoughness, cosTheta[COAT_NORMAL_IDX]);

        PreLightData_LoadLtcTransformSpecular(uv[COAT_LOBE_IDX], COAT_LOBE_IDX, preLightData);
    }

    uv[BASE_LOBEA_IDX] = LTC_LUT_OFFSET + LTC_LUT_SCALE * float2(bsdfData.perceptualRoughnessA, cosTheta[BASE_NORMAL_IDX]);
    uv[BASE_LOBEB_IDX] = LTC_LUT_OFFSET + LTC_LUT_SCALE * float2(bsdfData.perceptualRoughnessB, cosTheta[BASE_NORMAL_IDX]);
    PreLightData_LoadLtcTransformSpecular(uv[BASE_LOBEA_IDX], BASE_LOBEA_IDX, preLightData);
    PreLightData_LoadLtcTransformSpecular(uv[BASE_LOBEB_IDX], BASE_LOBEB_IDX, preLightData);


#ifdef USE_DIFFUSE_LAMBERT_BRDF
    preLightData.ltcTransformDiffuse = k_identity3x3;
#else
    // TODO
    // Get the inverse LTC matrix for Disney Diffuse
    //preLightData.ltcTransformDiffuse      = 0.0;
    //preLightData.ltcTransformDiffuse._m22 = 1.0;
    //preLightData.ltcTransformDiffuse._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTCLIGHTINGMODEL_DISNEY_DIFFUSE, 0);
#endif
} // PreLightData_SetupAreaLights

// cf with PreLightData_SetupAreaLights: we don't use NB_NORMALS but TOTAL_NB_LOBES in arrays because of the ibl aniso hack which
// makes a normal that is roughness dependent.
// Also uses
//     preLightData.iblPerceptualRoughness[] (vs bsdfData.(coat)perceptualRoughness(A|B))
void PreLightData_SetupAreaLightsAniso(BSDFData bsdfData, float3 V, float3 N[NB_NORMALS] /* for orthoBasisViewNormalDiffuse */, float3 iblN[TOTAL_NB_LOBES], inout PreLightData preLightData)
{
    // For sampling the LUTs
    float cosTheta[TOTAL_NB_LOBES];
    float2 uv[TOTAL_NB_LOBES];
    float iblNdotV[TOTAL_NB_LOBES];

    iblNdotV[COAT_LOBE_IDX] = dot(iblN[COAT_LOBE_IDX], V);
    iblNdotV[BASE_LOBEA_IDX] = dot(iblN[BASE_LOBEA_IDX], V);
    iblNdotV[BASE_LOBEB_IDX] = dot(iblN[BASE_LOBEB_IDX], V);

    // The 2 first cases will generate the same code when no coat since COAT_LOBE_IDX == BASE_LOBEA_IDX == 0,
    // and one will be pruned out:
    cosTheta[COAT_LOBE_IDX] =  sqrt(1-ClampNdotV(iblNdotV[COAT_LOBE_IDX]));
    cosTheta[BASE_LOBEA_IDX] =  sqrt(1-ClampNdotV(iblNdotV[BASE_LOBEA_IDX]));
    cosTheta[BASE_LOBEB_IDX] =  sqrt(1-ClampNdotV(iblNdotV[BASE_LOBEB_IDX]));

    // Now we need 3 matrices + 1 for transmission
    // Note we need to use ORTHOBASIS_VN_*_IDX since we could have no anisotropy and one or two normals but 3 lobes:
    // in that case, this function has to compile with preLightData.orthoBasisViewNormal[] (which will only have one or 2 slots)
    // but is never called
    preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_COAT_LOBE_IDX] = GetOrthoBasisViewNormal(V, iblN[COAT_LOBE_IDX], iblNdotV[COAT_LOBE_IDX]);
    preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_BASE_LOBEA_IDX] = GetOrthoBasisViewNormal(V, iblN[BASE_LOBEA_IDX], iblNdotV[BASE_LOBEA_IDX]);
    preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_BASE_LOBEB_IDX] = GetOrthoBasisViewNormal(V, iblN[BASE_LOBEB_IDX], iblNdotV[BASE_LOBEB_IDX]);
    preLightData.orthoBasisViewNormalDiffuse = GetOrthoBasisViewNormal(V, N[BASE_NORMAL_IDX], preLightData.NdotV[BASE_NORMAL_IDX]);

    if( IsVLayeredEnabled(bsdfData) )
    {
        uv[COAT_LOBE_IDX] = LTC_LUT_OFFSET + LTC_LUT_SCALE * float2(preLightData.iblPerceptualRoughness[COAT_LOBE_IDX], cosTheta[COAT_LOBE_IDX]);
        PreLightData_LoadLtcTransformSpecular(uv[COAT_LOBE_IDX], COAT_LOBE_IDX, preLightData);
    }

    uv[BASE_LOBEA_IDX] = LTC_LUT_OFFSET + LTC_LUT_SCALE * float2(preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX], cosTheta[BASE_LOBEA_IDX]);
    uv[BASE_LOBEB_IDX] = LTC_LUT_OFFSET + LTC_LUT_SCALE * float2(preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX], cosTheta[BASE_LOBEB_IDX]);
    PreLightData_LoadLtcTransformSpecular(uv[BASE_LOBEA_IDX], BASE_LOBEA_IDX, preLightData);
    PreLightData_LoadLtcTransformSpecular(uv[BASE_LOBEB_IDX], BASE_LOBEB_IDX, preLightData);


#ifdef USE_DIFFUSE_LAMBERT_BRDF
    preLightData.ltcTransformDiffuse = k_identity3x3;
#else
    // TODO
    // Get the inverse LTC matrix for Disney Diffuse
    //preLightData.ltcTransformDiffuse      = 0.0;
    //preLightData.ltcTransformDiffuse._m22 = 1.0;
    //preLightData.ltcTransformDiffuse._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTCLIGHTINGMODEL_DISNEY_DIFFUSE, 0);
#endif
} // PreLightData_SetupAreaLightsAniso

// See PreLightData_SetupOcclusion(): when we require something equivalent to a "diffuse color tint" but for the specular BSDF,
// we will use this:
float3 GetApproximatedBottomFresnel0ForTint(BSDFData bsdfData, PreLightData preLightData)
{
    float3 bottomF0;
    if( IsVLayeredEnabled(bsdfData) )
    {
        bottomF0 = preLightData.vLayerEnergyCoeff[BOTTOM_VLAYER_IDX];
    }
    else
    {
        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_IRIDESCENCE))
        {
            bottomF0 = preLightData.fresnelIridforCalculatingFGD;
        }
        else
        {
            bottomF0 = bsdfData.fresnel0;
        }
    }
    return bottomF0;
}

// This function return diffuse color or an equivalent color (in case of metal).
// This is use for MatCapView and reflection probe pass
float4 GetDiffuseOrDefaultColor(BSDFData bsdfData, float replace)
{
    // Use frensel0 as mettalic weight. all value below 0.2 (ior of diamond) are dielectric
    // all value above 0.45 are metal, in between we lerp.
    float weight = saturate((Max3(bsdfData.fresnel0.r, bsdfData.fresnel0.g, bsdfData.fresnel0.b) - 0.2) / (0.45 - 0.2));

    return float4(lerp(bsdfData.diffuseColor, bsdfData.fresnel0, weight * replace), weight);
}

// From SPTDistribution.hlsl:

//#define SPECULAR_OCCLUSION_FROM_AO 0
//#define SPECULAR_OCCLUSION_CONECONE 1
//#define SPECULAR_OCCLUSION_SPTD 2
//#define SPECULAR_OCCLUSION_CUSTOM_EXT_INPUT 3

float3 PreLightData_GetCommbinedSpecularOcclusion(float screenSpaceSpecularOcclusion, float specularOcclusionFromData, float3 fresnel0,
                                                  /* for debug: */
                                                  int dataBasedSpecularOcclusionAlgorithm)
{
    float3 debugCoeff = float3(1.0,1.0,1.0);
    switch(dataBasedSpecularOcclusionAlgorithm)
    {
    case SPECULAR_OCCLUSION_DISABLED:
        break;
    case SPECULAR_OCCLUSION_FROM_AO:
        //debugCoeff = float3(1.0,0.0,0.0);
        break;
    case SPECULAR_OCCLUSION_CUSTOM_EXT_INPUT:
        break;
    case SPECULAR_OCCLUSION_CONECONE:
        IF_DEBUG( if (_DebugSpecularOcclusion.w == -1.0) { debugCoeff = float3(1.0,0.0,0.0); } )
        break;
    case SPECULAR_OCCLUSION_SPTD:
        IF_DEBUG( if (_DebugSpecularOcclusion.w == -1.0) { debugCoeff = float3(0.0,1.0,0.0); } )
        break;
    }

    float3 specularOcclusion = debugCoeff * GTAOMultiBounce(min(specularOcclusionFromData, screenSpaceSpecularOcclusion), fresnel0);
    return specularOcclusion;
} // PreLightData_GetCommbinedSpecularOcclusion

float PreLightData_GetSpecularOcclusion(int specularOcclusionAlgorithm,
                                        float ambientOcclusion,
                                        float3 V, float3 normalWS, float NdotV /* clamped */,
                                        float perceptualRoughness,
                                        /* For advanced algorithms: */
                                        float3 bentNormalWS, int bentVisibilityAlgorithm,
                                        float3x3 orthoBasisViewNormal, bool useHemisphereClip,
                                        uint bentFixup = BENT_VISIBILITY_FIXUP_FLAGS_NONE,
                                        BSDFData bsdfData = (BSDFData)0) /* for reading info for bent cone fixup if needed or direct custom specular occlusion user input*/
{
    SphereCap hemiSphere = GetSphereCap(normalWS, 0.0);
    //test: SphereCap hemiSphere = GetSphereCap(normalWS, cos(HALF_PI*0.4));
    float roughness = PerceptualRoughnessToRoughness(perceptualRoughness);

    float specularOcclusion = 1.0;
    switch(specularOcclusionAlgorithm)
    {
    case SPECULAR_OCCLUSION_DISABLED:
        break;
    case SPECULAR_OCCLUSION_FROM_AO:
        specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(NdotV, ambientOcclusion, roughness);
        break;
    case SPECULAR_OCCLUSION_CONECONE:
        specularOcclusion = GetSpecularOcclusionFromBentAOConeCone(V, bentNormalWS, normalWS, ambientOcclusion, roughness, bentVisibilityAlgorithm,
                                                                   bentFixup,
                                                                   bsdfData.soFixupVisibilityRatioThreshold,
                                                                   bsdfData.soFixupStrengthFactor,
                                                                   bsdfData.soFixupMaxAddedRoughness,
                                                                   bsdfData.geomNormalWS);
        break;
    case SPECULAR_OCCLUSION_SPTD:
        specularOcclusion = GetSpecularOcclusionFromBentAOPivot(V, bentNormalWS, normalWS,
                                                                ambientOcclusion,
                                                                perceptualRoughness,
                                                                bentVisibilityAlgorithm,
                                                                /* useGivenBasis */ true,
                                                                orthoBasisViewNormal,
                                                                useHemisphereClip,
                                                                hemiSphere,
                                                                bentFixup,
                                                                bsdfData.soFixupVisibilityRatioThreshold,
                                                                bsdfData.soFixupStrengthFactor,
                                                                bsdfData.soFixupMaxAddedRoughness,
                                                                bsdfData.geomNormalWS);
        break;
    case SPECULAR_OCCLUSION_CUSTOM_EXT_INPUT:
        //specularOcclusion = 1.0;
        specularOcclusion = bsdfData.specularOcclusionCustomInput;
        break;
    }

    return specularOcclusion;
} // PreLightData_GetSpecularOcclusion

// Call after LTC so orthoBasisViewNormal[] are setup along with other preLightData fields:
//
// Makes use of ComputeAdding modified (if vlayered) iblPerceptualRoughness, vLayerEnergyCoeff
// and fresnelIridforCalculatingFGD (if not vlayered) if iridescence is enabled.
//
// TODOANISOTROPIC_SO: (cone isn't appropriate and SPTD doesn't support it either)
//
// Note: We make use of iblPerceptualRoughness[] even while depending at the point where we call _SetupOcclusion, we may have
// modified those in case of anisotropy (it is modified by vlayering if present and not clamped, and then by an empirical
// formula in case anisotropy is present too specifically for the IBL anisotropic hack).
// Since specular occlusion is not anisotropic anyways, we use these as-is, instead of a version
// "pre-GetGGXAnisotropicModifiedNormalAndRoughness".
//
// Uses:
//     preLightData.orthoBasisViewNormal[]
//     preLightData.iblPerceptualRoughness[]
//     preLightData.vLayerEnergyCoeff[]
//     preLightData.fresnelIridforCalculatingFGD
//
// Sets:
//     preLightData.screenSpaceAmbientOcclusion
//     preLightData.hemiSpecularOcclusion[]
//
void PreLightData_SetupOcclusion(PositionInputs posInput, BSDFData bsdfData, float3 V, float3 N[NB_NORMALS], float NdotV[NB_NORMALS] /* clamped */, float3x3 orthoBasisViewNormal[NB_NORMALS],
                                 inout PreLightData preLightData)
{
    float screenSpaceSpecularOcclusion[TOTAL_NB_LOBES];
    float dataBasedSpecularOcclusion[TOTAL_NB_LOBES];
    float3 bottomF0;

    preLightData.screenSpaceAmbientOcclusion = GetScreenSpaceDiffuseOcclusion(posInput.positionSS);

#ifdef _ENABLESPECULAROCCLUSION

    // -We have 3 lobes with different roughnesses, and these have been placed unclamped and modified by vlayering in
    // iblPerceptualRoughness[].
    // -We might have 2 different shading normals to consider.
    // -Bentnormal is always considered if the algorithm permits it, but it might trivially be the normal if no bent
    // normals were given by the user.
    //
    // -Finally, our pre-calculated specular occlusion will serve for IBL for now, which have unknown structure so the
    // whole hemisphere around the normal is taken as potential light visibility region, that's why the pre-calculated
    // values are identified as "hemiSpecularOcclusion". This would potentially need to be different per light type,
    // or even per light:
    // eg. for eventual LTC spherical area lights, we can have perfect 3 terms (geometric factor, BSDF, incoming radiance)
    // integration with pivot-transformed spherical cap integration domain, our SPTD GGX proxy for BSDF and LTC GGX proxy
    // analytic sphere irradiance.

    // For (baked) data based specular occlusion:
    int specularOcclusionAlgorithm = GetDataBasedSpecularOcclusionMethod();
    int bentVisibilityAlgorithm = GetDataBasedSpecularOcclusionVisibilityFromAoWeight();
    float3 bentVisibilityDir;

    bool useHemisphereClip = true;
    uint bentFixup = GetSpecularOcclusionBentVisibilityFixupFlags();

    // For SSAO based specular occlusion:
    int screenSpaceSpecularOcclusionAlgorithm = GetScreenSpaceSpecularOcclusionMethod();
    int screenSpaceBentVisibilityAlgorithm = GetScreenSpaceSpecularOcclusionVisibilityFromAoWeight();

    IF_DEBUG(specularOcclusionAlgorithm = clamp((int)(_DebugSpecularOcclusion.x), 0, SPECULAR_OCCLUSION_SPTD));
    IF_DEBUG(bentVisibilityAlgorithm = clamp((int)(_DebugSpecularOcclusion.y), 0, BENT_VISIBILITY_FROM_AO_COS_BENT_CORRECTION));
    IF_DEBUG(useHemisphereClip = (_DebugSpecularOcclusion.z == 1.0) );

    // For fresnel0 to use with GTAOMultiBounce for the bottom interface, since it's already a hack on an empirical fit
    // (empirical fit done for diffuse), we will try to use something we already calculated and should offer a proper tint:
    bottomF0 = GetApproximatedBottomFresnel0ForTint(bsdfData, preLightData);

    // We calculate the screen space AO-derived SO and then the one based on data (baked) values.
    // There is one such pair of SO value per lobe. Screen space values will be combined with min( , ) with data-based calculated SO.
    // Unlike Lit, for screen space AO based SO, we offer the more advanced algorithms too.

    // A caveat for SSAO-based SO methods that would use a bent visibility algorithm is that the SSAO algorithm doesn't calculate the
    // required direction, so we offer the choice to use a non dynamically calculated one:
    // (vertex interpolated, baked bent normal map, or shading normal)
    bentVisibilityDir = GetScreenSpaceSpecularOcclusionVisibilityDir(bsdfData, N[BASE_NORMAL_IDX]);
    screenSpaceSpecularOcclusion[BASE_LOBEA_IDX] = PreLightData_GetSpecularOcclusion(screenSpaceSpecularOcclusionAlgorithm,
                                                                                     preLightData.screenSpaceAmbientOcclusion,
                                                                                     V, N[BASE_NORMAL_IDX], NdotV[BASE_NORMAL_IDX] /* clamped */,
                                                                                     preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX],
                                                                                     bentVisibilityDir, screenSpaceBentVisibilityAlgorithm,
                                                                                     orthoBasisViewNormal[BASE_NORMAL_IDX], useHemisphereClip);

    screenSpaceSpecularOcclusion[BASE_LOBEB_IDX] = PreLightData_GetSpecularOcclusion(screenSpaceSpecularOcclusionAlgorithm,
                                                                                     preLightData.screenSpaceAmbientOcclusion,
                                                                                     V, N[BASE_NORMAL_IDX], NdotV[BASE_NORMAL_IDX] /* clamped */,
                                                                                     preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX],
                                                                                     bentVisibilityDir, screenSpaceBentVisibilityAlgorithm,
                                                                                     orthoBasisViewNormal[BASE_NORMAL_IDX], useHemisphereClip);


    // Get specular occlusion from data (baked) values temporarily in preLightData.hemiSpecularOcclusion[]
    // and combine those data-based SO values with the screen-space SO values into one final hemispherical specular occlusion:
    //
    // (TODO: Note that for now, for specularOcclusionAlgorithm == SPECULAR_OCCLUSION_CUSTOM_EXT_INPUT,
    // we only have one value that overrides and set each SO value of each lobe to the same unique user supplied value.)
    dataBasedSpecularOcclusion[BASE_LOBEA_IDX] = PreLightData_GetSpecularOcclusion(specularOcclusionAlgorithm,
                                                                                   bsdfData.ambientOcclusion,
                                                                                   V, N[BASE_NORMAL_IDX], NdotV[BASE_NORMAL_IDX] /* clamped */,
                                                                                   preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX],
                                                                                   bsdfData.bentNormalWS, bentVisibilityAlgorithm,
                                                                                   orthoBasisViewNormal[BASE_NORMAL_IDX], useHemisphereClip,
                                                                                   bentFixup, bsdfData);

    dataBasedSpecularOcclusion[BASE_LOBEB_IDX] = PreLightData_GetSpecularOcclusion(specularOcclusionAlgorithm,
                                                                                   bsdfData.ambientOcclusion,
                                                                                   V, N[BASE_NORMAL_IDX], NdotV[BASE_NORMAL_IDX] /* clamped */,
                                                                                   preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX],
                                                                                   bsdfData.bentNormalWS, bentVisibilityAlgorithm,
                                                                                   orthoBasisViewNormal[BASE_NORMAL_IDX], useHemisphereClip,
                                                                                   bentFixup, bsdfData);

    preLightData.hemiSpecularOcclusion[BASE_LOBEA_IDX] = PreLightData_GetCommbinedSpecularOcclusion(screenSpaceSpecularOcclusion[BASE_LOBEA_IDX],
                                                                                                    dataBasedSpecularOcclusion[BASE_LOBEA_IDX],
                                                                                                    bottomF0, specularOcclusionAlgorithm);

    preLightData.hemiSpecularOcclusion[BASE_LOBEB_IDX] = PreLightData_GetCommbinedSpecularOcclusion(screenSpaceSpecularOcclusion[BASE_LOBEB_IDX],
                                                                                                    dataBasedSpecularOcclusion[BASE_LOBEB_IDX],
                                                                                                    bottomF0, specularOcclusionAlgorithm);

    if( IsVLayeredEnabled(bsdfData) )
    {
        bentVisibilityDir = GetScreenSpaceSpecularOcclusionVisibilityDir(bsdfData, N[COAT_NORMAL_IDX]);
        screenSpaceSpecularOcclusion[COAT_LOBE_IDX] = PreLightData_GetSpecularOcclusion(screenSpaceSpecularOcclusionAlgorithm,
                                                                                        preLightData.screenSpaceAmbientOcclusion,
                                                                                        V, N[COAT_NORMAL_IDX], NdotV[COAT_NORMAL_IDX] /* clamped */,
                                                                                        preLightData.iblPerceptualRoughness[COAT_LOBE_IDX],
                                                                                        bentVisibilityDir, screenSpaceBentVisibilityAlgorithm,
                                                                                        orthoBasisViewNormal[COAT_NORMAL_IDX], useHemisphereClip);

        // (TODO: Note that for now, for specularOcclusionAlgorithm == SPECULAR_OCCLUSION_CUSTOM_EXT_INPUT,
        // we only have one value that overrides and set each SO value of each lobe to the same unique user supplied value.)
        dataBasedSpecularOcclusion[COAT_LOBE_IDX] = PreLightData_GetSpecularOcclusion(specularOcclusionAlgorithm,
                                                                                      bsdfData.ambientOcclusion,
                                                                                      V, N[COAT_NORMAL_IDX], NdotV[COAT_NORMAL_IDX] /* clamped */,
                                                                                      preLightData.iblPerceptualRoughness[COAT_LOBE_IDX],
                                                                                      bsdfData.bentNormalWS, bentVisibilityAlgorithm,
                                                                                      orthoBasisViewNormal[COAT_NORMAL_IDX], useHemisphereClip,
                                                                                      bentFixup, bsdfData);

        preLightData.hemiSpecularOcclusion[COAT_LOBE_IDX] = PreLightData_GetCommbinedSpecularOcclusion(screenSpaceSpecularOcclusion[COAT_LOBE_IDX],
                                                                                                       dataBasedSpecularOcclusion[COAT_LOBE_IDX],
                                                                                                       IorToFresnel0(bsdfData.coatIor), specularOcclusionAlgorithm);
    }
#else
    preLightData.hemiSpecularOcclusion[COAT_LOBE_IDX] =
    preLightData.hemiSpecularOcclusion[BASE_LOBEA_IDX] =
    preLightData.hemiSpecularOcclusion[BASE_LOBEB_IDX] = float3(1.0, 1.0, 1.0);

#endif // _ENABLESPECULAROCCLUSION

} // PreLightData_SetupOcclusion

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

    // Set depending on vlayering:
    float3 f0forCalculatingFGD;

    if( IsVLayeredEnabled(bsdfData) )
    {
        // VLAYERING:

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
        //   preLightData.diffuseEnergy             (just one term, computed ifdef VLAYERED_DIFFUSE_ENERGY_HACKED_TERM)
        //   preLightData.iblAnisotropy[]           (only if anisotropy is enabled)
        //   preLightData.layeredCoatRoughness

        // If we're not using VLAYERED_RECOMPUTE_PERLIGHT we also have calculated
        //   preLightData.layeredRoughnessT and B[],
        // Otherwise, the calculation of these is done for each light
        //

        // Handle FGD texture fetches for IBL + area light + multiscattering.
        // Note: use the unmodified by anisotropy iblPerceptualRoughness here.
        //
        // Here, we will fetch our actual FGD terms, see ComputeAdding for details: the F0 params
        // will be replaced by our energy coefficients. Note that the way to do it depends on the
        // formulation of ComputeAdding (with FGD fetches or only Fresnel terms).
        //
        // Also note that while the fetch directions for the light samples (IBL) are the ones
        // at the top interface, for the FGD terms (in fact, for all angle dependent BSDF
        // parametrization data), we need to use the actual interface angle a propagated direction
        // would have. So, for the base layer, this is a refracted direction through the coat.
        // Same for the top, but this is just NdotV.
        // This is because we should really have fetched FGD with the tracked cti (cos theta incoming)
        // at the bottom layer or top layer during ComputeAdding itself. We delayed the fetch after,
        // because our ComputeAdding formulation is with "energy" coefficients calculated with a
        // chain of Fresnel terms instead of a correct chain computed with the true FGD.

        preLightData.baseLayerNdotV = PreLightData_GetBaseNdotVForFGD(bsdfData, preLightData, NdotV);


        float diffuseFGDTmp; // unused, for coat layer FGD fetch

        // We will do the coat specific FGD fetch here:
        // (FGD fetches used for IBL + area light + multiscattering)
        GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV[COAT_NORMAL_IDX],
            preLightData.iblPerceptualRoughness[COAT_LOBE_IDX],
            preLightData.vLayerEnergyCoeff[TOP_VLAYER_IDX],
            preLightData.specularFGD[COAT_LOBE_IDX],
            diffuseFGDTmp,
            specularReflectivity[COAT_LOBE_IDX]);

        // We apply the coatMask here since even an f0 of 0 in the fetch above will give a
        // directional albedo (aka specular reflectivity) that is non zero:
        preLightData.specularFGD[COAT_LOBE_IDX] *= bsdfData.coatMask;
        // This is for the base FGD fetches factored out of "if vlayering or not":
        f0forCalculatingFGD = preLightData.vLayerEnergyCoeff[BOTTOM_VLAYER_IDX];
        // We need to lerp with coatMask here too since with "deferred" (out of ComputeAdding)
        // pre-integrated FGD fetches, we will have a F_Schlick term in the energy coefficient
        // and not the interface fresnel0 directly. Also, we first lerp the coatMask = 0 endpoint
        // to include iridescence:
        f0forCalculatingFGD = lerp(lerp(bsdfData.fresnel0, preLightData.fresnelIridforCalculatingFGD, bsdfData.iridescenceMask),
                                   f0forCalculatingFGD,
                                   bsdfData.coatMask);


    } // ...if( IsVLayeredEnabled(bsdfData) )
    else
    {
        // NO VLAYERING:
        //
        // Only a single normal map possible here, NdotV[] and N[] are sized to 1
        //
        // We will fill these:
        //
        //   preLightData.iblPerceptualRoughness[]
        //   preLightData.iblAnisotropy[]
        //
        // and also these (even without vlayering) to factor code:
        //
        //   preLightData.layeredRoughnessT and B[],

        // See ConvertSurfaceDataToBSDFData : The later are already clamped if
        // vlayering is disabled, so could be used directly, but for later
        // refactoring.
        // (TODOTODO: instead of BSDFdata A and B values, we should really
        // permit array definitions in the shader include code generation attributes)
        //
        // No coat here, so no preLightData.layeredCoatRoughness;
        preLightData.layeredRoughnessT[0] = bsdfData.roughnessAT;
        preLightData.layeredRoughnessB[0] = bsdfData.roughnessAB;
        preLightData.layeredRoughnessT[1] = bsdfData.roughnessBT;
        preLightData.layeredRoughnessB[1] = bsdfData.roughnessBB;

        // Also important: iblPerceptualRoughness[] is used for specular occlusion
        preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX] = bsdfData.perceptualRoughnessA;
        preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX] = bsdfData.perceptualRoughnessB;
        preLightData.iblAnisotropy[0] = bsdfData.anisotropyA;
        preLightData.iblAnisotropy[1] = bsdfData.anisotropyB;

        // This will be used for the factored out base layer FGD fetches
        preLightData.baseLayerNdotV = NdotV[0];
        f0forCalculatingFGD = bsdfData.fresnel0;

        // We also consider iridescence here (when vlayering, it is handled in ComputeAdding):
        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_IRIDESCENCE))
        {
            float topIor = 1.0; // Air on top, no coat.
            if (bsdfData.iridescenceMask > 0.0)
            {
                preLightData.fresnelIridforCalculatingFGD = EvalIridescence(topIor, NdotV[0], bsdfData.iridescenceThickness, bsdfData.fresnel0);
                f0forCalculatingFGD = lerp(f0forCalculatingFGD, preLightData.fresnelIridforCalculatingFGD, bsdfData.iridescenceMask);
            }
        }
    }


    // Handle base FGD texture fetches for IBL + area light + multiscattering:

    GetPreIntegratedFGDGGXAndDisneyDiffuse(preLightData.baseLayerNdotV,
        preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX],
        f0forCalculatingFGD,
        preLightData.specularFGD[BASE_LOBEA_IDX],
        diffuseFGD[0],
        specularReflectivity[BASE_LOBEA_IDX]);

    GetPreIntegratedFGDGGXAndDisneyDiffuse(preLightData.baseLayerNdotV,
        preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX],
        f0forCalculatingFGD,
        preLightData.specularFGD[BASE_LOBEB_IDX],
        diffuseFGD[1],
        specularReflectivity[BASE_LOBEB_IDX]);


    // Handle area lights and specular occlusion setup if no AREA_LIGHTS_SUPPORT_ANISOTROPY

    if (AREA_LIGHTS_ANISOTROPY_ENABLED == false)
    {
        // If area lights don't support anisotropy, we can setup area lights here and occlusion after, as the former
        // don't need the anisotropic modified normal and roughness (IBL anisotropy hack) and the later can use
        // the area lights preLightData.orthoBasisViewNormal:
        PreLightData_SetupAreaLights(bsdfData, V, N, NdotV, preLightData);

        // The following is only executed when preLightData.orthoBasisViewNormal[] is sized NB_NORMALS,
        // in that case, ORTHOBASIS_VN_BASE_LOBEA_IDX == ORTHOBASIS_VN_BASE_LOBEB_IDX == BASE_NORMAL_IDX,
        // following doesnt do anything but permit compilation without #ifdef.
        float3x3 orthoBasisViewNormal[NB_NORMALS];
        orthoBasisViewNormal[COAT_NORMAL_IDX] = preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_COAT_LOBE_IDX];
        orthoBasisViewNormal[BASE_NORMAL_IDX] = preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_BASE_LOBEA_IDX];
        PreLightData_SetupOcclusion(posInput, bsdfData, V, N, NdotV /* array, clamped */, orthoBasisViewNormal, preLightData /* inout */);
    }

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
    {
        // Note: there's no anisotropy possible on coat.
        float TdotV = dot(bsdfData.tangentWS,   V);
        float BdotV = dot(bsdfData.bitangentWS, V);

#ifndef VLAYERED_RECOMPUTE_PERLIGHT // Note that this is undef when no vlayering

        // We can precalculate lambdaVs for all lights here since we're not doing ComputeAdding per light
        // (idem if no vlayering obviously)

        // An enclosing "if( IsVLayeredEnabled(bsdfData) )"
        // isn't really needed, as if no vlayering, COAT_LOBE_IDX will be == to one of the BASE_LOBE?_IDX
        // and the following line will be pruned out by the compiler:
        preLightData.partLambdaV[COAT_LOBE_IDX] = GetSmithJointGGXPartLambdaV(NdotV[COAT_NORMAL_IDX], preLightData.layeredCoatRoughness);

        preLightData.partLambdaV[BASE_LOBEA_IDX] = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV[BASE_NORMAL_IDX],
                                                                                    preLightData.layeredRoughnessT[0], preLightData.layeredRoughnessB[0]);
        preLightData.partLambdaV[BASE_LOBEB_IDX] = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV[BASE_NORMAL_IDX],
                                                                                    preLightData.layeredRoughnessT[1], preLightData.layeredRoughnessB[1]);
#else
        if( IsVLayeredEnabled(bsdfData) )
        {
            // vlayering + VLAYERED_RECOMPUTE_PERLIGHT:
            // Store those for eval analytical lights since we're going to recalculate lambdaV after each ComputeAdding
            // for each light and will need those:
            preLightData.TdotV = TdotV;
            preLightData.BdotV = BdotV;
        }
#endif

        // Handle Specular Occlusion before the IBL anisotropic hack modifies iblPerceptualRoughness
        // Also handle AREA_LIGHTS_SUPPORT_ANISOTROPY: in that case, the orthoBasisViewNormal[] is different between SO and area lights (TODOANISOTROPIC_SO?)
        if (AREA_LIGHTS_ANISOTROPY_ENABLED)
        {
            // Use a frame that only consider possibly dual normals, but no lobe specific hack to the normal:
            float3x3 orthoBasisViewNormal[NB_NORMALS];
            orthoBasisViewNormal[COAT_NORMAL_IDX] = GetOrthoBasisViewNormal(V, N[COAT_NORMAL_IDX], preLightData.NdotV[COAT_NORMAL_IDX]);
            orthoBasisViewNormal[BASE_NORMAL_IDX] = GetOrthoBasisViewNormal(V, N[BASE_NORMAL_IDX], preLightData.NdotV[BASE_NORMAL_IDX]);
            PreLightData_SetupOcclusion(posInput, bsdfData, V, N, NdotV /* array, clamped */, orthoBasisViewNormal, preLightData /* inout */);
        }

        // No anisotropy for coat:
        // (if no vlayering, iblN[COAT_LOBE_IDX] will be overwritten below since COAT_LOBE_IDX will be == to one of the BASE_LOBE?_IDX)
        iblN[COAT_LOBE_IDX] = N[COAT_NORMAL_IDX];

        // Handle iblPerceptualRoughness modifications for anisotropy.
        // iblPerceptualRoughness is used as input and output here:
        GetGGXAnisotropicModifiedNormalAndRoughness(bsdfData.bitangentWS,
            bsdfData.tangentWS,
            N[BASE_NORMAL_IDX],
            V,
            preLightData.iblAnisotropy[0],
            preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX],
            iblN[BASE_LOBEA_IDX],
            preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX]);

        GetGGXAnisotropicModifiedNormalAndRoughness(bsdfData.bitangentWS,
            bsdfData.tangentWS,
            N[BASE_NORMAL_IDX],
            V,
            preLightData.iblAnisotropy[1],
            preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX],
            iblN[BASE_LOBEB_IDX],
            preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX]);

        // AREA_LIGHTS_SUPPORT_ANISOTROPY
        if (AREA_LIGHTS_ANISOTROPY_ENABLED)
        {
            // N is passed for the extra orthoBasisViewNormalDiffuse:
            PreLightData_SetupAreaLightsAniso(bsdfData, V, N, iblN, preLightData);
        }
    }
    else // no anisotropy:
    {

#ifndef VLAYERED_RECOMPUTE_PERLIGHT
        // We can precalculate lambdaVs for all lights here since we're not doing ComputeAdding per light
        // If no vlayering, VLAYERED_RECOMPUTE_PERLIGHT will be undef, and the COAT_LOBE_IDX line will be pruned (COAT_LOBE_IDX == BASE_LOBE?_IDX)
        preLightData.partLambdaV[COAT_LOBE_IDX] = GetSmithJointGGXPartLambdaV(NdotV[COAT_NORMAL_IDX], preLightData.layeredCoatRoughness);
        preLightData.partLambdaV[BASE_LOBEA_IDX] = GetSmithJointGGXPartLambdaV(NdotV[BASE_NORMAL_IDX], preLightData.layeredRoughnessT[0]);
        preLightData.partLambdaV[BASE_LOBEB_IDX] = GetSmithJointGGXPartLambdaV(NdotV[BASE_NORMAL_IDX], preLightData.layeredRoughnessT[1]);
#endif

        iblN[COAT_LOBE_IDX] = N[COAT_NORMAL_IDX]; // if no vlayering, iblN[COAT_LOBE_IDX] will be overwritten:
        iblN[BASE_LOBEA_IDX] = iblN[BASE_LOBEB_IDX] = N[BASE_NORMAL_IDX];
    } // no anisotropy


    // Handle IBL fetch direction R + GGX multiscattering energy loss compensation term
    for(int i = 0; i < TOTAL_NB_LOBES; i++)
    {
        preLightData.iblR[i] = reflect(-V, iblN[i]);
    }

#ifdef STACK_LIT_USE_GGX_ENERGY_COMPENSATION
    // TODOENERGY:
    // This is actually changing FGD to FGDinf in the vlayering framework but this needs to be folded in the energy calculations
    // during ComputeAdding. Also even analytical lights will need the energy terms, not just IBL.
    // (See also CalculateEnergyCompensationFromSpecularReflectivity and ApplyEnergyCompensationToSpecularLighting)

    // Wrong to apply these here vs in ComputeAdding as compensation means replacing FGD with FGDinf but we have a chain of
    // them built by ComputeAdding with terms like (FGDinf), (1-FGDinf). Also, the compensation approximation depends on f0.
    // See TODOENERGY [eg:  (a1*ef)(1-(a2*ef)) != ef (a1)(1-a2) ]

    // coat line will be pruned if no vlayering, (COAT_LOBE_IDX == BASE_LOBE?_IDX),
    // but important to place it before the others:
    preLightData.energyCompensationFactor[COAT_LOBE_IDX] = GetEnergyCompensationFactor(specularReflectivity[COAT_LOBE_IDX], IorToFresnel0(bsdfData.coatIor));
    preLightData.energyCompensationFactor[BASE_LOBEA_IDX] = GetEnergyCompensationFactor(specularReflectivity[BASE_LOBEA_IDX], bsdfData.fresnel0);
    preLightData.energyCompensationFactor[BASE_LOBEB_IDX] = GetEnergyCompensationFactor(specularReflectivity[BASE_LOBEB_IDX], bsdfData.fresnel0);
#else
    preLightData.energyCompensationFactor[BASE_LOBEA_IDX] =
    preLightData.energyCompensationFactor[BASE_LOBEB_IDX] =
    preLightData.energyCompensationFactor[COAT_LOBE_IDX] = 1.0;
#endif

    // Apply  * (1-bsdfData.lobeMix) and * (bsdfData.lobeMix) to the FGD themselves
    // so we don't need to mix elsewhere (makes sense also in the context of what is FGD)
    // (todo check we dont mix again elsewhere)
    preLightData.specularFGD[BASE_LOBEA_IDX] *= (1-bsdfData.lobeMix);
    preLightData.specularFGD[BASE_LOBEB_IDX] *= (bsdfData.lobeMix);

    preLightData.diffuseFGD = lerp(diffuseFGD[0], diffuseFGD[1], bsdfData.lobeMix);

#ifdef USE_DIFFUSE_LAMBERT_BRDF
    preLightData.diffuseFGD = 1.0;
#endif

    return preLightData;
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

#ifndef DISABLE_MODIFY_BAKED_DIFFUSE_LIGHTING
// This define allow to say that we implement a ModifyBakedDiffuseLighting function to be called in PostInitBuiltinData
#define MODIFY_BAKED_DIFFUSE_LIGHTING
#endif

void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, inout BuiltinData builtinData)
{
    // Add GI transmission contribution to bakeDiffuseLighting, we then drop backBakeDiffuseLighting (i.e it is not used anymore, this saves VGPR)
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION))
    {
        builtinData.bakeDiffuseLighting += builtinData.backBakeDiffuseLighting * bsdfData.transmittance;
    }

    // For SSS we need to take into account the state of diffuseColor
    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_SUBSURFACE_SCATTERING))
    {
        bsdfData.diffuseColor = GetModifiedDiffuseColorForSSS(bsdfData);
    }

    // Premultiply baked (possibly with back facing added) diffuse lighting information with diffuse
    // pre-integration and energy term.
    // preLightData.diffuseEnergy will be 1,1,1 if no vlayering or no VLAYERED_DIFFUSE_ENERGY_HACKED_TERM
    // Note: When baking reflection probes, we approximate the diffuse with the fresnel0
    builtinData.bakeDiffuseLighting *= preLightData.diffuseFGD * preLightData.diffuseEnergy * GetDiffuseOrDefaultColor(bsdfData, _ReplaceDiffuseForIndirect).rgb;

    // The lobe specific specular occlusion data, along with the result of the screen space occlusion sampling
    // will be computed in PreLightData.
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------

// Try to infer a metallic value from a specularColor parameterization,
// in the context of adding reflectance from f0 for metals for the meta pass:
// There's a limit to inferring a valid value. In particular, the diffuseColor
// close to 0.0 is a hard guess, especially if f0 is close to dielectricF0.
// The algorithm tries to avoid returning extremes in those cases.
//
// Assumes surfaceData.dielectricIor is valid, and not overriden, ie we're not using a diffusionProfile
// with transmission or SSS which fix metallic to 0 and sets bsdfData.fresnel0 to IorToFresnel0(diffusionProfile.IOR)
//
float GetInferredMetallic(float dielectricF0, float3 inDiffuseColor, float3 inFresnel0)
{
    float fresnel0 = Max3(inFresnel0.r, inFresnel0.g, inFresnel0.b);
    float diffuseColor = Max3(inDiffuseColor.r, inDiffuseColor.g, inDiffuseColor.b);
    float metallic = 0.0;

    if (dielectricF0 <= 0.0001)
    {
        // The baseColor + metallic parameterization gives (note that this is used to build
        // a possible conversion, but the given fresnel0, diffuseColor and dielectricF0 might not
        // be possible with a baseColor + metallic parameterization):
        //
        //  (A) fresnel0 = metallic * basecolor  +  (1.0 - metallic) * dielectricF0;
        //  (B) diffuseColor = baseColor * (1.0 - metallic);
        //
        // if dielectricF0 = 0, we have:
        //
        //  (1) fresnel0 = metallic * basecolor;
        //  (2) diffuseColor = (1.0 - metallic) * baseColor;
        //
        // if metallic == 0, fresnel0 must be == 0 and we can just output it (ie metallic = fresnel0 = 0.0);
        // else
        // metallic != 0 and
        // if baseColor != 0 (otherwise, if either is 0, fresnel0 == 0, and even if metallic was set to anything else than 0,
        //                   with dielectricF0 == 0, this doesn't make sense as a metal, so we will still infer metallic = 0
        //                   and again could output directly fresnel0 as our metallic),
        //
        // thus fresnel0 != 0 and we substitute safely 1 in 2:
        //
        //  fresnel0 / metallic = basecolor;
        //  diffuseColor = (1.0 - metallic) * fresnel0 / metallic;
        //
        //  metallic = 1/(diffuseColor/fresnel0 + 1);
        //  metallic = fresnel0/(diffuseColor + fresnel0);
        //
        // So we will use that formula when dielectricF0 == 0 since it outputs plausible values:
        //
        //   -When fresnel0 is 0, it will always output a desired (for the reasons discussed above) value of metallic = 0.
        //   -When input values are possible for (A) and (B), the formula is correct (for when dielectricF0 == 0 of course).
        //   -When input values are not possible with base/metal, it will output a sensible value.

        metallic = ((fresnel0 + diffuseColor) > 0.0) ? fresnel0 / (fresnel0 + diffuseColor) : 0.0;
    }
    else
    {
        float2 roots;
        // We will try to find positive roots, but again, the input parameters might not be possible
        // to replicate in a baseColor / metallic parameterization, so we must be careful to precondition
        // our values. Even when conditioned, the inputs might have no positive solutions.
        // Also, to mitigate extremes for the diffuseColor close to 0 case, we will average our
        // positive roots.
        diffuseColor = max(diffuseColor, 0.0001);
        dielectricF0 = min(dielectricF0, Sq(fresnel0 + diffuseColor) / 4*diffuseColor);

        float sqrtTerm = sqrt(Sq(fresnel0 + diffuseColor) - 4*diffuseColor*dielectricF0);
        roots.x = -( sqrtTerm - 2*dielectricF0 + fresnel0 + diffuseColor) / (2*dielectricF0);
        roots.y = -(-sqrtTerm - 2*dielectricF0 + fresnel0 + diffuseColor) / (2*dielectricF0);

        float2 rootsWeights = float2(roots >= 0);
        //float rootsTotalWeight = max(1.0, dot(rootsWeights, float2(1.0, 1.0)));
        float rootsTotalWeight = 1.0 + float(all(roots >= 0));
        float average = dot(roots, rootsWeights) / rootsTotalWeight;

        metallic = average;
    }

    return metallic;
}

LightTransportData GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LightTransportData lightTransportData;

    // diffuseColor for lightmapping should basically be diffuse color.
    // But rough metals (black diffuse) still scatter quite a lot of light around, so
    // we want to take some of that into account too.
    //
    // In Lit we do:
    //
    //float roughness = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
    //lightTransportData.diffuseColor = bsdfData.diffuseColor + bsdfData.fresnel0 * roughness * 0.5 * surfaceData.metallic;

    //
    // In StackLit we also need to deal with vlayering and the tint it might give from the diffuse energy factor,
    // so we end up with two directionally dependent hacks: the diffuseEnergy factor, and the rough metal bounce boost
    // (which can also be directionally dependent because of the modification to f0 with iridescence, see below)
    //
    // fraginputs are also ShaderPass/StackLitSharePass.hlsl, we should be able to compute the diffuse energy factor,
    // but we have to run GetPreLightData (but it's ok, meta pass is not performance critical, just baking).
    //
    // Another problem is the input parametrization: if we have specularColor, metallic is meaningless.
    // We can try to infer it.
    //
    // For using roughness like in the Lit hack commented above, if vlayered, we could mix the equivalent lobe roughnesses of
    // the base layer.
    // The fresnel0 shouldn't be bsdfData.fresnel0 in the vlayered case also, but preLightData.vLayerEnergyCoeff[BOTTOM_VLAYER_IDX].
    //
    // Also, when vlayered, our computations are directional (diffuseEnergy is a hack), and account for iridescence but obviously
    // it is not an effect that should normally impact diffuse baking (but neither should specular reflectance).
    // When not vlayered we can still handle iridescence to be consistent for the rough metal hack (see f0forCalculatingFGD).
    //
    // Finally, diffuseEnergy should multiply diffuseColor.

    float3 V = bsdfData.normalWS;
    // We have V in ShaderPassLightTransport.hlsl, but it isn't setup (the meta pass is just a simple baked-map uv unwrap to screen)

    float metallic = surfaceData.metallic;
    float roughness = PerceptualRoughnessToRoughness(lerp(bsdfData.perceptualRoughnessA, bsdfData.perceptualRoughnessB, surfaceData.lobeMix));
    float3 f0forCalculatingFGD;
    float3 diffuseEnergy = float3(1.0, 1.0, 1.0);

    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION))
    {
        metallic = 0.0;
    }
    else if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_SPECULAR_COLOR))
    {
        metallic = GetInferredMetallic(IorToFresnel0(surfaceData.dielectricIor), bsdfData.diffuseColor, bsdfData.fresnel0);
    }

    if (IsVLayeredEnabled(bsdfData))
    {
        PreLightData preLightData;
        ZERO_INITIALIZE(PreLightData, preLightData);

        float3 N[NB_NORMALS];
        float NdotV[NB_NORMALS];
        PreLightData_SetupNormals(bsdfData, preLightData, V, N, NdotV);

        // Here N == V everywhere so make sure dual normal maps will work using testSingularity:
        ComputeAdding(NdotV[COAT_NORMAL_IDX], V, bsdfData, preLightData, false /*calledPerLight*/, true /*testSingularity*/);

        roughness = PerceptualRoughnessToRoughness(lerp(preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX], preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX], surfaceData.lobeMix));
        f0forCalculatingFGD = preLightData.vLayerEnergyCoeff[BOTTOM_VLAYER_IDX];
        diffuseEnergy = preLightData.diffuseEnergy;
    }
    else
    {
        float NdotV = ClampNdotV(dot(bsdfData.normalWS, V));
        f0forCalculatingFGD = bsdfData.fresnel0;
        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_IRIDESCENCE))
        {
            float topIor = 1.0; // Air on top, no coat.
            if (bsdfData.iridescenceMask > 0.0)
            {
                float3 fresnelIridforCalculatingFGD = EvalIridescence(topIor, NdotV, bsdfData.iridescenceThickness, bsdfData.fresnel0);
                f0forCalculatingFGD = lerp(f0forCalculatingFGD, fresnelIridforCalculatingFGD, bsdfData.iridescenceMask);
            }
        }
    }

    lightTransportData.diffuseColor = diffuseEnergy * bsdfData.diffuseColor + f0forCalculatingFGD * roughness * 0.5 * metallic;
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

    if ( IsCoatNormalMapEnabled(bsdfData) )
    {
        N[COAT_NORMAL_IDX] = bsdfData.coatNormalWS;
    }

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
    float invLenLV = rsqrt(max(2.0 * LdotV + 2.0, FLT_EPS)); // invLenLV = rcp(length(L + V)), clamp to avoid rsqrt(0) = inf, inf * 0 = NaN
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

        // Must call ComputeAdding and update partLambdaV
        float lightMinRoughness = GetPerLightMinRoughnessAffectsOnlyCoat() ? 0.0 /* see ClampRoughness(): in that case already applied to bsdfData.coatRoughness*/
                                                                             : preLightData.minRoughness;

        ComputeAdding(savedLdotH, V[TOP_DIR_IDX], bsdfData, preLightData, true, /* testSingularity */ false, lightMinRoughness);
        // Notice the top LdotH as interface angle, symmetric model parametrization (see paper sec. 6 and comments
        // on ComputeAdding)
        // layered*Roughness* and vLayerEnergyCoeff are now updated for the proper light direction.

        // !Updates to PartLambdaV are now needed, will be done later when we consider anisotropy.

        // Note (see p9 eq(39)): if we don't recompute per light, we just reuse the IBL energy terms as the fresnel
        // terms for our LdotH, too bad (similar to what we do with iridescence), along with the "wrongly" calculated
        // energy.
        // TODOENERGY:
        // In any case, we should have used FGD terms (except for R12 at the start of the process) for the analytical
        // light case, see comments at the top of ComputeAdding
#endif //...VLAYERED_RECOMPUTE_PERLIGHT


#ifdef VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE

        // Also, we will use the base normal map automatically if we have dual normal maps (coat normals)
        // since we use generically N[BASE_NORMAL_IDX]

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

        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
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
        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
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

void BSDF_ModifyFresnelForIridescence(BSDFData bsdfData, PreLightData preLightData, float LdotH, inout float3 F)
{
        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_IRIDESCENCE))
        {
            float3 fresnelIridescent = preLightData.fresnelIridforCalculatingFGD;

#ifdef IRIDESCENCE_RECOMPUTE_PERLIGHT
            float topIor = 1.0; // default air on top.
            fresnelIridescent = EvalIridescence(topIor, LdotH, bsdfData.iridescenceThickness, bsdfData.fresnel0);
#endif
            F = lerp(F, fresnelIridescent, bsdfData.iridescenceMask);
        }
}

void GetNLForDirectionalPunctualLights(BSDFData bsdfData, PreLightData preLightData, float3 L, float3 V,
                                       out float3 mainN, out float3 mainL, out float mainNdotL,
                                       out bool lightCanOnlyBeTransmitted)
{
    // To avoid loosing the coat specular component due to the bottom normal modulation from the bottom normal map,
    // all the while keeping the code as much the same as with Lit and compatible with the rest of lightloop and
    // lightevaluation
    // (wrt to bias handling, shadow mask, shadow map, using only one light evaluation call and one attenuation,
    // and hidding the handling of transmission with PreEvaluate*LightTransmission() shadow index/mask / NdotL
    // overrides and N flipping trick),
    // we will only allow transmission when both NdotL[] are < 0.
    // For all other cases, the BSDF is evaluated for each lobe using the proper normal, but it also applies the
    // NdotL projection factor of the direct lighting integral so as to not have to return each layer lighting
    // components.
    //

    // Note: we should not use refracted directions provided by BSDF_SetupNormalsAndAngles as we only consider
    // incoming light here, not "equivalent-lobe" stack BSDF effects (eg see GetPreLightData)

    // For the N, L and NdotL used for the light evaluation, we will use the set of values for which N is closest
    // to the original light orientation (ie receives the most projected energy).

    float3 N[NB_NORMALS];
    N[BASE_NORMAL_IDX] = bsdfData.normalWS;
    N[COAT_NORMAL_IDX] = bsdfData.normalWS;
    if ( IsCoatNormalMapEnabled(bsdfData) )
    {
        N[COAT_NORMAL_IDX] = bsdfData.coatNormalWS;
    }
    float3 skewedL[NB_NORMALS]; // L might change according to the normal due to the sun disk hack
    float NdotL[NB_NORMALS];
    // ...cf with BSDF_SetupNormalsAndAngles: we dont use NDOTLV_SIZE and NB_LV_DIR as we don't consider the possibility
    // of using refracted directions for the bottom layer lobes: we are strictly evaluating incoming L now, see comments
    // where we use refract elsewhere in this file.
    skewedL[BASE_NORMAL_IDX] = L;
    skewedL[COAT_NORMAL_IDX] = L;

    NdotL[BASE_NORMAL_IDX] = dot(N[BASE_NORMAL_IDX], skewedL[BASE_NORMAL_IDX]);
    NdotL[COAT_NORMAL_IDX] = dot(N[COAT_NORMAL_IDX], skewedL[COAT_NORMAL_IDX]);

    // For transmission, we're going to use the bottom layer N and NdotL always.
    // For the rest, we will use the N which produces the biggest NdotL, as we don't want
    // to early out eg from the bottom layer when the top should have a highlight,
    // while the final BSDF evaluation will take care of applying the proper NdotL in any
    // case.
    // We could increase the cost and complexity of all this and actually
    // commit fully to making all these diract-light evaluations local to this file and
    // pass to BSDF the L[], V[], etc. arrays instead of hacking our way around just here.

    float maxNdotL = max(NdotL[COAT_NORMAL_IDX], NdotL[BASE_NORMAL_IDX]);

    lightCanOnlyBeTransmitted = maxNdotL < 0; // In case we have NdotL[base] < 0, NdotL[top] > 0, we won't have transmission, too bad.

    mainNdotL = maxNdotL;
    bool baseIsBrighter = NdotL[BASE_NORMAL_IDX] >= NdotL[COAT_NORMAL_IDX];
    if (lightCanOnlyBeTransmitted)
    {
        // In that case, we always use the base normal:
        baseIsBrighter = true;
        mainNdotL = NdotL[BASE_NORMAL_IDX];
    }

    mainL = baseIsBrighter ? skewedL[BASE_NORMAL_IDX] : skewedL[COAT_NORMAL_IDX];
    mainN = baseIsBrighter ? N[BASE_NORMAL_IDX] : N[COAT_NORMAL_IDX];
}

bool IsNonZeroBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    // Get N, L and NdotL parameters along with if transmission must be evaluated
    bool unused;
    float3 mainN, mainL;
    float mainNdotL;
    GetNLForDirectionalPunctualLights(bsdfData, preLightData, L, V, mainN, mainL, mainNdotL, unused);

    return HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION) || (mainNdotL > 0.0);
}


// This function is the core BSDF evaluation for analytical dirac-rays light (point and directional).
// (Reminder, lights are:
//  -point/directional: fully analytical integral[LFGD] which evaluates directly because of the dirac L.
//  -environments: split-sum, pre-integrated LD split from pre-integrated FGD
//  -LTC area lights: split-sum, analytical L, pre-integrated FGD, like environment
//  -SSR)
//
// Assumes that NdotL is positive.
CBSDF EvaluateBSDF(float3 inV, float3 inL, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    // Get N, L and NdotL parameters along with if transmission must be evaluated
    bool unused0;
    float3 unused1, unused2;
    float inNdotL;
    GetNLForDirectionalPunctualLights(bsdfData, preLightData, inL, inV, unused1, unused2, inNdotL, unused0);

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

        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
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

#if 1 //coatMask with proper lerped terms

        // Support for the coatMask hack costs us an additional F_Schlick evaluation if we want to lerp
        // properly to the LdotH fresnel term that we normally get without coat:
        // This is because in ComputeAdding, the energy coefficients are (either) (FGD based TODOENERGY or)
        // Schlick but even with Schlick, it is with NdotV, not LdotH like we have here.
        float3 bottomF;

#ifdef VLAYERED_RECOMPUTE_PERLIGHT // && VLAYERED which we are in this context, also: TODOENERGY: && COMPUTEADDING_DEFERRED_PREINT_FGD_FETCHES
        // In that case, preLightData.vLayerEnergyCoeff[BOTTOM_VLAYER_IDX] will contain an F_Schlick term
        // anyways (with other factors as usual for the stack) and possibly mixed or replaced by the iridescence term.
        bottomF = preLightData.vLayerEnergyCoeff[BOTTOM_VLAYER_IDX];
#else
        // If we don't recompute the stack per dirac lights, we ensure the coatmask make us lerp
        // to the usual LdotH Schlick term.
        bottomF = F_Schlick(bsdfData.fresnel0, savedLdotH);
        BSDF_ModifyFresnelForIridescence(bsdfData, preLightData, savedLdotH, bottomF);
#endif

        bottomF = lerp(bottomF, preLightData.vLayerEnergyCoeff[BOTTOM_VLAYER_IDX], bsdfData.coatMask);
#endif // ...coatMask with proper lerped terms

        cbsdf.specR =  max(0, NdotL[DNLV_BASE_IDX]) * bottomF
                              * lerp(DV[BASE_LOBEA_IDX] * preLightData.energyCompensationFactor[BASE_LOBEA_IDX],
                                     DV[BASE_LOBEB_IDX] * preLightData.energyCompensationFactor[BASE_LOBEB_IDX],
                                     bsdfData.lobeMix);

        cbsdf.specR +=  max(0, NdotL[DNLV_COAT_IDX]) * preLightData.vLayerEnergyCoeff[TOP_VLAYER_IDX]
                               * preLightData.energyCompensationFactor[COAT_LOBE_IDX]
                               * DV[COAT_LOBE_IDX];

#endif // ..._MATERIAL_FEATURE_COAT
    } // if( IsVLayeredEnabled(bsdfData) )
    else
    {
        // --------------------------------------------------------------------
        // NO VLAYERING:
        // --------------------------------------------------------------------

        // Note: See GetPreLightData(), in that case,
        // preLightData.layeredRoughnessT[0] = bsdfData.roughnessAT;
        // preLightData.layeredRoughnessB[0] = bsdfData.roughnessAB;
        // preLightData.layeredRoughnessT[1] = bsdfData.roughnessBT;
        // preLightData.layeredRoughnessB[1] = bsdfData.roughnessBB;

        // TODO: Proper Fresnel
        float3 F = F_Schlick(bsdfData.fresnel0, savedLdotH);

        BSDF_ModifyFresnelForIridescence(bsdfData, preLightData, savedLdotH, F);

        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
        {
            float TdotH, TdotL, BdotH, BdotL;
            CalculateAnisoAngles(bsdfData, H, L[0], V[0], TdotH, TdotL, BdotH, BdotL);
            DV[0] = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH[0], NdotV[0], TdotL, BdotL, NdotL[0],
                                          preLightData.layeredRoughnessT[0], preLightData.layeredRoughnessB[0],
                                          preLightData.partLambdaV[0]);
            DV[1] = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH[0], NdotV[0], TdotL, BdotL, NdotL[0],
                                          preLightData.layeredRoughnessT[1], preLightData.layeredRoughnessB[1],
                                          preLightData.partLambdaV[1]);
        }
        else
        {
            DV[0] = DV_SmithJointGGX(NdotH[0], NdotL[0], NdotV[0], preLightData.layeredRoughnessT[0], preLightData.partLambdaV[0]);
            DV[1] = DV_SmithJointGGX(NdotH[0], NdotL[0], NdotV[0], preLightData.layeredRoughnessT[1], preLightData.partLambdaV[1]);
        }

        IF_DEBUG( if(_DebugLobeMask.y == 0.0) DV[BASE_LOBEA_IDX] = (float3)0; )
        IF_DEBUG( if(_DebugLobeMask.z == 0.0) DV[BASE_LOBEB_IDX] = (float3)0; )

        cbsdf.specR = max(0, NdotL[0]) * F * lerp(DV[0]*preLightData.energyCompensationFactor[BASE_LOBEA_IDX],
                                                  DV[1]*preLightData.energyCompensationFactor[BASE_LOBEB_IDX],
                                                  bsdfData.lobeMix);
    }


    // TODO: config option + diffuse GGX
    float3 diffTerm = Lambert();

#ifdef VLAYERED_DIFFUSE_ENERGY_HACKED_TERM
    if( IsVLayeredEnabled(bsdfData) )
    {
        // Controlled by ifdef VLAYERED_DIFFUSE_ENERGY_HACKED_TERM
        // since preLightData.diffuseEnergy == float3(1,1,1) when not defined
        diffTerm *= preLightData.diffuseEnergy;
    }
#endif

    float diffuseNdotL = saturate(NdotL[DNLV_BASE_IDX]);
    cbsdf.diffR = diffTerm * diffuseNdotL;
    // TODO: Note for Stephane: I use -inNdotL here as it match visually what was done before the refactor but I guess it should be -diffuseNdotL
    cbsdf.diffT = diffTerm * ComputeWrappedDiffuseLighting(-inNdotL, TRANSMISSION_WRAP_LIGHT);

    return cbsdf;
}


void EvaluateBSDF_GetNormalUnclampedNdotV(BSDFData bsdfData, PreLightData preLightData, float3 V, out float3 N, out float unclampedNdotV)
{
    // TODO: This affects transmission and SSS, choose the normal to use when we have
    // both. For now, just use the base:
    N = bsdfData.normalWS;
    unclampedNdotV = preLightData.NdotV[BASE_NORMAL_IDX];

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
}

bool ShouldEvaluateThickObjectTransmission(float3 V, float3 L, PreLightData preLightData,
                                           BSDFData bsdfData, int shadowIndex)
{
#ifdef MATERIAL_INCLUDE_TRANSMISSION

    bool lightCanOnlyBeTransmitted;
    float3 mainN, mainL;
    float mainNdotL;
    GetNLForDirectionalPunctualLights(bsdfData, preLightData, L, V, mainN, mainL, mainNdotL, lightCanOnlyBeTransmitted);

    return HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_TRANSMISSION_MODE_THICK_OBJECT) &&
            (shadowIndex >= 0.0) && lightCanOnlyBeTransmitted;
#else
    return false;
#endif
}

//-----------------------------------------------------------------------------
// Surface shading (all light types) below
//-----------------------------------------------------------------------------

#define OVERRIDE_SHOULD_EVALUATE_THICK_OBJECT_TRANSMISSION
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"
//#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"
//...already included earlier.
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/SurfaceShading.hlsl"


//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                                        float3 V, PositionInputs posInput, PreLightData preLightData,
                                        DirectionalLightData light, BSDFData bsdfData,
                                        BuiltinData builtinData)
{
    return ShadeSurface_Directional(lightLoopContext, posInput, builtinData, preLightData, light, bsdfData, V);
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData light, BSDFData bsdfData, BuiltinData builtinData)
{
    return ShadeSurface_Punctual(lightLoopContext, posInput, builtinData, preLightData, light, bsdfData, V);
}

// NEWLITTODO: For a refence rendering option for area light, like STACK_LIT_DISPLAY_REFERENCE_AREA option in eg EvaluateBSDF_<area light type> :
//#include "LitReference.hlsl"

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Line(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 positionWS = posInput.positionWS;

#ifdef STACK_LIT_DISPLAY_REFERENCE_AREA
    // TODO This ref doesn't handle the StackLit model
    IntegrateBSDF_LineRef(V, positionWS, preLightData, lightData, bsdfData,
                          lighting.diffuse, lighting.specular);
#else
    float  len = lightData.size.x;
    float3 T   = lightData.right;

    float3 unL = lightData.positionRWS - positionWS;

    // Pick the major axis of the ellipsoid.
    float3 axis = lightData.right;

    // We define the ellipsoid s.t. r1 = (r + len / 2), r2 = r3 = r.
    // TODO: This could be precomputed.
    float range          = lightData.range;
    float invAspectRatio = saturate(range / (range + (0.5 * len)));

    // Compute the light attenuation.
    float intensity = EllipsoidalDistanceAttenuation(unL, axis, invAspectRatio,
                                                     lightData.rangeAttenuationScale,
                                                     lightData.rangeAttenuationBias);

    // Terminate if the shaded point is too far away.
    if (intensity == 0.0)
    {
        lightData.diffuseDimmer  *= intensity;
        lightData.specularDimmer *= intensity;

        // Translate the light s.t. the shaded point is at the origin of the coordinate system.
        lightData.positionRWS -= positionWS;

        // TODO: some of this could be precomputed.
        float3 P1 = lightData.positionRWS - T * (0.5 * len);
        float3 P2 = lightData.positionRWS + T * (0.5 * len);

        // Setup the default local canonical frame with X-Y aligned to the reflection plane
        // using orthoBasisViewNormal: without the anisotropic hack, this is only dependent on
        // if we have dual normal maps or not:

        // Rotate the endpoints into the local coordinate system.
        float3 localP1 = mul(P1, transpose(preLightData.orthoBasisViewNormal[BASE_NORMAL_IDX]));
        float3 localP2 = mul(P2, transpose(preLightData.orthoBasisViewNormal[BASE_NORMAL_IDX]));
        // Compute the binormal in the local coordinate system.
        float3 B = normalize(cross(localP1, localP2));

        if (AREA_LIGHTS_ANISOTROPY_ENABLED)  // statically known, so no need for if else, just overwrite the above
        {
            // Since we proceed with calculating diffuse and transmission irradiance, we setup
            // the points for the diffuse frame.
            // There's no anisotropy on the diffuse component and this is oriented considering
            // the proper base layer normal:
            localP1 = mul(P1, transpose(preLightData.orthoBasisViewNormalDiffuse));
            localP2 = mul(P2, transpose(preLightData.orthoBasisViewNormalDiffuse));
            B = normalize(cross(localP1, localP2));
        }

        // Calculate the L irradiance (ltcValue) first for the diffuse part and transmission,
        // then for the specular base layer and finishing with the coat.
        float ltcValue;

        // Evaluate the diffuse part
        ltcValue = LTCEvaluate(localP1, localP2, B, preLightData.ltcTransformDiffuse);
        ltcValue *= lightData.diffuseDimmer;
        // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
        lighting.diffuse = preLightData.diffuseFGD * preLightData.diffuseEnergy * ltcValue;

        UNITY_BRANCH if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION))
        {
            // Flip the view vector and the normal. The bitangent stays the same.
            float3x3 flipMatrix = float3x3(-1,  0,  0,
                                            0,  1,  0,
                                            0,  0, -1);

            // Use the Lambertian approximation for performance reasons.
            // The matrix multiplication should not generate any extra ALU on GCN.
            // TODO: double evaluation is very inefficient! This is a temporary solution.
            ltcValue  = LTCEvaluate(localP1, localP2, B, mul(flipMatrix, k_identity3x3));
            ltcValue *= lightData.diffuseDimmer;

            // VLAYERED_DIFFUSE_ENERGY_HACKED_TERM:
            // In Lit with Lambert, there's no diffuseFGD, it is one. In our case, we also
            // need a diffuse energy term when vlayered.

            // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
            // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            lighting.diffuse += bsdfData.transmittance * ltcValue * preLightData.diffuseEnergy;
        }

        // Evaluate the specular lobes for the stack
        IF_DEBUG( if ( _DebugLobeMask.y != 0.0) )
        {
            if (AREA_LIGHTS_ANISOTROPY_ENABLED)
            {
                // In that case, instead of only considering possibly dual normal maps and thus two
                // local canonical frames we have lobe specific frames because of the anisotropic hack:
                localP1 = mul(P1, transpose(preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_BASE_LOBEA_IDX]));
                localP2 = mul(P2, transpose(preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_BASE_LOBEA_IDX]));
                B = normalize(cross(localP1, localP2));
            }
            ltcValue = LTCEvaluate(localP1, localP2, B, preLightData.ltcTransformSpecular[BASE_LOBEA_IDX]);
            // See EvaluateBSDF_Env TODOENERGY:
            lighting.specular += preLightData.energyCompensationFactor[BASE_LOBEA_IDX] * preLightData.specularFGD[BASE_LOBEA_IDX] * ltcValue;
        }
        IF_DEBUG( if ( _DebugLobeMask.z != 0.0) )
        {
            if (AREA_LIGHTS_ANISOTROPY_ENABLED)
            {
                // In that case, instead of only considering possibly dual normal maps and thus two
                // local canonical frames we have lobe specific frames because of the anisotropic hack:
                localP1 = mul(P1, transpose(preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_BASE_LOBEB_IDX]));
                localP2 = mul(P2, transpose(preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_BASE_LOBEB_IDX]));
                B = normalize(cross(localP1, localP2));
            }
            ltcValue = LTCEvaluate(localP1, localP2, B, preLightData.ltcTransformSpecular[BASE_LOBEB_IDX]);
            lighting.specular += preLightData.energyCompensationFactor[BASE_LOBEB_IDX] * preLightData.specularFGD[BASE_LOBEB_IDX] * ltcValue;
        }

        if (IsVLayeredEnabled(bsdfData))
        {
            IF_DEBUG( if ( _DebugLobeMask.x != 0.0) )
            {
                if (IsCoatNormalMapEnabled(bsdfData))
                {
                    localP1 = mul(P1, transpose(preLightData.orthoBasisViewNormal[COAT_NORMAL_IDX]));
                    localP2 = mul(P2, transpose(preLightData.orthoBasisViewNormal[COAT_NORMAL_IDX]));
                    B = normalize(cross(localP1, localP2));
                }
                if (AREA_LIGHTS_ANISOTROPY_ENABLED) // statically known, so no need for if else, just overwrite the above
                {
                    // No need to check if we have dual normal maps here: alread taken care via iblN[COAT_LOBE_IDX]
                    // in GetPreLightData and setup in preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_COAT_LOBE_IDX].

                    // we have lobe specific frames because of the anisotropic hack (there's no anisotropy for the
                    // coat, but the index of the ortho basis is lobe-based still because of the base layer lobes which
                    // can have anisotropy).
                    localP1 = mul(P1, transpose(preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_COAT_LOBE_IDX]));
                    localP2 = mul(P2, transpose(preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_COAT_LOBE_IDX]));
                    B = normalize(cross(localP1, localP2));
                }
                ltcValue = LTCEvaluate(localP1, localP2, B, preLightData.ltcTransformSpecular[COAT_LOBE_IDX]);
                lighting.specular += preLightData.energyCompensationFactor[COAT_LOBE_IDX] * preLightData.specularFGD[COAT_LOBE_IDX] * ltcValue;
            }
        }
        lighting.specular *= lightData.specularDimmer;


        // Save ALU by applying 'lightData.color' only once.
        lighting.diffuse *= lightData.color;
        lighting.specular *= lightData.color;

    #ifdef DEBUG_DISPLAY
        if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
        {
            // Make sure we're using the base layer frame:
            localP1 = mul(P1, transpose(preLightData.orthoBasisViewNormal[BASE_NORMAL_IDX]));
            localP2 = mul(P2, transpose(preLightData.orthoBasisViewNormal[BASE_NORMAL_IDX]));
            if (AREA_LIGHTS_ANISOTROPY_ENABLED)
            {
                // In that case orthoBasisViewNormal[] is per lobe due to anistropic hack,
                // use orthoBasisViewNormalDiffuse:
                localP1 = mul(P1, transpose(preLightData.orthoBasisViewNormalDiffuse));
                localP2 = mul(P2, transpose(preLightData.orthoBasisViewNormalDiffuse));
            }
            B = normalize(cross(localP1, localP2));

            // Only lighting, not BSDF
            // Apply area light on lambert then multiply by PI to cancel Lambert
            lighting.diffuse = LTCEvaluate(localP1, localP2, B, k_identity3x3);
            lighting.diffuse *= PI * lightData.diffuseDimmer;
        }
    #endif
    }

#endif // STACK_LIT_DISPLAY_REFERENCE_AREA

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Rect - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

// #define ELLIPSOIDAL_ATTENUATION

DirectLighting EvaluateBSDF_Rect(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3 positionWS = posInput.positionWS;

#if SHADEROPTIONS_BARN_DOOR
    // Apply the barn door modification to the light data
    RectangularLightApplyBarnDoor(lightData, positionWS);
#endif

#ifdef STACK_LIT_DISPLAY_REFERENCE_AREA
    IntegrateBSDF_AreaRef(V, positionWS, preLightData, lightData, bsdfData,
                          lighting.diffuse, lighting.specular);
#else
    float3 unL = lightData.positionRWS - positionWS;

    // if (dot(lightData.forward, unL) >= eps), all points on the light are back-facing:
    // (Dont early return to guard against compiler bug for if / quick early return constructs)
    if (dot(lightData.forward, unL) < FLT_EPS)
    {

        // Rotate the light direction into the light space.
        float3x3 lightToWorld = float3x3(lightData.right, lightData.up, -lightData.forward);
        unL = mul(unL, transpose(lightToWorld));

        // TODO: This could be precomputed.
        float halfWidth  = lightData.size.x * 0.5;
        float halfHeight = lightData.size.y * 0.5;

        // Define the dimensions of the attenuation volume.
        // TODO: This could be precomputed.
        float  range      = lightData.range;
        float3 invHalfDim = rcp(float3(range + halfWidth,
                                       range + halfHeight,
                                       range));

        // Compute the light attenuation.
    #ifdef ELLIPSOIDAL_ATTENUATION
        // The attenuation volume is an axis-aligned ellipsoid s.t.
        // r1 = (r + w / 2), r2 = (r + h / 2), r3 = r.
        float intensity = EllipsoidalDistanceAttenuation(unL, invHalfDim,
                                                         lightData.rangeAttenuationScale,
                                                         lightData.rangeAttenuationBias);
    #else
        // The attenuation volume is an axis-aligned box s.t.
        // hX = (r + w / 2), hY = (r + h / 2), hZ = r.
        float intensity = BoxDistanceAttenuation(unL, invHalfDim,
                                                 lightData.rangeAttenuationScale,
                                                 lightData.rangeAttenuationBias);
    #endif

        // If the shaded point is too far away we avoid shading.
        // (guard against compiler bug for if / quick early return constructs)
        if (intensity != 0.0)
        {
            lightData.diffuseDimmer  *= intensity;
            lightData.specularDimmer *= intensity;

            // Translate the light s.t. the shaded point is at the origin of the coordinate system.
            lightData.positionRWS -= positionWS;

            float4x3 lightVerts;

            // TODO: some of this could be precomputed.
            lightVerts[0] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up * -halfHeight; // LL
            lightVerts[1] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up *  halfHeight; // UL
            lightVerts[2] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up *  halfHeight; // UR
            lightVerts[3] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up * -halfHeight; // LR

            // Rotate the endpoints into the local coordinate system.
            float4x3 localLightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal[BASE_NORMAL_IDX]));

            if (AREA_LIGHTS_ANISOTROPY_ENABLED)  // statically known, so no need for if else, just overwrite the above
            {
                // Since we proceed with calculating diffuse and transmission irradiance, we setup
                // the points for the diffuse frame.
                // There's no anisotropy on the diffuse component and this is oriented considering
                // the proper base layer normal:
                localLightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormalDiffuse));
            }

            // Calculate the L irradiance (ltcValue) first for the diffuse part and transmission,
            // then for the specular base layer and finishing with the coat.
            float3 ltcValue;

            // Evaluate the diffuse part
            // Polygon irradiance in the transformed configuration.
            float4x3 LD = mul(localLightVerts, preLightData.ltcTransformDiffuse);
            ltcValue  = PolygonIrradiance(LD);
            ltcValue *= lightData.diffuseDimmer;
            // Only apply cookie if there is one
            if ( lightData.cookieMode != COOKIEMODE_NONE )
            {
                // Compute the cookie data for the diffuse term
                float3 formFactorD =  PolygonFormFactor(LD);
                ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LD, formFactorD);
            }
            // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            lighting.diffuse = preLightData.diffuseFGD * preLightData.diffuseEnergy * ltcValue;

            UNITY_BRANCH if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION))
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
                float4x3 LTD = mul(localLightVerts, ltcTransform);
                ltcValue  = PolygonIrradiance(LTD);
                ltcValue *= lightData.diffuseDimmer;
                // Only apply cookie if there is one
                if ( lightData.cookieMode != COOKIEMODE_NONE )
                {
                    // Compute the cookie data for the transmission diffuse term
                    float3 formFactorTD = PolygonFormFactor(LTD);
                    ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LTD, formFactorTD);
                }
                // VLAYERED_DIFFUSE_ENERGY_HACKED_TERM:
                // In Lit with Lambert, there's no diffuseFGD, it is one. In our case, we also
                // need a diffuse energy term when vlayered.

                // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
                // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
                lighting.diffuse += bsdfData.transmittance * ltcValue * preLightData.diffuseEnergy;
            }

            // Evaluate the specular lobes for the stack
            IF_DEBUG( if ( _DebugLobeMask.y != 0.0) )
            {
                if (AREA_LIGHTS_ANISOTROPY_ENABLED)
                {
                    // In that case, instead of only considering possibly dual normal maps and thus two
                    // local canonical frames we have lobe specific frames because of the anisotropic hack:
                    localLightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_BASE_LOBEA_IDX]));
                }
                // Polygon irradiance in the transformed configuration.
                float4x3 LAS = mul(localLightVerts, preLightData.ltcTransformSpecular[BASE_LOBEA_IDX]);
                ltcValue  = PolygonIrradiance(LAS);
                // Only apply cookie if there is one
                if ( lightData.cookieMode != COOKIEMODE_NONE )
                {
                    // Compute the cookie data for the specular term
                    float3 formFactorAS =  PolygonFormFactor(LAS);
                    ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LAS, formFactorAS, bsdfData.perceptualRoughnessA);
                }

                // See EvaluateBSDF_Env TODOENERGY:
                lighting.specular += preLightData.energyCompensationFactor[BASE_LOBEA_IDX] * preLightData.specularFGD[BASE_LOBEA_IDX] * ltcValue;
            }
            IF_DEBUG( if ( _DebugLobeMask.z != 0.0) )
            {
                if (AREA_LIGHTS_ANISOTROPY_ENABLED)
                {
                    // In that case, instead of only considering possibly dual normal maps and thus two
                    // local canonical frames we have lobe specific frames because of the anisotropic hack:
                    localLightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_BASE_LOBEB_IDX]));
                }
                float4x3 LBS = mul(localLightVerts, preLightData.ltcTransformSpecular[BASE_LOBEB_IDX]);
                ltcValue  = PolygonIrradiance(LBS);
                // Only apply cookie if there is one
                if ( lightData.cookieMode != COOKIEMODE_NONE )
                {
                    // Compute the cookie data for the specular term
                    float3 formFactorBS =  PolygonFormFactor(LBS);
                    ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LBS, formFactorBS, bsdfData.perceptualRoughnessB);
                }

                lighting.specular += preLightData.energyCompensationFactor[BASE_LOBEB_IDX] * preLightData.specularFGD[BASE_LOBEB_IDX] * ltcValue;
            }

            if (IsVLayeredEnabled(bsdfData))
            {
                if (IsCoatNormalMapEnabled(bsdfData))
                {
                    localLightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal[COAT_NORMAL_IDX]));
                }
                if (AREA_LIGHTS_ANISOTROPY_ENABLED)
                {
                    // In that case, instead of only considering possibly dual normal maps and thus two
                    // local canonical frames we have lobe specific frames because of the anisotropic hack:
                    localLightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal[ORTHOBASIS_VN_COAT_LOBE_IDX]));
                }
                IF_DEBUG( if ( _DebugLobeMask.x != 0.0) )
                {
                    float4x3 LSCC = mul(localLightVerts, preLightData.ltcTransformSpecular[COAT_LOBE_IDX]);
                    ltcValue  = PolygonIrradiance(LSCC);
                    // Only apply cookie if there is one
                    if ( lightData.cookieMode != COOKIEMODE_NONE )
                    {
                        // Compute the cookie data for the specular term
                        float3 formFactorS =  PolygonFormFactor(LSCC);
                        ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LSCC, formFactorS, bsdfData.coatPerceptualRoughness);
                    }
                    lighting.specular += preLightData.energyCompensationFactor[COAT_LOBE_IDX] * preLightData.specularFGD[COAT_LOBE_IDX] * ltcValue;
                }
            }
            lighting.specular *= lightData.specularDimmer;

            SHADOW_TYPE shadow = EvaluateShadow_RectArea(lightLoopContext, posInput, lightData, builtinData, bsdfData.normalWS, normalize(lightData.positionRWS), length(lightData.positionRWS));
            lightData.color.rgb *= ComputeShadowColor(shadow, lightData.shadowTint, lightData.penumbraTint);

            // Save ALU by applying 'lightData.color' only once.
            lighting.diffuse *= lightData.color;
            lighting.specular *= lightData.color;

        #ifdef DEBUG_DISPLAY
            if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
            {
                // Make sure we're using the base layer frame:
                localLightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal[BASE_NORMAL_IDX]));
                if (AREA_LIGHTS_ANISOTROPY_ENABLED)
                {
                    // In that case orthoBasisViewNormal[] is per lobe due to anistropic hack,
                    // use orthoBasisViewNormalDiffuse:
                    localLightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormalDiffuse));
                }

                // Only lighting, not BSDF
                // Apply area light on lambert then multiply by PI to cancel Lambert
                lighting.diffuse = PolygonIrradiance(mul(localLightVerts, k_identity3x3));
                lighting.diffuse *= PI * lightData.diffuseDimmer;
            }
        #endif

        } // if light not too far

    } // if light not back-facing

#endif // STACK_LIT_DISPLAY_REFERENCE_AREA

    return lighting;
}

DirectLighting EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 V, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{
    if (lightData.lightType == GPULIGHTTYPE_TUBE)
    {
        return EvaluateBSDF_Line(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, builtinData);
    }
    else
    {
        return EvaluateBSDF_Rect(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, builtinData);
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_SSLighting for screen space lighting
// ----------------------------------------------------------------------------
IndirectLighting EvaluateBSDF_ScreenSpaceReflection(PositionInputs posInput,
                                                    inout PreLightData   preLightData, // inout for lobeReflectionWeight
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

    // For performance reasons, SSR doesn't allow us to be discriminating per lobe, ie wrt direction, roughness,
    // anisotropy, etc.
    // At least the vlayered BSDF stack model already represents the stack with a single interface with multiple
    // effective/equivalent lobes.

    // We could combine a "total reflectance value" so we won't be missing some intense reflections from
    // any lobe.
    // We can also try to match the data fed to the SSR compute via ConvertSurfaceDataToNormalData.
    // This is the approach we take since roughnesses between coat and base lobes can be very different, while
    // if the coat exist, ConvertSurfaceDataToNormalData will output the roughness of the coat and we don't need
    // a boost of sharp reflections from a potentially rough bottom layer.

    float3 reflectanceFactor = (float3)0.0;

    if (IsVLayeredEnabled(bsdfData) && (bsdfData.coatMask > 0))
    {
        reflectanceFactor = preLightData.specularFGD[COAT_LOBE_IDX];
        // TODOENERGY: If vlayered, should be done in ComputeAdding with FGD formulation for non dirac lights.
        // Incorrect, but for now:
        reflectanceFactor *= preLightData.energyCompensationFactor[COAT_LOBE_IDX];
        //float coatFGD = reflectanceFactor.r;
        reflectanceFactor *= preLightData.hemiSpecularOcclusion[COAT_LOBE_IDX];

        preLightData.lobeReflectionWeight[COAT_LOBE_IDX] = ssrLighting.a;

        // Coat-traced light re-use hack:
        //
        // Since we have a coat, the SSR light will be traced using its parameters.
        // We will however do like Lit and try to re-use that light for bottom lobes that have close enough roughnesses:
        //
        float basePerceptualRoughness[BASE_NB_LOBES];
        GetVLayeredBottomLobesPerceptualRoughnesses(bsdfData, preLightData, basePerceptualRoughness);

        for(int i = 0; i < BASE_NB_LOBES; i++)
        {
            float3 lobeFactor = preLightData.specularFGD[BASE_LOBEA_IDX + i]; // note: includes the lobeMix factor, see PreLightData.
            lobeFactor *= preLightData.hemiSpecularOcclusion[BASE_LOBEA_IDX + i];
            // TODOENERGY: If vlayered, should be done in ComputeAdding with FGD formulation for non dirac lights.
            // Incorrect, but for now:
            lobeFactor *= preLightData.energyCompensationFactor[BASE_LOBEA_IDX + i];

            // We use the coat-traced light according to how similar the base lobe roughness is to the coat roughness
            // (we can assume the coat is always smoother):
            //
            // - The roughness is equal to bsdfData.coatPerceptualRoughness
            //   We use the fact the clear coat and that base layer lobe have the same roughness and use the SSR as the indirect specular signal.
            // - The roughness is superior to bsdfData.coatPerceptualRoughness + 0.2.
            //   We cannot use the SSR for that base layer lobe.
            // - The roughness is within <= 0.2 away of bsdfData.coatPerceptualRoughness, we lerp between the two behaviors.
            float coatSSRLightOnBottomLayerBlendingFactor = lerp(1.0, 0.0, saturate( (basePerceptualRoughness[i] - bsdfData.coatPerceptualRoughness) / 0.2 ) );

            // Add the contribution of the coat-traced light for this base lobe, if any:
            reflectanceFactor += lobeFactor * coatSSRLightOnBottomLayerBlendingFactor;

            // Important: EvaluateBSDF_SSLighting() assumes it is the first light loop callback that contributes lighting,
            // we can thus directly set the reflectionHierarchyWeight instead of using UpdateLightingHierarchyWeights().

            // We initialize and keep track of the separate light reflection hierarchy weights and (see after this for loop scope) since only
            // reflectionHierarchyWeight is known to the light loop, normally a min() of all weights should be returned, but here, we know
            // the coat "consumes" at least as much than the bottom lobe, so the coatReflectionWeight dont interfere with the
            // (calculated from min of all) reflectionHierarchyWeight value returned.

            preLightData.lobeReflectionWeight[BASE_LOBEA_IDX + i] = ssrLighting.a * coatSSRLightOnBottomLayerBlendingFactor;
        }

        // We return min of lobeReflectionWeights, we know this is between the base lobes (coat hierarchyWeight is always higher or equal)
        reflectionHierarchyWeight = min(preLightData.lobeReflectionWeight[BASE_LOBEA_IDX], preLightData.lobeReflectionWeight[BASE_LOBEB_IDX]);
    }
    else
    {
        reflectionHierarchyWeight = ssrLighting.a;

        for(int i = 0; i < TOTAL_NB_LOBES; i++)
        {
            float3 lobeFactor = preLightData.specularFGD[i]; // note: includes the lobeMix factor, see PreLightData.
            lobeFactor *= preLightData.hemiSpecularOcclusion[i];
            // TODOENERGY: If vlayered, should be done in ComputeAdding with FGD formulation for non dirac lights.
            // Incorrect, but for now:
            lobeFactor *= preLightData.energyCompensationFactor[i];
            reflectanceFactor += lobeFactor;
        }

        preLightData.lobeReflectionWeight[BASE_LOBEA_IDX] =
        preLightData.lobeReflectionWeight[BASE_LOBEB_IDX] = reflectionHierarchyWeight;
    }

    // Note: RGB is already premultiplied by A.
    lighting.specularReflected = ssrLighting.rgb * reflectanceFactor;

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

    //NEWLITTODO

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
IndirectLighting EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    inout PreLightData preLightData, // inout for lobeReflectionWeight
                                    EnvLightData lightData, BSDFData bsdfData,
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

#ifdef STACK_LIT_DISPLAY_REFERENCE_IBL

    envLighting = IntegrateSpecularGGXIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);

    // TODO: Do refraction reference (is it even possible ?)
    // TODO: handle coat

    // TODO: Handle all lobes in reference


//    #ifdef USE_DIFFUSE_LAMBERT_BRDF
//    envLighting += IntegrateLambertIBLRef(lightData, V, bsdfData);
//    #else
//    envLighting += IntegrateDisneyDiffuseIBLRef(lightLoopContext, V, preLightData, lightData, bsdfData);
//    #endif

#else

    float3 R[TOTAL_NB_LOBES];
    float tempWeight[TOTAL_NB_LOBES];
    float smallestHierarchyWeight = 1.0;
    int i;

    for (i = 0; i < TOTAL_NB_LOBES; ++i)
    {
        tempWeight[i] = 1.0;
    }


    // We will sample the environment one time for each lobe.
    // Steps are:
    // -Calculate influence weights from intersection with the proxies.
    // -Fudge the sampling direction to dampen boundary artefacts.
    // -Fetch samples of preintegrated environment lighting
    // (see preLD, first part of the split-sum approx.)
    // -Use the BSDF preintegration terms we pre-fetched in preLightData
    // (second part of the split-sum approx.)
    // -Multiply the two split sum terms together for each lobe and add them.

    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)

    // if we want a single normal for influence proxy:
    //float3 influenceNormal; float unclampedNdotV;
    //EvaluateBSDF_GetNormalUnclampedNdotV(bsdfData, preLightData, V, influenceNormal, unclampedNdotV);

    UNITY_UNROLL
    for (i = 0; i < TOTAL_NB_LOBES; ++i)
    {
        float3 L;
        float3 normal;

#ifdef STACKLIT_DEBUG
        IF_FEATURE_COAT( if( (i == 0) && _DebugEnvLobeMask.x == 0.0) continue; )
        if( (i == (0 IF_FEATURE_COAT(+1))) && _DebugEnvLobeMask.y == 0.0) continue;
        if( (i == (1 IF_FEATURE_COAT(+1))) && _DebugEnvLobeMask.z == 0.0) continue;
#endif
        // If the lobe already grabbed all light it needed from previous lights (eg coat with SSR), we skip it.
        // We also check preLightData.specularFGD[i] to allow the compiler to optimize away when dual specular
        // lobe is not used: that way smallestHierarchyWeight can't be updated (uselessly) and more importantly,
        // can't influence a possible useless continuation of the lightloop because of consideration of its
        // hierarchyWeight
        if ((preLightData.lobeReflectionWeight[i] < 1.0) && any(preLightData.specularFGD[i] > 0))
        {
            // Compiler will deal with all that:
            normal = (NB_NORMALS > 1 && i == COAT_NORMAL_IDX) ? bsdfData.coatNormalWS : bsdfData.normalWS;

            R[i] = preLightData.iblR[i];
            if (!IsEnvIndexTexture2D(lightData.envIndex)) // ENVCACHETYPE_CUBEMAP
            {
                // Correction of reflected direction for better handling of rough material
                //
                // Notice again that when vlayering, the roughness and iblR properly use the output lobe statistics, but baseLayerNdotV
                // is used for the offspecular correction because the true original offspecular tilt is parametrized by
                // the angle at the base layer and the correction itself is influenced by that. See comments in GetPreLightData.
                float clampedNdotV = (NB_NORMALS > 1 && i == COAT_NORMAL_IDX) ? ClampNdotV(preLightData.NdotV[COAT_NORMAL_IDX]) : preLightData.baseLayerNdotV; // the later is already clamped

                R[i] = GetSpecularDominantDir(normal, preLightData.iblR[i], preLightData.iblPerceptualRoughness[i], clampedNdotV);
                // When we are rough, we tend to see outward shifting of the reflection when at the boundary of the projection volume
                // Also it appear like more sharp. To avoid these artifact and at the same time get better match to reference we lerp to original unmodified reflection.
                // Formula is empirical.
                float roughness = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness[i]);
                R[i] = lerp(R[i], preLightData.iblR[i], saturate(smoothstep(0, 1, roughness * roughness)));
            }

            float intersectionDistance = EvaluateLight_EnvIntersection(positionWS, normal, lightData, influenceShapeType, R[i], tempWeight[i]);

            float4 preLD = SampleEnvWithDistanceBaseRoughness(lightLoopContext, posInput, lightData, R[i], preLightData.iblPerceptualRoughness[i], intersectionDistance);

            // Used by planar reflection to discard pixel:
            tempWeight[i] *= preLD.a;

            L = preLD.rgb * preLightData.specularFGD[i];
            // TODOENERGY: If vlayered, should be done in ComputeAdding with FGD formulation for IBL. Same for LTC actually
            // Incorrect, but just for now:
            L *= preLightData.energyCompensationFactor[i];
            L *= preLightData.hemiSpecularOcclusion[i];

            UpdateLightingHierarchyWeights(preLightData.lobeReflectionWeight[i], tempWeight[i]);
            smallestHierarchyWeight = min(smallestHierarchyWeight, preLightData.lobeReflectionWeight[i]);
            envLighting += L * tempWeight[i];
        }
    }

#endif // STACK_LIT_DISPLAY_REFERENCE_IBL

    // The lightloop is aware of only one hierarchyWeight: to process further lighting for lobes not being lit up to
    // a full 1.0 of reflection contribution, just return the smallest we have
    // (Note we can't use the input hierarchyWeight in the loop above as it is important smallestHierarchyWeight
    // is initialized to 1.0 otherwise the lightloop visible weight will obviously never rise above what SSR left it at.)
    hierarchyWeight = smallestHierarchyWeight;

    envLighting *= lightData.multiplier;

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
                        PreLightData preLightData, BSDFData bsdfData, BuiltinData builtinData, AggregateLighting lighting,
                        out LightLoopOutput lightLoopOutput)
{
    // Specular occlusion has been pre-computed in GetPreLightData() and applied on indirect specular light
    // while screenSpaceAmbientOcclusion has also been cached in preLightData.
    // We apply the artistic diffuse occlusion on direct lighting here:
    float3 directAmbientOcclusion; // for debug below

    // bsdfData.diffuseColor is not appropriate to use when vlayered when doing GTAOMultiBounce here, but we can
    // try something with (bsdfData.diffuseColor * bsdfData.coatExtinction) (for specular occlusion with f0, it's
    // even worse but both are a hack anyway) We could also try "renormalizing diffuseEnergy" to the luminance of
    // diffuseColor.
    // For now, we use (bsdfData.diffuseColor * preLightData.diffuseEnergy) directly:
    float3 GTAOMultiBounceTintBase = (bsdfData.diffuseColor * preLightData.diffuseEnergy);
    GetApplyScreenSpaceDiffuseOcclusionForDirect(GTAOMultiBounceTintBase, preLightData.screenSpaceAmbientOcclusion, directAmbientOcclusion, lighting);

    // Subsurface scattering mode
    float3 modifiedDiffuseColor = GetModifiedDiffuseColorForSSS(bsdfData);

    // Compute diffuseOcclusion combining both the effects of the occlusion from data and from screen space, the
    // later which has already been sampled in GetPreLightData (also see comments about GTAOMultiBounceTintBase
    // above).
    float3 diffuseOcclusion = GetDiffuseOcclusionGTAOMultiBounce(bsdfData.ambientOcclusion, preLightData.screenSpaceAmbientOcclusion, GTAOMultiBounceTintBase);

    // Apply the albedo to the direct diffuse lighting (only once). The indirect (baked) diffuse lighting has
    // already had the albedo applied in ModifyBakedDiffuseLighting() but we now also apply the diffuse occlusion
    // on it. Specular occlusion has been applied per lobe during specular lighting evaluations before.
    // We also add emissive since it is not merged with bakeDiffuseLighting in ModifyBakedDiffuseLighting.
    // (also cf lit deferred EncodeToGBuffer function).
    lightLoopOutput.diffuseLighting = (modifiedDiffuseColor * lighting.direct.diffuse) + (builtinData.bakeDiffuseLighting * diffuseOcclusion) + builtinData.emissiveColor;

    lightLoopOutput.specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    // For specularOcclusion we display red to indicate there's not one value possible here.
    AmbientOcclusionFactor aoFactor;

    float3 specularOcclusion = float3(1.0,0.0,0.0);

#ifdef STACKLIT_DEBUG
    IF_FEATURE_COAT( if (_DebugSpecularOcclusion.w == 1.0) { specularOcclusion = preLightData.hemiSpecularOcclusion[COAT_LOBE_IDX]; } )
    if (_DebugSpecularOcclusion.w == 2.0) { specularOcclusion = preLightData.hemiSpecularOcclusion[BASE_LOBEA_IDX]; }
    if (_DebugSpecularOcclusion.w == 3.0) { specularOcclusion = preLightData.hemiSpecularOcclusion[BASE_LOBEB_IDX]; }
    if (_DebugSpecularOcclusion.w == 4.0) { specularOcclusion = /* float3(0.0,0.0,1.0) */ preLightData.screenSpaceAmbientOcclusion; }
#endif

    GetAmbientOcclusionFactor(diffuseOcclusion, specularOcclusion, directAmbientOcclusion /* not used for now in PostEvaluateBSDFDebugDisplay */ , aoFactor);

    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, lightLoopOutput);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
