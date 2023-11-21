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
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"

//-----------------------------------------------------------------------------
//
// Hardcoded config
//

//-----------------------------------------------------------------------------
// DEBUG
//#define DEBUG_HIDE_COAT

#if 1 // defined(SHADER_STAGE_RAY_TRACING)
// patch: in raytracing shader context, use float props that mirror the int props
// this is a workaround until we deal properly with the transfer of int properties in raytracing shaders on the engine side.

#define AXF_MATERIAL_FLAGS            (_FlagsB)

#define AXF_SVBRDF_BRDFTYPE_DIFFUSETYPE        (_SVBRDF_BRDFType_DiffuseType)
#define AXF_SVBRDF_BRDFTYPE_SPECULARTYPE       (_SVBRDF_BRDFType_SpecularType)
#define AXF_SVBRDF_BRDFVARIANTS_FRESNELTYPE    (_SVBRDF_BRDFVariants_FresnelType)
#define AXF_SVBRDF_BRDFVARIANTS_WARDTYPE       (_SVBRDF_BRDFVariants_WardType)
#define AXF_SVBRDF_BRDFVARIANTS_BLINNTYPE      (_SVBRDF_BRDFVariants_BlinnType)

#define AXF_CARPAINT2_FLAKEMAXTHETAI  (_CarPaint2_FlakeMaxThetaIF)
#define AXF_CARPAINT2_FLAKENUMTHETAF  (_CarPaint2_FlakeNumThetaFF)
#define AXF_CARPAINT2_FLAKENUMTHETAI  (_CarPaint2_FlakeNumThetaIF)

#else

#define AXF_MATERIAL_FLAGS                     (_Flags)

#define AXF_SVBRDF_BRDFTYPE_DIFFUSETYPE        ((_SVBRDF_BRDFType >> 0) & 1)
#define AXF_SVBRDF_BRDFTYPE_SPECULARTYPE       ((_SVBRDF_BRDFType >> 1) & 7)
#define AXF_SVBRDF_BRDFVARIANTS_FRESNELTYPE    ((_SVBRDF_BRDFVariants >> 0 ) & 3)
#define AXF_SVBRDF_BRDFVARIANTS_WARDTYPE       ((_SVBRDF_BRDFVariants >> 2)  & 3)
#define AXF_SVBRDF_BRDFVARIANTS_BLINNTYPE      ((_SVBRDF_BRDFVariants >> 4)  & 3)

#define AXF_CARPAINT2_FLAKEMAXTHETAI  (_CarPaint2_FlakeMaxThetaI)
#define AXF_CARPAINT2_FLAKENUMTHETAF  (_CarPaint2_FlakeNumThetaF)
#define AXF_CARPAINT2_FLAKENUMTHETAI  (_CarPaint2_FlakeNumThetaI)

#endif // defined(SHADER_STAGE_RAY_TRACING)


//-----------------------------------------------------------------------------

#define FORCE_DISABLE_LIGHT_TYPE_DIMMERS

// Comment to disable the BRDFColor table clamping (CARPAINT2 specific)
#define AUTO_PATCH_FOR_INCOMPLETE_BRDF_COLOR_TABLE // This requires importer version >= 0.1.5-preview and manually setting the diagonal clamping enable + scalings to offset the diagonal

// Uncomment to always consider carpaints as having a clearcoat:
//#define FORCE_CAR_PAINT_HAS_CLEARCOAT

#define NdotVMinCosSpread 0.0001 // ie this is the value used by ClampNdotV

#define FixedBRDFColorThetaHForIndirectLight (_CarPaint2_FixedColorThetaHForIndirectLight)
#define FixedFlakesThetaHForIndirectLight (_CarPaint2_FixedFlakesThetaHForIndirectLight)
//#define FixedThetaHForIndirectLight (0)

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

#ifdef _MAPPING_TRIPLANAR
#    define NB_FLAKES_RND_SHIFTS 3
#    define FLAKES_SHIFT_IDX_PLANAR_ZY (0)
#    define FLAKES_SHIFT_IDX_PLANAR_XZ (1)
#    define FLAKES_SHIFT_IDX_PLANAR_XY (2)
#else
#    define NB_FLAKES_RND_SHIFTS 1
#endif

// Define this to sample the environment maps/LTC samples for each lobe, instead of a single sample with an average lobe
#define USE_COOK_TORRANCE_MULTI_LOBES   1
#define MAX_CT_LOBE_COUNT 3
//#define CARPAINT2_LOBE_COUNT min(_CarPaint2_LobeCount,MAX_CT_LOBE_COUNT)
#define CARPAINT2_LOBE_COUNT MAX_CT_LOBE_COUNT


//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------
#define DIFFUSE_INDIRECT_FUDGE_FACTOR (1.0)
#define ENVIRONMENT_LD_FUDGE_FACTOR (1.0)
#define LTC_L_FUDGE_FACTOR (1.0)
#define SSR_L_FUDGE_FACTOR (1.0)

bool HasPhongTypeBRDF()
{
    uint type = AXF_SVBRDF_BRDFTYPE_SPECULARTYPE;
    return type == 1 || type == 4;
}

float2 AxFGetRoughnessFromSpecularLobeTexture(float2 specularLobe)
{
    // For Blinn-Phong, AxF encodes specularLobe.xy as log2(shiniExp_xy) so
    //     shiniExp = exp2(abs(specularLobe.xy))
    // A good fit for a corresponding Beckmann roughness is
    //     roughnessBeckmann^2 = 2 /(shiniExp + 2)
    // See eg
    // http://graphicrants.blogspot.com/2013/08/specular-brdf-reference.html
    // http://simonstechblog.blogspot.com/2011/12/microfacet-brdf.html

    // We thus have
    //     roughnessBeckmann = sqrt(2) * rsqrt(exp2(abs(specularLobe.xy)) + 2);
    //     shiniExp = 2 * rcp(max(0.0001,(roughnessBeckmann*roughnessBeckmann))) - 2;

    return (HasPhongTypeBRDF() ? (sqrt(2) * rsqrt(exp2(abs(specularLobe)) + 2)) : specularLobe);
}


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

// Used directly with an angle
float Refract(float inTheta, float eta)
{
    float sinout = saturate(sin(inTheta)*eta);
    return FastACosPos(sqrt(1-Sq(sinout)));
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
        // Note: sqrt(sinIncSq) shouldn't be close to 0, since b < 0 <=> (sinIncSq) > 1/Sq(eta) and eta shouldn't be close to 1/sqrt(eps)!

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

// Safe version preventing NaNs when IOR = 1
real F_FresnelDieletricSafe(real IOR, real u)
{
    u = max(1e-3, u); // Prevents NaNs
    real g = sqrt(max(0.0, Sq(IOR) + Sq(u) - 1.0));
    return 0.5 * Sq((g - u) / max(1e-4, g + u)) * (1.0 + Sq(((g + u) * u - 1.0) / ((g - u) * u + 1.0)));
}

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

// only for carpaint specular part
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
    return (AXF_SVBRDF_BRDFVARIANTS_FRESNELTYPE) != 0;
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
    return true;
#else
    return false;
#endif
}

bool HasAnisotropy()
{
    return (HasFlag(AXF_MATERIAL_FLAGS, FEATUREFLAGS_AXF_ANISOTROPY));
}

bool HasClearcoat()
{
    bool ret = (HasFlag(AXF_MATERIAL_FLAGS, FEATUREFLAGS_AXF_CLEAR_COAT));
#if defined(_AXF_BRDF_TYPE_CAR_PAINT) && defined(FORCE_CAR_PAINT_HAS_CLEARCOAT)
    ret = true;
#endif
    return ret;
}

bool HasClearcoatRefraction()
{
    return (HasFlag(AXF_MATERIAL_FLAGS, FEATUREFLAGS_AXF_CLEAR_COAT_REFRACTION));
}

bool HasClearcoatAndRefraction()
{
    uint bits = (FEATUREFLAGS_AXF_CLEAR_COAT | FEATUREFLAGS_AXF_CLEAR_COAT_REFRACTION);
    return ((AXF_MATERIAL_FLAGS & bits) == bits);
}

bool HasBRDFColorDiagonalClamp()
{
    return (HasFlag(AXF_MATERIAL_FLAGS, FEATUREFLAGS_AXF_BRDFCOLOR_DIAGONAL_CLAMP));
}

bool HonorMinRoughness()
{
    return (HasFlag(AXF_MATERIAL_FLAGS, FEATUREFLAGS_AXF_HONOR_MIN_ROUGHNESS));
}

