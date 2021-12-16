#ifndef UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED
#define UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 position     : POSITION;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthOnlyVertex
(
#ifdef BRG_DRAW_PROCEDURAL
    uint vertexID : SV_VertexID
#else
    Attributes input
#endif
)
{
    Varyings output = (Varyings)0;

#ifdef BRG_DRAW_PROCEDURAL
    float3 positionOS = LoadBRGProcedural_Position(vertexID);
    float2 uv0 = LoadBRGProcedural_UV0(vertexID);
#else
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionOS = input.position.xyz;
    float2 uv0 = input.texcoord;
#endif

    output.uv = TRANSFORM_TEX(uv0, _BaseMap);
    output.positionCS = TransformObjectToHClip(positionOS);
    return output;
}

half DepthOnlyFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
    return input.positionCS.z;
}
#endif
