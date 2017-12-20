#ifndef UNITY_VOLUME_RENDERING_INCLUDED
#define UNITY_VOLUME_RENDERING_INCLUDED

REAL OpticalDepthHomogeneous(REAL extinction, REAL intervalLength)
{
    return extinction * intervalLength;
}

REAL Transmittance(REAL opticalDepth)
{
    return exp(-opticalDepth);
}

REAL TransmittanceIntegralOverHomogeneousInterval(REAL extinction, REAL start, REAL end)
{
    return (exp(-extinction * start) - exp(-extinction * end)) / extinction;
}

REAL3 OpticalDepthHomogeneous(REAL3 extinction, REAL intervalLength)
{
    return extinction * intervalLength;
}

REAL3 Transmittance(REAL3 opticalDepth)
{
    return exp(-opticalDepth);
}

REAL3 TransmittanceIntegralOverHomogeneousInterval(REAL3 extinction, REAL start, REAL end)
{
    return (exp(-extinction * start) - exp(-extinction * end)) / extinction;
}

REAL IsotropicPhaseFunction()
{
    return INV_FOUR_PI;
}

REAL HenyeyGreensteinPhasePartConstant(REAL asymmetry)
{
    REAL g = asymmetry;

    return INV_FOUR_PI * (1 - g * g);
}

REAL HenyeyGreensteinPhasePartVarying(REAL asymmetry, REAL LdotD)
{
    REAL g = asymmetry;

    return pow(abs(1 + g * g - 2 * g * LdotD), -1.5);
}

REAL HenyeyGreensteinPhaseFunction(REAL asymmetry, REAL LdotD)
{
    return HenyeyGreensteinPhasePartConstant(asymmetry) *
           HenyeyGreensteinPhasePartVarying(asymmetry, LdotD);
}

// Absorption coefficient from Disney: http://blog.selfshadow.com/publications/s2015-shading-course/burley/s2015_pbs_disney_bsdf_notes.pdf
REAL3 TransmittanceColorAtDistanceToAbsorption(REAL3 transmittanceColor, REAL atDistance)
{
    return -log(transmittanceColor + FLT_EPS) / max(atDistance, FLT_EPS);
}


#endif // UNITY_VOLUME_RENDERING_INCLUDED
