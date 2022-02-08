#ifndef UNITY_PATH_TRACING_INTEGRATOR_INCLUDED
#define UNITY_PATH_TRACING_INTEGRATOR_INCLUDED

// Ray tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Common/AtmosphericScatteringRayTracing.hlsl"

// Path tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingIntersection.hlsl"
#ifdef HAS_LIGHTLOOP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingVolume.hlsl"
#endif

float PowerHeuristic(float f, float b)
{
    return Sq(f) / (Sq(f) + Sq(b));
}

float3 GetPositionBias(float3 geomNormal, float bias, bool below)
{
    return geomNormal * (below ? -bias : bias);
}

#ifdef _ENABLE_SHADOW_MATTE

// Compute scalar visibility for shadow mattes, between 0 and 1
float ComputeVisibility(float3 position, float3 normal, float3 inputSample)
{
    // Select active types of lights
    bool withPoint = asuint(_ShadowMatteFilter) & LIGHTFEATUREFLAGS_PUNCTUAL;
    bool withArea = asuint(_ShadowMatteFilter) & LIGHTFEATUREFLAGS_AREA;
    bool withDistant = asuint(_ShadowMatteFilter) & LIGHTFEATUREFLAGS_DIRECTIONAL;

    LightList lightList = CreateLightList(position, normal, DEFAULT_LIGHT_LAYERS, withPoint, withArea, withDistant);

    RayDesc ray;
    ray.Origin = position + normal * _RaytracingRayBias;
    ray.TMin = 0.0;

    // By default, full visibility
    float visibility = 1.0;

    // We will ignore value and pdf here, as we only want to catch occluders (no distance falloffs, cosines, etc.)
    float3 value;
    float pdf, shadowOpacity;

    if (SampleLights(lightList, inputSample, ray.Origin, normal, false, ray.Direction, value, pdf, ray.TMax, shadowOpacity))
    {
        // Shoot a transmission ray (to mark it as such, purposedly set remaining depth to an invalid value)
        PathIntersection payload;
        payload.segmentID = SEGMENT_ID_TRANSMISSION;
        ray.TMax -= _RaytracingRayBias;
        payload.value = 1.0;

        // FIXME: For the time being, we choose not to apply any back/front-face culling for shadows, will possibly change in the future
        TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER,
                 RAYTRACINGRENDERERFLAG_CAST_SHADOW, 0, 1, 1, ray, payload);

        visibility = Luminance(GetLightTransmission(payload.value, shadowOpacity));
    }

    return visibility;
}

#endif // _ENABLE_SHADOW_MATTE

// Function responsible for surface scattering
void ComputeSurfaceScattering(inout PathIntersection payload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes, float4 inputSample)
{
    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, fragInput);

    // Check whether we are called from a subsurface scattering computation
    if (payload.segmentID == SEGMENT_ID_RANDOM_WALK)
    {
        payload.value = fragInput.tangentToWorld[2]; // Returns normal
        return;
    }

    // A null direction equates to no further continuation ray
    payload.rayDirection = 0.0;

    // Make sure to add the additional travel distance
    payload.cone.width += payload.rayTHit * abs(payload.cone.spreadAngle);

#ifdef SHADER_UNLIT
    // This is quick and dirty way to avoid double contribution from light meshes
    if (payload.segmentID)
        payload.cone.spreadAngle = -1.0;
#endif

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = payload.pixelCoord;

    // For path tracing, we want the front-facing test to be performed on the actual geometric normal
    float3 geomNormal;
    GetCurrentIntersectionGeometricNormal(attributeData, geomNormal);
    fragInput.isFrontFace = dot(WorldRayDirection(), geomNormal) < 0.0;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, payload.cone, isVisible);

    // Check if we want to compute direct and emissive lighting for current depth
    bool computeDirect = payload.segmentID >= _RaytracingMinRecursion - 1;

