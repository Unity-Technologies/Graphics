#ifndef UNIVERSAL_FULLSCREEN_INCLUDED
#define UNIVERSAL_FULLSCREEN_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

uniform float4 _BlitScaleBias;

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

struct Attributes2
{
#if SHADER_API_GLES
    float4 positionOS : POSITION;
    float2 uv         : TEXCOORD0;
#else
    uint vertexID     : SV_VertexID;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings FullscreenVert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if _USE_DRAW_PROCEDURAL
    output.positionCS = GetQuadVertexPosition(input.vertexID);
    output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
    output.uv = GetQuadTexCoord(input.vertexID) * _ScaleBias.xy + _ScaleBias.zw;
#else
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
#endif

    return output;
}

Varyings FullscreenTriangleVert(Attributes2 input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if SHADER_API_GLES
    float4 pos = input.positionOS;
    float2 uv  = input.uv;
#else
    float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
    float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
#endif

    output.positionCS = pos;
    output.uv         = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;

    return output;
}

Varyings Vert(Attributes input)
{
    return FullscreenVert(input);
}

#endif
