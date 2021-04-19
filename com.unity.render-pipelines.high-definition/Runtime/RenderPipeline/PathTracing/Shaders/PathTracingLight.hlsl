#ifndef UNITY_PATH_TRACING_LIGHT_INCLUDED
#define UNITY_PATH_TRACING_LIGHT_INCLUDED

// This is just because it need to be defined, shadow maps are not used.
#define SHADOW_LOW

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/CookieSampling.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightLoop/LightLoopDef.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/LightEvaluation.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Shadows/SphericalQuad.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Common/AtmosphericScatteringRayTracing.hlsl"

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
    float3 lightToPosition = position - lightData.positionRWS;

#ifndef USE_LIGHT_CLUSTER
    // Check light range first
    if (Length2(lightToPosition) > Sq(lightData.range))
        return false;
#endif

    // Check that the shading position is in front of the light
    float lightCos = dot(lightToPosition, lightData.forward);
    if (lightCos < 0.0)
        return false;

    // Check that at least part of the light is above the tangent plane
   float lightTangentDist = dot(normal, lightToPosition);
   if (4.0 * lightTangentDist * abs(lightTangentDist) > Sq(lightData.size.x) + Sq(lightData.size.y))
        return false;

    return true;
}

bool IsPointLightActive(LightData lightData, float3 position, float3 normal)
{
    float3 lightToPosition = position - lightData.positionRWS;

#ifndef USE_LIGHT_CLUSTER
    // Check light range first
    if (Length2(lightToPosition) > Sq(lightData.range))
        return false;
#endif

    // Check that at least part of the light is above the tangent plane
    float lightTangentDist = dot(normal, lightToPosition);
    if (lightTangentDist * abs(lightTangentDist) > lightData.size.x)
        return false;

    // If this is an omni-directional point light, we're done
    if (lightData.lightType == GPULIGHTTYPE_POINT)
        return true;

    // Check that we are on the right side of the light plane
    float z = dot(lightToPosition, lightData.forward);
    if (z < 0.0)
        return false;

    if (lightData.lightType == GPULIGHTTYPE_SPOT)
    {
        // Offset the light position towards the back, to account for the radius,
        // then check whether we are still within the dilated cone angle
        float sinTheta2 = 1.0 - Sq(lightData.angleOffset / lightData.angleScale);
        float3 lightRadiusOffset = sqrt(lightData.size.x / sinTheta2) * lightData.forward;
        float lightCos = dot(normalize(lightToPosition + lightRadiusOffset), lightData.forward);

        return lightCos * lightData.angleScale + lightData.angleOffset > 0.0;
    }

    // Our light type is either BOX or PYRAMID
    float x = abs(dot(lightToPosition, lightData.right));
    float y = abs(dot(lightToPosition, lightData.up));

    return (lightData.lightType == GPULIGHTTYPE_PROJECTOR_BOX) ?
        x < 1.0 && y < 1.0 : // BOX
        x < z   && y < z;    // PYRAMID
}

bool IsDistantLightActive(DirectionalLightData lightData, float3 normal)
{
    return dot(normal, lightData.forward) <= sin(lightData.angularDiameter * 0.5);
}

