// Gather all kind of mapping in one struct, allow to improve code readability
struct LayerUV
{
    float2 uv;
    // triplanar
    bool isTriplanar;
    float2 uvYZ;
    float2 uvZX;
    float2 uvXY;
};

// Multiple includes of the file to handle all variations of textures sampling for regular, lod and bias

// Regular sampling functions
#define ADD_FUNC_SUFFIX(Name) Name
#define SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV, unused) SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV)
#include "SampleLayerInternal.hlsl"
#undef ADD_FUNC_SUFFIX
#undef SAMPLE_TEXTURE_FUNC

// Lod sampling functions
#define ADD_FUNC_SUFFIX(Name) Name##Lod
#define SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV, lod) SAMPLE_TEXTURE2D_LOD(layerTex, layerSampler, layerUV, lod)
#include "SampleLayerInternal.hlsl"
#undef ADD_FUNC_SUFFIX
#undef SAMPLE_TEXTURE_FUNC

// Bias sampling functions
#define ADD_FUNC_SUFFIX(Name) Name##Bias
#define SAMPLE_TEXTURE_FUNC(layerTex, layerSampler, layerUV, bias) SAMPLE_TEXTURE2D_BIAS(layerTex, layerSampler, layerUV, bias)
#include "SampleLayerInternal.hlsl"
#undef ADD_FUNC_SUFFIX
#undef SAMPLE_TEXTURE_FUNC

// Macro to improve readibility of surface data
#define SAMPLE_LAYER_TEXTURE2D(textureName, samplerName, coord)             SampleLayer(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, 0.0) // Last 0.0 is unused
#define SAMPLE_LAYER_TEXTURE2D_LOD(textureName, samplerName, coord, lod)    SampleLayerLod(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, lod)
#define SAMPLE_LAYER_TEXTURE2D_BIAS(textureName, samplerName, coord, bias)  SampleLayerBias(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, bias)

#define SAMPLE_LAYER_NORMALMAP(textureName, samplerName, coord, scale)              SampleLayerNormal(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, 0.0)
#define SAMPLE_LAYER_NORMALMAP_LOD(textureName, samplerName, coord, scale, lod)     SampleLayerNormalLod(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, lod)
#define SAMPLE_LAYER_NORMALMAP_BIAS(textureName, samplerName, coord, scale, bias)   SampleLayerNormalBias(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, bias)

#define SAMPLE_LAYER_NORMALMAP_AG(textureName, samplerName, coord, scale)              SampleLayerNormalAG(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, 0.0)
#define SAMPLE_LAYER_NORMALMAP_AG_LOD(textureName, samplerName, coord, scale, lod)     SampleLayerNormalAGLod(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, lod)
#define SAMPLE_LAYER_NORMALMAP_AG_BIAS(textureName, samplerName, coord, scale, bias)   SampleLayerNormalAGBias(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, bias)

#define SAMPLE_LAYER_NORMALMAP_RGB(textureName, samplerName, coord, scale)              SampleLayerNormalRGB(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, 0.0)
#define SAMPLE_LAYER_NORMALMAP_RGB_LOD(textureName, samplerName, coord, scale, lod)     SampleLayerNormalRGBLod(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, lod)
#define SAMPLE_LAYER_NORMALMAP_RGB_BIAS(textureName, samplerName, coord, scale, bias)   SampleLayerNormalRGBBias(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, bias)
