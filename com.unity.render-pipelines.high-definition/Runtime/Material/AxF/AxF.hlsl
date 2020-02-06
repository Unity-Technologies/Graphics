//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in AxF.cs which generates AxF.cs.hlsl
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxF.cs.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

// Declare the BSDF specific FGD property and its fetching function
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFPreIntegratedFGD.hlsl"

// Add support for LTC Area Lights
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/AxF/AxFLTCAreaLight/AxFLTCAreaLight.hlsl"

//-----------------------------------------------------------------------------
//
// Hardcoded config
//

#define FORCE_DISABLE_LIGHT_TYPE_DIMMERS

// Comment to disable the BRDFColor table clamping (CARPAINT2 specific)
#define AUTO_PATCH_FOR_INCOMPLETE_BRDF_COLOR_TABLE // This requires importer version >= 0.1.5-preview and manually setting the diagonal clamping enable + scalings to offset the diagonal

// Uncomment to always consider carpaints as having a clearcoat:
//#define FORCE_CAR_PAINT_HAS_CLEARCOAT

//-----------------------------------------------------------------------------
// DEBUG
//#define DEBUG_HIDE_COAT
//-----------------------------------------------------------------------------

#define NdotVMinCosSpread 0.0001 // ie this is the value used by ClampNdotV 

#ifdef CLEAR_COAT_PERCEPTUAL_ROUGHNESS
#undef CLEAR_COAT_PERCEPTUAL_ROUGHNESS
#endif
#ifdef CLEAR_COAT_PERCEPTUAL_SMOOTHNESS
#undef CLEAR_COAT_PERCEPTUAL_SMOOTHNESS
#endif
#ifdef CLEAR_COAT_ROUGHNESS
#undef CLEAR_COAT_ROUGHNESS
#endif
#define CLEAR_COAT_ROUGHNESS 0.0 // By default we set it to match the lack of dirac light response in the X-Rite Pantora viewer.
#define CLEAR_COAT_PERCEPTUAL_SMOOTHNESS RoughnessToPerceptualSmoothness(CLEAR_COAT_ROUGHNESS)
#define CLEAR_COAT_PERCEPTUAL_ROUGHNESS RoughnessToPerceptualRoughness(CLEAR_COAT_ROUGHNESS)

#define FLAKES_JUST_BTF
// To evaluate just the BTF for split-sum lights (environments and LTC), define the above.
//
// Normally, we would have to create an FGD texture for the flake BTF, but since we assume the BSDF to be very sparse
// and "sparkly", we do a bit the same as we do with the clearcoat, approximating the integral of the almost-dirac FGD with
// the Fresnel term evaluation itself, here evaluating the flakes BTF in place of having an FGD. We might want to add a
// general surface orientation effect by dimming with an additional angle-dependent pre-integrated FGD term, which we
// calculate with the GGX pre-integrated FGD with FLAKES_ROUGHNESS and FLAKES_F0.
//
// Also when FLAKES_JUST_BTF are defined, for the LTC transform, we will use the same LTC transform for the flakes as for
// the coat, as both are considered having very low roughness
#define FLAKES_ROUGHNESS 0.03
#define FLAKES_PERCEPTUAL_ROUGHNESS RoughnessToPerceptualRoughness(FLAKES_ROUGHNESS)
#define FLAKES_F0 0.95 // at 0.95 and with the angular compression of the clearcoat, variations due to this additional flakesFGD will be almost invisible though...

//#define FLAKES_IOR Fresnel0ToIor(FLAKES_F0) // f0 = 0.95, ior = ~38, makes no sense for dielectric, but is to fake metal with dielectric Fresnel equations

#ifndef FLAKES_JUST_BTF
#    define IFNOT_FLAKES_JUST_BTF(a) (a)
#    define IF_FLAKES_JUST_BTF(a)
#else
#    define IFNOT_FLAKES_JUST_BTF(a)
#    define IF_FLAKES_JUST_BTF(a) (a)
#endif

// Define this to sample the environment maps/LTC samples for each lobe, instead of a single sample with an average lobe
#define USE_COOK_TORRANCE_MULTI_LOBES   1
#define MAX_CT_LOBE_COUNT 3
//#define CARPAINT2_LOBE_COUNT min(_CarPaint2_LobeCount,MAX_CT_LOBE_COUNT)
#define CARPAINT2_LOBE_COUNT MAX_CT_LOBE_COUNT

//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

// AxF splits the chromaticity and f0 from the usual "SpecularColor" convention
// to just be a chromatic f0.
// CARPAINT2 has a different way to handle colors and must be accounted for too.
// Base refers to the "base layer", ie not the coat if present.
float3 GetColorBaseFresnelF0(BSDFData bsdfData)
{
    return bsdfData.fresnelF0.r * bsdfData.specularColor;
}

float3 GetColorBaseDiffuse(BSDFData bsdfData)
{
    float3 diffuseColor = 0;

#if defined(_AXF_BRDF_TYPE_SVBRDF)
    diffuseColor = bsdfData.diffuseColor;
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
    diffuseColor = bsdfData.diffuseColor * bsdfData.specularColor; // for carpaint, specularColor will be set from BRDFColor table.
#endif

    return diffuseColor;
}

float4 GetDiffuseOrDefaultColor(BSDFData bsdfData, float replace)
{
    float3 fresnel0 = GetColorBaseFresnelF0(bsdfData);
    float3 diffuseColor = GetColorBaseDiffuse(bsdfData);

    // Use frensel0 as mettalic weight. all value below 0.2 (ior of diamond) are dielectric
    // all value above 0.45 are metal, in between we lerp.
    float weight = saturate((Max3(fresnel0.r, fresnel0.g, fresnel0.b) - 0.2) / (0.45 - 0.2));

    return float4(lerp(diffuseColor, fresnel0, weight * replace), weight);
}

float3 GetNormalForShadowBias(BSDFData bsdfData)
{
    return bsdfData.geomNormalWS;
}

float GetAmbientOcclusionForMicroShadowing(BSDFData bsdfData)
{
    return 1.0;
}

float GetPreIntegratedFGDCookTorranceSampleMutiplier()
{
    float ret = 4.0;
    return ret;
    // Note: We multiply by 4 since the documentation uses a Cook-Torrance BSDF formula without the /4 from the dH/dV Jacobian,
    // and since we assume the lobe coefficients are fitted, we assume the effect of this factor to be in those.
    // However, our pre-integrated FGD uses the proper D() importance sampling method and weight, so that the D() is effectively
    // cancelled out when integrating FGD, whichever D() you choose to do importance sampling, along with the Jacobian of the
    // BSDF (FGD integrand) with the Jacobian from doing importance sampling in H while integrating over L.
    // We thus restitute the * 4 here.
    // The other term is mostly a tweak to enable a desired match eg VRED
}

#define DIFFUSE_INDIRECT_FUDGE_FACTOR (1.0)
#define ENVIRONMENT_LD_FUDGE_FACTOR (1.0)
#define LTC_L_FUDGE_FACTOR (1.0)
#define SSR_L_FUDGE_FACTOR (1.0)

float GetDiffuseIndirectDimmer()
{
    float ret = 1.0;
#ifndef FORCE_DISABLE_LIGHT_TYPE_DIMMERS
    ret = DIFFUSE_INDIRECT_FUDGE_FACTOR;
#endif
    return ret;
}

float GetSpecularIndirectDimmer()
{
    float ret = 1.0;
#ifndef FORCE_DISABLE_LIGHT_TYPE_DIMMERS
    ret = ENVIRONMENT_LD_FUDGE_FACTOR;
#endif
    return ret;
}

float GetLTCAreaLightDimmer()
{
    float ret = 1.0;
#ifndef FORCE_DISABLE_LIGHT_TYPE_DIMMERS
    ret = LTC_L_FUDGE_FACTOR;
#endif
    return ret;
}

float GetSSRDimmer()
{
    float ret = 1.0;
#ifndef FORCE_DISABLE_LIGHT_TYPE_DIMMERS
    ret = SSR_L_FUDGE_FACTOR;
#endif
    return ret;
}

bool IsDebugHideCoat()
{
    bool ret = false;
#ifdef DEBUG_HIDE_COAT
    ret = true;
#endif
    return ret;
}

bool HasFresnelTerm()
{
#if defined(_AXF_BRDF_TYPE_SVBRDF)
    return (_SVBRDF_BRDFVariants & 3) != 0;
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
    return true;
#else
    return false;
#endif
}

bool HasAnisotropy()
{
    return (HasFlag(_Flags, FEATUREFLAGS_AXF_ANISOTROPY));
}

bool HasClearcoat()
{
    bool ret = (HasFlag(_Flags, FEATUREFLAGS_AXF_CLEAR_COAT));
#if defined(_AXF_BRDF_TYPE_CAR_PAINT) && defined(FORCE_CAR_PAINT_HAS_CLEARCOAT)
    ret = true;
#endif
    return ret;
}

bool HasClearcoatRefraction()
{
    return (HasFlag(_Flags, FEATUREFLAGS_AXF_CLEAR_COAT_REFRACTION));
}

bool HasClearcoatAndRefraction()
{
    uint bits = (FEATUREFLAGS_AXF_CLEAR_COAT | FEATUREFLAGS_AXF_CLEAR_COAT_REFRACTION);
    return ((_Flags & bits) == bits);
}

bool HasBRDFColorDiagonalClamp()
{
    return (HasFlag(_Flags, FEATUREFLAGS_AXF_BRDFCOLOR_DIAGONAL_CLAMP));
}

bool HonorMinRoughness()
{
    return (HasFlag(_Flags, FEATUREFLAGS_AXF_HONOR_MIN_ROUGHNESS));
}

bool HonorMinRoughnessCoat()
{
    return (HasFlag(_Flags, FEATUREFLAGS_AXF_HONOR_MIN_ROUGHNESS_COAT));
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
    case DEBUGVIEW_AXF_SURFACEDATA_NORMAL_VIEW_SPACE:
        // Convert to view space
        result = TransformWorldToViewDir(surfaceData.normalWS) * 0.5 + 0.5;
        break;
    case DEBUGVIEW_AXF_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        result = TransformWorldToViewDir(surfaceData.geomNormalWS) * 0.5 + 0.5;
        break;
    }
}

void GetBSDFDataDebug(uint paramId, BSDFData bsdfData, inout float3 result, inout bool needLinearToSRGB)
{
    GetGeneratedBSDFDataDebug(paramId, bsdfData, result, needLinearToSRGB);

    // Overide debug value output to be more readable
    switch (paramId)
    {
    case DEBUGVIEW_AXF_BSDFDATA_NORMAL_VIEW_SPACE:
        // Convert to view space
        result = TransformWorldToViewDir(bsdfData.normalWS) * 0.5 + 0.5;
        break;
    case DEBUGVIEW_AXF_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        result = TransformWorldToViewDir(bsdfData.geomNormalWS) * 0.5 + 0.5;
        break;
    }
}

void GetPBRValidatorDebug(SurfaceData surfaceData, inout float3 result)
{
    result = surfaceData.diffuseColor;
}



// This function is used to help with debugging and must be implemented by any lit material
// Implementer must take into account what are the current override component and
// adjust SurfaceData properties accordingdly
void ApplyDebugToSurfaceData(float3x3 tangentToWorld, inout SurfaceData surfaceData)
{
#ifdef DEBUG_DISPLAY
    // NOTE: THe _Debug* uniforms come from /HDRP/Debug/DebugDisplay.hlsl

    // Override value if requested by user this can be use also in case of debug lighting mode like diffuse only
    bool overrideAlbedo = _DebugLightingAlbedo.x != 0.0;
    bool overrideSmoothness = _DebugLightingSmoothness.x != 0.0;
    bool overrideNormal = _DebugLightingNormal.x != 0.0;

    if (overrideAlbedo)
    {
        surfaceData.diffuseColor = _DebugLightingAlbedo.yzw;
    }

    if (overrideSmoothness)
    {
        float overrideSmoothnessValue = _DebugLightingSmoothness.y;
        surfaceData.specularLobe = PerceptualSmoothnessToRoughness(overrideSmoothnessValue);
    }

    if (overrideNormal)
    {
        surfaceData.normalWS = tangentToWorld[2];
    }

    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_DIFFUSE_COLOR)
    {
        surfaceData.diffuseColor = pbrDiffuseColorValidate(surfaceData.diffuseColor, surfaceData.specularColor, false, false).xyz;
    }
    else if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_VALIDATE_SPECULAR_COLOR)
    {
        surfaceData.diffuseColor = pbrSpecularColorValidate(surfaceData.diffuseColor, surfaceData.specularColor, false, false).xyz;
    }
#endif
}

// This function is similar to ApplyDebugToSurfaceData but for BSDFData
//
// NOTE:
//  This will be available and used in ShaderPassForward.hlsl since in AxF.shader,
//  just before including the core code of the pass (ShaderPassForward.hlsl) we include
//  Material.hlsl (or Lighting.hlsl which includes it) which in turn includes us,
//  AxF.shader, via the #if defined(UNITY_MATERIAL_*) glue mechanism.
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
        bsdfData.specularColor = overrideSpecularColor;
    }
#endif
}

// GetScalarRoughnessFromAnisoRoughness is different than GetProjectedRoughness:
// In this case, we don't have a direction to project to.
//
// All our IBL hacks or approximations where we need a single roughness are calibrated
// using an arbitrary roughness that corresponds to the average of the anisotropic roughnesses,
// as the code below show.
//
// The primary reason for this is that the (Anisotropy in [-1,1] + scalar roughness value)
// parametrization as suggested by SPI and used in HDRP has the advantage that the original
// "scalar roughness" value used always corresponds to the average of the anisotropic roughnesses,
// and in the case of isotropic roughnesses, this scalar (average) roughness will obviously match
// the axis aligned roughnesses (as they are equal).
//
// But in general (cf with GetProjectedRoughness), if we wanted eg the scalar roughness for an average
// azimuth angle of 45 degrees, (presumably chosen to be an "average direction" between T and B)
// we would need to calculate:
//
// projectedRoughness@45degree = sqrt(cos^2(pi/4) * roughnessT^2 + sin^2(pi/4) * roughnessB^2)
//                             = sqrt(0.5*roughnessT + 0.5*roughnessB)
//                             = sqrt(2)/2 * sqrt(roughnessT^2 + roughnessB^2)
//
// and we can see that this is == isotropic_roughness = roughnessT = roughnessB = 0.5 * (roughnessT + roughnessB)
// only in the isotropic case.
float GetScalarRoughnessFromAnisoRoughness(float roughnessT, float roughnessB)
{
    return 0.5 * (roughnessT + roughnessB);
}

