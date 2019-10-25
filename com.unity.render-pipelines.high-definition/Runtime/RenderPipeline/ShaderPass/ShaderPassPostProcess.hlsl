#if (SHADERPASS != SHADERPASS_POSTPROCESS) && (SHADERPASS != SHADERPASS_POSTPROCESS_PREVIEW)
#error SHADERPASS_is_not_correctly_define
#endif

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"


PackedVaryingsType Vert(AttributesMesh inputMesh)
{
    VaryingsType varyingsType;
    varyingsType.vmesh = TransformBlit(inputMesh);
    return PackVaryingsType(varyingsType);
}


/*
struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VSOutput
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

VSOutput Vert(Attributes input)
{
    VSOutput output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
    output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID) * _ScreenSize.xy;
    return output;
}

void Frag(VSOutput input, out float4 outColor : SV_Target)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    FragInputs fragInputs;
    PositionInputs posInputs;
    float3 V = float3(0, 0, 0);
    SurfaceData surfaceData;
    fragInputs.texCoord0.xy = input.texcoord;   
    GetSurfaceData(fragInputs, V, posInputs, surfaceData);
    outColor = surfaceData.output;    
}

*/

void Frag(PackedVaryingsToPS packedInput, out float4 outColor : SV_Target)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(packedInput);
    FragInputs fragInputs = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);
    fragInputs.texCoord0.xy *= _ScreenSize.xy;
    PositionInputs posInputs;
    float3 V = float3(0, 0, 0);
    SurfaceData surfaceData;
    GetSurfaceData(fragInputs, V, posInputs, surfaceData);
    outColor = surfaceData.output;
}
