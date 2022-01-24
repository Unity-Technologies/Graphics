#if (SHADERPASS != SHADERPASS_SURFACE_CACHE_MATERIAL)
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;

    varyingsType.vmesh = VertMesh(inputMesh);

    float2 lightmapUV =  inputMesh.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
    varyingsType.vmesh.positionCS = float4((lightmapUV - 0.5f) * float2(2.0f, -2.0f), 0, 1);

    return PackVaryingsType(varyingsType);
}



void Frag(PackedVaryingsToPS packedInput
    , out float4 outColor : SV_Target0
    , out float4 outNormal : SV_Target1
    , out float4 outEmissive : SV_Target2
    , out float4 outPosWS : SV_Target3
    , out float outDepth : SV_Depth)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    input.positionSS = TransformWorldToHClip(GetAbsolutePositionWS(input.positionRWS));

    uint2 tileIndex = uint2(input.positionSS.xy) / GetTileSize();

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, tileIndex);

    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);


    outColor = float4(surfaceData.baseColor, 1.0f);
    outNormal = float4(surfaceData.normalWS, 1.0f);
    outEmissive = float4(builtinData.emissiveColor + builtinData.bakeDiffuseLighting, 1.0f);
    outPosWS = float4(input.positionRWS, 1.0f);
    outDepth = input.positionSS.z / input.positionSS.w;
}
