//-----------------------------------------------------------------------------
// SurfaceData and BSDFData
//-----------------------------------------------------------------------------
// SurfaceData is defined in Hair.cs which generates Hair.cs.hlsl
#include "Hair.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/SubsurfaceScattering/SubsurfaceScattering.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

//-----------------------------------------------------------------------------
// Texture and constant buffer declaration
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/LTCAreaLight/LTCAreaLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/PreIntegratedFGD/PreIntegratedFGD.hlsl"

#include "HairMarschner.hlsl"

#define DEFAULT_HAIR_SPECULAR_VALUE 0.0465 // Hair is IOR 1.55

// Debug flag num
#define DEBUG_LOBE_0_OFF 0
#define DEBUG_LOBE_1_OFF 1
#define DEBUG_LOBE_2_OFF 2
#define DEBUG_LOBE_3_OFF 3
#define DEBUG_IBL_OFF    4


// Precomputed lighting data to send to the various lighting functions
struct PreLightData
{
    float NdotV;        // Could be negative due to normal mapping, use ClampNdotV()

    float3 lobeVariance; // for R, TT, TRT

    // IBL
    float3 iblR;                     // Reflected specular direction, used for IBL in EvaluateBSDF_Env()
    float  iblPerceptualRoughness;

    float3 specularFGD;              // Store preintegrated BSDF for both specular and diffuse
    float  diffuseFGD;
};


//-----------------------------------------------------------------------------
// Helper functions/variable specific to this material
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Marschner / Deon (Weta Digital) / Chiang (Disney)
//-----------------------------------------------------------------------------
bool UseHairMarschnerB()
{
    bool ret = false;
#ifdef _MATERIAL_FEATURE_HAIR_MARSCHNER_B
    ret = true;
#endif
    return ret;
}

bool TestDebugFlagNum(uint num)
{
    return HasFlag(_DebugFlags, (1<<num));
}

// https://www.pbrt.org/hair.pdf 
// p13
// https://disney-animation.s3.amazonaws.com/uploads/production/publication_asset/152/asset/eurographics2016Fur_Smaller.pdf 
// p5 eq(7)
void LongitudinalPerceptualRoughnessToLobeVarianceDisney(float perceptualRoughness, out float3 lobeVariance)
{
    float beta_m = perceptualRoughness;
    lobeVariance[0] = Sq(0.726 * beta_m + 0.812 * Sq(beta_m) + 3.7 * PositivePow(beta_m, 20));
    lobeVariance[1] = 0.25 * lobeVariance[0];
    lobeVariance[2] = 4 * lobeVariance[0];
}
// https://blog.selfshadow.com/publications/s2016-shading-course/karis/s2016_pbs_epic_hair.pdf
// Karis 2016 p44 see p48 for IBL modification hack
void LongitudinalPerceptualRoughnessToLobeVariance(float perceptualRoughness, out float3 lobeVariance)
{
    lobeVariance[0] = Sq(perceptualRoughness);
    lobeVariance[1] = 0.5 * lobeVariance[0];
    lobeVariance[2] = 2 * lobeVariance[0];
}

// https://disney-animation.s3.amazonaws.com/uploads/production/publication_asset/152/asset/eurographics2016Fur_Smaller.pdf
// p5 eq(8)
void AzimuthalPerceptualRoughnessToLogisticScaleDisney(float perceptualRoughness, out float logisticScale)
{
    logisticScale = sqrt(PI/8) 
                    * (0.265 * perceptualRoughness + 1.194 * Sq(perceptualRoughness) + 5.372 * PositivePow(perceptualRoughness, 22));
}

// https://disney-animation.s3.amazonaws.com/uploads/production/publication_asset/152/asset/eurographics2016Fur_Smaller.pdf
// p6 eq(9)
void AbsorptionCoefficientFromMSAlbedoAndAzimuthalRoughness(float3 msAlbedo, float perceptualRoughness, out float3 absorptionCoefficient)
{
    float beta = perceptualRoughness;
    float poly = (5.969 - 0.215*beta + 2.532*beta*beta - 10.73*beta*beta*beta + 5.574*beta*beta*beta*beta + 0.245*beta*beta*beta*beta*beta);
    float3 sigma = Sq(log(msAlbedo)/poly);
    absorptionCoefficient = sigma;
    //absorptionCoefficient = 0;
}

