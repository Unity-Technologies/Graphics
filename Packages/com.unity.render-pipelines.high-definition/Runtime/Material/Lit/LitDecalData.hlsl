void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, float3 vtxNormal, inout SurfaceData surfaceData
#if defined(DECAL_SURFACE_GRADIENT) && defined(SURFACE_GRADIENT)
    , inout float3 normalTS
#endif
)
{
    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html, mean weight of 1 is neutral

    // Note: We only test weight (i.e decalSurfaceData.xxx.w is < 1.0) if it can save something
    surfaceData.baseColor.xyz = surfaceData.baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;

    // Always test the normal as we can have decompression artifact
    if (decalSurfaceData.normalWS.w < 1.0)
    {
#if defined(DECAL_SURFACE_GRADIENT) && defined(SURFACE_GRADIENT)
        normalTS += SurfaceGradientFromVolumeGradient(vtxNormal, decalSurfaceData.normalWS.xyz);
#else
        surfaceData.normalWS.xyz = SafeNormalize(surfaceData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
#endif
    }

#ifdef DECALS_4RT // only smoothness in 3RT mode
#ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
    if (decalSurfaceData.MAOSBlend.x < 1.0)
    {
        float3 decalSpecularColor = ComputeFresnel0((decalSurfaceData.baseColor.w < 1.0) ? decalSurfaceData.baseColor.xyz : float3(1.0, 1.0, 1.0), decalSurfaceData.mask.x, DEFAULT_SPECULAR_VALUE);
        surfaceData.specularColor = surfaceData.specularColor * decalSurfaceData.MAOSBlend.x + decalSpecularColor * (1.0f - decalSurfaceData.MAOSBlend.x);
    }
#else
    surfaceData.metallic = surfaceData.metallic * decalSurfaceData.MAOSBlend.x + decalSurfaceData.mask.x;
#endif

    surfaceData.ambientOcclusion = surfaceData.ambientOcclusion * decalSurfaceData.MAOSBlend.y + decalSurfaceData.mask.y;
#endif

    surfaceData.perceptualSmoothness = surfaceData.perceptualSmoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
}
