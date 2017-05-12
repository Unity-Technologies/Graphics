// This file is empty by default.
// Project specific file to override the default shadow sampling routines.
// We need to define which dispatchers we're overriding, otherwise the compiler will pick default implementations which will lead to compilation errors.
// Check Shadow.hlsl right below where this header is included for the individual defines.



// This is an example of how to override the default dynamic resource dispatcher
// by hardcoding the resources used and calling the shadow sampling routines that take an explicit texture and sampler.
// It is the responsibility of the author to make sure that ShadowContext.hlsl binds the correct texture to the right slot,
// and that on the C# side the shadowContext bindDelegate binds the correct resource to the correct texture id.


#define SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL	// enables hardcoded resources and algorithm for directional lights
#define SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL		// enables hardcoded resources and algorithm for punctual lights
//#define SHADOW_DISPATCH_USE_SEPARATE_PUNC_ALGOS	// enables separate resources and algorithms for spot and point lights

// directional
#define SHADOW_DISPATCH_DIR_TEX 3
#define SHADOW_DISPATCH_DIR_SMP 0
#define SHADOW_DISPATCH_DIR_ALG GPUSHADOWALGORITHM_PCF_TENT_7X7
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
#define SHADOW_DISPATCH_PUNC_ALG GPUSHADOWALGORITHM_PCF_TENT_7X7

// example of overriding directional lights
#ifdef  SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL
float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L )
{
	Texture2DArray	        tex      = shadowContext.tex2DArray[SHADOW_DISPATCH_DIR_TEX];
	SamplerComparisonState	compSamp = shadowContext.compSamplers[SHADOW_DISPATCH_DIR_SMP];
	uint			        algo     = SHADOW_DISPATCH_DIR_ALG;

	return EvalShadow_CascadedDepth( shadowContext, algo, tex, compSamp, positionWS, normalWS, shadowDataIndex, L );
}

float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
	return GetDirectionalShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L );
}
#endif


// example of overriding punctual lights
#ifdef  SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L )
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
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
	return GetPunctualShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L );
}
#endif

// cleanup the defines
#undef SHADOW_DISPATCH_DIR_TEX
#undef SHADOW_DISPATCH_DIR_SMP
#undef SHADOW_DISPATCH_DIR_ALG
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
