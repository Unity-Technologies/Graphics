// Ray tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Shadows/SphericalQuad.hlsl"

// Path tracing includes
#ifdef HAS_LIGHTLOOP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingSampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Lit/LitPathTracing.hlsl"
#endif

bool RussianRouletteTest(float value, float rand, inout float factor, bool skip = false)
{
    // FIXME: to be tested and tuned further
    const float dynamicThreshold = 0.2 + 0.1 * _RaytracingMaxRecursion;

    if (skip || value >= dynamicThreshold)
        return true;

    if (rand * dynamicThreshold >= value)
        return false;

    factor = dynamicThreshold / value;
    return true;
}

float PowerHeuristic(float f, float b)
{
    return Sq(f) / (Sq(f) + Sq(b));
}

float3 GetPositionBias(float3 geomNormal, float bias, bool below)
{
    return geomNormal * (below ? -bias : bias);
}

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHit(inout PathIntersection pathIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // Always set the new t value
    pathIntersection.t = RayTCurrent();

    // If the max depth has been reached, bail out
    if (!pathIntersection.remainingDepth)
    {
        pathIntersection.color = 0.0;
        return;
    }

    // Grab depth information
    uint currentDepth = _RaytracingMaxRecursion - pathIntersection.remainingDepth;

    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, WorldRayDirection(), fragInput);

    // Let's compute the world space position (the non-camera relative one if camera relative rendering is enabled)
    const float3 position = GetAbsolutePositionWS(fragInput.positionRWS);

    // Make sure to add the additional travel distance
    pathIntersection.cone.width += pathIntersection.t * abs(pathIntersection.cone.spreadAngle);

#ifndef HAS_LIGHTLOOP
    // This is quick and dirty way to avoid double contribution from light meshes
    if (currentDepth)
        pathIntersection.cone.spreadAngle = -1.0;
#endif

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, pathIntersection.cone, isVisible);

    if (!isVisible)
    {
        // This should never happen, return magenta just in case
        pathIntersection.color = float3(1.0, 0.0, 0.5);
        return;
    }

    // Check if we want to compute direct and emissive lighting for current depth
    bool computeDirect = currentDepth >= _RaytracingMinRecursion - 1;

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

