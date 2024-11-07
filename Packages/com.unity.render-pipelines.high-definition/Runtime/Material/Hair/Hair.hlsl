//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in Hair.cs which generates Hair.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

//-----------------------------------------------------------------------------
// Texture and constant buffer declaration
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"

#ifndef _ENVIRONMENT_LIGHT_SAMPLE_COUNT
#define _ENVIRONMENT_LIGHT_SAMPLE_COUNT 1
#endif

#ifndef _AREA_LIGHT_SAMPLE_COUNT
#define _AREA_LIGHT_SAMPLE_COUNT 1
#endif

#define DEFAULT_HAIR_SPECULAR_VALUE 0.0465 // Hair is IOR 1.55

// These H offset values (-1, 1) are used to approximate the integral for far-field azimuthal scattering.
// For TT, the dominant contribution comes from light transmitted straight through the fiber (thus 0).
// For TRT, a similar observation is made and v3/2 is used to approximate.
#define HAIR_H_TT  0.0
#define HAIR_H_TRT 0.86602540378

// #define HAIR_DISPLAY_REFERENCE_BSDF
// #define HAIR_DISPLAY_REFERENCE_IBL

// Extra material feature flag we utilize to compile different versions of BSDF evaluation (for pre-integration, etc.)
#define MATERIALFEATUREFLAGS_HAIR_MARSCHNER_SKIP_R            (1 << 16)
#define MATERIALFEATUREFLAGS_HAIR_MARSCHNER_SKIP_TT           (1 << 17)
#define MATERIALFEATUREFLAGS_HAIR_MARSCHNER_SKIP_TRT          (1 << 18)
#define MATERIALFEATUREFLAGS_HAIR_MARSCHNER_SKIP_LONGITUDINAL (1 << 19)

//-----------------------------------------------------------------------------
// Absorption Parameterization Mappings
//-----------------------------------------------------------------------------

float GetAbsorptionDenominator(float azimuthalRoughness)
{
    const float beta = azimuthalRoughness;

#if 0
    float beta2 = beta  * beta;
    float beta3 = beta2 * beta;
    float beta4 = beta3 * beta;
    float beta5 = beta4 * beta;

    // Least squares fit of an inverse mapping between scattering parameters and scattering albedo.
    return 5.969 - (0.215 * beta) + (2.532 * beta2) - (10.73 * beta3) + (5.574 * beta4) + (0.245 * beta5);
#else
    // Simplified version of the above.
    return (((((0.245f * beta) + 5.574f) * beta - 10.73f) * beta + 2.532f) * beta - 0.215f) * beta + 5.969f;
#endif
}

// Ref: A Practical and Controllable Hair and Fur Model for Production Path Tracing Eq. 9
float3 AbsorptionFromReflectance(float3 diffuseColor, float azimuthalRoughness)
{
    // Enforce a minimum value to prevent NaNs.
    diffuseColor = max(diffuseColor, 1e-3);

    return Sq(log(diffuseColor) / GetAbsorptionDenominator(azimuthalRoughness));
}

// Require an inverse mapping, as we parameterize the LUTs by reflectance wavelength (or for approximation that rely on diffuse).
float3 ReflectanceFromAbsorption(float3 absorption, float azimuthalRoughness)
{
    // Enforce a minimum value to prevent NaNs.
    absorption = max(absorption, 0.0);

    return exp(-sqrt(absorption) * GetAbsorptionDenominator(azimuthalRoughness));
}

// Ref: An Energy-Conserving Hair Reflectance Model Sec. 6.1
float3 AbsorptionFromMelanin(float eumelanin, float pheomelanin)
{
    const float3 eA = float3(0.419, 0.697, 1.37);
    const float3 eP = float3(0.187, 0.4,   1.05);

    return (eumelanin * eA) + (pheomelanin * eP);
}

float3 ReflectanceFromMelanin(float eumelanin, float pheomelanin, float azimuthalRoughness)
{
    return ReflectanceFromAbsorption( AbsorptionFromMelanin(eumelanin, pheomelanin), azimuthalRoughness );
}

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

// Ref: "Light Scattering from Human Hair Fibers"
// Longitudinal scattering as modeled by a normal distribution.
// To be used as an approximation to d'Eon et al's Energy Conserving Longitudinal Scattering Function.
// TODO: Move me to BSDF.hlsl

half3 Gaussian(half3 thetaH, half3 beta)
{
    beta = max(beta, 1e-5); // zero-div guard

    // NOTE: This gaussian assumes that beta is already squared.
    return rcp(sqrt(TWO_PI * beta)) * exp(-Sq(thetaH) / (2 * beta));
}

float GetHFromTube(float3 L, float3 N, float3 T)
{
    // Angle of inclination from normal plane.
    float sinTheta = dot(L, T);

    // Project w to the normal plane.
    float3 LProj = SafeNormalize(L - sinTheta * T);

    // Find gamma in the normal plane.
    float cosGamma = dot(LProj, N);

    // Need to account for the sign to recover -1..1
    float sgn = sign(dot(N, cross(LProj, T)));

    // Length along the fiber width.
    return SafeSqrt(1 - Sq(cosGamma)) * sgn;
}

float ModifiedRefractionIndex(float cosThetaD)
{
    // Original derivation of modified refraction index for arbitrary IOR.
    // float sinThetaD = sqrt(1 - Sq(cosThetaD));
    // return sqrt(Sq(eta) - Sq(sinThetaD)) / cosThetaD;

    // Karis approximation for the modified refraction index for human hair (1.55)
    return 1.19 / cosThetaD + (0.36 * cosThetaD);
}

float4 GetDiffuseOrDefaultColor(BSDFData bsdfData, float replace)
{
    return float4(bsdfData.diffuseColor, 0.0);
}

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    return bsdfData.geomNormalWS;
}

float GetAmbientOcclusionForMicroShadowing(BSDFData bsdfData)
{
    // Don't do micro shadow for hair, don't really make sense
    return 1.0;
}

