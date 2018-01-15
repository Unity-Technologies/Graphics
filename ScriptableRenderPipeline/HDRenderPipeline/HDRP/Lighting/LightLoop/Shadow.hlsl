#ifndef LIGHTLOOP_SHADOW_HLSL
#define LIGHTLOOP_SHADOW_HLSL

#define SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL
#define SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL

#include "ShadowContext.hlsl"

// This is an example of how to override the default dynamic resource dispatcher
// by hardcoding the resources used and calling the shadow sampling routines that take an explicit texture and sampler.
// It is the responsibility of the author to make sure that ShadowContext.hlsl binds the correct texture to the right slot,
// and that on the C# side the shadowContext bindDelegate binds the correct resource to the correct texture id.


//#define SHADOW_DISPATCH_USE_SEPARATE_CASCADE_ALGOS  // enables separate cascade sampling variants for each cascade
//#define SHADOW_DISPATCH_USE_SEPARATE_PUNC_ALGOS	    // enables separate resources and algorithms for spot and point lights

// directional
#define SHADOW_DISPATCH_DIR_TEX   3
#define SHADOW_DISPATCH_DIR_SMP   0
#define SHADOW_DISPATCH_DIR_ALG   GPUSHADOWALGORITHM_PCF_TENT_5X5   // all cascades
#define SHADOW_DISPATCH_DIR_ALG_0 GPUSHADOWALGORITHM_PCF_TENT_7X7   // 1st cascade
#define SHADOW_DISPATCH_DIR_ALG_1 GPUSHADOWALGORITHM_PCF_TENT_5X5   // 2nd cascade
#define SHADOW_DISPATCH_DIR_ALG_2 GPUSHADOWALGORITHM_PCF_TENT_3X3   // 3rd cascade
#define SHADOW_DISPATCH_DIR_ALG_3 GPUSHADOWALGORITHM_PCF_1TAP       // 4th cascade
// point
#define SHADOW_DISPATCH_POINT_TEX 3
#define SHADOW_DISPATCH_POINT_SMP 0
#define SHADOW_DISPATCH_POINT_ALG GPUSHADOWALGORITHM_PCF_1TAP
// spot
#define SHADOW_DISPATCH_SPOT_TEX 3
#define SHADOW_DISPATCH_SPOT_SMP 0
#define SHADOW_DISPATCH_SPOT_ALG GPUSHADOWALGORITHM_PCF_9TAP
//punctual
#define SHADOW_DISPATCH_PUNC_TEX 3
#define SHADOW_DISPATCH_PUNC_SMP 0
#define SHADOW_DISPATCH_PUNC_ALG GPUSHADOWALGORITHM_PCF_9TAP

// example of overriding directional lights
#ifdef  SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL
float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L )
{
	Texture2DArray	        tex      = shadowContext.tex2DArray[SHADOW_DISPATCH_DIR_TEX];
	SamplerComparisonState	compSamp = shadowContext.compSamplers[SHADOW_DISPATCH_DIR_SMP];
#ifdef SHADOW_DISPATCH_USE_SEPARATE_CASCADE_ALGOS
	uint			        algo[kMaxShadowCascades] = { SHADOW_DISPATCH_DIR_ALG_0, SHADOW_DISPATCH_DIR_ALG_1, SHADOW_DISPATCH_DIR_ALG_2, SHADOW_DISPATCH_DIR_ALG_3 };
#else
	uint                    algo = SHADOW_DISPATCH_DIR_ALG;
#endif

	return EvalShadow_CascadedDepth_Blend( shadowContext, algo, tex, compSamp, positionWS, normalWS, shadowDataIndex, L );
}

float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float2 positionSS )
{
	return GetDirectionalShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L );
}
#endif


// example of overriding punctual lights
#ifdef  SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float4 L )
{
#ifdef SHADOW_DISPATCH_USE_SEPARATE_PUNC_ALGOS
	// example for choosing different algos for point and spot lights
	ShadowData sd = shadowContext.shadowDatas[shadowDataIndex];
	uint shadowType;
	UnpackShadowType( sd.shadowType, shadowType );

	[branch]
	if( shadowType == GPUSHADOWTYPE_POINT )
	{
		Texture2DArray			tex      = shadowContext.tex2DArray[SHADOW_DISPATCH_POINT_TEX];
		SamplerComparisonState	compSamp = shadowContext.compSamplers[SHADOW_DISPATCH_POINT_SMP];
		uint					algo     = SHADOW_DISPATCH_POINT_ALG;
		return EvalShadow_PointDepth( shadowContext, algo, tex, compSamp, positionWS, normalWS, shadowDataIndex, L );
	}
	else
	{
		Texture2DArray			tex      = shadowContext.tex2DArray[SHADOW_DISPATCH_SPOT_TEX];
		SamplerComparisonState	compSamp = shadowContext.compSamplers[SHADOW_DISPATCH_SPOT_SMP];
		uint					algo     = SHADOW_DISPATCH_SPOT_ALG;
		return EvalShadow_SpotDepth( shadowContext, algo, tex, compSamp, positionWS, normalWS, shadowDataIndex, L );
	}
#else
	// example for choosing the same algo
	Texture2DArray			tex      = shadowContext.tex2DArray[SHADOW_DISPATCH_PUNC_TEX];
	SamplerComparisonState	compSamp = shadowContext.compSamplers[SHADOW_DISPATCH_PUNC_SMP];
	uint					algo     = SHADOW_DISPATCH_PUNC_ALG;
	return EvalShadow_PunctualDepth( shadowContext, algo, tex, compSamp, positionWS, normalWS, shadowDataIndex, L );
#endif
}
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float4 L, float2 positionSS )
{
	return GetPunctualShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L );
}
#endif

// cleanup the defines
#undef SHADOW_DISPATCH_DIR_TEX
#undef SHADOW_DISPATCH_DIR_SMP
#undef SHADOW_DISPATCH_DIR_ALG
#undef SHADOW_DISPATCH_DIR_ALG_0
#undef SHADOW_DISPATCH_DIR_ALG_1
#undef SHADOW_DISPATCH_DIR_ALG_2
#undef SHADOW_DISPATCH_DIR_ALG_3
#undef SHADOW_DISPATCH_POINT_TEX
#undef SHADOW_DISPATCH_POINT_SMP
#undef SHADOW_DISPATCH_POINT_ALG
#undef SHADOW_DISPATCH_SPOT_TEX
#undef SHADOW_DISPATCH_SPOT_SMP
#undef SHADOW_DISPATCH_SPOT_ALG
#undef SHADOW_DISPATCH_PUNC_TEX
#undef SHADOW_DISPATCH_PUNC_SMP
#undef SHADOW_DISPATCH_PUNC_ALG
#ifdef SHADOW_DISPATCH_USE_SEPARATE_PUNC_ALGOS
#undef SHADOW_DISPATCH_USE_SEPARATE_PUNC_ALGOS
#endif



#endif // LIGHTLOOP_SHADOW_HLSL
