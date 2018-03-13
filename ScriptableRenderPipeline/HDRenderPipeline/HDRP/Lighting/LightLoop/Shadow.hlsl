#ifndef LIGHTLOOP_SHADOW_HLSL
#define LIGHTLOOP_SHADOW_HLSL

#define SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL
#define SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL

#define SHADOW_USE_VIEW_BIAS_SCALING            1   // Enable view bias scaling to mitigate light leaking across edges. Uses the light vector if SHADOW_USE_ONLY_VIEW_BASED_BIASING is defined, otherwise uses the normal.
// Note: Sample biasing work well but is very costly in term of VGPR, disable it for now
#define SHADOW_USE_SAMPLE_BIASING               0   // Enable per sample biasing for wide multi-tap PCF filters. Incompatible with SHADOW_USE_ONLY_VIEW_BASED_BIASING.
#define SHADOW_USE_DEPTH_BIAS                   0   // Enable clip space z biasing

#include "ShadowContext.hlsl"

// This is an example of how to override the default dynamic resource dispatcher
// by hardcoding the resources used and calling the shadow sampling routines that take an explicit texture and sampler.
// It is the responsibility of the author to make sure that ShadowContext.hlsl binds the correct texture to the right slot,
// and that on the C# side the shadowContext bindDelegate binds the correct resource to the correct texture id.


// Caution: When updating algorithm here, think to update Lightloop.ShadowSetup() function
// (the various m_ShadowMgr.SetGlobalShadowOverride( GPUShadowType.Point, ShadowAlgorithm.PCF, ShadowVariant.V1, ShadowPrecision.High, useGlobalOverrides ));
// The enum value for the variant is in GPUShadowAlgorithm and is express like: PCF_tent_3x3 = ShadowAlgorithm.PCF << 3 | ShadowVariant.V2

//#define SHADOW_DISPATCH_USE_SEPARATE_CASCADE_ALGOS  // enables separate cascade sampling variants for each cascade
//#define SHADOW_DISPATCH_USE_SEPARATE_PUNC_ALGOS	    // enables separate resources and algorithms for spot and point lights

// directional
#define SHADOW_DISPATCH_DIR_TEX   0
#define SHADOW_DISPATCH_DIR_SMP   0
#define SHADOW_DISPATCH_DIR_ALG   GPUSHADOWALGORITHM_PCF_TENT_5X5   // all cascades
#define SHADOW_DISPATCH_DIR_ALG_0 GPUSHADOWALGORITHM_PCF_TENT_7X7   // 1st cascade
#define SHADOW_DISPATCH_DIR_ALG_1 GPUSHADOWALGORITHM_PCF_TENT_5X5   // 2nd cascade
#define SHADOW_DISPATCH_DIR_ALG_2 GPUSHADOWALGORITHM_PCF_TENT_3X3   // 3rd cascade
#define SHADOW_DISPATCH_DIR_ALG_3 GPUSHADOWALGORITHM_PCF_1TAP       // 4th cascade
// point
#define SHADOW_DISPATCH_POINT_TEX 0
#define SHADOW_DISPATCH_POINT_SMP 0
#define SHADOW_DISPATCH_POINT_ALG GPUSHADOWALGORITHM_PCF_TENT_3X3
// spot
#define SHADOW_DISPATCH_SPOT_TEX 0
#define SHADOW_DISPATCH_SPOT_SMP 0
#define SHADOW_DISPATCH_SPOT_ALG GPUSHADOWALGORITHM_PCF_TENT_3X3
//punctual
#define SHADOW_DISPATCH_PUNC_TEX 0
#define SHADOW_DISPATCH_PUNC_SMP 0
#define SHADOW_DISPATCH_PUNC_ALG GPUSHADOWALGORITHM_PCF_TENT_3X3

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

float3 GetDirectionalShadowClosestSample( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int index, real4 L )
{
	return EvalShadow_GetClosestSample_Cascade( shadowContext, shadowContext.tex2DArray[SHADOW_DISPATCH_DIR_TEX], positionWS, normalWS, index, L );
}

#endif


// example of overriding punctual lights
#ifdef  SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist )
{
#ifdef SHADOW_DISPATCH_USE_SEPARATE_PUNC_ALGOS
	// example for choosing different algos for point and spot lights
	ShadowData sd = shadowContext.shadowDatas[shadowDataIndex];
	uint shadowType;
	UnpackShadowType( sd.shadowType, shadowType );

	UNITY_BRANCH
	if( shadowType == GPUSHADOWTYPE_POINT )
	{
		Texture2DArray			tex      = shadowContext.tex2DArray[SHADOW_DISPATCH_POINT_TEX];
		SamplerComparisonState	compSamp = shadowContext.compSamplers[SHADOW_DISPATCH_POINT_SMP];
		uint					algo     = SHADOW_DISPATCH_POINT_ALG;
		return EvalShadow_PointDepth( shadowContext, algo, tex, compSamp, positionWS, normalWS, shadowDataIndex, L, L_dist );
	}
	else
	{
		Texture2DArray			tex      = shadowContext.tex2DArray[SHADOW_DISPATCH_SPOT_TEX];
		SamplerComparisonState	compSamp = shadowContext.compSamplers[SHADOW_DISPATCH_SPOT_SMP];
		uint					algo     = SHADOW_DISPATCH_SPOT_ALG;
		return EvalShadow_SpotDepth( shadowContext, algo, tex, compSamp, positionWS, normalWS, shadowDataIndex, L, L_dist );
	}
#else
	// example for choosing the same algo
	Texture2DArray			tex      = shadowContext.tex2DArray[SHADOW_DISPATCH_PUNC_TEX];
	SamplerComparisonState	compSamp = shadowContext.compSamplers[SHADOW_DISPATCH_PUNC_SMP];
	uint					algo     = SHADOW_DISPATCH_PUNC_ALG;
	return EvalShadow_PunctualDepth( shadowContext, algo, tex, compSamp, positionWS, normalWS, shadowDataIndex, L, L_dist );
#endif
}
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float L_dist, float2 positionSS )
{
	return GetPunctualShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L, L_dist );
}

float3 GetPunctualShadowClosestSample( ShadowContext shadowContext, real3 positionWS, int index, real3 L )
{
	return EvalShadow_GetClosestSample_Punctual( shadowContext, shadowContext.tex2DArray[SHADOW_DISPATCH_PUNC_TEX], positionWS, index, L );
}

float GetPunctualShadowClosestDistance( ShadowContext shadowContext, SamplerState sampl, real3 positionWS, int index, float3 L, float3 lightPositionWS)
{
	return EvalShadow_SampleClosestDistance_Punctual( shadowContext, shadowContext.tex2DArray[SHADOW_DISPATCH_PUNC_TEX], sampl, positionWS, index, L, lightPositionWS );
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
