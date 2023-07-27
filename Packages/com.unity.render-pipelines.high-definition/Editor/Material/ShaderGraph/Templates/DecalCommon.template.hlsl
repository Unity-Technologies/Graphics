void ApplyDecalToSurfaceDataNoNormal(DecalSurfaceData decalSurfaceData, inout SurfaceData surfaceData);

void ApplyDecalAndGetNormal(FragInputs fragInputs, PositionInputs posInput, SurfaceDescription surfaceDescription,
    inout SurfaceData surfaceData)
{
    float3 doubleSidedConstants = GetDoubleSidedConstants();

#ifdef DECAL_NORMAL_BLENDING
    // SG nodes don't ouptut surface gradients, so if decals require surf grad blending, we have to convert
    // the normal to gradient before applying the decal. We then have to resolve the gradient back to world space
    float3 normalTS;
    $SurfaceDescription.NormalOS: normalTS = SurfaceGradientFromPerturbedNormal(fragInputs.tangentToWorld[2],
    $SurfaceDescription.NormalOS:     TransformObjectToWorldNormal(surfaceDescription.NormalOS));

    $SurfaceDescription.NormalTS: normalTS = SurfaceGradientFromTangentSpaceNormalAndFromTBN(surfaceDescription.NormalTS,
    $SurfaceDescription.NormalTS:     fragInputs.tangentToWorld[0], fragInputs.tangentToWorld[1]);

    $SurfaceDescription.NormalWS: normalTS = SurfaceGradientFromPerturbedNormal(fragInputs.tangentToWorld[2],
    $SurfaceDescription.NormalWS:     surfaceDescription.NormalWS);

    #if HAVE_DECALS
    if (_EnableDecals)
    {
        float alpha = 1.0;
        $SurfaceDescription.Alpha: alpha = surfaceDescription.Alpha;

        DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, fragInputs, alpha);
        ApplyDecalToSurfaceNormal(decalSurfaceData, fragInputs.tangentToWorld[2], normalTS);
        ApplyDecalToSurfaceDataNoNormal(decalSurfaceData, surfaceData);
    }
    #endif

    GetNormalWS_SG(fragInputs, normalTS, surfaceData.normalWS, doubleSidedConstants);
#else
    // normal delivered to master node
    $SurfaceDescription.NormalOS: GetNormalWS_SrcOS(fragInputs, surfaceDescription.NormalOS, surfaceData.normalWS, doubleSidedConstants);
    $SurfaceDescription.NormalTS: GetNormalWS(fragInputs, surfaceDescription.NormalTS, surfaceData.normalWS, doubleSidedConstants);
    $SurfaceDescription.NormalWS: GetNormalWS_SrcWS(fragInputs, surfaceDescription.NormalWS, surfaceData.normalWS, doubleSidedConstants);

    #if HAVE_DECALS
    if (_EnableDecals)
    {
        float alpha = 1.0;
        $SurfaceDescription.Alpha: alpha = surfaceDescription.Alpha;

        // Both uses and modifies 'surfaceData.normalWS'.
        DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, fragInputs, alpha);
        ApplyDecalToSurfaceNormal(decalSurfaceData, surfaceData.normalWS.xyz);
        ApplyDecalToSurfaceDataNoNormal(decalSurfaceData, surfaceData);
    }
    #endif
#endif
}
