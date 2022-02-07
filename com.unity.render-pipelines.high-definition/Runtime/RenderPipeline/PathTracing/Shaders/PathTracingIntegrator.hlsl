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

    RayDesc rayDescriptor;
    rayDescriptor.Origin = position + normal * _RaytracingRayBias;
    rayDescriptor.TMin = 0.0;

    // By default, full visibility
    float visibility = 1.0;

    // We will ignore value and pdf here, as we only want to catch occluders (no distance falloffs, cosines, etc.)
    float3 value;
    float pdf, shadowOpacity;

    if (SampleLights(lightList, inputSample, rayDescriptor.Origin, normal, false, rayDescriptor.Direction, value, pdf, rayDescriptor.TMax, shadowOpacity))
    {
        // Shoot a transmission ray (to mark it as such, purposedly set remaining depth to an invalid value)
        PathIntersection intersection;
        SetSegmentID(SEGMENT_ID_TRANSMISSION, intersection)
        rayDescriptor.TMax -= _RaytracingRayBias;
        intersection.value = 1.0;

        // FIXME: For the time being, we choose not to apply any back/front-face culling for shadows, will possibly change in the future
        TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER,
                 RAYTRACINGRENDERERFLAG_CAST_SHADOW, 0, 1, 1, rayDescriptor, intersection);

        visibility = Luminance(GetLightTransmission(intersection.value, shadowOpacity));
    }

    return visibility;
}

#endif // _ENABLE_SHADOW_MATTE

float3 ClampValue(float3 value)
{
    float intensity = Luminance(value) * GetCurrentExposureMultiplier();
    return intensity > _RaytracingIntensityClamp ? value * _RaytracingIntensityClamp / intensity : value;
}

// Function responsible for surface scattering
void ComputeSurfaceScattering(inout PathIntersection pathIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes, float4 inputSample)
{
    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, fragInput);

    // Grab our path segment ID (depth of the main path, unless we are dealing with a special segment)
    uint segmentID = GetSegmentID(pathIntersection);

    // Check whether we are called from a subsurface scattering computation
    if (segmentID == SEGMENT_ID_RANDOM_WALK)
    {
        pathIntersection.value = fragInput.tangentToWorld[2]; // Returns normal
        return;
    }

    // A null direction equates to no further continuation ray
    pathIntersection.rayDirection = 0.0;

    // Make sure to add the additional travel distance
    pathIntersection.cone.width += pathIntersection.rayTHit * abs(pathIntersection.cone.spreadAngle);

#ifdef SHADER_UNLIT
    // This is quick and dirty way to avoid double contribution from light meshes
    if (segmentID)
        pathIntersection.cone.spreadAngle = -1.0;
#endif

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = GetPixelCoordinates(pathIntersection);

    // For path tracing, we want the front-facing test to be performed on the actual geometric normal
    float3 geomNormal;
    GetCurrentIntersectionGeometricNormal(attributeData, geomNormal);
    fragInput.isFrontFace = dot(WorldRayDirection(), geomNormal) < 0.0;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, pathIntersection.cone, isVisible);

    // Check if we want to compute direct and emissive lighting for current depth
    bool computeDirect = segmentID >= _RaytracingMinRecursion - 1;