// This function is use to help with debugging and must be implemented by any lit material
// Implementer must take into account what are the current override component and
// adjust SurfaceData properties accordingdly
void ApplyDebugToSurfaceData(float3x3 tangentToWorld, inout SurfaceData surfaceData)
{
#ifdef DEBUG_DISPLAY
    // NOTE: THe _Debug* uniforms come from /HDRP/Debug/DebugDisplay.hlsl

    // Override value if requested by user
    // this can be use also in case of debug lighting mode like diffuse only
    bool overrideAlbedo = _DebugLightingAlbedo.x != 0.0;
    bool overrideSmoothness = _DebugLightingSmoothness.x != 0.0;
    bool overrideNormal = _DebugLightingNormal.x != 0.0;
    bool overrideAO = _DebugLightingAmbientOcclusion.x != 0.0;

    if (overrideAlbedo)
    {
        float3 overrideAlbedoValue = _DebugLightingAlbedo.yzw;
        surfaceData.diffuseColor = overrideAlbedoValue;
    }

    if (overrideSmoothness)
    {
        float overrideSmoothnessValue = _DebugLightingSmoothness.y;
        surfaceData.perceptualSmoothness = overrideSmoothnessValue;
        surfaceData.secondaryPerceptualSmoothness = overrideSmoothnessValue;
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

    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_DIFFUSE_COLOR)
    {
        surfaceData.diffuseColor = pbrDiffuseColorValidate(surfaceData.diffuseColor, DEFAULT_HAIR_SPECULAR_VALUE, false, false).xyz;
    }
    else if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_SPECULAR_COLOR)
    {
        surfaceData.diffuseColor = pbrSpecularColorValidate(surfaceData.diffuseColor, DEFAULT_HAIR_SPECULAR_VALUE, false, false).xyz;
    }
#endif
}

// Note: This will be available and used in ShaderPassForward.hlsl since in Hair.shader,
// just before including the core code of the pass (ShaderPassForward.hlsl) we include
// Material.hlsl (or Lighting.hlsl which includes it) which in turn includes us,
// Hair.shader, via the #if defined(UNITY_MATERIAL_*) glue mechanism.
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

NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    NormalData normalData;
    normalData.normalWS = surfaceData.normalWS;
    normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    return normalData;
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

float RoughnessToBlinnPhongSpecularExponent(float roughness)
{
    return clamp(2 * rcp(max(roughness * roughness, FLT_EPS)) - 2, FLT_EPS, rcp(FLT_EPS));
}

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)
{
    BSDFData bsdfData;
    ZERO_INITIALIZE(BSDFData, bsdfData);

    // IMPORTANT: All enable flags are statically know at compile time, so the compiler can do compile time optimization
    bsdfData.materialFeatures = surfaceData.materialFeatures;

    bsdfData.ambientOcclusion = surfaceData.ambientOcclusion;
    bsdfData.specularOcclusion = surfaceData.specularOcclusion;

    bsdfData.diffuseColor = surfaceData.diffuseColor;

    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.geomNormalWS = surfaceData.geomNormalWS;

    // Enforce a maximum smoothness to prevent NaNs.
    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(min(1.0 - 1e-2, surfaceData.perceptualSmoothness));

    // This value will be override by the value in diffusion profile
    bsdfData.fresnel0                 = DEFAULT_HAIR_SPECULAR_VALUE;
    bsdfData.transmittance            = surfaceData.transmittance;
    bsdfData.rimTransmissionIntensity = surfaceData.rimTransmissionIntensity;

    // This is the hair tangent (which represents the hair strand direction, root to tip).
    bsdfData.hairStrandDirectionWS = surfaceData.hairStrandDirectionWS;

    // Kajiya kay
    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY))
    {
        bsdfData.secondaryPerceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.secondaryPerceptualSmoothness);
        bsdfData.specularTint = surfaceData.specularTint;
        bsdfData.secondarySpecularTint = surfaceData.secondarySpecularTint;
        bsdfData.specularShift = surfaceData.specularShift;
        bsdfData.secondarySpecularShift = surfaceData.secondarySpecularShift;

        float roughness1 = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
        float roughness2 = PerceptualRoughnessToRoughness(bsdfData.secondaryPerceptualRoughness);

        bsdfData.specularExponent          = RoughnessToBlinnPhongSpecularExponent(roughness1);
        bsdfData.secondarySpecularExponent = RoughnessToBlinnPhongSpecularExponent(roughness2);

        bsdfData.anisotropy = 0.8; // For hair we fix the anisotropy
    }

    // Marschner
    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER) ||
        HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER_CINEMATIC))
    {
        // Cuticle Angle
        const float cuticleAngle = radians(surfaceData.cuticleAngle);
        bsdfData.cuticleAngle    = -cuticleAngle;
        bsdfData.cuticleAngleR   = -cuticleAngle;
        bsdfData.cuticleAngleTT  =  cuticleAngle * 0.5;
        bsdfData.cuticleAngleTRT =  cuticleAngle * 1.5;

        // Longitudinal Roughness
        const float roughnessL = bsdfData.perceptualRoughness;
        bsdfData.roughnessR    = PerceptualRoughnessToRoughness(roughnessL);
        bsdfData.roughnessTT   = PerceptualRoughnessToRoughness(roughnessL * 0.5);
        bsdfData.roughnessTRT  = PerceptualRoughnessToRoughness(roughnessL * 2.0);

        // Azimuthal Roughness
        bsdfData.perceptualRoughnessRadial = PerceptualSmoothnessToPerceptualRoughness(min(1.0 - 1e-2, surfaceData.perceptualRadialSmoothness));

        // Absorption. Note: We require diffuse color to parameterize LUTs and for approximation purposes.
    #if _ABSORPTION_FROM_COLOR
        bsdfData.absorption   = AbsorptionFromReflectance(surfaceData.diffuseColor, bsdfData.perceptualRoughnessRadial);
    #elif _ABSORPTION_FROM_MELANIN
        bsdfData.absorption   = AbsorptionFromMelanin(surfaceData.eumelanin, surfaceData.pheomelanin);
        bsdfData.diffuseColor = ReflectanceFromMelanin(surfaceData.eumelanin, surfaceData.pheomelanin, bsdfData.perceptualRoughnessRadial);
    #else
        bsdfData.absorption   = surfaceData.absorption;
        bsdfData.diffuseColor = ReflectanceFromAbsorption(bsdfData.absorption, bsdfData.perceptualRoughnessRadial);
    #endif

#if _MATERIAL_FEATURE_HAIR_MARSCHNER_CINEMATIC
        bsdfData.strandCountProbe = surfaceData.strandCountProbe;

    #if !_USE_SPLINE_VISIBILITY_FOR_MULTIPLE_SCATTERING
        // The user has specified that they would like to derive self-shadowing data only from the volumetric grid.
        bsdfData.visibility = -1;
    #endif
#endif

        // By default the normalization factor should be 1 and overridden by area lights.
        bsdfData.distributionNormalizationFactor = 1;

        // Only necesarry for reference.
        // bsdfData.h = -1 + 2 * InterleavedGradientNoise(positionSS, _TaaFrameInfo.z);
    }

    ApplyDebugToBSDFData(bsdfData);

    return bsdfData;
}

