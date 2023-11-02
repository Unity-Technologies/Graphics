SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
{
    SurfaceDescriptionInputs output;
    ZERO_INITIALIZE(SurfaceDescriptionInputs, output);

    const float R = _PlanetaryRadius;
    const float3 V = GetSkyViewDirWS(input.positionCS.xy);
    float3 N; float r;

#ifdef LOCAL_SKY
    const float3 O = _PBRSkyCameraPosPS;

    float tEntry = IntersectAtmosphere(O, V, N, r).x;
    float tExit  = IntersectAtmosphere(O, V, N, r).y;

    float cosChi = -dot(N, V);
    float cosHor = ComputeCosineOfHorizonAngle(r);
#else
    N = float3(0, 1, 0);
    r = _PlanetaryRadius;
    float cosChi = -dot(N, V);
    float cosHor = 0.0f;
    const float3 O = N * r;

    float tEntry = 0.0f;
    float tExit  = IntersectSphere(_AtmosphericRadius, -dot(N, V), r).y;
#endif

    bool rayIntersectsAtmosphere = (tEntry >= 0);
    bool lookAboveHorizon        = (cosChi >= cosHor);
    float tGround = tEntry + IntersectSphere(R, cosChi, r).x;
    float tFrag   = FLT_INF;

    output.WorldSpaceViewDirection = -V;
    $SurfaceDescriptionInputs.WorldSpaceNormal: output.WorldSpaceNormal = normalize(O - tGround * V);
    $SurfaceDescriptionInputs.WorldSpacePosition: output.WorldSpacePosition = - tGround * V;

    output.renderSunDisk = _RenderSunDisk;

    if (output.renderSunDisk != 0)
        output.radiance = RenderSunDisk(tFrag, tExit, V);

    output.tFrag = tFrag;
    output.intersectAtmosphere = rayIntersectsAtmosphere;
    output.hitGround = rayIntersectsAtmosphere && !lookAboveHorizon;
    output.tGround = tGround;

    $SurfaceDescriptionInputs.TangentSpaceNormal: output.TangentSpaceNormal = float3(0.0f, 0.0f, 1.0f);
    $SurfaceDescriptionInputs.TimeParameters:     output.TimeParameters = _TimeParameters.xyz;

    return output;
}