#ifdef HAS_LIGHTLOOP

    // Generate the new sample (following values of the sequence)
    float3 inputSample = 0.0;
    inputSample.x = GetSample(pathIntersection.pixelCoord, _RaytracingFrameIndex, 4 * currentDepth);
    inputSample.y = GetSample(pathIntersection.pixelCoord, _RaytracingFrameIndex, 4 * currentDepth + 1);
    inputSample.z = GetSample(pathIntersection.pixelCoord, _RaytracingFrameIndex, 4 * currentDepth + 2);

    // Get current path throughput
    float3 pathThroughput = pathIntersection.color;

    // And reset the ray intersection color, which will store our final result
    pathIntersection.color = computeDirect ? builtinData.emissiveColor : 0.0;

    // We adjust the BSDF data before doing anything else with it, to account for things computed in pre-lighting stage when rasterizing
    PreprocessBSDFData(pathIntersection, builtinData, bsdfData);

    // Initialize our material data
    MaterialData mtlData = CreateMaterialData(bsdfData);

    if (IsBlack(mtlData))
        return;

    // Create the list of active lights
    LightList lightList = CreateLightList(position, builtinData.renderingLayers);

    // Bunch of variables common to material and light sampling
    float pdf;
    float3 value;
    MaterialResult mtlResult;

    RayDesc rayDescriptor;
    rayDescriptor.Origin = position + bsdfData.geomNormalWS * _RaytracingRayBias;
    rayDescriptor.TMin = 0.0;

    PathIntersection nextPathIntersection;

    // Light sampling
    if (computeDirect)
    {
        if (SampleLights(lightList, inputSample, rayDescriptor.Origin, bsdfData.normalWS, rayDescriptor.Direction, value, pdf, rayDescriptor.TMax))
        {
            EvaluateMaterial(mtlData, rayDescriptor.Direction, mtlResult);

            value *= (mtlResult.diffValue + mtlResult.specValue) / pdf;
            if (Luminance(value) > 0.001)
            {
                // Shoot a transmission ray (to mark it as such, purposedly set remaining depth to an invalid value)
                nextPathIntersection.remainingDepth = _RaytracingMaxRecursion + 1;
                rayDescriptor.TMax -= _RaytracingRayBias;
                nextPathIntersection.color = 1.0;

                // FIXME: For the time being, we choose not to apply any back/front-face culling for shadows, will possibly change in the future
                TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER,
                         RAYTRACINGRENDERERFLAG_CAST_SHADOW, 0, 1, 1, rayDescriptor, nextPathIntersection);

                float misWeight = PowerHeuristic(pdf, mtlResult.diffPdf + mtlResult.specPdf);
                pathIntersection.color += value * nextPathIntersection.color * misWeight;
            }
        }
    }

    // Material sampling
    if (SampleMaterial(mtlData, inputSample, rayDescriptor.Direction, mtlResult))
    {
        // Compute overall material value and pdf
        pdf = mtlResult.diffPdf + mtlResult.specPdf;
        value = (mtlResult.diffValue + mtlResult.specValue) / pdf;

        // Apply Russian roulette to our path
        pathThroughput *= value;
        float russianRouletteValue = Luminance(pathThroughput);
        float russianRouletteFactor = 1.0;

        float rand = GetSample(pathIntersection.pixelCoord, _RaytracingFrameIndex, 4 * currentDepth + 3);
        if (RussianRouletteTest(russianRouletteValue, rand, russianRouletteFactor, !currentDepth))
        {
            bool isSampleBelow = IsBelow(mtlData, rayDescriptor.Direction);

            rayDescriptor.Origin = position + GetPositionBias(bsdfData.geomNormalWS, _RaytracingRayBias, isSampleBelow);
            rayDescriptor.TMax = FLT_INF;

            // Copy path constants across
            nextPathIntersection.pixelCoord = pathIntersection.pixelCoord;
            nextPathIntersection.cone.width = pathIntersection.cone.width;

            // Complete PathIntersection structure for this sample
            nextPathIntersection.color = pathThroughput * russianRouletteFactor;
            nextPathIntersection.remainingDepth = pathIntersection.remainingDepth - 1;
            nextPathIntersection.t = rayDescriptor.TMax;

            // In order to achieve filtering for the textures, we need to compute the spread angle of the pixel
            nextPathIntersection.cone.spreadAngle = pathIntersection.cone.spreadAngle + roughnessToSpreadAngle(nextPathIntersection.maxRoughness);

            // Adjust the max roughness, based on the estimated diff/spec ratio
            nextPathIntersection.maxRoughness = (mtlResult.specPdf * max(bsdfData.roughnessT, bsdfData.roughnessB) + mtlResult.diffPdf) / pdf;

#if defined(_SURFACE_TYPE_TRANSPARENT)
            // When transmitting with an IOR close to 1.0, roughness is barely noticeable -> take that into account for roughness clamping
            if (IsBelow(mtlData) != isSampleBelow)
                nextPathIntersection.maxRoughness = lerp(pathIntersection.maxRoughness, nextPathIntersection.maxRoughness, smoothstep(1.0, 1.3, bsdfData.ior));
#endif

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
                nextPathIntersection.color += lightValue * misWeight;
            }

#if defined(_SURFACE_TYPE_TRANSPARENT) && HAS_REFRACTION
            // Apply absorption on rays below the interface, using Beer-Lambert's law
            if (isSampleBelow)
            {
    #ifdef _REFRACTION_THIN
                nextPathIntersection.color *= exp(-bsdfData.absorptionCoefficient * REFRACTION_THIN_DISTANCE);
    #else
                // FIXME: maxDist might need some more tweaking
                float maxDist = surfaceData.atDistance * 10.0;
                nextPathIntersection.color *= exp(-bsdfData.absorptionCoefficient * min(nextPathIntersection.t, maxDist));
    #endif
            }
#endif

            pathIntersection.color += value * russianRouletteFactor * nextPathIntersection.color;
        }
    }

#else // HAS_LIGHTLOOP
    pathIntersection.color = (!currentDepth || computeDirect) ? bsdfData.color * GetInverseCurrentExposureMultiplier() + builtinData.emissiveColor : 0.0;
#endif

    if (currentDepth)
    {
        // Bias the result (making it too dark), but reduces fireflies a lot
        float intensity = Luminance(pathIntersection.color) * GetCurrentExposureMultiplier();
        if (intensity > _RaytracingIntensityClamp)
            pathIntersection.color *= _RaytracingIntensityClamp / intensity;
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
        pathIntersection.color *= surfaceData.transmittanceMask * surfaceData.transmittanceColor;
    #else
        pathIntersection.color *= 1.0 - builtinData.opacity;
    #endif
        if (Luminance(pathIntersection.color) < 0.001)
            AcceptHitAndEndSearch();
        else
            IgnoreHit();
#else
        // Opaque surface
        pathIntersection.color = 0.0;
        AcceptHitAndEndSearch();
#endif
    }
}