NormalData ConvertSurfaceDataToNormalData(SurfaceData surfaceData)
{
    NormalData normalData;

    // TODO: consider coat F0 ? flakes (but would require fetching them) ?
    if (HasClearcoat()) // in that case we automatically have dual normal maps
    {
        normalData.normalWS = surfaceData.clearcoatNormalWS;
        normalData.perceptualRoughness = CLEAR_COAT_PERCEPTUAL_ROUGHNESS;
    }
    else
    {
        normalData.normalWS = surfaceData.normalWS;

#if defined(_AXF_BRDF_TYPE_SVBRDF)
        float roughness = (HasAnisotropy()) ? GetScalarRoughnessFromAnisoRoughness(surfaceData.specularLobe.x, surfaceData.specularLobe.y) : surfaceData.specularLobe.x;
        normalData.perceptualRoughness = RoughnessToPerceptualRoughness(roughness);

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
        // Hack: try to get a "single equivalent" roughness
        normalData.perceptualRoughness = 0.0;

        float sumCoeffXRoughness = 0.0;
        float sumCoeff = 0.0;

        UNITY_UNROLL
        for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
        {
            float coeff = _CarPaint2_CTCoeffs[lobeIndex];
            float spread = _CarPaint2_CTSpreads[lobeIndex];

            sumCoeff += coeff;
            sumCoeffXRoughness += spread * coeff;
        }
        normalData.perceptualRoughness = RoughnessToPerceptualRoughness(min(1.0, SafeDiv(sumCoeffXRoughness,sumCoeff)));
#else
        // This is only possible if the AxF is a BTF type. However, there is a bunch of ifdefs do not support this third case
        normalData.perceptualRoughness = 0.0;
#endif
    }

    return normalData;
}

//----------------------------------------------------------------------
// From Walter 2007 eq. 40
// Expects incoming pointing AWAY from the surface
// eta = IOR_above / IOR_below
// rayIntensity returns 0 in case of total internal reflection
//
// Walter et al. formula seems to have a typo in it: the b term below
// needs to have eta^2 instead of eta.
// Note also that our sign(c) term here effectively makes the refractive
// surface dual sided.
float3  Refract(float3 incoming, float3 normal, float eta, out float rayIntensity)
{
    float   c = dot(incoming, normal);
    float   b = 1.0 + Sq(eta) * (c*c - 1.0);
    if (b >= 0.0)
    {
        float   k = eta * c - sign(c) * sqrt(b);
        float3  R = k * normal - eta * incoming;
        rayIntensity = 1;
        return normalize(R);
    }
    else
    {
        rayIntensity = 0;
        return -incoming;   // Total internal reflection, just return an unrefracted dir
    }
}

// Same but without handling total internal reflection because eta > 1
float3  Refract(float3 incoming, float3 normal, float eta)
{
    float   c = dot(incoming, normal);
    float   b = 1.0 + Sq(eta) * (c*c - 1.0);
    float   k = eta * c - sign(c) * sqrt(b);
    float3  R = k * normal - eta * incoming;
    return normalize(R);
}

float3  RefractSaturateToTIR(float3 incoming, float3 normal, float eta, out float rayIntensity, out float3 incomingSaturated)
{
    float c = dot(incoming, normal);
    float sinIncSq = 1 - c*c;
    float b = 1.0 - Sq(eta) * (sinIncSq);

    // The component in the "orthogonal to N direction" when
    // building the refracted vector is made from
    // -eta * ( incoming - N * dot(incoming, normal))
    // ie - eta * incoming + eta * c * normal
    // and we want it to "one" when we saturate the direction to the output-side
    // horizon (just avoiding TIR)
    // since the other component in the normal direction is 0.

    // We will normalize R, the output, at the end, but normally, this isn't required.
    bool noTIR = (b >= 0);
    rayIntensity = float(noTIR);

    const float exitBiasIfTIR = NdotVMinCosSpread; // so our exit direction isn't completely grazing

    float k = eta * c - sign(c) * sqrt(saturate(b)) + (noTIR ? 0: exitBiasIfTIR);
    float3 R = k * normal - eta * incoming;

    float3 criticalDir = (float3)0;
    incomingSaturated = incoming;
    if (noTIR == false)
    {
        float sinThetaCrit = saturate(rcp(eta));
        float cosThetaCrit = sqrt(1 - Sq(sinThetaCrit));
        float3 incOrthoN = (incoming - c * normal) * /*normalize the ortho component:*/rcp(sqrt(sinIncSq));

        criticalDir = sinThetaCrit * incOrthoN + cosThetaCrit * normal;

        incomingSaturated = criticalDir;
    }

    return normalize(R);
}

float3  SaturateDirToHorizon(float3 incoming, float3 normal)
{
    // add eps if you want a bit of positive bias:
    return normalize( incoming + normal * saturate(/*eps here*/NdotVMinCosSpread - dot(incoming, normal)) );
}

//----------------------------------------------------------------------
// Ref: https://seblagarde.wordpress.com/2013/04/29/memo-on-fresnel-equations/
// Fresnel dieletric / dielectric
// Safe version preventing NaNs when IOR = 1
real    F_FresnelDieletricSafe(real IOR, real u)
{
    u = max(1e-3, u); // Prevents NaNs
    real g = sqrt(max(0.0, Sq(IOR) + Sq(u) - 1.0));
    return 0.5 * Sq((g - u) / max(1e-4, g + u)) * (1.0 + Sq(((g + u) * u - 1.0) / ((g - u) * u + 1.0)));
}

float Fresnel0ToIorSafe(float fresnel0)
{
    // We guard against f0 = 1,
    // we always do conversion as if top has an IOR of 1.0, as the f0 is assumed
    // measured and baked-in, ie to be evaluated as-is, with whatever was specified
    // for the top in the rest of the AxF.
    return Fresnel0ToIor(min(0.999, fresnel0)); 
}

//----------------------------------------------------------------------
// Cook-Torrance functions as provided by X-Rite in the "AxF-Decoding-SDK-1.5.1/doc/html/page2.html#carpaint_BrightnessBRDF" document from the SDK
//
// Warning: This matches the SDK but is not the Beckmann D() NDF: a /PI is missing!
float CT_D(float N_H, float m)
{
    float cosb_sqr = N_H * N_H;
    float m_sqr = m * m;
    float e = (cosb_sqr - 1.0) / (cosb_sqr*m_sqr);  // -tan(a)^2 / m^2
    return exp(e) / (m_sqr*cosb_sqr*cosb_sqr);  // exp(-tan(a)^2 / m^2) / (m^2 * cos(a)^4)
}

// Classical Schlick approximation for Fresnel
float CT_F(float H_V, float F0)
{
    float f_1_sub_cos = 1.0 - H_V;
    float f_1_sub_cos_sqr = f_1_sub_cos * f_1_sub_cos;
    float f_1_sub_cos_fifth = f_1_sub_cos_sqr * f_1_sub_cos_sqr*f_1_sub_cos;
    return F0 + (1.0 - F0) * f_1_sub_cos_fifth;
}

float  MultiLobesCookTorrance(float NdotL, float NdotV, float NdotH, float VdotH)
{
    // Ensure numerical stability
    if (NdotV < 0.00174532836589830883577820272085 || NdotL < 0.00174532836589830883577820272085) //sin(0.1 deg )
        return 0.0;

    float   specularIntensity = 0.0;
    for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
    {
        float   F0 = _CarPaint2_CTF0s[lobeIndex];
        float   coeff = _CarPaint2_CTCoeffs[lobeIndex];
        float   spread = _CarPaint2_CTSpreads[lobeIndex];

        specularIntensity += coeff * CT_D(NdotH, spread) * CT_F(VdotH, F0);
    }
    specularIntensity *= G_CookTorrance(NdotH, NdotV, NdotL, VdotH)  // Shadowing/Masking term
        / (PI * max(1e-3, NdotV * NdotL));

    return specularIntensity;
}

// For CARPAINT2
// Samples the "BRDF Color Table" as explained in "AxF-Decoding-SDK-1.5.1/doc/html/page2.html#carpaint_ColorTable" from the SDK
float3  GetBRDFColor(float thetaH, float thetaD)
{

    // [ Update1:
    // Enable this path: in short, the color table seems fully defined in the sample tried like X-Rite_12-PTF_Blue-Violet_NR.axf,
    // and while acos() yields values up to PI, negative input values shouldn't be used
    // for cos(thetaH) (under horizon) and for cos(thetaD), it shouldn't even be possible. 
    // ]

    float2  UV = float2(2.0 * thetaH / PI, 2.0 * thetaD / PI);

#ifdef AUTO_PATCH_FOR_INCOMPLETE_BRDF_COLOR_TABLE
    // [ Update1:
    // The texture should be fully defined for thetaH and thetaD.

    // Although we should note here that some values of thetaD make no sense depending on phiD
    // [see "A New Change of Variables for Efficient BRDF Representation by Szymon M. Rusinkiewicz
    // https://www.cs.princeton.edu/~smr/papers/brdf_change_of_variables/brdf_change_of_variables.pdf
    // for the definition of these angles],
    // as when thetaH > 0, in the worst case when phiD = 0, thetaD must be <= (PI/2 - thetaH)
    // ie when thetaH = PI/2 and phiD = 0, thetaD must be 0,
    // while all values from 0 to PI/2 of thetaD are possible if phiD = PI/2.
    // (This is the reason the phiD = PI/2 "slice" contains more information on the BSDF, 
    // see also s2012_pbs_disney_brdf_notes_v3.pdf p4-5)
    //
    // But with only thetaH and thetaD indexing the table, phiD is ignored, and the
    // true 3D dependency of (even a non-anisotropic - anisotropic would need 4D) BSDF is lost in this parameterization.
    //
    // Having said that, it can happen that sometimes the color table is defined only for half of it, as if the measurements came from
    // such a phiD = 0 degrees slice. In that case, the importer (as of v0.1.5-preview) will try to detect the condition and set a flag
    // along with scalings to offset the diagonal clamp in case even less than half the table is defined.
    // We use these values here. In case the importer misdetects this condition, the UI still allow changing these values:
    // ]
    bool brdfColorUseDiagonalClamp = HasBRDFColorDiagonalClamp();

    if (brdfColorUseDiagonalClamp)
    {
        UV = float2(2.0 * thetaH / PI, INV_HALF_PI * min(HALF_PI - thetaH, thetaD));
        UV *= _CarPaint2_BRDFColorMapUVScale.xy;
    }
#endif

    // Rescale UVs to account for 0.5 texel offset
    uint2   textureSize;
    _CarPaint2_BRDFColorMap.GetDimensions(textureSize.x, textureSize.y);
    UV = (0.5 + UV * (textureSize - 1)) / textureSize;

    return _CarPaint2_BRDFColorMapScale * SAMPLE_TEXTURE2D_LOD(_CarPaint2_BRDFColorMap, sampler_CarPaint2_BRDFColorMap, float2(UV.x, 1 - UV.y), 0).xyz;
}

float GetCarPaintFresnelF0()
{
    float ret = 0;
    float curMax = 0;
    for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
    {
        float   F0 = _CarPaint2_CTF0s[lobeIndex];
        float   coeff = _CarPaint2_CTCoeffs[lobeIndex];

        if (curMax < (F0*coeff))
        {
            ret = F0;
            curMax = F0*coeff;
        }
    }
    return ret;
}

float3 GetCarPaintSpecularColor()
{
    //TODO: improve
    return GetBRDFColor(0,0);
}

//----------------------------------------------------------------------
// Simple Oren-Nayar implementation (from http://patapom.com/blog/BRDF/MSBRDFEnergyCompensation/#oren-nayar-diffuse-model)
//  normal, unit surface normal
//  light, unit vector pointing toward the light
//  view, unit vector pointing toward the view
//  roughness, Oren-Nayar roughness parameter in [0,PI/2]
//
float   OrenNayar(in float3 n, in float3 v, in float3 l, in float roughness)
{
    float   LdotN = dot(l, n);
    float   VdotN = dot(v, n);

    float   gamma = dot(v - n * VdotN, l - n * LdotN)
        / (sqrt(saturate(1.0 - VdotN * VdotN)) * sqrt(saturate(1.0 - LdotN * LdotN)));

    float rough_sq = roughness * roughness;
    //    float A = 1.0 - 0.5 * (rough_sq / (rough_sq + 0.33));   // You can replace 0.33 by 0.57 to simulate the missing inter-reflection term, as specified in footnote of page 22 of the 1992 paper
    float A = 1.0 - 0.5 * (rough_sq / (rough_sq + 0.57));   // You can replace 0.33 by 0.57 to simulate the missing inter-reflection term, as specified in footnote of page 22 of the 1992 paper
    float B = 0.45 * (rough_sq / (rough_sq + 0.09));

    // Original formulation
//  float angle_vn = acos(VdotN);
//  float angle_ln = acos(LdotN);
//  float alpha = max(angle_vn, angle_ln);
//  float beta  = min(angle_vn, angle_ln);
//  float C = sin(alpha) * tan(beta);

    // Optimized formulation (without tangents, arccos or sines)
    float2  cos_alpha_beta = VdotN < LdotN ? float2(VdotN, LdotN) : float2(LdotN, VdotN);   // Here we reverse the min/max since cos() is a monotonically decreasing function
    float2  sin_alpha_beta = sqrt(saturate(1.0 - cos_alpha_beta * cos_alpha_beta));           // Saturate to avoid NaN if ever cos_alpha > 1 (it happens with floating-point precision)
    float   C = sin_alpha_beta.x * sin_alpha_beta.y / (1e-6 + cos_alpha_beta.y);

    return A + B * max(0.0, gamma) * C;
}

//-----------------------------------------------------------------------------
// conversion function for forward
//-----------------------------------------------------------------------------