#ifndef SHADER_UNLIT

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

    // Override the geometric normal (otherwise, it is merely the non-mapped smooth normal)
    // Also make sure that it is in the same hemisphere as the shading normal (which may have been flipped)
    bsdfData.geomNormalWS = dot(bsdfData.normalWS, geomNormal) > 0.0 ? geomNormal : -geomNormal;

    // Compute the world space position (the non-camera relative one if camera relative rendering is enabled)
    float3 shadingPosition = fragInput.positionRWS;

    // And reset the payload value, which will store our final radiance result for this path depth
    payload.value = computeDirect ? builtinData.emissiveColor : 0.0;

    // Initialize our material data (this will alter the bsdfData to suit path tracing, and choose between BSDF or SSS evaluation)
    MaterialData mtlData;
    if (CreateMaterialData(payload, builtinData, bsdfData, shadingPosition, inputSample.z, mtlData))
    {
        // Create the list of active lights
    #ifdef _SURFACE_TYPE_TRANSPARENT
        float3 lightNormal = 0.0;
    #else
        float3 lightNormal = GetLightNormal(mtlData);
    #endif
        LightList lightList = CreateLightList(shadingPosition, lightNormal, builtinData.renderingLayers);

        float pdf, shadowOpacity;
        float3 value;
        MaterialResult mtlResult;

        RayDesc ray;
        ray.Origin = shadingPosition + mtlData.bsdfData.geomNormalWS * _RaytracingRayBias;
        ray.TMin = 0.0;

        PathIntersection shadowPayload;

        // Light sampling
        if (computeDirect)
        {
            if (SampleLights(lightList, inputSample.xyz, ray.Origin, lightNormal, false, ray.Direction, value, pdf, ray.TMax, shadowOpacity))
            {
                EvaluateMaterial(mtlData, ray.Direction, mtlResult);

                value *= (mtlResult.diffValue + mtlResult.specValue) / pdf;
                if (Luminance(value) > 0.001)
                {
                    // Shoot a transmission ray
                    shadowPayload.segmentID = SEGMENT_ID_TRANSMISSION;
                    ray.TMax -= _RaytracingRayBias;
                    shadowPayload.value = 1.0;

                    // FIXME: For the time being, we choose not to apply any back/front-face culling for shadows, will possibly change in the future
                    TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER,
                             RAYTRACINGRENDERERFLAG_CAST_SHADOW, 0, 1, 1, ray, shadowPayload);

                    float misWeight = PowerHeuristic(pdf, mtlResult.diffPdf + mtlResult.specPdf);
                    payload.value += value * GetLightTransmission(shadowPayload.value, shadowOpacity) * misWeight;
                }
            }
        }

        // Material sampling
        if (SampleMaterial(mtlData, inputSample.xyz, ray.Direction, mtlResult))
        {
            // Compute overall material value and pdf
            pdf = mtlResult.diffPdf + mtlResult.specPdf;
            value = (mtlResult.diffValue + mtlResult.specValue) / pdf;

            payload.throughput *= value;

            // Apply Russian roulette to our path
            const float rrThreshold = 0.2 + 0.1 * _RaytracingMaxRecursion;
            float rrFactor, rrValue = Luminance(payload.throughput);

            if (RussianRouletteTest(rrThreshold, rrValue, inputSample.w, rrFactor, !payload.segmentID))
            {
                bool isSampleBelow = IsBelow(mtlData, ray.Direction);

                ray.Origin = shadingPosition + GetPositionBias(mtlData.bsdfData.geomNormalWS, _RaytracingRayBias, isSampleBelow);
                ray.TMax = FLT_INF;

                // Prepare our shadow payload with all required information
                shadowPayload.segmentID = SEGMENT_ID_TRANSMISSION;
                shadowPayload.rayTHit = FLT_INF;

                // Shoot a shadow ray, and also get the nearest tHit, to optimize the continuation ray in the same direction
                TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 1, ray, shadowPayload);

                // Adjust the path max roughness (used for roughness clamping, to reduce fireflies)
                payload.maxRoughness = AdjustPathRoughness(mtlData, mtlResult, isSampleBelow, payload.maxRoughness);

                // To perform texture filtering, we maintain a footprint of the pixel
                payload.cone.spreadAngle = payload.cone.spreadAngle + roughnessToSpreadAngle(payload.maxRoughness);

                // Compute material absorption and apply it to our throughput
                float3 absorption = rrFactor * GetMaterialAbsorption(mtlData, surfaceData, shadowPayload.rayTHit, isSampleBelow);
                payload.throughput *= absorption;

                if (computeDirect)
                {
                    // Use same ray for direct lighting (use indirect result for occlusion)
                    ray.TMax = shadowPayload.rayTHit + _RaytracingRayBias;
                    float3 lightValue;
                    float lightPdf;
                    EvaluateLights(lightList, ray, lightValue, lightPdf);

                    float misWeight = PowerHeuristic(pdf, lightPdf);
                    payload.value += value * absorption * lightValue * misWeight;
                }

                // Update our payload to fire the next continuation ray
                SetContinuationRay(ray.Origin, ray.Direction, shadowPayload.rayTHit, payload);
            }
        }
    }

