//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in Hair.cs which generates Hair.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/Hair.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SubsurfaceScattering/SubsurfaceScattering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

//-----------------------------------------------------------------------------
// Texture and constant buffer declaration
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"

#define DEFAULT_HAIR_SPECULAR_VALUE 0.0465 // Hair is IOR 1.55

// These H offset values (-1, 1) are used to approximate the integral for far-field azimuthal scattering.
// For TT, the dominant contribution comes from light transmitted straight through the fiber (thus 0).
// For TRT, a similar observation is made and v3/2 is used to approximate.
#define HAIR_H_TT  0.0
#define HAIR_H_TRT 0.866

// #define HAIR_DISPLAY_REFERENCE_BSDF
// #define HAIR_DISPLAY_REFERENCE_IBL

// An extra material feature flag we utilize to compile two different versions of BSDF evaluation (one with transmission lobe
// for analytic lights, one without transmission lobe for environment light).
#define MATERIALFEATUREFLAGS_HAIR_MARSCHNER_SKIP_TT (1 << 16)

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

// Ref: "Light Scattering from Human Hair Fibers"
// Longitudinal scattering as modeled by a normal distribution.
// To be used as an approximation to d'Eon et al's Energy Conserving Longitudinal Scattering Function.
// TODO: Move me to BSDF.hlsl
real D_LongitudinalScatteringGaussian(real theta, real beta)
{
    real v = theta / beta;

    const real sqrtTwoPi = 2.50662827463100050241;
    return rcp(beta * sqrtTwoPi) * exp(-0.5 * v * v);
}

float ModifiedRefractionIndex(float cosThetaD)
{
    // Original derivation of modified refraction index for arbitrary IOR.
    // float sinThetaD = sqrt(1 - Sq(cosThetaD));
    // return sqrt(Sq(eta) - Sq(sinThetaD)) / cosThetaD;

    // Karis approximation for the modified refraction index for human hair (1.55)
    return 1.19 / cosThetaD + (0.36 * cosThetaD);
}

// Ref: A Practical and Controllable Hair and Fur Model for Production Path Tracing
float3 DiffuseColorToAbsorption(float3 diffuseColor, float azimuthalRoughness)
{
    float beta  = azimuthalRoughness;
    float beta2 = beta  * beta;
    float beta3 = beta2 * beta;
    float beta4 = beta3 * beta;
    float beta5 = beta4 * beta;

    // Least squares fit of an inverse mapping between scattering parameters and scattering albedo.
    float denom = 5.969 - (0.215 * beta) + (2.532 * beta2) - (10.73 * beta3) + (5.574 * beta4) + (0.245 * beta5);

    float3 t = log(diffuseColor) / denom;
    return t * t;
}

float4 GetDiffuseOrDefaultColor(BSDFData bsdfData, float replace)
{
    return float4(bsdfData.diffuseColor, 0.0);
}

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
#if _USE_LIGHT_FACING_NORMAL
    // TODO: should probably bias towards the light for splines...
    return bsdfData.geomNormalWS;
#else
    return bsdfData.geomNormalWS;
