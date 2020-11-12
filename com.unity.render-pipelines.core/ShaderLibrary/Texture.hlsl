#ifndef UNITY_TEXTURE_INCLUDED
#define UNITY_TEXTURE_INCLUDED

#ifdef SHADER_API_GLES
    #define SAMPLERDECL(n) GLES2UnsupportedSamplerState n;
#else
    #define SAMPLERDECL(n) SAMPLER(n);
#endif

struct GLES2UnsupportedSamplerState
{
};

struct UnitySamplerState
{
    SAMPLERDECL(samplerstate)
};

#ifdef SHADER_API_GLES
    #define UnityBuildSamplerStateStruct(n) UnityBuildSamplerStateStructInternal()
#else
    #define UnityBuildSamplerStateStruct(n) UnityBuildSamplerStateStructInternal(n)
#endif

UnitySamplerState UnityBuildSamplerStateStructInternal(SAMPLER(samplerstate))
{
    UnitySamplerState result;
    ASSIGN_SAMPLER(result.samplerstate, samplerstate);
    return result;
}

struct UnityTexture2D
{
    TEXTURE2D(tex);
    SAMPLERDECL(samplerstate)
    float4 texelSize;
    float4 scaleTranslate;
};

#define UnityBuildTexture2DStruct(n) UnityBuildTexture2DStructInternal(TEXTURE2D_ARGS(n, sampler##n), n##_TexelSize, n##_ST)
#define UnityBuildTexture2DStructNoScale(n) UnityBuildTexture2DStructInternal(TEXTURE2D_ARGS(n, sampler_##n), n##_TexelSize)
UnityTexture2D UnityBuildTexture2DStructInternal(TEXTURE2D_PARAM(tex, samplerstate), float4 texelSize, float4 scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f))
{
    UnityTexture2D result;
    result.tex = tex;
    ASSIGN_SAMPLER(result.samplerstate, samplerstate);
    result.texelSize = texelSize;
    result.scaleTranslate = scaleTranslate;
    return result;
}

struct UnityTexture2DArray
{
    TEXTURE2D_ARRAY(tex);
    SAMPLERDECL(samplerstate)
//    float4 texelSize;           // ??  are these valid for Texture2DArrays?
//    float4 scaleTranslate;      // ??
};

#define UnityBuildTexture2DArrayStruct(n) UnityBuildTexture2DArrayStructInternal(TEXTURE2D_ARRAY_ARGS(n, sampler##n))
UnityTexture2DArray UnityBuildTexture2DArrayStructInternal(TEXTURE2D_ARRAY_PARAM(tex, samplerstate))
{
    UnityTexture2DArray result;
    result.tex = tex;
    ASSIGN_SAMPLER(result.samplerstate, samplerstate);
//    result.texelSize = texelSize;
//    result.scaleTranslate = scaleTranslate;
    return result;
}


struct UnityTextureCube
{
    TEXTURECUBE(tex);
    SAMPLERDECL(samplerstate)
    //    float4 texelSize;           // ??  are these valid for Texture2DArrays?
    //    float4 scaleTranslate;      // ??
};

#define UnityBuildTextureCubeStruct(n) UnityBuildTextureCubeStructInternal(TEXTURECUBE_ARGS(n, sampler##n))
UnityTextureCube UnityBuildTextureCubeStructInternal(TEXTURECUBE_PARAM(tex, samplerstate)) //, float4 texelSize, float4 scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f))
{
    UnityTextureCube result;
    result.tex = tex;
    ASSIGN_SAMPLER(result.samplerstate, samplerstate);
    //    result.texelSize = texelSize;
    //    result.scaleTranslate = scaleTranslate;
    return result;
}


struct UnityTexture3D
{
    TEXTURE3D(tex);
    SAMPLERDECL(samplerstate)
    //    float4 texelSize;           // ??  are these valid for Texture2DArrays?
    //    float4 scaleTranslate;      // ??
};

#define UnityBuildTexture3DStruct(n) UnityBuildTexture3DStructInternal(TEXTURE3D_ARGS(n, sampler##n))
UnityTexture3D UnityBuildTexture3DStructInternal(TEXTURE3D_PARAM(tex, samplerstate)) //, float4 texelSize, float4 scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f))
{
    UnityTexture3D result;
    result.tex = tex;
    ASSIGN_SAMPLER(result.samplerstate, samplerstate);
    //    result.texelSize = texelSize;
    //    result.scaleTranslate = scaleTranslate;
    return result;
}

#undef SAMPLERDECL

#endif // UNITY_TEXTURE_INCLUDED
