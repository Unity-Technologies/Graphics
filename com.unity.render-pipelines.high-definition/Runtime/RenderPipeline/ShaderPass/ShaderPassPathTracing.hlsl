// Ray tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Shadows/SphericalQuad.hlsl"

// Path tracing includes
#ifdef HAS_LIGHTLOOP
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingSampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingLight.hlsl"
#endif

#define RUSSIAN_ROULETTE_THRESHOLD 0.5

bool RussianRouletteTest(float value, float rand, out float factor)
{
    if (value >= RUSSIAN_ROULETTE_THRESHOLD)
    {
        factor = 1.0;
        return true;
    }

    if (rand * RUSSIAN_ROULETTE_THRESHOLD >= value)
        return false;

    factor = RUSSIAN_ROULETTE_THRESHOLD / value;
    return true;
}

float PowerHeuristic(float f, float b)
{
    return Sq(f) / (Sq(f) + Sq(b));
}

void transferPathConstants(RayIntersection input, out RayIntersection output)
{
    output.pixelCoord = input.pixelCoord;
    output.rayCount = input.rayCount;
    output.cone.width = input.cone.width;
}

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHit(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // Always set the new t value
    rayIntersection.t = RayTCurrent();

    // If the max depth has been reached (or remaining depth is supsiciously large), bail out
    if (rayIntersection.remainingDepth == 0 || rayIntersection.remainingDepth > _RaytracingMaxRecursion)
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
    float3 position = GetAbsolutePositionWS(fragInput.positionRWS);

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
    GetSurfaceDataFromIntersection(fragInput, -WorldRayDirection(), posInput, currentVertex, rayIntersection.cone, surfaceData, builtinData);

    // Check if we want to compute direct and emissive lighting for current depth
    bool computeDirect = currentDepth >= _RaytracingMinRecursion - 1;

#ifdef HAS_LIGHTLOOP

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

    // FIXME: Adjust roughness to reduce fireflies
    bsdfData.roughnessT = max(rayIntersection.maxRoughness, bsdfData.roughnessT);
    bsdfData.roughnessB = max(rayIntersection.maxRoughness, bsdfData.roughnessB);

    // Generate the new sample (following values of the sequence)
    float3 inputSample = 0.0;
    inputSample.x = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * currentDepth);
    inputSample.y = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * currentDepth + 1);
    inputSample.z = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * currentDepth + 2);

    // Get current path throughput
    float3 pathThroughput = rayIntersection.color;

    // And reset the ray intersection color, which will store our final result
    rayIntersection.color = computeDirect ? builtinData.emissiveColor : 0.0;

    // Initialize our material
    Material mtl = CreateMaterial(bsdfData, -WorldRayDirection());

    if (IsBlack(mtl))
        return;

    // Create the list of active lights
    LightList lightList = CreateLightList(position, builtinData.renderingLayers);

    // Bunch of variables common to material and light sampling
    float pdf;
    float3 value;
    MaterialResult mtlResult;

    RayDesc rayDescriptor;
    rayDescriptor.Origin = position + bsdfData.normalWS * _RaytracingRayBias;
    rayDescriptor.TMin = 0;

    RayIntersection nextRayIntersection;

    // Light sampling
    if (computeDirect && SampleLights(lightList, inputSample, rayDescriptor.Origin, bsdfData.normalWS, rayDescriptor.Direction, value, pdf, rayDescriptor.TMax))
    {
        EvaluateMaterial(mtl, rayDescriptor.Direction, mtlResult);

        value *= (mtlResult.diffValue + mtlResult.specValue) / pdf;
        if (Luminance(value) > 0.001)
        {
            // Shoot a transmission ray (to mark it as such, purposedly set remaining depth to an invalid value)
            nextRayIntersection.remainingDepth = _RaytracingMaxRecursion + 1;
            rayDescriptor.TMax -= _RaytracingRayBias;
            nextRayIntersection.t = rayDescriptor.TMax;
            TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES | RAY_FLAG_FORCE_OPAQUE, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 0, rayDescriptor, nextRayIntersection);

            if (nextRayIntersection.t >= rayDescriptor.TMax)
            {
                float misWeight = PowerHeuristic(pdf, mtlResult.diffPdf + mtlResult.specPdf);
                rayIntersection.color += value * misWeight;
            }
        }
    }

    // Material sampling
    if (SampleMaterial(mtl, inputSample, rayDescriptor.Direction, mtlResult))
    {
        // Compute overall material value and pdf
        pdf = mtlResult.diffPdf + mtlResult.specPdf;
        value = (mtlResult.diffValue + mtlResult.specValue) / pdf;

        // Apply Russian roulette to our path
        pathThroughput *= value;
        float russianRouletteValue = Luminance(pathThroughput);
        float russianRouletteFactor = 1.0;

        float rand = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * currentDepth + 3);
        if (!currentDepth || RussianRouletteTest(russianRouletteValue, rand, russianRouletteFactor))
        {
            rayDescriptor.TMax = FLT_INF;

            // Copy path constants across
            transferPathConstants(rayIntersection, nextRayIntersection);

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

            rayIntersection.color += value * russianRouletteFactor * nextRayIntersection.color;
        }
    }

#else // HAS_LIGHTLOOP
    rayIntersection.color = !currentDepth || computeDirect ? builtinData.emissiveColor : 0.0;
#endif

    // Apply exposure modifier to our path result
    rayIntersection.color *= GetCurrentExposureMultiplier();

    // Bias the result (making it too dark), but reduces fireflies a lot
    float intensity = Luminance(rayIntersection.color);
    if (intensity > _RaytracingIntensityClamp)
        rayIntersection.color *= _RaytracingIntensityClamp / intensity;
}

// Handles fully transparent objects (not called if RAY_FLAG_FORCE_OPAQUE is set)
[shader("anyhit")]
void AnyHit(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentVertex;
    GetCurrentIntersectionVertex(attributeData, currentVertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentVertex, WorldRayDirection(), fragInput);

    // Compute the distance of the ray
    rayIntersection.t = RayTCurrent();

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = 0;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible = GetSurfaceDataFromIntersection(fragInput, -WorldRayDirection(), posInput, currentVertex, rayIntersection.cone, surfaceData, builtinData);

    // If this fella should be culled, then we cull it
    if (!isVisible)
        IgnoreHit();

    // If the depth information is marked as invalid, we are shooting a transmission ray
    if (rayIntersection.remainingDepth > _RaytracingMaxRecursion)
        AcceptHitAndEndSearch();
}
