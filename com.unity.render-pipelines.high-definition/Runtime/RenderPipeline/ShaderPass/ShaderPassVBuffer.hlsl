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
    uint primitiveID : SV_PrimitiveID,
    out uint VBuffer0 : SV_Target0,
    out uint VBuffer1 : SV_Target1,
    out float MaterialDepth : SV_Target2)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

    // Fetch triangle ID (32 bits)
    uint triangleId = primitiveID;

    // Fetch the Geometry ID (16 bits compressed)
    uint geometryId = _InstanceId;

    // Fetch the Material ID
    uint materialId = _MaterialId;

    // Write the VBuffer
    VBuffer0 = triangleId;
    VBuffer1 = geometryId & 0xffff;
    MaterialDepth = materialId;
}
