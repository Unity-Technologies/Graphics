#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

struct AttributesLensFlare
{
    uint vertexID : SV_VertexID;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VaryingsLensFlare
{
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD0;

    UNITY_VERTEX_OUTPUT_STEREO
};

VaryingsLensFlare vert(AttributesLensFlare input, uint instanceID : SV_InstanceID)
{
    VaryingsLensFlare output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if SHADER_API_GLES
    float4 posPreScale = input.positionCS;
    float2 uv = input.uv;
#else
    float4 posPreScale = float4(2.0f, 2.0f, 1.0f, 1.0f) * GetQuadVertexPosition(input.vertexID) - float4(1.0f, 1.0f, 0.0f, 0.0);
    float2 uv = GetQuadTexCoord(input.vertexID);
    uv.x = 1.0f - uv.x;
#endif

    output.texcoord.xy = uv;

    output.positionCS.xy = posPreScale.xy;
    output.positionCS.z = 1.0f;
    output.positionCS.w = 1.0f;

#ifdef HDRP_FLARE
    output.positionCS.x = (output.positionCS.x + 1.0f) * _RTHandleScale.x - 1.0f;
    output.positionCS.y = (output.positionCS.y - 1.0f) * _RTHandleScale.y + 1.0f;
#endif

    return output;
}

float4 frag(VaryingsLensFlare input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    return float4(1, 0, 0, 1);
}
