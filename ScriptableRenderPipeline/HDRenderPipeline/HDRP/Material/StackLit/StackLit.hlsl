//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in StackLit.cs which generates StackLit.cs.hlsl
#include "StackLit.cs.hlsl"
//#include "../SubsurfaceScattering/SubsurfaceScattering.hlsl"
//#include "CoreRP/ShaderLibrary/VolumeRendering.hlsl"

//NEWLITTODO : wireup CBUFFERs for ambientocclusion, and other uniforms and samplers used:
//
// We need this for AO, Depth/Color pyramids, LTC lights data, FGD pre-integrated data.
//
// Also add options at the top of this file, see Lit.hlsl.

//-----------------------------------------------------------------------------
// Texture and constant buffer declaration
//-----------------------------------------------------------------------------

// Declare the BSDF specific FGD property and its fetching function
#include "../PreIntegratedFGD/PreIntegratedFGD.hlsl"

//-----------------------------------------------------------------------------
// Definition
//-----------------------------------------------------------------------------

#define HAS_REFRACTION (defined(_REFRACTION_PLANE) || defined(_REFRACTION_SPHERE)) && (defined(_REFRACTION_SSRAY_PROXY) || defined(_REFRACTION_SSRAY_HIZ))
#define DEFAULT_SPECULAR_VALUE 0.04

//
// Vertically Layered BSDF : "vlayering"
//

// Vlayer config options: 

//#define VLAYERED_RECOMPUTE_PERLIGHT // TODO test more, make it a shader_features
// probably too slow but just to check the difference it makes
//#define VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE
//#define VLAYERED_DIFFUSE_ENERGY_HACKED_TERM
//#define VLAYERED_IBL_DESTRETCH_COMPENSATION_FOR_ANISOTROPY


// Automatic:

