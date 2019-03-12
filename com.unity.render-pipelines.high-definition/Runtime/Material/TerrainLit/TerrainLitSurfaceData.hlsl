struct TerrainLitSurfaceData
{
    float3 albedo;
    float3 normalData;
    float smoothness;
    float metallic;
    float ao;
};

void InitializeTerrainLitSurfaceData(out TerrainLitSurfaceData surfaceData)
{
    surfaceData.albedo = 0;
    surfaceData.normalData = 0;
    surfaceData.smoothness = 0;
    surfaceData.metallic = 0;
    surfaceData.ao = 1;
}