bool HonorMinRoughnessCoat()
{
    return (HasFlag(AXF_MATERIAL_FLAGS, FEATUREFLAGS_AXF_HONOR_MIN_ROUGHNESS_COAT));
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

float GetScalarRoughness(float3 roughness)
{
    float singleRoughness = 0.5;

#if defined(_AXF_BRDF_TYPE_SVBRDF)

    singleRoughness = (HasAnisotropy()) ? GetScalarRoughnessFromAnisoRoughness(roughness.x, roughness.y) : roughness.x;

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
    float sumCoeffXRoughness = 0.0;
    float sumCoeff = 0.0;
    UNITY_UNROLL
    for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++) // TODO remove all variable lobecnt code
    {
        float coeff = _CarPaint2_CTCoeffs[lobeIndex];
        float spread = roughness[lobeIndex];
        sumCoeff += coeff;
        sumCoeffXRoughness += spread * coeff;
    }
    singleRoughness = min(1.0, SafeDiv(sumCoeffXRoughness,sumCoeff));
#endif

    return singleRoughness;
}

float GetCarPaintFresnel0()
{
    float ret = 0;
    float curMax = 0;
    uint algo = 1;

    switch (algo)
    {
    case 0:
        {
            for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
            {
                float   f0 = _CarPaint2_CTF0s[lobeIndex];
                float   coeff = _CarPaint2_CTCoeffs[lobeIndex];

                if (curMax < (f0*coeff))
                {
                    ret = f0;
                    curMax = f0*coeff;
                }
            }
            break;
        }
    case 1:
        {
            float4 coeffXf0 = _CarPaint2_CTF0s * _CarPaint2_CTCoeffs;
            ret = Max3(coeffXf0[0], coeffXf0[1], coeffXf0[2]);
            break;
        }
    case 2:
        {
            ret = dot(_CarPaint2_CTF0s.xyz,_CarPaint2_CTCoeffs.xyz);
            break;
        }
    }
    return ret;
}

float3 GetCarPaintSpecularColor()
{
    //TODO: improve
    return GetBRDFColor(0,0);
}

//
// Flakes BTF access
//
struct FlakesSamplingInfo
{
    float2  flakesUVZY;
    float2  flakesUVXZ;
    float2  flakesUVXY;
    float   flakesMipLevelZY;
    float   flakesMipLevelXZ;
    float   flakesMipLevelXY;
    float3  flakesTriplanarWeights;
    float2  flakesDdxZY; // if non null, we will prefer gradients (to be used statically only!)
    float2  flakesDdyZY;
    float2  flakesDdxXZ;
    float2  flakesDdyXZ;
    float2  flakesDdxXY;
    float2  flakesDdyXY;
};

FlakesSamplingInfo GetFillFlakesSamplingInfo(SurfaceData surfaceData, BSDFData bsdfData, bool useBSDFData = true)
{
    FlakesSamplingInfo flakesSamplingInfo;
    ZERO_INITIALIZE(FlakesSamplingInfo, flakesSamplingInfo);

    if(useBSDFData)
    {
        flakesSamplingInfo.flakesUVZY = bsdfData.flakesUVZY;
        flakesSamplingInfo.flakesUVXZ = bsdfData.flakesUVXZ;
        flakesSamplingInfo.flakesUVXY = bsdfData.flakesUVXY;
        flakesSamplingInfo.flakesMipLevelZY = bsdfData.flakesMipLevelZY;
        flakesSamplingInfo.flakesMipLevelXZ = bsdfData.flakesMipLevelXZ;
        flakesSamplingInfo.flakesMipLevelXY = bsdfData.flakesMipLevelXY;
        flakesSamplingInfo.flakesTriplanarWeights = bsdfData.flakesTriplanarWeights;

        flakesSamplingInfo.flakesDdxZY = bsdfData.flakesDdxZY;
        flakesSamplingInfo.flakesDdyZY = bsdfData.flakesDdyZY;
        flakesSamplingInfo.flakesDdxXZ = bsdfData.flakesDdxXZ;
        flakesSamplingInfo.flakesDdyXZ = bsdfData.flakesDdyXZ;
        flakesSamplingInfo.flakesDdxXY = bsdfData.flakesDdxXY;
        flakesSamplingInfo.flakesDdyXY = bsdfData.flakesDdyXY;
    }
    else
    {
        // Fill using surfaceData: identical to FillFlakesBSDFData
    #ifdef _MAPPING_TRIPLANAR
        flakesSamplingInfo.flakesUVZY = surfaceData.flakesUVZY;
        flakesSamplingInfo.flakesUVXZ = surfaceData.flakesUVXZ;
        flakesSamplingInfo.flakesUVXY = surfaceData.flakesUVXY;
        flakesSamplingInfo.flakesMipLevelZY = surfaceData.flakesMipLevelZY;
        flakesSamplingInfo.flakesMipLevelXZ = surfaceData.flakesMipLevelXZ;
        flakesSamplingInfo.flakesMipLevelXY = surfaceData.flakesMipLevelXY;
        flakesSamplingInfo.flakesTriplanarWeights = surfaceData.flakesTriplanarWeights;

        flakesSamplingInfo.flakesDdxZY = surfaceData.flakesDdxZY;
        flakesSamplingInfo.flakesDdyZY = surfaceData.flakesDdyZY;
        flakesSamplingInfo.flakesDdxXZ = surfaceData.flakesDdxXZ;
        flakesSamplingInfo.flakesDdyXZ = surfaceData.flakesDdyXZ;
        flakesSamplingInfo.flakesDdxXY = surfaceData.flakesDdxXY;
        flakesSamplingInfo.flakesDdyXY = surfaceData.flakesDdyXY;
    #else
        // NOTE: When not triplanar UVZY has one uv set or one planar coordinate set,
        // and this planar coordinate set isn't necessarily ZY, we just reuse this field
        // as a common one.
        flakesSamplingInfo.flakesUVZY = surfaceData.flakesUVZY;
        flakesSamplingInfo.flakesMipLevelZY = surfaceData.flakesMipLevelZY;
        flakesSamplingInfo.flakesDdxZY = surfaceData.flakesDdxZY;
        flakesSamplingInfo.flakesDdyZY = surfaceData.flakesDdyZY;
        flakesSamplingInfo.flakesUVXZ = 0;
        flakesSamplingInfo.flakesUVXY = 0;
        flakesSamplingInfo.flakesMipLevelXZ = 0;
        flakesSamplingInfo.flakesMipLevelXY = 0;
        flakesSamplingInfo.flakesTriplanarWeights = 0;
        flakesSamplingInfo.flakesDdxXZ = 0;
        flakesSamplingInfo.flakesDdyXZ = 0;
        flakesSamplingInfo.flakesDdxXY = 0;
        flakesSamplingInfo.flakesDdyXY = 0;
    #endif
    }

    return flakesSamplingInfo;
}

void FillFlakesBSDFData(SurfaceData surfaceData, inout BSDFData bsdfData)
{
#ifdef _MAPPING_TRIPLANAR
    bsdfData.flakesUVZY = surfaceData.flakesUVZY;
    bsdfData.flakesUVXZ = surfaceData.flakesUVXZ;
    bsdfData.flakesUVXY = surfaceData.flakesUVXY;
    bsdfData.flakesMipLevelZY = surfaceData.flakesMipLevelZY;
    bsdfData.flakesMipLevelXZ = surfaceData.flakesMipLevelXZ;
    bsdfData.flakesMipLevelXY = surfaceData.flakesMipLevelXY;
    bsdfData.flakesTriplanarWeights = surfaceData.flakesTriplanarWeights;

    bsdfData.flakesDdxZY = surfaceData.flakesDdxZY;
    bsdfData.flakesDdyZY = surfaceData.flakesDdyZY;
    bsdfData.flakesDdxXZ = surfaceData.flakesDdxXZ;
    bsdfData.flakesDdyXZ = surfaceData.flakesDdyXZ;
    bsdfData.flakesDdxXY = surfaceData.flakesDdxXY;
    bsdfData.flakesDdyXY = surfaceData.flakesDdyXY;
#else
    // NOTE: When not triplanar UVZY has one uv set or one planar coordinate set,
    // and this planar coordinate set isn't necessarily ZY, we just reuse this field
    // as a common one.
    bsdfData.flakesUVZY = surfaceData.flakesUVZY;
    bsdfData.flakesMipLevelZY = surfaceData.flakesMipLevelZY;
    bsdfData.flakesDdxZY = surfaceData.flakesDdxZY;
    bsdfData.flakesDdyZY = surfaceData.flakesDdyZY;
    bsdfData.flakesUVXZ = 0;
    bsdfData.flakesUVXY = 0;
    bsdfData.flakesMipLevelXZ = 0;
    bsdfData.flakesMipLevelXY = 0;
    bsdfData.flakesTriplanarWeights = 0;
    bsdfData.flakesDdxXZ = 0;
    bsdfData.flakesDdyXZ = 0;
    bsdfData.flakesDdxXY = 0;
    bsdfData.flakesDdyXY = 0;
#endif
}

// Samples the "BTF Flakes" texture as explained in "AxF-Decoding-SDK-1.5.1/doc/html/page2.html#carpaint_FlakeBTF" from the SDK
uint    SampleFlakesLUT(uint index)
{
    return 255.0 * _CarPaint2_FlakeThetaFISliceLUTMap[uint2(index, 0)].x;
    // Hardcoded LUT
    //    uint    pipoLUT[] = { 0, 8, 16, 24, 32, 40, 47, 53, 58, 62, 65, 67 };
    //    return pipoLUT[min(11, _index)];
}

