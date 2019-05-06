#define GRA_HLSL_5 1
#define GRA_ROW_MAJOR 1
#define GRA_TEXTURE_ARRAY_SUPPORT 0
#include "GraniteShaderLib3.cginc"

/*
	This header adds the following pseudo definitions. Actual types etc may vary depending
	on vt- being on or off.
	
	    struct StackInfo { opaque struct ... }
	    StackInfo PrepareStack(float2 uv, Stack object);
	    float4 SampleStack(StackInfo info, Texture tex);
		
	To use this in your materials add the following to various locations in the shader:

	In shaderlab "Properties" section add:

	    MyFancyStack ("Fancy Stack", Stack ) = { TextureSlot1, TextureSlot2, ... }

	In your CGPROGRAM code for each of the passes add:

	    #pragma shader_feature_local VT_ON

    Then add the following to the PerMaterial constant buffer:

        CBUFFER_START(UnityPerMaterial)
        ...
        DECLARE_STACK_CB
        ...
        CBUFFER_END

    Then in your shader root add the following:

        ...

	    DECLARE_STACK(MyFancyStack, TextureSlot1)
	    or
	    DECLARE_STACK2(MyFancyStack, TextureSlot1, TextureSlot2)
	    or
	    DECLARE_STACK3(MyFancyStack, TextureSlot1, TextureSlot2, TextureSlot2)
	    etc...

	NOTE: The Stack shaderlab property and DECLARE_STACKn define need to match i.e. the same name and same texture slots.
	
	Then in the pixel shader function (likely somewhere at the beginning) do a call:

	    StackInfo info = PrepareStack(MyFancyStack, uvs);
	
	Then later on when you want to sample the actual texture do a call(s):
		
	    float4 color = SampleStack(info, TextureSlot1);
	    float4 color2 = SampleStack(info, TextureSlot2);
	    ...
	
	The above steps can be repeated for multiple stacks. But be sure that when using the SampleStack you always
	pass in the result of the PrepareStack for the correct stack the texture belongs to.

*/

#ifdef VT_ON

#define StackInfo GraniteLookupData

#define DECLARE_STACK_CB(stackName) \
    float4x4 stackName##_spaceparams[2];\
    float4 stackName##_atlasparams[2];\

#define DECLARE_STACK_BASE(stackName) \
TEXTURE2D(stackName##_transtab);\
SAMPLER(sampler##stackName##_transtab);\
\
StackInfo PrepareVT_##stackName(float2 uv)\
	{\
	GraniteStreamingTextureConstantBuffer textureParamBlock;\
	textureParamBlock.data[0] = stackName##_atlasparams[0];\
	textureParamBlock.data[1] = stackName##_atlasparams[1];\
\
	GraniteTilesetConstantBuffer graniteParamBlock;\
	graniteParamBlock.data[0] = stackName##_spaceparams[0];\
	graniteParamBlock.data[1] = stackName##_spaceparams[1];\
\
	GraniteConstantBuffers grCB;\
	grCB.tilesetBuffer = graniteParamBlock;\
	grCB.streamingTextureBuffer = textureParamBlock;\
\
	GraniteTranslationTexture translationTable;\
	translationTable.Texture = stackName##_transtab;\
	translationTable.Sampler = sampler##stackName##_transtab;\
\
	StackInfo outLookupData;\
	float4 outResolve;\
	Granite_Lookup_Anisotropic(grCB, translationTable, uv, outLookupData, outResolve);\
	return outLookupData;\
}

#define DECLARE_STACK_LAYER(stackName, layerSamplerName, layerIndex) \
TEXTURE2D(stackName##_c##layerIndex);\
SAMPLER(sampler##stackName##_c##layerIndex);\
\
float4 SampleVT_##layerSamplerName(StackInfo info)\
{\
	GraniteStreamingTextureConstantBuffer textureParamBlock;\
	textureParamBlock.data[0] = stackName##_atlasparams[0];\
	textureParamBlock.data[1] = stackName##_atlasparams[1];\
\
	GraniteTilesetConstantBuffer graniteParamBlock;\
	graniteParamBlock.data[0] = stackName##_spaceparams[0];\
	graniteParamBlock.data[1] = stackName##_spaceparams[1];\
\
	GraniteConstantBuffers grCB;\
	grCB.tilesetBuffer = graniteParamBlock;\
	grCB.streamingTextureBuffer = textureParamBlock;\
\
	GraniteCacheTexture cache;\
	cache.Texture = stackName##_c##layerIndex;\
	cache.Sampler = sampler##stackName##_c##layerIndex;\
\
	float4 output;\
	Granite_Sample_HQ(grCB, info, cache, layerIndex, output);\
	return output;\
}

#define DECLARE_STACK_RESOLVE(stackName)\
float4 ResolveVT_##stackName(float2 uv)\
{\
    GraniteStreamingTextureConstantBuffer textureParamBlock;\
    textureParamBlock.data[0] = stackName##_atlasparams[0];\
    textureParamBlock.data[1] = stackName##_atlasparams[1];\
\
    /* hack resolve scale into constant buffer here */\
    stackName##_spaceparams[0][2][0] *= VT_ResolveConstantPatch.x;\
    stackName##_spaceparams[0][3][0] *= VT_ResolveConstantPatch.y;\
\
    GraniteTilesetConstantBuffer graniteParamBlock;\
    graniteParamBlock.data[0] = stackName##_spaceparams[0];\
    graniteParamBlock.data[1] = stackName##_spaceparams[1];\
\
    GraniteConstantBuffers grCB;\
    grCB.tilesetBuffer = graniteParamBlock;\
    grCB.streamingTextureBuffer = textureParamBlock;\
\
    return Granite_ResolverPixel_Anisotropic(grCB, uv);\
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
	DECLARE_STACK_LAYER(stackName, layer2SamplerName,3)

#define PrepareStack(uv, stackName) PrepareVT_##stackName(uv)
#define SampleStack(info, textureName) SampleVT_##textureName(info)
#define ResolveStack(uv, stackName) ResolveVT_##stackName(uv)

#else

// Stacks amount to nothing when VT is off
#define DECLARE_STACK(stackName, layer0)
#define DECLARE_STACK2(stackName, layer0, layer1)
#define DECLARE_STACK3(stackName, layer0, layer1, layer2)
#define DECLARE_STACK4(stackName, layer0, layer1, layer2, layer3)
#define DECLARE_STACK_CB(stackName)

// Info is just the uv's
#define StackInfo float2

// Prepare just passes the texture coord around
#define PrepareStack(uv, stackName) uv

// Sample just samples the texture
#define SampleStack(info, texture) SAMPLE_TEXTURE2D(texture, sampler##texture, info)

// Resolve does nothing
#define ResolveStack(uv, stackName)

#endif
