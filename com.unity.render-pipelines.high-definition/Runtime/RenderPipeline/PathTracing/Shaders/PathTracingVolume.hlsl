#ifndef UNITY_PATH_TRACING_VOLUME_INCLUDED
#define UNITY_PATH_TRACING_VOLUME_INCLUDED

#ifdef HAS_LIGHTLOOP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"
#endif

float ComputeHeightFogMultiplier(float height)
{
    return ComputeHeightFogMultiplier(height, _HeightFogBaseHeight, _HeightFogExponents);
}

bool SampleVolumeScatteringPosition(inout float sample, inout float t, inout float pdf, out bool sampleLocalLights)
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

    sampleLocalLights = sample < localWeight;
    if (sampleLocalLights)
    {
        tMax = min(tMax, tFog);
        if (tMin >= tMax)
            return false;

        sample /= localWeight;
        pdfVol *= localWeight;
    }
    else
    {
        tMin = 0.0;
        tMax = tFog;

        sample -= localWeight;
        sample /= 1.0 - localWeight;
        pdfVol *= 1.0 - localWeight;
    }
#else
    tMin = 0.0;
    tMax = tFog;
#endif

    // FIXME: not quite sure what the sigmaS, sigmaT values are supposed to be...
    const float sigmaS = (t == FLT_MAX) ? 1.0 : sqrt(Luminance(_HeightFogBaseScattering.xyz));
    const float sigmaT = _HeightFogBaseExtinction;
    const float transmittanceMax = exp(-tMax * sigmaT);

    const float pdfNoVolA = 1.0 - sigmaS;
    const float pdfNoVolB = sigmaS * transmittanceMax;
    const float pdfNoVol = pdfNoVolA + pdfNoVolB;
    pdfVol *= 1.0 - pdfNoVol;

    if (sample >= sigmaS)
    {
        // Re-scale the sample
        sample -= sigmaS;
        sample /= 1.0 - sigmaS;

        // Set the pdf
        pdf *= pdfNoVol;

        return false;
    }

    // Re-scale the sample
    sample /= sigmaS;

    // Evaluate the length to a potential volume scattering event
    if (-log(1.0 - sample) / sigmaT >= tMax)
    {
        // Re-scale the sample
        sample -= 1.0 - transmittanceMax;
        sample /= transmittanceMax;

        // Set the pdf
        pdf *= pdfNoVol;

        return false;
    }

    if (sampleLocalLights)
    {
        // Re-scale the sample
        sample /= 1.0 - transmittanceMax;

        // Linear sampling
        float deltaT = tMax - tMin;
        t = tMin + sample * deltaT;

        // Set the pdf
        pdf *= pdfVol / deltaT;
    }
    else
    {
        // Let's (avoid very low transmittance, for robustness sake (minor bias)
        if (transmittanceMax < 0.01)
            sample = sample * 0.99 / (1.0 - transmittanceMax);

        // Log sampling
        float transmittance = 1.0 - sample;
        t = -log(transmittance) / sigmaT;

        // Set the pdf
        pdf *= pdfVol * sigmaT * transmittance;
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
        if (SampleLights(lightList, inputSample.xyz, scatteringPosition, 0.0, ray.Direction, value, pdf, ray.TMax))
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
