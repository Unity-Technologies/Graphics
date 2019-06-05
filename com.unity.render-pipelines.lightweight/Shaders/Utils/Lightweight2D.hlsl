#ifndef LIGHTWEIGHT_FALLBACK_2D_INCLUDED
#define LIGHTWEIGHT_FALLBACK_2D_INCLUDED

struct Attributes
{
    float4 positionOS       : POSITION;
    float2 uv               : TEXCOORD0;
};

struct Varyings
{
    float2 uv        : TEXCOORD0;
    float4 vertex : SV_POSITION;

    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    output.vertex = vertexInput.positionCS;
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    return output;
}

half4 frag(Varyings input) : SV_Target
{
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    half2 uv = input.uv;
    half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
    half3 color = texColor.rgb * _BaseColor.rgb;
    half alpha = texColor.a * _BaseColor.a;
    AlphaDiscard(alpha, _Cutoff);

#ifdef _ALPHAPREMULTIPLY_ON
    color *= alpha;
#endif
    return half4(color, alpha);
}

#endif
