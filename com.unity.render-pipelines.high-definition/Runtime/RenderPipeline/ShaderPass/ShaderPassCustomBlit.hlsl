#if (SHADERPASS != SHADERPASS_CUSTOMBLIT) && (SHADERPASS != SHADERPASS_CUSTOMBLIT_PREVIEW)
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    // initialize to 0 because shader graph code ads extra attributes when screen position is selected as input to custom blit sample node
    ZERO_INITIALIZE(VaryingsType, varyingsType);
    varyingsType.vmesh = TransformBlit(inputMesh);
    return PackVaryingsType(varyingsType);
}

void Frag(PackedVaryingsToPS packedInput, out float4 outColor : SV_Target)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs fragInputs = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    float depth = LoadCameraDepth(fragInputs.positionSS.xy);
    PositionInputs posInputs = GetPositionInput(fragInputs.positionSS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

    float3 V = GetWorldSpaceNormalizeViewDir(posInputs.positionWS);
    SurfaceData surfaceData;
    GetSurfaceData(fragInputs, V, posInputs, surfaceData);
    outColor = surfaceData.output;
}
