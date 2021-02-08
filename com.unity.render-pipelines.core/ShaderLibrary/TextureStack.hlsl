#ifndef TEXTURESTACK_include
#define TEXTURESTACK_include

#define GRA_HLSL_5 1
#define GRA_ROW_MAJOR 1
#define GRA_TEXTURE_ARRAY_SUPPORT 1
#define GRA_PACK_RESOLVE_OUTPUT 0
#if SHADER_API_PSSL
#define GRA_NO_UNORM 1
#endif
#include "VirtualTexturing.hlsl"
#include "Packing.hlsl"

/*
    This header adds the following pseudo definitions. Actual types etc may vary depending
    on vt- being on or off.

        struct StackInfo { opaque struct ... }
        StackInfo PrepareStack(float2 uv, Stack object);
        float4 SampleStack(StackInfo info, Texture tex);

    To use this in your materials add the following to various locations in the shader:

    In shaderlab "Properties" section add:

        [TextureStack.MyFancyStack] DiffuseTexture ("DiffuseTexture", 2D) = "white" {}
        [TextureStack.MyFancyStack] NormalTexture ("NormalTexture", 2D) = "white" {}

    This will declare a texture stack with two textures.

    Then add the following to the PerMaterial constant buffer:

        CBUFFER_START(UnityPerMaterial)
        ...
        DECLARE_STACK_CB(MyFancyStack)
        ...
        CBUFFER_END

    Then in your shader root add the following:

        ...

        DECLARE_STACK(MyFancyStack, DiffuseTexture)
        or
        DECLARE_STACK2(MyFancyStack, DiffuseTexture, NormalTexture)
        or
        DECLARE_STACK3(MyFancyStack, TextureSlot1, TextureSlot2, TextureSlot2)
        etc...

    NOTE: The Stack shaderlab property and DECLARE_STACKn define need to match i.e. the same name and same texture slots.

    Then in the pixel shader function (likely somewhere at the beginning) do a call:

        StackInfo info = PrepareStack(uvs, MyFancyStack);

    Then later on when you want to sample the actual texture do a call(s):

        float4 color = SampleStack(info, TextureSlot1);
        float4 color2 = SampleStack(info, TextureSlot2);
        ...

    The above steps can be repeated for multiple stacks. But be sure that when using the SampleStack you always
    pass in the result of the PrepareStack for the correct stack the texture belongs to.

*/

#if defined(UNITY_VIRTUAL_TEXTURING) && !defined(FORCE_VIRTUAL_TEXTURING_OFF)

struct StackInfo
{
    GraniteLookupData lookupData;
    GraniteLODLookupData lookupDataLod;
    float4 resolveOutput;
};

struct VTProperty
{
    GraniteConstantBuffers grCB;
    GraniteTranslationTexture translationTable;
    GraniteCacheTexture cacheLayer[4];
    int layerCount;
    int layerIndex[4];
};

#ifdef TEXTURESTACK_CLAMP
    #define GR_LOOKUP Granite_Lookup_Clamp_Linear
    #define GR_LOOKUP_LOD Granite_Lookup_Clamp
#else
    #define GR_LOOKUP Granite_Lookup_Anisotropic
    #define GR_LOOKUP_LOD Granite_Lookup
#endif

// This can be used by certain resolver implementations to override screen space derivatives
#ifndef RESOLVE_SCALE_OVERRIDE
#define RESOLVE_SCALE_OVERRIDE float2(1,1)
#endif

StructuredBuffer<GraniteTilesetConstantBuffer> _VTTilesetBuffer;

#define DECLARE_STACK_CB(stackName) \
    float4 stackName##_atlasparams[2]

