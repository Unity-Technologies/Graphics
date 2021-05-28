#ifndef UNITY_CORE_BLIT_COLOR_AND_DEPTH_INCLUDED
#define UNITY_CORE_BLIT_COLOR_AND_DEPTH_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

TEXTURE2D (_BlitTexture);
TEXTURE2D (_InputDepthTexture);
SamplerState sampler_PointClamp;
SamplerState sampler_LinearClamp;
uniform float4 _BlitScaleBias;
uniform float _BlitMipLevel;

#if SHADER_API_GLES
struct Attributes
{
    float4 positionOS       : POSITION;
    float2 uv               : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
#else
struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
#endif

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if SHADER_API_GLES
    float4 pos = input.positionOS;
    float2 uv  = input.uv;
#else
    float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
    float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
#endif

    output.positionCS = pos;
    output.texcoord   = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
    return output;
}

float4 FragColorOnly(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    return SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord.xy, _BlitMipLevel);
}

struct PixelData
{
    float4 color : SV_Target;
    float  depth : SV_Depth;
};

PixelData FragColorAndDepth(Varyings input)
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    PixelData pd;
    pd.color = SAMPLE_TEXTURE2D_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord.xy, _BlitMipLevel);
    pd.depth = SAMPLE_TEXTURE2D_LOD(_InputDepthTexture, sampler_PointClamp, input.texcoord.xy, _BlitMipLevel).x;
    return pd;
}

#endif // UNITY_CORE_BLIT_COLOR_AND_DEPTH_INCLUDED
