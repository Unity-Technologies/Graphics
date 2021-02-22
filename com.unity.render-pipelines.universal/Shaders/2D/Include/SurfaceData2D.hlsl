
#ifndef SURFACE_DATA_2D_INCLUDED
#define SURFACE_DATA_2D_INCLUDED

struct SurfaceData2D
{
    half3 albedo;
    half alpha;
    half4 mask;
    half3 normalTS;
    half2 lightingUV;
};

SurfaceData2D CreateSurfaceData(half3 albedo, half alpha, half4 mask, half3 normalTS, half2 lightingUV)
{
    SurfaceData2D surfaceData;

    surfaceData.albedo = albedo;
    surfaceData.alpha = alpha;
    surfaceData.mask = mask;
    surfaceData.normalTS = normalTS;
    surfaceData.lightingUV = lightingUV;

    return surfaceData;
}

SurfaceData2D CreateSurfaceData(half3 albedo, half alpha, half4 mask, half2 lightingUV)
{
    const half3 normalTS = half3(0, 0, 1);

    return CreateSurfaceData(albedo, alpha, mask, normalTS, lightingUV);
}

#endif
