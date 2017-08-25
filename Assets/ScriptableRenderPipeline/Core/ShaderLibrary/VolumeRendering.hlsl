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

#endif // UNITY_VOLUME_RENDERING_INCLUDED
