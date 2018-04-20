#ifndef LIGHTWEIGHT_DEPTH_COPY_INCLUDED
#define LIGHTWEIGHT_DEPTH_COPY_INCLUDED

#include "LWRP/ShaderLibrary/Core.hlsl"

struct VertexInput
{
    float4 vertex   : POSITION;
    float2 uv       : TEXCOORD0;
};

struct VertexOutput
{
    float4 position : SV_POSITION;
    float2 uv       : TEXCOORD0;
};

VertexOutput vert(VertexInput i)
{
    VertexOutput o;
    o.uv = i.uv;
    o.position = TransformObjectToHClip(i.vertex.xyz);
    return o;
}

#if MSAA_DEPTH
    Texture2DMS<float> _CameraDepthTexture;
    float _SampleCount;
    float4 _CameraDepthTexture_TexelSize;
#else
    TEXTURE2D(_CameraDepthTexture);
    SAMPLER(sampler_CameraDepthTexture);
#endif

float SampleDepth(float2 uv)
{
#if MSAA_DEPTH
    int2 coord = int2(uv * _CameraDepthTexture_TexelSize.zw);
    int samples = (int)_SampleCount;
    #if UNITY_REVERSED_Z
        float outDepth = 1.0;
        #define DEPTH_OP min
    #else
        float outDepth = 0.0;
        #define DEPTH_OP max
    #endif

    for (int i = 0; i < samples; ++i)
        outDepth = DEPTH_OP(LOAD_TEXTURE2D_MSAA(_CameraDepthTexture, coord, i), outDepth);

    return outDepth;
#else
    return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
#endif
}

#endif // LIGHTWEIGHT_DEPTH_COPY_INCLUDED
