#if SHADERPASS != SHADERPASS_DYNAMIC_GIDATA_GEN
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsToPS output;

    UNITY_SETUP_INSTANCE_ID(inputMesh);
    UNITY_TRANSFER_INSTANCE_ID(inputMesh, output.vmesh);

    // Output UV coordinate in vertex shader
    float2 uv = float2(0.0, 0.0);

    uv = inputMesh.uv0.xy;

    uv.y = 1.0f - uv.y;
    // OpenGL right now needs to actually use the incoming vertex position
    // so we create a fake dependency on it here that haven't any impact.
    output.vmesh.positionCS = float4(uv * 2.0 - 1.0, 0.0f, 1.0f);
#ifdef VARYINGS_NEED_POSITION_WS
    output.vmesh.positionRWS = TransformObjectToWorld(inputMesh.positionOS);
#endif

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    // Normal is required for triplanar mapping
    output.vmesh.normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);
    // Not required but assign to silent compiler warning
    output.vmesh.tangentWS = float4(1.0, 0.0, 0.0, 0.0);
#endif

#ifdef VARYINGS_NEED_TEXCOORD0
    output.vmesh.texCoord0 = inputMesh.uv0;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
    output.vmesh.texCoord1 = inputMesh.uv1;
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    output.vmesh.texCoord2 = inputMesh.uv2;
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
    output.vmesh.texCoord3 = inputMesh.uv3;
#endif
#ifdef VARYINGS_NEED_COLOR
    output.vmesh.color = inputMesh.color;
#endif

    return PackVaryingsToPS(output);
}

void Frag(  PackedVaryingsToPS packedInput,
            out float4 outAlbedo : SV_Target0
    )
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    // The following is way too overkill but simpler.
    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    outAlbedo.xyz = surfaceData.baseColor;
}
