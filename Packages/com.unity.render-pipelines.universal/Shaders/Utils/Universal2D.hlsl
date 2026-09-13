#ifndef UNIVERSAL_FALLBACK_2D_INCLUDED
#define UNIVERSAL_FALLBACK_2D_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/AlphaBlend.hlsl"

struct Attributes
{
    float4 positionOS       : POSITION;
    float2 uv               : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv        : TEXCOORD0;
    float4 vertex : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    output.vertex = vertexInput.positionCS;
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

    return output;
}

half4 frag(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    half2 uv = input.uv;
    half4 texColor = SampleBaseMap(uv);
    half3 color = texColor.rgb * _BaseColor.rgb;
    half alpha = texColor.a * _BaseColor.a;
    AlphaDiscard(alpha, _Cutoff);
    if (UseAlphaPremultiply())
        color = ApplyAlphaPremultiply(color, alpha);

    return half4(color, alpha);
}

#endif