//-----------------------------------------------------------------------------
// Debug method (use to display values)
//-----------------------------------------------------------------------------

// This function call the generated debug function and allow to override the debug output if needed
void GetSurfaceDataDebug(uint paramId, SurfaceData surfaceData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedSurfaceDataDebug(paramId, surfaceData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
    case DEBUGVIEW_HAIR_SURFACEDATA_NORMAL_VIEW_SPACE:
        // Convert to view space
        {
            float3 vsNormal = TransformWorldToViewDir(surfaceData.normalWS);
            result = IsNormalized(vsNormal) ?  vsNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_HAIR_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        {
            float3 vsGeomNormal = TransformWorldToViewDir(surfaceData.geomNormalWS);
            result = IsNormalized(vsGeomNormal) ?  vsGeomNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    }
}

// This function call the generated debug function and allow to override the debug output if needed
void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
    case DEBUGVIEW_HAIR_BSDFDATA_NORMAL_VIEW_SPACE:
        // Convert to view space
        {
            float3 vsNormal = TransformWorldToViewDir(bsdfData.normalWS);
            result = IsNormalized(vsNormal) ?  vsNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_HAIR_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        {
            float3 vsGeomNormal = TransformWorldToViewDir(bsdfData.geomNormalWS);
            result = IsNormalized(vsGeomNormal) ?  vsGeomNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    }
}

void GetPBRValidatorDebug(SurfaceData surfaceData, inout float3 result)
{
    result = surfaceData.diffuseColor;
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
    float NdotV;        // Could be negative due to normal mapping, use ClampNdotV()

    // IBL
    float3 iblR;                     // Reflected specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness;

    // Area lights (17 VGPRs)
    // TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.
    float3x3 orthoBasisViewNormal;   // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
    float3x3 ltcTransformDiffuse;    // Inverse transformation for Lambertian or Disney Diffuse        (4x VGPRs)
    float3x3 ltcTransformSpecular;   // Inverse transformation for GGX                                 (4x VGPRs)

    float3 specularFGD;              // Store preintegrated BSDF for both specular and diffuse
    float  diffuseFGD;
};

//
// ClampRoughness helper specific to this material
//
void ClampRoughness(inout PreLightData preLightData, inout BSDFData bsdfData, float minRoughness)
{
    bsdfData.perceptualRoughness = max(RoughnessToPerceptualRoughness(minRoughness), bsdfData.perceptualRoughness);
    bsdfData.secondaryPerceptualRoughness = max(RoughnessToPerceptualRoughness(minRoughness), bsdfData.secondaryPerceptualRoughness);

    bsdfData.roughnessR   = max(minRoughness, bsdfData.roughnessR);
    bsdfData.roughnessTT  = max(minRoughness, bsdfData.roughnessTT);
    bsdfData.roughnessTRT = max(minRoughness, bsdfData.roughnessTRT);
}

// This function is call to precompute heavy calculation before lightloop
PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    // Don't init to zero to allow to track warning about uninitialized data

#if _USE_LIGHT_FACING_NORMAL
    float3 N = ComputeViewFacingNormal(V, bsdfData.hairStrandDirectionWS);

    // Silence the imaginary square root compiler warning for the cosThetaParam.
    // The compiler seems to think that the dot product result between view vector and view facing normal produces
    // a result > 1, thus producing an imaginary square root for sqrt(1 - x).
    V = normalize(V);
#else
    float3 N = bsdfData.normalWS;
#endif

    preLightData.NdotV = dot(N, V);
    float clampedNdotV = ClampNdotV(preLightData.NdotV);

    float unused;

    // Both models share usage of GGX for now due to anisotropic LTC area light limitation, and Marschner invokes the BSDF directly for the environment evaluation.
    {
        // Note: For Kajiya hair we currently rely on a single cubemap sample instead of two, as in practice smoothness of both lobe aren't too far from each other.
        // and we take smoothness of the secondary lobe as it is often more rough (it is the colored one).
        preLightData.iblPerceptualRoughness = bsdfData.secondaryPerceptualRoughness;
        // TODO: adjust for Blinn-Phong here?
        GetPreIntegratedFGDGGXAndDisneyDiffuse(clampedNdotV, preLightData.iblPerceptualRoughness, bsdfData.fresnel0, preLightData.specularFGD, preLightData.diffuseFGD, unused);
        // We used lambert for hair for now
        // Note: this normalization term is wrong, correct one is (1/(Pi^2)).
        preLightData.diffuseFGD = 1.0;
    }

    // Stretch hack... Copy-pasted from GGX, ALU-optimized for hair.
    // float3 iblN = normalize(lerp(bsdfData.normalWS, N, bsdfData.anisotropy));
    float3 iblN = N;
    preLightData.iblR = reflect(-V, iblN);

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY))
    {
        preLightData.iblPerceptualRoughness *= saturate(1.2 - abs(bsdfData.anisotropy));
    }
    else
    {
        // Marschner utilizes the lowest environment mip and treats it as a directional light source to invoke the BSDF directly.
        preLightData.iblPerceptualRoughness = 1.0;
    }

    // Area light
    preLightData.ltcTransformDiffuse  = k_identity3x3;
    // IMPORTANT NOTE: For the time being, until we solve issues with Kajiya Kay anisotropy and LTC tables, hair will fall-back on GGX.
    // To be replaced with LTCLIGHTINGMODEL_KAJIYA_KAY_SPECULAR when that table is going to be valid.
    preLightData.ltcTransformSpecular = SampleLtcMatrix(bsdfData.perceptualRoughness, clampedNdotV, LTCLIGHTINGMODEL_GGX);

    // Construct a right-handed view-dependent orthogonal basis around the normal
    preLightData.orthoBasisViewNormal = GetOrthoBasisViewNormal(V, N, preLightData.NdotV);

    return preLightData;
}

//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

// This define allow to say that we implement a ModifyBakedDiffuseLighting function to be call in PostInitBuiltinData
#define MODIFY_BAKED_DIFFUSE_LIGHTING

