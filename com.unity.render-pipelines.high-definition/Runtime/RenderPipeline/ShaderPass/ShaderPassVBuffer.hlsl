#if (SHADERPASS != SHADERPASS_VBUFFER)
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;

        varyingsType.vmesh = VertMesh(inputMesh);

    return PackVaryingsType(varyingsType);
}


void Frag(PackedVaryingsToPS packedInput,
    out uint VBuffer1 : SV_Target0,
    out float MaterialDepth : SV_Target1)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

    // Fetch triangle ID

    // Write Triangle ID

    // Fetch Material ID ? somehow
    // Write Material ID to Depth <<< Later

}