BSDFData ConvertSurfaceDataToBSDFData(uint2 positionSS, SurfaceData surfaceData)
{
    BSDFData    bsdfData;
    //  ZERO_INITIALIZE(BSDFData, data);

    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.tangentWS = surfaceData.tangentWS;
    bsdfData.biTangentWS = cross(bsdfData.normalWS, bsdfData.tangentWS);

    //-----------------------------------------------------------------------------
#ifdef _AXF_BRDF_TYPE_SVBRDF
    bsdfData.diffuseColor = surfaceData.diffuseColor;
    bsdfData.specularColor = surfaceData.specularColor;

    bsdfData.fresnelF0 = surfaceData.fresnelF0; // See AxfData.hlsl: the actual sampled texture is always 1 channel, if we ever find otherwise, we will use the others.
    bsdfData.height_mm = surfaceData.height_mm;

    bsdfData.roughness = HasAnisotropy() ? surfaceData.specularLobe : surfaceData.specularLobe.xx;

    bsdfData.clearcoatColor = surfaceData.clearcoatColor;
    bsdfData.clearcoatNormalWS = HasClearcoat() ? surfaceData.clearcoatNormalWS : surfaceData.normalWS;
    bsdfData.clearcoatIOR = surfaceData.clearcoatIOR;

    // Useless but pass along anyway
    bsdfData.flakesUV = surfaceData.flakesUV;
    bsdfData.flakesMipLevel = surfaceData.flakesMipLevel;

    //-----------------------------------------------------------------------------
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
    bsdfData.diffuseColor = surfaceData.diffuseColor;
    bsdfData.flakesUV = surfaceData.flakesUV;
    bsdfData.flakesMipLevel = surfaceData.flakesMipLevel;
    bsdfData.clearcoatColor = 1.0;  // Not provided, assume white...
    bsdfData.clearcoatIOR = surfaceData.clearcoatIOR;
    bsdfData.clearcoatNormalWS = HasClearcoat() ? surfaceData.clearcoatNormalWS : surfaceData.normalWS;

    bsdfData.specularColor = GetCarPaintSpecularColor();
    bsdfData.fresnelF0 = GetCarPaintFresnelF0();
    bsdfData.roughness = 0;
    bsdfData.height_mm = 0;
#endif

    bsdfData.geomNormalWS = surfaceData.geomNormalWS;

    ApplyDebugToBSDFData(bsdfData);
    return bsdfData;
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
    float   NdotV_UnderCoat;    // NdotV after optional clear-coat refraction. Could be negative due to normal mapping, use ClampNdotV()
    float   NdotV_Clearcoat;    // NdotV before optional clear-coat refraction. Could be negative due to normal mapping, use ClampNdotV()
    float3  viewWS_UnderCoat;   // View vector after optional clear-coat refraction.

    // IBL
    float3  iblDominantDirectionWS_UnderCoat;   // Dominant specular direction, used for IBL in EvaluateBSDF_Env()
    float3  iblDominantDirectionWS_Clearcoat;   // Dominant specular direction, used for IBL in EvaluateBSDF_Env() and also in area lights when clearcoat is enabled
#ifdef _AXF_BRDF_TYPE_SVBRDF
    float   iblPerceptualRoughness;
    float3  specularFGD;
    float   diffuseFGD;
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT) 
#if !defined(USE_COOK_TORRANCE_MULTI_LOBES)
    float   iblPerceptualRoughness;     // Use this to store an average lobe roughness
    float3  specularCTFGDSingleLobe;
#else
    float3  iblPerceptualRoughness;   // per lobe values in xyz
    float3  specularCTFGDAtZeroF0;     // monochromatic FGD, per lobe values in xyz
    float3  specularCTFGDReflectivity; // monochromatic FGD, per lobe values in xyz
#endif
    float3  singleBRDFColor;
    float3  singleFlakesComponent;
    float   flakesFGD;
#endif
    float   coatFGD;
    float   coatPartLambdaV;

// Area lights (18 VGPRs)
// TODO: 'orthoBasisViewNormal' is just a rotation around the normal and should thus be just 1x VGPR.
    float3x3    orthoBasisViewNormal;       // Right-handed view-dependent orthogonal basis around the normal (6x VGPRs)
#ifdef _AXF_BRDF_TYPE_SVBRDF
    float3x3    ltcTransformDiffuse;    // Inverse transformation                                         (4x VGPRs)
    float3x3    ltcTransformSpecular;   // Inverse transformation                                         (4x VGPRs)
#endif
    float3x3    ltcTransformClearcoat;

#if defined(_AXF_BRDF_TYPE_CAR_PAINT)
    float3x3    ltcTransformSpecularCT[MAX_CT_LOBE_COUNT];   // Inverse transformation                                         (4x VGPRs)
    float3x3    ltcTransformFlakes;
#endif
};

//
// ClampRoughness helper specific to this material
//
void ClampRoughness(inout PreLightData preLightData, inout BSDFData bsdfData, float minRoughness)
{
}

#if defined(_AXF_BRDF_TYPE_CAR_PAINT)
//
// Flakes BTF access
//

// Samples the "BTF Flakes" texture as explained in "AxF-Decoding-SDK-1.5.1/doc/html/page2.html#carpaint_FlakeBTF" from the SDK
uint    SampleFlakesLUT(uint index)
{
    return 255.0 * _CarPaint2_FlakeThetaFISliceLUTMap[uint2(index, 0)].x;
    // Hardcoded LUT
    //    uint    pipoLUT[] = { 0, 8, 16, 24, 32, 40, 47, 53, 58, 62, 65, 67 };
    //    return pipoLUT[min(11, _index)];
}

float3  SamplesFlakes(float2 UV, uint sliceIndex, float mipLevel)
{
    return _CarPaint2_BTFFlakeMapScale * SAMPLE_TEXTURE2D_ARRAY_LOD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap, UV, sliceIndex, mipLevel).xyz;
}

//
// Working code, TODO_FLAKES: missing virtual thetaD (aka thetaI) bin generation
//
float3  CarPaint_BTF(float thetaH, float thetaD, BSDFData bsdfData)
{
    float2  UV = bsdfData.flakesUV;
    float   mipLevel = bsdfData.flakesMipLevel;

    // thetaH sampling defines the angular sampling, i.e. angular flake lifetime
    float   binIndexH = _CarPaint2_FlakeNumThetaF * (2.0 * thetaH / PI) + 0.5; // TODO: doc says to use NumThetaF for both, check if this isn't a typo
    float   binIndexD = _CarPaint2_FlakeNumThetaF * (2.0 * thetaD / PI) + 0.5;

    // Bilinear interpolate indices and weights
    uint    thetaH_low = floor(binIndexH);
    uint    thetaD_low = floor(binIndexD);
    uint    thetaH_high = thetaH_low + 1;
    uint    thetaD_high = thetaD_low + 1;
    float   thetaH_weight = binIndexH - thetaH_low;
    float   thetaD_weight = binIndexD - thetaD_low;

    // To allow lower thetaD samplings while preserving flake lifetime, "virtual" thetaD patches are generated by shifting existing ones
    float2   offset_l = 0;
    float2   offset_h = 0;

    // Organization of the flake BTF slice array and LUT:
    //
    // The two angles thetaH and thetaD (aka thetaF and thetaI in the documentation)
    // index an array of slices, and an indirection is first used through an integer LUT
    // (UVs are spatial to be finally used with the individual slices):
    //
    // Basically, the slices in the array are arranged in incrementing thetaH "steps" (or bins),
    // for each constant thetaD bin number,
    // ie in thetaD-major order, as for a single thetaD, the multiple thetaH slices
    // are consecutive, ie incrementing the wanted thetaD bin causes big jumps in the effective
    // slice index to use in the array.
    //
    // Another peculiarity is that the stride (number of slices to skip) to go to the next
    // thetaD bin is not constant as not all slices exist, some thetaH ranges "dying off" very quickly
    // depending on the thetaD: ie only a few thetaH slices can exist for a particular thetaD bin.
    //
    // Non-existing slices for a particular thetaD, thetaH are taken to fetch zero values (hence the
    // "dying off" above).
    //
    // The integer LUT is indexed by thetaD and gives the index in the slice array for this thetaD
    // and/at the start of the thetaH range, ie for the first thetaH bin for the thetaH range [0, 0 + deltaH)
    //
    // The absolute maximum index for the integer LUT is denoted _CarPaint2_FlakeMaxThetaI.
    //
    // Consider eg this thetaFISliceLut sized to 64 entries:
    //
    // 0 7 14 21 28 35 42 48 53 57 60 62 0 0 0 ... (all zeroes for the rest)
    //
    // FlakeMaxThetaI = 12, and indeed as we can see, after the first 12 entries, everything is 0.
    // We can have valid thetaD bins with thetaD_low from 0 to 10. See comments below for details about
    // this: indeed if thetaD_low = 11, LUT[11] will be 62, but LUT[11+1] will be 0, indicating no
    // index space left starting at 62 for the thetaH bins for this particular thetaD bin.
    //
    // In short, a valid range of final indices in the slice array for a particular thetaD bin is
    // indicated by a start index at LUT[i] and a limit index which is just indicated by the start
    // index of the next thetaD bin, at LUT[i+1].

    // ------------------------------------------------------
    // TODO; what they call "virtual thetaD" bins generation:
    // Check if this is needed, and port this
    // (eg with a noise texture):
    // ------------------------------------------------------
    //
    // Basically, from the documentation example, it seems that the number of bins considered
    // for *both* thetaH and thetaD are NumThetaF (aka NumThetaH)
    // ie for *both* angular spaces, the number of bin subdivisions is (counterintuitively) NumThetaF.
    //
    // However, the real sampling resolution of the thetaD space (_CarPaint_numThetaI) can be lower,
    // and this is indicated by the (_CarPaint_numThetaI < _CarPaint_numThetaF) condition.
    //
    // If this is the case, we squash back the overflowing "binIndexD"
    // (that we overextended by multiplying (2.0 * thetaD / PI) by _CarPaint2_FlakeNumThetaF)
    // and thus repeat usage of some slices, but we shift them *spatially* by random amounts to hide this.
    // (the offset_* below are to be used with the spatial UVs)
    //
    //    if (_CarPaint_numThetaI < _CarPaint_numThetaF) {
    //        offset_l = float2(rnd_numbers[2*thetaD_low], rnd_numbers[2*thetaD_low+1]);
    //        offset_h = float2(rnd_numbers[2*thetaD_high], rnd_numbers[2*thetaD_high+1]);
    //        if (thetaD_low & 1)
    //            UV.xy = UV.yx;
    //        if (thetaD_high & 1)
    //            UV.xy = UV.yx;
    //
    //        // Map to the original sampling
    //        thetaD_low = floor(thetaD_low * float(_CarPaint_numThetaI) / _CarPaint_numThetaF);
    //        thetaD_high = floor(thetaD_high * float(_CarPaint_numThetaI) / _CarPaint_numThetaF);
    //        //
    //        // WARNING: double check SDK but our original code was wrong in that case:
    //        //
    //        // Note that in that case, thetaD_low can be == to thetaD_high, 
    //        // eg with 
    //        //         _CarPaint_numThetaI = 7,
    //        //         _CarPaint_numThetaF = 12,
    //        //         original thetaD_low = 2 (and thus original thetaD_high = 3).
    //        // we get
    //        //         thetaD_low = floor( 2 * 7.0/12 ) = floor( 2 * 0.58333) = floor(1.1667) = 1
    //        //         thetaD_high = floor( 3 * 7.0/12 ) = floor( 3 * 0.58333) = floor(1.75) = 1
    //        //
    //        // Again in our original code, we systematically took thetaD_high == thetaD_low + 1 when
    //        // verifying the indexing limit using for LUT1:
    //        // 
    //        // uint    LUT0 = SampleFlakesLUT(thetaD_low);
    //        // uint    LUT1 = SampleFlakesLUT(thetaD_high);
    //        // uint    LUT2 = SampleFlakesLUT(thetaD_high + 1);
    //        //
    //        // LUT1 is NOT the value we should use to check if we slip over in thetaH (thetaF)!
    //        // it could be that thetaD_low == thetaD_high (virtual thetaD bins) where we stay in the
    //        // same bin but shift our UVs for that same slice to fake having another "thetaD" bin
    //        // (taking all the same slices for another aliased thetaD but shifting the UVs of those).
    //        // However, we still need to make sure, when we choose a final slice taking into account
    //        // the int offset due to the thetaH sampling/bin, that the calculated index doesn't fall
    //        // off the current valid range for the current thetaD as indicated by 2 consecutive LUT
    //        // entries!
    //
    //    }

    float3  H0_D0 = 0.0;
    float3  H1_D0 = 0.0;
    float3  H0_D1 = 0.0;
    float3  H1_D1 = 0.0;

    // Access flake texture - make sure to stay in the correct slices (no slip over)
    if (thetaD_low < _CarPaint2_FlakeMaxThetaI)
    {
        float2  UVl = UV + offset_l;
        float2  UVh = UV + offset_h;

        uint    LUT0 = SampleFlakesLUT(thetaD_low);
        uint    LUT1 = SampleFlakesLUT(thetaD_high);
        uint    LUT0_limit = SampleFlakesLUT(thetaD_low+1);
        // without "virtual thetaD" bins, LUT0_limit will be the same as LUT1 and optimized out.
        uint    LUT2 = SampleFlakesLUT(thetaD_high + 1);

        if (LUT0 + thetaH_low < LUT0_limit)
        {
            H0_D0 = SamplesFlakes(UVl, LUT0 + thetaH_low, mipLevel);
            if (LUT0 + thetaH_high < LUT0_limit)
            {
                H1_D0 = SamplesFlakes(UVl, LUT0 + thetaH_high, mipLevel);
            }
        }
        // else it means that the calculated index for that thetaD_low and the thetaH_low
        // bin doesn't even include the start of the H range we want to interpolate.
        // This could happen even if thetaH_low == 0, if for example we're at the last
        // non-zero value of the integer LUT due to thetaD_low itself: in that case
        // LUT1 value contains 0, ie we don't even have an index for the next thetaD bin
        // start which would give us a limit index to use for the maximum thetaH bin
        // in the current thetaD_low bin. (ie a valid thetaD bin needs LUT[i] and LUT[i+1]
        // to be valid as these indicate the limits for the final slice array index
        // calculated including the offset induced by the minor dimension thetaH-bin)

        if (thetaD_high < _CarPaint2_FlakeMaxThetaI)
        {
            if (LUT1 + thetaH_low < LUT2)
            {
                H0_D1 = SamplesFlakes(UVh, LUT1 + thetaH_low, mipLevel);
                if (LUT1 + thetaH_high < LUT2)
                {
                    H1_D1 = SamplesFlakes(UVh, LUT1 + thetaH_high, mipLevel);
                }
            }
            // else, same thing as our comment above
        }
    }

    // Bilinear interpolation
    float3  D0 = lerp(H0_D0, H1_D0, thetaH_weight);
    float3  D1 = lerp(H0_D1, H1_D1, thetaH_weight);
    return lerp(D0, D1, thetaD_weight);
}
#endif //...#if defined(_AXF_BRDF_TYPE_CAR_PAINT)

