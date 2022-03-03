#if (SHADERPASS != SHADERPASS_VBUFFER_LIGHTING_OFFSCREEN)
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VisibilityOITResources.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Visibility/VBufferDeferredMaterialCommon.hlsl"

float4 _VBufferLightingOffscreenParams;

uint GetDispatchWidth()
{
    return asuint(_VBufferLightingOffscreenParams.x);
}

uint GetMaximumSamplesCount()
{
    return asuint(_VBufferLightingOffscreenParams.y);
}

Varyings VertSingleTile(Attributes inputMesh)
{
    Varyings output;
    ZERO_INITIALIZE(Varyings, output);
    UNITY_SETUP_INSTANCE_ID(inputMesh);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

   // NaN SV_Position causes GPU to reject all triangles that use this vertex
    //float nan = sqrt(-1);
    float nan = 0.0f / 0.0f;
    output.positionCS = float4(nan, nan, nan, nan);

#ifdef DOTS_INSTANCING_ON
    uint tileIndex = inputMesh.vertexID >> 2;

    if (tileIndex >= 1)
        return output;

    int quadVertexID = inputMesh.vertexID % 4;

    float2 vertPos = float2(quadVertexID & 1, (quadVertexID >> 1) & 1);

    uint materialBatchGPUKey = UNITY_GET_INSTANCE_ID(inputMesh);
    uint currentMaterialKey = GetCurrentMaterialGPUKey(materialBatchGPUKey);

    output.positionCS.xy = vertPos * 2 - 1;
    output.positionCS.w = 1;
    output.positionCS.z = Visibility::PackDepthMaterialKey(materialBatchGPUKey);
    output.currentMaterialKey = currentMaterialKey;
    output.lightAndMaterialFeatures = GetShaderFeatureMask();
#endif

    return output;
}

void Frag(Varyings packedInput, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    uint visibilityIndex = (uint)packedInput.positionCS.y * GetDispatchWidth() + (uint)packedInput.positionCS.x;
    if (visibilityIndex >= GetMaximumSamplesCount())
    {
        outColor = float4(0,0,0,0);
        return;
    }

    uint3 packedData = _VisOITBuffer.Load3((visibilityIndex * 3) << 2);
    uint2 pixelCoordinate;
    float depthValue;
    Visibility::VisibilityData visibilityData;
    VisibilityOIT::UnpackVisibilityData(packedData, visibilityData, pixelCoordinate, depthValue);
    float2 pixelCoordinateCS = pixelCoordinate + 0.5;

    VBufferDeferredMaterialFragmentData fragmentData = BootstrapDeferredMaterialFragmentShader(
        float4((float2)pixelCoordinateCS.xy, 0.0, 1.0), packedInput.currentMaterialKey, visibilityData, depthValue, true/*custom depth value*/);

    if (!fragmentData.valid)
    {
        //TODO: implement material depth key: this will solve this issue by first writting the corresponding material depth key value from visibility, then using depth comparison.
        outColor = float4(0,0,0,0);
        clip(-1);
        return;
    }

    //Sampling of deferred material has been done.
    //Now perform forward lighting.
    FragInputs input = fragmentData.fragInputs;
    float3 V = fragmentData.V;

    int2 tileCoord = (float2)input.positionSS.xy / GetTileSize();
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, depthValue, UNITY_MATRIX_I_VP, GetWorldToViewMatrix(), tileCoord);

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    float3 colorVariantColor = 0;
    uint featureFlags = packedInput.lightAndMaterialFeatures & LIGHT_FEATURE_MASK_FLAGS_TRANSPARENT;

    LightLoopOutput lightLoopOutput;
    LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

    float3 diffuseLighting =  lightLoopOutput.diffuseLighting;
    float3 specularLighting = lightLoopOutput.specularLighting;

    diffuseLighting *= GetCurrentExposureMultiplier();
    specularLighting *= GetCurrentExposureMultiplier();

    //outColor.rgb = diffuseLighting + specularLighting;
    outColor.rgb = diffuseLighting + specularLighting;
    outColor.a = saturate(builtinData.opacity);
}
