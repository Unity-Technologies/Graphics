struct TerrainLitSurfaceData
{
    float3 albedo;
    float3 normalData;
    float smoothness;
    float metallic;
    float ao;
    float subsurfaceMask;
    uint diffusionProfile;
    float thickness;
};

void InitializeTerrainLitSurfaceData(out TerrainLitSurfaceData surfaceData)
{
    surfaceData.albedo = 0;
    surfaceData.normalData = 0;
    surfaceData.smoothness = 0;
    surfaceData.metallic = 0;
    surfaceData.ao = 0;
    surfaceData.subsurfaceMask = 0;
    surfaceData.diffusionProfile = 0;
    surfaceData.thickness = 1;
}