// https://disney-animation.s3.amazonaws.com/uploads/production/publication_asset/152/asset/eurographics2016Fur_Smaller.pdf
// p8 and p4 figure 3
// https://www.pbrt.org/hair.pdf
// p21
//
// Variance of regular logistic function is
//
//    variance = scale^2 * pi^2 / 3
//
// Scale factor of compact support logistic used by Disney is
//
//    scaleDisney = logisticScaleModifier
//
// If we assume the same variance from the modified scale factor (see p4 figure 3), we have:
//
//    varianceDisney = scaleDisney^2 * pi^2 / 3
//
void AzimuthalPerceptualRoughnessToVariance(float perceptualRoughness, out float azimuthalVariance)
{
    const float toVarianceFactor = (Sq(PI)/3);
    float scaleDisney;
    AzimuthalPerceptualRoughnessToLogisticScaleDisney(perceptualRoughness, scaleDisney);
    azimuthalVariance = Sq(scaleDisney) * toVarianceFactor;
}


// From StackLit, cleanup TODO
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


// Longitudinal scattering function:
//
// MpGaussian function from
//
// https://blog.selfshadow.com/publications/s2016-shading-course/karis/s2016_pbs_epic_hair.pdf
// p18
//
// approximation to Deon et al. Azimuthal scattering function, see:
// 
// http://www.eugenedeon.com/wp-content/uploads/2014/04/egsrhair.pdf
// Deon 2011 p3 eq(7)
// https://cg.ivd.kit.edu/publications/pubhanika/2013_hairbrief.pdf
// Deon 2013 p2 eq(5)
// http://www.eugenedeon.com/wp-content/uploads/2019/06/hair_supplemental.pdf
// Deon 2014 p2 eq(5)
//
// Note that Karis slides show a regular Gaussian, but it is indexed with
// sine of angles instead of directly angles. Presumably this is a good fit?
//
// But his equation also directly adds the shift (due to hair "cuticle scales")
// in the terms too.
//
// This means the shiftAlpha terms will need to be proportional and thus taken as sines.
//
// On the other end, the shift angles should be small, Matt Pharr mentions 2 degrees
// https://www.pbrt.org/hair.pdf
// Pharr p9
// but Marschner goes up to 15 degrees 
// (wrt to an half_theta parameterized Gaussian, so the true shift is twice that).
// 
// 
// For theta := shiftAlpha we have, let's say for:
//
// theta = 2 degrees = 0.03490658503988659153847381536977 rad
// sin(theta) = 0.03489949670250097164599518162533
// cos(theta) = 0.99939082701909573000624344004393
// cos(theta) ~= 1 - 0.5*theta*theta 
//            = 0.99939076516042658280130651290124
//
// and for 15 degrees:
//
// theta = 0.26179938779914943653855361527329
// sin(theta) = 0.25881904510252076234889883762405
// 1 - 0.5*theta*theta
//            = 0.96573054027399528257349135069487
// cos(theta) = 0.9659258262890682867497431997289
//
// so we should be good with small angles approximations.
//
// See also section 1.3.4 in Pharr, p22 to p25.
// While Karis sums directly sines of shift in his Gaussian,
// we could use the complete rotation as Pharr on p25.
//
// TODO


//ClampRoughnessForAnalyticalLights
float MpGaussian(float sti, float sto, float variance)
{
    float val;
    val = exp(-0.5 * Sq(sti + sto)/variance) / sqrt(variance * TWO_PI);
    return val;
}

