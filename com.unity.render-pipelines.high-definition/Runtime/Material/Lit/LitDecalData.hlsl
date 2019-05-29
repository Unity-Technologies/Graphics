void ApplyDecalToTangentSpaceNormal(DecalSurfaceData decalSurfaceData, float3 interpolatedNormalizedVertexNormalWS, inout float3 normalTS)
{
    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_NORMAL)
    {
        float3 surfGrad = SurfaceGradientFromVolumeGradient(interpolatedNormalizedVertexNormalWS, decalSurfaceData.normalWS.xyz);
        normalTS.xyz = normalTS.xyz * decalSurfaceData.normalWS.w + surfGrad.xyz;
    }
}

void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData)
{
    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_DIFFUSE)
    {
        surfaceData.baseColor.xyz = surfaceData.baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
    }

    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_MASK)
    {
#ifdef DECALS_4RT // only smoothness in 3RT mode
#ifdef _MATERIAL_FEATURE_SPECULAR_COLOR
        float3 decalSpecularColor = ComputeFresnel0((decalSurfaceData.HTileMask & DBUFFERHTILEBIT_DIFFUSE) ? decalSurfaceData.baseColor.xyz : float3(1.0, 1.0, 1.0), decalSurfaceData.mask.x, DEFAULT_SPECULAR_VALUE);
        surfaceData.specularColor = surfaceData.specularColor * decalSurfaceData.MAOSBlend.x + decalSpecularColor;
#else
        surfaceData.metallic = surfaceData.metallic * decalSurfaceData.MAOSBlend.x + decalSurfaceData.mask.x;
#endif
        surfaceData.ambientOcclusion = surfaceData.ambientOcclusion * decalSurfaceData.MAOSBlend.y + decalSurfaceData.mask.y;
#endif
        surfaceData.perceptualSmoothness = surfaceData.perceptualSmoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
    }
}
