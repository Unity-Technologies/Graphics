#ifndef UNITY_PATH_TRACING_VOLUME_INCLUDED
#define UNITY_PATH_TRACING_VOLUME_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

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

    SetPathTracingFlag(payload, PATHTRACING_FLAG_VOLUME_INTERACTION);

    // Check if we want to compute direct lighting for current depth
    bool minDepthAllowsDirect = payload.segmentID + 1 >= _RaytracingMinRecursion - 1;
    // Check if we want to send more rays after this segment
    bool haveReachedMaxDepth = payload.segmentID + 1 > _RaytracingMaxRecursion - 1;

    // Compute the scattering position
    float3 scatteringPosition = WorldRayOrigin() + payload.rayTHit * WorldRayDirection();
    float3 incomingDirection = WorldRayDirection();

    // Create the list of active lights (a local light can be forced by providing its position)
    LightList lightList = CreateLightList(scatteringPosition, sampleLocalLights, lightPosition);
    payload.lightListParams = float4(lightPosition, sampleLocalLights ? 1 : 0);

    float pdf, shadowOpacity;
    float3 value;
    float3 sampleRayDirection;
    float sampleRayDistance;

    if (minDepthAllowsDirect && !haveReachedMaxDepth)
    {
        float scatteringHeight = dot(scatteringPosition, _PlanetUp);

        // Light sampling
        if (SampleLights(lightList, inputSample, scatteringPosition, 0.0, true, sampleRayDirection, value, pdf, sampleRayDistance, shadowOpacity))
        {
            // Apply phase function and divide by PDF
            float phasePdf = HenyeyGreensteinPhaseFunction(_GlobalFogAnisotropy,  dot(incomingDirection, sampleRayDirection));
            value *= _HeightFogBaseScattering.xyz * ComputeHeightFogMultiplier(scatteringPosition.y) * phasePdf / pdf;

            if (GetCurrentExposureMultiplier() * Luminance(value) > 0.0001)
            {
                PushLightSampleQuery(scatteringPosition, sampleRayDirection, sampleRayDistance - _RayTracingRayBias, PowerHeuristic(pdf, phasePdf) * value, shadowOpacity, payload);
            }
        }

        // Phase function sampling
        if (SampleHenyeyGreenstein(incomingDirection, _GlobalFogAnisotropy, inputSample, sampleRayDirection, pdf))
        {
            // Applying phase function and dividing by PDF cancels out
            value = _HeightFogBaseScattering.xyz * ComputeHeightFogMultiplier(scatteringHeight);

            if (Luminance(value) > 0.001)
            {
                payload.throughput *= value;
                payload.interactionThroughput *= value;
                payload.materialSamplePdf = pdf;
                PushMaterialSampleQuery(scatteringPosition, sampleRayDirection, payload);
            }
        }
    }
}

#endif // UNITY_PATH_TRACING_VOLUME_INCLUDED