#endif
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
    return clamp(2 * rcp(roughness * roughness) - 2, FLT_EPS, rcp(FLT_EPS));
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
    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);

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
    if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER))
    {
        // Note: Light Path Length is computed per-light.

        // Cuticle Angle
        const float cuticleAngle = radians(surfaceData.cuticleAngle);
        bsdfData.cuticleAngleR   = -cuticleAngle;
        bsdfData.cuticleAngleTT  =  cuticleAngle * 0.5;
        bsdfData.cuticleAngleTRT =  cuticleAngle * 3.0 * 0.5;

        // Longitudinal Roughness
        const float roughnessL = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
        bsdfData.roughnessR   = roughnessL;
        bsdfData.roughnessTT  = roughnessL * 0.5;
        bsdfData.roughnessTRT = roughnessL * 2.0;

        // Azimuthal Roughness
    #if _USE_ROUGHENED_AZIMUTHAL_SCATTERING
        bsdfData.roughnessRadial = PerceptualSmoothnessToRoughness(surfaceData.perceptualRadialSmoothness);
    #else
        // Need to provide some sensible default in case of no roughened azimuthal scattering, since currently our
        // absorption is dependent on it.
        bsdfData.roughnessRadial = 0.5;
    #endif

        // Absorption
        bsdfData.absorption = DiffuseColorToAbsorption(surfaceData.diffuseColor, bsdfData.roughnessRadial);
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

    // Scattering
#if _USE_DENSITY_VOLUME_SCATTERING

#endif

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
    preLightData.iblPerceptualRoughness *= saturate(1.2 - abs(bsdfData.anisotropy));

    // Area light
    // UVs for sampling the LUTs
    // We use V = sqrt( 1 - cos(theta) ) for parametrization which is kind of linear and only requires a single sqrt() instead of an expensive acos()
    float cosThetaParam = sqrt(1 - clampedNdotV); // For Area light - UVs for sampling the LUTs
    float2 uv = Remap01ToHalfTexelCoord(float2(bsdfData.perceptualRoughness, cosThetaParam), LTC_LUT_SIZE);

    // Note we load the matrix transpose (avoid to have to transpose it in shader)
#if _USE_LIGHT_FACING_NORMAL
    // Get the inverse LTC matrix for Disney Diffuse
    preLightData.ltcTransformDiffuse      = 0.0;
    preLightData.ltcTransformDiffuse._m22 = 1.0;
    preLightData.ltcTransformDiffuse._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTCLIGHTINGMODEL_KAJIYA_KAY_DIFFUSE, 0);
#else
    preLightData.ltcTransformDiffuse = k_identity3x3;
#endif

    // Get the inverse LTC matrix for GGX
    // Note we load the matrix transpose (avoid to have to transpose it in shader)
    preLightData.ltcTransformSpecular      = 0.0;
    preLightData.ltcTransformSpecular._m22 = 1.0;
    // IMPORTANT NOTE: For the time being, until we solve issues with Kajiya Kay anisotropy and LTC tables, hair will fall-back on GGX.
    // To be replaced with LTCLIGHTINGMODEL_KAJIYA_KAY_SPECULAR when that table is going to be valid.
    preLightData.ltcTransformSpecular._m00_m02_m11_m20 = SAMPLE_TEXTURE2D_ARRAY_LOD(_LtcData, s_linear_clamp_sampler, uv, LTCLIGHTINGMODEL_GGX, 0);

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

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER))
    {
        // See: [NOTE-MARSCHNER-IBL]
        builtinData.bakeDiffuseLighting *= PI;
    }
    else
    {
        // Premultiply (back) bake diffuse lighting information with diffuse pre-integration
        builtinData.bakeDiffuseLighting *= preLightData.diffuseFGD * bsdfData.diffuseColor;
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

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/HairReference.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/PreIntegratedAzimuthalScattering.hlsl"

bool IsNonZeroBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    return true; // Due to either reflection or transmission being always active
}

void GetMarschnerAngle(float3 T, float3 V, float3 L,
                       out float thetaH, out float cosThetaD, out float cosPhi)
{
    // Optimized math for spherical coordinate angle derivation.
    // Ref: Light Scattering from Human Hair Fibers
    float sinThetaI = dot(T, L);
    float sinThetaR = dot(T, V);

    float thetaI = FastASin(sinThetaI);
    float thetaR = FastASin(sinThetaR);
    thetaH = (thetaI + thetaR) * 0.5;

    cosThetaD = cos((thetaR - thetaI) * 0.5);

    // Ref: Hair Animation and Rendering in the Nalu Demo
    // Projection onto the normal plane, and since phi is the relative angle, we take the cosine in this projection.
    float3 LProj = L - sinThetaI * T;
    float3 VProj = V - sinThetaR * T;
    cosPhi = dot(LProj, VProj) * rsqrt(dot(LProj, LProj) * dot(VProj, VProj));
}

CBSDF EvaluateBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    float3 T = bsdfData.hairStrandDirectionWS;
    float3 N = bsdfData.normalWS;

#if _USE_LIGHT_FACING_NORMAL
    // The Kajiya-Kay model has a "built-in" transmission, and the 'NdotL' is always positive.
    float cosTL = dot(T, L);
    float sinTL = sqrt(saturate(1.0 - cosTL * cosTL));
    float NdotL = sinTL; // Corresponds to the cosine w.r.t. the light-facing normal
