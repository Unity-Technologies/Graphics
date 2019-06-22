// Ray tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingFragInputs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingSampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/AreaShadows/SphericalQuad.hlsl"

// Path tracing includes
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingCommon.hlsl"

// How many lights (at most) do we support at one given shading point
#define MAX_LIGHT_COUNT 4

// Supports rect area lights only for the moment
class LightList
{
    void init(float3 position, BuiltinData builtinData)
    {
        // Initialize count to 0
        count = 0;

        // Grab active rect area lights
        uint begin = 0, end = 0;

        #ifdef USE_LIGHT_CLUSTER
        GetLightCountAndStartCluster(position, LIGHTCATEGORY_AREA, begin, end, _cellIdx);
        #else
        begin = _PunctualLightCountRT;
        end = _PunctualLightCountRT + _AreaLightCountRT;
        #endif

        for (uint i = begin; i < end && count < MAX_LIGHT_COUNT; i++)
        {
            #ifdef USE_LIGHT_CLUSTER
            LightData lightData = FetchClusterLightIndex(_cellIdx, i);
            #else
            LightData lightData = _LightDatasRT[i];
            #endif

            if (lightData.lightType == GPULIGHTTYPE_RECTANGLE && IsMatchingLightLayer(lightData.lightLayers, builtinData.renderingLayers))
                _idx[count++] = i;
        }
    }

    LightData getLightData(uint i)
    {
        #ifdef USE_LIGHT_CLUSTER
        return FetchClusterLightIndex(_cellIdx, _idx[i]);
        #else
        return _LightDatasRT[_idx[i]];
        #endif
    }

    uint count;
    uint _idx[MAX_LIGHT_COUNT];

    #ifdef USE_LIGHT_CLUSTER
    uint _cellIdx;
    #endif
};

LightList createLightList(float3 position, BuiltinData builtinData)
{
    LightList list;
    list.init(position, builtinData);
    return list;
}

bool sampleLights(float2 inputSample,
                  LightList lightList,
                  float3 position,
                  float3 normal,
              out float3 outgoingDir,
              out float3 value,
              out float pdf,
              out float dist)
{
    if (lightList.count == 0)
        return false;

    // Pick a light from the list
    float scaledSample = inputSample.x * lightList.count;
    uint idx = scaledSample;
    LightData lightData = lightList.getLightData(idx);

    // Rescale the sample
    inputSample.x = scaledSample - idx;

    // Generate a point on the surface of the light
    float3 lightCenter = GetAbsolutePositionWS(lightData.positionRWS);
    float3 samplePos = lightCenter + (inputSample.x - 0.5) * lightData.size.x * lightData.right + (inputSample.y - 0.5) * lightData.size.y * lightData.up;

    // And the corresponding direction
    outgoingDir = samplePos - position;
    dist = length(outgoingDir);
    outgoingDir /= dist;

    if (dot(normal, outgoingDir) < 0.001)
        return false;

    float cosTheta = -dot(outgoingDir, lightData.forward); // FIXME is forward normalized?
    if (cosTheta < 0.001)
        return false;

    float lightArea = length(cross(lightData.size.x * lightData.right, lightData.size.y * lightData.up));
    pdf = sqr(dist) / (lightArea * cosTheta * lightList.count);

    value = lightData.color;

    return true;
}

void evaluateLights(LightList lightList,
                    RayDesc rayDescriptor,
                    BuiltinData builtinData,
                    out float3 value,
                    out float pdf)
{
    value = 0.0;
    pdf = 0.0;

    for (uint i = 0; i < lightList.count; i++)
    {
        LightData lightData = lightList.getLightData(i);

        float t = rayDescriptor.TMax;
        float cosTheta = -dot(rayDescriptor.Direction, lightData.forward);
        float3 lightCenter = GetAbsolutePositionWS(lightData.positionRWS);

        // Check if we hit the light plane, at a distance below our tMax (coming from indirect computation)
        if (cosTheta > 0.0 && IntersectPlane(rayDescriptor.Origin, rayDescriptor.Direction, lightCenter, lightData.forward, t) && t < rayDescriptor.TMax)
        {
            float3 hitVec = rayDescriptor.Origin + t * rayDescriptor.Direction - lightCenter;

            // Then check if we are within the rectangle bounds
            if (2.0 * abs(dot(hitVec, lightData.right) / length2(lightData.right)) < lightData.size.x &&
                2.0 * abs(dot(hitVec, lightData.up) / length2(lightData.up)) < lightData.size.y)
            {
                value += lightData.color;

                float lightArea = length(cross(lightData.size.x * lightData.right, lightData.size.y * lightData.up));
                pdf += sqr(t) / (lightArea * cosTheta);
            }
        }
    }

    pdf /= lightList.count;
}