// Mostly for struct array declarations, not really loops:
#ifdef _MATERIAL_FEATURE_COAT
#    define COAT_NB_LOBES 1
#    define COAT_LOBE_IDX 0
#    define BASE_LOBEA_IDX (COAT_LOBE_IDX+1)
#    define BASE_LOBEB_IDX (BASE_LOBEA_IDX+1)
#else
#    undef VLAYERED_RECOMPUTE_PERLIGHT
#    define COAT_NB_LOBES 0
#    define COAT_LOBE_IDX 0
#    define BASE_LOBEA_IDX 0
#    define BASE_LOBEB_IDX (BASE_LOBEA_IDX+1)
#endif

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

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    // IMPORTANT: In our forward only case, all enable flags are statically know at compile time, so the compiler can do compile time optimization
    bsdfData.materialFeatures = surfaceData.materialFeatures;

    // Two lobe base material
    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.perceptualRoughnessA = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothnessA);
    bsdfData.perceptualRoughnessB = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothnessB);
    bsdfData.lobeMix = surfaceData.lobeMix;

    // There is no metallic with SSS and specular color mode
    //todo: float metallic = HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR | MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING | MATERIALFEATUREFLAGS_LIT_TRANSMISSION) ? 0.0 : surfaceData.metallic;
    float metallic = surfaceData.metallic;

    bsdfData.diffuseColor = ComputeDiffuseColor(surfaceData.baseColor, metallic);
    bsdfData.fresnel0 = ComputeFresnel0(surfaceData.baseColor, surfaceData.metallic, DEFAULT_SPECULAR_VALUE);

    // Kind of obsolete without gbuffer, ie could use _MATERIAL_FEATURE* shader_features directly, but
    // if anything, makes the code more readable.
    if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
    {
        FillMaterialAnisotropy(surfaceData.anisotropy, surfaceData.tangentWS, cross(surfaceData.normalWS, surfaceData.tangentWS), bsdfData);
    }

    if (HasFeatureFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_COAT))
    {
        FillMaterialCoatData(PerceptualSmoothnessToPerceptualRoughness(surfaceData.coatPerceptualSmoothness),
                             surfaceData.coatIor, surfaceData.coatThickness, surfaceData.coatExtinction, bsdfData);

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
    float NdotV;                              // Could be negative due to normal mapping, use ClampNdotV()
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

    float energyCompensation;
    // TODOENERGY: we actually can use a scalar for the non vlayered case to apply at
    // PostEvaluateBSDF time, and for the vlayered case, fold compensation into FGD
    // terms during ComputeAdding (ie FGD becomes FGDinf) (but the approximation depends on 
    // f0, our FGD is scalar, not rgb, see GetEnergyCompensationFactor.)

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
// would be wrong in our case, since the correction terms depend on the interface the lobe
// corresponds to and compensation must happen at each FGD use in ComputeAdding. However, our
// framework is exactly designed to handle that problem, in that if we calculate and apply proper
// energy coefficient terms (should be calculated from FGDinf) and modulate each specular calculations
// with them, this will actually do compensation.

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
void ComputeStatistics(in  float  cti,   in  int    i, in BSDFData bsdfData,
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
        // Update energy
        R12 = F_Schlick(bsdfData.fresnel0, cti);
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


void ComputeAdding(float _cti, in BSDFData bsdfData, inout PreLightData preLightData, bool calledPerLight = false)
{
    // Global Variables
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
        ComputeStatistics(cti, i, bsdfData, ctt, R12, T12, R21, T21, s_r12, s_t12, j12, s_r21, s_t21, j21);

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
    // TODO: VLAYERED_RECOMPUTE_PERLIGHT and calledPerLight bool

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

#ifdef VLAYERED_IBL_DESTRETCH_COMPENSATION_FOR_ANISOTROPY
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

#ifdef VLAYERED_IBL_DESTRETCH_COMPENSATION_FOR_ANISOTROPY
        // TODOANISOTROPY
        ConvertRoughnessToAnisotropy(roughnessT, roughnessB, preLightData.iblAnisotropy[1]);
#else
        preLightData.iblAnisotropy[1] = bsdfData.anisotropy;
#endif
        preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX] = RoughnessToPerceptualRoughness((roughnessT + roughnessB)/2.0);

        if( !perLightOption )
        {
            // We're not going to get called again per analytical light so store the result needed and used by them:
            // LOBEA T and B part:
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




PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    ZERO_INITIALIZE(PreLightData, preLightData);

    float3 N = bsdfData.normalWS;
    preLightData.NdotV = dot(N, V);
    float NdotV = ClampNdotV(preLightData.NdotV);

    preLightData.diffuseEnergy = float3(1.0, 1.0, 1.0);

    // For eval IBL lights, we need:
    //
    // iblPerceptualRoughness (for FGD, mip, etc)
    // iblR                   (fetch direction compensated dominant spec)
    // specularFGD            (coatIblF is now in there too)
    // energyCompensation     (to a apply for each light sample since with layering it becomes interface specific)
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
    float baseLayerNdotV = NdotV;

    if( IsVLayeredEnabled(bsdfData) )
    {
#ifdef _MATERIAL_FEATURE_COAT
        // --------------------------------------------------------------------
        // VLAYERING:
        // --------------------------------------------------------------------

        preLightData.coatIeta = 1.0 / GetCoatEta(bsdfData);

        // First thing we need is compute the energy coefficients and new roughnesses.
        // Even if configured to do it also per analytical light, we need it for IBLs too.
        ComputeAdding(NdotV, bsdfData, preLightData, false);

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
            preLightData.partLambdaV[COAT_LOBE_IDX] = GetSmithJointGGXPartLambdaV(NdotV, preLightData.layeredCoatRoughness);
            preLightData.partLambdaV[BASE_LOBEA_IDX] = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV, preLightData.layeredRoughnessT[0], preLightData.layeredRoughnessB[0]);
            preLightData.partLambdaV[BASE_LOBEB_IDX] = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV, preLightData.layeredRoughnessT[1], preLightData.layeredRoughnessB[1]);
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

            iblN[COAT_LOBE_IDX] = N; // no anisotropy for coat.
            iblN[BASE_LOBEA_IDX] = GetAnisotropicModifiedNormal(grainDirWS[0], N, V, stretch[0]);
            iblN[BASE_LOBEB_IDX] = GetAnisotropicModifiedNormal(grainDirWS[1], N, V, stretch[1]);

        }
        else
        {

#ifndef VLAYERED_RECOMPUTE_PERLIGHT
            // We can precalculate lambdaVs for all lights here since we're not doing ComputeAdding per light
            preLightData.partLambdaV[COAT_LOBE_IDX] = GetSmithJointGGXPartLambdaV(NdotV, preLightData.layeredCoatRoughness);
            preLightData.partLambdaV[BASE_LOBEA_IDX] = GetSmithJointGGXPartLambdaV(NdotV, preLightData.layeredRoughnessT[0]);
            preLightData.partLambdaV[BASE_LOBEB_IDX] = GetSmithJointGGXPartLambdaV(NdotV, preLightData.layeredRoughnessT[1]);
#endif
            iblN[0] = iblN[1] = iblN[2] = N;
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

        baseLayerNdotV = sqrt(1 + Sq(preLightData.coatIeta)*(Sq(NdotV) - 1));
        //TODO refactor with EvalIridescence, Lit::GetPreLightData

        float diffuseFGDTmp; // unused, for coat layer FGD fetch

        GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV,
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
        preLightData.iblR[COAT_LOBE_IDX] = GetSpecularDominantDir(N, iblR[COAT_LOBE_IDX], preLightData.iblPerceptualRoughness[COAT_LOBE_IDX], NdotV);
        preLightData.iblR[BASE_LOBEA_IDX] = GetSpecularDominantDir(N, iblR[BASE_LOBEA_IDX], preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX], baseLayerNdotV);
        preLightData.iblR[BASE_LOBEB_IDX] = GetSpecularDominantDir(N, iblR[BASE_LOBEB_IDX], preLightData.iblPerceptualRoughness[BASE_LOBEB_IDX], baseLayerNdotV);

#ifdef LIT_USE_GGX_ENERGY_COMPENSATION
        // TODOENERGY:
        // This is actually changing FGD to FGDinf in the vlayering framework but this needs to be folded in the energy calculations
        // during ComputeAdding. Also even analytical lights will need the energy terms, not just IBL.
        // (See also CalculateEnergyCompensationFromSpecularReflectivity and ApplyEnergyCompensationToSpecularLighting)

        // Wrong to apply these here vs in ComputeAdding as compensation means replacing FGD with FGDinf but we have a chain of
        // them built by ComputeAdding with terms like (FGDinf), (1-FGDinf). Also, the compensation approximation depends on f0.
        // See TODOENERGY [eg:  (a1*ef)(1-(a2*ef)) != ef (a1)(1-a2) ]

        float specularReflectivityBase =  lerp(specularReflectivity[BASE_LOBEA_IDX], specularReflectivity[BASE_LOBEB_IDX], bsdfData.lobeMix);

        preLightData.energyCompensationFactor[BASE_LOBEA_IDX] = GetEnergyCompensationFactor(specularReflectivityBase, bsdfData.fresnel0);
        preLightData.energyCompensationFactor[BASE_LOBEB_IDX] = GetEnergyCompensationFactor(specularReflectivityBase, bsdfData.fresnel0);
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

            preLightData.partLambdaV[0] = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV, preLightData.layeredRoughnessT[0], preLightData.layeredRoughnessB[0]);
            preLightData.partLambdaV[1] = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV, preLightData.layeredRoughnessT[1], preLightData.layeredRoughnessB[1]);

            // For GGX aniso and IBL we have done an empirical (eye balled) approximation compare to the reference.
            // We use a single fetch, and we stretch the normal to use based on various criteria.
            // result are far away from the reference but better than nothing
            // For positive anisotropy values: tangent = highlight stretch (anisotropy) direction, bitangent = grain (brush) direction.
            float3 grainDirWS = (bsdfData.anisotropy >= 0.0) ? bsdfData.bitangentWS : bsdfData.tangentWS;

            // Reduce stretching for (perceptualRoughness < 0.2).
            float stretch[2];
            stretch[0] = abs(bsdfData.anisotropy) * saturate(5 * preLightData.iblPerceptualRoughness[0]);
            stretch[1] = abs(bsdfData.anisotropy) * saturate(5 * preLightData.iblPerceptualRoughness[1]);
            iblN[0] = GetAnisotropicModifiedNormal(grainDirWS, N, V, stretch[0]);
            iblN[1] = GetAnisotropicModifiedNormal(grainDirWS, N, V, stretch[1]);
        }
        else
        {
            preLightData.partLambdaV[0] = GetSmithJointGGXPartLambdaV(NdotV, preLightData.layeredRoughnessT[0]);
            preLightData.partLambdaV[1] = GetSmithJointGGXPartLambdaV(NdotV, preLightData.layeredRoughnessT[1]);
            iblN[0] = iblN[1] = N;
        } // ...no anisotropy


        // IBL
        // Handle IBL pre calculated data + GGX multiscattering energy loss compensation term

        GetPreIntegratedFGDGGXAndDisneyDiffuse(baseLayerNdotV, // just NdotV here...
                                               preLightData.iblPerceptualRoughness[BASE_LOBEA_IDX],
                                               bsdfData.fresnel0,
                                               preLightData.specularFGD[BASE_LOBEA_IDX],
                                               diffuseFGD[0],
                                               specularReflectivity[BASE_LOBEA_IDX]);

        GetPreIntegratedFGDGGXAndDisneyDiffuse(baseLayerNdotV,
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
        preLightData.iblR[0] = GetSpecularDominantDir(N, iblR[0], preLightData.iblPerceptualRoughness[0], NdotV);
        preLightData.iblR[1] = GetSpecularDominantDir(N, iblR[1], preLightData.iblPerceptualRoughness[1], NdotV);

#ifdef LIT_USE_GGX_ENERGY_COMPENSATION
        // Here, since this compensation term is already an average applied to a sum
        // (akin to a "split sum" approximation) we will just lerp our two "specularReflectivities".
        // When in vlayering, the same split approximation idea is embedded in the whole aggregate statistical
        // formulation. ie Compensation corresponds to using FGDinf instead of FGD.
        float specR =  lerp(specularReflectivity[0], specularReflectivity[1], bsdfData.lobeMix);
        //preLightData.energyCompensation[0] = CalculateEnergyCompensationFromSpecularReflectivity(specR);
        preLightData.energyCompensation = CalculateEnergyCompensationFromSpecularReflectivity(specR);
#else
        preLightData.energyCompensation = 0.0;
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
// LightLoop related function (Only include if required)
// HAS_LIGHTLOOP is define in Lighting.hlsl
//-----------------------------------------------------------------------------

#ifdef HAS_LIGHTLOOP

#ifndef _SURFACE_TYPE_TRANSPARENT
// For /Lighting/LightEvaluation.hlsl:
#define USE_DEFERRED_DIRECTIONAL_SHADOWS // Deferred shadows are always enabled for opaque objects
#endif

#include "../../Lighting/LightEvaluation.hlsl"
#include "../../Lighting/Reflection/VolumeProjection.hlsl"

//-----------------------------------------------------------------------------
// Lighting structure for light accumulation
//-----------------------------------------------------------------------------

// These structure allow to accumulate lighting accross the Lit material
// AggregateLighting is init to zero and transfer to EvaluateBSDF, but the LightLoop can't access its content.
//
// In fact, all structures here are opaque but used by LightLoop.hlsl.
// The Accumulate* functions are also used by LightLoop to accumulate the contributions of lights.
//
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

void CalculateAnisoAngles(BSDFData bsdfData, float3 L, float3 V, float invLenLV, out float TdotH, out float TdotL, out float BdotH, out float BdotL)
{
    float3 H = (L + V) * invLenLV;

    // For anisotropy we must not saturate these values
    TdotH = dot(bsdfData.tangentWS, H);
    TdotL = dot(bsdfData.tangentWS, L);
    BdotH = dot(bsdfData.bitangentWS, H);
    BdotL = dot(bsdfData.bitangentWS, L);
}

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

    float3 DV[TOTAL_NB_LOBES];

    // TODO: Proper Fresnel
    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);

    // TODO: with iridescence, will be optionally per light sample 

    if( IsVLayeredEnabled(bsdfData) )
    {
        // --------------------------------------------------------------------
        // VLAYERING:
        // --------------------------------------------------------------------
#ifdef _MATERIAL_FEATURE_COAT

        float topLdotH = LdotH; // == VdotH)
        float topNdotH = NdotH;
        float topNdotL = NdotL;
        float topNdotV = NdotV;
#if VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE
        // TODOWIP

        // Use the refracted angle at the bottom interface for BSDF calculations:
        // Seems like the more correct ones to use, but not obvious as we have the energy 
        // coefficients already (vLayerEnergyCoeff), which are like FGD (but no deferred 
        // FGD fetch to do here for analytical lights), so normally, we should use 
        // an output lobe parametrization, but multiple-scattering is not accounted fully
        // byt ComputeAdding (by deriving azimuth dependent covariance operators).
        // In the IBL case, we don't have a specific incoming light direction so the light 
        // representation matches more the correct context of a split sum approximation,
        // though we don't handle anisotropy correctly either anyway. 
        // In both cases we need to work around it, so just to test:
        float3 H = (L + V) * invLenLV;
        // H stays the same so calculate it one time
        V = CoatRefract(V, H, preLightData.coatIeta);
        L = reflect(-V, H);
        LdotV    = dot(L, V);
        invLenLV = rsqrt(max(2.0 * LdotV + 2.0, FLT_EPS));   // invLenLV = rcp(length(L + V)), clamp to avoid rsqrt(0) = NaN
        NdotH    = saturate((NdotL + dot(N, V)) * invLenLV); // Do not clamp NdotV here
        LdotH    = saturate(invLenLV * LdotV + invLenLV);
        NdotV    = ClampNdotV(dot(N, V));
#endif

#ifdef VLAYERED_RECOMPUTE_PERLIGHT
        // TODOWIP
        // Must call ComputeAdding and update partLambdaV
        ComputeAdding(topLdotH, bsdfData, preLightData, true); 
        // Notice topLdotH as interface angle, symmetric model parametrization (see sec. 6 and comments
        // on ComputeAdding)
        // layered*Roughness* and vLayerEnergyCoeff are now updated for the proper light direction.
        preLightData.partLambdaV[COAT_LOBE_IDX] = GetSmithJointGGXPartLambdaV(NdotV, preLightData.layeredCoatRoughness);
#endif

        // p9 eq(39): if we don't recompute per light, we just reuse the IBL energy terms as the fresnel terms
        // for our LdotH, too bad (similar to what we do with iridescence), along with the "wrongly" calculated energy.
        // Note that in any case, we should have used FGD terms (except for R12 at the start of the process) 
        // for the analytical light case, see comments at the top of ComputeAdding

        if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
        {

#ifdef VLAYERED_RECOMPUTE_PERLIGHT 
#ifdef VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE
            // we just changed V, update those:
            preLightData.TdotV = dot(bsdfData.tangentWS,   V);
            preLightData.BdotV = dot(bsdfData.bitangentWS, V);
#endif
            preLightData.partLambdaV[BASE_LOBEA_IDX] = GetSmithJointGGXAnisoPartLambdaV(preLightData.TdotV, preLightData.BdotV, NdotV,
                                                                                        preLightData.layeredRoughnessT[0], preLightData.layeredRoughnessB[0]);
            preLightData.partLambdaV[BASE_LOBEB_IDX] = GetSmithJointGGXAnisoPartLambdaV(preLightData.TdotV, preLightData.BdotV, NdotV,
                                                                                        preLightData.layeredRoughnessT[1], preLightData.layeredRoughnessB[1]);
#endif

            //float3 H = (L + V) * invLenLV;
            float TdotH, TdotL, BdotH, BdotL;
            CalculateAnisoAngles(bsdfData, L, V, invLenLV, TdotH, TdotL, BdotH, BdotL);

            DV[BASE_LOBEA_IDX] = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH, NdotV, TdotL, BdotL, NdotL, 
                                                      preLightData.layeredRoughnessT[0], preLightData.layeredRoughnessB[0], 
                                                      preLightData.partLambdaV[BASE_LOBEA_IDX]);

            DV[BASE_LOBEB_IDX] = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH, NdotV, TdotL, BdotL, NdotL, 
                                                      preLightData.layeredRoughnessT[1], preLightData.layeredRoughnessB[1], 
                                                      preLightData.partLambdaV[BASE_LOBEB_IDX]);

            DV[COAT_LOBE_IDX] = DV_SmithJointGGX(topNdotH, topNdotL, topNdotV, preLightData.layeredCoatRoughness, preLightData.partLambdaV[COAT_LOBE_IDX]);

        }
        else 
        {
#ifdef VLAYERED_RECOMPUTE_PERLIGHT
            preLightData.partLambdaV[BASE_LOBEA_IDX] = GetSmithJointGGXPartLambdaV(NdotV, preLightData.layeredRoughnessT[0]);
            preLightData.partLambdaV[BASE_LOBEB_IDX] = GetSmithJointGGXPartLambdaV(NdotV, preLightData.layeredRoughnessT[1]);
#endif
            DV[COAT_LOBE_IDX] = DV_SmithJointGGX(topNdotH, topNdotL, topNdotV, preLightData.layeredCoatRoughness, preLightData.partLambdaV[COAT_LOBE_IDX]);
            DV[BASE_LOBEA_IDX] = DV_SmithJointGGX(NdotH, NdotL, NdotV, preLightData.layeredRoughnessT[0], preLightData.partLambdaV[BASE_LOBEA_IDX]);
            DV[BASE_LOBEB_IDX] = DV_SmithJointGGX(NdotH, NdotL, NdotV, preLightData.layeredRoughnessT[1], preLightData.partLambdaV[BASE_LOBEB_IDX]);
        }


        specularLighting =  preLightData.vLayerEnergyCoeff[BOTTOM_VLAYER_IDX] 
                          * preLightData.energyCompensationFactor[BASE_LOBEA_IDX] // either energyCompensationFactor index will do, same one.
                          * lerp(DV[BASE_LOBEA_IDX], DV[BASE_LOBEB_IDX], bsdfData.lobeMix);

        specularLighting +=  preLightData.vLayerEnergyCoeff[TOP_VLAYER_IDX]
                           * preLightData.energyCompensationFactor[COAT_LOBE_IDX]
                           * DV[COAT_LOBE_IDX];

