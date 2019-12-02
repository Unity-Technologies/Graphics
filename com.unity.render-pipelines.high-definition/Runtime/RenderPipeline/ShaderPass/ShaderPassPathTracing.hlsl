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

real3 SamplePhaseFunction(real u1, real u2, float g, out float outPDF)
{
    float phi = 2.0 * PI * u2;
    float g2 = g * g;
    float a = (1.0 - g2)/(1.0 - g + 2 * g * u1);
    float cosTheta = (1.0 + g2 - a * a) / (2 * g);

    float b = pow(1 + g2 - 2* g * cosTheta, 3.0/2.0) * 4.0 * PI;
    outPDF = 1 - g2 / b;

    float cosPhi = cos(phi);
    float sinPhi = sin(phi);
    float sinTheta = sqrt(1.0 - cosTheta*cosTheta);
    return float3(sinTheta * cosPhi, sinTheta * sinPhi, cosTheta);
}

// Generic function that handles the reflection code
[shader("closesthit")]
void ClosestHit(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // Always set the new t value
    rayIntersection.t = RayTCurrent();

    // If this is a volumetric flag, we are done
    if (rayIntersection.volFlag == 1)
        return;

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
    rayIntersection.color = computeDirect ? builtinData.emissiveColor * GetCurrentExposureMultiplier() : 0.0;

    // Initialize our material data
    MaterialData mtlData = CreateMaterialData(bsdfData, -WorldRayDirection());

    if (IsBlack(mtlData))
        return;


    // Path traced SSS
    float3 outputVolumetricPosition = float3(0.0, 0.0, 0.0);
    float3 outputVolumetricDirection = float3(0.0, 0.0, 0.0);
    float3 robertDeNiro = float3(1.0, 1.0, 1.0);

    if (IsVolumetric(mtlData))
    {
        // Evaluate the length of our steps
        float3 dist = -log(exp(-bsdfData.scatteringCoeff)) / bsdfData.transmittanceCoeff;
        RayDesc internalRayDesc;
        RayIntersection internalRayIntersection;

        int maxWalkSteps = 16;
        bool hit = false;
        int internalSegment = 0;
        float3 currentPathPosition = position;
        float3 transmittance;
        float3 sampleDir;

        while (!hit && internalSegment < maxWalkSteps)
        {
            // Samples the random numbers for the direction
            float dir0Rnd = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount,  4 * internalSegment + 0);
            float dir1Rnd = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount,  4 * internalSegment + 1);

            // Samples the random numbers for the distance
            float dstRndSample = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * internalSegment + 2);

            // Random number used to do channel selection
            float channelSelection = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * internalSegment + 3);
            
            // Evaluate what channel we should be using for this sample
            int channelIdx = (int) (channelSelection * 3.0);

            // Fetch sigmaT and sigmaS
            float currentSigmaT = bsdfData.transmittanceCoeff[channelIdx];
            float currentSigmaS = bsdfData.scatteringCoeff[channelIdx];

            // Evaluate the length of our steps
            float currentDist = 0.01 * dstRndSample;//dist[channelIdx];

            float samplePDF;
            float3 rayOrigin;
            if (internalSegment != 0)
            {
                #if 0
                sampleDir = SamplePhaseFunction(dir0Rnd, dir1Rnd, bsdfData.phaseCoeff, samplePDF);
                rayOrigin = currentPathPosition;
                #else
                sampleDir = SampleSphereUniform(dir0Rnd, dir1Rnd);
                samplePDF = 2.0 * PI;
                rayOrigin = currentPathPosition;
                #endif
            }
            else
            {
                // If it's the first sample, the surface is considered lambertian
                sampleDir = SampleHemisphereCosine(dir0Rnd, dir1Rnd, -bsdfData.geomNormalWS);
                samplePDF = dot(sampleDir, -bsdfData.geomNormalWS);
                rayOrigin = currentPathPosition - bsdfData.geomNormalWS * 0.001;
            }

            // Now that we have all the info for throwing our ray
            internalRayDesc.Origin = rayOrigin;
            internalRayDesc.Direction = sampleDir;
            internalRayDesc.TMin = 0.0;
            internalRayDesc.TMax = currentDist;

            // Initialize the intersection data
            internalRayIntersection.t = -1.0;
            internalRayIntersection.volFlag = 1;
            
            TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_FORCE_OPAQUE, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 1, internalRayDesc, internalRayIntersection);

            // Define if we did a hit
            hit = internalRayIntersection.t > 0.0;
            // Evalaute the transmittance for the current segment
            transmittance = exp(-currentDist * bsdfData.transmittanceCoeff);

            // Evaluate the pdf for the current segment
            float3 pdf = hit ? transmittance : bsdfData.transmittanceCoeff * transmittance;

            // Contribute to the throughput
            pathThroughput *= (hit ? transmittance : bsdfData.scatteringCoeff * transmittance) / (pdf * samplePDF);
            robertDeNiro *= (hit ? transmittance : bsdfData.scatteringCoeff * transmittance) / (pdf * samplePDF);

            // Compute the next path position
            currentPathPosition = currentPathPosition + sampleDir * (hit ? internalRayIntersection.t : currentDist);

            // increment the path
            internalSegment++;
        }

        outputVolumetricPosition = currentPathPosition;
        outputVolumetricDirection = sampleDir;
    }

    //rayIntersection.color = robertDeNiro;
    //return;
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
                nextRayIntersection.volFlag = 0;
                TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES | RAY_FLAG_FORCE_OPAQUE, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 0, rayDescriptor, nextRayIntersection);

                if (nextRayIntersection.t >= rayDescriptor.TMax)
                {
                    float misWeight = PowerHeuristic(pdf, mtlResult.diffPdf + mtlResult.specPdf);
                    rayIntersection.color += value * misWeight * GetCurrentExposureMultiplier();
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

        float rand = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * currentDepth + 3);
        if (RussianRouletteTest(russianRouletteValue, rand, russianRouletteFactor, !currentDepth))
        {
            if (IsVolumetric(mtlData))
            {
                rayDescriptor.Origin = outputVolumetricPosition + outputVolumetricDirection * _RaytracingRayBias;
                rayDescriptor.Direction = outputVolumetricDirection;
            }
            else
            {
                rayDescriptor.Origin = position + GetPositionBias(bsdfData.geomNormalWS, rayDescriptor.Direction, _RaytracingRayBias);
            }
            rayDescriptor.TMax = FLT_INF;

            // Copy path constants across
            nextRayIntersection.pixelCoord = rayIntersection.pixelCoord;
            nextRayIntersection.rayCount =   rayIntersection.rayCount;
            nextRayIntersection.cone.width = rayIntersection.cone.width;
            nextRayIntersection.volFlag = 0;

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
                nextRayIntersection.color += lightValue * misWeight * GetCurrentExposureMultiplier();
            }
            rayIntersection.color += value * russianRouletteFactor * nextRayIntersection.color;
        }
    }

#else // HAS_LIGHTLOOP
    rayIntersection.color = (!currentDepth || computeDirect) ? builtinData.emissiveColor * GetCurrentExposureMultiplier() : 0.0;
#endif

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
