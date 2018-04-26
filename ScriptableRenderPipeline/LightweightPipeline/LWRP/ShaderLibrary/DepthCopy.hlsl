#ifndef LIGHTWEIGHT_DEPTH_COPY_INCLUDED
#define LIGHTWEIGHT_DEPTH_COPY_INCLUDED

#include "LWRP/ShaderLibrary/Core.hlsl"

struct VertexInput
{
    float4 vertex   : POSITION;
    float2 uv       : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 position : SV_POSITION;
    float2 uv       : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutput vert(VertexInput i)
{
    VertexOutput o;
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_TRANSFER_INSTANCE_ID(i, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.uv = i.uv;
    o.position = TransformObjectToHClip(i.vertex.xyz);
    return o;
}

#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
#define DEPTH_TEXTURE_MS Texture2DMSArray
#define DEPTH_TEXTURE(name) TEXTURE2D_ARRAY(name)
#define LOAD(uv, sampleIndex) LOAD_TEXTURE2D_ARRAY_MSAA(_CameraDepthTexture, uv, unity_StereoEyeIndex, sampleIndex)
#define SAMPLE(uv) SAMPLE_TEXTURE2D_ARRAY(_CameraDepthTexture, sampler_CameraDepthTexture, uv, unity_StereoEyeIndex).r
#else
#define DEPTH_TEXTURE_MS Texture2DMS
#define DEPTH_TEXTURE(name) TEXTURE2D(name)
#define LOAD(uv, sampleIndex) LOAD_TEXTURE2D_MSAA(_CameraDepthTexture, uv, sampleIndex)
#define SAMPLE(uv) SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv)
#endif

#ifdef _MSAA_DEPTH
    DEPTH_TEXTURE_MS<float> _CameraDepthTexture;
    float _SampleCount;
    float4 _CameraDepthTexture_TexelSize;
#else
    DEPTH_TEXTURE(_CameraDepthTexture);
    SAMPLER(sampler_CameraDepthTexture);
#endif

float SampleDepth(float2 uv)
{
#ifdef _MSAA_DEPTH
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
        outDepth = DEPTH_OP(LOAD(uv, i), outDepth);
    
    return outDepth;
#else
    return SAMPLE(uv);
#endif
}

float frag(VertexOutput i) : SV_Depth
{
    UNITY_SETUP_INSTANCE_ID(i);
    return SampleDepth(i.uv);
}

#endif // LIGHTWEIGHT_DEPTH_COPY_INCLUDED