LightList CreateLightList(float3 position, float3 normal, uint lightLayers, bool withLocal = true, bool withDistant = true)
{
    LightList list;
    uint i;

    // First take care of local lights (point, area)
    list.localCount = 0;
    list.localPointCount = 0;

if (withLocal)
{
    uint localPointCount, localCount;

#ifdef USE_LIGHT_CLUSTER
    if (PointInsideCluster(position))
    {
        list.cellIndex = GetClusterCellIndex(position);
        localPointCount = GetPunctualLightClusterCellCount(list.cellIndex);
        localCount = GetAreaLightClusterCellCount(list.cellIndex);
    }
    else
    {
        localPointCount = 0;
        localCount = 0;
    }
#else
    localPointCount = _PunctualLightCountRT;
    localCount = _PunctualLightCountRT + _AreaLightCountRT;
#endif

    // First point lights (including spot lights)
    for (i = 0; i < localPointCount && list.localPointCount < MAX_LOCAL_LIGHT_COUNT; i++)
    {
#ifdef USE_LIGHT_CLUSTER
        const LightData lightData = FetchClusterLightIndex(list.cellIndex, i);
#else
        const LightData lightData = _LightDatasRT[i];
#endif

        if (IsMatchingLightLayer(lightData.lightLayers, lightLayers) && IsPointLightActive(lightData, position, normal))
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

        if (IsMatchingLightLayer(lightData.lightLayers, lightLayers) && IsRectAreaLightActive(lightData, position, normal))
            list.localIndex[list.localCount++] = i;
    }
}

    // Then filter the active distant lights (directional)
    list.distantCount = 0;

if (withDistant)
{
    for (i = 0; i < _DirectionalLightCount && list.distantCount < MAX_DISTANT_LIGHT_COUNT; i++)
    {
        if (IsMatchingLightLayer(_DirectionalLightDatas[i].lightLayers, lightLayers) && IsDistantLightActive(_DirectionalLightDatas[i], normal))
            list.distantIndex[list.distantCount++] = i;
    }
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

bool PickLocalLights(LightList list, inout float theSample)
{
    if (theSample < list.localWeight)
    {
        // We pick local lighting
        theSample /= list.localWeight;
        return true;
    }

    // Otherwise, distant lighting
    theSample = (theSample - list.localWeight) / list.distantWeight;
    return false;
 }

bool PickDistantLights(LightList list, inout float theSample)
{
    return !PickLocalLights(list, theSample);
}

float3 GetPunctualEmission(LightData lightData, float3 outgoingDir, float dist)
{
    float3 emission = lightData.color;

    // Punctual attenuation
    float4 distances = float4(dist, Sq(dist), rcp(dist), -dist * dot(outgoingDir, lightData.forward));
    emission *= PunctualLightAttenuation(distances, lightData.rangeAttenuationScale, lightData.rangeAttenuationBias, lightData.angleScale, lightData.angleOffset);

#ifndef LIGHT_EVALUATION_NO_COOKIE
    if (lightData.cookieMode != COOKIEMODE_NONE)
    {
        LightLoopContext context;
        emission *= EvaluateCookie_Punctual(context, lightData, -dist * outgoingDir);
    }
#endif

    return emission;
}

float3 GetDirectionalEmission(DirectionalLightData lightData, float3 outgoingVec)
{
    float3 emission = lightData.color;

#ifndef LIGHT_EVALUATION_NO_COOKIE
    if (lightData.cookieMode != COOKIEMODE_NONE)
    {
        LightLoopContext context;
        emission *= EvaluateCookie_Directional(context, lightData, -outgoingVec);
    }
#endif

    return emission;
}

float3 GetAreaEmission(LightData lightData, float centerU, float centerV, float sqDist)
{
    float3 emission = lightData.color;

    // Range windowing (see LightLoop.cs to understand why it is written this way)
    if (lightData.rangeAttenuationBias == 1.0)
        emission *= SmoothDistanceWindowing(sqDist, rcp(Sq(lightData.range)), lightData.rangeAttenuationBias);

#ifndef LIGHT_EVALUATION_NO_COOKIE
    if (lightData.cookieMode != COOKIEMODE_NONE)
    {
        float2 uv = float2(0.5 - centerU, 0.5 + centerV);
        emission *= SampleCookie2D(uv, lightData.cookieScaleOffset);
    }
#endif

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

    // Are we lighting a volume or a surface?
    bool isVolume = !any(normal);

    if (PickLocalLights(lightList, inputSample.z))
    {
        // Pick a local light from the list
        LightData lightData = GetLocalLightData(lightList, inputSample.z);

        if (lightData.lightType == GPULIGHTTYPE_RECTANGLE)
        {
            // Generate a point on the surface of the light
            float centerU = inputSample.x - 0.5;
            float centerV = inputSample.y - 0.5;
            float3 lightCenter = lightData.positionRWS;
            float3 samplePos = lightCenter + centerU * lightData.size.x * lightData.right + centerV * lightData.size.y * lightData.up;

            // And the corresponding direction
            outgoingDir = samplePos - position;
            float sqDist = Length2(outgoingDir);
            dist = sqrt(sqDist);
            outgoingDir /= dist;

            if (!isVolume && dot(normal, outgoingDir) < 0.001)
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
            outgoingDir = lightData.positionRWS - position;
            float sqDist = Length2(outgoingDir);
            dist = sqrt(sqDist);
            outgoingDir /= dist;

            if (lightData.size.x > 0.0) // Stores the square radius
            {
                float3x3 localFrame = GetLocalFrame(outgoingDir);
                SampleCone(inputSample.xy, sqrt(1.0 / (1.0 + lightData.size.x / sqDist)), outgoingDir, pdf); // computes rcpPdf

                outgoingDir = outgoingDir.x * localFrame[0] + outgoingDir.y * localFrame[1] + outgoingDir.z * localFrame[2];
                pdf = min(rcp(pdf), DELTA_PDF);
            }
            else
            {
                // DELTA_PDF represents 1 / area, where the area is infinitesimal
                pdf = DELTA_PDF;
            }

            if (!isVolume && dot(normal, outgoingDir) < 0.001)
                return false;

            value = GetPunctualEmission(lightData, outgoingDir, dist) * pdf;
            pdf = GetLocalLightWeight(lightList) * pdf;
        }

        if (isVolume)
            value *= lightData.volumetricLightDimmer;

#ifndef LIGHT_EVALUATION_NO_HEIGHT_FOG
        ApplyFogAttenuation(position, outgoingDir, dist, value);
#endif
    }
    else // Distant lights
    {
        // Pick a distant light from the list
        DirectionalLightData lightData = GetDistantLightData(lightList, inputSample.z);

        // The position-to-light unnormalized vector is used for cookie evaluation
        float3 OutgoingVec = lightData.positionRWS - position;

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

        if (!isVolume && (dot(normal, outgoingDir) < 0.001))
            return false;

        dist = FLT_INF;

        if (isVolume)
            value *= lightData.volumetricLightDimmer;

#ifndef LIGHT_EVALUATION_NO_HEIGHT_FOG
        ApplyFogAttenuation(position, outgoingDir, value);
#endif
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
        float3 lightCenter = lightData.positionRWS;

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
                    float3 lightValue = GetAreaEmission(lightData, centerU, centerV, t2);
#ifndef LIGHT_EVALUATION_NO_HEIGHT_FOG
                    ApplyFogAttenuation(rayDescriptor.Origin, rayDescriptor.Direction, t, lightValue);
#endif
                    value += lightValue;

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
                float3 lightValue = GetDirectionalEmission(lightData, rayDescriptor.Direction);
#ifndef LIGHT_EVALUATION_NO_HEIGHT_FOG
                ApplyFogAttenuation(rayDescriptor.Origin, rayDescriptor.Direction, lightValue);
#endif
                float rcpPdf = TWO_PI * (1.0 - cosHalfAngle);
                value += lightValue / rcpPdf;
                pdf += GetDistantLightWeight(lightList) / rcpPdf;
            }
        }
    }
}

