#ifndef SDFRP_FULLSCREEN_INCLUDED
#define SDFRP_FULLSCREEN_INCLUDED


#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl"

struct Attributes
{
#if _USE_DRAW_PROCEDURAL
    uint vertexID     : SV_VertexID;
#else
    float4 positionOS : POSITION;
    float2 uv         : TEXCOORD0;
#endif
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 uv         : TEXCOORD0;
};

Varyings FullscreenVert(Attributes input)
{
    Varyings output;

#if _USE_DRAW_PROCEDURAL
    output.positionCS = GetQuadVertexPosition(input.vertexID);
    output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
    output.uv = GetQuadTexCoord(input.vertexID) * _ScaleBias.xy + _ScaleBias.zw;
#else
    output.positionCS = float4(input.positionOS.xyz, 1.0);
    output.uv = input.uv;
#endif

    return output;
}

Varyings Vert(Attributes input)
{
    return FullscreenVert(input);
}

#endif