#ifndef SHADER_UNLIT

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

    // Override the geometric normal (otherwise, it is merely the non-mapped smooth normal)
    // Also make sure that it is in the same hemisphere as the shading normal (which may have been flipped)
    bsdfData.geomNormalWS = dot(bsdfData.normalWS, geomNormal) > 0.0 ? geomNormal : -geomNormal;

    // Compute the world space position (the non-camera relative one if camera relative rendering is enabled)
    float3 shadingPosition = fragInput.positionRWS;

    // // Get current path throughput
    // float3 pathThroughput = pathIntersection.value;

    // And reset the ray intersection color, which will store our final result
    pathIntersection.value = computeDirect ? pathIntersection.throughput * builtinData.emissiveColor : 0.0;

    // Initialize our material data (this will alter the bsdfData to suit path tracing, and choose between BSDF or SSS evaluation)
    MaterialData mtlData;
    if (CreateMaterialData(pathIntersection, builtinData, bsdfData, shadingPosition, inputSample.z, mtlData))
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

        RayDesc rayDescriptor;
        rayDescriptor.Origin = shadingPosition + mtlData.bsdfData.geomNormalWS * _RaytracingRayBias;
        rayDescriptor.TMin = 0.0;

        PathIntersection nextPathIntersection;

        // Light sampling
        if (computeDirect)
        {
            if (SampleLights(lightList, inputSample.xyz, rayDescriptor.Origin, lightNormal, false, rayDescriptor.Direction, value, pdf, rayDescriptor.TMax, shadowOpacity))
            {
                EvaluateMaterial(mtlData, rayDescriptor.Direction, mtlResult);

                value *= (mtlResult.diffValue + mtlResult.specValue) / pdf;
                if (Luminance(value) > 0.001)
                {
                    // Shoot a transmission ray
                    SetSegmentID(SEGMENT_ID_TRANSMISSION, nextPathIntersection);
                    rayDescriptor.TMax -= _RaytracingRayBias;
                    nextPathIntersection.value = 1.0;

                    // FIXME: For the time being, we choose not to apply any back/front-face culling for shadows, will possibly change in the future
                    TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER,
                             RAYTRACINGRENDERERFLAG_CAST_SHADOW, 0, 1, 1, rayDescriptor, nextPathIntersection);

                    float misWeight = PowerHeuristic(pdf, mtlResult.diffPdf + mtlResult.specPdf);
                    pathIntersection.value += pathIntersection.throughput * value * GetLightTransmission(nextPathIntersection.value, shadowOpacity) * misWeight;
                }
            }
        }

        // Material sampling
        if (SampleMaterial(mtlData, inputSample.xyz, rayDescriptor.Direction, mtlResult))
        {
            // Compute overall material value and pdf
            pdf = mtlResult.diffPdf + mtlResult.specPdf;
            value = (mtlResult.diffValue + mtlResult.specValue) / pdf;

            pathIntersection.throughput *= value;

            // Apply Russian roulette to our path
            const float rrThreshold = 0.2 + 0.1 * _RaytracingMaxRecursion;
            float rrFactor, rrValue = Luminance(pathIntersection.throughput);

            if (RussianRouletteTest(rrThreshold, rrValue, inputSample.w, rrFactor, !segmentID))
            {
                bool isSampleBelow = IsBelow(mtlData, rayDescriptor.Direction);

                rayDescriptor.Origin = shadingPosition + GetPositionBias(mtlData.bsdfData.geomNormalWS, _RaytracingRayBias, isSampleBelow);
                rayDescriptor.TMax = FLT_INF;

                // Prepare our shadow payload with all required information
                SetSegmentID(SEGMENT_ID_TRANSMISSION, nextPathIntersection);
                nextPathIntersection.rayTHit = FLT_INF;

                // Shoot a shadow ray, and also get the nearest tHit, to optimize the continuation ray in the same direction
                TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 1, rayDescriptor, nextPathIntersection);
                // TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES | RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER,
                //          RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 1, rayDescriptor, nextPathIntersection);

                // Adjust the path max roughness (used for roughness clamping, to reduce fireflies)
                pathIntersection.maxRoughness = AdjustPathRoughness(mtlData, mtlResult, isSampleBelow, pathIntersection.maxRoughness);

                // To perform texture filtering, we maintain a footprint of the pixel
                pathIntersection.cone.spreadAngle = pathIntersection.cone.spreadAngle + roughnessToSpreadAngle(pathIntersection.maxRoughness);

                // Apply material absorption to our throughput
                pathIntersection.throughput = rrFactor * ApplyAbsorption(mtlData, surfaceData, nextPathIntersection.rayTHit, isSampleBelow, pathIntersection.throughput);

                if (computeDirect)
                {
                    // Use same ray for direct lighting (use indirect result for occlusion)
                    rayDescriptor.TMax = nextPathIntersection.rayTHit + _RaytracingRayBias;
                    float3 lightValue;
                    float lightPdf;
                    EvaluateLights(lightList, rayDescriptor, lightValue, lightPdf);

                    float misWeight = PowerHeuristic(pdf, lightPdf);
                    pathIntersection.value += pathIntersection.throughput * lightValue * misWeight;
                }

                // Update our payload to fire the next continuation ray
                pathIntersection.rayOrigin = rayDescriptor.Origin;
                pathIntersection.rayDirection = rayDescriptor.Direction;
                pathIntersection.rayTHit = nextPathIntersection.rayTHit;
            }
        }
    }

#else // SHADER_UNLIT

    pathIntersection.value = computeDirect ? surfaceData.color * GetInverseCurrentExposureMultiplier() + builtinData.emissiveColor : 0.0;

    // Apply shadow matte if requested
    #ifdef _ENABLE_SHADOW_MATTE
    float3 shadowColor = lerp(pathIntersection.value, surfaceData.shadowTint.rgb * GetInverseCurrentExposureMultiplier(), surfaceData.shadowTint.a);
    float visibility = ComputeVisibility(fragInput.positionRWS, surfaceData.normalWS, inputSample.xyz);
    pathIntersection.value = lerp(shadowColor, pathIntersection.value, visibility);
    #endif

