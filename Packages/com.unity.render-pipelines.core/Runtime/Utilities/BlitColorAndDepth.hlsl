#ifndef UNITY_CORE_BLIT_COLOR_AND_DEPTH_INCLUDED
#define UNITY_CORE_BLIT_COLOR_AND_DEPTH_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScaling.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

TEXTURE2D (_BlitTexture);
TEXTURE2D (_InputDepthTexture);
uniform float4 _BlitScaleBias;
uniform float _BlitMipLevel;

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

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

    float4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
    float2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);

    output.positionCS = pos;
    output.texcoord   = DYNAMIC_SCALING_APPLY_SCALEBIAS(uv);

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
