#ifndef UNITY_VOLUME_RENDERING_INCLUDED
#define UNITY_VOLUME_RENDERING_INCLUDED

float3 OpticalDepthHomogeneous(float3 extinction, float intervalLength)
{
    return extinction * intervalLength;
}

float3 Transmittance(float3 opticalDepth)
{
    return exp(-opticalDepth);
}

float IsotropicPhaseFunction()
{
    return INV_FOUR_PI;
}

float HenyeyGreensteinPhaseFunction(float asymmetry, float LdotD)
{
    float g = asymmetry;

    // Note: the factor before pow() is a constant, and could therefore be factored out.
    return (INV_FOUR_PI * (1 - g * g)) * pow(abs(1 + g * g - 2 * g * LdotD), -1.5);
}

#endif // UNITY_VOLUME_RENDERING_INCLUDED
