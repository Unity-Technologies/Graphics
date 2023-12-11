#ifndef UNITY_PATH_TRACING_INTEGRATOR_INCLUDED
#define UNITY_PATH_TRACING_INTEGRATOR_INCLUDED

#define ENABLE_MATERIAL_AMBIENT_OCCLUSION

// Ray tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Common/AtmosphericScatteringRayTracing.hlsl"

// Path tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingPayload.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingSkySampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingAOV.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingSurface.hlsl"
#ifdef HAS_LIGHTLOOP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingVolume.hlsl"
#endif

float3 GetSkyValue(PathPayload payload, float3 direction)
{
    if (payload.segmentID == 0 && all(direction == WorldRayDirection()))
    {
        // If we can, access our high resolution screen-space background
        return GetSkyBackground(payload.pixelCoord).rgb;
    }

    // Otherwise query the lower resolution cubemap
    return GetSkyValue(direction);
}

// Generic function that handles one scattering event (a vertex along the full path), can be either:
// - Surface scattering
// - Volume scattering
[shader("closesthit")]
void ClosestHit(inout PathPayload payload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // Always set the new t and initial alpha value
    payload.rayTHit = RayTCurrent();
    payload.alpha = 1.0;

    bool minDepthAllowsEmissive = payload.segmentID >= _RaytracingMinRecursion - 1;
    bool sampleVolume = false;

    float4 inputSample = 0.0;
    float volSurfPdf = 1.0;

    SurfaceInfo surfaceInfo = (SurfaceInfo)0;

#ifdef HAS_LIGHTLOOP

    float3 lightPosition;
    bool sampleLocalLights;

    // Generate a 4D unit-square sample for this depth, from our QMC sequence
    inputSample = GetSample4D(payload.pixelCoord, _RaytracingSampleIndex, 4 * payload.segmentID);

    // For the time being, we test for volumetric scattering only on camera rays
    float scatteringT = payload.rayTHit;
    if (!payload.segmentID && minDepthAllowsEmissive)
        sampleVolume = SampleVolumeScatteringPosition(payload.pixelCoord, inputSample.w, scatteringT, volSurfPdf, sampleLocalLights, lightPosition);

    // If we need to sample volume we won't do the scattering part in the function but we might still need it to evaluate
    // the AOV values if we write the volumetric scattering values separately in another AOV.
    const bool alwaysWriteAOV = NeedAOVData(payload);
    if (!sampleVolume || alwaysWriteAOV)
    {
        GetSurfaceInfo(payload, attributeData, surfaceInfo);
        PushSurfaceAOVData(surfaceInfo, payload);
    }

    if (sampleVolume)
    {
        payload.rayTHit = scatteringT;
        ComputeVolumeScattering(payload, inputSample.xyz, sampleLocalLights, lightPosition);

        // Override AOV motion vector information unless we always need to output the value
        if (!alwaysWriteAOV)
            payload.aovMotionVector = 0.0;
    }
    else
    {
        ComputeSurfaceScattering(payload, surfaceInfo, inputSample);
    }

    minDepthAllowsEmissive &= !sampleVolume;

    // If we don't always write AOV data, depending if we have evaluated a surface or volume scattering event,
    // we need to normalize with the PDF of going through one of these two events. If we always write AOV data
    // then probability is 1 anyway.
    if (!alwaysWriteAOV)
    {
        payload.aovAlbedo /= volSurfPdf;
        payload.aovNormal /= volSurfPdf;
        payload.aovMotionVector /= volSurfPdf;
    }

#else // HAS_LIGHTLOOP

    GetSurfaceInfo(payload, attributeData, surfaceInfo);
    PushSurfaceAOVData(surfaceInfo, payload);
    ComputeSurfaceScattering(payload, surfaceInfo, inputSample);

#endif // HAS_LIGHTLOOP

    // Apply volumetric attenuation (beware of passing the right distance to the shading point)
    ApplyFogAttenuation(WorldRayOrigin(), WorldRayDirection(), payload.rayTHit, payload.value, payload.lightSampleShadowColor, payload.alpha,
                        payload.lightSampleShadowOpacityAndShadowTint.y, payload.throughput, payload.segmentThroughput, payload.lightSampleValue, minDepthAllowsEmissive);

    // Apply the volume/surface PDF
    payload.value /= volSurfPdf;
    payload.alpha /= volSurfPdf;
    payload.lightSampleShadowOpacityAndShadowTint.y /= volSurfPdf;
    payload.throughput /= volSurfPdf;
    payload.segmentThroughput /= volSurfPdf;
    payload.lightSampleValue /= volSurfPdf;
}


[shader("anyhit")]
void AnyHit(inout PathPayload payload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
#ifdef _ALPHATEST_ON

    // First grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, fragInput);

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = payload.pixelCoord;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, payload.cone, isVisible);

    // Check alpha clipping
    if (!isVisible)
    {
        IgnoreHit();
        return;
    }

#endif // _ALPHATEST_ON

    if (payload.segmentID == SEGMENT_ID_NEAREST_HIT )
    {
        // We just need the nearest hit distance here
        payload.rayTHit = min(payload.rayTHit, RayTCurrent());
    }
    else if (payload.segmentID == SEGMENT_ID_RANDOM_WALK)
    {
        if (RayTCurrent() < payload.rayTHit)
        {

#ifndef _ALPHATEST_ON

            // First grab the intersection vertex
            IntersectionVertex currentVertex;
            GetCurrentIntersectionVertex(attributeData, currentVertex);

            // Build the Frag inputs from the intersection vertex
            FragInputs fragInput;
            BuildFragInputsFromIntersection(currentVertex, fragInput);

#endif // _ALPHATEST_ON

            payload.value = fragInput.tangentToWorld[2];
            payload.rayTHit = RayTCurrent();
        }
    }
    else if (payload.segmentID == SEGMENT_ID_TRANSMISSION)
    {
#ifdef _SURFACE_TYPE_TRANSPARENT

    #ifndef _ALPHATEST_ON

        // First grab the intersection vertex
        IntersectionVertex currentVertex;
        GetCurrentIntersectionVertex(attributeData, currentVertex);

        // Build the Frag inputs from the intersection vertex
        FragInputs fragInput;
        BuildFragInputsFromIntersection(currentVertex, fragInput);

        PositionInputs posInput;
        posInput.positionWS = fragInput.positionRWS;
        posInput.positionSS = payload.pixelCoord;

        // Build the surfacedata and builtindata
        SurfaceData surfaceData;
        BuiltinData builtinData;
        bool isVisible;
        GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, payload.cone, isVisible);

    #endif // _ALPHATEST_ON

    #if HAS_REFRACTION
        payload.value *= surfaceData.transmittanceMask * surfaceData.transmittanceColor;
    #else
        payload.value *= 1.0 - builtinData.opacity;
    #endif
        if (Luminance(payload.value) > 0)
        {
            IgnoreHit();
        }

#else // _SURFACE_TYPE_TRANSPARENT

        payload.value = 0.0; // Opaque surface

#endif // _SURFACE_TYPE_TRANSPARENT

    }
#ifdef _PATH_TRACED_DUAL_SCATTERING
    else if (payload.segmentID == SEGMENT_ID_DUAL_SCATTERING)
    {
        // We have intersected one strand.
        payload.alpha = payload.alpha + 1.0;

        // And keep going until TMax.
        IgnoreHit();
    }
    else if (payload.segmentID == SEGMENT_ID_DUAL_SCATTERING_VIS)
    {
        IgnoreHit();
        return;
    }
#endif
}

#endif // UNITY_PATH_TRACING_INTEGRATOR_INCLUDED
