// This allows us to either use the light cluster to pick which lights should be used, or use all the lights available

uint GetTotalLightClusterCellCount(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 4) + 0];
}

uint GetPunctualLightClusterCellCount(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 4) + 1];
}

uint GetAreaLightClusterCellCount(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 4) + 2];
}

uint GetEnvLightClusterCellCount(int cellIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 4) + 3];
}

uint GetLightClusterCellLightByIndex(int cellIndex, int lightIndex)
{
    return _RaytracingLightCluster[cellIndex * (_LightPerCellCount + 4) + 4 + lightIndex];
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

        // Grab the light count
        lightStart = lightCategory == 0 ? 0 : (lightCategory == 1 ? GetPunctualLightClusterCellCount(cellIndex) : GetAreaLightClusterCellCount(cellIndex));
        lightEnd = lightCategory == 0 ? GetPunctualLightClusterCellCount(cellIndex) : (lightCategory == 1 ? GetAreaLightClusterCellCount(cellIndex) : GetEnvLightClusterCellCount(cellIndex));
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
