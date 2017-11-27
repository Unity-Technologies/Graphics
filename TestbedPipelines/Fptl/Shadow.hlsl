#ifndef FPTL_SHADOW_HLSL
#define FPTL_SHADOW_HLSL

#define SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL    // Define this to provide custom implementations of GetPunctualShadowAttenuation
#define SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL // Define this to provide custom implementations of GetDirectionalShadowAttenuation

#include "ShadowContext.hlsl"

#ifdef  SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL
float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L )
{
	Texture2DArray			tex      = shadowContext.tex2DArray[0];
	SamplerComparisonState	compSamp = shadowContext.compSamplers[0];
	uint					algo     = GPUSHADOWALGORITHM_PCF_9TAP;

	return EvalShadow_CascadedDepth_Blend( shadowContext, algo, tex, compSamp, positionWS, normalWS, shadowDataIndex, L );
}

float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
	return GetDirectionalShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L );
}
#endif

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

#endif // FPTL_SHADOW_HLSL