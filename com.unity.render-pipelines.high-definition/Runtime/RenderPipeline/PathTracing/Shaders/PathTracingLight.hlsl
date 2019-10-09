// How many lights (at most) do we support at one given shading point
#define MAX_LIGHT_COUNT 4

// Supports rect area lights only for the moment
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
    uint begin = 0, end = 0;

    #ifdef USE_LIGHT_CLUSTER
    GetLightCountAndStartCluster(position, LIGHTCATEGORY_AREA, begin, end, list.cellIdx);
    #else
    begin = _PunctualLightCountRT;
    end = _PunctualLightCountRT + _AreaLightCountRT;
    #endif

    for (uint i = begin; i < end && list.count < MAX_LIGHT_COUNT; i++)
    {
        #ifdef USE_LIGHT_CLUSTER
        LightData lightData = FetchClusterLightIndex(list.cellIdx, i);
        #else
        LightData lightData = _LightDatasRT[i];
        #endif

        if (lightData.lightType == GPULIGHTTYPE_RECTANGLE && IsMatchingLightLayer(lightData.lightLayers, builtinData.renderingLayers))
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

bool SampleLights(LightList lightList,
                  float2 inputSample,
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
    LightData lightData = GetLightData(lightList, idx);

    // Rescale the sample we used for further use
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
    pdf = Sq(dist) / (lightArea * cosTheta * lightList.count);

    value = lightData.color;

    return true;
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
