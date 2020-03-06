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

float3 GetPositionBias(float3 geomNormal, float3 dir, float bias)
{
    return geomNormal * (dot(geomNormal, dir) > 0.0 ? bias : -bias);
}

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHit(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // Always set the new t value
    rayIntersection.t = RayTCurrent();

    // If the max depth has been reached (or remaining depth is supsiciously large), bail out
    if ((rayIntersection.remainingDepth == 0) || (rayIntersection.remainingDepth > _RaytracingMaxRecursion))
    {
        rayIntersection.color = 0.0;
        return;
    }

    // Grab depth information
    uint currentDepth = _RaytracingMaxRecursion - rayIntersection.remainingDepth;

    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, WorldRayDirection(), fragInput);

    // Let's compute the world space position (the non-camera relative one if camera relative rendering is enabled)
    const float3 position = GetAbsolutePositionWS(fragInput.positionRWS);

    // Make sure to add the additional travel distance
    rayIntersection.cone.width += rayIntersection.t * abs(rayIntersection.cone.spreadAngle);

#ifndef HAS_LIGHTLOOP
    // This is quick and dirty way to avoid double contribution from light meshes
    if (currentDepth)
        rayIntersection.cone.spreadAngle = -1.0;
#endif

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible;
    GetSurfaceAndBuiltinData(fragInput, -WorldRayDirection(), posInput, surfaceData, builtinData, currentVertex, rayIntersection.cone, isVisible);

    // Check if we want to compute direct and emissive lighting for current depth
    bool computeDirect = currentDepth >= _RaytracingMinRecursion - 1;

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

#ifdef HAS_LIGHTLOOP

    // FIXME: Adjust roughness to reduce fireflies
    bsdfData.roughnessT = max(rayIntersection.maxRoughness, bsdfData.roughnessT);
    bsdfData.roughnessB = max(rayIntersection.maxRoughness, bsdfData.roughnessB);

    // Generate the new sample (following values of the sequence)
    float3 inputSample = 0.0;
    inputSample.x = GetSample(rayIntersection.pixelCoord, _RaytracingFrameIndex, 4 * currentDepth);
    inputSample.y = GetSample(rayIntersection.pixelCoord, _RaytracingFrameIndex, 4 * currentDepth + 1);
    inputSample.z = GetSample(rayIntersection.pixelCoord, _RaytracingFrameIndex, 4 * currentDepth + 2);

    // Get current path throughput
    float3 pathThroughput = rayIntersection.color;

    // And reset the ray intersection color, which will store our final result
    rayIntersection.color = computeDirect ? builtinData.emissiveColor : 0.0;

    // Initialize our material data
    MaterialData mtlData = CreateMaterialData(bsdfData, -WorldRayDirection());

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

    RayIntersection nextRayIntersection;

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
                nextRayIntersection.remainingDepth = _RaytracingMaxRecursion + 1;
                rayDescriptor.TMax -= _RaytracingRayBias;
                nextRayIntersection.t = rayDescriptor.TMax;

                // FIXME: For the time being, we choose not to apply any back/front-face culling for shadows, will possibly change in the future
                TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_FORCE_NON_OPAQUE | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER | RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH,
                         RAYTRACINGRENDERERFLAG_CAST_SHADOW, 0, 1, 0, rayDescriptor, nextRayIntersection);

                if (nextRayIntersection.t >= rayDescriptor.TMax)
                {
                    float misWeight = PowerHeuristic(pdf, mtlResult.diffPdf + mtlResult.specPdf);
                    rayIntersection.color += value * misWeight;
                }
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

        float rand = GetSample(rayIntersection.pixelCoord, _RaytracingFrameIndex, 4 * currentDepth + 3);
        if (RussianRouletteTest(russianRouletteValue, rand, russianRouletteFactor, !currentDepth))
        {
            rayDescriptor.Origin = position + GetPositionBias(bsdfData.geomNormalWS, rayDescriptor.Direction, _RaytracingRayBias);
            rayDescriptor.TMax = FLT_INF;

            // Copy path constants across
            nextRayIntersection.pixelCoord = rayIntersection.pixelCoord;
            nextRayIntersection.cone.width = rayIntersection.cone.width;

            // Complete RayIntersection structure for this sample
            nextRayIntersection.color = pathThroughput * russianRouletteFactor;
            nextRayIntersection.remainingDepth = rayIntersection.remainingDepth - 1;
            nextRayIntersection.t = rayDescriptor.TMax;

            // Adjust the max roughness, based on the estimated diff/spec ratio
            nextRayIntersection.maxRoughness = (mtlResult.specPdf * max(bsdfData.roughnessT, bsdfData.roughnessB) + mtlResult.diffPdf) / pdf;

            // In order to achieve filtering for the textures, we need to compute the spread angle of the pixel
            nextRayIntersection.cone.spreadAngle = rayIntersection.cone.spreadAngle + roughnessToSpreadAngle(nextRayIntersection.maxRoughness);

            // Shoot ray for indirect lighting
            TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES | RAY_FLAG_FORCE_OPAQUE, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 0, rayDescriptor, nextRayIntersection);

            if (computeDirect)
            {
                // Use same ray for direct lighting (use indirect result for occlusion)
                rayDescriptor.TMax = nextRayIntersection.t + _RaytracingRayBias;
                float3 lightValue;
                float lightPdf;
                EvaluateLights(lightList, rayDescriptor, lightValue, lightPdf);

                float misWeight = PowerHeuristic(pdf, lightPdf);
                nextRayIntersection.color += lightValue * misWeight;
            }

#if HAS_REFRACTION
            // Apply absorption on rays below the interface, using Beer-Lambert's law
            if (isfinite(nextRayIntersection.t) && IsBelow(mtlData, rayDescriptor.Direction))
            {
#ifdef _REFRACTION_THIN
                nextRayIntersection.color *= exp(-mtlData.bsdfData.absorptionCoefficient * REFRACTION_THIN_DISTANCE);
#else
                nextRayIntersection.color *= exp(-mtlData.bsdfData.absorptionCoefficient * nextRayIntersection.t);
#endif
            }
#endif

            rayIntersection.color += value * russianRouletteFactor * nextRayIntersection.color;
        }
    }

#else // HAS_LIGHTLOOP
    rayIntersection.color = (!currentDepth || computeDirect) ? bsdfData.color * GetInverseCurrentExposureMultiplier() + builtinData.emissiveColor : 0.0;
#endif

    // Bias the result (making it too dark), but reduces fireflies a lot
    float intensity = Luminance(rayIntersection.color) * GetCurrentExposureMultiplier();
    if (intensity > _RaytracingIntensityClamp)
        rayIntersection.color *= _RaytracingIntensityClamp / intensity;
}

[shader("anyhit")]
void AnyHit(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    rayIntersection.t = RayTCurrent();
    AcceptHitAndEndSearch();
}
