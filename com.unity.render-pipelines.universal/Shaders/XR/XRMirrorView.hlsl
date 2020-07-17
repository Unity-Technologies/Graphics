#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

#if SRC_TEXTURE2D_X_ARRAY
TEXTURE2D_ARRAY(_SourceTex);
#else
TEXTURE2D(_SourceTex);
#endif

SamplerState sampler_LinearClamp;
uniform float4 _ScaleBias;
uniform float4 _ScaleBiasRt;
uniform uint _SourceTexArraySlice;
uniform uint _SRGBRead;

struct Attributes
{
    uint vertexID : SV_VertexID;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
};

Varyings VertQuad(Attributes input)
{
    Varyings output;
    output.positionCS = GetQuadVertexPosition(input.vertexID) * float4(_ScaleBiasRt.x, _ScaleBiasRt.y, 1, 1) + float4(_ScaleBiasRt.z, _ScaleBiasRt.w, 0, 0);
    output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
    output.texcoord = GetQuadTexCoord(input.vertexID) * _ScaleBias.xy + _ScaleBias.zw;
    return output;
}

float4 FragBilinear(Varyings input) : SV_Target
{
    float4 outColor;

#if SRC_TEXTURE2D_X_ARRAY
    outColor = SAMPLE_TEXTURE2D_ARRAY(_SourceTex, sampler_LinearClamp, input.texcoord.xy, _SourceTexArraySlice);
#else
    outColor = SAMPLE_TEXTURE2D(_SourceTex, sampler_LinearClamp, input.texcoord.xy);
#endif

    if (_SRGBRead)
        outColor = SRGBToLinear(outColor);

    return outColor;
}