#else // SHADER_UNLIT

    payload.value = computeDirect ? surfaceData.color * GetInverseCurrentExposureMultiplier() + builtinData.emissiveColor : 0.0;

    // Apply shadow matte if requested
    #ifdef _ENABLE_SHADOW_MATTE
    float3 shadowColor = lerp(payload.value, surfaceData.shadowTint.rgb * GetInverseCurrentExposureMultiplier(), surfaceData.shadowTint.a);
    float visibility = ComputeVisibility(fragInput.positionRWS, surfaceData.normalWS, inputSample.xyz);
    payload.value = lerp(shadowColor, payload.value, visibility);
    #endif

    // Simulate opacity blending by simply continuing along the current ray
    #ifdef _SURFACE_TYPE_TRANSPARENT
    if (builtinData.opacity < 1.0)
    {
        float bias = dot(WorldRayDirection(), fragInput.tangentToWorld[2]) > 0.0 ? _RaytracingRayBias : -_RaytracingRayBias;
        float3 rayOrigin = fragInput.positionRWS + bias * fragInput.tangentToWorld[2];

        // Update our payload to fire a straight continuation ray
        payload.throughput *= 1.0 - builtinData.opacity;
        payload.value *= builtinData.opacity;
        SetContinuationRay(rayOrigin, WorldRayDirection(), -1.0, payload);
    }
    #endif

#endif // SHADER_UNLIT
}

// Generic function that handles one scattering event (a vertex along the full path), can be either:
// - Surface scattering
// - Volume scattering
[shader("closesthit")]
void ClosestHit(inout PathIntersection payload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // Always set the new t value
    payload.rayTHit = RayTCurrent();

    if (payload.segmentID == SEGMENT_ID_TRANSMISSION)
        return;

    bool computeDirect = payload.segmentID >= _RaytracingMinRecursion - 1;

    float4 inputSample = 0.0;
    float volSurfPdf = 1.0;

#ifdef HAS_LIGHTLOOP

    float3 lightPosition;
    bool sampleLocalLights, sampleVolume = false;

    // Skip this code if getting out of a SSS random walk
    if (payload.segmentID != SEGMENT_ID_RANDOM_WALK)
    {
        // Generate a 4D unit-square sample for this depth, from our QMC sequence
        inputSample = GetSample4D(payload.pixelCoord, _RaytracingSampleIndex, 4 * payload.segmentID);

        // For the time being, we test for volumetric scattering only on camera rays
        if (!payload.segmentID && computeDirect)
            sampleVolume = SampleVolumeScatteringPosition(payload.pixelCoord, inputSample.w, payload.rayTHit, volSurfPdf, sampleLocalLights, lightPosition);
    }

    if (sampleVolume)
        ComputeVolumeScattering(payload, inputSample.xyz, sampleLocalLights, lightPosition);
    else
        ComputeSurfaceScattering(payload, attributeData, inputSample);

    computeDirect &= !sampleVolume;

#else // HAS_LIGHTLOOP

    ComputeSurfaceScattering(payload, attributeData, inputSample);

#endif // HAS_LIGHTLOOP

    // Skip this code if getting out of a SSS random walk
    if (payload.segmentID != SEGMENT_ID_RANDOM_WALK)
    {
        // Apply volumetric attenuation (beware of passing the right distance to the shading point)
        ApplyFogAttenuation(WorldRayOrigin(), WorldRayDirection(), sampleVolume ? payload.rayTHit : RayTCurrent(),
                            payload.value, payload.throughput, computeDirect);

        // Apply the volume/surface PDF
        payload.value /= volSurfPdf;
        payload.throughput /= volSurfPdf;
    }
}

[shader("anyhit")]
void AnyHit(inout PathIntersection payload : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // The first thing that we should do is grab the intersection vertex
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
    }
    else if (payload.segmentID == SEGMENT_ID_TRANSMISSION)
    {
#ifdef _SURFACE_TYPE_TRANSPARENT
    #if HAS_REFRACTION
        payload.value *= surfaceData.transmittanceMask * surfaceData.transmittanceColor;
    #else
        payload.value *= 1.0 - builtinData.opacity;
    #endif
        if (Luminance(payload.value) < 0.001)
            AcceptHitAndEndSearch();
        else
            IgnoreHit();
#else
        // Opaque surface
        payload.value = 0.0;
        AcceptHitAndEndSearch();
#endif
    }
}

#endif // UNITY_PATH_TRACING_INTEGRATOR_INCLUDED
