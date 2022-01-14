#if (SHADERPASS != SHADERPASS_VBUFFER_LIGHTING)
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VBufferDeferredMaterialCommon.hlsl"

void Frag(Varyings packedInput, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    VBufferDeferredMaterialFragmentData fragmentData = BootstrapDeferredMaterialFragmentShader(packedInput);
    if (!fragmentData.valid)
    {
        outColor = float4(0,0,0,0);
        return;
    }

    //Sampling of deferred material has been done.
    //Now perform forward lighting.
    FragInputs input = fragmentData.fragInputs;
    float depthValue = fragmentData.depthValue;
    float3 V = fragmentData.V;

    int2 tileCoord = (float2)input.positionSS.xy / GetTileSize();
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, depthValue, UNITY_MATRIX_I_VP, GetWorldToViewMatrix(), tileCoord);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    float3 colorVariantColor = 0;
    uint featureFlags = packedInput.lightAndMaterialFeatures;

    LightLoopOutput lightLoopOutput;
    LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

    float3 diffuseLighting =  lightLoopOutput.diffuseLighting;
    float3 specularLighting = lightLoopOutput.specularLighting;

    diffuseLighting *= GetCurrentExposureMultiplier();
    specularLighting *= GetCurrentExposureMultiplier();

    outColor.rgb = diffuseLighting + specularLighting;
    outColor.a = 1;
}
