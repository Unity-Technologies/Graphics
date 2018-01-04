#ifndef UNITY_VOLUME_RENDERING_INCLUDED
#define UNITY_VOLUME_RENDERING_INCLUDED

real OpticalDepthHomogeneous(real extinction, real intervalLength)
{
    return extinction * intervalLength;
}

real Transmittance(real opticalDepth)
{
    return exp(-opticalDepth);
}

real TransmittanceIntegralOverHomogeneousInterval(real extinction, real start, real end)
{
    return (exp(-extinction * start) - exp(-extinction * end)) / extinction;
}

real3 OpticalDepthHomogeneous(real3 extinction, real intervalLength)
{
    return extinction * intervalLength;
}

real3 Transmittance(real3 opticalDepth)
{
    return exp(-opticalDepth);
}

real3 TransmittanceIntegralOverHomogeneousInterval(real3 extinction, real start, real end)
{
    return (exp(-extinction * start) - exp(-extinction * end)) / extinction;
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

real HenyeyGreensteinPhasePartVarying(real asymmetry, real LdotD)
{
    real g = asymmetry;

    return pow(abs(1 + g * g - 2 * g * LdotD), -1.5);
}

real HenyeyGreensteinPhaseFunction(real asymmetry, real LdotD)
{
    return HenyeyGreensteinPhasePartConstant(asymmetry) *
           HenyeyGreensteinPhasePartVarying(asymmetry, LdotD);
}

// Absorption coefficient from Disney: http://blog.selfshadow.com/publications/s2015-shading-course/burley/s2015_pbs_disney_bsdf_notes.pdf
real3 TransmittanceColorAtDistanceToAbsorption(real3 transmittanceColor, real atDistance)
{
    return -log(transmittanceColor + FLT_EPS) / max(atDistance, FLT_EPS);
}


#endif // UNITY_VOLUME_RENDERING_INCLUDED
