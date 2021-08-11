#ifndef PROBE_PROPAGATION_SPHERICAL_GAUSSIANS
#define PROBE_PROPAGATION_SPHERICAL_GAUSSIANS

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/ProbePropagation.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/ProbeVolume/DynamicGI/SphericalGaussians.hlsl"

float3 EvaluateSG(uint probeIndex, float3 normal, float sharpness, in float4 _RayAxis[NEIGHBOR_AXIS_COUNT])
{
    float3 color = 0;
    float weight = 0.0f;
    for(int i=0; i < NEIGHBOR_AXIS_COUNT; ++i)
    {
        float3 prevAxisRadiance = ReadPreviousPropagationAxis(probeIndex, i);
        float3 axis = _RayAxis[i].xyz;

        SphericalGaussian sg;
        sg.amplitude = 1;
        sg.sharpness = sharpness;
        sg.mean = axis;

        float sgWeight = SGEvaluateFromDirection(sg, normal);

        weight += sgWeight;
        color += prevAxisRadiance * sgWeight;
    }

    color *= 1.0 / weight;
    return color;
}


float3 EvaluateBRDFLambertApproximate(uint probeIndex, float3 surfaceNormal, float sharpness, in float4 _RayAxis[NEIGHBOR_AXIS_COUNT])
{
    float3 color = 0;
    float solidAngleBasis = 0.0;
    for(int i=0; i < NEIGHBOR_AXIS_COUNT; ++i)
    {
        float3 prevAxisRadiance = ReadPreviousPropagationAxis(probeIndex, i);
        float3 axis = _RayAxis[i].xyz;

        SphericalGaussian sg;
        sg.amplitude = 1;
        sg.sharpness = sharpness;
        sg.mean = axis;

        float sgWeight = SGIrradianceFitted(sg, surfaceNormal);
        color += prevAxisRadiance * sgWeight;
        solidAngleBasis += SGIntegral(sg);
    }

    // TODO: Precompute global normalization term for given SG sharpness offline, rather than per pixel.
    const float SOLID_ANGLE_SPHERE = 4.0 * PI;
    float normalization = SOLID_ANGLE_SPHERE / solidAngleBasis;
    return color * normalization;
}


#endif // endof PROBE_PROPAGATION_SPHERICAL_GAUSSIANS
