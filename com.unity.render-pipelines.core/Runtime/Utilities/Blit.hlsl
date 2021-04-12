#ifndef UNITY_CORE_BLIT_INCLUDED
#define UNITY_CORE_BLIT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

TEXTURE2D_X(_BlitTexture);
TEXTURECUBE(_BlitCubeTexture);
SamplerState sampler_PointClamp;
SamplerState sampler_LinearClamp;
SamplerState sampler_PointRepeat;
SamplerState sampler_LinearRepeat;
uniform real4 _BlitScaleBias;
uniform real4 _BlitScaleBiasRt;
uniform real _BlitMipLevel;
uniform real2 _BlitTextureSize;
uniform uint _BlitPaddingSize;
uniform int _BlitTexArraySlice;

#if SHADER_API_GLES
struct Attributes
{
    real4 positionCS       : POSITION;
    real2 uv               : TEXCOORD0;
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
    real4 positionCS : SV_POSITION;
    real2 texcoord   : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings Vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if SHADER_API_GLES
    real4 pos = input.positionCS;
    real2 uv  = input.uv;
#else
    real4 pos = GetFullScreenTriangleVertexPosition(input.vertexID);
    real2 uv  = GetFullScreenTriangleTexCoord(input.vertexID);
#endif

    output.positionCS = pos;
    output.texcoord   = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
    return output;
}

Varyings VertQuad(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if SHADER_API_GLES
    real4 pos = input.positionCS;
    real2 uv  = input.uv;
#else
    real4 pos = GetQuadVertexPosition(input.vertexID);
    real2 uv  = GetQuadTexCoord(input.vertexID);
#endif

    output.positionCS    = pos * real4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + real4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
    output.positionCS.xy = output.positionCS.xy * real2(2.0f, -2.0f) + real2(-1.0f, 1.0f); //convert to -1..1
    output.texcoord      = uv * _BlitScaleBias.xy + _BlitScaleBias.zw;
    return output;
}

Varyings VertQuadPadding(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    real2 scalePadding = ((_BlitTextureSize + real(_BlitPaddingSize)) / _BlitTextureSize);
    real2 offsetPaddding = (real(_BlitPaddingSize) / 2.0) / (_BlitTextureSize + _BlitPaddingSize);

#if SHADER_API_GLES
    real4 pos = input.positionCS;
    real2 uv  = input.uv;
#else
    real4 pos = GetQuadVertexPosition(input.vertexID);
    real2 uv  = GetQuadTexCoord(input.vertexID);
#endif

    output.positionCS = pos * real4(_BlitScaleBiasRt.x, _BlitScaleBiasRt.y, 1, 1) + real4(_BlitScaleBiasRt.z, _BlitScaleBiasRt.w, 0, 0);
    output.positionCS.xy = output.positionCS.xy * real2(2.0f, -2.0f) + real2(-1.0f, 1.0f); //convert to -1..1
    output.texcoord = uv;
    output.texcoord = (output.texcoord - offsetPaddding) * scalePadding;
    output.texcoord = output.texcoord * _BlitScaleBias.xy + _BlitScaleBias.zw;
    return output;
}

real4 Frag(Varyings input, SamplerState s)
{
#if defined(USE_TEXTURE2D_X_AS_ARRAY) && defined(BLIT_SINGLE_SLICE)
    return SAMPLE_TEXTURE2D_ARRAY_LOD(_BlitTexture, s, input.texcoord.xy, _BlitTexArraySlice, _BlitMipLevel);
#endif

    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, s, input.texcoord.xy, _BlitMipLevel);
}

real4 FragNearest(Varyings input) : SV_Target
{
    return Frag(input, sampler_PointClamp);
}

real4 FragBilinear(Varyings input) : SV_Target
{
    return Frag(input, sampler_LinearClamp);
}

real4 FragBilinearRepeat(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    real2 uv = input.texcoord.xy;
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
}

real4 FragNearestRepeat(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    real2 uv = input.texcoord.xy;
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointRepeat, uv, _BlitMipLevel);
}

real4 FragOctahedralBilinearRepeat(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    real u = input.texcoord.x;
    real v = input.texcoord.y;

    real2 uv;
    if (u < 0.0f)
    {
        if (v < 0.0f)
            uv = real2(1.0f + u, 1.0f + v);
        else if (v < 1.0f)
            uv = real2(-u, 1.0f - v);
        else
            uv = real2(1.0f + u, v - 1.0f);
    }
    else if (u < 1.0f)
    {
        if (v < 0.0f)
            uv = real2(1.0f - u, -v);
        else if (v < 1.0f)
            uv = real2(u, v);
        else
            uv = real2(1.0f - u, 2.0f - v);
    }
    else
    {
        if (v < 0.0f)
            uv = real2(u - 1.0f, 1.0f + v);
        else if (v < 1.0f)
            uv = real2(2.0f - u, 1.0f - v);
        else
            uv = real2(u - 1.0f, v - 1.0f);
    }

    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);
}

real4 FragOctahedralProject(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    real2 UV = saturate(input.texcoord);
    real3 dir = UnpackNormalOctQuadEncode(2.0f*UV - 1.0f);
    return real4(SAMPLE_TEXTURECUBE_LOD(_BlitCubeTexture, sampler_LinearRepeat, dir, _BlitMipLevel).rgb, 1);
}

real4 FragBilinearLuminance(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    real2 uv = input.texcoord.xy;
    // sRGB/Rec.709
    return Luminance(SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel)).xxxx;
}

real4 FragBilinearRedToRGBA(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    real2 uv = input.texcoord.xy;
    return Luminance(SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel)).rrrr;
}

real4 FragBilinearAlphaToRGBA(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    real2 uv = input.texcoord.xy;
    return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel).aaaa;
}

#endif //UNITY_CORE_BLIT_INCLUDED
