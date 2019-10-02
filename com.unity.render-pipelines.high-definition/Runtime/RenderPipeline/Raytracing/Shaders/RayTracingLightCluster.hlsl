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
