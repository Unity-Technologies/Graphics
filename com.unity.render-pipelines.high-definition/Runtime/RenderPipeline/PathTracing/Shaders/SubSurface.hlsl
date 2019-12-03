float3 SamplePhaseFunction(real u1, real u2, float g, out float outPDF)
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

void RemapSubSurfaceScatteringParameters(float3 albedo, float radius, out float3 sigmaT, out float3 sigmaS)
{
    const float3 a = 1.0f - exp(albedo * (-5.09406f + albedo * (2.61188f - albedo * 4.31805f)));
    const float3 s = 1.9f - albedo + 3.5f * pow(albedo - 0.8f, 2.0);

    sigmaT = 1.0f / max(radius * s, 1e-16f);
    sigmaS = sigmaT * a;
}

struct ScatteringResult
{
    float3 outputPosition;
    float3 outputDirection;
    float3 outputNormal;
    bool hit;
};

int GetChannel(float u1, float3 channelWeight)
{
    if (channelWeight.x > u1)
    {
        return 0;
    }
    else if ((channelWeight.x + channelWeight.y) > u1)
    {
        return 1;
    }
    else
    {
        return 2;
    }
}

float3 safeDivide(float3 val0, float3 val1)
{
    float3 result;
    result.x = val1.x != 0.0 ? val0.x / val1.x : 0.0;
    result.y = val1.y != 0.0 ? val0.y / val1.y : 0.0;
    result.z = val1.z != 0.0 ? val0.z / val1.z : 0.0;
}

ScatteringResult ScatteringWalk(BSDFData bsdfData, RayIntersection rayIntersection, float3 positionWS, float3 viewWS, inout float3 pathThroughput)
{
    float3 sigmaS;
    float3 sigmaT;
    RemapSubSurfaceScatteringParameters(bsdfData.scatteringCoeff, bsdfData.transmittanceCoeff.x, sigmaT, sigmaS);

    ScatteringResult result;
    result.hit = false;

    // Evaluate the length of our steps
    RayDesc internalRayDesc;
    RayIntersection internalRayIntersection;

    int maxWalkSteps = 256;
    int walkIdx = 0;
    float3 currentPathPosition = positionWS;
    float3 transmittance;
    float3 sampleDir;

    while (!result.hit && walkIdx < maxWalkSteps)
    {
        // Samples the random numbers for the direction
        float dir0Rnd = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount,  4 * walkIdx + 0);
        float dir1Rnd = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount,  4 * walkIdx + 1);

        // Samples the random numbers for the distance
        float dstRndSample = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * walkIdx + 2);

        // Random number used to do channel selection
        float channelSelection = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * walkIdx + 3);
        
        float3 weights = pathThroughput * sigmaS / sigmaT;
        float channelSum = weights.x + weights.y + weights.z;
        float3 channelWeight;
        channelWeight = weights / channelSum;

        // Evaluate what channel we should be using for this sample
        int channelIdx = GetChannel(channelSelection, channelWeight);

        // Fetch sigmaT
        float currentSigmaT = sigmaT[channelIdx];

        // Evaluate the length of our steps
        float currentDist = -log(1.0f - dstRndSample)/currentSigmaT;

        float samplePDF;
        float3 rayOrigin;
        if (walkIdx != 0)
        {
            /*
            #if 0
            sampleDir = SamplePhaseFunction(dir0Rnd, dir1Rnd, bsdfData.phaseCoeff, samplePDF);
            rayOrigin = currentPathPosition;
            #else
            */
            sampleDir = normalize(SampleSphereUniform(dir0Rnd, dir1Rnd));
            samplePDF = 1.0 /(2.0 * PI);
            rayOrigin = currentPathPosition;
            // #endif
        }
        else
        {
            // If it's the first sample, the surface is considered lambertian
            sampleDir = normalize(SampleHemisphereCosine(dir0Rnd, dir1Rnd, -bsdfData.geomNormalWS));
            samplePDF = dot(sampleDir, -bsdfData.geomNormalWS);
            rayOrigin = positionWS - bsdfData.geomNormalWS * 0.0001;
        }

        // Now that we have all the info for throwing our ray
        internalRayDesc.Origin = rayOrigin;
        internalRayDesc.Direction = sampleDir;
        internalRayDesc.TMin = 0.0;
        internalRayDesc.TMax = currentDist;

        // Initialize the intersection data
        internalRayIntersection.t = -1.0;
        internalRayIntersection.rayType = VOLUMETRIC_RAY;
        internalRayIntersection.normal = 0.0;
        
        TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_FORCE_OPAQUE, RAYTRACINGRENDERERFLAG_PATH_TRACING, 0, 1, 0, internalRayDesc, internalRayIntersection);

        // Define if we did a hit
        result.hit = internalRayIntersection.t > 0.0;

        // How much did the ray travel?
        float t = result.hit ? internalRayIntersection.t : currentDist;

        // Evalaute the transmittance for the current segment
        transmittance = exp(-t * sigmaT);

        // Evaluate the pdf for the current segment
        float pdf = dot((result.hit ? transmittance : sigmaT * transmittance), channelWeight);

        // Contribute to the throughput
        pathThroughput *= (result.hit ? transmittance : sigmaS * transmittance) / (pdf);

        // Compute the next path position
        currentPathPosition = currentPathPosition + sampleDir * t;
        result.outputNormal = internalRayIntersection.normal;
        
        // increment the path
        walkIdx++;
    }

    if (!result.hit)
        pathThroughput = float3(0.0, 0.0, 0.0);

    result.outputPosition = currentPathPosition;
    result.outputDirection = sampleDir;
    return result;
}