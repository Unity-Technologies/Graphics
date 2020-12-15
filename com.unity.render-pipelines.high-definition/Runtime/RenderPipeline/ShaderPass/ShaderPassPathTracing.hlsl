// Ray tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Common/AtmosphericScatteringRayTracing.hlsl"

// Path tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingIntersection.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingVolume.hlsl"
#ifdef HAS_LIGHTLOOP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingSampling.hlsl"
#endif

float PowerHeuristic(float f, float b)
{
    return Sq(f) / (Sq(f) + Sq(b));
}

float3 GetPositionBias(float3 geomNormal, float bias, bool below)
{
    return geomNormal * (below ? -bias : bias);
}

// Function responsible for surface scattering
void ComputeSurfaceScattering(inout PathIntersection pathIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes, float4 inputSample)
{
    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, WorldRayDirection(), fragInput);

    // Such an invalid remainingDepth value means we are called from a subsurface computation
    if (pathIntersection.remainingDepth > _RaytracingMaxRecursion)
    {
        pathIntersection.value = fragInput.tangentToWorld[2]; // Returns normal
        return;
    }

    // Grab depth information
    uint currentDepth = _RaytracingMaxRecursion - pathIntersection.remainingDepth;

    // Make sure to add the additional travel distance
    pathIntersection.cone.width += pathIntersection.t * abs(pathIntersection.cone.spreadAngle);

#ifdef SHADER_UNLIT
    // This is quick and dirty way to avoid double contribution from light meshes
    if (currentDepth)
        pathIntersection.cone.spreadAngle = -1.0;
#endif

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = pathIntersection.pixelCoord;

    // For path tracing, we want the front-facing test to be performed on the actual geometric normal
    float3 geomNormal;
    GetCurrentIntersectionGeometricNormal(attributeData, geomNormal);
    fragInput.isFrontFace = dot(WorldRayDirection(), geomNormal) < 0.0;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, pathIntersection.cone, isVisible);

    // if (!isVisible)
    // {
    //     // This should never happen, return magenta just in case
    //     pathIntersection.value = float3(1.0, 0.0, 0.5);
    //     return;
    // }

    // Check if we want to compute direct and emissive lighting for current depth
    bool computeDirect = currentDepth >= _RaytracingMinRecursion - 1;

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

