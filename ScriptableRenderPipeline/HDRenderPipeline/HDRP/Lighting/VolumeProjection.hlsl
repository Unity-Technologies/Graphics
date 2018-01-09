#ifndef UNITY_VOLUMEPROJECTION_INCLUDED
#define UNITY_VOLUMEPROJECTION_INCLUDED

float3x3 EnvProjData_GetWorldToLocal(EnvProjData projData)
{
    return transpose(float3x3(projData.right, projData.up, projData.forward)); // worldToLocal assume no scaling
}

float3x3 EnvProjData_WorldToLocalPosition(EnvProjData projData, float3x3 worldToLS, float3 positionWS)
{
    float3 positionLS = positionWS - projData.positionWS;
    positionLS = mul(positionLS, worldToLS).xyz;
    return positionLS;
}

float EnvProjData_Sphere_Project(EnvProjData projData, float3 dirPS, float3 positionPS)
{
    float sphereOuterDistance = projData.extents.x;
    float projectionDistance = SphereRayIntersectSimple(positionPS, dirPS, sphereOuterDistance);
    projectionDistance = max(projectionDistance, projData.minProjectionDistance); // Setup projection to infinite if requested (mean no projection shape)

    return projectionDistance;
}

float3x3 EnvLightData_GetWorldToLocal(EnvLightData lightData)
{
    return transpose(float3x3(lightData.right, lightData.up, lightData.forward)); // worldToLocal assume no scaling
}

float3 EnvLightData_WorldToLocalPosition(EnvLightData lightData, float3x3 worldToLS, float3 positionWS)
{
    // CAUTION: localToWorld is the transform use to convert the cubemap capture point to world space (mean it include the offset)
    // the center of the bounding box is thus in locals space: positionLS - offsetLS
    // We use this formulation as it is the one of legacy unity that was using only AABB box.
    float3 positionLS = positionWS - lightData.positionWS;
    positionLS = mul(positionLS, worldToLS).xyz - lightData.offsetLS;
    return positionLS;
}

#endif // UNITY_VOLUMEPROJECTION_INCLUDED