// Azimuthal dependency in the perfect specular longitudinal reflection cone:
//
// https://blog.selfshadow.com/publications/s2016-shading-course/karis/s2016_pbs_epic_hair.pdf
//
// p19 Karis mentions using the azimuth dependency on longitudinal shift from hair "cuticle scales"
// that Deon introduced in:
//
// Deon2014:
// http://www.eugenedeon.com/project/a-fiber-scattering-model-with-non-separable-lobes/
// http://www.eugenedeon.com/wp-content/uploads/2015/03/nonsephair2014.pdf
// http://www.eugenedeon.com/wp-content/uploads/2019/06/hair_supplemental.pdf
// p3 (before and with eq(6))
// 
// Notice that:
//
// thetaRShifted = − arcsin(  sin(thetaI) − B )
//              =   arcsin( -sin(thetaI) + B )
//               =   arcsin( sin(-thetaI) + B )
// 
// where B = 2*sin(shiftAlpha)*( cos(phi/2)*cos(shiftAlpha)*cos(thetaI) + sin(shiftAlpha)*sin(thetaI) )
// 
// B is zero when original shiftAlpha is zero, thus B acts as a direct additive bias (shift) towards the sin(thetaI)
// (ie shift not directly in rads)
//
// Since Karis' Gaussian approximation of the longitudinal scattering function Mp (Deon2011)
// uses just a regular Gaussian and add directly *sin of angles* as part of the approximation,
// we could use the B term above directly. 
// 
// Note that for consistency, the same rule should apply to the other shifts
// (or tilts noted alpha in original Marschner 2003 paper): they should be sin(shiftAlpha_lobe#). 
// 
// If the shiftAlpha we use above is consistent with Deon2014, we should thus have:
// 
// modifiedSinShiftAlpha = 2*sinShiftAlpha*( cos(phi/2)*cos(sinShiftAlpha)*cos(thetaI) + sinShiftAlpha*sin(thetaI) )
// modifiedSinShiftAlpha = 2*sinShiftAlpha*( cos(phi/2)*sqrt(1-sinShiftAlpha*sinShiftAlpha)*cos(thetaI) + sinShiftAlpha*sin(thetaI) )
//
// But for small angles, like we showed above, shiftAlpha could be used directly:
//
// Note that this is only valid for the p = 0 path, ie R interaction (reflection).
float GetModifiedSinShiftAlpha(float shiftAlpha, float cosHalfPhi /* cos(phi/2) */, float cti, float sti)
{
    float sinShiftAlpha = shiftAlpha;
    float cosShiftAlpha = 1 - 0.5*shiftAlpha*shiftAlpha;
    float cosSinShiftAlpha = /* cos(sinShiftAlpha) ~= cos(shiftAlpha)*/ cosShiftAlpha;

    float modifiedSinShiftAlpha = 2*sinShiftAlpha * ( cosHalfPhi*cosSinShiftAlpha*cti + sinShiftAlpha*sti );
    return modifiedSinShiftAlpha;
}

// Azimuthal dependency in the longitudinal roughness:
// In Deon hair_supplemental.pdf (see above), eq(6) also gives this for the R paths:
//
// In Deon 2011, variance is simply 
//    variance = longitudinalRoughness^2
// his new term is:
//    variance = longitudinalRoughness^2 * 2 * cos(phi/2)^2
//
// We will thus transform our variance with * 2 * cos(phi/2)^2 :
void ModifyVarianceRForAzimuth(inout float3 lobeVariance, float cosHalfPhi)
{
    lobeVariance[0] = lobeVariance[0] * 2 * Sq(cosHalfPhi);
}

float3 GetLobeShiftAlpha(float shift)
{
    float3 lobeShiftAlpha;
    // pharr p22 or Marschner 2003.
    //
    // These should be *added* to thetaI
    lobeShiftAlpha[0] = 2*shift;
    lobeShiftAlpha[1] = -1*shift;
    lobeShiftAlpha[2] = -4*shift;

    return lobeShiftAlpha;
}

// Azimuthal scattering function:
//
// Unfortunately, no closed form for rough azimuthal scattering functions exist
// so for now, use Marschner's like Karis p20, 29 and 32.
// The problem is that even for dirac light rays, after passing one scattering
// boundary, there is a lobe, not a single ray, so even knowing h is still a
// very gross approximation (like the A terms using Fresnel for that matter).
//
// For the R path, at least N_0 is correct for dirac lights:
float3 NpR(float cosHalfPhi, float3 attenuation)
{
    return 0.25 * cosHalfPhi * attenuation;
}
// TODOTODO logistic to use azimuthal roughness in the term too
// Karis p29:
float3 NpTT(float cosPhi, float3 attenuation)
{
    return exp(-3.65*cosPhi - 3.98) * attenuation;
}
// Karis p32:
float3 NpTRT(float cosPhi, float3 attenuation)
{
    return exp(17*cosPhi - 16.78) * attenuation;
}

// Attenuation for azimuthal scattering function:
// http://www.eugenedeon.com/wp-content/uploads/2014/04/egsrhair.pdf
// p5 eq(12, 13, 14)
// Note again for p >= 1, these are for ideal cylinders and taken as approximations
// as split (or factored out) attenuation factors applied for all paths in the
// "lobe propagation".
// Note that by symmetry of R vs T = 1-R for perfectly smooth interfaces, the f0
// or eta used in our Fresnel terms never changes (see eq(13), eq(14) Deon 2014 above)
//
// See also
// https://www.pbrt.org/hair.pdf
// Pharr p13 to p17:
// TODOTODO try to use a recursive formulation.

// TODOTODO: IOR to boost primary reflections, see Disney 2016.