#define DECLARE_STACK_BASE(stackName) \
TEXTURE2D(stackName##_transtab);\
SAMPLER(sampler##stackName##_transtab);\
\
GraniteTilesetConstantBuffer GetConstantBuffer_##stackName() \
{ \
    int idx = (int)stackName##_atlasparams[1].w; \
    GraniteTilesetConstantBuffer graniteParamBlock; \
    graniteParamBlock = _VTTilesetBuffer[idx]; \
    \
    graniteParamBlock.data[0][2][0] *= RESOLVE_SCALE_OVERRIDE.x; \
    graniteParamBlock.data[0][3][0] *= RESOLVE_SCALE_OVERRIDE.y; \
    \
    return graniteParamBlock; \
} \
StackInfo PrepareVT_##stackName(VtInputParameters par)\
{\
    GraniteStreamingTextureConstantBuffer textureParamBlock;\
    textureParamBlock.data[0] = stackName##_atlasparams[0];\
    textureParamBlock.data[1] = stackName##_atlasparams[1];\
\
    GraniteTilesetConstantBuffer graniteParamBlock = GetConstantBuffer_##stackName(); \
\
    GraniteConstantBuffers grCB;\
    grCB.tilesetBuffer = graniteParamBlock;\
    grCB.streamingTextureBuffer = textureParamBlock;\
\
    GraniteTranslationTexture translationTable;\
    translationTable.Texture = stackName##_transtab;\
    translationTable.Sampler = sampler##stackName##_transtab;\
\
    StackInfo info;\
    VirtualTexturingLookup(grCB, translationTable, par, info.lookupData, info.resolveOutput);\
    return info;\
}

// TODO: we could replace all uses of GetConstantBuffer_*() with this one:
GraniteTilesetConstantBuffer GetConstantBuffer(GraniteStreamingTextureConstantBuffer textureParamBlock)
{
    int idx = (int)textureParamBlock.data[1].w;
    GraniteTilesetConstantBuffer graniteParamBlock;
    graniteParamBlock = _VTTilesetBuffer[idx];

    graniteParamBlock.data[0][2][0] *= RESOLVE_SCALE_OVERRIDE.x;
    graniteParamBlock.data[0][3][0] *= RESOLVE_SCALE_OVERRIDE.y;

    return graniteParamBlock;
}

#define jj2(a, b) a##b
#define jj(a, b) jj2(a, b)

#define DECLARE_STACK_LAYER(stackName, layerSamplerName, layerIndex) \
TEXTURE2D_ARRAY(stackName##_c##layerIndex);\
SAMPLER(sampler##stackName##_c##layerIndex);\
\
float4 SampleVT_##layerSamplerName(StackInfo info, int lodCalculation, int quality)\
{\
    GraniteStreamingTextureConstantBuffer textureParamBlock;\
    textureParamBlock.data[0] = stackName##_atlasparams[0];\
    textureParamBlock.data[1] = stackName##_atlasparams[1];\
\
    GraniteTilesetConstantBuffer graniteParamBlock = GetConstantBuffer_##stackName(); \
\
    GraniteConstantBuffers grCB;\
    grCB.tilesetBuffer = graniteParamBlock;\
    grCB.streamingTextureBuffer = textureParamBlock;\
\
    GraniteCacheTexture cache;\
    cache.TextureArray = stackName##_c##layerIndex;\
    cache.Sampler = sampler##stackName##_c##layerIndex;\
\
    float4 output;\
    VirtualTexturingSample(grCB.tilesetBuffer, info.lookupData, cache, layerIndex, lodCalculation, quality, output);\
    return output;\
}

#define DECLARE_BUILD_PROPERTIES(stackName, layers, layer0Index, layer1Index, layer2Index, layer3Index)\
    VTProperty BuildVTProperties_##stackName()\
    {\
        VTProperty vtProperty; \
        \
        GraniteStreamingTextureConstantBuffer textureParamBlock; \
        textureParamBlock.data[0] = stackName##_atlasparams[0]; \
        textureParamBlock.data[1] = stackName##_atlasparams[1]; \
        \
        vtProperty.grCB.tilesetBuffer = GetConstantBuffer(textureParamBlock); \
        vtProperty.grCB.streamingTextureBuffer = textureParamBlock; \
        \
        vtProperty.translationTable.Texture = stackName##_transtab; \
        vtProperty.translationTable.Sampler = sampler##stackName##_transtab; \
        \
        vtProperty.layerCount = layers; \
        vtProperty.layerIndex[0] = layer0Index; \
        vtProperty.layerIndex[1] = layer1Index; \
        vtProperty.layerIndex[2] = layer2Index; \
        vtProperty.layerIndex[3] = layer3Index; \
        \
        vtProperty.cacheLayer[0].TextureArray = stackName##_c##layer0Index; \
        vtProperty.cacheLayer[0].Sampler = sampler##stackName##_c##layer0Index;\
        vtProperty.cacheLayer[1].TextureArray = stackName##_c##layer1Index; \
        vtProperty.cacheLayer[1].Sampler = sampler##stackName##_c##layer1Index;\
        vtProperty.cacheLayer[2].TextureArray = stackName##_c##layer2Index; \
        vtProperty.cacheLayer[2].Sampler = sampler##stackName##_c##layer2Index;\
        vtProperty.cacheLayer[3].TextureArray = stackName##_c##layer3Index; \
        vtProperty.cacheLayer[3].Sampler = sampler##stackName##_c##layer3Index;\
        \
        return vtProperty; \
    }

#define DECLARE_STACK(stackName, layer0SamplerName)\
    DECLARE_STACK_BASE(stackName)\
    DECLARE_STACK_LAYER(stackName, layer0SamplerName, 0)\
    DECLARE_BUILD_PROPERTIES(stackName, 1, 0, 0, 0, 0)

#define DECLARE_STACK2(stackName, layer0SamplerName, layer1SamplerName)\
    DECLARE_STACK_BASE(stackName)\
    DECLARE_STACK_LAYER(stackName, layer0SamplerName, 0)\
    DECLARE_STACK_LAYER(stackName, layer1SamplerName, 1)\
    DECLARE_BUILD_PROPERTIES(stackName, 2, 0, 1, 1, 1)

#define DECLARE_STACK3(stackName, layer0SamplerName, layer1SamplerName, layer2SamplerName)\
    DECLARE_STACK_BASE(stackName)\
    DECLARE_STACK_LAYER(stackName, layer0SamplerName, 0)\
    DECLARE_STACK_LAYER(stackName, layer1SamplerName, 1)\
    DECLARE_STACK_LAYER(stackName, layer2SamplerName, 2)\
    DECLARE_BUILD_PROPERTIES(stackName, 3, 0, 1, 2, 2)

#define DECLARE_STACK4(stackName, layer0SamplerName, layer1SamplerName, layer2SamplerName, layer3SamplerName)\
    DECLARE_STACK_BASE(stackName)\
    DECLARE_STACK_LAYER(stackName, layer0SamplerName, 0)\
    DECLARE_STACK_LAYER(stackName, layer1SamplerName, 1)\
    DECLARE_STACK_LAYER(stackName, layer2SamplerName, 2)\
    DECLARE_STACK_LAYER(stackName, layer3SamplerName, 3)\
    DECLARE_BUILD_PROPERTIES(stackName, 4, 0, 1, 2, 3)

#define PrepareStack(inputParams, stackName) PrepareVT_##stackName(inputParams)
#define SampleStack(info, lodMode, quality, textureName) SampleVT_##textureName(info, lodMode, quality)
#define GetResolveOutput(info) info.resolveOutput
#define PackResolveOutput(output) Granite_PackTileId(output)

StackInfo PrepareVT(VTProperty vtProperty, VtInputParameters vtParams)
{
    StackInfo info;
    VirtualTexturingLookup(vtProperty.grCB, vtProperty.translationTable, vtParams, info.lookupData, info.resolveOutput);
    return info;
}

// NOTE: layerIndex here can only be an immediate constant (i.e. 0,1,2, or 3) -- it CANNOT be a variable or expression
// this is because we use macro concatentation on it when VT is disabled
float4 SampleVTLayer(VTProperty vtProperty, VtInputParameters vtParams, StackInfo info, int layerIndex)
{
    float4 result;
    VirtualTexturingSample(vtProperty.grCB.tilesetBuffer, info.lookupData, vtProperty.cacheLayer[layerIndex], vtProperty.layerIndex[layerIndex], vtParams.levelMode, vtParams.sampleQuality, result);
    return result;
}

float4 GetPackedVTFeedback(float4 feedback)
{
    return Granite_PackTileId(feedback);
}

#define VIRTUAL_TEXTURING_SHADER_ENABLED

#else
// Virtual Texturing Disabled -- fallback to regular texture sampling

#define DECLARE_BUILD_PROPERTIES(stackName, layers, layer0, layer1, layer2, layer3)\
    VTProperty BuildVTProperties_##stackName()\
    {\
        VTProperty vtProperty; \
        \
        vtProperty.layerCount = layers; \
        vtProperty.Layer0 = layer0; \
        vtProperty.Layer1 = layer1; \
        vtProperty.Layer2 = layer2; \
        vtProperty.Layer3 = layer3; \
        \
        ASSIGN_SAMPLER(vtProperty.samplerLayer0, sampler##layer0); \
        ASSIGN_SAMPLER(vtProperty.samplerLayer1, sampler##layer1); \
        ASSIGN_SAMPLER(vtProperty.samplerLayer2, sampler##layer2); \
        ASSIGN_SAMPLER(vtProperty.samplerLayer3, sampler##layer3); \
        \
        return vtProperty; \
    }

// Stacks amount to nothing when VT is off
#define DECLARE_STACK(stackName, layer0) \
    DECLARE_BUILD_PROPERTIES(stackName, 1, layer0, layer0, layer0, layer0)

#define DECLARE_STACK2(stackName, layer0, layer1) \
    DECLARE_BUILD_PROPERTIES(stackName, 2, layer0, layer1, layer1, layer1)

#define DECLARE_STACK3(stackName, layer0, layer1, layer2) \
    DECLARE_BUILD_PROPERTIES(stackName, 3, layer0, layer1, layer2, layer2)

#define DECLARE_STACK4(stackName, layer0, layer1, layer2, layer3) \
    DECLARE_BUILD_PROPERTIES(stackName, 4, layer0, layer1, layer2, layer3)

#define DECLARE_STACK_CB(stackName)

// Info is just the uv's
// We could do a straight #define StackInfo float2 but this makes it a bit more type safe
// and allows us to do things like function overloads,...
struct StackInfo
{
    VtInputParameters vt;
};

struct VTProperty
{
    int layerCount;
    TEXTURE2D(Layer0);
    TEXTURE2D(Layer1);
    TEXTURE2D(Layer2);
    TEXTURE2D(Layer3);
#ifndef SHADER_API_GLES
    SAMPLER(samplerLayer0);
    SAMPLER(samplerLayer1);
    SAMPLER(samplerLayer2);
    SAMPLER(samplerLayer3);
#endif
};

StackInfo MakeStackInfo(VtInputParameters vt)
{
    StackInfo result;
    result.vt = vt;
    return result;
}

// Prepare just passes the texture coord around
#define PrepareStack(inputParams, stackName) MakeStackInfo(inputParams)

// Sample just samples the texture
#define SampleStack(info, vtLevelMode, quality, texture) \
    SampleVTFallbackToTexture(info, vtLevelMode, TEXTURE2D_ARGS(texture, sampler##texture))


float4 SampleVTFallbackToTexture(StackInfo info, int vtLevelMode, TEXTURE2D_PARAM(layerTexture, layerSampler))
{
    if (vtLevelMode == VtLevel_Automatic)
        return SAMPLE_TEXTURE2D(layerTexture, layerSampler, info.vt.uv);
    else if (vtLevelMode == VtLevel_Lod)
        return SAMPLE_TEXTURE2D_LOD(layerTexture, layerSampler, info.vt.uv, info.vt.lodOrOffset);
    else if (vtLevelMode == VtLevel_Bias)
        return SAMPLE_TEXTURE2D_BIAS(layerTexture, layerSampler, info.vt.uv, info.vt.lodOrOffset);
    else // vtLevelMode == VtLevel_Derivatives
        return SAMPLE_TEXTURE2D_GRAD(layerTexture, layerSampler, info.vt.uv, info.vt.dx, info.vt.dy);
}

StackInfo PrepareVT(VTProperty vtProperty, VtInputParameters vtParams)
{
    StackInfo result;
    result.vt = vtParams;
    return result;
}

// NOTE: layerIndex here can only be an immediate constant (i.e. 0,1,2, or 3) -- it CANNOT be a variable or expression
// this is because we use macro concatentation on it when VT is disabled
#define SampleVTLayer(vtProperty, vtParams, info, layerIndex) \
    SampleVTFallbackToTexture(info, vtParams.levelMode, TEXTURE2D_ARGS(vtProperty.Layer##layerIndex, vtProperty.samplerLayer##layerIndex))

// Resolve does nothing
#define GetResolveOutput(info) float4(1,1,1,1)
#define PackResolveOutput(output) output
#define GetPackedVTFeedback(feedback) feedback

#endif



// Shared code between VT enabled and VT disabled, adding TextureType handling

// these texture types should be kept in sync with LayerTextureType in C# code
#define TEXTURETYPE_DEFAULT 0                   // LayerTextureType.Default
#define TEXTURETYPE_NORMALTANGENTSPACE 1        // LayerTextureType.NormalTangentSpace
#define TEXTURETYPE_NORMALOBJECTSPACE 2         // LayerTextureType.NormalObjectSpace

struct VTPropertyWithTextureType
{
    VTProperty vtProperty;
    int layerTextureType[4];
};

VTPropertyWithTextureType AddTextureType(VTProperty vtProperty, int layer0TextureType, int layer1TextureType = TEXTURETYPE_DEFAULT, int layer2TextureType = TEXTURETYPE_DEFAULT, int layer3TextureType = TEXTURETYPE_DEFAULT)
{
    VTPropertyWithTextureType result;
    result.vtProperty = vtProperty;
    result.layerTextureType[0] = layer0TextureType;
    result.layerTextureType[1] = layer1TextureType;
    result.layerTextureType[2] = layer2TextureType;
    result.layerTextureType[3] = layer3TextureType;
    return result;
}

float4 ApplyTextureType(float4 value, int textureType)
{
    // NOTE: when textureType is a compile-time constant, the branches compile out
    if (textureType == TEXTURETYPE_NORMALTANGENTSPACE)
    {
        value.rgb = UnpackNormalmapRGorAG(value);
    }
    else if (textureType == TEXTURETYPE_NORMALOBJECTSPACE)
    {
        value.rgb = UnpackNormalRGB(value);
    }
    return value;
}

// if we _could_ express it as a function, the function signature would be:
//   float4 SampleVTLayerWithTextureType(VTPropertyWithTextureType vtProperty, VtInputParameters vtParams, StackInfo info, [immediate] int layerIndex)
// NOTE: layerIndex here can only be an immediate constant (i.e. 0,1,2, or 3) -- it CANNOT be a variable or expression
// this is because we use macro concatentation on it when VT is disabled

#define SampleVTLayerWithTextureType(vtProperty, vtParams, info, layerIndex) \
    ApplyTextureType(SampleVTLayer(vtProperty.vtProperty, vtParams, info, layerIndex), vtProperty.layerTextureType[layerIndex])

#endif //TEXTURESTACK_include
