// How many lights (at most) do we support at one given shading point
// FIXME: hardcoded limit is evil
#define MAX_LIGHT_COUNT 8

#define LARGE_PDF 1000000.0

// Only supports punctual and rect area lights at the moment
struct LightList
{
    uint count;
    uint idx[MAX_LIGHT_COUNT];

    #ifdef USE_LIGHT_CLUSTER
    uint cellIdx;
    #endif
};

LightList CreateLightList(float3 position, BuiltinData builtinData)
{
    LightList list;

    // Initialize count to 0
    list.count = 0;

    // Grab active rect area lights
    uint begin, end = 0;

    #ifdef USE_LIGHT_CLUSTER
    GetLightCountAndStartCluster(position, LIGHTCATEGORY_AREA, begin, end, list.cellIdx);
    #else
    end = _PunctualLightCountRT + _AreaLightCountRT;
    #endif
    begin = 0;

    for (uint i = begin; i < end && list.count < MAX_LIGHT_COUNT; i++)
    {
        #ifdef USE_LIGHT_CLUSTER
        LightData lightData = FetchClusterLightIndex(list.cellIdx, i);
        #else
        LightData lightData = _LightDatasRT[i];
        #endif

        if ((lightData.lightType == GPULIGHTTYPE_POINT || lightData.lightType == GPULIGHTTYPE_SPOT || lightData.lightType == GPULIGHTTYPE_RECTANGLE) &&
            IsMatchingLightLayer(lightData.lightLayers, builtinData.renderingLayers))
            list.idx[list.count++] = i;
    }

    return list;
}

LightData GetLightData(LightList list, uint i)
{
    #ifdef USE_LIGHT_CLUSTER
    return FetchClusterLightIndex(list.cellIdx, list.idx[i]);
    #else
    return _LightDatasRT[list.idx[i]];
    #endif
}

float3 GetPunctualEmission(LightData lightData, float3 outgoingDir, float dist)
{
    float4 distances = float4(dist, Sq(dist), 1.0 / dist, -dist * dot(outgoingDir, lightData.forward));
    return lightData.color * PunctualLightAttenuation(distances, lightData.rangeAttenuationScale, lightData.rangeAttenuationBias, lightData.angleScale, lightData.angleOffset);
}

bool SampleLights(LightList lightList,
                  float3 inputSample,
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
    uint idx = inputSample.z * lightList.count;
    LightData lightData = GetLightData(lightList, idx);

    // Generate a point on the surface of the light
    float3 lightCenter = GetAbsolutePositionWS(lightData.positionRWS);
    float3 samplePos = lightCenter + (inputSample.x - 0.5) * lightData.size.x * lightData.right + (inputSample.y - 0.5) * lightData.size.y * lightData.up;

    // And the corresponding direction
    outgoingDir = samplePos - position;
    dist = length(outgoingDir);
    outgoingDir /= dist;

    if (dot(normal, outgoingDir) < 0.001)
        return false;

    if (lightData.lightType == GPULIGHTTYPE_RECTANGLE)
    {
        float cosTheta = -dot(outgoingDir, lightData.forward);
        if (cosTheta < 0.001)
            return false;

        float lightArea = length(cross(lightData.size.x * lightData.right, lightData.size.y * lightData.up));
        value = lightData.color;
        pdf = Sq(dist) / (lightArea * cosTheta * lightList.count);
    }
    else // Punctual light
    {
        // LARGE_PDF represents 1 / area, where the area is infinitesimal
        value = LARGE_PDF * GetPunctualEmission(lightData, outgoingDir, dist);
        pdf = LARGE_PDF / lightList.count;
    }

    return any(value);
}

void EvaluateLights(LightList lightList,
                    RayDesc rayDescriptor,
                    BuiltinData builtinData,
                    out float3 value,
                    out float pdf)
{
    value = 0.0;
    pdf = 0.0;

    if (!lightList.count)
        return;

    for (uint i = 0; i < lightList.count; i++)
    {
        LightData lightData = GetLightData(lightList, i);

        // Punctual lights have a quasi-null probability of being hit here
        if (lightData.lightType != GPULIGHTTYPE_RECTANGLE)
            continue;

        float t = rayDescriptor.TMax;
        float cosTheta = -dot(rayDescriptor.Direction, lightData.forward);
        float3 lightCenter = GetAbsolutePositionWS(lightData.positionRWS);

        // Check if we hit the light plane, at a distance below our tMax (coming from indirect computation)
        if (cosTheta > 0.0 && IntersectPlane(rayDescriptor.Origin, rayDescriptor.Direction, lightCenter, lightData.forward, t) && t < rayDescriptor.TMax)
        {
            float3 hitVec = rayDescriptor.Origin + t * rayDescriptor.Direction - lightCenter;

            // Then check if we are within the rectangle bounds
            if (2.0 * abs(dot(hitVec, lightData.right) / Length2(lightData.right)) < lightData.size.x &&
                2.0 * abs(dot(hitVec, lightData.up) / Length2(lightData.up)) < lightData.size.y)
            {
                value += lightData.color;

                float lightArea = length(cross(lightData.size.x * lightData.right, lightData.size.y * lightData.up));
                pdf += Sq(t) / (lightArea * cosTheta);
            }
        }
    }

    pdf /= lightList.count;
}
