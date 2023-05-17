#ifndef UNITY_CORE_BLIT_INCLUDED
#define UNITY_CORE_BLIT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"

TEXTURE2D_X(_BlitTexture);
TEXTURECUBE(_BlitCubeTexture);

uniform float4 _BlitScaleBias;
uniform float4 _BlitScaleBiasRt;
uniform float _BlitMipLevel;
uniform float2 _BlitTextureSize;
uniform uint _BlitPaddingSize;
uniform int _BlitTexArraySlice;
uniform float4 _BlitDecodeInstructions;

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
    output.texcoord   = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;

    return output;
}

Varyings VertQuad(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float4 pos = GetQuadVertexPosition(input.vertexID);
    float2 uv  = GetQuadTexCoord(input.vertexID);

    output.positionCS    = pos * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
    output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
    output.texcoord      = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
    return output;
}

Varyings VertQuadPadding(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float2 scalePadding = ((_BlitTextureSize + float(_BlitPaddingSize)) / _BlitTextureSize);
    float2 offsetPadding = (float(_BlitPaddingSize) / 2.0) / (_BlitTextureSize + _BlitPaddingSize);

    float4 pos = GetQuadVertexPosition(input.vertexID);
    float2 uv  = GetQuadTexCoord(input.vertexID);

    output.positionCS = pos * float4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + float4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
    output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
    output.texcoord = uv;
    output.texcoord = (output.texcoord - offsetPadding) * scalePadding;
    output.texcoord = output.texcoord * _BlitScaleBias.xy + _BlitScaleBias.zw;
    return output;
}

float4 FragBlit(Varyings input, SamplerState s)
{
#if defined(USE_TEXTURE2D_X_AS_ARRAY) && defined(BLIT_SINGLE_SLICE)
    return SAMPLE_TEXTURE2D_ARRAY_LOD(_BlitTexture, s, input.texcoord.xy, _BlitTexArraySlice, _BlitMipLevel);
#endif

    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, s, input.texcoord.xy, _BlitMipLevel);
}

float4 FragNearest(Varyings input) : SV_Target
{
    return FragBlit(input, sampler_PointClamp);
}

float4 FragBilinear(Varyings input) : SV_Target
{
    return FragBlit(input, sampler_LinearClamp);
}

float4 FragBilinearRepeat(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.texcoord.xy;
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
}

float4 FragNearestRepeat(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.texcoord.xy;
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointRepeat, uv, _BlitMipLevel);
}

float4 FragOctahedralBilinearRepeat(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = RepeatOctahedralUV(input.texcoord.x, input.texcoord.y);
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
}

float4 FragOctahedralProject(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 UV = saturate(input.texcoord);
    float3 dir = UnpackNormalOctQuadEncode(2.0f*UV - 1.0f);
    return float4(SAMPLE_TEXTURECUBE_LOD(_BlitCubeTexture, sampler_LinearRepeat, dir, _BlitMipLevel).rgb, 1);
}

float4 FragOctahedralProjectNearestRepeat(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = RepeatOctahedralUV(input.texcoord.x, input.texcoord.y);
    float3 dir = UnpackNormalOctQuadEncode(2.0f * uv - 1.0f);
#ifdef BLIT_DECODE_HDR
    return float4(DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(_BlitCubeTexture, sampler_PointRepeat, dir, _BlitMipLevel), _BlitDecodeInstructions), 1);
#else
    return float4(SAMPLE_TEXTURECUBE_LOD(_BlitCubeTexture, sampler_PointRepeat, dir, _BlitMipLevel).rgb, 1);
#endif
}

float4 FragOctahedralProjectBilinearRepeat(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = RepeatOctahedralUV(input.texcoord.x, input.texcoord.y);
    float3 dir = UnpackNormalOctQuadEncode(2.0f * uv - 1.0f);
#ifdef BLIT_DECODE_HDR
    return float4(DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(_BlitCubeTexture, sampler_LinearRepeat, dir, _BlitMipLevel), _BlitDecodeInstructions), 1);
#else
    return float4(SAMPLE_TEXTURECUBE_LOD(_BlitCubeTexture, sampler_LinearRepeat, dir, _BlitMipLevel).rgb, 1);
#endif
}

// 8-bit single channel sampling/format conversions

float4 FragOctahedralProjectLuminance(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 UV = saturate(input.texcoord);
    float3 dir = UnpackNormalOctQuadEncode(2.0f*UV - 1.0f);
    // sRGB/Rec.709
    return Luminance(SAMPLE_TEXTURECUBE_LOD(_BlitCubeTexture, sampler_LinearRepeat, dir, _BlitMipLevel)).xxxx;
}

float4 FragOctahedralProjectRedToRGBA(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 UV = saturate(input.texcoord);
    float3 dir = UnpackNormalOctQuadEncode(2.0f*UV - 1.0f);
    return SAMPLE_TEXTURECUBE_LOD(_BlitCubeTexture, sampler_LinearRepeat, dir, _BlitMipLevel).rrrr;
}

float4 FragOctahedralProjectAlphaToRGBA(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 UV = saturate(input.texcoord);
    float3 dir = UnpackNormalOctQuadEncode(2.0f*UV - 1.0f);
    return SAMPLE_TEXTURECUBE_LOD(_BlitCubeTexture, sampler_LinearRepeat, dir, _BlitMipLevel).aaaa;
}

float4 FragBilinearLuminance(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.texcoord.xy;
    // sRGB/Rec.709
    return Luminance(SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel)).xxxx;
}

float4 FragBilinearRedToRGBA(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.texcoord.xy;
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel).rrrr;
}

float4 FragBilinearAlphaToRGBA(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float2 uv = input.texcoord.xy;
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel).aaaa;
}

#endif //UNITY_CORE_BLIT_INCLUDED
