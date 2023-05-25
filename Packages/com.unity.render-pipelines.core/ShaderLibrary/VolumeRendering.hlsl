#ifndef UNITY_VOLUME_RENDERING_INCLUDED
#define UNITY_VOLUME_RENDERING_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"

// Reminder:
// OpticalDepth(x, y) = Integral{x, y}{Extinction(t) dt}
// Transmittance(x, y) = Exp(-OpticalDepth(x, y))
// Transmittance(x, z) = Transmittance(x, y) * Transmittance(y, z)
// Integral{a, b}{Transmittance(0, t) dt} = Transmittance(0, a) * Integral{a, b}{Transmittance(0, t - a) dt}

float TransmittanceFromOpticalDepth(float opticalDepth)
{
    return exp(-opticalDepth);
}

float3 TransmittanceFromOpticalDepth(float3 opticalDepth)
{
    return exp(-opticalDepth);
}

float OpacityFromOpticalDepth(float opticalDepth)
{
    return 1 - TransmittanceFromOpticalDepth(opticalDepth);
}

float3 OpacityFromOpticalDepth(float3 opticalDepth)
{
    return 1 - TransmittanceFromOpticalDepth(opticalDepth);
}

float OpticalDepthFromOpacity(float opacity)
{
    return -log(1 - opacity);
}

float3 OpticalDepthFromOpacity(float3 opacity)
{
    return -log(1 - opacity);
}

//
// ---------------------------------- Deep Pixel Compositing ---------------------------------------
//

// TODO: it would be good to improve the perf and numerical stability
// of approximations below by finding a polynomial approximation.

// input = {radiance, opacity}
// Note that opacity must be less than 1 (not fully opaque).
real4 LinearizeRGBA(real4 value)
{
    // See "Deep Compositing Using Lie Algebras".
    // log(A) = {OpticalDepthFromOpacity(A.a) / A.a * A.rgb, -OpticalDepthFromOpacity(A.a)}.
    // We drop redundant negations.
    real a = value.a;
    real d = -log(1 - a);
    real r = (a >= REAL_EPS) ? (d * rcp(a)) : 1; // Prevent numerical explosion
    return real4(r * value.rgb, d);
}

// input = {radiance, optical_depth}
// Note that opacity must be less than 1 (not fully opaque).
real4 LinearizeRGBD(real4 value)
{
    // See "Deep Compositing Using Lie Algebras".
    // log(A) = {A.a / OpacityFromOpticalDepth(A.a) * A.rgb, -A.a}.
    // We drop redundant negations.
    real d = value.a;
    real a = 1 - exp(-d);
    real r = (a >= REAL_EPS) ? (d * rcp(a)) : 1; // Prevent numerical explosion
    return real4(r * value.rgb, d);
}

// output = {radiance, opacity}
// Note that opacity must be less than 1 (not fully opaque).
real4 DelinearizeRGBA(real4 value)
{
    // See "Deep Compositing Using Lie Algebras".
    // exp(B) = {OpacityFromOpticalDepth(-B.a) / -B.a * B.rgb, OpacityFromOpticalDepth(-B.a)}.
    // We drop redundant negations.
    real d = value.a;
    real a = 1 - exp(-d);
    real i = (a >= REAL_EPS) ? (a * rcp(d)) : 1; // Prevent numerical explosion
    return real4(i * value.rgb, a);
}

// input = {radiance, optical_depth}
// Note that opacity must be less than 1 (not fully opaque).
real4 DelinearizeRGBD(real4 value)
{
    // See "Deep Compositing Using Lie Algebras".
    // exp(B) = {OpacityFromOpticalDepth(-B.a) / -B.a * B.rgb, -B.a}.
    // We drop redundant negations.
    real d = value.a;
    real a = 1 - exp(-d);
    real i = (a >= REAL_EPS) ? (a * rcp(d)) : 1; // Prevent numerical explosion
    return real4(i * value.rgb, d);
}

//
// ----------------------------- Homogeneous Participating Media -----------------------------------
//

real OpticalDepthHomogeneousMedium(real extinction, real intervalLength)
{
    return extinction * intervalLength;
}

real TransmittanceHomogeneousMedium(real extinction, real intervalLength)
{
    return TransmittanceFromOpticalDepth(OpticalDepthHomogeneousMedium(extinction, intervalLength));
}

