void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData, out float3 bentNormalWS)
{
    // setup defaults -- these are used if the graph doesn't output a value
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    $SurfaceDescription.BaseColor:                  surfaceData.baseColor =                 surfaceDescription.BaseColor;

    $SurfaceDescription.NormalWS:                   surfaceData.normalWS =                  surfaceDescription.NormalWS;
    $SurfaceDescription.LowFrequencyNormalWS:       surfaceData.lowFrequencyNormalWS =      surfaceDescription.LowFrequencyNormalWS;

    $SurfaceDescription.Smoothness:                 surfaceData.perceptualSmoothness =      surfaceDescription.Smoothness;
    $SurfaceDescription.FoamColor:                  surfaceData.foamColor =                 surfaceDescription.FoamColor;
    $SurfaceDescription.SpecularSelfOcclusion:      surfaceData.specularSelfOcclusion =     surfaceDescription.SpecularSelfOcclusion;

    $SurfaceDescription.TipThickness:               surfaceData.tipThickness =              surfaceDescription.TipThickness;
    $SurfaceDescription.RefractionColor:            surfaceData.refractionColor =           surfaceDescription.RefractionColor;

    // These static material feature allow compile time optimization
    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_WATER_STANDARD;
    #ifdef _MATERIAL_FEATURE_WATER_CINEMATIC
        surfaceData.materialFeatures |= MATERIALFEATUREFLAGS_WATER_CINEMATIC;
    #endif

    bentNormalWS = float3(0, 1, 0);
}
