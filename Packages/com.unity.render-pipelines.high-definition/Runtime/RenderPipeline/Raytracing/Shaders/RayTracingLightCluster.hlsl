#ifndef UNITY_RAY_TRACING_LIGHT_CLUSTER_INCLUDED
#define UNITY_RAY_TRACING_LIGHT_CLUSTER_INCLUDED

// This allows us to either use the light cluster to pick which lights should be used, or use all the lights available

uint GetTotalLightClusterCellCount(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + CELL_META_DATA_SIZE) + CELL_META_DATA_TOTAL_INDEX];
}

uint GetPunctualLightEndIndexInClusterCell(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + CELL_META_DATA_SIZE) + CELL_META_DATA_PUNCTUAL_END_INDEX];
}

uint GetAreaLightEndIndexInClusterCell(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + CELL_META_DATA_SIZE) + CELL_META_DATA_AREA_END_INDEX];
}

uint GetEnvLightEndIndexInClusterCell(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + CELL_META_DATA_SIZE) + CELL_META_DATA_ENV_END_INDEX];
}

uint GetDecalEndIndexInClusterCell(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + CELL_META_DATA_SIZE) + CELL_META_DATA_DECAL_END_INDEX];
}

uint GetLightClusterCellLightByIndex(int cellIndex, int lightIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + CELL_META_DATA_SIZE) + CELL_META_DATA_SIZE + lightIndex];
}

bool PointInsideCluster(float3 positionWS)
{
    return !(positionWS.x < _MinClusterPos.x || positionWS.y < _MinClusterPos.y || positionWS.z < _MinClusterPos.z
        || positionWS.x > _MaxClusterPos.x || positionWS.y > _MaxClusterPos.y || positionWS.z > _MaxClusterPos.z);
}

uint GetClusterCellIndex(float3 positionWS)
{
    // Compute the grid position
    uint3 gridPosition = (uint3)((positionWS - _MinClusterPos) / (_MaxClusterPos - _MinClusterPos) * float3(64.0, 64.0, 32.0));

    // Deduce the cell index
    return gridPosition.z + gridPosition.y * 32 + gridPosition.x * 2048;
}


void GetLightCountAndStartCluster(float3 positionWS, uint lightCategory, out uint lightStart, out uint lightEnd, out uint cellIndex)
{
    lightStart = 0;
    lightEnd = 0;
    cellIndex = 0;

    // If this point is inside the cluster, get lights
    if(PointInsideCluster(positionWS))
    {
        // Deduce the cell index
        cellIndex = GetClusterCellIndex(positionWS);

        // Grab the light count -- in principle all invocations take the same branch 
        switch (lightCategory)
        {
            case 0: // LIGHTCATEGORY_PUNCTUAL
                lightStart = 0;
                lightEnd = GetPunctualLightEndIndexInClusterCell(cellIndex);
                break;
            case 1: // LIGHTCATEGORY_AREA
                lightStart = GetPunctualLightEndIndexInClusterCell(cellIndex);
                lightEnd = GetAreaLightEndIndexInClusterCell(cellIndex);
                break;
            case 2: // LIGHTCATEGORY_ENV
                lightStart = GetAreaLightEndIndexInClusterCell(cellIndex);
                lightEnd = GetEnvLightEndIndexInClusterCell(cellIndex);
                break;
            case 3: // LIGHTCATEGORY_DECAL
                lightStart = GetEnvLightEndIndexInClusterCell(cellIndex);
                lightEnd = GetDecalEndIndexInClusterCell(cellIndex);
                break;
                
        }
    }
}

LightData FetchClusterLightIndex(int cellIndex, uint lightIndex)
{
    int absoluteLightIndex = GetLightClusterCellLightByIndex(cellIndex, lightIndex);
    return _LightDatasRT[absoluteLightIndex];
}

EnvLightData FetchClusterEnvLightIndex(int cellIndex, uint lightIndex)
{
    int absoluteLightIndex = GetLightClusterCellLightByIndex(cellIndex, lightIndex);
    return _EnvLightDatasRT[absoluteLightIndex];
}

#if defined(HAS_LIGHTLOOP) && (SHADERPASS != SHADERPASS_PATH_TRACING)
float3 RayTraceReflectionProbes(float3 rayOrigin, float3 rayDirection, inout float totalWeight)
{
    float3 result = 0.0;
    totalWeight = 0.0;

    uint lightStart = 0, lightEnd = 0, cellIndex = 0;
    #ifdef USE_LIGHT_CLUSTER
    // Get the punctual light count
    GetLightCountAndStartCluster(rayOrigin, LIGHTCATEGORY_ENV, lightStart, lightEnd, cellIndex);
    #else
    lightStart = 0;
    lightEnd = _EnvLightCountRT;
    #endif
    // Scalarized loop, same rationale of the punctual light version
    uint envLightIdx = lightStart;
    while (envLightIdx < lightEnd)
    {
        #ifdef USE_LIGHT_CLUSTER
        EnvLightData envLightData = FetchClusterEnvLightIndex(cellIndex, envLightIdx);
        #else
        EnvLightData envLightData = _EnvLightDatasRT[envLightIdx];
        #endif

        if (IsEnvIndexCubemap(envLightData.envIndex) && totalWeight < 1.0)
        {
            float weight = 1.0;
            float3 R = rayDirection;
            float intersectionDistance = EvaluateLight_EnvIntersection(rayOrigin, rayDirection, envLightData, envLightData.influenceShapeType, R, weight);

            int index = abs(envLightData.envIndex) - 1;

            float2 atlasCoords = GetReflectionAtlasCoordsCube(CUBE_SCALE_OFFSET[index], R, 0);

            float3 probeResult = SAMPLE_TEXTURE2D_ARRAY_LOD(_ReflectionAtlas, s_trilinear_clamp_sampler, atlasCoords, 0, 0).rgb * envLightData.rangeCompressionFactorCompensation;
            probeResult = ClampToFloat16Max(probeResult);

            UpdateLightingHierarchyWeights(totalWeight, weight);
            result += weight * probeResult * envLightData.multiplier;
        }

        envLightIdx++;
    }
    totalWeight = saturate(totalWeight);
    return result;
}
#endif


#endif
