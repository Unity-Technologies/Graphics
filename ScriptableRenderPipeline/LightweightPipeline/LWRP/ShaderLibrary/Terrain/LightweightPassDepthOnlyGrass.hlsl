#ifndef LIGHTWEIGHT_PASS_DEPTH_ONLY_INCLUDED
#define LIGHTWEIGHT_PASS_DEPTH_ONLY_INCLUDED

#include "LWRP/ShaderLibrary/Core.hlsl"

struct VertexInput
{
    float4 position     : POSITION;
    half4 color         : COLOR;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float2 uv           : TEXCOORD0;
    half4 color         : TEXCOORD1;
    float4 clipPos      : SV_POSITION;
};

VertexOutput DepthOnlyVertex(VertexInput v)
{
    VertexOutput o = (VertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(v);

    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
    // MeshGrass v.color.a: 1 on top vertices, 0 on bottom vertices
    // _WaveAndDistance.z == 0 for MeshLit
    float waveAmount = v.color.a * _WaveAndDistance.z;
    o.color = TerrainWaveGrass (v.position, waveAmount, v.color);
    o.clipPos = TransformObjectToHClip(v.position.xyz);
    return o;
}

half4 DepthOnlyFragment(VertexOutput IN) : SV_TARGET
{
    Alpha(SampleAlbedoAlpha(IN.uv, TEXTURE2D_PARAM(_MainTex, sampler_MainTex)).a, IN.color, _Cutoff);
    return 0;
}
#endif
