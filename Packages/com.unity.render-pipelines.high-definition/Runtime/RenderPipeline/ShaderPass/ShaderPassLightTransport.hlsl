#if SHADERPASS != SHADERPASS_LIGHT_TRANSPORT
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/MetaPass.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsToPS Vert(AttributesMesh inputMesh)
{
    VaryingsToPS output = (VaryingsToPS)0;

    UNITY_SETUP_INSTANCE_ID(inputMesh);
    UNITY_TRANSFER_INSTANCE_ID(inputMesh, output.vmesh);

#if defined(HAVE_MESH_MODIFICATION)
    inputMesh = ApplyMeshModification(inputMesh, _TimeParameters.xyz);
#endif

    output.vmesh.positionCS = UnityMetaVertexPosition(inputMesh.positionOS, inputMesh.uv1.xy, inputMesh.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);

#ifdef VARYINGS_NEED_POSITION_WS
    output.vmesh.positionRWS = TransformObjectToWorld(inputMesh.positionOS);
#endif

#ifdef VARYINGS_NEED_TANGENT_TO_WORLD
    // Normal is required for triplanar mapping
    output.vmesh.normalWS = TransformObjectToWorldNormal(inputMesh.normalOS);
    // Not required but assign to silent compiler warning
    output.vmesh.tangentWS = float4(1.0, 0.0, 0.0, 0.0);
#endif

#ifdef EDITOR_VISUALIZATION
    // originally, input uv0 was scaled using the main texture's ST. this does not seem necessary for HD, but if it is, scaling would need to be applied before generating vizUV.
    float2 vizUV = 0;
    float4 lightCoord = 0;
    UnityEditorVizData(inputMesh.positionOS.xyz, inputMesh.uv0.xy, inputMesh.uv1.xy, inputMesh.uv2.xy, vizUV, lightCoord);
#endif

#ifdef VARYINGS_NEED_TEXCOORD0
    output.vmesh.texCoord0 = inputMesh.uv0;
#endif
#ifdef VARYINGS_NEED_TEXCOORD1
#ifdef EDITOR_VISUALIZATION
    output.vmesh.texCoord1.xy = vizUV.xy;
#else
    output.vmesh.texCoord1 = inputMesh.uv1;
#endif
#endif
#ifdef VARYINGS_NEED_TEXCOORD2
    // texCoord2 = lightCoord
#ifdef EDITOR_VISUALIZATION
    output.vmesh.texCoord2.xy = lightCoord.xy;
#else
    output.vmesh.texCoord2 = inputMesh.uv2;
#endif
#endif
#ifdef VARYINGS_NEED_TEXCOORD3
#ifdef EDITOR_VISUALIZATION
    output.vmesh.texCoord3.xy = lightCoord.zw;
#else
    output.vmesh.texCoord3 = inputMesh.uv3;
#endif
#endif
#ifdef VARYINGS_NEED_COLOR
    output.vmesh.color = inputMesh.color;
#endif

    return PackVaryingsToPS(output);
}

float4 Frag(PackedVaryingsToPS packedInput) : SV_Target
{
    FragInputs input = UnpackVaryingsToFragInputs(packedInput);

    // input.positionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, input.positionSS.z, input.positionSS.w, input.positionRWS);

#ifdef VARYINGS_NEED_POSITION_WS
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);
#else
    // Unused
    float3 V = float3(1.0, 1.0, 1.0); // Avoid the division by 0
#endif

    SurfaceData surfaceData;
    BuiltinData builtinData;
    GetSurfaceAndBuiltinData(input, V, posInput, surfaceData, builtinData);

    // no debug apply during light transport pass

    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);
    LightTransportData lightTransportData = GetLightTransportData(surfaceData, builtinData, bsdfData);

    // This shader is call two times. Once for getting emissiveColor, the other time to get diffuseColor
    // We use unity_MetaFragmentControl to make the distinction.
    float4 res = float4(0.0, 0.0, 0.0, 1.0);

    UnityMetaInput metaInput;
    metaInput.Albedo = lightTransportData.diffuseColor.rgb;
    metaInput.Emission = lightTransportData.emissiveColor;
#ifdef EDITOR_VISUALIZATION
    metaInput.VizUV = input.texCoord1.xy;
    metaInput.LightCoord = float4(input.texCoord2.xy, input.texCoord3.xy);
#endif
    res = UnityMetaFragment(metaInput);

    return res;
}