// Integral{a, b}{TransmittanceHomogeneousMedium(k, t - a) dt}.
real TransmittanceIntegralHomogeneousMedium(real extinction, real intervalLength)
{
    // Note: when multiplied by the extinction coefficient, it becomes
    // Albedo * (1 - TransmittanceFromOpticalDepth(d)) = Albedo * Opacity(d).
    return rcp(extinction) - rcp(extinction) * exp(-extinction * intervalLength);
}

//
// ----------------------------------- Height Fog --------------------------------------------------
//

// Can be used to scale base extinction and scattering coefficients.
float ComputeHeightFogMultiplier(real height, real baseHeight, real2 heightExponents)
{
    real h    = max(height - baseHeight, 0);
    float rcpH = heightExponents.x;

    return exp(-h * rcpH);
}

// Optical depth between two endpoints.
float OpticalDepthHeightFog(real baseExtinction, real baseHeight, real2 heightExponents,
                           real cosZenith, real startHeight, real intervalLength)
{
    // Height fog is composed of two slices of optical depth:
    // - homogeneous fog below 'baseHeight': d = k * t
    // - exponential fog above 'baseHeight': d = Integrate[k * e^(-(h + z * x) / H) dx, {x, 0, t}]

    float H          = heightExponents.y;
    float rcpH       = heightExponents.x;
    real Z          = cosZenith;
    real absZ       = max(abs(cosZenith), 0.001f);
    real rcpAbsZ    = rcp(absZ);

    real endHeight  = startHeight + intervalLength * Z;
    real minHeight  = min(startHeight, endHeight);
    real h          = max(minHeight - baseHeight, 0);

    real homFogDist = clamp((baseHeight - minHeight) * rcpAbsZ, 0, intervalLength);
    real expFogDist = intervalLength - homFogDist;
    float expFogMult = exp(-h * rcpH) * (1 - exp(-expFogDist * absZ * rcpH)) * (rcpAbsZ * H);

    return baseExtinction * (homFogDist + expFogMult);
}

// This version of the function assumes the interval of infinite length.
float OpticalDepthHeightFog(real baseExtinction, real baseHeight, real2 heightExponents,
                           real cosZenith, real startHeight)
{
    float H          = heightExponents.y;
    float rcpH       = heightExponents.x;
    real Z          = cosZenith;
    real absZ       = max(abs(cosZenith), REAL_EPS);
    real rcpAbsZ    = rcp(absZ);

    real minHeight  = (Z >= 0) ? startHeight : -rcp(REAL_EPS);
    real h          = max(minHeight - baseHeight, 0);

    real homFogDist = max((baseHeight - minHeight) * rcpAbsZ, 0);
    float expFogMult = exp(-h * rcpH) * (rcpAbsZ * H);

    return baseExtinction * (homFogDist + expFogMult);
}

float TransmittanceHeightFog(real baseExtinction, real baseHeight, real2 heightExponents,
                            real cosZenith, real startHeight, real intervalLength)
{
    float od = OpticalDepthHeightFog(baseExtinction, baseHeight, heightExponents,
                                    cosZenith, startHeight, intervalLength);
    return TransmittanceFromOpticalDepth(od);
}

float TransmittanceHeightFog(real baseExtinction, real baseHeight, real2 heightExponents,
                            real cosZenith, real startHeight)
{
    float od = OpticalDepthHeightFog(baseExtinction, baseHeight, heightExponents,
                                    cosZenith, startHeight);
    return TransmittanceFromOpticalDepth(od);
}

//
// ----------------------------------- Phase Functions ---------------------------------------------
//

real IsotropicPhaseFunction()
{
    return INV_FOUR_PI;
}

real RayleighPhaseFunction(real cosTheta)
{
    real k = 3 / (16 * PI);
    return k * (1 + cosTheta * cosTheta);
}

real HenyeyGreensteinPhasePartConstant(real anisotropy)
{
    real g = anisotropy;

    return INV_FOUR_PI * (1 - g * g);
}

real HenyeyGreensteinPhasePartVarying(real anisotropy, real cosTheta)
{
    real g = anisotropy;
    real x = 1 + g * g - 2 * g * cosTheta;
    real f = rsqrt(max(x, REAL_EPS)); // x^(-1/2)

    return f * f * f; // x^(-3/2)
}

real HenyeyGreensteinPhaseFunction(real anisotropy, real cosTheta)
{
    return HenyeyGreensteinPhasePartConstant(anisotropy) *
           HenyeyGreensteinPhasePartVarying(anisotropy, cosTheta);
}