float3 FindAverageBaseLobeDirOnTop(BSDFData bsdfData, PreLightData preLightData)
{
    float3 outDir;

#if 0 
    // simple test: eg for carpaint or any material without any normal maps, this should give the same 
    // fetch alignment as just using the view reflected on top:
    float3 vRefractedBottomReflected = reflect(-preLightData.viewWS_UnderCoat, bsdfData.normalWS);
    outDir = Refract(-vRefractedBottomReflected, -bsdfData.clearcoatNormalWS, bsdfData.clearcoatIOR);
    return outDir;
#else
    float3 vRefractedBottomReflected = reflect(-preLightData.viewWS_UnderCoat, bsdfData.normalWS);
    // First make sure that vRefractedBottomReflected is directed towards the coat surface we want to pass:
    // ie make sure it is not under the top horizon (let alone in TIR which we ignore!)
    vRefractedBottomReflected = SaturateDirToHorizon(vRefractedBottomReflected, bsdfData.clearcoatNormalWS);

    //to test SaturateDirToHorizon:
    //outDir = Refract(-vRefractedBottomReflected, -bsdfData.clearcoatNormalWS, bsdfData.clearcoatIOR);
    //return outDir;

    // Now whether the direction was past the critical angle nor not, refract while making sure that 
    // in case of TIR, we just output an horizon grazing direction:
    
    //to debug when actually TIR happened:
    float3 incomingSaturated; 
    float rayIntensity;
    outDir = RefractSaturateToTIR(-vRefractedBottomReflected, -bsdfData.clearcoatNormalWS, bsdfData.clearcoatIOR, rayIntensity, incomingSaturated);
#endif    
    return outDir;

}


PreLightData    GetPreLightData(float3 viewWS_Clearcoat, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData    preLightData;
    //  ZERO_INITIALIZE(PreLightData, preLightData);

    preLightData.NdotV_Clearcoat = dot(bsdfData.clearcoatNormalWS, viewWS_Clearcoat);
    preLightData.viewWS_UnderCoat = viewWS_Clearcoat;   // Save original view before optional refraction by clearcoat

    //-----------------------------------------------------------------------------
    // Handle clearcoat refraction of view ray
    if (HasClearcoatAndRefraction())
    {
        preLightData.viewWS_UnderCoat = -Refract(viewWS_Clearcoat, bsdfData.clearcoatNormalWS, 1.0 / bsdfData.clearcoatIOR);
    }
    //todo_dir test_disable_refract for environments:
    //preLightData.viewWS_UnderCoat = viewWS_Clearcoat;
    // Compute under-coat view-dependent data after optional refraction
    preLightData.NdotV_UnderCoat = dot(bsdfData.normalWS, preLightData.viewWS_UnderCoat);
    //preLightData.NdotV_UnderCoat = min(preLightData.NdotV_UnderCoat, preLightData.NdotV_Clearcoat);

    float   NdotV_UnderCoat = ClampNdotV(preLightData.NdotV_UnderCoat);
    float   NdotV_Clearcoat = ClampNdotV(preLightData.NdotV_Clearcoat);
    //test_disable_refract for environments:
    //NdotV_UnderCoat = NdotV_Clearcoat;
    //NdotV_UnderCoat = min(NdotV_UnderCoat, NdotV_Clearcoat);
    //-----------------------------------------------------------------------------
    // Handle IBL +  multiscattering
    // todo_dir:
    // todo_dir todo_modes todo_pseudorefract: cant use undercoat like that, but better than to lose the bottom normal effect for now...
    preLightData.iblDominantDirectionWS_UnderCoat = reflect(-preLightData.viewWS_UnderCoat, bsdfData.normalWS);    
    if (HasClearcoatAndRefraction())
    {
        preLightData.iblDominantDirectionWS_UnderCoat = FindAverageBaseLobeDirOnTop(bsdfData, preLightData); // much better
    }
    preLightData.iblDominantDirectionWS_Clearcoat = reflect(-viewWS_Clearcoat, bsdfData.clearcoatNormalWS);
    //preLightData.iblDominantDirectionWS_UnderCoat = preLightData.iblDominantDirectionWS_Clearcoat;

#ifdef _AXF_BRDF_TYPE_SVBRDF
    // @TODO => Anisotropic IBL?
    // TODO_SL
    preLightData.iblPerceptualRoughness = RoughnessToPerceptualRoughness(GetScalarRoughnessFromAnisoRoughness(bsdfData.roughness.x, bsdfData.roughness.y));
    
    // todo_fresnel: TOCHECK: Make BRDF and FGD for env. consistent with dirac lights for HasFresnelTerm() handling:
    // currently, we only check it for Ward and its variants.
    float3 tempF0 = HasFresnelTerm() ? bsdfData.fresnelF0.rrr : 1.0;
    tempF0 *= bsdfData.specularColor; // Important to use in the PreIntegratedFGD interpolated fetches!

    float specularReflectivity;
    switch ((_SVBRDF_BRDFType >> 1) & 7)
    {
    //@TODO: Oren-Nayar diffuse FGD
    case 0:
        GetPreIntegratedFGDWardAndLambert(NdotV_UnderCoat, preLightData.iblPerceptualRoughness, tempF0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
        // Although we have pre-integrated FGD for non-GGX BRDFs, all our IBL are pre-convolved with GGX, so use this rough conversion:
        preLightData.iblPerceptualRoughness = PerceptualRoughnessBeckmannToGGX(preLightData.iblPerceptualRoughness);
        break;

    // case 1: // @TODO: Support Blinn-Phong FGD?
    case 2:
        GetPreIntegratedFGDCookTorranceAndLambert(NdotV_UnderCoat, preLightData.iblPerceptualRoughness, tempF0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
        preLightData.specularFGD *= GetPreIntegratedFGDCookTorranceSampleMutiplier();
        // Although we have pre-integrated FGD for non-GGX BRDFs, all our IBL are pre-convolved with GGX, so use this rough conversion:
        preLightData.iblPerceptualRoughness = PerceptualRoughnessBeckmannToGGX(preLightData.iblPerceptualRoughness);
        break;
    case 3:
        GetPreIntegratedFGDGGXAndLambert(NdotV_UnderCoat, preLightData.iblPerceptualRoughness, tempF0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
        break;

     // case 4: // @TODO: Support Blinn-Phong FGD?

    default:    // Use GGX by default
        GetPreIntegratedFGDGGXAndLambert(NdotV_UnderCoat, preLightData.iblPerceptualRoughness, tempF0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
        break;
    }

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
    float   sumRoughness = 0.0;
    float   sumCoeff = 0.0;
    float   sumF0 = 0.0;
    float3  tempF0;
    float   diffuseFGD, reflectivity; //TODO
    float3  specularFGD;
    preLightData.iblPerceptualRoughness = 0;
    preLightData.specularCTFGDAtZeroF0 = 0;
    preLightData.specularCTFGDReflectivity = 0;
    preLightData.ltcTransformSpecularCT = (float3x3[MAX_CT_LOBE_COUNT])0;

    // TODO_diffuseFGDColor: better one, averaged maybe...
    preLightData.singleBRDFColor = 1.0;
    float thetaH = 0; //acos(clamp(NdotH, 0, 1));
    float thetaD = acos(clamp(preLightData.NdotV_UnderCoat, 0, 1));
    
    preLightData.singleBRDFColor *= GetBRDFColor(thetaH, thetaD);
    preLightData.singleFlakesComponent = CarPaint_BTF(thetaH, thetaD, bsdfData);

    UNITY_UNROLL
    for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
    {
        float   F0 = _CarPaint2_CTF0s[lobeIndex];
        float   coeff = _CarPaint2_CTCoeffs[lobeIndex];
        float   spread = _CarPaint2_CTSpreads[lobeIndex];
#if !USE_COOK_TORRANCE_MULTI_LOBES
        // Computes weighted average of roughness values
        sumCoeff += coeff;
        sumF0 += F0;
        sumRoughness += spread;
#else
        // We also do the pre-integrated FGD fetches here:
        // Note that PreIntegratedFGD_CookTorrance is done using (non perceptual) Beckmann roughness as it should:
        float perceptualRoughnessBeckmann = RoughnessToPerceptualRoughness(spread);

        GetPreIntegratedFGDCookTorranceAndLambert(NdotV_UnderCoat, perceptualRoughnessBeckmann, (float3)0.0, specularFGD, diffuseFGD, reflectivity);

        preLightData.specularCTFGDAtZeroF0[lobeIndex] = specularFGD.x * GetPreIntegratedFGDCookTorranceSampleMutiplier();
        preLightData.specularCTFGDReflectivity[lobeIndex] = reflectivity.x * GetPreIntegratedFGDCookTorranceSampleMutiplier();

        //float3 specularFGDFromGGX;
        //test_Beckmann_to_GGX on preintegratedFGD:
        //GetPreIntegratedFGDGGXAndLambert(NdotV_UnderCoat, PerceptualRoughnessBeckmannToGGX(perceptualRoughnessBeckmann), F0.xxx, specularFGDFromGGX, diffuseFGD, reflectivity);
        //test_Beckmann_to_GGX on preintegratedFGD:
        //preLightData.specularCTFGD[lobeIndex] = lerp(specularFGD.x, specularFGDFromGGX.x, _SVBRDF_HeightMapMaxMM);
        //if (_SVBRDF_HeightMapMaxMM == 3.0)
        //{
        //    GetPreIntegratedFGDGGXAndLambert(NdotV_UnderCoat, perceptualRoughnessBeckmann, F0.xxx, specularFGDFromGGX, diffuseFGD, reflectivity);
        //    preLightData.specularCTFGD[lobeIndex] = specularFGDFromGGX.x;
        //}

        // debugtest:
        //preLightData.iblPerceptualRoughness[lobeIndex] = _SVBRDF_HeightMapMaxMM * PerceptualRoughnessBeckmannToGGX(perceptualRoughnessBeckmann);
        preLightData.iblPerceptualRoughness[lobeIndex] = PerceptualRoughnessBeckmannToGGX(perceptualRoughnessBeckmann);

        // And the area lights LTC inverse transform:
        // todo_modes todo_pseudorefract: commented, cant use undercoat like that.
        //float2   UV = LTCGetSamplingUV(NdotV_UnderCoat, preLightData.iblPerceptualRoughness[lobeIndex]);
        float2   UV = LTCGetSamplingUV(NdotV_Clearcoat, preLightData.iblPerceptualRoughness[lobeIndex]);
        preLightData.ltcTransformSpecularCT[lobeIndex] = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_COOK_TORRANCE);
#endif
    }

#if !USE_COOK_TORRANCE_MULTI_LOBES
    // Not used if sampling the environment for each Cook-Torrance lobe
    // Simulate one lobe with averaged roughness and f0
    float oneOverLobeCnt = rcp(CARPAINT2_LOBE_COUNT);
    preLightData.iblPerceptualRoughness = RoughnessToPerceptualRoughness(sumRoughness * oneOverLobeCnt);
    tempF0 = sumF0 * oneOverLobeCnt;
    // todo_BeckmannToGGX
    GetPreIntegratedFGDCookTorranceAndLambert(NdotV_UnderCoat, preLightData.iblPerceptualRoughness, tempF0 * preLightData.singleBRDFColor, specularFGD, diffuseFGD, reflectivity);
    specularFGD *= GetPreIntegratedFGDCookTorranceSampleMutiplier();
    preLightData.specularCTFGDSingleLobe = specularFGD * sumCoeff;
#endif
    // preLightData.flakesFGD =
    //
    // For flakes, even if they are to be taken as tiny mirrors, the orientation would need to be
    // captured by a high res normal map with the problems that this implies.
    // So instead we have a pseudo BTF that is the "left overs" that the CT lobes don't fit, indexed
    // by two angles (which is theoretically a problem, see comments in GetBRDFColor).
    // If we wanted to add more variations on top, here we could consider 
    // a pre-integrated FGD for flakes. 
    // If we assume very low roughness like the coat, we could also approximate it as being a Fresnel
    // term like for coatFGD below.
    // If the f0 is already very high though (metallic flakes), the variations won't be substantial.
    //
    // For testing for now:
    preLightData.flakesFGD = 1.0;
    GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV_UnderCoat, FLAKES_PERCEPTUAL_ROUGHNESS, FLAKES_F0, specularFGD, diffuseFGD, reflectivity);
    IFNOT_FLAKES_JUST_BTF(preLightData.flakesFGD = specularFGD.x);
    preLightData.singleFlakesComponent *= preLightData.flakesFGD;

    // We will override this with the coat transform if we just want the BTF term in LTC lights
    // todo_modes todo_pseudorefract: cant use undercoat like that:
    //float2 UV = LTCGetSamplingUV(NdotV_UnderCoat, FLAKES_PERCEPTUAL_ROUGHNESS);
    float2 UV = LTCGetSamplingUV(NdotV_Clearcoat, FLAKES_PERCEPTUAL_ROUGHNESS);
    IFNOT_FLAKES_JUST_BTF(preLightData.ltcTransformFlakes = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_GGX));

#endif//#ifdef _AXF_BRDF_TYPE_SVBRDF


//-----------------------------------------------------------------------------
// Area lights

// Construct a right-handed view-dependent orthogonal basis around the normal
    preLightData.orthoBasisViewNormal[2] = bsdfData.normalWS;
    preLightData.orthoBasisViewNormal[0] = normalize(viewWS_Clearcoat - preLightData.NdotV_Clearcoat * bsdfData.normalWS);    // Do not clamp NdotV here
    preLightData.orthoBasisViewNormal[1] = cross(preLightData.orthoBasisViewNormal[2], preLightData.orthoBasisViewNormal[0]);

#ifdef _AXF_BRDF_TYPE_SVBRDF
    // UVs for sampling the LUTs
    // todo_modes todo_pseudorefract: cant use undercoat like that
    //float2  UV = LTCGetSamplingUV(NdotV_UnderCoat, preLightData.iblPerceptualRoughness);

    float2  UV = LTCGetSamplingUV(NdotV_Clearcoat, preLightData.iblPerceptualRoughness);

    // Load diffuse LTC & FGD
    if (_SVBRDF_BRDFType & 1)
    {
        preLightData.ltcTransformDiffuse = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_OREN_NAYAR);
    }
    else
    {
        preLightData.ltcTransformDiffuse = k_identity3x3;   // Lambert
    }

    // Load specular LTC & FGD
    switch ((_SVBRDF_BRDFType >> 1) & 7)
    {
    // Warning: all these LTC_MATRIX_INDEX_ are the same for now, and fitted for GGX, hence the code
    // above that selected the UVs all used a preLightData.iblPerceptualRoughness value that used a 
    // conversion formula for Beckmann NDF (exp) based BRDFs
    // (see switch ((_SVBRDF_BRDFType >> 1) & 7) above and usage of PerceptualRoughnessBeckmannToGGX)
    //
    case 0: preLightData.ltcTransformSpecular = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_WARD); break;
    case 2: preLightData.ltcTransformSpecular = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_COOK_TORRANCE); break;
    case 3: preLightData.ltcTransformSpecular = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_GGX); break;
    case 1: // BLINN-PHONG
    case 4: // PHONG;
    {
        // According to https://computergraphics.stackexchange.com/questions/1515/what-is-the-accepted-method-of-converting-shininess-to-roughness-and-vice-versa
        //  float   exponent = 2/roughness^4 - 2;
        //
        float   exponent = PerceptualRoughnessToRoughness(preLightData.iblPerceptualRoughness);
        float   roughness = pow(max(0.0, 2.0 / (exponent + 2)), 1.0 / 4.0);
        // todo_modes todo_pseudorefract: cant use undercoat like that
        //float2  UV = LTCGetSamplingUV(NdotV_UnderCoat, RoughnessToPerceptualRoughness(roughness));
        float2  UV = LTCGetSamplingUV(NdotV_Clearcoat, RoughnessToPerceptualRoughness(roughness));
        preLightData.ltcTransformSpecular = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_COOK_TORRANCE);
        break;
    }

    default:    // @TODO
        preLightData.ltcTransformSpecular = 0;
        break;
    }

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

    // already sampled the matrices in our loop for pre-integrated FGD above

