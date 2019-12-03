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

ScatteringResult ScatteringWalk(MaterialData mtlData, RayIntersection rayIntersection, float3 positionWS, inout float3 pathThroughput)
{
    float3 sigmaS;
    float3 sigmaT;
    RemapSubSurfaceScatteringParameters(mtlData.bsdfData.scatteringCoeff, mtlData.bsdfData.transmittanceCoeff.x, sigmaT, sigmaS);

    ScatteringResult result;
    result.hit = false;

    if (IsVolumetric(mtlData) && IsAbove(mtlData))
    {
        // Evaluate the length of our steps
        RayDesc internalRayDesc;
        RayIntersection internalRayIntersection;

        int maxWalkSteps = 64;
        int internalSegment = 0;
        float3 currentPathPosition = positionWS;
        float3 transmittance;
        float3 sampleDir;

        while (!result.hit && internalSegment < maxWalkSteps)
        {
            // Samples the random numbers for the direction
            float dir0Rnd = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount,  4 * internalSegment + 0);
            float dir1Rnd = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount,  4 * internalSegment + 1);

            // Samples the random numbers for the distance
            float dstRndSample = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * internalSegment + 2);

            // Random number used to do channel selection
            float channelSelection = GetSample(rayIntersection.pixelCoord, rayIntersection.rayCount, 4 * internalSegment + 3);
            
            // Evaluate what channel we should be using for this sample
            int channelIdx = (int)floor(channelSelection * 3.0);

            // Fetch sigmaT and sigmaS
            float currentSigmaT = sigmaT[channelIdx];

            // Evaluate the length of our steps
            float currentDist = -log(1.0f - dstRndSample)/currentSigmaT; //0.001 * dstRndSample;//dist[channelIdx];

            float samplePDF;
            float3 rayOrigin;
            if (internalSegment != 0)
            {
                /*
                #if 0
                sampleDir = SamplePhaseFunction(dir0Rnd, dir1Rnd, bsdfData.phaseCoeff, samplePDF);
                rayOrigin = currentPathPosition;
                #else
                */
                sampleDir = normalize(SampleSphereUniform(dir0Rnd, dir1Rnd));
                samplePDF = 2.0 * PI;
                rayOrigin = currentPathPosition;
                // #endif
            }
            else
            {
                // If it's the first sample, the surface is considered lambertian
                sampleDir = normalize(SampleHemisphereCosine(dir0Rnd, dir1Rnd, -mtlData.bsdfData.geomNormalWS));
                samplePDF = dot(sampleDir, -mtlData.bsdfData.geomNormalWS);
                rayOrigin = positionWS - mtlData.bsdfData.geomNormalWS * 0.001;
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
            result.hit = internalRayIntersection.t > 0.0;
            // Evalaute the transmittance for the current segment
            transmittance = exp(-currentDist * sigmaT);

            // Evaluate the pdf for the current segment
            float pdf = (result.hit ? transmittance : sigmaT * transmittance)[channelIdx];

            // Contribute to the throughput
            pathThroughput *= (result.hit ? transmittance : sigmaS * transmittance) / (pdf);

            // Compute the next path position
            currentPathPosition = currentPathPosition + sampleDir * (result.hit ? internalRayIntersection.t: currentDist);
            result.outputNormal = internalRayIntersection.incidentDirection;
            if (result.hit)
                break;
            // increment the path
            internalSegment++;
        }

        if (!result.hit)
            pathThroughput = float3(0.0, 0.0, 0.0);

        result.outputPosition = currentPathPosition;
        result.outputDirection = sampleDir;
    }
    return result;
}