#ifndef SHADER_UNLIT

    // Override the geometric normal (otherwise, it is merely the non-mapped smooth normal)
    // Also make sure that it is in the same hemisphere as the shading normal (which may have been flipped)
    bsdfData.geomNormalWS = dot(bsdfData.normalWS, geomNormal) > 0.0 ? geomNormal : -geomNormal;

    // Compute the world space position (the non-camera relative one if camera relative rendering is enabled)
    float3 shadingPosition = fragInput.positionRWS;

    // Get current path throughput
    float3 pathThroughput = pathIntersection.value;

    // And reset the ray intersection color, which will store our final result
    pathIntersection.value = computeDirect ? builtinData.emissiveColor : 0.0;

    // Initialize our material data (this will alter the bsdfData to suit path tracing, and choose between BSDF or SSS evaluation)
    MaterialData mtlData;
    if (CreateMaterialData(pathIntersection, builtinData, bsdfData, shadingPosition, inputSample.z, mtlData))
    {
        // Create the list of active lights
    #ifdef _SURFACE_TYPE_TRANSPARENT
        float3 lightNormal = 0.0;
    #else
        float3 lightNormal = mtlData.bsdfData.geomNormalWS;
    #endif
        LightList lightList = CreateLightList(shadingPosition, lightNormal, builtinData.renderingLayers);

        // Bunch of variables common to material and light sampling
        float pdf;
        float3 value;
        MaterialResult mtlResult;

        RayDesc rayDescriptor;
        rayDescriptor.Origin = shadingPosition + mtlData.bsdfData.geomNormalWS * _RaytracingRayBias;
        rayDescriptor.TMin = 0.0;

        PathIntersection nextPathIntersection;

        // Light sampling
        if (computeDirect)
        {
            if (SampleLights(lightList, inputSample.xyz, rayDescriptor.Origin, mtlData.bsdfData.normalWS, rayDescriptor.Direction, value, pdf, rayDescriptor.TMax))
            {
                EvaluateMaterial(mtlData, rayDescriptor.Direction, mtlResult);

                value *= (mtlResult.diffValue + mtlResult.specValue) / pdf;
                if (Luminance(value) > 0.001)
                {
                    // Shoot a transmission ray (to mark it as such, purposedly set remaining depth to an invalid value)
                    nextPathIntersection.remainingDepth = _RaytracingMaxRecursion + 1;
                    rayDescriptor.TMax -= _RaytracingRayBias;
                    nextPathIntersection.value = 1.0;

                    // FIXME: For the time being, we choose not to apply any back/front-face culling for shadows, will possibly change in the future
                    TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER,
                             RAYTRACINGRENDERERFLAG_CAST_SHADOW, 0, 1, 1, rayDescriptor, nextPathIntersection);

                    float misWeight = PowerHeuristic(pdf, mtlResult.diffPdf + mtlResult.specPdf);
                    pathIntersection.value += value * nextPathIntersection.value * misWeight;
                }
            }
        }

        // Material sampling
        if (SampleMaterial(mtlData, inputSample.xyz, rayDescriptor.Direction, mtlResult))
        {
            // Compute overall material value and pdf
            pdf = mtlResult.diffPdf + mtlResult.specPdf;
            value = (mtlResult.diffValue + mtlResult.specValue) / pdf;

            pathThroughput *= value;

            // Apply Russian roulette to our path
            const float rrThreshold = 0.2 + 0.1 * _RaytracingMaxRecursion;
            float rrFactor, rrValue = Luminance(pathThroughput);

            if (RussianRouletteTest(rrThreshold, rrValue, inputSample.w, rrFactor, !currentDepth))
            {
                bool isSampleBelow = IsBelow(mtlData, rayDescriptor.Direction);

                rayDescriptor.Origin = shadingPosition + GetPositionBias(mtlData.bsdfData.geomNormalWS, _RaytracingRayBias, isSampleBelow);
                rayDescriptor.TMax = FLT_INF;

                // Copy path constants across
                nextPathIntersection.pixelCoord = pathIntersection.pixelCoord;
                nextPathIntersection.cone.width = pathIntersection.cone.width;

                // Complete PathIntersection structure for this sample
                nextPathIntersection.value = pathThroughput * rrFactor;
                nextPathIntersection.remainingDepth = pathIntersection.remainingDepth - 1;
                nextPathIntersection.t = rayDescriptor.TMax;

                // Adjust the path max roughness (used for roughness clamping, to reduce fireflies)
                nextPathIntersection.maxRoughness = AdjustPathRoughness(mtlData, mtlResult, isSampleBelow, pathIntersection.maxRoughness);

                // In order to achieve filtering for the textures, we need to compute the spread angle of the pixel
                nextPathIntersection.cone.spreadAngle = pathIntersection.cone.spreadAngle + roughnessToSpreadAngle(nextPathIntersection.maxRoughness);

                // Shoot ray for indirect lighting
                TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 2, rayDescriptor, nextPathIntersection);

                if (computeDirect)
                {
                    // Use same ray for direct lighting (use indirect result for occlusion)
                    rayDescriptor.TMax = nextPathIntersection.t + _RaytracingRayBias;
                    float3 lightValue;
                    float lightPdf;
                    EvaluateLights(lightList, rayDescriptor, lightValue, lightPdf);

                    float misWeight = PowerHeuristic(pdf, lightPdf);
                    nextPathIntersection.value += lightValue * misWeight;
                }

                // Apply material absorption
                float dist = min(nextPathIntersection.t, surfaceData.atDistance * 10.0);
                nextPathIntersection.value = ApplyAbsorption(mtlData, dist, isSampleBelow, nextPathIntersection.value);

                pathIntersection.value += value * rrFactor * nextPathIntersection.value;
            }
        }
    }