#endif  // _AXF_BRDF_TYPE_SVBRDF

    // Load clear-coat LTC & FGD
    preLightData.ltcTransformClearcoat = 0.0;
#if defined(_AXF_BRDF_TYPE_CAR_PAINT)
    IF_FLAKES_JUST_BTF(preLightData.ltcTransformFlakes = 0.0);
#endif
    preLightData.coatFGD = 0;
    preLightData.coatPartLambdaV = 0;
    if (HasClearcoat())
    {
        float2  UV = LTCGetSamplingUV(NdotV_Clearcoat, CLEAR_COAT_PERCEPTUAL_ROUGHNESS);
        preLightData.ltcTransformClearcoat = LTCSampleMatrix(UV, LTC_MATRIX_INDEX_GGX);
#if defined(_AXF_BRDF_TYPE_CAR_PAINT)
        IF_FLAKES_JUST_BTF(preLightData.ltcTransformFlakes = preLightData.ltcTransformClearcoat);
#endif

        #if 0
        float   clearcoatF0 = IorToFresnel0(bsdfData.clearcoatIOR);
        float   specularReflectivity, dummyDiffuseFGD;
        GetPreIntegratedFGDGGXAndDisneyDiffuse(NdotV_Clearcoat, CLEAR_COAT_PERCEPTUAL_ROUGHNESS, clearcoatF0, preLightData.coatFGD, dummyDiffuseFGD, specularReflectivity);
        // Cheat a little and make the amplitude go to 0 when F0 is 0 (which the actual dieletric Fresnel should do!)
        preLightData.coatFGD *= smoothstep(0, 0.01, clearcoatF0);
        #else
        // We can approximate the pre-integrated FGD term for a near dirac BSDF as the
        // point evaluation of the Fresnel term itself when L is at the NdotV angle,
        // which is the split sum environment assumption (cf Lit doing the same with preLightData.coatIblF)
        // We use expensive Fresnel here so the clearcoat properly disappears when IOR -> 1
        preLightData.coatFGD = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, NdotV_Clearcoat);
        #endif

        // For the coat lobe, we need a sharp BSDF for the high smoothness,
        // See axf-decoding-sdk/doc/html/page1.html#svbrdf_subsec03
        // we arbitrarily use GGX
        preLightData.coatPartLambdaV = GetSmithJointGGXPartLambdaV(NdotV_Clearcoat, CLEAR_COAT_ROUGHNESS);
    }

    return preLightData;
}

//----------------------------------------------------------------------
// Computes Fresnel reflection/refraction of view and light vectors due to clearcoating
// Returns the ratios of the incoming reflected and refracted energy
// Also refracts the provided view and light vectors if refraction is enabled
//
//void    ComputeClearcoatReflectionAndExtinction(inout float3 viewWS, inout float3 lightWS, BSDFData bsdfData, out float3 reflectedRatio, out float3 refractedRatio) {
//
//    // Computes perfect mirror reflection
//    float3  H = normalize(viewWS + lightWS);
//    float   LdotH = saturate(dot(lightWS, H));
//
//    reflectedRatio = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, LdotH);    // Full reflection in mirror direction (we use expensive Fresnel here so the clearcoat properly disappears when IOR -> 1)
//
//    // Compute input/output Fresnel reflections
//    float   LdotN = saturate(dot(lightWS, bsdfData.clearcoatNormalWS));
//    float3  Fin = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, LdotN);
//
//    float   VdotN = saturate(dot(viewWS, bsdfData.clearcoatNormalWS));
//    float3  Fout = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, VdotN);
//
//    // Apply optional refraction
//    if (_Flags & 4U) {
//          float eta = 1.0 / bsdfData.clearcoatIOR;
//        lightWS = -Refract(lightWS, bsdfData.clearcoatNormalWS, eta);
//        viewWS = -Refract(viewWS, bsdfData.clearcoatNormalWS, eta);
//    }
//
//    refractedRatio = (1-Fin) * (1-Fout);
//}

void    ComputeClearcoatReflectionAndExtinction_UsePreLightData(inout float3 viewWS, inout float3 lightWS, BSDFData bsdfData, PreLightData preLightData, out float reflectedRatio, out float refractedRatio)
{

    // Computes perfect mirror reflection
    float3  H = normalize(viewWS + lightWS);
    float   LdotH = saturate(dot(lightWS, H));

    reflectedRatio = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, LdotH); // we use expensive Fresnel here so the clearcoat properly disappears when IOR -> 1

    // TOCHECK

    // Compute input/output Fresnel reflections
    float   LdotN = saturate(dot(lightWS, bsdfData.clearcoatNormalWS));
    float   Fin = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, LdotN);

    float   VdotN = saturate(dot(viewWS, bsdfData.clearcoatNormalWS));
    float   Fout = F_FresnelDieletricSafe(bsdfData.clearcoatIOR, VdotN);

    // Apply optional refraction
    if (HasClearcoatRefraction())
    {
        lightWS = -Refract(lightWS, bsdfData.clearcoatNormalWS, 1.0 / bsdfData.clearcoatIOR);
        viewWS = preLightData.viewWS_UnderCoat;
    }

    refractedRatio = (1 - Fin) * (1 - Fout);
}


//-----------------------------------------------------------------------------
// bake lighting function
//-----------------------------------------------------------------------------

// This define allow to say that we implement a ModifyBakedDiffuseLighting function to be call in PostInitBuiltinData
#define MODIFY_BAKED_DIFFUSE_LIGHTING

void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, SurfaceData surfaceData, inout BuiltinData builtinData)
{
    // To get the data we need to do the whole process - compiler should optimize everything
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);
    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    // Note: When baking reflection probes, we approximate the diffuse with the fresnel0
#ifdef _AXF_BRDF_TYPE_SVBRDF
    builtinData.bakeDiffuseLighting *= GetDiffuseIndirectDimmer() * preLightData.diffuseFGD * GetDiffuseOrDefaultColor(bsdfData, _ReplaceDiffuseForIndirect).rgb;
#else
    // diffuse is Lambert, but we want the influence of the color still...
    // TODO_diffuseFGDColor
    builtinData.bakeDiffuseLighting *= GetDiffuseIndirectDimmer() * preLightData.singleBRDFColor * GetDiffuseOrDefaultColor(bsdfData, _ReplaceDiffuseForIndirect).rgb;
    // debugtest
    //builtinData.bakeDiffuseLighting *= 0;
#endif
    // todo_energy: attenuate diffuse lighting for coat ie with (1.0 - preLightData.coatFGD)
}

//-----------------------------------------------------------------------------
// light transport functions
//-----------------------------------------------------------------------------
LightTransportData  GetLightTransportData(SurfaceData surfaceData, BuiltinData builtinData, BSDFData bsdfData)
{
    LightTransportData lightTransportData;

    lightTransportData.diffuseColor = GetColorBaseDiffuse(bsdfData);
    lightTransportData.emissiveColor = float3(0.0, 0.0, 0.0);

    return lightTransportData;
}

//-----------------------------------------------------------------------------
// LightLoop related function (Only include if required)
// HAS_LIGHTLOOP is define in Lighting.hlsl
//-----------------------------------------------------------------------------

#ifdef HAS_LIGHTLOOP

//-----------------------------------------------------------------------------
// BSDF shared between directional light, punctual light and area light (reference)
//-----------------------------------------------------------------------------

// Same for all shading models.
bool IsNonZeroBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    float NdotL = dot(bsdfData.normalWS, L);

    return NdotL > 0.0;
}

#ifdef _AXF_BRDF_TYPE_SVBRDF

float3 ComputeWard(float3 H, float LdotH, float NdotL, float NdotV, PreLightData preLightData, BSDFData bsdfData)
{

    // Evaluate Fresnel term
    float  F = 1.0;
    switch (_SVBRDF_BRDFVariants & 3)
    {
    case 1: F_FresnelDieletricSafe(Fresnel0ToIorSafe(bsdfData.fresnelF0.r), LdotH); break;
    case 2: F = F_Schlick(bsdfData.fresnelF0.r, LdotH); break;
    }

    // Evaluate normal distribution function
    float3  tsH = float3(dot(H, bsdfData.tangentWS), dot(H, bsdfData.biTangentWS), dot(H, bsdfData.normalWS));
    //float2  rotH = tsH.xy / tsH.z;
    float2  rotH = tsH.xy / max(0.00001, tsH.z);
    //float2  roughness = bsdfData.roughness;
    float2  roughness = max(0.0001, bsdfData.roughness);
    //if (bsdfData.roughness.y == 0.0) bsdfData.specularColor = float3(1,0,0);

    if (roughness.x * roughness.y <= 0.0001 && tsH.z < 1.0) 
    {
        return 0;
    }
    
    float   N = exp(-Sq(rotH.x / roughness.x) - Sq(rotH.y / roughness.y));
    N /= max(0.0001, PI * roughness.x * roughness.y);
    //N /= (PI * roughness.x * roughness.y);    

    switch ((_SVBRDF_BRDFVariants >> 2) & 3)
    {
    case 0: N /= max(0.0001, 4.0 * Sq(LdotH) * Sq(Sq(tsH.z))); break; // Moroder
    case 1: N /= max(0.0001, 4.0 * NdotL * NdotV); break;             // Duer
    case 2: N /= max(0.0001, 4.0 * sqrt(NdotL * NdotV)); break;       // Ward
    }

    return bsdfData.specularColor * F * N;
}

float3  ComputeBlinnPhong(float3 H, float LdotH, float NdotL, float NdotV, PreLightData preLightData, BSDFData bsdfData)
{
    float2  exponents = exp2(bsdfData.roughness);

    // Evaluate normal distribution function
    float3  tsH = float3(dot(H, bsdfData.tangentWS), dot(H, bsdfData.biTangentWS), dot(H, bsdfData.normalWS));
    float2  rotH = tsH.xy;

    float3  N = 0;
    switch ((_SVBRDF_BRDFVariants >> 4) & 3)
    {
    case 0:
    {   // Ashikmin-Shirley
        N = sqrt((1 + exponents.x) * (1 + exponents.y)) / (8 * PI)
            * PositivePow(saturate(tsH.z), SafeDiv( (exponents.x * Sq(rotH.x) + exponents.y * Sq(rotH.y)), (1 - Sq(tsH.z)) ) )
            / (LdotH * max(NdotL, NdotV));
        break;
    }

    case 1:
    {   // Blinn
        float   exponent = 0.5 * (exponents.x + exponents.y);    // Should be isotropic anyway...
        N = (exponent + 2) / (8 * PI)
            * PositivePow(saturate(tsH.z), exponent);
        break;
    }

    case 2: // VRay
    case 3: // Lewis
        N = 1000 * float3(1, 0, 1);   // Not documented...
        break;
    }

    return bsdfData.specularColor * N;
}

float3  ComputeCookTorrance(float3 H, float LdotH, float NdotL, float NdotV, PreLightData preLightData, BSDFData bsdfData)
{
    float   NdotH = dot(H, bsdfData.normalWS);
    float   sqNdotH = Sq(NdotH);

    // Evaluate Fresnel term
    float  F = F_Schlick(bsdfData.fresnelF0.r, LdotH);

    // Evaluate (isotropic) normal distribution function (Beckmann)
    float   roughness = GetScalarRoughnessFromAnisoRoughness(bsdfData.roughness.x, bsdfData.roughness.y);
    float   sqAlpha = roughness*roughness;
    float   N = exp((sqNdotH - 1) / max(0.00001, sqNdotH * sqAlpha))
        / max(0.00001, PI * Sq(sqNdotH) * sqAlpha);

    // Evaluate shadowing/masking term
    float   G = G_CookTorrance(NdotH, NdotV, NdotL, LdotH);

    return bsdfData.specularColor * F * N * G;
}