void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, inout BuiltinData builtinData)
{
    // Add GI transmission contribution to bakeDiffuseLighting, we then drop backBakeDiffuseLighting (i.e it is not used anymore, this save VGPR)
    {
        // TODO: disabled until further notice (not clear how to handle occlusion).
        //builtinData.bakeDiffuseLighting += builtinData.backBakeDiffuseLighting * bsdfData.transmittance;
    }

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY))
    {
        // Premultiply (back) bake diffuse lighting information with diffuse pre-integration
        builtinData.bakeDiffuseLighting *= preLightData.diffuseFGD * bsdfData.diffuseColor;
    }
    else
    {
        // Marschner model has no diffuse component.
        // Thus we do not want GI to influence it (we handle this in the specular term with IBL lighting).
        builtinData.bakeDiffuseLighting = 0;
    }
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------

LightTransportData GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LightTransportData lightTransportData;

    // DiffuseColor for lightmapping
    lightTransportData.diffuseColor = bsdfData.diffuseColor;
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
    return true; // Due to either reflection or transmission being always active
}

struct HairAngle
{
    half sinThetaI;
    half sinThetaO;
    half cosThetaI;
    half cosThetaO;
    half cosThetaD;
    half thetaH;
    half phiI;
    half phiO;
    half phi;
    half cosPhi;
    half sinThetaT;
    half cosThetaT;
};

void GetHairAngleLocal(float3 wo, float3 wi, inout HairAngle angles)
{
    angles.sinThetaO = wo.x;
    angles.sinThetaI = wi.x;

    half thetaO = FastASin(angles.sinThetaO);
    half thetaI = FastASin(angles.sinThetaI);
    angles.thetaH = (thetaI + thetaO) * 0.5;

    angles.cosThetaD = cos((thetaO - thetaI) * 0.5);
    angles.cosThetaI = SafeSqrt(1 - Sq(angles.sinThetaI));
    angles.cosThetaO = SafeSqrt(1 - Sq(angles.sinThetaO));

    angles.phiI = FastAtan2(wi.z, wi.y);
    angles.phiO = FastAtan2(wo.z, wo.y);
    angles.phi  = angles.phiI - angles.phiO;

    angles.cosPhi = cos(angles.phi);

    angles.sinThetaT = angles.sinThetaO / 1.55;
    angles.cosThetaT = SafeSqrt(1 - Sq(angles.sinThetaT));
}