// "Physically Based Rendering, 15.2.3 Sampling Phase Functions"
bool SampleHenyeyGreenstein(real3 incomingDir,
                            real anisotropy,
                            real3 inputSample,
                            out real3 outgoingDir,
                            out real pdf)
{
    real g = anisotropy;

    // Compute costheta
    real cosTheta;
    if (abs(g) < 0.001)
    {
        cosTheta = 1.0 - 2.0 * inputSample.x;
    }
    else
    {
        real sqrTerm = (1.0 - g * g) / (1.0 - g + 2.0 * g * inputSample.x);
        cosTheta = (1.0 + g * g - sqrTerm * sqrTerm) / (2 * g);
    }

    // Compute direction
    real sinTheta = sqrt(max(0.0, 1.0 - cosTheta * cosTheta));
    real phi = 2.0 * PI * inputSample.y;

    real3 wc = normalize(incomingDir);
    real3x3 coordsys = GetLocalFrame(wc);

    real sinPhi, cosPhi;
    sincos(phi, sinPhi, cosPhi);
    outgoingDir = sinTheta * cosPhi * coordsys[0] +
                  sinTheta * sinPhi * coordsys[1] +
                  cosTheta *  coordsys[2];
    pdf = HenyeyGreensteinPhaseFunction(g, cosTheta);

    return any(pdf);
}

real CornetteShanksPhasePartConstant(real anisotropy)
{
    real g = anisotropy;

    return (3 / (8 * PI)) * (1 - g * g) / (2 + g * g);
}

// Similar to the RayleighPhaseFunction.
real CornetteShanksPhasePartSymmetrical(real cosTheta)
{
    real h = 1 + cosTheta * cosTheta;
    return h;
}

real CornetteShanksPhasePartAsymmetrical(real anisotropy, real cosTheta)
{
    real g = anisotropy;
    real x = 1 + g * g - 2 * g * cosTheta;
    real f = rsqrt(max(x, REAL_EPS)); // x^(-1/2)
    return f * f * f;                 // x^(-3/2)
}

real CornetteShanksPhasePartVarying(real anisotropy, real cosTheta)
{
    return CornetteShanksPhasePartSymmetrical(cosTheta) *
           CornetteShanksPhasePartAsymmetrical(anisotropy, cosTheta); // h * x^(-3/2)
}

// A better approximation of the Mie phase function.
// Ref: Henyey-Greenstein and Mie phase functions in Monte Carlo radiative transfer computations
real CornetteShanksPhaseFunction(real anisotropy, real cosTheta)
{
    return CornetteShanksPhasePartConstant(anisotropy) *
           CornetteShanksPhasePartVarying(anisotropy, cosTheta);
}

//
// --------------------------------- Importance Sampling -------------------------------------------
//

// Samples the interval of homogeneous participating medium using the closed-form tracking approach
// (proportionally to the transmittance).
// Returns the offset from the start of the interval and the weight = (transmittance / pdf).
// Ref: Monte Carlo Methods for Volumetric Light Transport Simulation, p. 5.
void ImportanceSampleHomogeneousMedium(real rndVal, real extinction, real intervalLength,
                                       out real offset, out real weight)
{
    // pdf    = extinction * exp(extinction * (intervalLength - t)) / (exp(intervalLength * extinction) - 1)
    // pdf    = extinction * exp(-extinction * t) / (1 - exp(-extinction * intervalLength))
    // weight = TransmittanceFromOpticalDepth(t) / pdf
    // weight = exp(-extinction * t) / pdf
    // weight = (1 - exp(-extinction * intervalLength)) / extinction
    // weight = OpacityFromOpticalDepth(extinction * intervalLength) / extinction

    real x = 1 - exp(-extinction * intervalLength);
    real c = rcp(extinction);

    // TODO: return 'rcpPdf' to support imperfect importance sampling...
    weight = x * c;
    offset = -log(1 - rndVal * x) * c;
}

void ImportanceSampleExponentialMedium(real rndVal, real extinction, real falloff,
                                       out real offset, out real rcpPdf)
{

    // Extinction[t] = Extinction[0] * exp(-falloff * t).
    real c = extinction;
    real a = falloff;

    // TODO: optimize...
    offset = -log(1 - a / c * log(rndVal)) / a;
    rcpPdf = rcp(c * exp(-a * offset) * exp(-c / a * (1 - exp(-a * offset))));
}