float3  ComputeGGX(float3 H, float LdotH, float NdotL, float NdotV, PreLightData preLightData, BSDFData bsdfData)
{
    // Evaluate Fresnel term
    float   F = F_Schlick(bsdfData.fresnelF0.r, LdotH);

    float3  tsH = float3(dot(H, bsdfData.tangentWS), dot(H, bsdfData.biTangentWS), dot(H, bsdfData.normalWS));

    // Evaluate normal distribution function (Trowbridge-Reitz)
    float N = D_GGXAniso(tsH.x, tsH.y, tsH.z, bsdfData.roughness.x, bsdfData.roughness.y);

    // Evaluate shadowing/masking term
    float roughness = GetProjectedRoughness(tsH.x, tsH.y, tsH.z, bsdfData.roughness.x, bsdfData.roughness.y);

    // G1 in the SDK matches up with
    // Ref: Microfacet Models for Refraction through Rough Surfaces, Walter et al. 2007, p. 7 eq(34)
    // Ref: Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs, Heitz, 2014, p. 84 (37/60)
    // We have G1(NdotV, a) where a is roughness
    //                     = 2 * NdotV / (NdotV + sqrt(a*a + (1 - a*a) * Sq(NdotV)))
    //                     = 1 / (0.5 + 0.5 * sqrt(a*a/Sq(NdotV) + (1 - a*a)))
    //                     = 1 / (0.5 + 0.5 * sqrt((1/Sq(NdotV) - 1)*a*a + 1))
    // which we have defined as G_MaskingSmithGGX() in  core/ShaderLibrary/BSDF.hlsl
    float   G = G_MaskingSmithGGX(NdotL, roughness) * G_MaskingSmithGGX(NdotV, roughness);
    G /= max(0.00001, 4.0 * NdotL * NdotV);

    return bsdfData.specularColor * F * N * G;
}

float3  ComputePhong(float3 H, float LdotH, float NdotL, float NdotV, PreLightData preLightData, BSDFData bsdfData)
{
    return 1000 * float3(1, 0, 1);
}


// This function applies the BSDF. Assumes that NdotL is positive.
//_AXF_BRDF_TYPE_SVBRDF version:
CBSDF EvaluateBSDF(float3 viewWS_Clearcoat, float3 lightWS_Clearcoat, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    float NdotL;

    float3 viewWS_UnderCoat = viewWS_Clearcoat;   //Note: ComputeClearcoatReflectionAndExtinction_UsePreLightData possibly modifies its input directions.
    float3 lightWS_UnderCoat = lightWS_Clearcoat;

    // Compute half vector used by various components of the BSDF
    float3  H = normalize(viewWS_Clearcoat + lightWS_Clearcoat);

    // Apply clearcoat
    float  clearcoatExtinction = 1.0;
    float3  clearcoatReflectionLobeNdotL = 0.0;
    if (HasClearcoat())
    {
        NdotL = dot(bsdfData.clearcoatNormalWS, lightWS_Clearcoat);
        float coatNdotH = dot(bsdfData.clearcoatNormalWS, H);
        float coatNdotV = ClampNdotV(preLightData.NdotV_Clearcoat);

        float reflectionCoeff;
        ComputeClearcoatReflectionAndExtinction_UsePreLightData(viewWS_UnderCoat, lightWS_UnderCoat, bsdfData, preLightData, reflectionCoeff, clearcoatExtinction);
        if (HasClearcoatRefraction())
        {
            // Recompute H after possible refraction:
            H = normalize(viewWS_UnderCoat + lightWS_UnderCoat);
        }

        // See axf-decoding-sdk/doc/html/page1.html#svbrdf_subsec03
        // the coat is an almost-dirac BSDF lobe like expected.
        // There's nothing said about clearcoatColor, and it doesn't make sense to actually color its reflections but we
        // treat clearcoatColor as other specular colors (as the AxF SVBRDF model includes both a general coloring term
        // that they call "specular color" while the f0 is actually another term)
        clearcoatReflectionLobeNdotL = saturate(NdotL) * bsdfData.clearcoatColor * reflectionCoeff * DV_SmithJointGGX(coatNdotH, NdotL, coatNdotV, CLEAR_COAT_ROUGHNESS, preLightData.coatPartLambdaV);
    }

    // undercoat values:
    float   NdotH = dot(bsdfData.normalWS, H);
    float   NdotV = ClampNdotV(preLightData.NdotV_UnderCoat);

    // Compute rest of needed cosine of angles after possible refraction:
    float   LdotH = dot(H, lightWS_UnderCoat);
    NdotL = dot(bsdfData.normalWS, lightWS_UnderCoat);

    // Compute diffuse term
    float3  diffuseTerm = Lambert();
    if (_SVBRDF_BRDFType & 1)
    {
        float   diffuseRoughness = 0.5 * HALF_PI; // Arbitrary roughness (not specified in the documentation...)
        diffuseTerm = INV_PI * OrenNayar(bsdfData.normalWS, viewWS_UnderCoat, lightWS_UnderCoat, diffuseRoughness);
    }

    // Compute specular term
    float3  specularTerm = float3(1, 0, 0);
    switch ((_SVBRDF_BRDFType >> 1) & 7)
    {
    case 0: specularTerm = ComputeWard(H, LdotH, NdotL, NdotV, preLightData, bsdfData); break;
    case 1: specularTerm = ComputeBlinnPhong(H, LdotH, NdotL, NdotV, preLightData, bsdfData); break;
    case 2: specularTerm = ComputeCookTorrance(H, LdotH, NdotL, NdotV, preLightData, bsdfData); break;
    case 3: specularTerm = ComputeGGX(H, LdotH, NdotL, NdotV, preLightData, bsdfData); break;
    case 4: specularTerm = ComputePhong(H, LdotH, NdotL, NdotV, preLightData, bsdfData); break;
    default:    // @TODO
        specularTerm = 1000 * float3(1, 0, 1);
        break;
    }

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    cbsdf.diffR = clearcoatExtinction * diffuseTerm * saturate(NdotL);
    cbsdf.specR = (clearcoatExtinction * specularTerm * saturate(NdotL) + clearcoatReflectionLobeNdotL);

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    return cbsdf;
}

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)


float3 GetCarPaintSpecularFGDForLobe(PreLightData preLightData, uint lobeIndex)
{
    return lerp(preLightData.specularCTFGDAtZeroF0[lobeIndex], preLightData.specularCTFGDReflectivity[lobeIndex], _CarPaint2_CTF0s[lobeIndex]*preLightData.singleBRDFColor);
}


// This function applies the BSDF. Assumes that NdotL is positive.
// For _AXF_BRDF_TYPE_CAR_PAINT
CBSDF EvaluateBSDF(float3 viewWS_Clearcoat, float3 lightWS_Clearcoat, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);
    //debugtest
    //return cbsdf;
#if 0
    //cbsdf.diffR = Lambert() * saturate(dot(bsdfData.normalWS, lightWS_Clearcoat));
    //return cbsdf;

#elif 1

    float NdotL;

    float3 viewWS_UnderCoat = viewWS_Clearcoat;   //Note: ComputeClearcoatReflectionAndExtinction_UsePreLightData possibly modifies its input directions.
    float3 lightWS_UnderCoat = lightWS_Clearcoat;

    // Compute half vector used by various components of the BSDF
    float3  H = normalize(viewWS_Clearcoat + lightWS_Clearcoat);

    // Apply clearcoat
    float  clearcoatExtinction = 1.0;
    float3  clearcoatReflectionLobeNdotL = 0.0;
    if (HasClearcoat())
    {
        NdotL = dot(bsdfData.clearcoatNormalWS, lightWS_Clearcoat);
        float coatNdotH = dot(bsdfData.clearcoatNormalWS, H);
        float coatNdotV = ClampNdotV(preLightData.NdotV_Clearcoat);

        float reflectionCoeff;
        ComputeClearcoatReflectionAndExtinction_UsePreLightData(viewWS_UnderCoat, lightWS_UnderCoat, bsdfData, preLightData, reflectionCoeff, clearcoatExtinction);
        if (HasClearcoatRefraction())
        {
            // Recompute H after possible refraction:
            H = normalize(viewWS_UnderCoat + lightWS_UnderCoat);
        }

        // See axf-decoding-sdk/doc/html/page1.html#svbrdf_subsec03
        // the coat is an almost-dirac BSDF lobe like expected.
        // There's nothing said about clearcoatColor, and it doesn't make sense to actually color its reflections but we
        // treat clearcoatColor as other specular colors (as the AxF SVBRDF model includes both a general coloring term
        // that they call "specular color" while the f0 is actually another term)
        clearcoatReflectionLobeNdotL = saturate(NdotL) * bsdfData.clearcoatColor * reflectionCoeff * DV_SmithJointGGX(coatNdotH, NdotL, coatNdotV, CLEAR_COAT_ROUGHNESS, preLightData.coatPartLambdaV);
    }

    // undercoat values:
    float   NdotH = dot(bsdfData.normalWS, H);
    float   NdotV = ClampNdotV(preLightData.NdotV_UnderCoat);

    // Compute rest of needed cosine of angles after possible refraction:
    float LdotH = dot(H, lightWS_UnderCoat);
    float VdotH = LdotH;
    NdotL = dot(bsdfData.normalWS, lightWS_UnderCoat);

    float   thetaH = acos(clamp(NdotH, 0, 1));
    float   thetaD = acos(clamp(LdotH, 0, 1));

    // Simple lambert
    float3  diffuseTerm = Lambert();

    // Apply multi-lobes Cook-Torrance
    float3  specularTerm = MultiLobesCookTorrance(NdotL, NdotV, NdotH, VdotH);

    // Apply BRDF color
    float3  BRDFColor = GetBRDFColor(thetaH, thetaD);
    diffuseTerm *= BRDFColor; // tocheck: dont forget handling BRDFColor for the indirect diffuse lighting!
                              // Also note that the monochromatic bsdfData.diffuseColor (in the case of CARPAINT2)
                              // is still applied in PostEvaluateBSDF and not here, like in the SVBRDF case!
    specularTerm *= BRDFColor;

    // Apply flakes
    //TODO_FLAKES
    specularTerm += CarPaint_BTF(thetaH, thetaD, bsdfData);

    cbsdf.diffR = clearcoatExtinction * diffuseTerm * saturate(NdotL);
    cbsdf.specR = (clearcoatExtinction * specularTerm * saturate(NdotL) + clearcoatReflectionLobeNdotL);

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    return cbsdf;
#endif // #if 0
}

#else

// No _AXF_BRDF_TYPE

// This function applies the BSDF. Assumes that NdotL is positive.
CBSDF EvaluateBSDF(float3 viewWS_UnderCoat, float3 lightWS_UnderCoat, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    float NdotL = dot(bsdfData.normalWS, lightWS_UnderCoat);
    float diffuseTerm = Lambert();

    cbsdf.diffR = diffuseTerm * saturate(NdotL);

    // We don't multiply by 'bsdfData.diffuseColor' here. It's done only once in PostEvaluateBSDF().
    return cbsdf;
}

#endif // _AXF_BRDF_TYPE_SVBRDF

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
    //return (DirectLighting)0;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Punctual (supports spot, point and projector lights)
//-----------------------------------------------------------------------------

DirectLighting EvaluateBSDF_Punctual(LightLoopContext lightLoopContext,
                                     float3 V, PositionInputs posInput,
                                     PreLightData preLightData, LightData lightData,
                                     BSDFData bsdfData, BuiltinData builtinData)
{
    return ShadeSurface_Punctual(lightLoopContext, posInput, builtinData,
                                 preLightData, lightData, bsdfData, V);
}

//-----------------------------------------------------------------------------
// AREA LIGHTS
//-----------------------------------------------------------------------------

// ------ HELPERS ------

// Computes the best light direction given an initial light direction
// The direction will be projected onto the area light's plane and clipped by the rectangle's bounds, the resulting normalized vector is returned
//
//  lightPositionRWS, the rectangular area light's position in local space (i.e. relative to the point currently being lit)
//  lightWS, the light direction in world-space
//
float3  ComputeBestLightDirection_Rectangle(float3 lightPositionRWS, float3 lightWS, LightData lightData)
{
    float   halfWidth = lightData.size.x * 0.5;
    float   halfHeight = lightData.size.y * 0.5;

    float   t = dot(lightPositionRWS, lightData.forward) / dot(lightWS, lightData.forward);                  // Distance until we intercept the light plane following light direction
    float3  hitPosLS = t * lightWS;                                                                             // Position of intersection with light plane
    float2  hitPosTS = float2(dot(hitPosLS, lightData.right), dot(hitPosLS, lightData.up));               // Same but in tangent space
    hitPosTS = clamp(hitPosTS, float2(-halfWidth, -halfHeight), float2(halfWidth, halfHeight));   // Clip to rectangle
    hitPosLS = lightWS + hitPosTS.x * lightData.right + hitPosTS.y * lightData.up;                              // Recompose clipped intersection
    return normalize(hitPosLS);                                                                               // Now use that direction as best light vector
}

// Computes the best light direction given an initial light direction
// The direction will be projected onto the area light's line and clipped by the segment's bounds, the resulting normalized vector is returned
//
//  lightPositionRWS, the linear area light's position in local space (i.e. relative to the point currently being lit)
//  lightWS, the light direction in world-space
//
float3  ComputeBestLightDirection_Line(float3 lightPositionRWS, float3 lightWS, LightData lightData)
{

    return lightWS;

    //    float   len = lightData.size.x;
    //    float3  T   = lightData.right;
    //
    //
    //    float   t = dot(lightPositionRWS, lightData.forward) / dot(lightWS, lightData.forward);                  // Distance until we intercept the light plane following light direction
    //    float3  hitPosLS = t * lightWS;                                                                             // Position of intersection with light plane
    //    float2  hitPosTS = float2(dot(hitPosLS, lightData.right), dot(hitPosLS, lightData.up));               // Same but in tangent space
    //            hitPosTS = clamp(hitPosTS, float2(-halfWidth, -halfHeight), float2(halfWidth, halfHeight));   // Clip to rectangle
    //    hitPosLS = lightWS + hitPosTS.x * lightData.right + hitPosTS.y * lightData.up;                              // Recompose clipped intersection
    //    return normalize(hitPosLS);                                                                               // Now use that direction as best light vector
}