// FIXME!! Reactivate

    // // Simulate opacity blending by simply continuing along the current ray
    // #ifdef _SURFACE_TYPE_TRANSPARENT
    // if (builtinData.opacity < 1.0)
    // {
    //     RayDesc rayDescriptor;
    //     float bias = dot(WorldRayDirection(), fragInput.tangentToWorld[2]) > 0.0 ? _RaytracingRayBias : -_RaytracingRayBias;
    //     rayDescriptor.Origin = fragInput.positionRWS + bias * fragInput.tangentToWorld[2];
    //     rayDescriptor.Direction = WorldRayDirection();
    //     rayDescriptor.TMin = 0.0;
    //     rayDescriptor.TMax = FLT_INF;

    //     PathIntersection nextPathIntersection = pathIntersection;
    //     nextPathIntersection.remainingDepth--;

    //     TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 3, rayDescriptor, nextPathIntersection);

    //     pathIntersection.value = lerp(nextPathIntersection.value, pathIntersection.value, builtinData.opacity);
    // }
    // #endif

#endif // SHADER_UNLIT
}

// Generic function that handles one scattering event (a vertex along the full path), can be either:
// - Surface scattering
// - Volume scattering
[shader("closesthit")]
void ClosestHit(inout PathIntersection pathIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // Always set the new t value
    pathIntersection.rayTHit = RayTCurrent();

    uint segmentID = GetSegmentID(pathIntersection);
    if (segmentID == SEGMENT_ID_TRANSMISSION)
        return;

    // FIXME: we should not need this test anymore, since we have removed recursion
    // // If the max depth has been reached, bail out
    // if (!pathIntersection.remainingDepth)
    // {
    //     pathIntersection.value = 0.0;
    //     return;
    // }

    bool computeDirect = segmentID >= _RaytracingMinRecursion - 1;

    float4 inputSample = 0.0;
    float pdf = 1.0;

#ifdef HAS_LIGHTLOOP

    float3 lightPosition;
    bool sampleLocalLights, sampleVolume = false;

    // Skip this code if getting out of a SSS random walk
    if (segmentID != SEGMENT_ID_RANDOM_WALK)
    {
        // Generate a 4D unit-square sample for this depth, from our QMC sequence
        inputSample = GetSample4D(GetPixelCoordinates(pathIntersection), _RaytracingSampleIndex, 4 * segmentID);

        // For the time being, we test for volumetric scattering only on camera rays
        if (!segmentID && computeDirect)
            sampleVolume = SampleVolumeScatteringPosition(GetPixelCoordinates(pathIntersection), inputSample.w, pathIntersection.rayTHit, pdf, sampleLocalLights, lightPosition);
    }

    if (sampleVolume)
        ComputeVolumeScattering(pathIntersection, inputSample.xyz, sampleLocalLights, lightPosition);
    else
        ComputeSurfaceScattering(pathIntersection, attributeData, inputSample);

    computeDirect &= !sampleVolume;

#else // HAS_LIGHTLOOP

    ComputeSurfaceScattering(pathIntersection, attributeData, inputSample);

#endif // HAS_LIGHTLOOP

    // Skip this code if getting out of a SSS random walk (currentDepth < 0)
    // if (currentDepth >= 0)
    // {
    //     // FIXME : need to be added again, with separation between thoughput and value for this depth
    //     // Apply volumetric attenuation
    //     ApplyFogAttenuation(WorldRayOrigin(), WorldRayDirection(), pathIntersection.t, pathIntersection.value, computeDirect);

    //     // Apply the volume/surface pdf
    //     pathIntersection.value /= pdf;

    //     // Apply clamping on indirect values (can darken the result slightly, but significantly reduces fireflies)
    //     if (currentDepth)
    //         pathIntersection.value = ClampValue(pathIntersection.value);
    // }
}

[shader("anyhit")]
void AnyHit(inout PathIntersection pathIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, fragInput);

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = GetPixelCoordinates(pathIntersection);

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, pathIntersection.cone, isVisible);

    // Check alpha clipping
    if (!isVisible)
    {
        IgnoreHit();
    }
    // else if (pathIntersection.remainingDepth == _RaytracingMaxRecursion + 2) // FIXME: SegmentID
    // {
    //     pathIntersection.t = min(RayTCurrent(), pathIntersection.t);
    // }
    else if (GetSegmentID(pathIntersection) == SEGMENT_ID_TRANSMISSION)
    {
#ifdef _SURFACE_TYPE_TRANSPARENT
    #if HAS_REFRACTION
        pathIntersection.value *= surfaceData.transmittanceMask * surfaceData.transmittanceColor;
    #else
        pathIntersection.value *= 1.0 - builtinData.opacity;
    #endif
        if (Luminance(pathIntersection.value) < 0.001)
            AcceptHitAndEndSearch();
        else
            IgnoreHit();
#else
        // Opaque surface
        pathIntersection.value = 0.0;
        AcceptHitAndEndSearch();
#endif
    }
}

#endif // UNITY_PATH_TRACING_INTEGRATOR_INCLUDED
