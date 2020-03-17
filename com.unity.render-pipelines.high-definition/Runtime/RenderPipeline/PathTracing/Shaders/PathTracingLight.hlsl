// How many lights (at most) do we support at one given shading point
// FIXME: hardcoded limits are evil, this LightList should instead be put together in C#
#define MAX_LOCAL_LIGHT_COUNT 16
#define MAX_DISTANT_LIGHT_COUNT 4

#define DELTA_PDF 1000000.0

// Supports punctual, spot, rect area and directional lights at the moment
struct LightList
{
    uint  localCount;
    uint  localPointCount;
    uint  localIndex[MAX_LOCAL_LIGHT_COUNT];
    float localWeight;

    uint  distantCount;
    uint  distantIndex[MAX_DISTANT_LIGHT_COUNT];
    float distantWeight;

#ifdef USE_LIGHT_CLUSTER
    uint  cellIndex;
#endif
};

bool IsRectAreaLightActive(LightData lightData, float3 position, float3 normal)
{
    float3 lightVec = position - GetAbsolutePositionWS(lightData.positionRWS);

    // Check that the shading position is in front of the light
    float lightCos = dot(lightVec, lightData.forward);
    if (lightCos < 0.0)
        return false;

    // Check that at least part of the light is above the tangent plane
   float lightTangentDist = dot(normal, lightVec);
   if (4.0 * lightTangentDist * abs(lightTangentDist) > Sq(lightData.size.x) + Sq(lightData.size.y))
        return false;

    return true;
}

bool IsPointLightActive(LightData lightData, float3 position, float3 normal)
{
    float3 lightVec = position - GetAbsolutePositionWS(lightData.positionRWS);

    // Check that at least part of the light is above the tangent plane
    float lightTangentDist = dot(normal, lightVec);
    if (lightTangentDist * abs(lightTangentDist) > lightData.size.x)
        return false;

    // Offset the light position towards the back, to account for the radius,
    // then check whether we are still within the dilated cone angle
    float sqSinAngle = 1.0 - Sq(lightData.angleOffset / lightData.angleScale);
    float3 lightOffset = sqrt(lightData.size.x / sqSinAngle) * lightData.forward;
    float lightCos = dot(normalize(lightVec + lightOffset), lightData.forward);

    return lightCos * lightData.angleScale + lightData.angleOffset > 0.0;
}

bool IsDistantLightActive(DirectionalLightData lightData, float3 normal)
{
    return dot(normal, lightData.forward) < sin(lightData.angularDiameter * 0.5);
}