// Expects non-normalized vertex positions.
// Same as regular PolygonIrradiance found in AreaLighting.hlsl except I need the form factor F
// (cf. http://blog.selfshadow.com/publications/s2016-advances/s2016_ltc_rnd.pdf pp. 92 for an explanation on the meaning of that sphere approximation)
real PolygonIrradiance(real4x3 L, out float3 F)
{
    UNITY_UNROLL
        for (uint i = 0; i < 4; i++)
        {
            L[i] = normalize(L[i]);
        }

    F = 0.0;

    UNITY_UNROLL
        for (uint edge = 0; edge < 4; edge++)
        {
            real3 V1 = L[edge];
            real3 V2 = L[(edge + 1) % 4];

            F += INV_TWO_PI * ComputeEdgeFactor(V1, V2);
        }

    // Clamp invalid values to avoid visual artifacts.
    real f2 = saturate(dot(F, F));
    real sinSqSigma = min(sqrt(f2), 0.999);
    real cosOmega = clamp(F.z * rsqrt(f2), -1, 1);

    return DiffuseSphereLightIrradiance(sinSqSigma, cosOmega);
}


//-----------------------------------------------------------------------------
// EvaluateBSDF_Line - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

DirectLighting  EvaluateBSDF_Line(  LightLoopContext lightLoopContext,
                                    float3 viewWS_Clearcoat, PositionInputs posInput,
                                    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3  positionWS = posInput.positionWS;

    float   len = lightData.size.x;
    float3  T = lightData.right;

    float3  unL = lightData.positionRWS - positionWS;

    // Pick the major axis of the ellipsoid.
    float3  axis = lightData.right;

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
        return lighting;

    lightData.diffuseDimmer *= intensity;
    lightData.specularDimmer *= intensity;

    // Translate the light s.t. the shaded point is at the origin of the coordinate system.
    float3  lightPositionRWS = lightData.positionRWS - positionWS;

    // TODO: some of this could be precomputed.
    float3  P1 = lightPositionRWS - T * (0.5 * len);
    float3  P2 = lightPositionRWS + T * (0.5 * len);

    // Rotate the endpoints into the local coordinate system.
    P1 = mul(P1, transpose(preLightData.orthoBasisViewNormal));
    P2 = mul(P2, transpose(preLightData.orthoBasisViewNormal));

    // Compute the binormal in the local coordinate system.
    float3  B = normalize(cross(P1, P2));

    float   ltcValue;

    //-----------------------------------------------------------------------------
#if defined(_AXF_BRDF_TYPE_SVBRDF)

    // Evaluate the diffuse part
    // Polygon irradiance in the transformed configuration.
    ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformDiffuse);
    ltcValue *= lightData.diffuseDimmer;
    lighting.diffuse = preLightData.diffuseFGD * ltcValue;

    // Evaluate the specular part
    // Polygon irradiance in the transformed configuration.
    ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformSpecular);
    ltcValue *= lightData.specularDimmer;
    lighting.specular = preLightData.specularFGD * ltcValue;

    //-----------------------------------------------------------------------------
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

    float   NdotV = ClampNdotV(preLightData.NdotV_UnderCoat);

    //-----------------------------------------------------------------------------
    // Use Lambert for diffuse
    ltcValue = LTCEvaluate(P1, P2, B, k_identity3x3);    // No transform: Lambert uses identity
    ltcValue *= lightData.diffuseDimmer;
    lighting.diffuse = ltcValue; // no FGD, lambert gives 1

    // Evaluate a BRDF color response in diffuse direction
    // We project the point onto the area light's plane using the light's forward direction and recompute the light direction from this position
    // todo_dir
#if 0
    float3  bestLightWS_Diffuse = ComputeBestLightDirection_Line(lightPositionRWS, -lightData.forward, lightData);

    // todo_dir todo_pseudorefract
    // refract light dir here for GetBRDFColor since it is a fresnel-like effect, but
    // compute LTC / env fetching using *non refracted dir*

    float3  H = normalize(preLightData.viewWS_UnderCoat + bestLightWS_Diffuse);
    float   NdotH = dot(bsdfData.normalWS, H);
    float   VdotH = dot(preLightData.viewWS_UnderCoat, H);

    float   thetaH = acos(clamp(NdotH, 0, 1));
    float   thetaD = acos(clamp(VdotH, 0, 1));
#else
    // Just use the same assumptions as for environments:
    float   thetaH = 0;
    float   thetaD = acos(clamp(preLightData.NdotV_UnderCoat, 0, 1));
#endif
    lighting.diffuse *= preLightData.singleBRDFColor;


    //-----------------------------------------------------------------------------
    // Evaluate multi-lobes Cook-Torrance
    // Each CT lobe samples the environment with the appropriate roughness
    for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
    {
        float   coeff = GetLTCAreaLightDimmer() * _CarPaint2_CTCoeffs[lobeIndex];
        ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformSpecularCT[lobeIndex]);
        lighting.specular += coeff * GetCarPaintSpecularFGDForLobe(preLightData, lobeIndex) * ltcValue;
    }
    lighting.specular *= lightData.specularDimmer;

    // Evaluate a BRDF color response in specular direction
    // We project the point onto the area light's plane using the reflected view direction and recompute the light direction from this position
    // todo_dir:
#if 0
    float3  bestLightWS_Specular = ComputeBestLightDirection_Line(lightPositionRWS, preLightData.iblDominantDirectionWS_UnderCoat, lightData);

    // todo_dir todo_pseudorefract
    // refract light dir here for GetBRDFColor since it is a fresnel-like effect, but
    // compute LTC / env fetching using *non refracted dir*
    H = normalize(preLightData.viewWS_UnderCoat + bestLightWS_Specular);
    NdotH = dot(bsdfData.normalWS, H);
    VdotH = dot(preLightData.viewWS_UnderCoat, H);

    thetaH = acos(clamp(NdotH, 0, 1));
    thetaD = acos(clamp(VdotH, 0, 1));
#else
    // Just use the same assumptions as for environments 
    // (already calculated thetaH and thetaD above)
#endif
    //now already in rebuilt specularFGD: lighting.specular *= GetBRDFColor(thetaH, thetaD);


    //-----------------------------------------------------------------------------
    // Sample flakes as tiny mirrors
    // (update1: this is not really doing that, more like applying a BTF on a
    // lobe following the top normalmap. For them being like tiny mirrors, you would
    // need the N of the flake, and then you end up with the problem of normal aliasing)
    // (See also #define FLAKES_JUST_BTF, which makes us use the coat ltc transform and no FGD,
    // - TODO in that case calculated irradiance should be the same as clearcoat, should be optimized)
    // todo_dir NdotV wrong
    ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformFlakes);
    ltcValue *= lightData.specularDimmer;

    lighting.specular += ltcValue * preLightData.singleFlakesComponent; //preLightData.flakesFGD * CarPaint_BTF(thetaH, thetaD, bsdfData);

#endif

    //-----------------------------------------------------------------------------

    // Evaluate the clear-coat
    if (HasClearcoat())
    {

        // Use the complement of FGD value as an approximation of the extinction of the undercoat
        float3  clearcoatExtinction = 1.0 - preLightData.coatFGD;

        // Apply clear-coat extinction to existing lighting
        lighting.diffuse *= clearcoatExtinction;
        lighting.specular *= clearcoatExtinction;

        // Then add clear-coat contribution
        ltcValue = LTCEvaluate(P1, P2, B, preLightData.ltcTransformClearcoat);
        ltcValue *= lightData.specularDimmer;
        lighting.specular += preLightData.coatFGD * ltcValue * bsdfData.clearcoatColor;
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
        lighting.diffuse *= PI * lightData.diffuseDimmer;
    }
#endif

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_Area - Approximation with Linearly Transformed Cosines
//-----------------------------------------------------------------------------

// #define ELLIPSOIDAL_ATTENUATION

DirectLighting  EvaluateBSDF_Rect(LightLoopContext lightLoopContext,
    float3 viewWS_Clearcoat, PositionInputs posInput,
    PreLightData preLightData, LightData lightData, BSDFData bsdfData, BuiltinData builtinData)
{
    DirectLighting lighting;
    ZERO_INITIALIZE(DirectLighting, lighting);

    float3  positionWS = posInput.positionWS;

#if SHADEROPTIONS_BARN_DOOR
    // Apply the barn door modification to the light data
    RectangularLightApplyBarnDoor(lightData, positionWS);
#endif
    // Translate the light s.t. the shaded point is at the origin of the coordinate system.
    float3  lightPositionRWS = lightData.positionRWS - positionWS;
    if (dot(lightData.forward, lightPositionRWS) >= 0.0001)
    {
        return lighting;    // The light is back-facing.
    }

    // Rotate the light direction into the light space.
    float3x3    lightToWorld = float3x3(lightData.right, lightData.up, -lightData.forward);
    float3      unL = mul(lightPositionRWS, transpose(lightToWorld));

    // TODO: This could be precomputed.
    float   halfWidth = lightData.size.x * 0.5;
    float   halfHeight = lightData.size.y * 0.5;

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
    if (intensity == 0.0)
        return lighting;

    //TOCHECK, had to fix this:
    // Translate the light s.t. the shaded point is at the origin of the coordinate system.
    lightData.positionRWS -= positionWS;

    lightData.diffuseDimmer *= intensity;
    lightData.specularDimmer *= intensity;

    // TODO: some of this could be precomputed.
    float4x3    lightVerts;
    lightVerts[0] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up * -halfHeight; // LL
    lightVerts[1] = lightData.positionRWS + lightData.right * -halfWidth + lightData.up *  halfHeight; // UL
    lightVerts[2] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up *  halfHeight; // UR
    lightVerts[3] = lightData.positionRWS + lightData.right *  halfWidth + lightData.up * -halfHeight; // LR

    // Rotate the endpoints into tangent space
    lightVerts = mul(lightVerts, transpose(preLightData.orthoBasisViewNormal));

    float   ltcValue;

    //-----------------------------------------------------------------------------

#if defined(_AXF_BRDF_TYPE_SVBRDF)

    // Evaluate the diffuse part
    // Polygon irradiance in the transformed configuration.
    ltcValue = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformDiffuse));
    ltcValue *= lightData.diffuseDimmer;
    lighting.diffuse = preLightData.diffuseFGD * ltcValue;


    // Evaluate the specular part
    // Polygon irradiance in the transformed configuration.
    ltcValue = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformSpecular));
    ltcValue *= lightData.specularDimmer;
    lighting.specular = preLightData.specularFGD * ltcValue;

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

    float   NdotV = ClampNdotV(preLightData.NdotV_UnderCoat);
    // TODO_dir: refract light dir for GetBRDFColor like for FGD since it is a fresnel-like effect, but
    // compute LTC / env fetching using *non refracted dir*

    //-----------------------------------------------------------------------------
    // Use Lambert for diffuse
//        float3  bestLightWS_Diffuse;
//        ltcValue  = PolygonIrradiance(lightVerts, bestLightWS_Diffuse);    // No transform: Lambert uses identity
//        bestLightWS_Diffuse = normalize(bestLightWS_Diffuse);
    ltcValue = PolygonIrradiance(lightVerts);    // No transform: Lambert uses identity
    ltcValue *= lightData.diffuseDimmer;
    lighting.diffuse = ltcValue;

    // Evaluate a BRDF color response in diffuse direction
    // We project the point onto the area light's plane using the light's forward direction and recompute the light direction from this position
    //TODO_dir:
#if 0
    float3  bestLightWS_Diffuse = ComputeBestLightDirection_Rectangle(lightPositionRWS, -lightData.forward, lightData);

    // TODO_dir: refract light dir for GetBRDFColor here since it is a fresnel-like effect, but
    // compute LTC / env fetching using *non refracted dir*

    float3  H = normalize(preLightData.viewWS_UnderCoat + bestLightWS_Diffuse);
    float   NdotH = dot(bsdfData.normalWS, H);
    float   VdotH = dot(preLightData.viewWS_UnderCoat, H);

    float   thetaH = acos(clamp(NdotH, 0, 1));
    float   thetaD = acos(clamp(VdotH, 0, 1));
#else
    // Just use the same assumptions as for environments:
    float   thetaH = 0;
    float   thetaD = acos(clamp(preLightData.NdotV_UnderCoat, 0, 1));
#endif

    lighting.diffuse *= preLightData.singleBRDFColor; //GetBRDFColor(thetaH, thetaD);


    //-----------------------------------------------------------------------------
    // Evaluate multi-lobes Cook-Torrance
    // Each CT lobe samples the environment with the appropriate roughness
    for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
    {
        float   coeff = GetLTCAreaLightDimmer() * _CarPaint2_CTCoeffs[lobeIndex];
        ltcValue = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformSpecularCT[lobeIndex]));
        lighting.specular += coeff * GetCarPaintSpecularFGDForLobe(preLightData, lobeIndex) * ltcValue;
    }
    lighting.specular *= lightData.specularDimmer;

    // Evaluate a BRDF color response in specular direction
    // We project the point onto the area light's plane using the reflected view direction and recompute the light direction from this position
    // TODO_dir: 
#if 0
    float3  bestLightWS_Specular = ComputeBestLightDirection_Rectangle(lightPositionRWS, preLightData.iblDominantDirectionWS_UnderCoat, lightData);

    // TODO_dir: refract light dir for GetBRDFColor here since it is a fresnel-like effect, but
    // compute LTC / env fetching using *non refracted dir*

    H = normalize(preLightData.viewWS_UnderCoat + bestLightWS_Specular);
    NdotH = dot(bsdfData.normalWS, H);
    VdotH = dot(preLightData.viewWS_UnderCoat, H);

    thetaH = acos(clamp(NdotH, 0, 1));
    thetaD = acos(clamp(VdotH, 0, 1));
#else
    // Just use the same assumptions as for environments 
    // (already calculated thetaH and thetaD above)
