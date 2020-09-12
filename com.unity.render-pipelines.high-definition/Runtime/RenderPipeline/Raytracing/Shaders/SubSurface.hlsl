#ifndef UNITY_SUB_SURFACE_INCLUDED
#define UNITY_SUB_SURFACE_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/SubSurface/RayTracingIntersectionSubSurface.hlsl"

// Data for the sub-surface walk
struct ScatteringResult
{
    bool hit;
    float3 outputPosition;
    float3 outputNormal;
    float3 outputDirection;
    float3 outputDiffuse;
    float3 outputThroughput;
};

// This function does the remapping from scattering color and distance to sigmaS and sigmaT
void RemapSubSurfaceScatteringParameters(float3 albedo, float3 radius, out float3 sigmaS, out float3 sigmaT)
{
    float3 a = 1.0 - exp(albedo * (-5.09406 + albedo * (2.61188 - albedo * 4.31805)));
    float3 s = 1.9 - albedo + 3.5 * (albedo - 0.8) * (albedo - 0.8);

    sigmaT = 1.0 / max(radius * s, 1e-16);
    sigmaS = sigmaT * a;
}

// This function allows us to pick a color channel
int GetChannel(float u1, float3 channelWeight)
{
    if (channelWeight.x > u1)
        return 0;
    if ((channelWeight.x + channelWeight.y) > u1)
        return 1;
    return 2;
}

// Safe division to avoid nans
float3 SafeDivide(float3 val0, float3 val1)
{
    float3 result;
    result.x = val1.x != 0.0 ? val0.x / val1.x : 0.0;
    result.y = val1.y != 0.0 ? val0.y / val1.y : 0.0;
    result.z = val1.z != 0.0 ? val0.z / val1.z : 0.0;
    return result;
}

void ScatteringWalk(float3 normalWS, float3 diffuseColor, float3 subSurfaceColor, uint2 pixelCoord, int rayCount, float3 positionWS, inout ScatteringResult scatteringResult)
{
    // Initialize our variable
    scatteringResult.outputThroughput = float3(1.0, 1.0, 1.0);
    scatteringResult.hit = false;

    // Remap from our user-friendly parameters to and sigmaS and sigmaT
    float3 sigmaS, sigmaT;
    RemapSubSurfaceScatteringParameters(diffuseColor, subSurfaceColor, sigmaS, sigmaT);

    // Initialize the intersection structure
    RayIntersectionSubSurface internalRayIntersection;
    ZERO_INITIALIZE(RayIntersectionSubSurface, internalRayIntersection);
    internalRayIntersection.pixelCoord = pixelCoord;

    // Initialize the walk parameters
    RayDesc internalRayDesc;
    internalRayDesc.TMin = 0.0;

    int maxWalkSteps = 16;
    int walkIdx = 0;
    float3 currentPathPosition = positionWS;
    float3 transmittance;

    while (!scatteringResult.hit && walkIdx < maxWalkSteps)
    {
        // Samples the random numbers for the direction
        float dir0Rnd = GetBNDSequenceSample(pixelCoord, rayCount,  4 * walkIdx + 0);
        float dir1Rnd = GetBNDSequenceSample(pixelCoord, rayCount,  4 * walkIdx + 1);

        // Samples the random numbers for the distance
        float dstRndSample = GetBNDSequenceSample(pixelCoord, rayCount, 4 * walkIdx + 2);

        // Random number used to do channel selection
        float channelSelection = GetBNDSequenceSample(pixelCoord, rayCount, 4 * walkIdx + 3);

        // Compute the per-channel weight
        float3 weights = scatteringResult.outputThroughput * SafeDivide(sigmaS, sigmaT);

        // Normalize our weights
        float channelSum = weights.x + weights.y + weights.z;
        float3 channelWeight = SafeDivide(weights, channelSum);

        // Evaluate what channel we should be using for this sample
        int channelIdx = GetChannel(channelSelection, channelWeight);

        // Fetch sigmaT
        float currentSigmaT = sigmaT[channelIdx];

        // Evaluate the length of our steps
        internalRayDesc.TMax = -log(1.0f - dstRndSample) / currentSigmaT;

        if (walkIdx != 0)
        {
            internalRayDesc.Direction = normalize(SampleSphereUniform(dir0Rnd, dir1Rnd));
            internalRayDesc.Origin = currentPathPosition;
        }
        else
        {
            // If we just started the walk, the surface is considered back-Lambertian
            internalRayDesc.Direction = normalize(SampleHemisphereCosine(dir0Rnd, dir1Rnd, -normalWS));
            internalRayDesc.Origin = positionWS - normalWS * _RaytracingRayBias;
        }

        // Initialize the intersection data
        internalRayIntersection.t = -1.0;
        internalRayIntersection.outNormal = 0.0;

        // Do the next step
        // TODO: Maybe include only the subsurface meshes.
        TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_FORCE_OPAQUE | RAY_FLAG_CULL_FRONT_FACING_TRIANGLES,
                 RAYTRACINGRENDERERFLAG_OPAQUE, 0, 1, 0, internalRayDesc, internalRayIntersection);

        // Define if we did a hit
        scatteringResult.hit = internalRayIntersection.t > 0.0;

        // How much did the ray travel?
        float t = scatteringResult.hit ? internalRayIntersection.t : internalRayDesc.TMax;

        // Evaluate the transmittance for the current segment
        transmittance = exp(-t * sigmaT);

        // Evaluate the pdf for the current segment
        float pdf = dot((scatteringResult.hit ? transmittance : sigmaT * transmittance), channelWeight);

        // Contribute to the throughput
        scatteringResult.outputThroughput *= SafeDivide(scatteringResult.hit ? transmittance : sigmaS * transmittance, pdf);

        // FIXME: The following multiplication by diffuseColor looks rather fishy...
        // This should probably not be done at all, and definitely not on select path lengths.

        // If we exit right away, the diffuse color is the throughput value
        if (scatteringResult.hit && walkIdx == 0)
            scatteringResult.outputThroughput *= diffuseColor;

        // Compute the next path position
        currentPathPosition = currentPathPosition + internalRayDesc.Direction * t;
        scatteringResult.outputNormal = internalRayIntersection.outNormal;

        // increment the path depth
        walkIdx++;
    }

    // If we did not hit (we could connect) but for the moment we kill the path
    if (!scatteringResult.hit)
        scatteringResult.outputThroughput = float3(0.0, 0.0, 0.0);
    else
    {
        scatteringResult.outputPosition = currentPathPosition;
        scatteringResult.outputDirection = internalRayDesc.Direction;
        scatteringResult.outputDiffuse = internalRayIntersection.outIndirectDiffuse;
    }
}

#endif // UNITY_SUB_SURFACE_INCLUDED