float3 ApR(BSDFData bsdfData, float LdotH, float cti, float cosGamma, bool usePBRTAngles = false)
{
    float3 F = F_Schlick(bsdfData.fresnel0, LdotH);
    // ... so take the term with an angle wrt / as if there was
    // a "pseudo microfacet" (we use that term because Fresnel is only correctly defined
    // as reflectance for a perfectly smooth planar interface) that gave the possibility
    // of that bounce with this exact F reflectance value. The (pseudo) NDF (with shadowing-masking)
    // would be the distributions we use for our scattering profiles M and N.
    // Of course in the hair context, we're more empirical even than having built a proper
    // microfacet model, we directly have scattering functions, so the choice of F and angle
    // to be used is not completely clear.
    // The R case is easier to understand because for dirac lights, there's no multiple
    // path interactions after crossing a boundary, the dirac light collapse the lobe
    // "spawned" by the V ray.
    // We're still left with some choice of F() and angle to use, as the literature
    // clearly demonstrates: Deon et al. 2011 choose p5 eq(12) the "microfacet" perspective,
    // which makes sense if we think that the reflection cone profile is *mollified* to not
    // have exactly thin support but spread out over the whole elevation (longitudinal) angular
    // range: see figure 7 p5 (and figure 4 p4 to see the mollification of the cone by the M
    // longitudinal distribution).
    // Pixar also discusses choices of F( ) even for R in DataDrivenHairScattering/paper.pdf
    // p4 section 4.1 (see also p5 figure 5).
    //
    // Finally, Matt Pharr clearly uses a Fresnel term for F that clamps the "pseudo microfacet":
    // in Pharr p17, for p = 0, we see that they use cosThetaO * cosGammaO (see ***), which is the
    // cosine of the angle in 3D between w_o and the normal of a perfect cylinder hit at height
    // "h" on its width (also ignoring roughness for F)
    // We could say that we could decompose the w_o and w_i rays by projecting them in the normal
    // plane and then only take azimuthal half difference angle for F, but as noted by Pixar, this
    // looses some longitudinal influence. They use the fully free Deon 2011 F term for R.
    // Karis does the same thing, p20.
    //
    // Using the Pharr term keeps the F term more constrained. Notice that when thinking about the
    // mollified case (rough) vs perfectly smooth cylindrical ideal case, even in the perfect case
    // we model the far field agregate by thinking of the ray contribution (total irradiance) being
    // spread out on the width of the fiber (=2 because h go from -1 to 1), consistent with the
    // fact that we still have some degree of liberty to select from many normals all in the normal plane
    // (although in near field the distribution of irradiance across the width is not uniform).
    // 
    // We can use Pharr or Deon 2011 here. 
    // For higher orders, Pharr mentions that even in the rough
    // case, since we're already approximating the A terms with the smooth case, we still consider refraction
    // cancelling out on exit, and there's no need to complify the already approximate F terms
    // (which really should be T = 1 - integral F*distro, see eg StackLit model with
    // R average of lobe = integrated FGD and T = 1-R, but additionnal roughness added on a lobe has some
    // TIR losses implications too!)
    //
    // Also see p36 of Pharr: even for this F() term we could have a choice because our usage of w_i or w_o
    // changes the evaluation, our scattering function (including Fresnel and other A terms) is not reciprocal.

    // if pharr term
    // TODO TOCHECK:
    //
    // Need Pharr's cosThetaO and cosGammaO, but check how we get h in GetHeightForTT from Karris root solution approximation:
    // we could get back a dependency on the sign of h in that case which require care because the scattering function
    // isn't reciprocal. (In PBRT, see p4 of the hair paper, the parametric curves intersection yields the h
    // naturally.) p17 cosGammaO, p10 cosThetaO (***)
    //
    // ***
    // Note that where it could matter - non reciprocity - PBRT (Pharr) uses w_O and (other)*_O as V (see also Sample_f p29)
    // Here in the code, like for StackLit, in Mitsuba and Marschner paper, we use _I as V.
    //
    // Here for cosGamma it doesn't matter.
    //
    if(usePBRTAngles)
    {
        F = F_Schlick(bsdfData.fresnel0, cti*cosGamma);
    }
    return F;
}

// eq(14): cos(thetaD) * cos(arcsin(h)) = cos(thetaD) * sqrt(1-Sq(h))
// see also Karis p23.
float3 ApTT(BSDFData bsdfData, float cosThetaD, float h, float3 transmittanceFactor)
{
    float3 F = F_Schlick(bsdfData.fresnel0, cosThetaD * sqrt(1 - Sq(h)) );
    float3 attenuation = Sq(1 - F) * transmittanceFactor;
    return attenuation;
}

// Karis p32
float3 ApTRT(BSDFData bsdfData, float cosThetaD, float h, float3 transmittanceFactor)
{
    // TODO: normally, only the first F term is different. By symmetry (but again such
    // symmetry is only valid for perfectly smooth interfaces), the rest should all use
    // the same and could use a recursive form, see Pharr p17 or Deon 2014 p5
   
    h = 0.5*sqrt(3);
    float3 F = F_Schlick(bsdfData.fresnel0, cosThetaD * sqrt(1-Sq(h)));
    float3 attenuation = Sq(1-F) * transmittanceFactor;
    return attenuation;
}


