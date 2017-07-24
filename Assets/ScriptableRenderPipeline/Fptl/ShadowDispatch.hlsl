#define SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL	// enables hardcoded resources and algorithm for directional lights
#define SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL		// enables hardcoded resources and algorithm for punctual lights

// example of overriding directional lights
#ifdef  SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL
float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L )
{
	Texture2DArray			tex      = shadowContext.tex2DArray[0];
	SamplerComparisonState	compSamp = shadowContext.compSamplers[0];
	uint					algo     = GPUSHADOWALGORITHM_PCF_9TAP;

	return EvalShadow_CascadedDepth( shadowContext, algo, tex, compSamp, positionWS, normalWS, shadowDataIndex, L );
}

float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
	return GetDirectionalShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L );
}
#endif


// example of overriding punctual lights
#ifdef  SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float4 L )
{
	// example for choosing the same algo 
	Texture2DArray			tex = shadowContext.tex2DArray[0];
	SamplerComparisonState	compSamp = shadowContext.compSamplers[0];
	uint					algo     = GPUSHADOWALGORITHM_PCF_9TAP;
	return EvalShadow_PunctualDepth( shadowContext, algo, tex, compSamp, positionWS, normalWS, shadowDataIndex, L );
}
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float4 L, float2 unPositionSS )
{
	return GetPunctualShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L );
}
#endif
