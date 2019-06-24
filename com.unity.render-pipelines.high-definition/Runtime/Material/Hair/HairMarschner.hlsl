//-----------------------------------------------------------------------------
// Helper functions for a Marschner-esque illumination model that uses
// some modifications from the Zootopia hair paper --
// "A Practical and Controllable Hair and Fur Model for Production Path Tracing"
//-----------------------------------------------------------------------------
// Uses some separation of lobes approach derived from the implementation
// used in --
// "ISHair: Importance Sampling for Hair Scattering"
// Though this is mainly done for readability more so than any other reason
//-----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// "eta" in this case is the ratio of the IORs of the entering and exiting
// material.  For instance, if entering a hairstrand from air, eta = IOR...
// but if exiting the hair and going to air, eta = 1.0/IOR
// Technically, there is more complex relationship that involves an
// angle-dependent IOR, but that may be a bit much for our needs.  The full
// model involves a series of bounces that needs to be integrated over
// the width of the hair shaft, but this is exactly what both the Zootopia
// and ISHair tried to avoid by just directly evaluating
float HairFresnelEval(float eta, float phi)
{
    // Fresnel equation computes s-polarized and p-polarized separately
    // because we have a separate IOR for each of them.
    // http://en.wikipedia.org/wiki/Fresnel_equations 
    float perpend = 1.0;    // S-polarized
    float parallel = 1.0;   // P-polarized
    
    //============s-polarized================================
    // Select the IOR of the material that we're currently in
    // and we assume that the other one is air
    float n1 = (eta > 1.0) ? 1.0 : 1.0 / eta;
    float n2 = (eta > 1.0) ? eta : 1.0;
    
    float cosTheta = cos(phi);
    float sinTheta = sin(phi);
    float a = (n1 / n2) * sinTheta;
    a *= a;

    if (a <= 1) {
        float b = n2 * sqrt(1.0 - a);
        float c = n1 * cosTheta;
        float d = n1 * sqrt(1.0 - a);
        float e = n2 * cosTheta;
        
        perpend = (c - b) / (c + b);
        parallel = (d - e) / (d + e);
        perpend *= perpend;
        parallel *= parallel;
        perpend = min(1.0, perpend);
        parallel = min(1.0, parallel);
    }
    
    return 0.5 * (parallel + perpend);
}

float3 HairFresnelAllLobes(float IOR, float phiD)
{
    float phiT = FastASin(sin(phiD * 0.5) / IOR);   // refracted angle
    
    float AirToHair = HairFresnelEval(IOR, phiD * 0.5);
    float HairToAir = HairFresnelEval(1/IOR, phiT);
    
    // fresValues.x = R Fresnel
    // fresValues.y = TT Fresnel (two transmissions)
    // fresValues.z = TRT Fresnel (2 transmissions and 1 reflection)
    // R Fresnel has an extra cosine term which is a cheap approximation
    // of the full integral over the cylinder width.  It's not a good one,
    // but it at least works and helps prevent some noise/sparkles.
    // It's the same approach used in Sadeghi's "artist-friendly" Marschner
    // model.
    float3 fresValues;
    fresValues.x = AirToHair * cos(phiD * 0.5);
    fresValues.y = (1.0 - AirToHair) * (1.0 - HairToAir) * PI;
    fresValues.z = (1.0 - AirToHair) * HairToAir * (1.0 - HairToAir) * 2 * PI;
}

void ComputeHairRelativeAngles(float3 direction, float3 strandDir, float3 strandNorm, out float theta, out float phi)
{
    theta = FastACos(clamp(dot(direction, strandDir), -1.0, 1.0));
    float3 azimProj = direction - strandDir * dot(direction, strandDir);
    azimProj = SafeNormalize(azimProj);
    phi = FastACos(dot(azimProj, strandNorm));
}

// The Zootopia paper uses a Logistic distribution instead of a Gaussian
// so that you can sample and normalize effectively.
float unitHtLogistic(float x, float stdDev)
{
    float val = exp(-x / stdDev);
    return val / (stdDev * (1 + val) * (1 + val));
}

float unitNormLogistic(float x, float stdDev, float minX, float maxX)
{
    float boundA = 1.0 / (1.0 + exp(minX / stdDev));
    float boundB = 1.0 / (1.0 + exp(maxX / stdDev));
    return unitHtLogistic(x, stdDev) / (boundA - boundB);   
}

// For importance sampling?
float inverseLogisticCDF(float u, float stdDev)
{
    return -stdDev * log(1 / u - 1);
}

// Functions below are just for evaluating the lobes.  These still need
// to be multiplied by Fresnel terms, attenuation factors, and cosine term.

float evalMTerm(float thetaI, float thetaH, float longWidth, float longShift)
{
    float maxTH = ((thetaI + HALF_PI) * 0.5 - longShift);
    float minTH = ((thetaI - HALF_PI) * 0.5 - longShift);
    return unitNormLogistic(thetaH + longShift, longWidth, minTH, maxTH);
}

// Since the hair normal we use is always aligned with the eye vector, we already
// know what min and max phiH would be...
float evalNTermR(float phiH, float azimWidth)
{
    return unitNormLogistic(phiH, azimWidth, -HALF_PI, HALF_PI);
}

float evalNTermTT(float phiH, float azimWidth)
{
    return unitNormLogistic(phiH - HALF_PI, azimWidth, -HALF_PI, HALF_PI);
}

float evalNTermTRT(float phiH, float azimWidth)
{
    return unitNormLogistic(phiH, azimWidth, -HALF_PI, HALF_PI);
}