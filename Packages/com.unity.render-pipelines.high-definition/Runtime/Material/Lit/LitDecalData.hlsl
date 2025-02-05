void ApplyDecalToSurfaceDataNoNormal(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
{
    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html, mean weight of 1 is neutral

    // Note: We only test weight (i.e decalSurfaceData.xxx.w is < 1.0) if it can save something
    surfaceData.baseColor.xyz = surfaceData.baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;

#ifdef DECALS_4RT // only smoothness in 3RT mode
#ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
    if (decalSurfaceData.MAOSBlend.x < 1.0)
    {
        float3 decalSpecularColor = ComputeFresnel0((decalSurfaceData.baseColor.w < 1.0) ? decalSurfaceData.baseColor.xyz : float3(1.0, 1.0, 1.0), decalSurfaceData.mask.x, DEFAULT_SPECULAR_VALUE);
        surfaceData.specularColor = surfaceData.specularColor * decalSurfaceData.MAOSBlend.x + decalSpecularColor * (1.0f - decalSurfaceData.MAOSBlend.x);
        surfaceData.baseColor = ComputeDiffuseColor(surfaceData.baseColor, decalSurfaceData.mask.x);
    }
#else
    surfaceData.metallic = surfaceData.metallic * decalSurfaceData.MAOSBlend.x + decalSurfaceData.mask.x;
#endif

    surfaceData.ambientOcclusion = surfaceData.ambientOcclusion * decalSurfaceData.MAOSBlend.y + decalSurfaceData.mask.y;
#endif

    surfaceData.perceptualSmoothness = surfaceData.perceptualSmoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
}

void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, float3 vtxNormal, inout SurfaceData surfaceData
#ifdef DECAL_SURFACE_GRADIENT
    , inout float3 normalTS
#endif
)
{
#ifdef DECAL_SURFACE_GRADIENT
    ApplyDecalToSurfaceNormal(decalSurfaceData, vtxNormal, normalTS);
#else
    ApplyDecalToSurfaceNormal(decalSurfaceData, surfaceData.normalWS.xyz);
#endif

    ApplyDecalToSurfaceDataNoNormal(decalSurfaceData, surfaceData);
}
