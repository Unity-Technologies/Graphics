// How many lights (at most) do we support at one given shading point
// FIXME: hardcoded limits are evil, this LightList should instead be put together in C#
#define MAX_LOCAL_LIGHT_COUNT 16
#define MAX_DISTANT_LIGHT_COUNT 4

#define DELTA_PDF 1000000.0

// Supports punctual, spot, rect area and directional lights at the moment
struct LightList
{
    uint  localCount;
    uint  localIndex[MAX_LOCAL_LIGHT_COUNT];
    float localWeight;

    uint  distantCount;
    uint  distantIndex[MAX_DISTANT_LIGHT_COUNT];
    float distantWeight;

    #ifdef USE_LIGHT_CLUSTER
    uint  cellIndex;
    #endif
};

LightList CreateLightList(float3 position, uint lightLayers)
{
    LightList list;

    // First take care of local lights (area, point, spot)
    list.localCount = 0;
    uint localCount;

    #ifdef USE_LIGHT_CLUSTER
    GetLightCountAndStartCluster(position, LIGHTCATEGORY_AREA, localCount, localCount, list.cellIndex);
    #else
    localCount = _PunctualLightCountRT + _AreaLightCountRT;
    #endif

    for (uint i = 0; i < localCount && list.localCount < MAX_LOCAL_LIGHT_COUNT; i++)
    {
        #ifdef USE_LIGHT_CLUSTER
        const LightData lightData = FetchClusterLightIndex(list.cellIndex, i);
        #else
        const LightData lightData = _LightDatasRT[i];
        #endif

        if (IsMatchingLightLayer(lightData.lightLayers, lightLayers))
            list.localIndex[list.localCount++] = i;
    }

    // Then filter the active distant lights (directional)
    list.distantCount = 0;

    for (uint i = 0; i < _DirectionalLightCount && list.distantCount < MAX_DISTANT_LIGHT_COUNT; i++)
    {
        if (IsMatchingLightLayer(_DirectionalLightDatas[i].lightLayers, lightLayers))
            list.distantIndex[list.distantCount++] = i;
    }

    // Compute the weights, used for the lights PDF (we split 50/50 between local and distant, if both are present)
    list.localWeight = list.localCount ? (list.distantCount ? 0.5 : 1.0) : 0.0;
    list.distantWeight = list.distantCount ? 1.0 - list.localWeight : 0.0;

    return list;
}

uint GetLightCount(LightList list)
{
    return list.localCount + list.distantCount;
}

LightData GetLocalLightData(LightList list, uint i)
{
    #ifdef USE_LIGHT_CLUSTER
    return FetchClusterLightIndex(list.cellIndex, list.localIndex[i]);
    #else
    return _LightDatasRT[list.localIndex[i]];
    #endif
}

LightData GetLocalLightData(LightList list, float inputSample)
{
    return GetLocalLightData(list, (uint)(inputSample * list.localCount));
}

DirectionalLightData GetDistantLightData(LightList list, uint i)
{
    return _DirectionalLightDatas[list.distantIndex[i]];
}

DirectionalLightData GetDistantLightData(LightList list, float inputSample)
{
    return GetDistantLightData(list, (uint)(inputSample * list.distantCount));
}

float GetLocalLightWeight(LightList list)
{
    return list.localWeight / list.localCount;
}

float GetDistantLightWeight(LightList list)
{
    return list.distantWeight / list.distantCount;
}

bool PickLocalLights(LightList list, inout float sample)
{
    if (sample < list.localWeight)
    {
        // We pick local lighting
        sample /= list.localWeight;
        return true;
    }

    // Otherwise, distant lighting
    sample = (sample - list.localWeight) / list.distantWeight;
    return false;
 }