float3  SampleFlakes(float2 offsets[NB_FLAKES_RND_SHIFTS], uint sliceIndex, FlakesSamplingInfo flakesSamplingInfo)
{
    // We can't use SAMPLE_TEXTURE2D_ARRAY, the compiler can't unroll in that case, and the lightloop is built with unroll
    // That's why we calculate gradients or LOD earlier.
    // TODO: The LOD code path (useFlakesMipLevel == true) is kept for a possible performance/appearance trade-off
    // (less VGPR for LOD) and also for (future) raytracing, it is easier to substitute an approximate single LOD value
    // than a full 2x2 Jacobian.
    float3 val = 0;
    bool useFlakesMipLevel = all(flakesSamplingInfo.flakesDdxZY == (float2)0); // should be known statically!

#ifdef _MAPPING_TRIPLANAR
    val += flakesSamplingInfo.flakesTriplanarWeights.x *
           (useFlakesMipLevel ?
             SAMPLE_TEXTURE2D_ARRAY_LOD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap,
                                        flakesSamplingInfo.flakesUVZY + offsets[FLAKES_SHIFT_IDX_PLANAR_ZY],
                                        sliceIndex, flakesSamplingInfo.flakesMipLevelZY).xyz
           : SAMPLE_TEXTURE2D_ARRAY_GRAD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap,
                                         flakesSamplingInfo.flakesUVZY + offsets[FLAKES_SHIFT_IDX_PLANAR_ZY],
                                         sliceIndex, flakesSamplingInfo.flakesDdxZY, flakesSamplingInfo.flakesDdyZY).xyz );

    val += flakesSamplingInfo.flakesTriplanarWeights.y *
           (useFlakesMipLevel ?
             SAMPLE_TEXTURE2D_ARRAY_LOD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap,
                                        flakesSamplingInfo.flakesUVXZ + offsets[FLAKES_SHIFT_IDX_PLANAR_XZ],
                                        sliceIndex, flakesSamplingInfo.flakesMipLevelXZ).xyz
           : SAMPLE_TEXTURE2D_ARRAY_GRAD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap,
                                         flakesSamplingInfo.flakesUVXZ + offsets[FLAKES_SHIFT_IDX_PLANAR_XZ],
                                         sliceIndex, flakesSamplingInfo.flakesDdxXZ, flakesSamplingInfo.flakesDdyXZ).xyz );
    val += flakesSamplingInfo.flakesTriplanarWeights.z *
           (useFlakesMipLevel ?
             SAMPLE_TEXTURE2D_ARRAY_LOD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap,
                                        flakesSamplingInfo.flakesUVXY + offsets[FLAKES_SHIFT_IDX_PLANAR_XY],
                                        sliceIndex, flakesSamplingInfo.flakesMipLevelXY).xyz
           : SAMPLE_TEXTURE2D_ARRAY_GRAD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap,
                                         flakesSamplingInfo.flakesUVXY + offsets[FLAKES_SHIFT_IDX_PLANAR_XY],
                                         sliceIndex, flakesSamplingInfo.flakesDdxXY, flakesSamplingInfo.flakesDdyXY).xyz );
    val *= _CarPaint2_BTFFlakeMapScale;
#else
    val = _CarPaint2_BTFFlakeMapScale *
          (useFlakesMipLevel ?
            SAMPLE_TEXTURE2D_ARRAY_LOD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap,
                                       flakesSamplingInfo.flakesUVZY + offsets[0], sliceIndex, flakesSamplingInfo.flakesMipLevelZY).xyz
          : SAMPLE_TEXTURE2D_ARRAY_GRAD(_CarPaint2_BTFFlakeMap, sampler_CarPaint2_BTFFlakeMap,
                                        flakesSamplingInfo.flakesUVZY + offsets[0], sliceIndex, flakesSamplingInfo.flakesDdxZY, flakesSamplingInfo.flakesDdyZY).xyz );
#endif
    return val;
}

