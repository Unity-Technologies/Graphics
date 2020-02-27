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

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"


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

    This will declare a texture stack with two shaders.

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
    float4x4 stackName##_spaceparams[2];\
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
    /* hack resolve scale into constant buffer here */\
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

#define DECLARE_STACK_RESOLVE(stackName)\
float4 ResolveVT_##stackName(float2 uv)\
{\
    return float4(0.0f,0.0f, 0.0f, 0.0f);\
}

#define DECLARE_STACK(stackName, layer0SamplerName)\
	DECLARE_STACK_BASE(stackName)\
    DECLARE_STACK_RESOLVE(stackName)\
	DECLARE_STACK_LAYER(stackName, layer0SamplerName,0)

#define DECLARE_STACK2(stackName, layer0SamplerName, layer1SamplerName)\
	DECLARE_STACK_BASE(stackName)\
    DECLARE_STACK_RESOLVE(stackName)\
	DECLARE_STACK_LAYER(stackName, layer0SamplerName,0)\
	DECLARE_STACK_LAYER(stackName, layer1SamplerName,1)

#define DECLARE_STACK3(stackName, layer0SamplerName, layer1SamplerName, layer2SamplerName)\
	DECLARE_STACK_BASE(stackName)\
    DECLARE_STACK_RESOLVE(stackName)\
	DECLARE_STACK_LAYER(stackName, layer0SamplerName,0)\
	DECLARE_STACK_LAYER(stackName, layer1SamplerName,1)\
	DECLARE_STACK_LAYER(stackName, layer2SamplerName,2)

#define DECLARE_STACK4(stackName, layer0SamplerName, layer1SamplerName, layer2SamplerName, layer3SamplerName)\
	DECLARE_STACK_BASE(stackName)\
    DECLARE_STACK_RESOLVE(stackName)\
	DECLARE_STACK_LAYER(stackName, layer0SamplerName,0)\
	DECLARE_STACK_LAYER(stackName, layer1SamplerName,1)\
	DECLARE_STACK_LAYER(stackName, layer2SamplerName,2)\
	DECLARE_STACK_LAYER(stackName, layer3SamplerName,3)

#define PrepareStack(inputParams, stackName) PrepareVT_##stackName(inputParams)
#define SampleStack(info, lodMode, quality, textureName) SampleVT_##textureName(info, lodMode, quality)
#define GetResolveOutput(info) info.resolveOutput
#define PackResolveOutput(output) Granite_PackTileId(output)
#define ResolveStack(uv, stackName) ResolveVT_##stackName(uv)

float4 GetPackedVTFeedback(float4 feedback)
{
    return Granite_PackTileId(feedback);
}

#define VIRTUAL_TEXTURING_SHADER_ENABLED

#else

// Stacks amount to nothing when VT is off
#define DECLARE_STACK(stackName, layer0)
#define DECLARE_STACK2(stackName, layer0, layer1)
#define DECLARE_STACK3(stackName, layer0, layer1, layer2)
#define DECLARE_STACK4(stackName, layer0, layer1, layer2, layer3)
#define DECLARE_STACK_CB(stackName)

// Info is just the uv's
// We could do a straight #defube StackInfo float2 but this makes it a bit more type safe
// and allows us to do things like function overloads,...
struct StackInfo
{
    VtInputParameters vt;
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
#define SampleStack(info, lodMode, quality, texture) \
    (lodMode == VtLevel_Automatic) ?\
        SAMPLE_TEXTURE2D(texture, sampler##texture, info.vt.uv)\
    :( (lodMode == VtLevel_Lod) ?\
        SAMPLE_TEXTURE2D_LOD(texture, sampler##texture, info.vt.uv, info.vt.lodOrOffset)\
    :( (lodMode == VtLevel_Bias) ?\
        SAMPLE_TEXTURE2D_BIAS(texture, sampler##texture, info.vt.uv, info.vt.lodOrOffset)\
    :( /*(lodMode == /VtLevel_Derivatives)*/\
        SAMPLE_TEXTURE2D_GRAD(texture, sampler##texture, info.vt.uv, info.vt.dx, info.vt.dy)\
    )));

// Resolve does nothing
#define GetResolveOutput(info) float4(1,1,1,1)
#define ResolveStack(uv, stackName) float4(1,1,1,1)
#define PackResolveOutput(output) output
#define GetPackedVTFeedback(feedback) feedback


#endif

#endif //TEXTURESTACK_include
