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
#define USE_ARRAY_TEXTURE 1
#endif

#ifdef MSAA_DEPTH
    #ifdef USE_ARRAY_TEXTURE
        Texture2DMSArray<float> _CameraDepthTexture;
    #else
        Texture2DMS<float> _CameraDepthTexture;
    #endif
    float _SampleCount;
    float4 _CameraDepthTexture_TexelSize;
#else
    #ifdef USE_ARRAY_TEXTURE
        TEXTURE2D_ARRAY(_CameraDepthTexture);
    #else
        TEXTURE2D(_CameraDepthTexture);
    #endif
    SAMPLER(sampler_CameraDepthTexture);
#endif

float SampleDepth(float2 uv)
{
#ifdef MSAA_DEPTH
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
        #ifdef USE_ARRAY_TEXTURE
            outDepth = DEPTH_OP(LOAD_TEXTURE2D_ARRAY_MSAA(_CameraDepthTexture, uv, unity_StereoEyeIndex, i), outDepth);
        #else
            outDepth = DEPTH_OP(LOAD_TEXTURE2D_MSAA(_CameraDepthTexture, uv, i), outDepth);
        #endif

    return outDepth;
#else
    #ifdef USE_ARRAY_TEXTURE
        return SAMPLE_TEXTURE2D_ARRAY(_CameraDepthTexture, sampler_CameraDepthTexture, uv, unity_StereoEyeIndex).r;
    #else
        return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
    #endif
#endif
}

float frag(VertexOutput i) : SV_Depth
{
    UNITY_SETUP_INSTANCE_ID(i);
    return SampleDepth(i.uv);
}

#endif // LIGHTWEIGHT_DEPTH_COPY_INCLUDED