// Functions used by volumetric sampling

bool GetSphereInterval(float3 lightToRayOrigin, float radius, float3 rayDirection, out float tMin, out float tMax)
{
    // We consider Direction to be normalized => a = 1
    float b = 2.0 * dot(rayDirection, lightToRayOrigin);
    float c = Length2(lightToRayOrigin) - Sq(radius);

    float2 t;
    if (!SolveQuadraticEquation(1.0, b, c, t))
        return false;

    tMin = max(t.x, 0.0);
    tMax = max(t.y, 0.0);

    return tMin < tMax;
}

bool GetRectAreaLightInterval(LightData lightData, float3 rayOrigin, float3 rayDirection, out float tMin, out float tMax)
{
    if (lightData.volumetricLightDimmer < 0.001)
        return false;

    float3 lightToRayOrigin = rayOrigin - lightData.positionRWS;

    if (!GetSphereInterval(lightToRayOrigin, lightData.range, rayDirection, tMin, tMax))
        return false;

    float LdotD = dot(lightData.forward, rayDirection);
    float t = -dot(lightData.forward, lightToRayOrigin) / LdotD;
    if (LdotD > 0.0)
        tMin = max(tMin, t);
    else
        tMax = min(tMax, t);

    return tMin < tMax;
}

void Sort(inout float x, inout float y)
{
    if (x > y)
    {
        float tmp = x;
        x = y;
        y = tmp;
    }
}

void GetFrontInterval(float oz, float dz, float t1, float t2, inout float tMin, inout float tMax)
{
    bool t1Valid = oz + t1 * dz > 0.0;
    bool t2Valid = oz + t2 * dz > 0.0;

    if (t1Valid)
    {
        if (t2Valid)
        {
            tMin = max(t1, tMin);
            tMax = min(t2, tMax);
        }
        else
        {
            tMax = min(t1, tMax);
        }
    }
    else
    {
        tMin = t2Valid ? max(t2, tMin) : tMax;
    }
}