#endif

    //now already in rebuilt specularFGD: lighting.specular *= GetBRDFColor(thetaH, thetaD);

    //-----------------------------------------------------------------------------
    // Sample flakes as tiny mirrors
    // TODO_dir NdotV wrong
    // (See also #define FLAKES_JUST_BTF, which makes us use the coat ltc transform and no FGD,
    // - TODO in that case calculated irradiance should be the same as clearcoat, should be optimized)
    ltcValue = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformFlakes));
    ltcValue *= lightData.specularDimmer;

    lighting.specular += ltcValue * preLightData.singleFlakesComponent; //preLightData.flakesFGD * CarPaint_BTF(thetaH, thetaD, bsdfData);

#endif


    //-----------------------------------------------------------------------------

    // Evaluate the clear-coat
    if (HasClearcoat())
    {

        // Use the complement of FGD value as an approximation of the extinction of the undercoat
        float3  clearcoatExtinction = 1.0 - preLightData.coatFGD;

        // Apply clear-coat extinction to existing lighting
        lighting.diffuse *= clearcoatExtinction;
        lighting.specular *= clearcoatExtinction;

        // Then add clear-coat contribution
        ltcValue = PolygonIrradiance(mul(lightVerts, preLightData.ltcTransformClearcoat));
        ltcValue *= lightData.specularDimmer;
        lighting.specular += preLightData.coatFGD * ltcValue * bsdfData.clearcoatColor;
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
        lighting.diffuse *= PI * lightData.diffuseDimmer;
    }
#endif

    return lighting;
}

DirectLighting  EvaluateBSDF_Area(LightLoopContext lightLoopContext,
    float3 viewWS, PositionInputs posInput,
    PreLightData preLightData, LightData lightData,
    BSDFData bsdfData, BuiltinData builtinData)
{

    if (lightData.lightType == GPULIGHTTYPE_TUBE)
    {
        return EvaluateBSDF_Line(lightLoopContext, viewWS, posInput, preLightData, lightData, bsdfData, builtinData);
    }
    else
    {
        return EvaluateBSDF_Rect(lightLoopContext, viewWS, posInput, preLightData, lightData, bsdfData, builtinData);
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

    float3 reflectanceFactor = 0.0;

    if (HasClearcoat())
    {
        reflectanceFactor = GetSSRDimmer() * bsdfData.clearcoatColor * preLightData.coatFGD;
        // TODO_flakes ?
    }
    else
    {
#if defined(_AXF_BRDF_TYPE_SVBRDF)
        reflectanceFactor = preLightData.specularFGD;

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
        // Like for environments, in that case, H is supposed N if we don't use
        // GetSpecularDominantDir. So NdotH = 1 and thetaH = 0.
        // V dot H is NdotV and we get thetaD from that.
        // preLightData.NdotV_UnderCoat == preLightData.NdotV_Clearcoat since
        // there's no clear coat.
        float   thetaH = 0;
        float   thetaD = acos(clamp(preLightData.NdotV_UnderCoat, 0, 1));

        for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
        {
            float   coeff = _CarPaint2_CTCoeffs[lobeIndex];
            reflectanceFactor += coeff * GetCarPaintSpecularFGDForLobe(preLightData, lobeIndex);
        }
        // TODO_flakes ?
        reflectanceFactor *= GetSSRDimmer(); //now already in rebuilt specularFGD: * GetBRDFColor(thetaH, thetaD);
#else
        // This is only possible if the AxF is a BTF type. However, there is a bunch of ifdefs do not support this third case
#endif
    }

    lighting.specularReflected = ssrLighting.rgb * reflectanceFactor;
    reflectionHierarchyWeight  = ssrLighting.a;

    return lighting;
}

IndirectLighting    EvaluateBSDF_ScreenspaceRefraction( LightLoopContext lightLoopContext,
                                                        float3 viewWS_Clearcoat, PositionInputs posInput,
                                                        PreLightData preLightData, BSDFData bsdfData,
                                                        EnvLightData _envLightData,
                                                        inout float hierarchyWeight)
{

    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    return lighting;
}


//-----------------------------------------------------------------------------
// EvaluateBSDF_Env
// ----------------------------------------------------------------------------
float GetEnvMipLevel(EnvLightData lightData, float iblPerceptualRoughness)
{
    float iblMipLevel;

    // TODO: We need to match the PerceptualRoughnessToMipmapLevel formula for planar, so we don't do this test (which is specific to our current lightloop)
    // Specific case for Texture2Ds, their convolution is a gaussian one and not a GGX one - So we use another roughness mip mapping.
    if (IsEnvIndexTexture2D(lightData.envIndex))
    {
        // Empirical remapping
        iblMipLevel = PlanarPerceptualRoughnessToMipmapLevel(iblPerceptualRoughness, _ColorPyramidScale.z);
    }
    else
    {
        iblMipLevel = PerceptualRoughnessToMipmapLevel(iblPerceptualRoughness);
    }
    return iblMipLevel;
}

float3 GetModifiedEnvSamplingDir(EnvLightData lightData, float3 N, float3 iblR, float iblPerceptualRoughness, float clampedNdotV)
{
    float3 ret = iblR;
    if (!IsEnvIndexTexture2D(lightData.envIndex)) // ENVCACHETYPE_CUBEMAP
    {
        // When we are rough, we tend to see outward shifting of the reflection when at the boundary of the projection volume
        // Also it appear like more sharp. To avoid these artifact and at the same time get better match to reference we lerp to original unmodified reflection.
        // Formula is empirical.
        ret = GetSpecularDominantDir(N, iblR, iblPerceptualRoughness, clampedNdotV);
        float iblRoughness = PerceptualRoughnessToRoughness(iblPerceptualRoughness);
        ret = lerp(ret, iblR, saturate(smoothstep(0, 1, iblRoughness * iblRoughness)));
    }
    return ret;
}

IndirectLighting EvaluateBSDF_Env_e(  LightLoopContext lightLoopContext,
                                    float3 viewWS_Clearcoat, PositionInputs posInput,
                                    PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                    int _influenceShapeType, int _GPUImageBasedLightingType,
                                    inout float hierarchyWeight)
{
    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);
    return lighting;
}

// _preIntegratedFGD and _CubemapLD are unique for each BRDF
IndirectLighting EvaluateBSDF_Env(  LightLoopContext lightLoopContext,
                                    float3 viewWS_Clearcoat, PositionInputs posInput,
                                    PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                    int _influenceShapeType, int _GPUImageBasedLightingType,
                                    inout float hierarchyWeight)
{

    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    if (_GPUImageBasedLightingType != GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
        return lighting;    // We don't support transmission

    float3  positionWS = posInput.positionWS;
    float   weight = 1.0;

    // TODO_dir: this shouldn't be undercoat.
    float3  environmentSamplingDirectionWS_UnderCoat = preLightData.iblDominantDirectionWS_UnderCoat;

#if defined(_AXF_BRDF_TYPE_SVBRDF)
    float3  envLighting = 0.0;

    
    float   NdotV = ClampNdotV(preLightData.NdotV_UnderCoat);

    environmentSamplingDirectionWS_UnderCoat = GetModifiedEnvSamplingDir(lightData, bsdfData.normalWS, preLightData.iblDominantDirectionWS_UnderCoat, preLightData.iblPerceptualRoughness, NdotV);

    // Note: using _influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, _influenceShapeType, environmentSamplingDirectionWS_UnderCoat, weight);

    float   IBLMipLevel;
    IBLMipLevel = GetEnvMipLevel(lightData, preLightData.iblPerceptualRoughness);

    // Sample the pre-integrated environment lighting
    float4  preLD = SampleEnv(lightLoopContext, lightData.envIndex, environmentSamplingDirectionWS_UnderCoat, IBLMipLevel, lightData.rangeCompressionFactorCompensation);
    weight *= preLD.w; // Used by planar reflection to discard pixel

    envLighting = GetSpecularIndirectDimmer() * preLightData.specularFGD * preLD.xyz;

    //-----------------------------------------------------------------------------
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

    float3  envLighting = 0.0;

    float   NdotV = ClampNdotV(preLightData.NdotV_UnderCoat);

    // A part of this BRDF depends on thetaH and thetaD and should thus have entered
    // the split sum pre-integration. We do a further approximation by pulling those 
    // terms out and evaluating them in the specular dominant direction,
    // for BRDFColor and flakes.
    float3  viewWS_UnderCoat = preLightData.viewWS_UnderCoat;
    float3  lightWS_UnderCoat = environmentSamplingDirectionWS_UnderCoat;

    float3  H = normalize(viewWS_UnderCoat + lightWS_UnderCoat);
    float   NdotH = dot(bsdfData.normalWS, H);
    float   VdotH = dot(viewWS_UnderCoat, H);

    // TODO_dir: so this is just thetaH = 0, etc. CHECK and remove.
    float   thetaH = acos(clamp(NdotH, 0, 1));
    float   thetaD = acos(clamp(VdotH, 0, 1));

    #if USE_COOK_TORRANCE_MULTI_LOBES

    // Multi-lobes approach
    // Each CT lobe samples the environment with the appropriate roughness
    float   sumWeights = 0.0;
    for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
    {
        float   coeff = _CarPaint2_CTCoeffs[lobeIndex];

        float   lobeMipLevel = PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness[lobeIndex]);
        float4  preLD = SampleEnv(lightLoopContext, lightData.envIndex, lightWS_UnderCoat, lobeMipLevel, lightData.rangeCompressionFactorCompensation);

        //todotodo: try removing coeff
        envLighting += coeff * GetCarPaintSpecularFGDForLobe(preLightData, lobeIndex) * preLD.xyz;
        sumWeights += preLD.w;
    }
    envLighting *= GetSpecularIndirectDimmer();
    //now already in rebuilt specularFGD: envLighting *= GetBRDFColor(thetaH, thetaD);

    // Sample flakes
    //TODO_FLAKES
    float   flakesMipLevel = 0;   // Flakes are supposed to be perfect mirrors
    //envLighting += preLightData.flakesFGD * CarPaint_BTF(thetaH, thetaD, bsdfData) * SampleEnv(lightLoopContext, lightData.envIndex, lightWS_UnderCoat, flakesMipLevel, lightData.rangeCompressionFactorCompensation).xyz;
    envLighting += preLightData.singleFlakesComponent * SampleEnv(lightLoopContext, lightData.envIndex, lightWS_UnderCoat, flakesMipLevel, lightData.rangeCompressionFactorCompensation).xyz;
    weight *= sumWeights / CARPAINT2_LOBE_COUNT;

    #else // USE_COOK_TORRANCE_MULTI_LOBES

    // Single lobe approach
    // We computed an average mip level stored in preLightData.iblPerceptualRoughness that we use for all CT lobes
    float   IBLMipLevel;
    IBLMipLevel = GetEnvMipLevel(lightData, preLightData.iblPerceptualRoughness);

    // Sample the actual environment lighting
    float4  preLD = SampleEnv(lightLoopContext, lightData.envIndex, lightWS_UnderCoat, IBLMipLevel, lightData.rangeCompressionFactorCompensation);
    float3  envLighting;

    envLighting = preLightData.specularCTFGDSingleLobe * GetSpecularIndirectDimmer() * GetBRDFColor(thetaH, thetaD);
    //TODO_FLAKES
    //envLighting += preLightData.flakesFGD * CarPaint_BTF(thetaH, thetaD, bsdfData);
    envLighting += preLightData.singleFlakesComponent;
    envLighting *= preLD.xyz;
    weight *= preLD.w; // Used by planar reflection to discard pixel

    #endif // USE_COOK_TORRANCE_MULTI_LOBES

//-----------------------------------------------------------------------------
#else // ..._AXF_BRDF_TYPE_CAR_PAINT

    float3  envLighting = 0; // error / unknown BRDF type

#endif // BRDF type

    //-----------------------------------------------------------------------------
    // Evaluate the clearcoat component if needed
    if (!IsDebugHideCoat() && HasClearcoat())
    {

        // Evaluate clearcoat sampling direction
        float   unusedWeight = 0.0;
        float3  lightWS_Clearcoat = preLightData.iblDominantDirectionWS_Clearcoat;
        EvaluateLight_EnvIntersection(positionWS, bsdfData.clearcoatNormalWS, lightData, _influenceShapeType, lightWS_Clearcoat, unusedWeight);

        // Attenuate environment lighting under the clearcoat by the complement to the Fresnel term
        //todo_energy: 
        envLighting *= 1.0 - preLightData.coatFGD;
        //envLighting *= Sq(1.0 - preLightData.coatFGD);

        // Then add the environment lighting reflected by the clearcoat (with mip level 0, like mirror)
        float4  preLD = SampleEnv(lightLoopContext, lightData.envIndex, lightWS_Clearcoat, 0.0, lightData.rangeCompressionFactorCompensation);
        envLighting += preLightData.coatFGD * preLD.xyz * bsdfData.clearcoatColor;
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
                        out float3 diffuseLighting, out float3 specularLighting)
{
    // There is no AmbientOcclusion from data with AxF, but let's apply our SSAO
    AmbientOcclusionFactor aoFactor;
    GetScreenSpaceAmbientOcclusionMultibounce(  posInput.positionSS, preLightData.NdotV_UnderCoat,
                                                RoughnessToPerceptualRoughness(GetScalarRoughnessFromAnisoRoughness(bsdfData.roughness.x, bsdfData.roughness.y)),
                                                1.0, 1.0, GetColorBaseDiffuse(bsdfData), GetColorBaseFresnelF0(bsdfData), aoFactor);
    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);

    diffuseLighting = bsdfData.diffuseColor * lighting.direct.diffuse + builtinData.bakeDiffuseLighting;
    specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#if !defined(_AXF_BRDF_TYPE_SVBRDF) && !defined(_AXF_BRDF_TYPE_CAR_PAINT)
    // Not supported: Display a flashy color instead
    diffuseLighting = 10 * float3(1, 0.3, 0.01);
#endif

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, diffuseLighting, specularLighting);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP

//
// WIP
//
// todo/tocheck_envsampling, envsampling_test, todotodo, 
// todo_dir todo_modes todo_pseudorefract
// todo_energy
// todo_fresnel
// debugtest (cur)
// todo_BeckmannToGGX
// TOCHECK
//
//