#define RUSSIAN_ROULETTE_THRESHOLD 0.01

bool russianRouletteTest(float value, float rand, out float factor)
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

float powerHeuristic(float f, float b)
{
    return sqr(f) / (sqr(f) + sqr(b));
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////

bool sampleGGX(float2 inputSample,
               float3x3 localToWorld,
               float3 incomingDir,
               BSDFData bsdfData,
           out float3 outgoingDir,
           out float3 value,
           out float pdf)
{
    float NdotL, NdotH, VdotH;
    SampleGGXDir(inputSample, incomingDir, localToWorld, bsdfData.roughnessT, outgoingDir, NdotL, NdotH, VdotH);

    if (NdotL < 0.001)
        return false;

    float D = D_GGX(NdotH, bsdfData.roughnessT);
    pdf = D * NdotH / (4.0 * VdotH);

    if (pdf < 0.001)
        return false;

    float NdotV = dot(localToWorld[2], incomingDir);
    float3 F = F_Schlick(bsdfData.fresnel0, NdotV);
    float V = V_SmithJointGGX(NdotL, NdotV, bsdfData.roughnessT);

    value = F * D * V * NdotL;

    return true;
}

void evaluateGGX(float3x3 localToWorld,
                 float3 incomingDir,
                 float3 outgoingDir,
                 BSDFData bsdfData,
             out float3 value,
             out float pdf)
{
    float NdotV = dot(localToWorld[2], incomingDir);
    if (NdotV < 0.001)
    {
        value = 0.0;
        pdf = 0.0;
    }
    float NdotL = dot(localToWorld[2], outgoingDir);

    float3 H = normalize(incomingDir + outgoingDir);
    float NdotH = dot(localToWorld[2], H);
    float VdotH = dot(incomingDir, H);
    float D = D_GGX(NdotH, bsdfData.roughnessT);
    pdf = D * NdotH / (4.0 * VdotH);

    float3 F = F_Schlick(bsdfData.fresnel0, NdotV);
    float V = V_SmithJointGGX(NdotL, NdotV, bsdfData.roughnessT);

    value = F * D * V * NdotL;
}

bool sampleLambert(float2 inputSample,
                   float3 normal,
                   BSDFData bsdfData,
               out float3 outgoingDir,
               out float3 value,
               out float pdf )
{
    outgoingDir = SampleHemisphereCosine(inputSample.x, inputSample.y, normal);
    pdf = dot(normal, outgoingDir) * INV_PI;

    if (pdf < 0.001)
        return false;

    value = bsdfData.diffuseColor * pdf;

    return true;
}

void evaluateLambert(float3 normal,
                     float3 outgoingDir,
                     BSDFData bsdfData,
                 out float3 value,
                 out float pdf)
{
    pdf = dot(normal, outgoingDir) * INV_PI;
    value = bsdfData.diffuseColor * pdf;
}

//////////////////////////////////////////////////////////////////////////////////////////////////////////////

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

    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentvertex;
    GetCurrentIntersectionVertex(attributeData, currentvertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentvertex, rayIntersection, fragInput);

    // Let's compute the world space position (the non-camera relative one if camera relative rendering is enabled)
    float3 position = GetAbsolutePositionWS(fragInput.positionRWS);

    // Make sure to add the additional travel distance
    rayIntersection.cone.width += rayIntersection.t * abs(rayIntersection.cone.spreadAngle);

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;

    float3 V = -rayIntersection.incidentDirection;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceDataFromIntersection(fragInput, V, posInput, currentvertex, rayIntersection.cone, surfaceData, builtinData);

    // Compute the bsdf data
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(posInput.positionSS, surfaceData);

    // FIXME: Adjust roughness to reduce fireflies
    bsdfData.roughnessT = max(rayIntersection.maxRoughness, bsdfData.roughnessT);
    bsdfData.roughnessB = max(rayIntersection.maxRoughness, bsdfData.roughnessB);

    // Update depth information
    uint currentDepth = _RaytracingMaxRecursion - rayIntersection.remainingDepth;
    uint indirectRemainingDepth = rayIntersection.remainingDepth - 1;

    // Generate the new sample (following values of the sequence)
    float2 inputSample = 0.0;
    inputSample.x = GetRaytracingNoiseSample(_RaytracingFrameIndex, 2 * currentDepth, rayIntersection.pixelScrambling.x);
    inputSample.y = GetRaytracingNoiseSample(_RaytracingFrameIndex, 2 * currentDepth + 1, rayIntersection.pixelScrambling.y);

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Compute the local frame that matches the normal
    float3x3 localToWorld = GetLocalFrame(surfaceData.normalWS);

    float3 sampleDir, diffValue, specValue;
    float diffPdf, specPdf;

    float NdotV = dot(surfaceData.normalWS, V);

    // If N.V < 0 (can happen with normal mapping) we want to avoid spec sampling
    float specProb = NdotV > 0.001 ? luminance(F_Schlick(bsdfData.fresnel0, NdotV)) : 0.0;
    float diffProb = luminance(bsdfData.diffuseColor);
    float probDenom = diffProb + specProb;

    // If we are basically black, bail out
    if (probDenom < 0.001)
    {
        rayIntersection.color = 0.0;
        return;
    }

    specProb /= probDenom;
    diffProb = 1.0 - specProb;

    if (inputSample.x < specProb)
    {
        // Rescale the sample
        inputSample.x /= specProb;

        if (!sampleGGX(inputSample, localToWorld, V, bsdfData, sampleDir, specValue, specPdf))
        {
            rayIntersection.color = 0.0;
            return;
        }
        evaluateLambert(surfaceData.normalWS, sampleDir, bsdfData, diffValue, diffPdf);
    }
    else
    {
        // Rescale the sample
        inputSample.x = (inputSample.x - specProb) / diffProb;

        if (!sampleLambert(inputSample, surfaceData.normalWS, bsdfData, sampleDir, diffValue, diffPdf))
        {
            rayIntersection.color = 0.0;
            return;
        }
        evaluateGGX(localToWorld, V, sampleDir, bsdfData, specValue, specPdf);
    }

    diffPdf *= diffProb;
    specPdf *= specProb;

    float pdf = diffPdf + specPdf;
    float3 value = (diffValue + specValue) / pdf;

    // Apply Russian roulette to our path
    float3 indirectPassThrough = rayIntersection.color * value;
    float russianRouletteValue = average(indirectPassThrough);
    float russianRouletteFactor = 1.0;

    float rand = GetRaytracingNoiseSample(_RaytracingFrameIndex, 2 * currentDepth + 2, rayIntersection.pixelScrambling.x);
    if (!russianRouletteTest(russianRouletteValue, rand, russianRouletteFactor))
    {
        rayIntersection.color = 0.0;
        return;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    LightList lightList = createLightList(position, builtinData);

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Create the ray descriptor for this sample
    RayDesc rayDescriptor;
    rayDescriptor.Origin = position + surfaceData.normalWS * _RaytracingRayBias;
    rayDescriptor.Direction = sampleDir;
    rayDescriptor.TMin = 0;
    rayDescriptor.TMax = _RaytracingRayMaxLength;

    // Create and init the RayIntersection structure for this sample
    RayIntersection indirectRayIntersection;
    indirectRayIntersection.color = indirectPassThrough * russianRouletteFactor;
    indirectRayIntersection.incidentDirection = rayDescriptor.Direction;
    indirectRayIntersection.origin = rayDescriptor.Origin;
    indirectRayIntersection.remainingDepth = indirectRemainingDepth;
    indirectRayIntersection.t = _RaytracingRayMaxLength;

    // Adjust the max roughness, based on the estimated diff spec ratio
    indirectRayIntersection.pixelScrambling = rayIntersection.pixelScrambling;
    indirectRayIntersection.maxRoughness = (specPdf * max(bsdfData.roughnessT, bsdfData.roughnessB) + diffPdf) / pdf;

    // In order to achieve filtering for the textures, we need to compute the spread angle of the pixel
    indirectRayIntersection.cone.spreadAngle = rayIntersection.cone.spreadAngle + roughnessToSpreadAngle(indirectRayIntersection.maxRoughness);
    indirectRayIntersection.cone.width = rayIntersection.cone.width;

    // Shoot ray for indirect lighting
    TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACING_OPAQUE_FLAG | RAYTRACING_TRANSPARENT_FLAG, 0, 1, 0, rayDescriptor, indirectRayIntersection);

    // Use same ray for direct lighting
    rayDescriptor.TMax = indirectRayIntersection.t + _RaytracingRayBias;
    float3 lightValue;
    float lightPdf;
    evaluateLights(lightList, rayDescriptor, builtinData, lightValue, lightPdf);

    float misWeight = powerHeuristic(pdf, lightPdf);
    rayIntersection.color = value * russianRouletteFactor * (lightValue * misWeight + indirectRayIntersection.color);

    // Light sampling
    if (sampleLights(inputSample, lightList, rayDescriptor.Origin, surfaceData.normalWS, sampleDir, value, pdf, rayDescriptor.TMax))
    {
        evaluateLambert(surfaceData.normalWS, sampleDir, bsdfData, diffValue, diffPdf);
        evaluateGGX(localToWorld, V, sampleDir, bsdfData, specValue, specPdf);
        diffPdf *= diffProb;
        specPdf *= specProb;

        value *= (diffValue + specValue) / pdf;
        if (luminance(value) > 0.001)
        {
            // Shoot a transmission ray (to mark it as such, purposedly set remaining depth to an invalid value)
            indirectRayIntersection.remainingDepth = _RaytracingMaxRecursion + 1;
            rayDescriptor.Direction = sampleDir;
            indirectRayIntersection.t = rayDescriptor.TMax;
            TraceRay(_RaytracingAccelerationStructure, RAY_FLAG_CULL_BACK_FACING_TRIANGLES, RAYTRACING_OPAQUE_FLAG | RAYTRACING_TRANSPARENT_FLAG, 0, 1, 0, rayDescriptor, indirectRayIntersection);

            if (indirectRayIntersection.t >= rayDescriptor.TMax)
            {
                misWeight = powerHeuristic(pdf, diffPdf + specPdf);
                rayIntersection.color += value * misWeight;
            }
        }
    }

    // Bias the result (making it too dark), but reduces fireflies a lot
    float intensity = luminance(rayIntersection.color);
    if (intensity > _RaytracingIntensityClamp)
        rayIntersection.color *= _RaytracingIntensityClamp / intensity;
}

// Generic function that handles the reflection code
[shader("anyhit")]
void AnyHit(inout RayIntersection rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
{
    // The first thing that we should do is grab the intersection vertex
    IntersectionVertex currentvertex;
    GetCurrentIntersectionVertex(attributeData, currentvertex);

    // Build the Frag inputs from the intersection vertex
    FragInputs fragInput;
    BuildFragInputsFromIntersection(currentvertex, rayIntersection, fragInput);

    // Compute the distance of the ray
    rayIntersection.t = RayTCurrent();

    PositionInputs posInput;
    posInput.positionWS = fragInput.positionRWS;
    posInput.positionSS = 0;

    // Build the surfacedata and builtindata
    SurfaceData surfaceData;
    BuiltinData builtinData;
    bool isVisible = GetSurfaceDataFromIntersection(fragInput, -rayIntersection.incidentDirection, posInput, currentvertex, rayIntersection.cone, surfaceData, builtinData);

    // If this fella should be culled, then we cull it
    if (!isVisible)
        IgnoreHit();

    // If the depth information is marked as invalid, we are shooting a transmission ray
    if (rayIntersection.remainingDepth > _RaytracingMaxRecursion)
        AcceptHitAndEndSearch();

}