// Transmittance Factors:
//
//https://blog.selfshadow.com/publications/s2016-shading-course/karis/s2016_pbs_epic_hair.pdf
// p28 from modifying Disney's transmittance factor:
//https://graphics.pixar.com/library/DataDrivenHairScattering/paper.pdf
// p5 eq(5)
//
// The goal in the p28 Karis factor modification is to make the Zeta(C) function in the Pixar
// term the least costly possible while making it so that transmittance is equal to C when
// the path relative length is maximal (the Pixar paper p5 states when the exponent is maximal,
// but should be read as maximally negative, as the other endpoint is T = 1, ie no attenuation
// for zero length paths).
//
// Tpixar = e^(-p * zeta(C) abs(cos(gamma_t)/cos(thetaD)))        (where p is the lobe order <= 2)
//
//        = (e^(zeta(C))^( -p *abs(cos(gamma_t)/cos(thetaD)) )
//
// note the max of the cos terms is 1, so if we pick zeta(C) = ln(C), we can see that we have
//
//        = C^( -p *abs(cos(gamma)/cos(thetaD)) )
//
// but to get the behavior of the exponential decay, C must be > 1. 
// Otherwise, by keeping C < 1, you can remove the sign and obtain the same inverse relation to
// the cos terms.
//
// There's a 1/2 that pops out in their used equation, presumably providing a better fit.
//
// Note that
//
//    gamma_t = arcsin(h/eta_modified)
//    cos(gamma_t) = cos( arcsin(h/eta_modified) ) = sqrt(1 - (h/eta_modified)^2)
//

// TODO: test full, but for now, use p26 Karis approximation for the same IOR we use, 1.55
float GetModifiedIOR(float cosThetaD)
{
    float iorPrime = (1.19/cosThetaD) + 0.36*cosThetaD;
    return iorPrime;
}

//TODO: setup absorptionCoefficient in prelightData etc.

bool UseDisneyAbsportion()
{
    return false;
}

float3 GetTransmittanceFactorDisney(BSDFData bsdfData, PreLightData preLightData, uint p, float cosThetaT, float cosGammaT)
{
    return 0;
    // TODO:
    //return exp(-sigma_a * (2 * cosGammaT / cosThetaT))
}

// There's technically no roots for finding paths matching a direction phi in a non smooth case,
// but again we use the ideal directions to estimate.
// Karis p25
float GetHeightForTT(float cosPhi, float cosHalfPhi, float iorPrime)
{
    float h = (1 + (1/iorPrime)*(0.6 - 0.8*cosPhi))*cosHalfPhi;
    return h;
}

float3 GetTransmittanceFactor(BSDFData bsdfData, PreLightData preLightData, uint p, float cosThetaD, float cosGammaT)
{
    float3 factor = float3(1.0,1.0,1.0);
    //float iorPrime = GetModifiedIOR(cosThetaD);
    //float cosGammaT = sqrt(1 - (h*h)/(iorPrime*iorPrime) );
    float3 colorOrAbsportion = bsdfData.secondarySpecularTint;
    switch (p)
    {
        case 0:
            break;
        case 1:
            factor = PositivePow(colorOrAbsportion, 0.5*cosGammaT/cosThetaD);
            break;
        case 2:
            factor = PositivePow(colorOrAbsportion, 0.8/cosThetaD);
            break;
    }
    return factor;
}

//-----------------------------------------------------------------------------

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

