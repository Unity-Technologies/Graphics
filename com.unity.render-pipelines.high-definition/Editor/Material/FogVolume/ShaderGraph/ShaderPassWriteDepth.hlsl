#if SHADERPASS != SHADERPASS_FOGVOLUME_WRITE_DEPTH
#error SHADERPASS_is_not_correctly_define
#endif

RW_TEXTURE2D_X(float, _FogVolumeDepth) : register(u2);

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(varyingsType);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplayMaterial.hlsl"

void Frag(PackedVaryingsToPS packedInput, out float4 outColor : SV_Target0)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);
    
    float3 positionVS = TransformWorldToView(input.positionRWS);
    // if (positionVS.z <= 0) // it's okay to do this because we only support convex meshes
    // {
    //     _FogVolumeDepth[COORD_TEXTURE2D_X(posInput.positionSS.xy)] = 0;
    // }
    // TODO: check is front face and disable ZClip + CullMode to avoid camera clipping issues
    if (input.isFrontFace)
    {
        _FogVolumeDepth[COORD_TEXTURE2D_X(posInput.positionSS.xy)] = length(input.positionRWS);
    }
    // else if (positionVS.z > 0)
    // {
    //     _FogVolumeDepth[COORD_TEXTURE2D_X(posInput.positionSS.xy)] = 0.1;
    // }
}
