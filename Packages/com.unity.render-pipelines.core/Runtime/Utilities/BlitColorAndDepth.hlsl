#ifndef UNITY_CORE_BLIT_COLOR_AND_DEPTH_INCLUDED
#define UNITY_CORE_BLIT_COLOR_AND_DEPTH_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScaling.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"

#pragma multi_compile _ _MSAA_2X _MSAA_4X _MSAA_8X

#if defined(_MSAA_2X)
# define SAMPLE_COUNT 2
#elif defined (_MSAA_4X)
# define SAMPLE_COUNT 4
#elif defined(_MSAA_8X)
# define SAMPLE_COUNT 8
#else
# define SAMPLE_COUNT 1
#endif

#if UNITY_REVERSED_Z
    #define DEPTH_DEFAULT_VALUE 1.0
    #define DEPTH_OP min
#else
    #define DEPTH_DEFAULT_VALUE 0.0
    #define DEPTH_OP max
#endif

TEXTURE2D_X(_BlitTexture);
TEXTURE2D(_InputDepthTexture);
TEXTURE2D_X(_InputDepthTextureXR);
TEXTURE2D_X_MSAA(float4, _InputDepthTextureXR_MS);

uniform float4 _BlitScaleBias;
uniform float _BlitMipLevel;
uniform float2 _SourceResolution;

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

    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord.xy, _BlitMipLevel);
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
    pd.color = SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, input.texcoord.xy, _BlitMipLevel);
    pd.depth = SAMPLE_TEXTURE2D_LOD(_InputDepthTexture, sampler_PointClamp, input.texcoord.xy, _BlitMipLevel).x;
    return pd;
}

float SampleDepth(float2 uv)
{
    uint2 pixelCoords = uint2(uv * _SourceResolution);
#if SAMPLE_COUNT == 1
    return LOAD_TEXTURE2D_X_LOD(_InputDepthTextureXR, pixelCoords, _BlitMipLevel).x;
#else
    float outDepth = DEPTH_DEFAULT_VALUE;

    UNITY_UNROLL
    for (int i = 0; i < SAMPLE_COUNT; ++i)
        outDepth = DEPTH_OP(LOAD_TEXTURE2D_X_MSAA(_InputDepthTextureXR_MS, pixelCoords, i), outDepth).x;
    return outDepth;
#endif
}

float FragDepthOnly(Varyings input) : SV_Depth
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    return SampleDepth(input.texcoord.xy);
}

#endif // UNITY_CORE_BLIT_COLOR_AND_DEPTH_INCLUDED
