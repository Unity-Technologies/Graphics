
#ifndef SURFACE_DATA_2D_INCLUDED
#define SURFACE_DATA_2D_INCLUDED

struct SurfaceData2D
{
    half3 albedo;
    half alpha;
    half4 mask;
    half3 normalTS;
#if defined(DEBUG_DISPLAY)
    half3 normalWS;
#endif
};

void InitializeSurfaceData(half3 albedo, half alpha, half4 mask, half3 normalTS, out SurfaceData2D surfaceData)
{
    surfaceData = (SurfaceData2D)0;

    surfaceData.albedo = albedo;
    surfaceData.alpha = alpha;
    surfaceData.mask = mask;
    surfaceData.normalTS = normalTS;
}

void InitializeSurfaceData(half3 albedo, half alpha, half4 mask, out SurfaceData2D surfaceData)
{
    const half3 normalTS = half3(0, 0, 1);

    InitializeSurfaceData(albedo, alpha, mask, normalTS, surfaceData);
}

void InitializeSurfaceData(half3 albedo, half alpha, out SurfaceData2D surfaceData)
{
    InitializeSurfaceData(albedo, alpha, 1, surfaceData);
}

#endif
