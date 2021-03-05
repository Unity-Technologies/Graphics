#ifndef UNIVERSAL_FALLBACK_2D_INCLUDED
#define UNIVERSAL_FALLBACK_2D_INCLUDED

struct Attributes
{
    float4 positionOS       : POSITION;
    half2  uv               : TEXCOORD0;
};

struct Varyings
{
    float4 vertex    : SV_POSITION;
    half2  uv        : TEXCOORD0;
};

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    output.vertex = vertexInput.positionCS;
    output.uv = (half2)TRANSFORM_TEX(input.uv, _BaseMap);


    return output;
}

half4 frag(Varyings input) : SV_Target
{
    half2 uv = input.uv;
    half4 texColor = (half4)SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    half3 color = texColor.rgb * _BaseColor.rgb;
    half alpha = texColor.a * _BaseColor.a;
    AlphaDiscard(alpha, _Cutoff);

#ifdef _ALPHAPREMULTIPLY_ON
    color *= alpha;
#endif
    return half4(color, alpha);
}

#endif
