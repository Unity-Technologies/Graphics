#ifndef UNIVERSAL_FULLSCREEN_INCLUDED
#define UNIVERSAL_FULLSCREEN_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#if _USE_DRAW_PROCEDURAL
void GetProceduralQuad(in uint vertexID, out float4 positionCS, out float2 uv)
{
    positionCS = GetQuadVertexPosition(vertexID);
    positionCS.xy = positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
    uv = GetQuadTexCoord(vertexID) * _ScaleBias.xy + _ScaleBias.zw;
}
#endif

struct Attributes
{
#if _USE_DRAW_PROCEDURAL
    uint vertexID     : SV_VertexID;
#else
    float4 positionOS : POSITION;
    float2 uv         : TEXCOORD0;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};


Varyings FullTest(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#if _USE_DRAW_PROCEDURAL
    output.positionCS = GetQuadVertexPosition(input.vertexID);
    output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
    output.uv = GetQuadTexCoord(input.vertexID) * _ScaleBias.xy + _ScaleBias.zw;
#elif _USE_VISIBILITY_MESH
    output.positionCS = float4(input.positionOS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f), UNITY_NEAR_CLIP_VALUE, 1.0f);
    output.uv = output.positionCS.xy;
#else
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
#endif 
    return output; 
}

Varyings FullscreenVert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#if _USE_DRAW_PROCEDURAL
    output.positionCS = GetQuadVertexPosition(input.vertexID);
    output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
    output.uv = GetQuadTexCoord(input.vertexID) * _ScaleBias.xy + _ScaleBias.zw;
#elif _USE_VISIBILITY_MESH
    output.positionCS = float4(input.positionOS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f), UNITY_NEAR_CLIP_VALUE, 1.0f);
    output.uv = float2(1.0f, 1.0f);
#else
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
#endif 

    return output;
}

Varyings Vert(Attributes input)
{
    return FullscreenVert(input);
}

#endif
