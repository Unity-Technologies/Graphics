#ifndef UNITY_PATH_TRACING_VOLUME_INCLUDED
#define UNITY_PATH_TRACING_VOLUME_INCLUDED

#ifdef HAS_LIGHTLOOP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"
#endif

float ComputeHeightFogMultiplier(float height)
{
    return ComputeHeightFogMultiplier(height, _HeightFogBaseHeight, _HeightFogExponents);
}

bool SampleVolumeScatteringPosition(inout float theSample, inout float t, inout float pdf, out bool sampleLocalLights)
{
    sampleLocalLights = false;

    if (!_FogEnabled || !_EnableVolumetricFog)
        return false;

    // This will determin the interval in which volumetric scattering can occur
    float tMin, tMax;
    float pdfVol = 1.0;
    float tFog = min(t, _MaxFogDistance);

#ifdef HAS_LIGHTLOOP
    float localWeight = GetLocalLightsInterval(WorldRayOrigin(), WorldRayDirection(), tMin, tMax);

    if (localWeight < 0.0)
        return false;

    sampleLocalLights = theSample < localWeight;
    if (sampleLocalLights)
    {
        tMax = min(tMax, tFog);
        if (tMin >= tMax)
            return false;

        theSample /= localWeight;
        pdfVol *= localWeight;
    }
    else
    {
        tMin = 0.0;
        tMax = tFog;

        theSample -= localWeight;
        theSample /= 1.0 - localWeight;
        pdfVol *= 1.0 - localWeight;
    }
#else
    tMin = 0.0;
    tMax = tFog;
#endif

    // FIXME: not quite sure what the sigmaT value is supposed to be...
    const float sigmaT = _HeightFogBaseExtinction;
    const float transmittanceTMax = max(exp(-tMax * sigmaT), 0.01);
    const float transmittanceThreshold = t < FLT_MAX ? 1.0 - min(0.5, transmittanceTMax) : 1.0;

    if (theSample >= transmittanceThreshold)
    {
        // Re-scale the sample
        theSample -= transmittanceThreshold;
        theSample /= 1.0 - transmittanceThreshold;

        // Adjust the pdf
        pdf *= 1.0 - transmittanceThreshold;

        return false;
    }

    // Re-scale the sample
    theSample /= transmittanceThreshold;

    // Adjust the pdf
    pdf *= pdfVol * transmittanceThreshold;

    if (sampleLocalLights)
    {
        // Linear sampling
        float deltaT = tMax - tMin;
        t = tMin + theSample * deltaT;

        // Adjust the pdf
        pdf /= deltaT;
    }
    else
    {
        // Exponential sampling
        float transmittance = transmittanceTMax + theSample * (1.0 - transmittanceTMax);
        t = -log(transmittance) / sigmaT;

        // Adjust the pdf
        pdf *= sigmaT * transmittance;
    }

    return true;
}

// Function responsible for volume scattering
void ComputeVolumeScattering(inout PathIntersection pathIntersection : SV_RayPayload, float3 inputSample, bool sampleLocalLights)
{
    // Reset the ray intersection color, which will store our final result
    pathIntersection.value = 0.0;

#ifdef HAS_LIGHTLOOP

    // Grab depth information
    uint currentDepth = _RaytracingMaxRecursion - pathIntersection.remainingDepth;

    // Check if we want to compute direct and emissive lighting for current depth
    bool computeDirect = currentDepth >= _RaytracingMinRecursion - 1;

    // Compute the scattering position
    float3 scatteringPosition = WorldRayOrigin() + pathIntersection.t * WorldRayDirection();

    // Create the list of active lights
    LightList lightList = CreateLightList(scatteringPosition, sampleLocalLights);

    // Bunch of variables common to material and light sampling
    float pdf;
    float3 value;

    RayDesc ray;
    ray.Origin = scatteringPosition;
    ray.TMin = 0.0;

    PathIntersection nextPathIntersection;

    // Light sampling
    if (computeDirect)
    {
        if (SampleLights(lightList, inputSample, scatteringPosition, 0.0, ray.Direction, value, pdf, ray.TMax))
        {
            // FIXME: Apply phase function and divide by pdf (only isotropic for now, and not sure about sigmaS value)
            value *= _HeightFogBaseScattering.xyz * ComputeHeightFogMultiplier(scatteringPosition.y) * INV_FOUR_PI / pdf;

            if (Luminance(value) > 0.001)
            {
                // Shoot a transmission ray (to mark it as such, purposedly set remaining depth to an invalid value)
                nextPathIntersection.remainingDepth = _RaytracingMaxRecursion + 1;
                ray.TMax -= _RaytracingRayBias;
                nextPathIntersection.value = 1.0;

                // FIXME: For the time being, we choose not to apply any back/front-face culling for shadows, will possibly change in the future
                TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER,
                         RAYTRACINGRENDERERFLAG_CAST_SHADOW, 0, 1, 1, ray, nextPathIntersection);

                pathIntersection.value += value * nextPathIntersection.value;
            }
        }
    }

#endif // HAS_LIGHTLOOP
}

#endif // UNITY_PATH_TRACING_VOLUME_INCLUDED