void GetHairAngleWorld(float3 V, float3 L, float3 T, inout HairAngle angles)
{
    // It might exceed the range [-1, 1], so explicitly clamp here to prevent nan output from FastASin.
    angles.sinThetaO = clamp(dot(T, V), -1.0, 1.0);
    angles.sinThetaI = clamp(dot(T, L), -1.0, 1.0);

    half thetaO = FastASin(angles.sinThetaO);
    half thetaI = FastASin(angles.sinThetaI);
    angles.thetaH = (thetaI + thetaO) * 0.5;

    angles.cosThetaD = cos((thetaO - thetaI) * 0.5);
    angles.cosThetaO = cos(thetaO);
    angles.cosThetaI = cos(thetaI);

    // Projection onto the normal plane, and since phi is the relative angle, we take the cosine in this projection.
    half3 VProj = V - angles.sinThetaO * T;
    half3 LProj = L - angles.sinThetaI * T;
    angles.cosPhi = dot(LProj, VProj) * rsqrt(dot(LProj, LProj) * dot(VProj, VProj) + 1e-5); // zero-div guard
    angles.phi = FastACos(angles.cosPhi);

    // Fixed for approximate human hair IOR
    angles.sinThetaT = angles.sinThetaO / 1.55;
    angles.cosThetaT = SafeSqrt(1 - Sq(angles.sinThetaT));
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Reference/HairReference.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/PreIntegratedAzimuthalScattering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/MultipleScattering/HairMultipleScattering.hlsl"

CBSDF EvaluateBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    half3 T = bsdfData.hairStrandDirectionWS;
    half3 N = bsdfData.normalWS;

#if _USE_LIGHT_FACING_NORMAL
    // The Kajiya-Kay model has a "built-in" transmission, and the 'NdotL' is always positive.
    half cosTL = dot(T, L);
    half sinTL = sqrt(saturate(1.0 - cosTL * cosTL));
    half NdotL = sinTL; // Corresponds to the cosine w.r.t. the light-facing normal
#else
    // Double-sided Lambert.
    half NdotL = dot(N, L);
#endif

    half NdotV = preLightData.NdotV;
    half clampedNdotV = ClampNdotV(NdotV);
    half clampedNdotL = saturate(NdotL);

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY))
    {
        float LdotV, NdotH, LdotH, invLenLV;
        GetBSDFAngle(V, L, NdotL, NdotV, LdotV, NdotH, LdotH, invLenLV);

        float3 t1 = ShiftTangent(T, N, bsdfData.specularShift);
        float3 t2 = ShiftTangent(T, N, bsdfData.secondarySpecularShift);

        float3 H = (L + V) * invLenLV;

        // Balancing energy between lobes, as well as between diffuse and specular is left to artists.
        float3 hairSpec1 = bsdfData.specularTint          * D_KajiyaKay(t1, H, bsdfData.specularExponent);
        float3 hairSpec2 = bsdfData.secondarySpecularTint * D_KajiyaKay(t2, H, bsdfData.secondarySpecularExponent);

        float3 F = F_Schlick(bsdfData.fresnel0, LdotH);

    #if _USE_LIGHT_FACING_NORMAL
        // See "Analytic Tangent Irradiance Environment Maps for Anisotropic Surfaces".
        cbsdf.diffR = rcp(PI * PI) * clampedNdotL;
        // Transmission is built into the model, and it's not exactly clear how to split it.
        cbsdf.diffT = 0;
    #else
        // Double-sided Lambert.
        cbsdf.diffR = Lambert() * clampedNdotL;
    #endif
        // Bypass the normal map...
        float geomNdotV = dot(bsdfData.geomNormalWS, V);

        // G = NdotL * NdotV.
        cbsdf.specR = 0.25 * F * (hairSpec1 + hairSpec2) * clampedNdotL * saturate(geomNdotV * FLT_MAX);

        // Yibing's and Morten's hybrid scatter model hack.
        float scatterFresnel1 = pow(saturate(-LdotV), 9.0) * pow(saturate(1.0 - geomNdotV * geomNdotV), 12.0);
        float scatterFresnel2 = saturate(PositivePow((1.0 - geomNdotV), 20.0));

        cbsdf.specT = scatterFresnel1 + bsdfData.rimTransmissionIntensity * scatterFresnel2;
    }

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER) ||
        HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER_CINEMATIC))
    {
        // Approximation of the three primary paths in a hair fiber (R, TT, TRT), with concepts from:
        // "Strand-Based Hair Rendering in Frostbite" (Tafuri 2019)
        // "A Practical and Controllable Hair and Fur Model for Production Path Tracing" (Chiang 2016)
        // "Physically Based Hair Shading in Unreal" (Karis 2016)
        // "An Energy-Conserving Hair Reflectance Model" (d'Eon 2011)
        // "Light Scattering from Human Hair Fibers" (Marschner 2003)

        // Reminder: All of these flags are known at compile time and the compiler will strip away the unused paths.

        // Retrieve angles via spherical coordinates in the hair shading space.
        HairAngle angles;
        ZERO_INITIALIZE(HairAngle, angles);
#if 0
        // Transform to the local frame for spherical coordinates,
        // Note that the strand direction is assumed to lie pointing down the +X axis.
        const float3x3 frame = GetLocalFrame(bsdfData.geomNormalWS, bsdfData.hairStrandDirectionWS);
        const float3 wo = mul(V, transpose(frame));
        const float3 wi = mul(L, transpose(frame));
        GetHairAngleLocal(wi, wo, angles);
#else
        GetHairAngleWorld(V, L, T, angles);
#endif

        const half3 alpha = half3(
            bsdfData.cuticleAngleR,
            bsdfData.cuticleAngleTT,
            bsdfData.cuticleAngleTRT
        );

        const half3 beta = half3(
            bsdfData.roughnessR,
            bsdfData.roughnessTT,
            bsdfData.roughnessTRT
        );

        // The index of refraction that can be used to analyze scattering in the normal plane (Bravais' Law).
        const half etaPrime = ModifiedRefractionIndex(angles.cosThetaD);

        // Reduced absorption coefficient.
        const half3 mu = bsdfData.absorption;

        // Various misc. terms reused between lobe evaluation.
        half3 F, Tr, S = 0;

        // Per-path attenuations.
        half3 A[3];

        half3 M;
        if (!HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER_CINEMATIC))
        {
            // For non-cinematic hair shading, use a cheaper gaussian for longitudinal scattering.
            M = Gaussian(angles.thetaH - alpha, beta) * bsdfData.distributionNormalizationFactor;
        }
        else
        {
            // Evaluate the energy conserving longitudinal scattering for all three paths.
            M = GetEnergyConservingLongitudinalScattering(angles.sinThetaI, angles.sinThetaO, bsdfData.perceptualRoughness);
        }

        // Fetch the preintegrated azimuthal distributions for each path
        const half3 N = GetRoughenedAzimuthalScatteringDistribution(angles.phi, angles.cosThetaD, bsdfData.perceptualRoughnessRadial);

        // Solve the first three lobes (R, TT, TRT).

        // R
        {
            // Attenuation for this path as proposed by d'Eon et al, replaced with a trig identity for cos half phi.
            A[0] = F_Schlick(bsdfData.fresnel0, sqrt(0.5 + 0.5 * dot(L, V)));
            S += M[0] * A[0] * N[0];
        }

        // TT
        if (!HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER_SKIP_TT))
        {
            // Attenutation (Simplified for H = 0)
            half cosGammaO = SafeSqrt(1 - Sq(HAIR_H_TT));
            half cosTheta  = angles.cosThetaO * cosGammaO;
            F = F_Schlick(bsdfData.fresnel0, cosTheta);

            half sinGammaT = HAIR_H_TT / etaPrime;
            half cosGammaT = SafeSqrt(1 - Sq(sinGammaT));
            Tr = exp(-mu * (2 * cosGammaT / angles.cosThetaT));

            A[1] = Sq(1 - F) * Tr;

            S += M[1] * A[1] * N[1];
        }
        else
            A[1] = 0; // Required to fully initialize.

        // TRT
        {
            // Attenutation (Simplified for H = √3/2)
            half cosGammaO = SafeSqrt(1 - Sq(HAIR_H_TRT));
            half cosTheta  = angles.cosThetaO * cosGammaO;
            F = F_Schlick(bsdfData.fresnel0, cosTheta);

            half sinGammaT = HAIR_H_TRT / etaPrime;
            half cosGammaT = SafeSqrt(1 - Sq(sinGammaT));
            Tr = exp(-mu * (2 * cosGammaT / angles.cosThetaT));

            A[2] = Sq(1 - F) * F * Sq(Tr);

            S += M[2] * A[2] * N[2];
        }

        // TODO: Residual TRRT+ Lobe. (accounts for ~15% energy otherwise lost by the first three lobes).
        // Transmission event is built into the model.
        // Some stubborn NaNs have cropped up due to the angle optimization, we suppress them here with a max for now.
        cbsdf.specR = max(S, 0);

        // Multiple Scattering
        cbsdf.specR = ComputeDualScattering(bsdfData, angles, DecodeHairStrandCount(L, bsdfData.strandCountProbe), cbsdf.specR);
    }

    return cbsdf;
}

//-----------------------------------------------------------------------------
// Surface shading (all light types) below
//-----------------------------------------------------------------------------

#if _MATERIAL_FEATURE_HAIR_MARSCHNER || _MATERIAL_FEATURE_HAIR_MARSCHNER_CINEMATIC
    // The Marschner model has no diffuse component. Thus we request the light loop to compile without APV / light probe evaluation.
    #define LIGHT_EVALUATION_SKIP_INDIRECT_DIFFUSE

    // Extra configuration for multiple scattering in the Marschner model.
    #if _MATERIAL_FEATURE_HAIR_MARSCHNER_CINEMATIC
        // Inform the light loop to compile with bsdf visibility information.
        #define LIGHT_EVALUATION_BSDF_HANDLES_VISIBILITY
        // Disable the contact shadow in case of multiple scattering.
        #define LIGHT_EVALUATION_NO_CONTACT_SHADOWS
    #else
        // Force contact shadows to skip the NdotL computation (allows to mitigate glowing heads for un-shadow mapped lights).
        #define LIGHT_EVALUATION_CONTACT_SHADOW_DISABLE_NDOTL
    #endif
#endif

// Hair used precomputed transmittance, no thick transmittance required
#define MATERIAL_INCLUDE_PRECOMPUTED_TRANSMISSION

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/SurfaceShading.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/MostRepresentativePoint.hlsl"