bool PickDistantLights(LightList list, inout float sample)
{
    return !PickLocalLights(list, sample);
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
    if (!GetLightCount(lightList))
        return false;

    if (PickLocalLights(lightList, inputSample.z))
    {
        // Pick a local light from the list
        LightData lightData = GetLocalLightData(lightList, inputSample.z);

        if (lightData.lightType == GPULIGHTTYPE_RECTANGLE)
        {
            // Generate a point on the surface of the light
            float3 lightCenter = GetAbsolutePositionWS(lightData.positionRWS);
            float3 samplePos = lightCenter + (inputSample.x - 0.5) * lightData.size.x * lightData.right + (inputSample.y - 0.5) * lightData.size.y * lightData.up;

            // And the corresponding direction
            outgoingDir = samplePos - position;
            dist = length(outgoingDir);
            outgoingDir /= dist;

            if (dot(normal, outgoingDir) < 0.001)
                return false;

            float cosTheta = -dot(outgoingDir, lightData.forward);
            if (cosTheta < 0.001)
                return false;

            float lightArea = length(cross(lightData.size.x * lightData.right, lightData.size.y * lightData.up));
            value = lightData.color;
            pdf = GetLocalLightWeight(lightList) * Sq(dist) / (lightArea * cosTheta);
        }
        else // Punctual light
        {
            // Direction from shading point to light position
            outgoingDir = GetAbsolutePositionWS(lightData.positionRWS) - position;
            float sqDist = Length2(outgoingDir);

            if (lightData.size.x > 0.0) // Stores the square radius
            {
                float3x3 localFrame = GetLocalFrame(normalize(outgoingDir));
                SampleCone(inputSample, sqrt(saturate(1.0 - lightData.size.x / sqDist)), outgoingDir, pdf); // computes rcpPdf

                outgoingDir = normalize(outgoingDir.x * localFrame[0] + outgoingDir.y * localFrame[1] + outgoingDir.z * localFrame[2]);

                if (dot(normal, outgoingDir) < 0.001)
                    return false;

                dist = max(sqrt((sqDist - lightData.size.x)), 0.001);
                value = GetPunctualEmission(lightData, outgoingDir, dist) / pdf;
                pdf = GetLocalLightWeight(lightList) / pdf;
            }
            else
            {
                dist = sqrt(sqDist);
                outgoingDir /= dist;

                if (dot(normal, outgoingDir) < 0.001)
                    return false;

                // DELTA_PDF represents 1 / area, where the area is infinitesimal
                value = GetPunctualEmission(lightData, outgoingDir, dist) * DELTA_PDF;
                pdf = GetLocalLightWeight(lightList) * DELTA_PDF;
            }
        }
    }
    else // Distant lights
    {
        // Pick a distant light from the list
        DirectionalLightData lightData = GetDistantLightData(lightList, inputSample.z);

        if (lightData.angularDiameter > 0.0)
        {
            SampleCone(inputSample, cos(lightData.angularDiameter * 0.5), outgoingDir, pdf); // computes rcpPdf
            value = lightData.color / pdf;
            pdf = GetDistantLightWeight(lightList) / pdf;
            outgoingDir = normalize(outgoingDir.x * normalize(lightData.right) + outgoingDir.y * normalize(lightData.up) - outgoingDir.z * lightData.forward);
        }
        else
        {
            outgoingDir = -lightData.forward;
            value = lightData.color * DELTA_PDF;
            pdf = GetDistantLightWeight(lightList) * DELTA_PDF;
        }

        if (dot(normal, outgoingDir) < 0.001)
            return false;

        dist = FLT_INF;
    }

    return any(value);
}

void EvaluateLights(LightList lightList,
                    RayDesc rayDescriptor,
                    out float3 value,
                    out float pdf)
{
    value = 0.0;
    pdf = 0.0;

    // First local lights
    for (uint i = 0; i < lightList.localCount; i++)
    {
        LightData lightData = GetLocalLightData(lightList, i);

        // Punctual lights have a quasi-null probability of being hit here
        if (lightData.lightType != GPULIGHTTYPE_RECTANGLE)
            continue;

        float t = rayDescriptor.TMax;
        float cosTheta = -dot(rayDescriptor.Direction, lightData.forward);
        float3 lightCenter = GetAbsolutePositionWS(lightData.positionRWS);

        // Check if we hit the light plane, at a distance below our tMax (coming from indirect computation)
        if (cosTheta > 0.0 && IntersectPlane(rayDescriptor.Origin, rayDescriptor.Direction, lightCenter, lightData.forward, t))
        {
            if (t < rayDescriptor.TMax)
            {
                float3 hitVec = rayDescriptor.Origin + t * rayDescriptor.Direction - lightCenter;

                // Then check if we are within the rectangle bounds
                if (2.0 * abs(dot(hitVec, lightData.right) / Length2(lightData.right)) < lightData.size.x &&
                    2.0 * abs(dot(hitVec, lightData.up) / Length2(lightData.up)) < lightData.size.y)
                {
                    value += lightData.color;

                    float lightArea = length(cross(lightData.size.x * lightData.right, lightData.size.y * lightData.up));
                    pdf += GetLocalLightWeight(lightList) * Sq(t) / (lightArea * cosTheta);

                    // If we consider that a ray is very unlikely to hit 2 area lights one after another, we can exit the loop
                    break;
                }
            }
        }
    }

    // Then distant lights
    for (uint i = 0; i < lightList.distantCount; i++)
    {
        DirectionalLightData lightData = GetDistantLightData(lightList, i);

        if (lightData.angularDiameter > 0.0 && rayDescriptor.TMax >= FLT_INF)
        {
            float cosHalfAngle = cos(lightData.angularDiameter * 0.5);
            float cosTheta = -dot(rayDescriptor.Direction, lightData.forward);
            if (cosTheta >= cosHalfAngle)
            {
                float rcpPdf = TWO_PI * (1.0 - cosHalfAngle);
                value += lightData.color / rcpPdf;
                pdf += GetDistantLightWeight(lightList) / rcpPdf;
            }
        }
    }
}
