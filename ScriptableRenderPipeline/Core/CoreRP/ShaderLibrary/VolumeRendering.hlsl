#ifndef UNITY_VOLUME_RENDERING_INCLUDED
#define UNITY_VOLUME_RENDERING_INCLUDED

float OpticalDepthHomogeneous(float extinction, float intervalLength)
{
    return extinction * intervalLength;
}

float Transmittance(float opticalDepth)
{
    return exp(-opticalDepth);
}

float TransmittanceIntegralOverHomogeneousInterval(float extinction, float start, float end)
{
    return (exp(-extinction * start) - exp(-extinction * end)) / extinction;
}

float3 OpticalDepthHomogeneous(float3 extinction, float intervalLength)
{
    return extinction * intervalLength;
}

float3 Transmittance(float3 opticalDepth)
{
    return exp(-opticalDepth);
}

float3 TransmittanceIntegralOverHomogeneousInterval(float3 extinction, float start, float end)
{
    return (exp(-extinction * start) - exp(-extinction * end)) / extinction;
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

// Absorption coefficient from Disney: http://blog.selfshadow.com/publications/s2015-shading-course/burley/s2015_pbs_disney_bsdf_notes.pdf
float3 TransmittanceColorAtDistanceToAbsorption(float3 transmittanceColor, float atDistance)
{
    return -log(transmittanceColor + FLT_EPS) / max(atDistance, FLT_EPS);
}


#endif // UNITY_VOLUME_RENDERING_INCLUDED
