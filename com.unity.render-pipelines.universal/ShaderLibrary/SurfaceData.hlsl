#ifndef UNIVERSAL_SURFACE_DATA_INCLUDED
#define UNIVERSAL_SURFACE_DATA_INCLUDED

// Must match Universal ShaderGraph master node
struct SurfaceData
{
    half3 albedo;
    half3 specular;
    half  metallic;
    half  smoothness;
    half3 normalTS;
    half3 emission;
    half  occlusion;
    half  alpha;
    half  clearCoatMask;
    half  clearCoatSmoothness;
};

SurfaceData CreateSurfaceData(half3 albedo, half metallic, half3 specular, half smoothness,
                              half occlusion, half3 emission, half alpha, half3 normalTS,
                              half clearCoatMask, half clearCoatSmoothness)
{
    SurfaceData surfaceData;

    surfaceData.albedo = albedo;
    surfaceData.specular = specular;
    surfaceData.metallic = metallic;
    surfaceData.smoothness = smoothness;
    surfaceData.normalTS = normalTS;
    surfaceData.emission = emission;
    surfaceData.occlusion = occlusion;
    surfaceData.alpha = alpha;
    surfaceData.clearCoatMask = clearCoatMask;
    surfaceData.clearCoatSmoothness = clearCoatSmoothness;

    return surfaceData;
}

SurfaceData CreateSurfaceData(half3 albedo, half metallic, half3 specular, half smoothness,
                              half occlusion, half3 emission, half alpha, half3 normalTS)
{
    return CreateSurfaceData(albedo, metallic, specular, smoothness, occlusion, emission, alpha, normalTS, 0, 1);
}

SurfaceData CreateSurfaceData(half3 albedo, half metallic, half3 specular, half smoothness,
                              half occlusion, half3 emission, half alpha)
{
    half3 normalTS = half3(0.0h, 0.0h, 1.0h);

    return CreateSurfaceData(albedo, metallic, specular, smoothness, occlusion, emission, alpha, normalTS);
}

#endif