#else // SHADER_UNLIT

    pathIntersection.value = (!currentDepth || computeDirect) ? bsdfData.color * GetInverseCurrentExposureMultiplier() + builtinData.emissiveColor : 0.0;

// Simulate opacity blending by simply continuing along the current ray
#ifdef _SURFACE_TYPE_TRANSPARENT
    if (builtinData.opacity < 1.0)
    {
        RayDesc rayDescriptor;
        rayDescriptor.Origin = GetAbsolutePositionWS(fragInput.positionRWS) - fragInput.tangentToWorld[2] * _RaytracingRayBias;
        rayDescriptor.Direction = WorldRayDirection();
        rayDescriptor.TMin = 0.0;
        rayDescriptor.TMax = FLT_INF;

        PathIntersection nextPathIntersection = pathIntersection;
        nextPathIntersection.remainingDepth--;

        TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 2, rayDescriptor, nextPathIntersection);

        pathIntersection.value = lerp(nextPathIntersection.value, pathIntersection.value, builtinData.opacity);
    }
#endif

#endif // SHADER_UNLIT
}

// Generic function that handles one scattering event (a vertex along the full path), can be either:
// - Surface scattering
// - Volume scattering
[shader("closesthit")]
void ClosestHit(inout PathIntersection pathIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // Always set the new t value
    pathIntersection.t = RayTCurrent();

    // If the max depth has been reached, bail out
    if (!pathIntersection.remainingDepth)
    {
        pathIntersection.value = 0.0;
        return;
    }

    // Grab depth information
    int currentDepth = _RaytracingMaxRecursion - pathIntersection.remainingDepth;

    float4 inputSample = 0.0;

#ifdef HAS_LIGHTLOOP

    float pdf = 1.0;
    bool sampleLocalLights, sampleVolume = false;

    if (currentDepth >= 0)
    {
        // Generate a 4D unit-square sample for this depth, from our QMC sequence
        inputSample = GetSample4D(pathIntersection.pixelCoord, _RaytracingSampleIndex, 4 * currentDepth);

        // For the time being, we test for volumetric scattering only on camera rays
        if (!currentDepth)
            sampleVolume = SampleVolumeScatteringPosition(inputSample.w, pathIntersection.t, pdf, sampleLocalLights);
    }

    if (sampleVolume)
        ComputeVolumeScattering(pathIntersection, inputSample.xyz, sampleLocalLights);
    else
        ComputeSurfaceScattering(pathIntersection, attributeData, inputSample);

    // Apply the volume/surface pdf
    pathIntersection.value /= pdf;

#else // HAS_LIGHTLOOP

    ComputeSurfaceScattering(pathIntersection, attributeData, inputSample);

#endif // HAS_LIGHTLOOP

    // Apply volumetric attenuation
    bool computeDirect = currentDepth >= _RaytracingMinRecursion - 1;
    ApplyFogAttenuation(WorldRayOrigin(), WorldRayDirection(), pathIntersection.t, pathIntersection.value, computeDirect);

    if (currentDepth)
    {
        // Bias the result (making it too dark), but reduces fireflies a lot
        float intensity = Luminance(pathIntersection.value) * GetCurrentExposureMultiplier();
        if (intensity > _RaytracingIntensityClamp)
            pathIntersection.value *= _RaytracingIntensityClamp / intensity;
    }
}

[shader("anyhit")]
void AnyHit(inout PathIntersection pathIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // The first thing that we should do is grab the intersection vertice
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, WorldRayDirection(), fragInput);

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = pathIntersection.pixelCoord;

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
    else if (pathIntersection.remainingDepth > _RaytracingMaxRecursion)
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