LightList CreateLightList(float3 position, float3 normal, uint lightLayers)
{
    LightList list;

    // First take care of local lights (point, area)
    uint i, localPointCount, localCount;

#ifdef USE_LIGHT_CLUSTER
    list.cellIndex = GetClusterCellIndex(position);
    localPointCount = GetPunctualLightClusterCellCount(list.cellIndex);
    localCount = GetAreaLightClusterCellCount(list.cellIndex);
#else
    localPointCount = _PunctualLightCountRT;
    localCount = _PunctualLightCountRT + _AreaLightCountRT;
#endif

    // First point lights (including spot lights)
    for (list.localPointCount = 0, i = 0; i < localPointCount && list.localPointCount < MAX_LOCAL_LIGHT_COUNT; i++)
    {
#ifdef USE_LIGHT_CLUSTER
        const LightData lightData = FetchClusterLightIndex(list.cellIndex, i);
#else
        const LightData lightData = _LightDatasRT[i];
#endif

        if (IsMatchingLightLayer(lightData.lightLayers, lightLayers)
#ifndef _SURFACE_TYPE_TRANSPARENT
            && IsPointLightActive(lightData, position, normal)
#endif
            )
            list.localIndex[list.localPointCount++] = i;
    }

    // Then rect area lights
    for (list.localCount = list.localPointCount; i < localCount && list.localCount < MAX_LOCAL_LIGHT_COUNT; i++)
    {
#ifdef USE_LIGHT_CLUSTER
        const LightData lightData = FetchClusterLightIndex(list.cellIndex, i);
#else
        const LightData lightData = _LightDatasRT[i];
#endif

        if (IsMatchingLightLayer(lightData.lightLayers, lightLayers)
#ifndef _SURFACE_TYPE_TRANSPARENT
            && IsRectAreaLightActive(lightData, position, normal)
#endif
            )
            list.localIndex[list.localCount++] = i;
    }

    // Then filter the active distant lights (directional)
    list.distantCount = 0;

    for (i = 0; i < _DirectionalLightCount && list.distantCount < MAX_DISTANT_LIGHT_COUNT; i++)
    {
        if (IsMatchingLightLayer(_DirectionalLightDatas[i].lightLayers, lightLayers)
#ifndef _SURFACE_TYPE_TRANSPARENT
            && IsDistantLightActive(_DirectionalLightDatas[i], normal)
#endif
            )
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
    float3 emission = lightData.color;

    // Punctual attenuation
    float4 distances = float4(dist, Sq(dist), rcp(dist), -dist * dot(outgoingDir, lightData.forward));
    emission *= PunctualLightAttenuation(distances, lightData.rangeAttenuationScale, lightData.rangeAttenuationBias, lightData.angleScale, lightData.angleOffset);

    // Cookie
    if (lightData.cookieMode != COOKIEMODE_NONE)
    {
        LightLoopContext context;
        emission *= EvaluateCookie_Punctual(context, lightData, -dist * outgoingDir);
    }

    return emission;
}

float3 GetDirectionalEmission(DirectionalLightData lightData, float3 outgoingVec)
{
    float3 emission = lightData.color;

    // Cookie
    if (lightData.cookieMode != COOKIEMODE_NONE)
    {
        LightLoopContext context;
        emission *= EvaluateCookie_Directional(context, lightData, -outgoingVec);
    }

    return emission;
}

float3 GetAreaEmission(LightData lightData, float centerU, float centerV, float sqDist)
{
    float3 emission = lightData.color;

    // Cookie
    if (lightData.cookieMode != COOKIEMODE_NONE)
    {
        float2 uv = float2(0.5 - centerU, 0.5 + centerV);
        emission *= SampleCookie2D(uv, lightData.cookieScaleOffset);
    }

    // Range windowing (see LightLoop.cs to understand why it is written this way)
    if (lightData.rangeAttenuationBias == 1.0)
        emission *= SmoothDistanceWindowing(sqDist, rcp(Sq(lightData.range)), lightData.rangeAttenuationBias) ;

    return emission;
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
            float centerU = inputSample.x - 0.5;
            float centerV = inputSample.y - 0.5;
            float3 lightCenter = GetAbsolutePositionWS(lightData.positionRWS);
            float3 samplePos = lightCenter + centerU * lightData.size.x * lightData.right + centerV * lightData.size.y * lightData.up;

            // And the corresponding direction
            outgoingDir = samplePos - position;
            float sqDist = Length2(outgoingDir);
            dist = sqrt(sqDist);
            outgoingDir /= dist;

            if (dot(normal, outgoingDir) < 0.001)
                return false;

            float cosTheta = -dot(outgoingDir, lightData.forward);
            if (cosTheta < 0.001)
                return false;

            float lightArea = length(cross(lightData.size.x * lightData.right, lightData.size.y * lightData.up));

            value = GetAreaEmission(lightData, centerU, centerV, sqDist);
            pdf = GetLocalLightWeight(lightList) * sqDist / (lightArea * cosTheta);
        }
        else // Punctual light
        {
            // Direction from shading point to light position
            outgoingDir = GetAbsolutePositionWS(lightData.positionRWS) - position;
            float sqDist = Length2(outgoingDir);

            if (lightData.size.x > 0.0) // Stores the square radius
            {
                float3x3 localFrame = GetLocalFrame(normalize(outgoingDir));
                SampleCone(inputSample.xy, sqrt(saturate(1.0 - lightData.size.x / sqDist)), outgoingDir, pdf); // computes rcpPdf

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

        // The position-to-light unnormalized vector is used for cookie evaluation
        float3 OutgoingVec = GetAbsolutePositionWS(lightData.positionRWS) - position;

        if (lightData.angularDiameter > 0.0)
        {
            SampleCone(inputSample.xy, cos(lightData.angularDiameter * 0.5), outgoingDir, pdf); // computes rcpPdf
            value = GetDirectionalEmission(lightData, OutgoingVec) / pdf;
            pdf = GetDistantLightWeight(lightList) / pdf;
            outgoingDir = normalize(outgoingDir.x * normalize(lightData.right) + outgoingDir.y * normalize(lightData.up) - outgoingDir.z * lightData.forward);
        }
        else
        {
            value = GetDirectionalEmission(lightData, OutgoingVec) * DELTA_PDF;
            pdf = GetDistantLightWeight(lightList) * DELTA_PDF;
            outgoingDir = -lightData.forward;
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
    uint i;

    // First local lights (area lights only, as we consider the probability of hitting a point light neglectable)
    for (i = lightList.localPointCount; i < lightList.localCount; i++)
    {
        LightData lightData = GetLocalLightData(lightList, i);

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
                float centerU = dot(hitVec, lightData.right) / (lightData.size.x * Length2(lightData.right));
                float centerV = dot(hitVec, lightData.up) / (lightData.size.y * Length2(lightData.up));
                if (abs(centerU) < 0.5 && abs(centerV) < 0.5)
                {
                    float t2 = Sq(t);
                    value += GetAreaEmission(lightData, centerU, centerV, t2);

                    float lightArea = length(cross(lightData.size.x * lightData.right, lightData.size.y * lightData.up));
                    pdf += GetLocalLightWeight(lightList) * t2 / (lightArea * cosTheta);

                    // If we consider that a ray is very unlikely to hit 2 area lights one after another, we can exit the loop
                    break;
                }
            }
        }
    }

    // Then distant lights
    for (i = 0; i < lightList.distantCount; i++)
    {
        DirectionalLightData lightData = GetDistantLightData(lightList, i);

        if (lightData.angularDiameter > 0.0 && rayDescriptor.TMax >= FLT_INF)
        {
            float cosHalfAngle = cos(lightData.angularDiameter * 0.5);
            float cosTheta = -dot(rayDescriptor.Direction, lightData.forward);
            if (cosTheta >= cosHalfAngle)
            {
                float rcpPdf = TWO_PI * (1.0 - cosHalfAngle);
                value += GetDirectionalEmission(lightData, rayDescriptor.Direction) / rcpPdf;
                pdf += GetDistantLightWeight(lightList) / rcpPdf;
            }
        }
    }
}
