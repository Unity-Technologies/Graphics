#ifndef LIGHTWEIGHT_PASS_DEPTH_ONLY_INCLUDED
#define LIGHTWEIGHT_PASS_DEPTH_ONLY_INCLUDED

#include "LWRP/ShaderLibrary/Core.hlsl"
#include "LWRP/ShaderLibrary/InputSurface.hlsl"

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

    o.uv = TransformMainTextureCoord(v.texcoord);
    o.clipPos = TransformObjectToHClip(v.position.xyz);
    return o;
}

half4 DepthOnlyFragment(VertexOutput IN) : SV_TARGET
{
    Alpha(MainTexture(IN.uv).a);
    return 0;
}
#endif
