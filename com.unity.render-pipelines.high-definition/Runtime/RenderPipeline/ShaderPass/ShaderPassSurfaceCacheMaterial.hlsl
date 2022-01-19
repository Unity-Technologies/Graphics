#if (SHADERPASS != SHADERPASS_SURFACE_CACHE_MATERIAL)
#error SHADERPASS_is_not_correctly_define
#endif

#define VARYINGS_NEED_TANGENT_TO_WORLD
#define VARYINGS_NEED_POSITION_WS
#define VARYINGS_NEED_TEXCOORD0

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"


PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;

    float2 uv1 =  inputMesh.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
    uv1 = (uv1 * 2.0f) - 1.0f;
    varyingsType.vmesh.positionCS = float4(uv1, 0, 1);
    varyingsType.vmesh.positionRWS = TransformObjectToWorld(inputMesh.positionOS);
    varyingsType.vmesh.normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);
    varyingsType.vmesh.texCoord0 = inputMesh.uv0;
    return PackVaryingsType(varyingsType);
}



void Frag(PackedVaryingsToPS packedInput
    , out float4 outColor : SV_Target0
    , out float4 outNormal : SV_Target1)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    input.positionSS = TransformWorldToHClip(GetAbsolutePositionWS(input.positionRWS));

    uint2 tileIndex = uint2(input.positionSS.xy) / GetTileSize();

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz, tileIndex);

    #ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
    #else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
    #endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    outColor = float4(0.0, 0.0, 0.0, 0.0);


    #ifdef _SURFACE_TYPE_TRANSPARENT
    uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_TRANSPARENT;
    #else
    uint featureFlags = LIGHT_FEATURE_MASK_FLAGS_OPAQUE;
    #endif
    LightLoopOutput lightLoopOutput;
    LightLoop(V, posInput, preLightData, bsdfData, builtinData, featureFlags, lightLoopOutput);

    // Alias
    //float3 diffuseLighting = lightLoopOutput.diffuseLighting;
    outColor = float4(surfaceData.baseColor, 1.0f);
    outNormal = float4(surfaceData.normalWS, 1.0f);
}
