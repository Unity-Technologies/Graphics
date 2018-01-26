#ifndef UNITY_VOLUMEPROJECTION_INCLUDED
#define UNITY_VOLUMEPROJECTION_INCLUDED

#define ENVMAP_FEATURE_PERFACEINFLUENCE
#define ENVMAP_FEATURE_PERFACEFADE
#define ENVMAP_FEATURE_INFLUENCENORMAL

#include "../LightDefinition.cs.hlsl"

float3x3 WorldToProxySpace(EnvLightData lightData)
{
    return transpose(
        float3x3(
            GetProxyRight(lightData),
            GetProxyUp(lightData),
            GetProxyForward(lightData)
        )
    ); // worldToLocal assume no scaling
}

float3 WorldToProxyPosition(EnvLightData lightData, float3x3 worldToPS, float3 positionWS)
{
    float3 positionPS = positionWS - GetProxyPositionWS(lightData);
    positionPS = mul(positionPS, worldToPS).xyz;
    return positionPS;
}

float IntersectSphereProxy(EnvLightData lightData, float3 dirPS, float3 positionPS)
{
    float sphereOuterDistance = lightData.proxyExtentsX;
    float projectionDistance = IntersectRaySphereSimple(positionPS, dirPS, sphereOuterDistance);
    projectionDistance = max(projectionDistance, lightData.minProjectionDistance); // Setup projection to infinite if requested (mean no projection shape)

    return projectionDistance;
}

float IntersectBoxProxy(EnvLightData lightData, float3 dirPS, float3 positionPS)
{
    float3 boxOuterDistance = GetProxyExtents(lightData);
    float projectionDistance = IntersectRayAABBSimple(positionPS, dirPS, -boxOuterDistance, boxOuterDistance);
    projectionDistance = max(projectionDistance, lightData.minProjectionDistance); // Setup projection to infinite if requested (mean no projection shape)

    return projectionDistance;
}

float InfluenceSphereWeight(EnvLightData lightData, BSDFData bsdfData, float3 positionWS, float3 positionLS, float3 dirLS)
{
    float lengthPositionLS = length(positionLS);
    float sphereInfluenceDistance = lightData.influenceExtentsX - lightData.blendDistancePositiveX;
    float distFade = max(lengthPositionLS - sphereInfluenceDistance, 0.0);
    float alpha = saturate(1.0 - distFade / max(lightData.blendDistancePositiveX, 0.0001)); // avoid divide by zero

#if defined(ENVMAP_FEATURE_INFLUENCENORMAL)
    float insideInfluenceNormalVolume = lengthPositionLS <= (lightData.influenceExtentsX - lightData.blendNormalDistancePositiveX) ? 1.0 : 0.0;
    float insideWeight = InfluenceFadeNormalWeight(bsdfData.normalWS, normalize(positionWS - GetCapturePositionWS(lightData)));
    alpha *= insideInfluenceNormalVolume ? 1.0 : insideWeight;
#endif

    return alpha;
}

float InfluenceBoxWeight(EnvLightData lightData, BSDFData bsdfData, float3 positionWS, float3 positionIS, float3 dirIS)
{
    float3 influenceExtents = GetInfluenceExtents(lightData);
    // 2. Process the position influence
    // Calculate falloff value, so reflections on the edges of the volume would gradually blend to previous reflection.
#if defined(ENVMAP_FEATURE_PERFACEINFLUENCE) || defined(ENVMAP_FEATURE_INFLUENCENORMAL) || defined(ENVMAP_FEATURE_PERFACEFADE)
    // Distance to each cube face
    float3 negativeDistance = influenceExtents + positionIS;
    float3 positiveDistance = influenceExtents - positionIS;
#endif

#if defined(ENVMAP_FEATURE_PERFACEINFLUENCE)
    // Influence falloff for each face
    float3 negativeFalloff = negativeDistance / max(0.0001, GetBlendDistanceNegative(lightData));
    float3 positiveFalloff = positiveDistance / max(0.0001, GetBlendDistancePositive(lightData));

    // Fallof is the min for all faces
    float influenceFalloff = min(
        min(min(negativeFalloff.x, negativeFalloff.y), negativeFalloff.z),
        min(min(positiveFalloff.x, positiveFalloff.y), positiveFalloff.z));

    float alpha = saturate(influenceFalloff);
#else
    float distFace = DistancePointBox(positionIS, -influenceExtents + lightData.blendDistancePositiveX, influenceExtents - lightData.blendDistancePositiveX);
    float alpha = saturate(1.0 - distFace / max(lightData.blendDistancePositiveX, 0.0001));
#endif

#if defined(ENVMAP_FEATURE_INFLUENCENORMAL)
    // 3. Process the normal influence
    // Calculate a falloff value to discard normals pointing outward the center of the environment light
    float3 belowPositiveInfluenceNormalVolume = positiveDistance / max(0.0001, GetBlendNormalDistancePositive(lightData));
    float3 aboveNegativeInfluenceNormalVolume = negativeDistance / max(0.0001, GetBlendNormalDistanceNegative(lightData));
    float insideInfluenceNormalVolume = all(belowPositiveInfluenceNormalVolume >= 1.0) && all(aboveNegativeInfluenceNormalVolume >= 1.0) ? 1.0 : 0;
    float insideWeight = InfluenceFadeNormalWeight(bsdfData.normalWS, normalize(positionWS - GetCapturePositionWS(lightData)));
    alpha *= insideInfluenceNormalVolume ? 1.0 : insideWeight;
#endif

#if defined(ENVMAP_FEATURE_PERFACEFADE)
    // 4. Fade specific cubemap faces
    // For each axes (both positive and negative ones), we want to fade from the center of one face to another
    // So we normalized the sample direction (R) and use its component to fade for each axis
    // We consider R.x as cos(X) and then fade as angle from 60°(=acos(1/2)) to 75°(=acos(1/4))
    // For positive axes: axisFade = (R - 1/4) / (1/2 - 1/4)
    // <=> axisFace = 4 * R - 1;
    float3 faceFade = saturate((4 * dirIS - 1) * GetBoxSideFadePositive(lightData))
                    + saturate((-4 * dirIS - 1) * GetBoxSideFadeNegative(lightData));
    alpha *= saturate(faceFade.x + faceFade.y + faceFade.z);
#endif

    return alpha;
}



float3x3 WorldToInfluenceSpace(EnvLightData lightData)
{
    return transpose(
        float3x3(
            GetInfluenceRight(lightData),
            GetInfluenceUp(lightData),
            GetInfluenceForward(lightData)
        )
    ); // worldToLocal assume no scaling
}

float3 WorldToInfluencePosition(EnvLightData lightData, float3x3 worldToIS, float3 positionWS)
{
    float3 positionIS = positionWS - GetInfluencePositionWS(lightData);
    positionIS = mul(positionIS, worldToIS).xyz;
    return positionIS;
}

#endif // UNITY_VOLUMEPROJECTION_INCLUDED
