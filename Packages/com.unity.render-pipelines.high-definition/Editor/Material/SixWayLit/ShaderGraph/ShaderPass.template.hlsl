void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData, out float3 bentNormalWS)
{
    ZERO_INITIALIZE(SurfaceData, surfaceData);

    $SurfaceDescription.BaseColor:          surfaceData.baseColor.rgb =             surfaceDescription.BaseColor;
    $SurfaceDescription.Alpha:              surfaceData.baseColor.a =               surfaceDescription.Alpha;

    $SurfaceDescription.RightTopBack:       surfaceData.rightTopBack =              surfaceDescription.RightTopBack * INV_PI;
    $SurfaceDescription.LeftBottomFront:    surfaceData.leftBottomFront =           surfaceDescription.LeftBottomFront * INV_PI;
    $SurfaceDescription.AbsorptionStrength: surfaceData.absorptionRange =           INV_PI + saturate(surfaceDescription.AbsorptionStrength) * (1 - INV_PI);
    $SurfaceDescription.Occlusion:          surfaceData.ambientOcclusion =          surfaceDescription.Occlusion;

    $FragInputs.diffuseGIData0:             surfaceData.bakeDiffuseLighting0 =      fragInputs.diffuseGIData[0];
    $FragInputs.diffuseGIData1:             surfaceData.bakeDiffuseLighting1 =      fragInputs.diffuseGIData[1];
    $FragInputs.diffuseGIData2:             surfaceData.bakeDiffuseLighting2 =      fragInputs.diffuseGIData[2];

    float frontFaceSign = fragInputs.isFrontFace ? 1.0f : -1.0f;

    surfaceData.tangentWS = float4(normalize(fragInputs.tangentToWorld[0].xyz), 1);
    surfaceData.bitangentWS = fragInputs.tangentToWorld[1].xyz;
    surfaceData.normalWS = frontFaceSign * fragInputs.tangentToWorld[2].xyz;

    surfaceData.bakeDiffuseLighting2.xyz *= frontFaceSign;


    bentNormalWS = surfaceData.normalWS; //Not used
    #ifdef DEBUG_DISPLAY
        // We need to call ApplyDebugToSurfaceData after filling the surfarcedata and before filling builtinData
        // as it can modify attribute use for static lighting
        ApplyDebugToSurfaceData(fragInputs.tangentToWorld, surfaceData);
    #endif

}
