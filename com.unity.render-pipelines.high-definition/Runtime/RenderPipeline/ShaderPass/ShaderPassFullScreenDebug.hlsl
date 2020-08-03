#if (SHADERPASS != SHADERPASS_FULLSCREEN_DEBUG)
#error SHADERPASS_is_not_correctly_define
#endif

#define DEBUG_DISPLAY
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/FullScreenDebug.hlsl"

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = VertMesh(inputMesh);
    return PackVaryingsType(varyingsType);
}

#ifdef TESSELLATION_ON

PackedVaryingsToPS VertTesselation(VaryingsToDS input)
{
    VaryingsToPS output;
    output.vmesh = VertMeshTesselation(input.vmesh);
    return PackVaryingsToPS(output);
}

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/TessellationShare.hlsl"

#endif // TESSELLATION_ON

#if !defined(_DEPTHOFFSET_ON)
[earlydepthstencil] // quad overshading debug mode writes to UAV
#endif
void Frag(PackedVaryingsToPS packedInput
#if !defined(SHADER_API_METAL) // Metal does not support SV_PrimitiveID
        , uint primitiveID : SV_PrimitiveID
#endif
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);

    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS.xyz);

    if (_DebugFullScreenMode == FULLSCREENDEBUGMODE_QUAD_OVERDRAW)
    {
#if !defined(SHADER_API_METAL) // Metal does not support SV_PrimitiveID
        IncrementQuadOverdrawCounter(posInput.positionSS.xy, primitiveID);
#endif
    }
}
