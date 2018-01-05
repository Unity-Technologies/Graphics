#ifndef UNITY_VOLUME_RENDERING_INCLUDED
#define UNITY_VOLUME_RENDERING_INCLUDED

// Reminder:
// Optical_Depth(x, y) = Integral{x, y}{Extinction(t) dt}
// Transmittance(x, y) = Exp(-Optical_Depth(x, y))
// Transmittance(x, z) = Transmittance(x, y) * Transmittance(y, z)
// Integral{a, b}{Transmittance(0, t) * L_s(t) dt} = Transmittance(0, a) * Integral{a, b}{Transmittance(0, t - a) * L_s(t) dt}.

float OpticalDepthHomogeneousMedium(float extinction, float intervalLength)
{
    return extinction * intervalLength;
}

float3 OpticalDepthHomogeneousMedium(float3 extinction, float intervalLength)
{
    return extinction * intervalLength;
}

float Transmittance(float opticalDepth)
{
    return exp(-opticalDepth);
}

float3 Transmittance(float3 opticalDepth)
{
    return exp(-opticalDepth);
}

float TransmittanceHomogeneousMedium(float extinction, float intervalLength)
{
    return Transmittance(OpticalDepthHomogeneousMedium(extinction, intervalLength));
}

float3 TransmittanceHomogeneousMedium(float3 extinction, float intervalLength)
{
    return Transmittance(OpticalDepthHomogeneousMedium(extinction, intervalLength));
}

// Integral{a, b}{Transmittance(0, t - a) dt}.
float TransmittanceIntegralHomogeneousMedium(float extinction, float intervalLength)
{
    return rcp(extinction) - rcp(extinction) * exp(-extinction * intervalLength);
}

// Integral{a, b}{Transmittance(0, t - a) dt}.
float3 TransmittanceIntegralHomogeneousMedium(float3 extinction, float intervalLength)
{
    return rcp(extinction) - rcp(extinction) * exp(-extinction * intervalLength);
}

float IsotropicPhaseFunction()
{
    return INV_FOUR_PI;
}

float HenyeyGreensteinPhasePartConstant(float asymmetry)
{
    float g = asymmetry;

    return INV_FOUR_PI * (1 - g * g);
}

float HenyeyGreensteinPhasePartVarying(float asymmetry, float LdotD)
{
    float g = asymmetry;

    return pow(abs(1 + g * g - 2 * g * LdotD), -1.5);
}

float HenyeyGreensteinPhaseFunction(float asymmetry, float LdotD)
{
    return HenyeyGreensteinPhasePartConstant(asymmetry) *
           HenyeyGreensteinPhasePartVarying(asymmetry, LdotD);
}

// Samples the interval of homogeneous participating medium using the closed-form tracking approach
// (proportionally to the transmittance).
// Returns the offset from the start of the interval and the weight = (transmittance / pdf).
// Ref: Production Volume Rendering, 3.6.1.
void ImportanceSampleHomogeneousMedium(float rndVal, float extinction, float intervalLength,
                                      out float offset, out float weight)
{
    // pdf    = extinction * exp(-extinction * t) / (1 - exp(-intervalLength * extinction))
    // weight = exp(-extinction * t) / pdf
    // weight = (1 - exp(-extinction * intervalLength)) / extinction;

    float x = 1 - exp(-extinction * intervalLength);

    weight = x * rcp(extinction);
    offset = -log(1 - rndVal * x) * rcp(extinction);
}

// Implements equiangular light sampling.
// Returns the distance from the origin of the ray, the squared (radial) distance from the light,
// and the reciprocal of the PDF.
// Ref: Importance Sampling of Area Lights in Participating Medium.
void ImportanceSamplePunctualLight(float rndVal, float3 lightPosition,
                                   float3 rayOrigin, float3 rayDirection,
                                   float tMin, float tMax,
                                   out float dist, out float rSq, out float rcpPdf,
                                   float minDistSq = FLT_EPS)
{
    float3 originToLight       = lightPosition - rayOrigin;
    float  originToLightProj   = dot(originToLight, rayDirection);
    float  originToLightDistSq = dot(originToLight, originToLight);
    float  rayToLightDistSq    = max(originToLightDistSq - originToLightProj * originToLightProj, minDistSq);

    float a    = tMin - originToLightProj;
    float b    = tMax - originToLightProj;
    float dSq  = rayToLightDistSq;
    float dRcp = rsqrt(dSq);
    float d    = dSq * dRcp;

    // TODO: optimize me. :-(
    float theta0 = FastATan(a * dRcp);
    float theta1 = FastATan(b * dRcp);
    float gamma  = theta1 - theta0;
    float theta  = lerp(theta0, theta1, rndVal);
    float t      = d * tan(theta);

    dist   = originToLightProj + t;
    rSq    = dSq + t * t;
    rcpPdf = gamma * rSq * dRcp;
}

// Absorption coefficient from Disney: http://blog.selfshadow.com/publications/s2015-shading-course/burley/s2015_pbs_disney_bsdf_notes.pdf
float3 TransmittanceColorAtDistanceToAbsorption(float3 transmittanceColor, float atDistance)
{
    return -log(transmittanceColor + FLT_EPS) / max(atDistance, FLT_EPS);
}

#endif // UNITY_VOLUME_RENDERING_INCLUDED