// Implements equiangular light sampling.
// Returns the distance from the origin of the ray, the squared distance from the light,
// and the reciprocal of the PDF.
// Ref: Importance Sampling of Area Lights in Participating Medium.
void ImportanceSamplePunctualLight(real rndVal, real3 lightPosition, real lightSqRadius,
                                   real3 rayOrigin, real3 rayDirection,
                                   real tMin, real tMax,
                                   out real t, out real sqDist, out real rcpPdf)
{
    real3 originToLight         = lightPosition - rayOrigin;
    real  originToLightProjDist = dot(originToLight, rayDirection);
    real  originToLightSqDist   = dot(originToLight, originToLight);
    real  rayToLightSqDist      = originToLightSqDist - originToLightProjDist * originToLightProjDist;

    // Virtually offset the light to modify the PDF distribution.
    real sqD  = max(rayToLightSqDist + lightSqRadius, REAL_EPS);
    real rcpD = rsqrt(sqD);
    real d    = sqD * rcpD;
    real a    = tMin - originToLightProjDist;
    real b    = tMax - originToLightProjDist;
    real x    = a * rcpD;
    real y    = b * rcpD;

#if 0
    real theta0   = FastATan(x);
    real theta1   = FastATan(y);
    real gamma    = theta1 - theta0;
    real tanTheta = tan(theta0 + rndVal * gamma);
#else
    // Same but faster:
    // atan(y) - atan(x) = atan((y - x) / (1 + x * y))
    // tan(atan(x) + z)  = (x * cos(z) + sin(z)) / (cos(z) - x * sin(z))
    // Both the tangent and the angle  cannot be negative.
    real tanGamma = abs((y - x) * rcp(max(0, 1 + x * y)));
    real gamma    = FastATanPos(tanGamma);
    real z        = rndVal * gamma;
    real numer    = x * cos(z) + sin(z);
    real denom    = cos(z) - x * sin(z);
    real tanTheta = numer * rcp(denom);
#endif

    real tRelative = d * tanTheta;

    sqDist = sqD + tRelative * tRelative;
    rcpPdf = gamma * rcpD * sqDist;
    t      = originToLightProjDist + tRelative;

    // Remove the virtual light offset to obtain the real geometric distance.
    sqDist = max(sqDist - lightSqRadius, REAL_EPS);
}

// Returns the cosine.
// Weight = Phase / Pdf = 1.
real ImportanceSampleRayleighPhase(real rndVal)
{
    // real a = sqrt(16 * (rndVal - 1) * rndVal + 5);
    // real b = -4 * rndVal + a + 2;
    // real c = PositivePow(b, 0.33333333);
    // return rcp(c) - c;

    // Approximate...
    return lerp(cos(PI * rndVal + PI), 2 * rndVal - 1, 0.5);
}

//
// ------------------------------------ Miscellaneous ----------------------------------------------
//

// Absorption coefficient from Disney: http://blog.selfshadow.com/publications/s2015-shading-course/burley/s2015_pbs_disney_bsdf_notes.pdf
real3 TransmittanceColorAtDistanceToAbsorption(real3 transmittanceColor, real atDistance)
{
    return -log(transmittanceColor + REAL_EPS) / max(atDistance, REAL_EPS);
}

float ApplyExponentialFadeFactor(float fade, bool exponential, bool multiplyBlendMode)
{
    if (exponential)
    {
        if (multiplyBlendMode)
            fade = 1 - PositivePow(abs(fade - 1), 2.2);
        else
            fade = PositivePow(fade, 2.2);
    }
    return fade;
}

float ComputeVolumeFadeFactor(float3 coordNDC, float dist,
                        float3 rcpPosFaceFade, float3 rcpNegFaceFade, bool invertFade,
                        float rcpDistFadeLen, float endTimesRcpDistFadeLen,
                        bool exponentialFalloff, bool multiplyBlendMode)
{
    float3 posF = Remap10(coordNDC, rcpPosFaceFade, rcpPosFaceFade);
    float3 negF = Remap01(coordNDC, rcpNegFaceFade, 0);
    float  dstF = Remap10(dist, rcpDistFadeLen, endTimesRcpDistFadeLen);
    float  fade = posF.x * posF.y * posF.z * negF.x * negF.y * negF.z;

    // We only apply exponential falloff on the Blend Distance and not Distance Fade
    fade = ApplyExponentialFadeFactor(fade, exponentialFalloff, multiplyBlendMode);

    fade = dstF * (invertFade ? (1 - fade) : fade);

    return fade;
}

float ExtinctionFromMeanFreePath(float meanFreePath)
{
    // Keep in sync with kMinFogDistance
    return rcp(max(0.05, meanFreePath));
}

#endif // UNITY_VOLUME_RENDERING_INCLUDED