void ClampRoughness(inout BSDFData bsdfData, float minRoughness)
{
    bsdfData.perceptualRoughness = max(RoughnessToPerceptualRoughness(minRoughness), bsdfData.perceptualRoughness);
    bsdfData.secondaryPerceptualRoughness = max(RoughnessToPerceptualRoughness(minRoughness), bsdfData.secondaryPerceptualRoughness);
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
    else if (HasFlag(surfaceData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER))
    {
        bsdfData.secondaryPerceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.secondaryPerceptualSmoothness);        
        bsdfData.specularTint = surfaceData.specularTint;
        bsdfData.specularShift = surfaceData.specularShift;
        bsdfData.secondarySpecularShift = surfaceData.secondarySpecularShift;
        bsdfData.anisotropy = 0.8; // For hair we fix the anisotropy
        bsdfData.azimuthalPerceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.azimuthalSmoothness);
        bsdfData.indexOfRefraction = surfaceData.indexOfRefraction;
    }
    else if (UseHairMarschnerB())
    {
        bsdfData.azimuthalPerceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.azimuthalSmoothness);
        bsdfData.indexOfRefraction = surfaceData.indexOfRefraction;

        bsdfData.secondaryPerceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surfaceData.secondaryPerceptualSmoothness);        
        bsdfData.specularTint = surfaceData.specularTint;
        bsdfData.secondarySpecularTint = surfaceData.secondarySpecularTint;
        bsdfData.specularShift = surfaceData.specularShift;
        bsdfData.secondarySpecularShift = surfaceData.secondarySpecularShift;

        float roughness1 = PerceptualRoughnessToRoughness(bsdfData.perceptualRoughness);
        float roughness2 = PerceptualRoughnessToRoughness(bsdfData.secondaryPerceptualRoughness);

        bsdfData.perceptualRoughness = ClampRoughnessForAnalyticalLights(bsdfData.perceptualRoughness);
        bsdfData.secondaryPerceptualRoughness = ClampRoughnessForAnalyticalLights(bsdfData.secondaryPerceptualRoughness);

        bsdfData.specularExponent          = RoughnessToBlinnPhongSpecularExponent(roughness1);
        bsdfData.secondarySpecularExponent = RoughnessToBlinnPhongSpecularExponent(roughness2);

        bsdfData.anisotropy = 0.8; // For hair we fix the anisotropy
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
        result = TransformWorldToViewDir(surfaceData.normalWS) * 0.5 + 0.5;
        break;
    case DEBUGVIEW_HAIR_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        result = TransformWorldToViewDir(surfaceData.geomNormalWS) * 0.5 + 0.5;
        break;
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
        result = TransformWorldToViewDir(bsdfData.normalWS) * 0.5 + 0.5;
        break;
    case DEBUGVIEW_HAIR_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
        result = TransformWorldToViewDir(bsdfData.geomNormalWS) * 0.5 + 0.5;
        break;
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


// This function is call to precompute heavy calculation before lightloop
PreLightData GetPreLightData(float3 V, PositionInputs posInput, inout BSDFData bsdfData)
{
    PreLightData preLightData;
    // Don't init to zero to allow to track warning about uninitialized data

#if _USE_LIGHT_FACING_NORMAL
    float3 N = ComputeViewFacingNormal(V, bsdfData.hairStrandDirectionWS);
#else
    float3 N = bsdfData.normalWS;
#endif

    preLightData.NdotV = dot(N, V);
    float clampedNdotV = ClampNdotV(preLightData.NdotV);

    float unused;

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY)
        || UseHairMarschnerB())
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
    else if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER))
    {
        N = ComputeViewFacingNormal(V, bsdfData.hairStrandDirectionWS);
        preLightData.iblPerceptualRoughness = bsdfData.perceptualRoughness;
        preLightData.specularFGD = 0.0;
        preLightData.diffuseFGD = 1.0;
    }

    if (UseHairMarschnerB())
    {
        LongitudinalPerceptualRoughnessToLobeVariance(bsdfData.perceptualRoughness, preLightData.lobeVariance);
    }
    else
    {
        preLightData.lobeVariance = float3(0,0,0);
    }

    // Stretch hack... Copy-pasted from GGX, ALU-optimized for hair.
    // float3 iblN = normalize(lerp(bsdfData.normalWS, N, bsdfData.anisotropy));
    float3 iblN = N;
    preLightData.iblR = reflect(-V, iblN);
    preLightData.iblPerceptualRoughness *= saturate(1.2 - abs(bsdfData.anisotropy));

    return preLightData;
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

    // Add GI transmission contribution to bakeDiffuseLighting, we then drop backBakeDiffuseLighting (i.e it is not used anymore, this save VGPR)
    {
        // TODO: disabled until further notice (not clear how to handle occlusion).
        //builtinData.bakeDiffuseLighting += builtinData.backBakeDiffuseLighting * bsdfData.transmittance;
    }

    // Premultiply (back) bake diffuse lighting information with diffuse pre-integration
    builtinData.bakeDiffuseLighting *= preLightData.diffuseFGD * bsdfData.diffuseColor;
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

