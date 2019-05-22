float2 SafeNormalizeFloat2(float2 inVec)
{
    float dp3 = max(REAL_MIN, dot(inVec, inVec));
    return inVec * rsqrt(dp3);
}

void ApplyDecalToTangentSpaceNormal(DecalSurfaceData decalSurfaceData, UVMapping uvMapping, float3 dPdx, float3 dPdy, inout float3 normalTS)
{
    float dsdx = ddx(uvMapping.uv.x), dsdy = ddy(uvMapping.uv.x);
    float dtdx = ddx(uvMapping.uv.y), dtdy = ddy(uvMapping.uv.y);

    float recipDet = (dsdx*dtdy - dtdx * dsdy) < 0 ? -1 : 1;

    float dxds = dtdy, dxdt = -dsdy;
    float dyds = -dtdx, dydt = dsdx;

    dxds *= recipDet;
    dxdt *= recipDet;
    dyds *= recipDet;
    dydt *= recipDet;

    const float ds = decalSurfaceData.normalWS.x * dxds + decalSurfaceData.normalWS.y * dyds;
    const float dt = decalSurfaceData.normalWS.x * dxdt + decalSurfaceData.normalWS.y * dydt;

    float pseudoWidth = 1;
    float pseudoHeight = 1;

#ifdef _NORMALMAP
    _NormalMap.GetDimensions(pseudoWidth, pseudoHeight);
#else
    float3 dPds = dPdx * dxds + dPdy * dyds;
    float3 dPdt = dPdx * dxdt + dPdy * dydt;

    pseudoWidth = length(dPds);
    pseudoHeight = length(dPdt);
#endif

    float2 deriv = float2(ds, dt) / float2(pseudoWidth, pseudoHeight);
    deriv = decalSurfaceData.normalWS.z * SafeNormalizeFloat2(deriv);
    deriv *= -rsqrt(max(1 - Sq(deriv.x) - Sq(deriv.y), Sq(FLT_EPS)));

    float3 surfGrad = SurfaceGradientFromTBN(deriv, uvMapping.tangentWS, uvMapping.bitangentWS);
 
    if (decalSurfaceData.HTileMask & DBUFFERHTILEBIT_NORMAL)
    {
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
