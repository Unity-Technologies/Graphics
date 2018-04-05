#ifndef LIGHTWEIGHT_PASS_DEPTH_ONLY_INCLUDED
#define LIGHTWEIGHT_PASS_DEPTH_ONLY_INCLUDED

#include "LWRP/ShaderLibrary/Core.hlsl"

struct VertexInput
{
    float4 position     : POSITION;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float2 uv           : TEXCOORD0;
    float4 clipPos      : SV_POSITION;
};

VertexOutput DepthOnlyVertex(VertexInput v)
{
    VertexOutput o = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);

    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    o.clipPos = TransformObjectToHClip(v.position.xyz);
    return o;
}

half4 DepthOnlyFragment(VertexOutput IN) : SV_TARGET
{
    Alpha(SampleAlbedoAlpha(IN.uv, TEXTURE2D_PARAM(_MainTex, sampler_MainTex)).a, _Color, _Cutoff);
    return 0;
}
#endif
