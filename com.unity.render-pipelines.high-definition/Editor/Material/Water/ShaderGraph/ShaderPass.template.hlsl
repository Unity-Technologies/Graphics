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
    $SurfaceDescription.RefractionColor:            surfaceData.refractionColor =           surfaceDescription.RefractionColor;

    bentNormalWS = float3(0, 1, 0);
}