#else
    // Double-sided Lambert.
    float NdotL = dot(N, L);
#endif

    float NdotV = preLightData.NdotV;
    float clampedNdotV = ClampNdotV(NdotV);
    float clampedNdotL = saturate(NdotL);

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

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER))
    {
#ifdef HAIR_DISPLAY_REFERENCE_BSDF
        cbsdf = EvaluateMarschnerReference(V, L, bsdfData);
#else
        // Approximation of the three primary paths in a hair fiber (R, TT, TRT), with concepts from:
        // "Strand-Based Hair Rendering in Frostbite" (Tafuri 2019)
        // "A Practical and Controllable Hair and Fur Model for Production Path Tracing" (Chiang 2016)
        // "Physically Based Hair Shading in Unreal" (Karis 2016)
        // "An Energy-Conserving Hair Reflectance Model" (d'Eon 2011)
        // "Light Scattering from Human Hair Fibers" (Marschner 2003)

        // Retrieve angles via spherical coordinates in the hair shading space.
        float thetaH, cosThetaD, cosPhi;
        GetMarschnerAngle(T, V, L, thetaH, cosThetaD, cosPhi);

        // The index of refraction that can be used to analyze scattering in the normal plane (Bravais' Law).
        float etaPrime = ModifiedRefractionIndex(cosThetaD);

        // Reduced absorption coefficient.
        // Note: Technically should divide absorption by thetaT here, but comparing to reference
        // proved a negligible difference and thus not worth the extra computation cost.
        float3 mu = bsdfData.absorption;

        // Various terms reused between lobe evaluation.
        float  M, D       = 0;
        float3 A, F, T, S = 0;

        // Solve the first three lobes (R, TT, TRT).

        // R
        {
            M = D_LongitudinalScatteringGaussian(thetaH - bsdfData.cuticleAngleR, bsdfData.roughnessR);

            // Distribution and attenuation for this path as proposed by d'Eon et al, replaced with a trig identity for cos half phi.
            D = 0.25 * sqrt(0.5 + 0.5 * cosPhi);
            A = F_Schlick(bsdfData.fresnel0, sqrt(0.5 + 0.5 * dot(L, V)));

            S += M * A * D;
        }

        // TT
        if (!HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER_SKIP_TT))
        {
            M = D_LongitudinalScatteringGaussian(thetaH - bsdfData.cuticleAngleTT, bsdfData.roughnessTT);

        #if _USE_ROUGHENED_AZIMUTHAL_SCATTERING
            // This lobe's distribution is determined by sampling coefficients from a pre-integrated LUT of the distribution and evaluating a gaussian.
            D = GetPreIntegratedAzimuthalScatteringTransmissionDistribution(bsdfData.roughnessRadial, cosThetaD, cosPhi);
        #else
            // Karis' approximation of Pixar's logisitic with scale of √0.35
            D = exp(-3.65 * cosPhi - 3.98);
        #endif

            // Attenutation (Simplified for H = 0)
            // Note: H = ~0.55 seems to be more suitable for this lobe's attenuation, but H = 0 allows us to simplify more of the math at the cost of slightly more error.
            // Plot: https://www.desmos.com/calculator/pum8esu6ot
            F = F_Schlick(bsdfData.fresnel0, cosThetaD);
            T = exp(-4 * mu);
            A = Sq(1 - F) * T;

            S += M * A * D;
        }

        // TRT
        {
            M = D_LongitudinalScatteringGaussian(thetaH - bsdfData.cuticleAngleTRT, bsdfData.roughnessTRT);

            // TODO: Move this out of the BSDF evaluation.
        #if _USE_ROUGHENED_AZIMUTHAL_SCATTERING
            // This lobe's distribution is determined by Frostbite's improvement over Karis' TRT approximation (maintaining Azimuthal Roughness).
            float scaleFactor = saturate(1.5 * (1 - bsdfData.roughnessRadial));
        #else
            float scaleFactor = 1;
        #endif
            D = scaleFactor * exp(scaleFactor * (17.0 * cosPhi - 16.78));

            // Attenutation (Simplified for H = √3/2)
            F = F_Schlick(bsdfData.fresnel0, cosThetaD * 0.5);
            T = exp(-2 * mu * (1 + cos(2 * FastASin(HAIR_H_TRT / etaPrime))));
            A = Sq(1 - F) * F * Sq(T);

            S += M * A * D;
        }

        // Transmission event is built into the model.
        // Some stubborn NaNs have cropped up due to the angle optimization, we suppress them here with a max for now.
        cbsdf.specR = max(S, 0);
    #endif

        // Multiple Scattering
    #if _USE_DENSITY_VOLUME_SCATTERING
        cbsdf.diffR = 0;
    #else
    #if _USE_LIGHT_FACING_NORMAL
        // See "Analytic Tangent Irradiance Environment Maps for Anisotropic Surfaces".
        cbsdf.diffR = rcp(PI * PI) * clampedNdotL;
        // Transmission is built into the model, and it's not exactly clear how to split it.
        cbsdf.diffT = 0;
    #else
        // Double-sided Lambert.
        cbsdf.diffR = Lambert() * clampedNdotL;
    #endif // _USE_LIGHT_FACING_NORMAL
    #endif // _USE_DENSITY_VOLUME_SCATTERING
    }

    return cbsdf;
}

