#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

TEXTURE2D_X(_SourceTex);
SamplerState sampler_LinearClamp;
uniform float4 _ScaleBias;
uniform float4 _ScaleBiasRt;
uniform uint _SourceTexArraySlice;

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
#if defined(USE_TEXTURE2D_X_AS_ARRAY)
    return SAMPLE_TEXTURE2D_ARRAY(_SourceTex, sampler_LinearClamp, input.texcoord.xy, _SourceTexArraySlice);
#else
    return SAMPLE_TEXTURE2D(_SourceTex, sampler_LinearClamp, input.texcoord.xy);
#endif
}