CBSDF EvaluateBSDF(float3 V, float3 L, PreLightData preLightData, BSDFData bsdfData)
{
    CBSDF cbsdf;
    ZERO_INITIALIZE(CBSDF, cbsdf);

    float3 T = bsdfData.hairStrandDirectionWS;
    float3 N = bsdfData.normalWS;

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER))
    {
        // To account for normal maps, we re-orthogonalize and compute
        // a new tangent.
        float3 bT = cross(N, T);
        T = SafeNormalize(cross(bT, N));
    }


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

    float LdotV, NdotH, LdotH, invLenLV;
    GetBSDFAngle(V, L, NdotL, NdotV, LdotV, NdotH, LdotH, invLenLV);

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY))
    {
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
    else if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_MARSCHNER))
    {
        // Get a normal aligned with the view vector so we know PhiI should be zero
        //float3 hairNorm = SafeNormalize(V - T * dot(V, T));
        float3 hairNorm = N;
        float thetaI, thetaL, phiI, phiL;
        ComputeHairRelativeAngles(V, T, hairNorm, thetaI, phiI);
        ComputeHairRelativeAngles(L, T, hairNorm, thetaL, phiL);
        float phiD = abs(phiL - phiI);
        float3 Fres = HairFresnelAllLobes(bsdfData.indexOfRefraction, phiD);
        
        // R
        cbsdf.specR = Fres.xxx * evalMTerm(thetaI, 0.5 * (thetaI + thetaL), bsdfData.perceptualRoughness, bsdfData.specularShift) * evalNTermR(phiD * 0.5, bsdfData.azimuthalPerceptualRoughness);
        // TRT
        cbsdf.specR += bsdfData.specularTint * bsdfData.specularTint * Fres.zzz * evalMTerm(thetaI, 0.5 * (thetaI + thetaL), bsdfData.secondaryPerceptualRoughness, -bsdfData.specularShift) * evalNTermTRT(phiD * 0.5, bsdfData.azimuthalPerceptualRoughness);
        // TT
        cbsdf.specT = bsdfData.specularTint * Fres.yyy * evalMTerm(thetaI, 0.5 * (thetaI + thetaL), bsdfData.secondaryPerceptualRoughness, 0) * evalNTermTT(phiD * 0.5, bsdfData.azimuthalPerceptualRoughness);
        
        cbsdf.specR *= evalHairCosTerm(thetaL, thetaI);
        
        // What you see below is an extraordinarily hacky and legendarily moronic way of faking
        // a multiple scattering effect.  Good boys and girls should not duplicate what you see
        // below, and it should always be held up as a scarlet letter of how to be foolish.
        // That said, it is here because it sometimes looks okay, and that's really all there is to it.
        float3 diffAlb = bsdfData.specularTint * (2.0 - bsdfData.specularTint);
        cbsdf.diffR = diffAlb * PI * evalMTerm(thetaI, 0.5 * (thetaI + thetaL), lerp(bsdfData.secondaryPerceptualRoughness, 1.0, 1-Fres.y), 0) * evalNTermTRT(phiD * 0.5, lerp(bsdfData.azimuthalPerceptualRoughness, 1.0, 1-Fres.y));
    }
    else if(UseHairMarschnerB())
    {
#ifndef _USE_LIGHT_FACING_NORMAL
        float cosTL = dot(T, L);
        //float sinTL = sqrt(saturate(1.0 - cosTL * cosTL));
#endif
        float cosTV = dot(T, V);
        float sinTV = sqrt(saturate(1.0 - cosTV * cosTV));

        float sti = cosTV; // sinThetaI: this is not an error, TdotV is the cos of the PI/2 - thetaI angle, thus the sin(thetaI)
        float cti = sinTV; // 
        float sto = cosTL; // sinThetaO

        float3 LprojInNormalPlane = GetOrthogonalComponent(L, T, false /*testSingularity*/);
        float3 VprojInNormalPlane = GetOrthogonalComponent(V, T, false);

        float cosPhi = dot(VprojInNormalPlane, LprojInNormalPlane);
        float cosHalfPhi = sqrt(saturate(0.5 + 0.5*cosPhi));
        float cosThetaD = cos(0.5*abs(FastASin(sti) - FastASin(sto)));

        float iorPrime = GetModifiedIOR(cosThetaD);
        float h = GetHeightForTT(cosPhi, cosHalfPhi, iorPrime);
        float cosGamma = sqrt(saturate( 1 - (h*h) ));
        float cosGammaT = sqrt(saturate( 1 - (h*h)/(iorPrime*iorPrime) ));

        float3 lobeShiftAlpha = GetLobeShiftAlpha(bsdfData.specularShift);


        // R paths estimation
        if(!TestDebugFlagNum(DEBUG_LOBE_0_OFF))
        {
            lobeShiftAlpha[0] = GetModifiedSinShiftAlpha(lobeShiftAlpha[0], cosHalfPhi, cti, sti);
            ModifyVarianceRForAzimuth(preLightData.lobeVariance, cosHalfPhi);

            // See ***, doesn't matter here if shift is on sti or sto obviously. Pharr modifies his thetaI,
            // hence our thetaO.
            float M = MpGaussian(sti + lobeShiftAlpha[0], sto, preLightData.lobeVariance[0]);
            float3 A = ApR(bsdfData, LdotH, cti, cosGamma, /* usePBRTAngles */ false); // TODOTODO totest
            float3 N = NpR(cosHalfPhi, A);
            cbsdf.specR += M * N; // note: N includes multiply by attenuation factor, the later has both Fresnel
        }
        // TT
        if(!TestDebugFlagNum(DEBUG_LOBE_1_OFF))
        {
            float M = MpGaussian(sti + lobeShiftAlpha[1], sto, preLightData.lobeVariance[1]);
            float3 T = GetTransmittanceFactor(bsdfData, preLightData, 1, cosThetaD, cosGammaT);
            float3 A = ApTT(bsdfData, cosThetaD, h, T);
            float3 N = NpTT(cosHalfPhi, A);
            cbsdf.specT += M * N; // note: N includes multiply by attenuation factor, the later has both Fresnel and transmittanceFactor
        }
        // TRT
        if(!TestDebugFlagNum(DEBUG_LOBE_2_OFF))
        {
            float M = MpGaussian(sti + lobeShiftAlpha[2], sto, preLightData.lobeVariance[2]);
            float3 T = GetTransmittanceFactor(bsdfData, preLightData, 2, cosThetaD, cosGammaT);
            float3 A = ApTRT(bsdfData, cosThetaD, h, T);
            float3 N = NpTRT(cosHalfPhi, A);
            cbsdf.specR += M * N;
        }

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

    // TODO

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

    // Note: RGB is already premultiplied by A.
    // TODO: we should multiply all indirect lighting by the FGD value only ONCE.
    lighting.specularReflected = ssrLighting.rgb /* * ssrLighting.a */ * preLightData.specularFGD;
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

    float3 envLighting;
    float3 positionWS = posInput.positionWS;
    float weight = 1.0;

    float3 R = preLightData.iblR;

    // Note: using influenceShapeType and projectionShapeType instead of (lightData|proxyData).shapeType allow to make compiler optimization in case the type is know (like for sky)
    EvaluateLight_EnvIntersection(positionWS, bsdfData.normalWS, lightData, influenceShapeType, R, weight);

    float iblMipLevel;
    // TODO: We need to match the PerceptualRoughnessToMipmapLevel formula for planar, so we don't do this test (which is specific to our current lightloop)
    // Specific case for Texture2Ds, their convolution is a gaussian one and not a GGX one - So we use another roughness mip mapping.
    if (IsEnvIndexTexture2D(lightData.envIndex))
    {
        // Empirical remapping
        iblMipLevel = PositivePow(preLightData.iblPerceptualRoughness, 0.8) * uint(max(_ColorPyramidScale.z - 1, 0));
    }
    else
    {
        iblMipLevel = PerceptualRoughnessToMipmapLevel(preLightData.iblPerceptualRoughness);
    }

    float4 preLD = SampleEnv(lightLoopContext, lightData.envIndex, R, iblMipLevel);
    weight *= preLD.a; // Used by planar reflection to discard pixel

    envLighting = preLightData.specularFGD * preLD.rgb;

    if (HasFlag(bsdfData.materialFeatures, MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY)
        || (UseHairMarschnerB() && (!TestDebugFlagNum(DEBUG_IBL_OFF)))  )
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
                        out float3 diffuseLighting, out float3 specularLighting)
{
    AmbientOcclusionFactor aoFactor;
    GetScreenSpaceAmbientOcclusionMultibounce(posInput.positionSS, preLightData.NdotV, bsdfData.perceptualRoughness, bsdfData.ambientOcclusion, bsdfData.specularOcclusion, bsdfData.diffuseColor, bsdfData.fresnel0, aoFactor);
    ApplyAmbientOcclusionFactor(aoFactor, builtinData, lighting);

    // Apply the albedo to the direct diffuse lighting (only once). The indirect (baked)
    // diffuse lighting has already multiply the albedo in ModifyBakedDiffuseLighting().
    diffuseLighting = bsdfData.diffuseColor * lighting.direct.diffuse + builtinData.bakeDiffuseLighting + builtinData.emissiveColor;
    specularLighting = lighting.direct.specular + lighting.indirect.specularReflected;

#ifdef DEBUG_DISPLAY
    PostEvaluateBSDFDebugDisplay(aoFactor, builtinData, lighting, bsdfData.diffuseColor, diffuseLighting, specularLighting);
#endif
}

#endif // #ifdef HAS_LIGHTLOOP