//-----------------------------------------------------------------------------
// Surface shading (all light types) below
//-----------------------------------------------------------------------------

// Hair used precomputed transmittance, no thick transmittance required
#define MATERIAL_INCLUDE_PRECOMPUTED_TRANSMISSION
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialEvaluation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/SurfaceShading.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Hair/HairScattering.hlsl"

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
// EvaluateBSDF_Line
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Line(   LightLoopContext lightLoopContext,
                                    float3 V, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    // TODO

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Rect
//-----------------------------------------------------------------------------

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

    float3 unL = lightData.positionRWS - positionWS;

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

        // Terminate if the shaded point is too far away.
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
            lightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal));

            float3 ltcValue;

            // Evaluate the diffuse part
            // Polygon irradiance in the transformed configuration.
            float4x3 LD = mul(lightVerts, preLightData.ltcTransformDiffuse);
            float3 formFactorD;
#ifdef APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT
            formFactorD = PolygonFormFactor(LD);
            ltcValue = PolygonIrradianceFromVectorFormFactor(formFactorD);
#else
            ltcValue = PolygonIrradiance(LD, formFactorD);
#endif
            ltcValue *= lightData.diffuseDimmer;

            // Only apply cookie if there is one
            if ( lightData.cookieMode != COOKIEMODE_NONE )
            {
#ifndef APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT
                formFactorD = PolygonFormFactor(LD);
#endif
                ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LD, formFactorD);
            }

            // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
            // See comment for specular magnitude, it apply to diffuse as well
            lighting.diffuse = preLightData.diffuseFGD * ltcValue;

            // Transmission Lobe
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
                float4x3 LTD = mul(lightVerts, ltcTransform);
                ltcValue  = PolygonIrradiance(LTD);
                ltcValue *= lightData.diffuseDimmer;

                // Only apply cookie if there is one
                if ( lightData.cookieMode != COOKIEMODE_NONE )
                {
                    // Compute the cookie data for the transmission diffuse term
                    float3 formFactorTD = PolygonFormFactor(LTD);
                    ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LTD, formFactorTD);
                }

                // We use diffuse lighting for accumulation since it is going to be blurred during the SSS pass.
                // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
                lighting.diffuse += bsdfData.transmittance * ltcValue;
            }

            // Evaluate the specular part
            // Polygon irradiance in the transformed configuration.
            float4x3 LS = mul(lightVerts, preLightData.ltcTransformSpecular);
            float3 formFactorS;
#ifdef APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT
            formFactorS = PolygonFormFactor(LS);
            ltcValue = PolygonIrradianceFromVectorFormFactor(formFactorS);
#else
            ltcValue = PolygonIrradiance(LS);
