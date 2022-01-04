void ApplyDecalToSurfaceData(DecalSurfaceData decalSurfaceData, float3 vtxNormal, inout SurfaceData surfaceData)
{
    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
    float decalFoam = Luminance(decalSurfaceData.baseColor.xyz);
    surfaceData.foam = surfaceData.foam * decalSurfaceData.baseColor.w + decalFoam;

    // Always test the normal as we can have decompression artifact
    if (decalSurfaceData.normalWS.w < 1.0)
    {
        surfaceData.normalWS.xyz = SafeNormalize(surfaceData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
    }

    surfaceData.perceptualSmoothness = surfaceData.perceptualSmoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
}

void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData, out float3 bentNormalWS)
{
    // setup defaults -- these are used if the graph doesn't output a value
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    $SurfaceDescription.BaseColor:                  surfaceData.baseColor =                 surfaceDescription.BaseColor;

    $SurfaceDescription.NormalWS:                   surfaceData.normalWS =                  surfaceDescription.NormalWS;
    $SurfaceDescription.LowFrequencyNormalWS:       surfaceData.lowFrequencyNormalWS =      surfaceDescription.LowFrequencyNormalWS;

    $SurfaceDescription.Smoothness:                 surfaceData.perceptualSmoothness =      surfaceDescription.Smoothness;
    $SurfaceDescription.Foam:                       surfaceData.foam =                      surfaceDescription.Foam;

    $SurfaceDescription.TipThickness:               surfaceData.tipThickness =              surfaceDescription.TipThickness;
    $SurfaceDescription.Caustics:                   surfaceData.caustics =                  surfaceDescription.Caustics;

    bentNormalWS = float3(0, 1, 0);

    #if HAVE_DECALS
        if (_EnableDecals)
        {
            float alpha = 1.0;
            // Both uses and modifies 'surfaceData.normalWS'.
            DecalSurfaceData decalSurfaceData = GetDecalSurfaceData(posInput, fragInputs, _WaterDecalLayer, alpha);
            ApplyDecalToSurfaceData(decalSurfaceData, fragInputs.tangentToWorld[2], surfaceData);
        }
    #endif

    // Kill the scattering and the refraction based on where foam is perceived
    surfaceData.baseColor *= (1 - saturate(surfaceData.foam));
}
