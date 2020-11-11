#ifndef UNITY_TEXTURE_INCLUDED
#define UNITY_TEXTURE_INCLUDED

#ifdef SHADER_API_GLES
    #define SAMPLERDECL(n) GLES2UnsupportedSamplerState n;
#else
    #define SAMPLERDECL(n) SAMPLER(n);
#endif

struct GLES2UnsupportedSamplerState
{
    // if you get an error trying to use this structure,
    // then the shader code is trying to use a samplerstate on a GLES2 device, which is not supported
    // make sure all of the shader code is properly guarded by using the texture and sampler macros defined in GLES2.hlsl
};

struct UnitySamplerState
{
    SAMPLERDECL(samplerstate)
};

UnitySamplerState UnityBuildSamplerStateStruct(SAMPLER(samplerstate))
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

UnityTexture2D UnityBuildTexture2DStruct(TEXTURE2D_PARAM(tex, samplerstate), float4 texelSize, float4 scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f))
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

UnityTexture2DArray UnityBuildTexture2DArrayStruct(TEXTURE2D_ARRAY_PARAM(tex, samplerstate)) //, float4 texelSize, float4 scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f))
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

UnityTextureCube UnityBuildTextureCubeStruct(TEXTURECUBE_PARAM(tex, samplerstate)) //, float4 texelSize, float4 scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f))
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

UnityTexture3D UnityBuildTexture3DStruct(TEXTURE3D_PARAM(tex, samplerstate)) //, float4 texelSize, float4 scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f))
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