#endif
            ltcValue *= lightData.specularDimmer;

            // Only apply cookie if there is one
            if ( lightData.cookieMode != COOKIEMODE_NONE)
            {
                // Compute the cookie data for the specular term
#ifndef APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT
                formFactorS =  PolygonFormFactor(LS);
#endif
                ltcValue *= SampleAreaLightCookie(lightData.cookieScaleOffset, LS, formFactorS);
            }

            // We need to multiply by the magnitude of the integral of the BRDF
            // ref: http://advances.realtimerendering.com/s2016/s2016_ltc_fresnel.pdf
            // This value is what we store in specularFGD, so reuse it
            lighting.specular += preLightData.specularFGD * ltcValue;

            // Raytracing shadow algorithm require to evaluate lighting without shadow, so it defined SKIP_RASTERIZED_AREA_SHADOWS
            // This is only present in Lit Material as it is the only one using the improved shadow algorithm.
        #ifndef SKIP_RASTERIZED_AREA_SHADOWS
            SHADOW_TYPE shadow = EvaluateShadow_RectArea(lightLoopContext, posInput, lightData, builtinData, bsdfData.normalWS, normalize(lightData.positionRWS), length(lightData.positionRWS));
            lightData.color.rgb *= ComputeShadowColor(shadow, lightData.shadowTint, lightData.penumbraTint);
        #endif

            // Save ALU by applying 'lightData.color' only once.
            lighting.diffuse *= lightData.color;
            lighting.specular *= lightData.color;

        #ifdef DEBUG_DISPLAY
            if (_DebugLightingMode == DEBUGLIGHTINGMODE_LUX_METER)
            {
                // Only lighting, not BSDF
                // Apply area light on lambert then multiply by PI to cancel Lambert
                lighting.diffuse = PolygonIrradiance(mul(lightVerts, k_identity3x3));
                lighting.diffuse *= PI * lightData.diffuseDimmer;
            }
        #endif
        }
    }
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

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER))
    {
        // See: [NOTE-MARSCHNER-IBL]
        return lighting;
    }

    float3 envLighting;
    float3 positionWS = posInput.positionWS;
    float weight = 1.0;

    float3 R = preLightData.iblR;

    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    float intersectionDistance = EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, R, weight);

    float4 preLD = SampleEnvWithDistanceBaseRoughness(lightLoopContext, posInput, lightData, R, preLightData.iblPerceptualRoughness, intersectionDistance);
    weight *= preLD.a; // Used by planar reflection to discard pixel

    envLighting = preLightData.specularFGD * preLD.rgb;

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY))
    {
        // We tint the HDRI with the secondary lob specular as it is more representatative of indirect lighting on hair.
        envLighting *= bsdfData.secondarySpecularTint;
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

    float3 indirectDiffuse  = builtinData.bakeDiffuseLighting;
    float3 indirectSpecular = lighting.indirect.specularReflected;

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER))
    {
        // [NOTE-MARSCHNER-IBL]
        // For now we approximate Marschner IBL as proposed by Brian Karis in "Physically Based Hair Shading in Unreal":

        // Modify the roughness
        bsdfData.roughnessR   = saturate(bsdfData.roughnessR   + 0.2);
        bsdfData.roughnessTRT = saturate(bsdfData.roughnessTRT + 0.2);

        // Skip TT for the environment sample (compiler will optimizate for these two different BSDF versions)
        bsdfData.materialFeatures |= MATERIALFEATUREFLAGS_HAIR_MARSCHNER_SKIP_TT;

        // This sample is treated as a directional light source and we evaluate the BSDF with it directly.
        CBSDF cbsdf = EvaluateBSDF(V, bsdfData.normalWS, preLightData, bsdfData);

        // Repurpose the spherical harmonic sample of the environment lighting (sampled with the modified normal).
        indirectDiffuse  = cbsdf.diffR * builtinData.bakeDiffuseLighting * bsdfData.diffuseColor;
        indirectSpecular = cbsdf.specR * builtinData.bakeDiffuseLighting;
    }

    // Apply the albedo to the direct diffuse lighting (only once). The indirect (baked)
    // diffuse lighting has already multiply the albedo in ModifyBakedDiffuseLighting().
    lightLoopOutput.diffuseLighting = bsdfData.diffuseColor * lighting.direct.diffuse + indirectDiffuse + builtinData.emissiveColor;
    lightLoopOutput.specularLighting = lighting.direct.specular + indirectSpecular;

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, lightLoopOutput);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