bool GetPointLightInterval(LightData lightData, float3 rayOrigin, float3 rayDirection, out float tMin, out float tMax)
{
    if (lightData.volumetricLightDimmer < 0.001)
        return false;

    float3 lightToRayOrigin = rayOrigin - lightData.positionRWS;

    if (!GetSphereInterval(lightToRayOrigin, lightData.range, rayDirection, tMin, tMax))
        return false;

    // This is just a point light (no spot cone angle)
    if (lightData.lightType == GPULIGHTTYPE_POINT)
        return true;

    // We are dealing with either a cone, a pyramid or a box
    float3 localOrigin = float3(dot(lightToRayOrigin, lightData.right),
                                dot(lightToRayOrigin, lightData.up),
                                dot(lightToRayOrigin, lightData.forward));
    float3 localDirection = float3(dot(rayDirection, lightData.right),
                                   dot(rayDirection, lightData.up),
                                   dot(rayDirection, lightData.forward));

    if (lightData.lightType == GPULIGHTTYPE_PROJECTOR_BOX)
    {
        // Compute intersections with planes x=-1 and x=1
        float tx1 = (-1.0 - localOrigin.x) / localDirection.x;
        float tx2 = (1.0 - localOrigin.x) / localDirection.x;
        Sort(tx1, tx2);

        // Compute intersections with planes y=-1 and y=1
        float ty1 = (-1.0 - localOrigin.y) / localDirection.y;
        float ty2 = (1.0 - localOrigin.y) / localDirection.y;
        Sort(ty1, ty2);

        // Compute intersection with plane z=0
        float tz = -localOrigin.z / localDirection.z;

        float t1 = max(tx1, ty1);
        float t2 = min(tx2, ty2);

        // Check validity of the intersections (we want them only in front of the light)
        bool t1Valid = localOrigin.z + t1 * localDirection.z > 0.0;
        bool t2Valid = localOrigin.z + t2 * localDirection.z > 0.0;

        tMin = t1Valid ? max(t1, tMin) : tz;
        tMax = t2Valid ? min(t2, tMax) : tz;
    }
    else if (lightData.lightType == GPULIGHTTYPE_PROJECTOR_PYRAMID)
    {
        // Compute intersections with planes x=-z and x=z
        float tx1 = -(localOrigin.x - localOrigin.z) / (localDirection.x - localDirection.z);
        float tx2 = -(localOrigin.x + localOrigin.z) / (localDirection.x + localDirection.z);
        Sort(tx1, tx2);

        // Check validity of the intersections (we want them only in front of the light)
        GetFrontInterval(localOrigin.z, localDirection.z, tx1, tx2, tMin, tMax);

        if (tMin < tMax)
        {
            // Compute intersections with planes y=-1 and y=1
            float ty1 = -(localOrigin.y - localOrigin.z) / (localDirection.y - localDirection.z);
            float ty2 = -(localOrigin.y + localOrigin.z) / (localDirection.y + localDirection.z);
            Sort(ty1, ty2);

            // Check validity of the intersections (we want them only in front of the light)
            GetFrontInterval(localOrigin.z, localDirection.z, ty1, ty2, tMin, tMax);
        }
    }
    else // lightData.lightType == GPULIGHTTYPE_SPOT
    {
        float cosTheta2 = Sq(lightData.angleOffset / lightData.angleScale);

        // Offset light origin to account for light radius
        localOrigin.z += sqrt(lightData.size.x / (1.0 - cosTheta2));

        // Account for non-normalized local basis
        float3 normalizedLocalOrigin = float3(localOrigin.x / Length2(lightData.right),
                                              localOrigin.y / Length2(lightData.up),
                                              localOrigin.z);

        float a = Sq(localDirection.z) - cosTheta2;
        float b = 2.0 * (localOrigin.z * localDirection.z - dot(normalizedLocalOrigin, localDirection) * cosTheta2);
        float c = Sq(localOrigin.z) - dot(normalizedLocalOrigin, localOrigin) * cosTheta2;

        float2 t;
        if (!SolveQuadraticEquation(a, b, c, t))
            return false;

        // Check validity of the intersections (we want them only in front of the light)
        GetFrontInterval(localOrigin.z, localDirection.z, t.x, t.y, tMin, tMax);
    }

    return tMin < tMax;
}

float GetLocalLightsInterval(float3 rayOrigin, float3 rayDirection, out float tMin, out float tMax)
{
    tMin = FLT_MAX;
    tMax = 0.0;

    float tLightMin, tLightMax;

    // First process point lights
    uint i = 0, n = _PunctualLightCountRT, localCount = 0;
    for (; i < n; i++)
    {
        if (GetPointLightInterval(_LightDatasRT[i], rayOrigin, rayDirection, tLightMin, tLightMax))
        {
            tMin = min(tMin, tLightMin);
            tMax = max(tMax, tLightMax);
            localCount++;
        }
    }

    // Then area lights
    n += _AreaLightCountRT;
    for (; i < n; i++)
    {
        if (GetRectAreaLightInterval(_LightDatasRT[i], rayOrigin, rayDirection, tLightMin, tLightMax))
        {
            tMin = min(tMin, tLightMin);
            tMax = max(tMax, tLightMax);
            localCount++;
        }
    }

    uint lightCount = localCount + _DirectionalLightCount;

    return lightCount ? float(localCount) / lightCount : -1.0;
}

LightList CreateLightList(float3 position, bool sampleLocalLights)
{
    return CreateLightList(position, 0.0, ~0, sampleLocalLights, !sampleLocalLights);
}

#endif // UNITY_PATH_TRACING_LIGHT_INCLUDED
