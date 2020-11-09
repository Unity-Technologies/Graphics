#ifndef UNITY_TEXTURE_INCLUDED
#define UNITY_TEXTURE_INCLUDED

struct UnityTexture2D
{
    TEXTURE2D(tex);
    SAMPLER(texSampler);
    float4 texelSize;
    float4 scaleTranslate;
};

UnityTexture2D UnityBuildTexture2DStruct(TEXTURE2D_PARAM(tex, texSampler), float4 texelSize, float4 scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f))
{
    UnityTexture2D result;
    result.tex = tex;
    ASSIGN_SAMPLER(result.texSampler, texSampler);
    result.texelSize = texelSize;
    result.scaleTranslate = scaleTranslate;
    return result;
}

struct UnityTexture2DArray
{
    TEXTURE2D_ARRAY(tex);
    SAMPLER(texSampler);
//    float4 texelSize;           // ??  are these valid for Texture2DArrays?
//    float4 scaleTranslate;      // ??
};

UnityTexture2DArray UnityBuildTexture2DArrayStruct(TEXTURE2D_ARRAY_PARAM(tex, texSampler)) //, float4 texelSize, float4 scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f))
{
    UnityTexture2DArray result;
    result.tex = tex;
    ASSIGN_SAMPLER(result.texSampler, texSampler);
//    result.texelSize = texelSize;
//    result.scaleTranslate = scaleTranslate;
    return result;
}


struct UnityTextureCube
{
    TEXTURECUBE(tex);
    SAMPLER(texSampler);
    //    float4 texelSize;           // ??  are these valid for Texture2DArrays?
    //    float4 scaleTranslate;      // ??
};

UnityTextureCube UnityBuildTextureCubeStruct(TEXTURECUBE_PARAM(tex, texSampler)) //, float4 texelSize, float4 scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f))
{
    UnityTextureCube result;
    result.tex = tex;
    ASSIGN_SAMPLER(result.texSampler, texSampler);
    //    result.texelSize = texelSize;
    //    result.scaleTranslate = scaleTranslate;
    return result;
}


struct UnityTexture3D
{
    TEXTURE3D(tex);
    SAMPLER(texSampler);
    //    float4 texelSize;           // ??  are these valid for Texture2DArrays?
    //    float4 scaleTranslate;      // ??
};

UnityTexture3D UnityBuildTexture3DStruct(TEXTURE3D_PARAM(tex, texSampler)) //, float4 texelSize, float4 scaleTranslate = float4(1.0f, 1.0f, 0.0f, 0.0f))
{
    UnityTexture3D result;
    result.tex = tex;
    ASSIGN_SAMPLER(result.texSampler, texSampler);
    //    result.texelSize = texelSize;
    //    result.scaleTranslate = scaleTranslate;
    return result;
}

#endif // UNITY_TEXTURE_INCLUDED