//
// Working code, TODO_FLAKES: missing virtual thetaD (aka thetaI) bin generation
//
float3  CarPaint_BTF(float thetaH, float thetaD, SurfaceData surfaceData, BSDFData bsdfData, bool useBSDFData = true)
{
    // debug raytracing: seems uint in constant buffer get corrupted!
    uint flakeMaxThetaI = AXF_CARPAINT2_FLAKEMAXTHETAI;
    uint flakeNumThetaF = AXF_CARPAINT2_FLAKENUMTHETAF;
    uint flakeNumThetaI = AXF_CARPAINT2_FLAKENUMTHETAI;

    // Note: this has no impact on perf, it is just to support multiple callee contexts:
    FlakesSamplingInfo flakesSamplingInfo = GetFillFlakesSamplingInfo(surfaceData, bsdfData, useBSDFData);

    // thetaH sampling defines the angular sampling, i.e. angular flake lifetime
    float   binIndexH = flakeNumThetaF * (2.0 * thetaH / PI) + 0.5; // TODO: doc says to use NumThetaF for both, check if this isn't a typo
    float   binIndexD = flakeNumThetaF * (2.0 * thetaD / PI) + 0.5;

    // Bilinear interpolate indices and weights
    uint    thetaH_low = floor(binIndexH);
    uint    thetaD_low = floor(binIndexD);
    uint    thetaH_high = thetaH_low + 1;
    uint    thetaD_high = thetaD_low + 1;
    float   thetaH_weight = binIndexH - thetaH_low;
    float   thetaD_weight = binIndexD - thetaD_low;

    // To allow lower thetaD samplings while preserving flake lifetime, "virtual" thetaD patches are generated by shifting existing ones
    // NB_FLAKES_RND_SHIFTS = 1 if not triplanar; otherwise this is in case we want a randomization that takes planar coordinate index into account
    float2   offset_l[NB_FLAKES_RND_SHIFTS] = (float2[NB_FLAKES_RND_SHIFTS])0;
    float2   offset_h[NB_FLAKES_RND_SHIFTS] = (float2[NB_FLAKES_RND_SHIFTS])0;

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
    if (thetaD_low < flakeMaxThetaI)
    {
        // These are spatial UVs, we let SampleFlakes deal with them in case of triplanar,
        // and just submit the random shift offsets (TODO "virtual" angular patches)
        //float2  UVl = UV + offset_l;
        //float2  UVh = UV + offset_h;

        uint    LUT0 = SampleFlakesLUT(thetaD_low);
        uint    LUT1 = SampleFlakesLUT(thetaD_high);
        uint    LUT0_limit = SampleFlakesLUT(thetaD_low+1);
        // without "virtual thetaD" bins, LUT0_limit will be the same as LUT1 and optimized out.
        uint    LUT2 = SampleFlakesLUT(thetaD_high + 1);

        if (LUT0 + thetaH_low < LUT0_limit)
        {
            H0_D0 = SampleFlakes(offset_l, LUT0 + thetaH_low, flakesSamplingInfo);
            if (LUT0 + thetaH_high < LUT0_limit)
            {
                H1_D0 = SampleFlakes(offset_l, LUT0 + thetaH_high, flakesSamplingInfo);
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

        if (thetaD_high < flakeMaxThetaI)
        {
            if (LUT1 + thetaH_low < LUT2)
            {
                H0_D1 = SampleFlakes(offset_h, LUT1 + thetaH_low, flakesSamplingInfo);
                if (LUT1 + thetaH_high < LUT2)
                {
                    H1_D1 = SampleFlakes(offset_h, LUT1 + thetaH_high, flakesSamplingInfo);
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

// AxF splits the chromaticity and f0 from the usual "SpecularColor" convention
// to just be a chromatic f0.
// CARPAINT2 has a different way to handle colors and must be accounted for too.
// Base refers to the "base layer", ie not the coat if present.
float3 GetColorBaseFresnelF0(BSDFData bsdfData)
{
    return bsdfData.fresnel0.r * bsdfData.specularColor;
}

// For raytracing fit to standard Lit:
// Giving V will use a codepath where V is used, otherwise, the ortho direction is used
void GetCarPaintSpecularColorAndFlakesComponent(BSDFData bsdfData, out float3 singleBRDFColor, out float3 singleFlakesComponent, out float coatFGD, float3 V = 0)
{
    //TODO: use approximated top lobe dir (if refractive coat) to have more appropriate and consistent base dirs

    // This is statically known
    bool useViewDir = ((V.x * V.y * V.z) != 0.0);

    if (useViewDir)
    {
        float3 coatNormalWS = HasClearcoat() ? bsdfData.clearcoatNormalWS : bsdfData.normalWS;
        float coatNdotV = dot(coatNormalWS, V);
        coatFGD = HasClearcoat() ? F_FresnelDieletricSafe(bsdfData.clearcoatIOR, coatNdotV) : 0;

        float3 refractedViewWS = V;
        float thetaHForBRDFColor = FixedBRDFColorThetaHForIndirectLight;
        float thetaHForFlakes = FixedFlakesThetaHForIndirectLight;
        if (HasClearcoatAndRefraction())
        {
            refractedViewWS = -Refract(V, coatNormalWS, 1.0 / bsdfData.clearcoatIOR);
            thetaHForBRDFColor = Refract(thetaHForBRDFColor, 1.0 / bsdfData.clearcoatIOR);
            thetaHForFlakes = Refract(thetaHForFlakes, 1.0 / bsdfData.clearcoatIOR);
        }
        float NdotV = dot(bsdfData.normalWS, refractedViewWS);

        float thetaH = 0; //FastACosPos(clamp(NdotH, 0, 1));
        float thetaD = FastACosPos(clamp(NdotV, 0, 1));

        singleBRDFColor = GetBRDFColor(thetaHForBRDFColor, thetaD);
        singleFlakesComponent = CarPaint_BTF(thetaHForFlakes, thetaD, (SurfaceData)0, bsdfData, /*useBSDFData:*/true);
    }
    else
    {
        //coatFGD = HasClearcoat() ? F_FresnelDieletricSafe(surfaceData.clearcoatIOR, 1) : 0;
        // ...this is just F0 of coat, so we do the equivalent:
        coatFGD = HasClearcoat() ? IorToFresnel0(bsdfData.clearcoatIOR) : 0;

        singleBRDFColor = GetBRDFColor(0,0);
        singleFlakesComponent = CarPaint_BTF(0, 0,(SurfaceData)0, bsdfData, /*useBSDFData:*/true);
    }
}

// For raytracing fit to standard Lit:
// Giving V will use a codepath where V is used, this is relevant only for carpaint model
// (cf GetColorBaseDiffuse() and GetColorBaseFresnelF0())
void GetBaseSurfaceColorAndF0(BSDFData bsdfData, out float3 diffuseColor, out float3 fresnel0, out float3 specBRDFColor, out float3 singleFlakesComponent, out float coatFGD, float3 V = 0, bool mixFlakes = false)
{
    coatFGD = 0;
    singleFlakesComponent = (float3)0;
    fresnel0 = (float3)0;
    float3 specularColor = (float3)0;
    specBRDFColor = float3(1,1,1); // only used for carpaint
    diffuseColor = bsdfData.diffuseColor;

#ifdef _AXF_BRDF_TYPE_SVBRDF

    specularColor = bsdfData.specularColor;
    fresnel0 = bsdfData.fresnel0; // See AxfData.hlsl: the actual sampled texture is always 1 channel, if we ever find otherwise, we will use the others.
    fresnel0 = HasFresnelTerm() ? fresnel0.r * specularColor : specularColor;

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

    GetCarPaintSpecularColorAndFlakesComponent(bsdfData, /*out*/specBRDFColor, /*out*/singleFlakesComponent, /*out*/coatFGD, V);

    // For carpaint, diffuseColor is not chromatic.
    // A chromatic diffuse albedo is the result of a scalar diffuse coefficient multiplied by the brdf color table value.
    specularColor = specBRDFColor;
    diffuseColor *= specBRDFColor;
    fresnel0 = saturate(3*bsdfData.fresnel0);//GetCarPaintFresnel0() TODO: presumably better fit using V, see also GetCarPaintSpecularColor that uses V
    fresnel0 = fresnel0.r * specularColor;

    if (mixFlakes)
    {
        float maxf0 = Max3(fresnel0.r, fresnel0.g, fresnel0.b);
        fresnel0 = saturate(singleFlakesComponent + fresnel0);
    }

#endif

    float baseEnergy = (1-coatFGD); // should be Sq but at this point we eyeball anyway,
    //specularColor *= baseEnergy;
    //diffuseColor *= baseEnergy;
    //...commented, seems better without it.
}

void GetRoughnessNormalCoatMaskForFitToStandardLit(BSDFData bsdfData, float coatFGD, out float3 normalWS, out float roughness, out float coatMask)
{
    normalWS = bsdfData.normalWS; // todo: "refract back" hack
    // Try to simulate apparent roughness increase when he have refraction as we can't store refracted V in the GBUFFER,
    // we could try another hack and modify the normal too.
    roughness = GetScalarRoughness(bsdfData.roughness);
    roughness = saturate(roughness * (HasClearcoatAndRefraction() ? (max(1, bsdfData.clearcoatIOR)) : 1) );
    coatMask = HasClearcoat()? Sq(coatFGD) * Max3(bsdfData.clearcoatColor.r, bsdfData.clearcoatColor.g, bsdfData.clearcoatColor.b) : 0;
    // Sq(coatFGD) is a hack to better fit what AxF shows vs the usage of the coatmask with Lit
    coatMask = 0;
    //...disable for now coat reduces too much visibility of primary surface and in any case in performance mode where we use FitToStandardLit,
    //we will not get another reflection bounce so the coat reflection will be a fallback probe
}

float3 GetColorBaseDiffuse(BSDFData bsdfData)
{
    float3 diffuseColor = 0;

#if defined(_AXF_BRDF_TYPE_SVBRDF)
    diffuseColor = bsdfData.diffuseColor;
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
    // For carpaint, specularColor will be set from BRDFColor table and
    // diffuseColor is not chromatic. ie chromatic diffuse albedo is the result of
    // scalar diffuse coefficient tinted by the brdf color table
    diffuseColor = bsdfData.diffuseColor * bsdfData.specularColor;
#endif

    return diffuseColor;
}

float4 GetDiffuseOrDefaultColor(BSDFData bsdfData, float replace)
{
    float3 fresnel0 = GetColorBaseFresnelF0(bsdfData);
    float3 diffuseColor = GetColorBaseDiffuse(bsdfData);

    // Use fresnel0 as mettalic weight. all value below 0.2 (ior of diamond) are dielectric
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
        {
            float3 vsNormal = TransformWorldToViewDir(surfaceData.normalWS);
            result = IsNormalized(vsNormal) ?  vsNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_AXF_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        {
            float3 vsGeomNormal = TransformWorldToViewDir(surfaceData.geomNormalWS);
            result = IsNormalized(vsGeomNormal) ?  vsGeomNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
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
    case DEBUGVIEW_AXF_BSDFDATA_NORMAL_VIEW_SPACE:
        // Convert to view space
        {
            float3 vsNormal = TransformWorldToViewDir(bsdfData.normalWS);
            result = IsNormalized(vsNormal) ?  vsNormal * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        }
    case DEBUGVIEW_AXF_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
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
        surfaceData.perceptualSmoothness = overrideSmoothnessValue;
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

        // Hack: try to get a "single equivalent" roughness
        normalData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    }

    return normalData;
}


//----------------------------------------------------------------------
// Ref: https://seblagarde.wordpress.com/2013/04/29/memo-on-fresnel-equations/
// Fresnel dieletric / dielectric

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

float  MultiLobesCookTorrance(BSDFData bsdfData, float NdotL, float NdotV, float NdotH, float VdotH)
{
    // Ensure numerical stability
    if (NdotV < 0.00174532836589830883577820272085 || NdotL < 0.00174532836589830883577820272085) //sin(0.1 deg )
        return 0.0;

    float   specularIntensity = 0.0;
    for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
    {
        float   F0 = _CarPaint2_CTF0s[lobeIndex];
        float   coeff = _CarPaint2_CTCoeffs[lobeIndex];
        float   spread = bsdfData.roughness[lobeIndex]; // _CarPaint2_CTSpreads[lobeIndex];

        specularIntensity += coeff * CT_D(NdotH, spread) * CT_F(VdotH, F0);
    }

    // FIXME: should be 4 instead of PI at the denominator, this was a mistake in the original paper
    specularIntensity *= G_CookTorrance(NdotH, NdotV, NdotL, VdotH)  // Shadowing/Masking term
        / (PI * max(1e-3, NdotV * NdotL));

    return specularIntensity;
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
    ZERO_INITIALIZE(BSDFData, bsdfData);

    bsdfData.ambientOcclusion = surfaceData.ambientOcclusion;
    bsdfData.specularOcclusion = surfaceData.specularOcclusion;

    // V is needed for raytracing performance fit to lit:
    // TODO: should just modify FitToStandardLit in ShaderPassRaytracingGBuffer.hlsl and callee
    // to have "V" (from -incidentDir)
    bsdfData.viewWS = surfaceData.viewWS;
    bsdfData.normalWS = surfaceData.normalWS;
    bsdfData.tangentWS = surfaceData.tangentWS;
    bsdfData.bitangentWS = cross(bsdfData.normalWS, bsdfData.tangentWS);

    bsdfData.roughness = 0;
    // see AxFData.hlsl: important, this is used in PostEvaluateBSDF here and in AxFRayTracing
    bsdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.perceptualSmoothness);
    //-----------------------------------------------------------------------------
#ifdef _AXF_BRDF_TYPE_SVBRDF
    bsdfData.diffuseColor = surfaceData.diffuseColor;
    bsdfData.specularColor = surfaceData.specularColor;

    bsdfData.fresnel0 = surfaceData.fresnel0; // See AxfData.hlsl: the actual sampled texture is always 1 channel, if we ever find otherwise, we will use the others.
    bsdfData.height_mm = surfaceData.height_mm;

    bsdfData.roughness.xy = HasAnisotropy() ? surfaceData.specularLobe.xy : surfaceData.specularLobe.xx;

    bsdfData.clearcoatColor = surfaceData.clearcoatColor;
    bsdfData.clearcoatNormalWS = HasClearcoat() ? surfaceData.clearcoatNormalWS : surfaceData.normalWS;
    bsdfData.clearcoatIOR = surfaceData.clearcoatIOR;

    //-----------------------------------------------------------------------------
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
    bsdfData.diffuseColor = surfaceData.diffuseColor; // See GetColorBaseDiffuse() for carpaint!
    FillFlakesBSDFData(surfaceData, bsdfData);
    bsdfData.clearcoatColor = 1.0;  // Not provided, assume white...
    bsdfData.clearcoatIOR = surfaceData.clearcoatIOR;
    bsdfData.clearcoatNormalWS = HasClearcoat() ? surfaceData.clearcoatNormalWS : surfaceData.normalWS;

    bsdfData.specularColor = GetCarPaintSpecularColor();
    bsdfData.fresnel0 = GetCarPaintFresnel0();
    bsdfData.roughness.xyz = surfaceData.specularLobe.xyz; // the later stores per lobe possibly modified (for geometric specular AA) _CarPaint2_CTSpreads
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
    float3  iblDominantDirectionWS_BottomLobeOnTop; // Dominant specular direction, for bottom lobe but as it exit on top, used for IBL in EvaluateBSDF_Env()
    float3  iblDominantDirectionWS_Clearcoat;       // Dominant specular direction, used for IBL in EvaluateBSDF_Env() and also in area lights when clearcoat is enabled
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
    float   coatReflectionWeight; // Extra light reflectionHierarchyWeight
    float   baseReflectionWeight; // Note: even for car paint we just track one weight for the bottom layer to simplify, see EvaluateBSDF_Env and EvaluateBSDF_ScreenSpaceReflection
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


float3 FindAverageBaseLobeDirOnTop(BSDFData bsdfData, PreLightData preLightData, out float3 lobeDirUndercoat)
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
    lobeDirUndercoat = -incomingSaturated; // incoming is away from the top interface from under the surface so *-1 to reverse quadrant.
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
    float3 reflectedLobeDirUndercoat = reflect(-preLightData.viewWS_UnderCoat, bsdfData.normalWS);
    preLightData.iblDominantDirectionWS_BottomLobeOnTop = reflectedLobeDirUndercoat;
    if (HasClearcoatAndRefraction())
    {
        preLightData.iblDominantDirectionWS_BottomLobeOnTop = FindAverageBaseLobeDirOnTop(bsdfData, preLightData, reflectedLobeDirUndercoat); // much better
        // reflectedLobeDirUndercoat is now adjusted to correspond to the refracted-back on top direction returned by FindAverageBaseLobeDirOnTop()

        //sanity check: If both normals are equal, then this shouldn't change the output:
        //preLightData.iblDominantDirectionWS_BottomLobeOnTop = reflect(-viewWS_Clearcoat, bsdfData.clearcoatNormalWS);
        //reflectedLobeDirUndercoat = reflect(-preLightData.viewWS_UnderCoat, bsdfData.normalWS);
    }
    preLightData.iblDominantDirectionWS_Clearcoat = reflect(-viewWS_Clearcoat, bsdfData.clearcoatNormalWS);
    //preLightData.iblDominantDirectionWS_BottomLobeOnTop = preLightData.iblDominantDirectionWS_Clearcoat;

#ifdef _AXF_BRDF_TYPE_SVBRDF
    // @TODO => Anisotropic IBL?
    // TODO_SL
    preLightData.iblPerceptualRoughness = RoughnessToPerceptualRoughness(GetScalarRoughnessFromAnisoRoughness(bsdfData.roughness.x, bsdfData.roughness.y));

    // todo_fresnel: TOCHECK: Make BRDF and FGD for env. consistent with dirac lights for HasFresnelTerm() handling:
    // currently, we only check it for Ward and its variants.
    float3 tempF0 = HasFresnelTerm() ? bsdfData.fresnel0.rrr : 1.0;
    tempF0 *= bsdfData.specularColor; // Important to use in the PreIntegratedFGD interpolated fetches!

    float specularReflectivity;
    switch (AXF_SVBRDF_BRDFTYPE_SPECULARTYPE)
    {
    //@TODO: Oren-Nayar diffuse FGD
    case 0:
        GetPreIntegratedFGDWardAndLambert(NdotV_UnderCoat, preLightData.iblPerceptualRoughness, tempF0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
        // Although we have pre-integrated FGD for non-GGX BRDFs, all our IBL are pre-convolved with GGX, so use this rough conversion:
        preLightData.iblPerceptualRoughness = PerceptualRoughnessBeckmannToGGX(preLightData.iblPerceptualRoughness);
        break;

    case 1: //Phong
    case 4: //Blinn-Phong : just approximate with Cook-Torrance which uses a Beckmann distribution
    case 2:
        GetPreIntegratedFGDCookTorranceAndLambert(NdotV_UnderCoat, preLightData.iblPerceptualRoughness, tempF0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
        preLightData.specularFGD *= GetPreIntegratedFGDCookTorranceSampleMutiplier();
        // Although we have pre-integrated FGD for non-GGX BRDFs, all our IBL are pre-convolved with GGX, so use this rough conversion:
        preLightData.iblPerceptualRoughness = PerceptualRoughnessBeckmannToGGX(preLightData.iblPerceptualRoughness);
        break;
    case 3:
        GetPreIntegratedFGDGGXAndLambert(NdotV_UnderCoat, preLightData.iblPerceptualRoughness, tempF0, preLightData.specularFGD, preLightData.diffuseFGD, specularReflectivity);
        break;

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

    // TODO_diffuseFGDColor: better one, averaged maybe: ie depending on roughness also
    preLightData.singleBRDFColor = 1.0;
    float thetaH = 0;
    float thetaD = FastACosPos(clamp(preLightData.NdotV_UnderCoat, 0, 1));
    // The above is the same as
    //float3 lightDir = reflect(-preLightData.viewWS_UnderCoat, bsdfData.normalWS);
    //float3 H = normalize(preLightData.viewWS_UnderCoat + lightDir);
    //float NdotH = dot(bsdfData.normalWS, H);
    //float LdotH = dot(H, lightDir);
    //thetaH = FastACosPos(clamp(NdotH, 0, 1));
    //thetaD = FastACosPos(clamp(LdotH, 0, 1));

    // Also, could use reflectedLobeDirUndercoat here (and see TODO_diffuseFGDColor: if we make it depends on roughness, one per lobe)
    // This is relevant only if both normals aren't the same obviously.
    // In the case of CARPAINT, this means a clearcoat normal map.
    // (ie orange peel)
    if (false)
    {
        float3 H = normalize(preLightData.viewWS_UnderCoat + reflectedLobeDirUndercoat);
        float NdotH = dot(bsdfData.normalWS, H);

        float LdotH = dot(H, reflectedLobeDirUndercoat);
        thetaH = FastACosPos(clamp(NdotH, 0, 1));
        thetaD = FastACosPos(clamp(LdotH, 0, 1));
    }

    float thetaHForBRDFColor = HasClearcoatAndRefraction() ? Refract(FixedBRDFColorThetaHForIndirectLight, 1.0 / bsdfData.clearcoatIOR) : FixedBRDFColorThetaHForIndirectLight;
    float thetaHForFlakes = HasClearcoatAndRefraction() ? Refract(FixedFlakesThetaHForIndirectLight, 1.0 / bsdfData.clearcoatIOR) : FixedFlakesThetaHForIndirectLight;
    preLightData.singleBRDFColor *= GetBRDFColor(thetaHForBRDFColor, thetaD);
    preLightData.singleFlakesComponent = CarPaint_BTF(thetaHForFlakes, thetaD, (SurfaceData)0, bsdfData, /*useBSDFData:*/true);

    UNITY_UNROLL
    for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
    {
        float   F0 = _CarPaint2_CTF0s[lobeIndex];
        float   coeff = _CarPaint2_CTCoeffs[lobeIndex];
        float   spread = bsdfData.roughness[lobeIndex]; // _CarPaint2_CTSpreads[lobeIndex];
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
        preLightData.ltcTransformSpecularCT[lobeIndex] = SampleLtcMatrix(preLightData.iblPerceptualRoughness[lobeIndex], NdotV_Clearcoat, LTCLIGHTINGMODEL_COOK_TORRANCE);
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
    preLightData.iblPerceptualRoughness = PerceptualRoughnessBeckmannToGGX(preLightData.iblPerceptualRoughness);
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
    IFNOT_FLAKES_JUST_BTF(preLightData.ltcTransformFlakes = SampleLtcMatrix(FLAKES_PERCEPTUAL_ROUGHNESS, NdotV_Clearcoat, LTCLIGHTINGMODEL_GGX));
#endif//#ifdef _AXF_BRDF_TYPE_SVBRDF


//-----------------------------------------------------------------------------
// Area lights

// Construct a right-handed view-dependent orthogonal basis around the normal
    preLightData.orthoBasisViewNormal[2] = bsdfData.normalWS;
    preLightData.orthoBasisViewNormal[0] = normalize(viewWS_Clearcoat - preLightData.NdotV_Clearcoat * bsdfData.normalWS);    // Do not clamp NdotV here
    preLightData.orthoBasisViewNormal[1] = cross(preLightData.orthoBasisViewNormal[2], preLightData.orthoBasisViewNormal[0]);

#ifdef _AXF_BRDF_TYPE_SVBRDF

    // Load diffuse LTC & FGD
    if (AXF_SVBRDF_BRDFTYPE_DIFFUSETYPE)
    {
        // todo_modes todo_pseudorefract: cant use undercoat like that
        preLightData.ltcTransformDiffuse = SampleLtcMatrix(preLightData.iblPerceptualRoughness, NdotV_Clearcoat, LTCLIGHTINGMODEL_OREN_NAYAR);
    }
    else
    {
        preLightData.ltcTransformDiffuse = k_identity3x3;   // Lambert
    }

    uint bsdfIndex;

    switch (AXF_SVBRDF_BRDFTYPE_SPECULARTYPE)
    {
    case 0:
        bsdfIndex = LTCLIGHTINGMODEL_WARD;
        break;
    case 3:
        bsdfIndex = LTCLIGHTINGMODEL_GGX;
        break;
    default: // COOK-TORRANCE, BLINN-PHONG, PHONG, or missing
        bsdfIndex = LTCLIGHTINGMODEL_COOK_TORRANCE;
        break;
    }

    // Load specular LTC & FGD
    // Warning: all these LTC_MATRIX_INDEX_ are the same for now, and fitted for GGX, hence the code
    // above that selected the UVs all used a preLightData.iblPerceptualRoughness value that used a
    // conversion formula for Beckmann NDF (exp) based BRDFs
    // (see switch (AXF_SVBRDF_BRDFTYPE_SPECULARTYPE) above and usage of PerceptualRoughnessBeckmannToGGX)
    //
    // todo_modes todo_pseudorefract: cant use undercoat like that
    preLightData.ltcTransformSpecular = SampleLtcMatrix(preLightData.iblPerceptualRoughness, NdotV_Clearcoat, bsdfIndex);

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
        preLightData.ltcTransformClearcoat = SampleLtcMatrix(CLEAR_COAT_PERCEPTUAL_ROUGHNESS, NdotV_Clearcoat, LTCLIGHTINGMODEL_GGX);
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

    // Init light hierarchy reflection weights to 0:
    preLightData.coatReflectionWeight = preLightData.baseReflectionWeight = 0;

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

void ModifyBakedDiffuseLighting(float3 V, PositionInputs posInput, PreLightData preLightData, BSDFData bsdfData, inout BuiltinData builtinData)
{
    // Note: When baking reflection probes, we approximate the diffuse with the fresnel0
    builtinData.bakeDiffuseLighting *= GetDiffuseIndirectDimmer();
#ifdef _AXF_BRDF_TYPE_SVBRDF
    builtinData.bakeDiffuseLighting *= preLightData.diffuseFGD * GetDiffuseOrDefaultColor(bsdfData, _ReplaceDiffuseForIndirect).rgb;
#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
    // diffuse is Lambert, but we want the influence of the color table still...
    builtinData.bakeDiffuseLighting *= preLightData.singleBRDFColor * GetDiffuseOrDefaultColor(bsdfData, _ReplaceDiffuseForIndirect).rgb;
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
    switch (AXF_SVBRDF_BRDFVARIANTS_FRESNELTYPE)
    {
    case 1: F = F_FresnelDieletricSafe(Fresnel0ToIorSafe(bsdfData.fresnel0.r), LdotH); break;
    case 2: F = F_Schlick(bsdfData.fresnel0.r, LdotH); break;
    }

    // Evaluate normal distribution function
    float3  tsH = float3(dot(H, bsdfData.tangentWS), dot(H, bsdfData.bitangentWS), dot(H, bsdfData.normalWS));
    //float2  rotH = tsH.xy / tsH.z;
    float2  rotH = tsH.xy / max(0.00001, tsH.z);
    //float2  roughness = bsdfData.roughness.xy;
    float2  roughness = max(0.0001, bsdfData.roughness.xy);
    //if (bsdfData.roughness.y == 0.0) bsdfData.specularColor = float3(1,0,0);

    if (roughness.x * roughness.y <= 0.0001 && tsH.z < 1.0)
    {
        return 0;
    }

    float   N = exp(-Sq(rotH.x / roughness.x) - Sq(rotH.y / roughness.y));
    N /= max(0.0001, PI * roughness.x * roughness.y);
    //N /= (PI * roughness.x * roughness.y);

    switch (AXF_SVBRDF_BRDFVARIANTS_WARDTYPE)
    {
    case 0: N /= max(0.0001, 4.0 * Sq(LdotH) * Sq(Sq(tsH.z))); break; // Moroder
    case 1: N /= max(0.0001, 4.0 * NdotL * NdotV); break;             // Duer
    case 2: N /= max(0.0001, 4.0 * sqrt(NdotL * NdotV)); break;       // Ward
    }

    return bsdfData.specularColor * F * N;
}

float3  ComputeBlinnPhong(float3 H, float LdotH, float NdotL, float NdotV, PreLightData preLightData, BSDFData bsdfData)
{
    // See AxFGetRoughnessFromSpecularLobeTexture in AxFData
    float2  exponents = 2 * rcp(max(0.0001,(bsdfData.roughness.xy*bsdfData.roughness.xy))) - 2;

    // Evaluate normal distribution function
    float3  tsH = float3(dot(H, bsdfData.tangentWS), dot(H, bsdfData.bitangentWS), dot(H, bsdfData.normalWS));
    float2  rotH = tsH.xy;

    float3  N = 0;
    switch (AXF_SVBRDF_BRDFVARIANTS_BLINNTYPE)
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
    float  F = F_Schlick(bsdfData.fresnel0.r, LdotH);

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
    float   F = F_Schlick(bsdfData.fresnel0.r, LdotH);

    float3  tsH = float3(dot(H, bsdfData.tangentWS), dot(H, bsdfData.bitangentWS), dot(H, bsdfData.normalWS));

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
    if (AXF_SVBRDF_BRDFTYPE_DIFFUSETYPE)
    {
        float   diffuseRoughness = 0.5 * HALF_PI; // Arbitrary roughness (not specified in the documentation...)
        diffuseTerm = INV_PI * OrenNayar(bsdfData.normalWS, viewWS_UnderCoat, lightWS_UnderCoat, diffuseRoughness);
    }

    // Compute specular term
    float3  specularTerm = float3(1, 0, 0);
    switch (AXF_SVBRDF_BRDFTYPE_SPECULARTYPE)
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
    //return lerp(preLightData.specularCTFGDAtZeroF0[lobeIndex], preLightData.specularCTFGDReflectivity[lobeIndex], _CarPaint2_CTF0s[lobeIndex])*preLightData.singleBRDFColor;
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

    float   thetaH = FastACosPos(clamp(NdotH, 0, 1));
    float   thetaD = FastACosPos(clamp(LdotH, 0, 1));

    // Simple lambert
    float3  diffuseTerm = Lambert();

    // Apply multi-lobes Cook-Torrance
    float3  specularTerm = MultiLobesCookTorrance(bsdfData, NdotL, NdotV, NdotH, VdotH);

    // Apply BRDF color
    float3  BRDFColor = GetBRDFColor(thetaH, thetaD);
    diffuseTerm *= BRDFColor; // tocheck: dont forget handling BRDFColor for the indirect diffuse lighting!
                              // Also note that the monochromatic bsdfData.diffuseColor (in the case of CARPAINT2)
                              // is still applied in PostEvaluateBSDF and not here, like in the SVBRDF case!
    specularTerm *= BRDFColor;

    // Apply flakes
    //TODO_FLAKES
    specularTerm += CarPaint_BTF(thetaH, thetaD, (SurfaceData)0, bsdfData, /*useBSDFData:*/true);

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

    #if defined(_AXF_BRDF_TYPE_SVBRDF)

        float diffusePerceptualRoughness = AXF_SVBRDF_BRDFTYPE_DIFFUSETYPE ? preLightData.iblPerceptualRoughness : 1.0;

        // Evaluate the diffuse part
        ltcValue = EvaluateLTC_Area(isRectLight, center, right, up, halfLength, halfHeight,
                                    // LTC light cookies appear broken unless diffuse roughness is set to 1.
                                    transpose(preLightData.ltcTransformDiffuse), /*diffusePerceptualRoughness*/ 1.0f,
                                    lightData.cookieMode, lightData.cookieScaleOffset);

        ltcValue.a *= intensity * lightData.diffuseDimmer;

        lighting.diffuse = preLightData.diffuseFGD * ltcValue.rgb * ltcValue.a;

        // Evaluate the specular part
        ltcValue = EvaluateLTC_Area(isRectLight, center, right, up, halfLength, halfHeight,
                                    transpose(preLightData.ltcTransformSpecular), bsdfData.perceptualRoughness,
                                    lightData.cookieMode, lightData.cookieScaleOffset);

        ltcValue.a *= intensity * lightData.specularDimmer;

        lighting.specular = preLightData.specularFGD * ltcValue.rgb * ltcValue.a;

    #elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

        // Use Lambert for diffuse
        ltcValue = EvaluateLTC_Area(isRectLight, center, right, up, halfLength, halfHeight,
                                    k_identity3x3, 1.0,
                                    lightData.cookieMode, lightData.cookieScaleOffset);

        ltcValue.a *= intensity * lightData.diffuseDimmer;

        lighting.diffuse = preLightData.singleBRDFColor * ltcValue.rgb * ltcValue.a; // the BRDF specular flipflop color table also applies to diffuse

        //
        // Evaluate multi-lobes Cook-Torrance
        // Each CT lobe samples the environment with the appropriate roughness
        //
        for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
        {
            ltcValue = EvaluateLTC_Area(isRectLight, center, right, up, halfLength, halfHeight,
                                        transpose(preLightData.ltcTransformSpecularCT[lobeIndex]), RoughnessToPerceptualRoughness(bsdfData.roughness[lobeIndex]),
                                        lightData.cookieMode, lightData.cookieScaleOffset);

            ltcValue.a *= intensity * lightData.specularDimmer;

            float coeff = GetLTCAreaLightDimmer() * _CarPaint2_CTCoeffs[lobeIndex];

            lighting.specular += GetCarPaintSpecularFGDForLobe(preLightData, lobeIndex) * ltcValue.rgb * (ltcValue.a * coeff);
        }

        // Sample flakes as tiny mirrors
        // (update1: this is not really doing that, more like applying a BTF on a
        // lobe following the top normalmap. For them being like tiny mirrors, you would
        // need the N of the flake, and then you end up with the problem of normal aliasing)
        // (See also #define FLAKES_JUST_BTF, which makes us use the coat ltc transform and no FGD,
        // - in that case calculated irradiance should be the same as clearcoat, should be optimized)
        // todo_dir NdotV wrong
        ltcValue = EvaluateLTC_Area(isRectLight, center, right, up, halfLength, halfHeight,
                                    transpose(preLightData.ltcTransformFlakes), FLAKES_PERCEPTUAL_ROUGHNESS,
                                    lightData.cookieMode, lightData.cookieScaleOffset);

        ltcValue.a *= intensity * lightData.specularDimmer;

        lighting.specular += preLightData.singleFlakesComponent * ltcValue.rgb * ltcValue.a;

    #endif // carpaint

        // Evaluate the clear-coat
        if (HasClearcoat())
        {
            ltcValue = EvaluateLTC_Area(isRectLight, center, right, up, halfLength, halfHeight,
                                        transpose( preLightData.ltcTransformClearcoat), CLEAR_COAT_PERCEPTUAL_ROUGHNESS,
                                        lightData.cookieMode, lightData.cookieScaleOffset);

            ltcValue.a *= intensity * lightData.specularDimmer;

            // Use the complement of FGD value as an approximation of what is transmitted past the undercoat
            lighting.diffuse *= 1.0 - preLightData.coatFGD;
            lighting.specular = lerp(lighting.specular, bsdfData.clearcoatColor * ltcValue.rgb * ltcValue.a, preLightData.coatFGD);
        }

        // Save ALU by applying 'lightData.color' only once.
        lighting.diffuse  *= lightData.color;
        lighting.specular *= lightData.color;

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

    return lighting;
}

//-----------------------------------------------------------------------------
// EvaluateBSDF_SSLighting for screen space lighting
// ----------------------------------------------------------------------------
IndirectLighting EvaluateBSDF_ScreenSpaceReflection(PositionInputs posInput,
                                                    inout PreLightData   preLightData,
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

        // Init the light reflection hierarchy weight for the coat:
        preLightData.coatReflectionWeight = ssrLighting.a;

        // We use the coat-traced light according to how similar the base lobe roughness is to the coat roughness
        // (we can assume the coat is always smoother).
        // For AxF, even for CARPAINT we simplify by using a single lobe-coefficient-weighted averaged roughness
        // to compare and calculate the blend factor and base hierarchyWeight.
        // For that value, we use bsdfData.perceptualRoughness (set from GetScalarRoughness, see also AxFData.hlsl).
        //
        // - The roughness is equal to CLEAR_COAT_PERCEPTUAL_ROUGHNESS
        //   We use the fact the clear coat and that base layer lobe have the same roughness and use the SSR as the indirect specular signal.
        // - The roughness is superior to CLEAR_COAT_PERCEPTUAL_ROUGHNESS + 0.2.
        //   We cannot use the SSR for that base layer lobe.
        // - The roughness is within <= 0.2 away of CLEAR_COAT_PERCEPTUAL_ROUGHNESS, we lerp between the two behaviors.
        float coatSSRLightOnBottomLayerBlendingFactor = lerp(1.0, 0.0, saturate( (bsdfData.perceptualRoughness - CLEAR_COAT_PERCEPTUAL_ROUGHNESS) / 0.2 ) );

        // Calculate the base lobe reflectance factor (pre-integrated FGD)
        float3 baseLobeReflectanceFactor = 0;
        {
        #if defined(_AXF_BRDF_TYPE_SVBRDF)
            baseLobeReflectanceFactor = preLightData.specularFGD;

        #elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
            for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
            {
                float   coeff = _CarPaint2_CTCoeffs[lobeIndex];
                baseLobeReflectanceFactor += coeff * GetCarPaintSpecularFGDForLobe(preLightData, lobeIndex);
            }
            // TODO_flakes ?
            baseLobeReflectanceFactor *= GetSSRDimmer(); //now already in rebuilt specularFGD: * GetBRDFColor(thetaH, thetaD);
        #else
            // This is only possible if the AxF is a BTF type. However, there is a bunch of ifdefs do not support this third case
        #endif
        }

        // Add the contribution of the coat-traced light for this base lobe, if any:
        reflectanceFactor += baseLobeReflectanceFactor * coatSSRLightOnBottomLayerBlendingFactor;

        // Important: EvaluateBSDF_SSLighting() assumes it is the first light loop callback that contributes lighting,
        // we can thus directly set the reflectionHierarchyWeight instead of using UpdateLightingHierarchyWeights().

        // We initialize and keep track of the separate light reflection hierarchy weights and (see below) since only
        // reflectionHierarchyWeight is known to the light loop, normally a min() of all weights should be returned,
        // but here, we know the coat "consumes" at least as much than the bottom lobe, so the coatReflectionWeight dont
        // interfere with the (calculated from min of all) reflectionHierarchyWeight value returned.

        preLightData.baseReflectionWeight = ssrLighting.a * coatSSRLightOnBottomLayerBlendingFactor;
        reflectionHierarchyWeight = preLightData.baseReflectionWeight;
    }
    else
    {
        // No coat case:

#if defined(_AXF_BRDF_TYPE_SVBRDF)
        reflectanceFactor = preLightData.specularFGD;

#elif defined(_AXF_BRDF_TYPE_CAR_PAINT)
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

        //lightHierarchyData.coatReflectionWeight = will be unused anyway
        reflectionHierarchyWeight = ssrLighting.a;
        preLightData.baseReflectionWeight = ssrLighting.a;
    }

    lighting.specularReflected = ssrLighting.rgb * reflectanceFactor;

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
                                    inout PreLightData preLightData, EnvLightData lightData, BSDFData bsdfData,
                                    int _influenceShapeType, int _GPUImageBasedLightingType,
                                    inout float hierarchyWeight)
{

    IndirectLighting lighting;
    ZERO_INITIALIZE(IndirectLighting, lighting);

    if (_GPUImageBasedLightingType != GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION)
        return lighting;    // We don't support transmission

    float3  positionWS = posInput.positionWS;
    float   weight = 1.0;
    float3  envLighting = 0.0;

    float3  envSamplingDirForBottomLayer = preLightData.iblDominantDirectionWS_BottomLobeOnTop;

    // If the base already grabbed all light it needed from previous lights, skip it
    if (preLightData.baseReflectionWeight < 1.0)
    {
    #if defined(_AXF_BRDF_TYPE_SVBRDF)

        float   NdotV = ClampNdotV(preLightData.NdotV_UnderCoat);
        // Here we use bsdfData.clearcoatNormalWS: if there's no coat, bsdfData.clearcoatNormalWS == bsdfData.normalWS anyway.
        // The reason is that, normally, since GetModifiedEnvSamplingDir (off-specular effect) is roughness dependent,
        // we would have to store another direction (lightData is only used to escape the modification in case of planar probe)
        // and in case of carpaint, one for each lobe. However, if we would like to "correctly" take into account the effect, we would have
        // to calculate the effect on the bottom layer where directions are different, and then use FindAverageBaseLobeDirOnTop().
        // We decide to just apply the effect on top instead.
        // (FindAverageBaseLobeDirOnTop is alreayd an approximation ignoring under-horizon or TIR. If we saturated to the critical angle undercoat
        // and thus grazing when exiting on top, a tilt back for off-specular effect might in fact have no effect since the lobe could still
        // be under horizon. On the other hand, if we didn't have to saturate, a little tilt-back toward normal (from GetModifiedEnvSamplingDir)
        // should have translated into a bigger one on top because of angle range decompression.)
        envSamplingDirForBottomLayer = GetModifiedEnvSamplingDir(lightData, bsdfData.clearcoatNormalWS, preLightData.iblDominantDirectionWS_BottomLobeOnTop, preLightData.iblPerceptualRoughness, NdotV);

        // Note: using _influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
        float intersectionDistance = EvaluateLight_EnvIntersection(positionWS, bsdfData.clearcoatNormalWS, lightData, _influenceShapeType, envSamplingDirForBottomLayer, weight);
        // ...here the normal is only used for normal fading mode of the influence volume.

        // Another problem with having even two fetch directions is the reflection hierarchy that only supports one weight.
        // (TODO: We could have a vector tracking multiplied weights already applied per lobe that we update and that is
        // passed back by the light loop but otherwise opaque to it, with the single hierarchyWeight tracked alongside.
        // That way no "overlighting" would be done and by returning the hierarchyWeight = min(all weights) up to now,
        // we could potentially avoid artifacts in having eg the clearcoat reflection not available from one influence volume
        // while the base has full weight reflection. This ends up always preventing a blend for the coat reflection when the
        // bottom reflection is full. Lit doesn't have this problem too much in practice since only GetModifiedEnvSamplingDir
        // changes the direction vs the coat.)

        // Sample the pre-integrated environment lighting
        float4 preLD = SampleEnvWithDistanceBaseRoughness(lightLoopContext, posInput, lightData, envSamplingDirForBottomLayer, preLightData.iblPerceptualRoughness, intersectionDistance);
        weight *= preLD.w; // Used by planar reflection to discard pixel

        envLighting = GetSpecularIndirectDimmer() * preLightData.specularFGD * preLD.xyz;

        //-----------------------------------------------------------------------------
    #elif defined(_AXF_BRDF_TYPE_CAR_PAINT)

        // A part of this BRDF depends on thetaH and thetaD and should thus have entered
        // the split sum pre-integration. We do a further approximation by pulling those
        // terms out and evaluating them in the specular dominant direction,
        // for BRDFColor and flakes, see GetPreLightData.

        // Note: we don't use GetModifiedEnvSamplingDir() per lobe here, and see comment above about reflection hierarchy.
        float intersectionDistance = EvaluateLight_EnvIntersection(positionWS, bsdfData.clearcoatNormalWS, lightData, _influenceShapeType, envSamplingDirForBottomLayer, weight);

        #if USE_COOK_TORRANCE_MULTI_LOBES

        // Multi-lobes approach
        // Each CT lobe samples the environment with the appropriate roughness
        float probeSkipFactor = 1;
        for (uint lobeIndex = 0; lobeIndex < CARPAINT2_LOBE_COUNT; lobeIndex++)
        {
            float coeff = _CarPaint2_CTCoeffs[lobeIndex];

            float4 preLD = SampleEnvWithDistanceBaseRoughness(lightLoopContext, posInput, lightData, envSamplingDirForBottomLayer, preLightData.iblPerceptualRoughness[lobeIndex], intersectionDistance);

            //todotodo: try removing coeff
            envLighting += coeff * GetCarPaintSpecularFGDForLobe(preLightData, lobeIndex) * preLD.xyz;
            // Note: preLD.w is only used by planar probes, returning 0 if outside captured direction or 1 otherwise (the influence volume weight fades, not this).
            // Since this is only used for planar probes, even if we had used GetModifiedEnvSamplingDir() above, all directions would be the same in that case anyway
            // since GetModifiedEnvSamplingDir() doesn't do anything for planar probes.
            // For that reason, only one preLD.w needs to be used, no need to average them, they should all be the same.
            // sumWeights += preLD.w;
            probeSkipFactor = preLD.w;
        }
        // See discussion about reflection hierarchy above for SVBRDF, same thing here: When we will evaluate the coat, we will ignore its weight.
        weight *= probeSkipFactor;
        envLighting *= GetSpecularIndirectDimmer();
        //now already in rebuilt specularFGD: envLighting *= GetBRDFColor(thetaH, thetaD);

        // Sample flakes
        //TODO_FLAKES
        float   flakesMipLevel = 0;   // Flakes are supposed to be perfect mirrors
        envLighting += preLightData.singleFlakesComponent * SampleEnv(lightLoopContext, lightData.envIndex, envSamplingDirForBottomLayer, flakesMipLevel, lightData.rangeCompressionFactorCompensation, posInput.positionNDC).xyz;

        #else // USE_COOK_TORRANCE_MULTI_LOBES

        // Single lobe approach
        // We computed an average mip level stored in preLightData.iblPerceptualRoughness that we use for all CT lobes
        // Sample the actual environment lighting
        float4 preLD = SampleEnvWithDistanceBaseRoughness(lightLoopContext, posInput, lightData, envSamplingDirForBottomLayer, preLightData.iblPerceptualRoughness, intersectionDistance);
        float3  envLighting;

        envLighting = preLightData.specularCTFGDSingleLobe * GetSpecularIndirectDimmer();
        //TODO_FLAKES
        envLighting += preLightData.singleFlakesComponent;
        envLighting *= preLD.xyz;
        weight *= preLD.w; // Used by planar reflection to discard pixel

        #endif // USE_COOK_TORRANCE_MULTI_LOBES

    //-----------------------------------------------------------------------------
    #else // ..._AXF_BRDF_TYPE_CAR_PAINT
        // error / unknown BRDF type
    #endif // BRDF type

        UpdateLightingHierarchyWeights(preLightData.baseReflectionWeight, weight);
        envLighting *= weight;
    } // if (preLightData.baseReflectionWeight < 1.0)

    //-----------------------------------------------------------------------------
    // Evaluate the clearcoat component if needed
    if (!IsDebugHideCoat() && HasClearcoat() && (preLightData.coatReflectionWeight < 1.0))
    {
        weight = 1.0;
        // Evaluate clearcoat sampling direction
        float3  lightWS_Clearcoat = preLightData.iblDominantDirectionWS_Clearcoat;
        EvaluateLight_EnvIntersection(positionWS, bsdfData.clearcoatNormalWS, lightData, _influenceShapeType, lightWS_Clearcoat, weight);

        // Attenuate environment lighting under the clearcoat by the complement to the Fresnel term
        //todo_energy:
        envLighting *= 1.0 - preLightData.coatFGD;
        //envLighting *= Sq(1.0 - preLightData.coatFGD);

        // Then add the environment lighting reflected by the clearcoat (with mip level 0, like mirror)
        float4  preLD = SampleEnv(lightLoopContext, lightData.envIndex, lightWS_Clearcoat, 0.0, lightData.rangeCompressionFactorCompensation, posInput.positionNDC);
        weight *= preLD.w;

        // Update the coat weight, but make sure the weight is only then applied to the additional coat lighting:
        UpdateLightingHierarchyWeights(preLightData.coatReflectionWeight, weight);
        envLighting += weight * preLightData.coatFGD * preLD.xyz * bsdfData.clearcoatColor;

        hierarchyWeight = min(preLightData.baseReflectionWeight, preLightData.coatReflectionWeight);
    }
    else
    {
        hierarchyWeight = preLightData.baseReflectionWeight;
    }

    envLighting *= lightData.multiplier;

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
    // There is no AmbientOcclusion from data with AxF, but let's apply our SSAO
    AmbientOcclusionFactor aoFactor;
    GetScreenSpaceAmbientOcclusionMultibounce(posInput.positionSS, preLightData.NdotV_UnderCoat,
                                              bsdfData.perceptualRoughness,
                                              bsdfData.ambientOcclusion, bsdfData.specularOcclusion,
                                              GetColorBaseDiffuse(bsdfData), GetColorBaseFresnelF0(bsdfData), aoFactor);

    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);

    lightLoopOutput.diffuseLighting = bsdfData.diffuseColor * lighting.direct.diffuse + builtinData.bakeDiffuseLighting;
    lightLoopOutput.specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#if !defined(_AXF_BRDF_TYPE_SVBRDF) && !defined(_AXF_BRDF_TYPE_CAR_PAINT)
    // Not supported: Display a flashy color instead
    lightLoopOutput.diffuseLighting = 10 * float3(1, 0.3, 0.01);
#endif

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, lightLoopOutput);
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
