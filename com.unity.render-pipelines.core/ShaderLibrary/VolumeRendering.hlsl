#ifndef UNITY_VOLUME_RENDERING_INCLUDED
#define UNITY_VOLUME_RENDERING_INCLUDED

// Reminder:
// OpticalDepth(x, y) = Integral{x, y}{Extinction(t) dt}
// Transmittance(x, y) = Exp(-OpticalDepth(x, y))
// Transmittance(x, z) = Transmittance(x, y) * Transmittance(y, z)
// Integral{a, b}{Transmittance(0, t) dt} = Transmittance(0, a) * Integral{a, b}{Transmittance(0, t - a) dt}

real TransmittanceFromOpticalDepth(real opticalDepth)
{
    return exp(-opticalDepth);
}

real OpacityFromOpticalDepth(real opticalDepth)
{
    return 1 - TransmittanceFromOpticalDepth(opticalDepth);
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
    real r = (a >= FLT_EPS) ? (d * rcp(a)) : 1; // Prevent numerical explosion
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
    real r = (a >= FLT_EPS) ? (d * rcp(a)) : 1; // Prevent numerical explosion
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
    real i = (a >= FLT_EPS) ? (a * rcp(d)) : 1; // Prevent numerical explosion
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
    real i = (a >= FLT_EPS) ? (a * rcp(d)) : 1; // Prevent numerical explosion
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

// Integral{a, b}{TransmittanceFromOpticalDepth(0, t - a) dt}.
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
real ComputeHeightFogMultiplier(real height, real baseHeight, real2 heightExponents)
{
    real h    = max(height - baseHeight, 0);
    real rcpH = heightExponents.x;

    return exp(-h * rcpH);
}

// Optical depth between two endpoints.
real OpticalDepthHeightFog(real baseExtinction, real baseHeight, real2 heightExponents,
                           real cosZenith, real startHeight, real intervalLength)
{
    // Height fog is composed of two slices of optical depth:
    // - homogeneous fog below 'baseHeight': d = k * t
    // - exponential fog above 'baseHeight': d = Integrate[k * e^(-(h + z * x) / H) dx, {x, 0, t}]

    real H          = heightExponents.y;
    real rcpH       = heightExponents.x;
    real Z          = cosZenith;
    real absZ       = max(abs(cosZenith), FLT_EPS);
    real rcpAbsZ    = rcp(absZ);

    real endHeight  = startHeight + intervalLength * Z;
    real minHeight  = min(startHeight, endHeight);
    real h          = max(minHeight - baseHeight, 0);

    real homFogDist = clamp((baseHeight - minHeight) * rcpAbsZ, 0, intervalLength);
    real expFogDist = intervalLength - homFogDist;
    real expFogMult = exp(-h * rcpH) * (1 - exp(-expFogDist * absZ * rcpH)) * (rcpAbsZ * H);

    return baseExtinction * (homFogDist + expFogMult);
}

// This version of the function assumes the interval of infinite length.
real OpticalDepthHeightFog(real baseExtinction, real baseHeight, real2 heightExponents,
                           real cosZenith, real startHeight)
{
    real H          = heightExponents.y;
    real rcpH       = heightExponents.x;
    real Z          = cosZenith;
    real absZ       = max(abs(cosZenith), FLT_EPS);
    real rcpAbsZ    = rcp(absZ);

    real minHeight  = (Z >= 0) ? startHeight : -rcp(FLT_EPS);
    real h          = max(minHeight - baseHeight, 0);

    real homFogDist = max((baseHeight - minHeight) * rcpAbsZ, 0);
    real expFogMult = exp(-h * rcpH) * (rcpAbsZ * H);

    return baseExtinction * (homFogDist + expFogMult);
}

real TransmittanceHeightFog(real baseExtinction, real baseHeight, real2 heightExponents,
                            real cosZenith, real startHeight, real intervalLength)
{
    real od = OpticalDepthHeightFog(baseExtinction, baseHeight, heightExponents,
                                    cosZenith, startHeight, intervalLength);
    return TransmittanceFromOpticalDepth(od);
}

real TransmittanceHeightFog(real baseExtinction, real baseHeight, real2 heightExponents,
                            real cosZenith, real startHeight)
{
    real od = OpticalDepthHeightFog(baseExtinction, baseHeight, heightExponents,
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

real HenyeyGreensteinPhasePartConstant(real anisotropy)
{
    real g = anisotropy;

    return INV_FOUR_PI * (1 - g * g);
}

real HenyeyGreensteinPhasePartVarying(real anisotropy, real cosTheta)
{
    real g = anisotropy;
    real f = rsqrt(saturate(1 + g * g - 2 * g * cosTheta)); // x^(-1/2)

    return f * f * f; // x^(-3/2)
}

real HenyeyGreensteinPhaseFunction(real anisotropy, real cosTheta)
{
    return HenyeyGreensteinPhasePartConstant(anisotropy) *
           HenyeyGreensteinPhasePartVarying(anisotropy, cosTheta);
}

real CornetteShanksPhasePartConstant(real anisotropy)
{
    real g = anisotropy;

    return INV_FOUR_PI * 1.5 * (1 - g * g) / (2 + g * g);
}

real CornetteShanksPhasePartVarying(real anisotropy, real cosTheta)
{
    real g = anisotropy;
    real f = rsqrt(saturate(1 + g * g - 2 * g * cosTheta)); // x^(-1/2)
    real h = (1 + cosTheta * cosTheta);

    // Note that this function is not perfectly isotropic for (g = 0). We force it to be.
    // TODO: in the future, when (g = 0), specialize the Volumetric Lighting kernel
    // to not do anything anisotropy-specific. This way we could avoid this test
    // (along with tons of other overhead and hacks).
    return (g == 0) ? 1.33333333 : h * (f * f * f); // h * x^(-3/2)
}

// A better approximation of the Mie phase function.
// Ref: Henyey–Greenstein and Mie phase functions in Monte Carlo radiative transfer computations
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

    weight = x * c;
    offset = -log(1 - rndVal * x) * c;
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
    real sqD  = max(rayToLightSqDist + lightSqRadius, FLT_EPS);
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
    sqDist = max(sqDist - lightSqRadius, FLT_EPS);
}

//
// ------------------------------------ Miscellaneous ----------------------------------------------
//

// Absorption coefficient from Disney: http://blog.selfshadow.com/publications/s2015-shading-course/burley/s2015_pbs_disney_bsdf_notes.pdf
real3 TransmittanceColorAtDistanceToAbsorption(real3 transmittanceColor, real atDistance)
{
    return -log(transmittanceColor + FLT_EPS) / max(atDistance, FLT_EPS);
}

#endif // UNITY_VOLUME_RENDERING_INCLUDED