float3 RayPlaneIntersect(in float3 rayOrigin, in float3 rayDirection, in float3 planeOrigin, in float3 planeNormal)
{
    float dist = dot(planeNormal, planeOrigin - rayOrigin) / dot(planeNormal, rayDirection);
    return rayOrigin + rayDirection * dist;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Directional
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Directional(LightLoopContext lightLoopContext,
                                        float3 V, PositionInputs posInput, PreLightData preLightData,
                                        DirectionalLightData lightData, BSDFData bsdfData,
                                        BuiltinData builtinData)
{
    return ShadeSurface_Directional(lightLoopContext, posInput, builtinData,
                                    preLightData, lightData, bsdfData, V);
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    return ShadeSurface_Punctual(lightLoopContext, posInput, builtinData,
                                 preLightData, lightData, bsdfData, V);
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Line(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    // Translate the light s.t. the shaded point is at the origin of the coordinate system.
    float3 unL = lightData.positionRWS - posInput.positionWS;

    // These values could be precomputed on CPU to save VGPR or ALU.
    float halfLength = lightData.size.x * 0.5;

    float intensity = CapsuleWindowing(unL, lightData.right, halfLength,
                                       lightData.rangeAttenuationScale, lightData.rangeAttenuationBias);

    // Terminate if the shaded point is too far away.
    if (intensity > 0)
    {
        lightData.diffuseDimmer  *= intensity;
        lightData.specularDimmer *= intensity;

        float3 center = mul(preLightData.orthoBasisViewNormal, unL);
        float3 axis   = mul(preLightData.orthoBasisViewNormal, lightData.right);

        float ltcValue;

        // ----- 1. Evaluate the diffuse part -----

        ltcValue = I_ltc_line(transpose(preLightData.ltcTransformDiffuse), center, axis, halfLength);

        // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
        lighting.diffuse += ltcValue * lightData.diffuseDimmer;

        // Transmission Lobe
        {
            // Flip the surface while maintaining the view direction.
            float3x3 flipMatrix = float3x3(1,  0,  0,
                                           0, -1,  0,
                                           0,  0, -1);

            // Transform the vectors instead of transforming the basis.
            // Use the Lambertian approximation for performance reasons.
            // TODO: performing the evaluation twice is very inefficient!
            ltcValue = I_ltc_line(k_identity3x3, mul(flipMatrix, center), mul(flipMatrix, axis), halfLength);

            // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
            // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            lighting.diffuse += bsdfData.transmittance * (ltcValue * lightData.diffuseDimmer);
        }

        // ----- 2. Evaluate the specular part -----

        ltcValue = I_ltc_line(transpose(preLightData.ltcTransformSpecular), center, axis, halfLength);

        lighting.specular += ltcValue * lightData.specularDimmer;

        // We need to multiply by the magnitude of the integral of the BRDF
        // ref: http://advances.realtimerendering.com/s2016/s2016_ltc_fresnel.pdf
        lighting.diffuse  *= lightData.color * preLightData.diffuseFGD;
        lighting.specular *= lightData.color * preLightData.specularFGD;

        // ----- 3. Debug display -----

    #ifdef DEBUG_DISPLAY
        if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
        {
            ltcValue = I_ltc_line(k_identity3x3, center, axis, halfLength);

            // Only lighting, not BSDF
            lighting.diffuse  = lightData.color * (ltcValue * lightData.diffuseDimmer);
            // Apply area light on lambert then multiply by PI to cancel Lambert
            lighting.diffuse *= PI;
        }
    #endif
    }

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Rect_MRP - Approximation with Most Representative Point
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Rect_MRP(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    // Ref: Moving Frostbite to PBR (Appendix E)
    // Solve the area lighting using the Most Representative Point detection method.
    // This is a stop-gap solution until further research is given to LTC support for anisotropic BSDFs.
    // In the future, when strand-space shading is added, it might be doable to take a "structured sampling" approach.
    const float3 positionWS = posInput.positionWS;

#if SHADEROPTIONS_BARN_DOOR
    // Apply the barn door modification to the light data
    RectangularLightApplyBarnDoor(lightData, positionWS);
#endif

    float3 unL = lightData.positionRWS - positionWS;

    if (dot(lightData.forward, unL) < FLT_EPS)
    {
        const float halfWidth  = lightData.size.x * 0.5;
        const float halfHeight = lightData.size.y * 0.5;

        // Solid angle computation (brute force or approximate routine).
        // In our measurements the brute force is slightly more expensive but not by much. Additionally MRP is faster overall
        // than LTC so we accept the cost for the quality benefit.
    #if 1
        float4x3 lightVerts;
        lightVerts[0] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up * -halfHeight; // LL
        lightVerts[1] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up *  halfHeight; // UL
        lightVerts[2] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up *  halfHeight; // UR
        lightVerts[3] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up * -halfHeight; // LR

        const float solidAngle = SolidAngleRectangle(positionWS, lightVerts);
    #else
        const float solidAngle = SolidAngleRightPyramid(positionWS, lightData.positionRWS, halfWidth, halfHeight);
    #endif

        float3 L;

        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER))
        {
            // Let's choose a dominant direction with the same philosophy as we have for marschner IBL, using the the vector in the
            // tangent-camera plane that is orthogonal to the tangent. This provides well-behaved results (though not perfect)
            // with respect to the reference, instead of the classic reflection vector.
            const float3 dh = ComputeViewFacingNormal(V, bsdfData.hairStrandDirectionWS);

            // Intersect the dominant specular direction with the light plane.
            float3 ph = RayPlaneIntersect(positionWS, dh, lightData.positionRWS, lightData.forward);

            // Compute the closest position on the rectangle.
            ph = ClosestPointRectangle(ph, lightData.positionRWS, -lightData.right, lightData.up, halfWidth, halfHeight);

            // Determine the dominant hemisphere direction based on the camera-light plane angle.
            const float LdotV = max(dot(-lightData.forward, V), 0);

            // Construct the most representative direction.
            // We must consider multiple specular lobes, on the backward (R, TRT) and forward (TT) scattering hemisphere.
            // For the backward hemisphere we handle R and TRT similarly, and use the MRP result (based on the "fake" normal just like how we use it for IBL).
            // For the forward hemisphere we need to approximate harsher. We can get away with falling back to the light center and modifying the roughness.
            const float3 LBHemisphere = ph - positionWS;
            const float3 LFHemisphere = unL;
            L = SafeNormalize(lerp(LFHemisphere, LBHemisphere, LdotV));

            // Define a factor here to weight the solid angle contribution term to match the reference as close as possible for varying sizes.
            const float solidAngleFactor = 0.1;
            const float roughnessTTPrime = saturate(bsdfData.roughnessTT + solidAngleFactor * solidAngle);

            // Modify the roughness for the forward hemisphere scattering.
            bsdfData.roughnessTT = lerp(roughnessTTPrime, bsdfData.roughnessTT, LdotV);

            // Attempt at energy normalization for rectangular lights.
            // Similar in spirit to the "Specular D Normalization" heuristic (eq. 10 Real Shading in Unreal Engine 4)
            // Choose this solid angle-based heuristic to attempt to normalize the longitudinal distribution.
            const float3 alpha = float3(
                bsdfData.roughnessR,
                bsdfData.roughnessTT,
                bsdfData.roughnessTRT
            );

            const float3 alphaPrime = saturate(alpha + solidAngle);

            bsdfData.distributionNormalizationFactor = sqrt(alpha / alphaPrime);
        }
        else
        {
            // For Kajiya instead of MRP, fall back to the light center and modulate the roughnesses by the solid angle.
            // This isn't perfect for respecting the shape's orientation but generally good enough at widening the distribution for rects of varying size.
            L = normalize(lightData.positionRWS - positionWS);

            const float roughness1 = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
            const float roughness2 = PerceptualRoughnessToRoughness(bsdfData.secondaryPerceptualRoughness);

            // Again, define a factor to fudge the solid angle to closely match the reference.
            const float solidAngleFactor = 0.05;

            bsdfData.specularExponent          = RoughnessToBlinnPhongSpecularExponent(saturate(roughness1 + solidAngleFactor * solidAngle));
            bsdfData.secondarySpecularExponent = RoughnessToBlinnPhongSpecularExponent(saturate(roughness2 + solidAngleFactor * solidAngle));
        }

        // Configure a theoretically placed point light at the most important position contributing the area light irradiance.
        float3 lightColor = lightData.color * solidAngle;

        // Only apply cookie if there is one
        if ( lightData.cookieMode != COOKIEMODE_NONE )
        {
            // Compute cookie's mip count.
            const float cookieWidth = lightData.cookieScaleOffset.x * _CookieAtlasSize.x; // Guaranteed power of two.
            const float cookieMips  = round(log2(cookieWidth));

            // Normalize the solid angle against the hemisphere surface area to determine a weight for choosing the mip.
            const float cookieMip = cookieMips - (cookieMips * solidAngle * INV_TWO_PI);

            LightData lightDataFlipped = lightData;
            {
                // Flip the matrix since the cookie seems flipped incorrectly otherwise.
                lightDataFlipped.right = -lightDataFlipped.right;
            }

            // Sample the cookie as if it were a typical punctual light.
            lightColor *= EvaluateCookie_Punctual(lightLoopContext, lightDataFlipped, -unL, cookieMip).rgb;
        }

        // Raytracing shadow algorithm require to evaluate lighting without shadow, so it defined SKIP_RASTERIZED_AREA_SHADOWS
        // This is only present in Lit Material as it is the only one using the improved shadow algorithm.
    #ifndef SKIP_RASTERIZED_AREA_SHADOWS
        SHADOW_TYPE shadow = EvaluateShadow_RectArea(lightLoopContext, posInput, lightData, builtinData, bsdfData.normalWS, normalize(lightData.positionRWS), length(lightData.positionRWS));
        lightColor.rgb *= ComputeShadowColor(shadow, lightData.shadowTint, lightData.penumbraTint);
    #endif

        // Simulate a sphere/disk light with this hack.
        // Note that it is not correct with our precomputation of PartLambdaV
        // (means if we disable the optimization it will not have the
        // same result) but we don't care as it is a hack anyway.
        ClampRoughness(preLightData, bsdfData, lightData.minRoughness);

        lighting = ShadeSurface_Infinitesimal(preLightData, bsdfData, V, L, lightColor.rgb,
                                              lightData.diffuseDimmer, lightData.specularDimmer);
    }

    return lighting;
}

void IntegrateBSDF_AreaRef(float3 V, float3 positionWS,
                           PreLightData preLightData, LightData lightData, BSDFData bsdfData,
                           out float3 diffuseLighting, out float3 specularLighting,
                           uint sampleCount = _AREA_LIGHT_SAMPLE_COUNT)
{
    diffuseLighting  = float3(0.0, 0.0, 0.0);
    specularLighting = float3(0.0, 0.0, 0.0);

    for (uint i = 0; i < sampleCount; ++i)
    {
        float3 P = float3(0.0, 0.0, 0.0);   // Sample light point. Random point on the light shape in local space.
        float3 Ns = float3(0.0, 0.0, 0.0);  // Unit surface normal at P
        float lightPdf = 0.0;               // Pdf of the light sample

        float2 u = Hammersley2d(i, sampleCount);

        // Lights in Unity point backward.
        float4x4 localToWorld = float4x4(float4(lightData.right, 0.0), float4(lightData.up, 0.0), float4(-lightData.forward, 0.0), float4(lightData.positionRWS, 1.0));

        switch (lightData.lightType)
        {
            case GPULIGHTTYPE_RECTANGLE:
                SampleRectangle(u, localToWorld, lightData.size.x, lightData.size.y, lightPdf, P, Ns);
                break;
        }

        // Get distance
        float3 unL = P - positionWS;
        float sqrDist = dot(unL, unL);
        float3 L = normalize(unL);

        // Cosine of the angle between the light direction and the normal of the light's surface.
        float cosLNs = saturate(dot(-L, Ns));

        // We calculate area reference light with the area integral rather than the solid angle one.
        float NdotL = saturate(dot(bsdfData.normalWS, L));
        float illuminance = cosLNs / (sqrDist * lightPdf);

        if (illuminance > 0.0)
        {
            CBSDF cbsdf = EvaluateBSDF(V, L, preLightData, bsdfData);

            // Note: Again we do not accumulate diffuse here since marschner has no diffuse lobe.
            specularLighting += cbsdf.specR * lightData.color * illuminance * lightData.specularDimmer;
        }
    }

    specularLighting /= float(sampleCount);
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
        if (!HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER_CINEMATIC))
            return EvaluateBSDF_Rect_MRP(lightLoopContext, V, posInput, preLightData, lightData, bsdfData, builtinData);
        else
        {
            // Raytracing shadow algorithm require to evaluate lighting without shadow, so it defined SKIP_RASTERIZED_AREA_SHADOWS
            // This is only present in Lit Material as it is the only one using the improved shadow algorithm.
        #ifndef SKIP_RASTERIZED_AREA_SHADOWS
            SHADOW_TYPE shadow = EvaluateShadow_RectArea(lightLoopContext, posInput, lightData, builtinData, bsdfData.normalWS, normalize(lightData.positionRWS), length(lightData.positionRWS));

        #ifdef LIGHT_EVALUATION_BSDF_HANDLES_VISIBILITY
            if (AreaOccluderInRendererBounds(lightLoopContext, lightData, posInput))
            {
            #if _USE_SPLINE_VISIBILITY_FOR_MULTIPLE_SCATTERING
                // Use the shadow sample as a visibility term. Otherwise the volumetric data will be used to compute the self-shadow.
                bsdfData.visibility = shadow;
            #endif
            }
            else
        #endif
            {
                lightData.color.rgb *= ComputeShadowColor(shadow, lightData.shadowTint, lightData.penumbraTint);
            }
        #endif

            DirectLighting lighting;
            ZERO_INITIALIZE(DirectLighting, lighting);

            IntegrateBSDF_AreaRef(V, posInput.positionWS, preLightData, lightData, bsdfData, lighting.diffuse, lighting.specular);

            return lighting;
        }
    }
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_SSLighting for screen space lighting
// ----------------------------------------------------------------------------

