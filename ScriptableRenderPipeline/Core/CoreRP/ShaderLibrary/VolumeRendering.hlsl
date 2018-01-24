#ifndef UNITY_VOLUME_RENDERING_INCLUDED
#define UNITY_VOLUME_RENDERING_INCLUDED

// Reminder:
// Optical_Depth(x, y) = Integral{x, y}{Extinction(t) dt}
// Transmittance(x, y) = Exp(-Optical_Depth(x, y))
// Transmittance(x, z) = Transmittance(x, y) * Transmittance(y, z)
// Integral{a, b}{Transmittance(0, t) * L_s(t) dt} = Transmittance(0, a) * Integral{a, b}{Transmittance(0, t - a) * L_s(t) dt}.

real OpticalDepthHomogeneousMedium(real extinction, real intervalLength)
{
    return extinction * intervalLength;
}

real3 OpticalDepthHomogeneousMedium(real3 extinction, real intervalLength)
{
    return extinction * intervalLength;
}

real Transmittance(real opticalDepth)
{
    return exp(-opticalDepth);
}

real3 Transmittance(real3 opticalDepth)
{
    return exp(-opticalDepth);
}

real TransmittanceHomogeneousMedium(real extinction, real intervalLength)
{
    return Transmittance(OpticalDepthHomogeneousMedium(extinction, intervalLength));
}

real3 TransmittanceHomogeneousMedium(real3 extinction, real intervalLength)
{
    return Transmittance(OpticalDepthHomogeneousMedium(extinction, intervalLength));
}

// Integral{a, b}{Transmittance(0, t - a) dt}.
real TransmittanceIntegralHomogeneousMedium(real extinction, real intervalLength)
{
    return rcp(extinction) - rcp(extinction) * exp(-extinction * intervalLength);
}

// Integral{a, b}{Transmittance(0, t - a) dt}.
real3 TransmittanceIntegralHomogeneousMedium(real3 extinction, real intervalLength)
{
    return rcp(extinction) - rcp(extinction) * exp(-extinction * intervalLength);
}

real IsotropicPhaseFunction()
{
    return INV_FOUR_PI;
}

real HenyeyGreensteinPhasePartConstant(real asymmetry)
{
    real g = asymmetry;

    return INV_FOUR_PI * (1 - g * g);
}

real HenyeyGreensteinPhasePartVarying(real asymmetry, real cosTheta)
{
    real g = asymmetry;

    return pow(abs(1 + g * g - 2 * g * cosTheta), -1.5);
}

real HenyeyGreensteinPhaseFunction(real asymmetry, real cosTheta)
{
    return HenyeyGreensteinPhasePartConstant(asymmetry) *
           HenyeyGreensteinPhasePartVarying(asymmetry, cosTheta);
}

real CornetteShanksPhasePartConstant(real asymmetry)
{
    real g = asymmetry;

    return INV_FOUR_PI * 1.5 * (1 - g * g) / (2 + g * g);
}

real CornetteShanksPhasePartVarying(real asymmetry, real cosTheta)
{
    real g = asymmetry;

    return (1 + cosTheta * cosTheta) * pow(abs(1 + g * g - 2 * g * cosTheta), -1.5);
}

// A better approximation of the Mie phase function.
// Ref: Henyey–Greenstein and Mie phase functions in Monte Carlo radiative transfer computations
real CornetteShanksPhaseFunction(real asymmetry, real cosTheta)
{
    return CornetteShanksPhasePartConstant(asymmetry) *
           CornetteShanksPhasePartVarying(asymmetry, cosTheta);
}

// Samples the interval of homogeneous participating medium using the closed-form tracking approach
// (proportionally to the transmittance).
// Returns the offset from the start of the interval and the weight = (transmittance / pdf).
// Ref: Production Volume Rendering, 3.6.1.
void ImportanceSampleHomogeneousMedium(real rndVal, real extinction, real intervalLength,
                                      out real offset, out real weight)
{
    // pdf    = extinction * exp(-extinction * t) / (1 - exp(-intervalLength * extinction))
    // weight = exp(-extinction * t) / pdf
    // weight = (1 - exp(-extinction * intervalLength)) / extinction;

    real x = 1 - exp(-extinction * intervalLength);

    // Avoid division by 0.
    real rcpExt = extinction != 0 ? rcp(extinction) : 0;

    weight = x * rcpExt;
    offset = -log(1 - rndVal * x) * rcpExt;
}

// Implements equiangular light sampling.
// Returns the distance from the origin of the ray, the squared (radial) distance from the light,
// and the reciprocal of the PDF.
// Ref: Importance Sampling of Area Lights in Participating Medium.
void ImportanceSamplePunctualLight(real rndVal, real3 lightPosition,
                                   real3 rayOrigin, real3 rayDirection,
                                   real tMin, real tMax,
                                   out real dist, out real rSq, out real rcpPdf,
                                   real minDistSq = FLT_EPS)
{
    real3 originToLight       = lightPosition - rayOrigin;
    real  originToLightProj   = dot(originToLight, rayDirection);
    real  originToLightDistSq = dot(originToLight, originToLight);
    real  rayToLightDistSq    = max(originToLightDistSq - originToLightProj * originToLightProj, minDistSq);

    real a    = tMin - originToLightProj;
    real b    = tMax - originToLightProj;
    real dSq  = rayToLightDistSq;
    real dRcp = rsqrt(dSq);
    real d    = dSq * dRcp;

    // TODO: optimize me. :-(
    real theta0 = FastATan(a * dRcp);
    real theta1 = FastATan(b * dRcp);
    real gamma  = theta1 - theta0;
    real theta  = lerp(theta0, theta1, rndVal);
    real t      = d * tan(theta);

    dist   = originToLightProj + t;
    rSq    = dSq + t * t;
    rcpPdf = gamma * rSq * dRcp;
}

// Absorption coefficient from Disney: http://blog.selfshadow.com/publications/s2015-shading-course/burley/s2015_pbs_disney_bsdf_notes.pdf
real3 TransmittanceColorAtDistanceToAbsorption(real3 transmittanceColor, real atDistance)
{
    return -log(transmittanceColor + FLT_EPS) / max(atDistance, FLT_EPS);
}

#endif // UNITY_VOLUME_RENDERING_INCLUDED