#endif // ..._MATERIAL_FEATURE_COAT
    } // if( IsVLayeredEnabled(bsdfData) )
    else
    {
        // --------------------------------------------------------------------
        // NO VLAYERING:
        // --------------------------------------------------------------------
        if (HasFeatureFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY))
        {
            float3 H = (L + V) * invLenLV;
            float TdotH, TdotL, BdotH, BdotL;
            CalculateAnisoAngles(bsdfData, L, V, invLenLV, TdotH, TdotL, BdotH, BdotL);
            DV[0] = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH, NdotV, TdotL, BdotL, NdotL, 
                                          bsdfData.roughnessAT, bsdfData.roughnessAB, 
                                          preLightData.partLambdaV[0]);
            DV[1] = DV_SmithJointGGXAniso(TdotH, BdotH, NdotH, NdotV, TdotL, BdotL, NdotL, 
                                          bsdfData.roughnessBT, bsdfData.roughnessBB, 
                                          preLightData.partLambdaV[1]);
        }
        else
        {
            DV[0] = DV_SmithJointGGX(NdotH, NdotL, NdotV, bsdfData.roughnessAT, preLightData.partLambdaV[0]);
            DV[1] = DV_SmithJointGGX(NdotH, NdotL, NdotV, bsdfData.roughnessBT, preLightData.partLambdaV[1]);
        }
        specularLighting = F * lerp(DV[0], DV[1], bsdfData.lobeMix);
        //...and energy compensation is applied at PostEvaluateBSDF when no vlayering.
    }


    // TODO: config option + diffuse GGX
    float3 diffuseTerm = Lambert();

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
    //float  NdotV = ClampNdotV(preLightData.NdotV);
    float  NdotL = dot(N, L);
    //float  LdotV = dot(L, V);

    // color and attenuation are outputted  by EvaluateLight:
    float3 color;
    float attenuation;
    EvaluateLight_Directional(lightLoopContext, posInput, lightData, bakeLightingData, N, L, color, attenuation);

    float intensity = max(0, attenuation * NdotL); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: We use NdotL here to early out, but in case of coat this is not correct. But we are ok with this
    UNITY_BRANCH if (intensity > 0.0)
    {
        BSDF(V, L, NdotL, posInput.positionWS, preLightData, bsdfData, lighting.diffuse, lighting.specular);

        lighting.diffuse  *= intensity * lightData.diffuseScale;
        lighting.specular *= intensity * lightData.specularScale;
    }

    // NEWLITTODO: Mixed thickness, transmission

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
    float  NdotV = ClampNdotV(preLightData.NdotV);
    float  NdotL = dot(N, L);
    float  LdotV = dot(L, V);

    // NEWLITTODO: mixedThickness, transmission

    float3 color;
    float attenuation;
    EvaluateLight_Punctual(lightLoopContext, posInput, lightData, bakeLightingData, N, L,
                           lightToSample, distances, color, attenuation);


    float intensity = max(0, attenuation * NdotL); // Warning: attenuation can be greater than 1 due to the inverse square attenuation (when position is close to light)

    // Note: We use NdotL here to early out, but in case of coat this is not correct. But we are ok with this
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

    //NEWLITTODO : transmission


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
    // -Do early discard for planar reflections.

    // -Fetch samples of preintegrated environment lighting
    // (see preLD, first part of the split-sum approx.)
    // -Use the BSDF preintegration terms we pre-fetched in preLightData
    // (second part of the split-sum approx.,
    //  and common to all Env. Lights. using the same BSDF and
    //  we only have GGX thus only one FGD map for now)
    // -Multiply the two split sum terms together for each lobe
    // and lerp them and/or add them.

    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)

    for ( i = 0; i < TOTAL_NB_LOBES; ++i)
    {
        float3 L;

        EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, R[i], tempWeight[i]);

        // When we are rough, we tend to see outward shifting of the reflection when at the boundary of the projection volume
        // Also it appear like more sharp. To avoid these artifact and at the same time get better match to reference we lerp to original unmodified reflection.
        // Formula is empirical.
        float roughness = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness[i]);
        R[i] = lerp(R[i], preLightData.iblR[i], saturate(smoothstep(0, 1, roughness * roughness)));


        float iblMipLevel = PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness[i]);
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
    float3 bakeDiffuseLighting = bakeLightingData.bakeDiffuseLighting;

    AmbientOcclusionFactor aoFactor;
    // Use GTAOMultiBounce approximation for ambient occlusion (allow to get a tint from the baseColor)
    GetScreenSpaceAmbientOcclusionMultibounce(posInput.positionSS, preLightData.NdotV, lerp(bsdfData.perceptualRoughnessA, bsdfData.perceptualRoughnessB, bsdfData.lobeMix), bsdfData.ambientOcclusion, 1.0, bsdfData.diffuseColor, bsdfData.fresnel0, aoFactor);

    // Add indirect diffuse + emissive (if any) - Ambient occlusion is multiply by emissive which is wrong but not a big deal
    bakeDiffuseLighting                 *= aoFactor.indirectAmbientOcclusion;
    lighting.direct.diffuse             *= aoFactor.directAmbientOcclusion;

    // diffuse lighting has already had the albedo applied in GetBakedDiffuseLighting().
    diffuseLighting = bsdfData.diffuseColor * lighting.direct.diffuse + bakeDiffuseLighting;

    specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

    if (!IsVLayeredEnabled(bsdfData))
    {
        // Note that when we're vlayered, this can be different per lobe depending on
        // which interface generates it.
        // Apply the fudge factor (boost) to compensate for multiple scattering not accounted for in the BSDF.
        // This assumes all spec comes from a GGX BSDF.
        specularLighting = ApplyEnergyCompensationToSpecularLighting(specularLighting, bsdfData.fresnel0, preLightData.energyCompensation);
    }

#ifdef DEBUG_DISPLAY

    if (_DebugLightingMode != 0)
    {
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting

        switch (_DebugLightingMode)
        {
        case DEBUGLIGHTINGMODE_LUX_METER:
            diffuseLighting = lighting.direct.diffuse + bakeLightingData.bakeDiffuseLighting;
            break;

        case DEBUGLIGHTINGMODE_INDIRECT_DIFFUSE_OCCLUSION:
            diffuseLighting = aoFactor.indirectAmbientOcclusion;
            break;

        case DEBUGLIGHTINGMODE_INDIRECT_SPECULAR_OCCLUSION:
            //diffuseLighting = aoFactor.indirectSpecularOcclusion;
            break;

        case DEBUGLIGHTINGMODE_SCREEN_SPACE_TRACING_REFRACTION:
            //if (_DebugLightingSubMode != DEBUGSCREENSPACETRACING_COLOR)
            //    diffuseLighting = lighting.indirect.specularTransmitted;
            break;
        }
    }
    else if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        diffuseLighting = bsdfData.diffuseColor;
        specularLighting = float3(0.0, 0.0, 0.0); // Disable specular lighting
    }

#endif
}

#endif // #ifdef HAS_LIGHTLOOP
