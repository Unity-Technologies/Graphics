#ifndef UNITY_PATH_TRACING_VOLUME_INCLUDED
#define UNITY_PATH_TRACING_VOLUME_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"

float ComputeHeightFogMultiplier(float height)
{
    return ComputeHeightFogMultiplier(height, _HeightFogBaseHeight, _HeightFogExponents);
}

bool SampleVolumeScatteringPosition(uint2 pixelCoord, inout float inputSample, inout float t, inout float pdf, out bool sampleLocalLights, out float3 lightPosition)
{
    sampleLocalLights = false;

    if (!_FogEnabled || !_EnableVolumetricFog)
        return false;

    // This will determine the interval in which volumetric scattering can occur
    float tMin, tMax;
    float pdfVol = 1.0;
    float tFog = min(t, _MaxFogDistance);

    if (_FogDirectionalOnly)
    {
        if (!_DirectionalLightCount)
            return false;

        tMin = 0.0;
        tMax = tFog;
    }
    else // Directional and local lights
    {
        float pickedLightWeight;
        float localWeight = PickLocalLightInterval(WorldRayOrigin(), WorldRayDirection(), inputSample, lightPosition, pickedLightWeight, tMin, tMax);

        if (localWeight < 0.0)
            return false;

        sampleLocalLights = inputSample < localWeight;
        if (sampleLocalLights)
        {
            tMax = min(tMax, tFog);
            if (tMin >= tMax)
                return false;

            inputSample = RescaleSampleUnder(inputSample, localWeight);
            pdfVol *= localWeight * pickedLightWeight;
        }
        else
        {
            tMin = 0.0;
            tMax = tFog;

            inputSample = RescaleSampleOver(inputSample, localWeight);
            pdfVol *= 1.0 - localWeight;
        }
    }

    // FIXME: not quite sure what the sigmaT value is supposed to be...
    const float sigmaT = _HeightFogBaseExtinction;
    const float transmittanceTMin = max(exp(-tMin * sigmaT), 0.01);
    const float transmittanceTMax = max(exp(-tMax * sigmaT), 0.01);
    const float transmittanceThreshold = t < FLT_MAX ? 1.0 - min(0.5, transmittanceTMax) : 1.0;

    if (inputSample >= transmittanceThreshold)
    {
        inputSample = RescaleSampleOver(inputSample, transmittanceThreshold);
        pdf *= 1.0 - transmittanceThreshold;

        return false;
    }

    inputSample = RescaleSampleUnder(inputSample, transmittanceThreshold);
    pdf *= pdfVol * transmittanceThreshold;

    // Exponential sampling
    float transmittance = transmittanceTMax + inputSample * (transmittanceTMin - transmittanceTMax);
    t = -log(transmittance) / sigmaT;
    pdf *= sigmaT * transmittance / (transmittanceTMin - transmittanceTMax);

    return true;
}

// Function responsible for volume scattering
void ComputeVolumeScattering(inout PathPayload payload : SV_RayPayload, float3 inputSample, bool sampleLocalLights, float3 lightPosition)
{
    // Reset the payload color, which will store our final result
    payload.value = 0.0;

    // Check if we want to compute direct and emissive lighting for current depth
    bool computeDirect = payload.segmentID >= _RaytracingMinRecursion - 1;

    // Compute the scattering position
    float3 scatteringPosition = WorldRayOrigin() + payload.rayTHit * WorldRayDirection();

    // Create the list of active lights (a local light can be forced by providing its position)
    LightList lightList = CreateLightList(scatteringPosition, sampleLocalLights, lightPosition);

    float pdf, shadowOpacity;
    float3 value;

    RayDesc ray;
    ray.Origin = scatteringPosition;
    ray.TMin = 0.0;

    PathPayload shadowPayload;

    // Light sampling
    if (computeDirect)
    {
        if (SampleLights(lightList, inputSample, scatteringPosition, 0.0, true, ray.Direction, value, pdf, ray.TMax, shadowOpacity))
        {
            // Apply phase function and divide by PDF
            value *= _HeightFogBaseScattering.xyz * ComputeHeightFogMultiplier(scatteringPosition.y) * INV_FOUR_PI / pdf;

            if (Luminance(value) > 0.001)
            {
                // Shoot a transmission ray
                shadowPayload.segmentID = SEGMENT_ID_TRANSMISSION;
                shadowPayload.value = 1.0;
                ray.TMax -= _RayTracingRayBias;

                // FIXME: For the time being, there is no front/back face culling for shadows
                TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER,
                         RAYTRACINGRENDERERFLAG_CAST_SHADOW, 0, 1, 1, ray, shadowPayload);

                payload.value += value * GetLightTransmission(shadowPayload.value, shadowOpacity);
            }
        }
    }

    // Override AOV motion vector information
    payload.aovMotionVector = 0.0;
}

#endif // UNITY_PATH_TRACING_VOLUME_INCLUDED