IndirectLighting EvaluateBSDF_ScreenSpaceReflection(PositionInputs posInput,
                                                    PreLightData   preLightData,
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
    lighting.specularReflected = ssrLighting.rgb * preLightData.specularFGD;
    reflectionHierarchyWeight = ssrLighting.a;

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

    // TODO

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
IndirectLighting EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                    int influenceShapeType, int GPUImageBasedLightingType,
                                    inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    if (GPUImageBasedLightingType == GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION)
        return lighting;

    float3 envLighting = 0;
    float3 positionWS = posInput.positionWS;
    float weight = 1.0;

    float3 R = preLightData.iblR;

    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    // Note: Even though intersection distance is not used in cinematic sampling we still need to compute the correct weight for the hierarchy.
    float intersectionDistance = EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, R, weight);

    // For non-cinematic hair shading, fall back to more optimal environment evaluation routines.
    if (!HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER_CINEMATIC))
    {
        float4 preLD = SampleEnvWithDistanceBaseRoughness(lightLoopContext, posInput, lightData, R, preLightData.iblPerceptualRoughness, intersectionDistance);
        weight *= preLD.a; // Used by planar reflection to discard pixel

        if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY))
        {
            envLighting = preLightData.specularFGD * preLD.rgb;

            // We tint the HDRI with the secondary lob specular as it is more representatative of indirect lighting on hair.
            envLighting *= bsdfData.secondarySpecularTint;
        }
        else
        {
            // For now we approximate Marschner IBL as proposed by Brian Karis in "Physically Based Hair Shading in Unreal":
            // With slight variant in approach, instead of sampling a spherical harmonic of the environment, sample from the lowest mip.

            // Modify the roughness to approximate a larger area light source.
            bsdfData.roughnessR   = saturate(bsdfData.roughnessR   + 0.1);
            bsdfData.roughnessTRT = saturate(bsdfData.roughnessTRT + 0.1);

            // Skip TT for the environment sample (compiler will optimizate for these two different BSDF versions)
            bsdfData.materialFeatures |= MATERIALFEATUREFLAGS_HAIR_MARSCHNER_SKIP_TT;

            // This sample is treated as a directional light source and we evaluate the BSDF with it directly.
            CBSDF cbsdf = EvaluateBSDF(V, bsdfData.normalWS, preLightData, bsdfData);

            envLighting = cbsdf.specR * preLD.rgb * PI;
        }
    }
    else
    {
        // Transform to the local frame for spherical coordinates,
        // Note that the strand direction is assumed to lie pointing down the X axis, as this is expected by the BSDF.
        half3x3 localToWorld = GetLocalFrame(bsdfData.normalWS, bsdfData.hairStrandDirectionWS);

        // Rely on the spherical harmonic for visibility.
        bsdfData.visibility = -1;

        // TODO: This sample count is a good opportunity for varying quality levels.
        const int      sampleCount = _ENVIRONMENT_LIGHT_SAMPLE_COUNT;
        const half  rcpSampleCount = rcp(sampleCount);

#ifdef LINE_RENDERING_OFFSCREEN_SHADING
        // For HQ Line rendering, unfortunately since the shading occurs in object space we can't really pull off jittered samples with TAA.
        // The penalty is that we have to pay the cost of more samples to stabilize the result. In the future, we should explore
        // re-coupling the shading rate to the visibility of strands to benefit from TAA / Upscalers which drastically reduce the shading cost
        // that normally comes with coupled shading/visibility. For the moment, this incurred extra cost can be offset with the shading atlas
        // history which can allow artist control over the percentage of shading points to compute in the current frame.
        half2 sampleJitter = InitRandom(V.xy * 0.5 + 0.5);
#else
        // Rely on TAA to get some extra samples.
        half sampleJitterAngle = InterleavedGradientNoise(posInput.positionSS.xy, _TaaFrameInfo.z) * 2.0 * PI;
        half2 sampleJitter = float2(sin(sampleJitterAngle), cos(sampleJitterAngle));
#endif

        UNITY_LOOP
        for (uint i = 0; i < (uint)sampleCount; ++i)
        {
            float2 u    = Hammersley2d(i, sampleCount);
            u           = frac(u + sampleJitter);

            half3 localL = SampleSphereUniform(u.x, u.y);
            half3 L = mul(localL, localToWorld);

            half4 val = SampleEnv(lightLoopContext, lightData.envIndex, L, 4, lightData.rangeCompressionFactorCompensation, 0.5);

            // Invoke the fiber scattering function.
            CBSDF cbsdf = EvaluateBSDF(V, L, preLightData, bsdfData);

            envLighting += (cbsdf.specR * abs(localL.z) * rcpSampleCount * val.rgb) / INV_FOUR_PI;
        }
    }

    UpdateLightingHierarchyWeights(hierarchyWeight, weight);
    envLighting *= weight * lightData.multiplier;
    lighting.specularReflected = envLighting;

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
    GetScreenSpaceAmbientOcclusionMultibounce(posInput.positionSS, preLightData.NdotV, bsdfData.perceptualRoughness, bsdfData.ambientOcclusion, bsdfData.specularOcclusion, bsdfData.diffuseColor, bsdfData.fresnel0, aoFactor);
    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);

    // Apply the albedo to the direct diffuse lighting (only once). The indirect (baked)
    // diffuse lighting has already multiply the albedo in ModifyBakedDiffuseLighting().
    lightLoopOutput.diffuseLighting = bsdfData.diffuseColor * lighting.direct.diffuse + builtinData.bakeDiffuseLighting + builtinData.emissiveColor;
    lightLoopOutput.specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, lightLoopOutput);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
