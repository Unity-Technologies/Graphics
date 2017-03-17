// This file is empty by default.
// Project specific file to override the default shadow sampling routines.
// We need to define which dispatchers we're overriding, otherwise the compiler will pick default implementations which will lead to compile errors.
// Check Shadow.hlsl right below where this header is included for the individual defines.

// example of overriding directional lights
//#define SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL
#ifdef  SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL
float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L )
{
	Texture2DArray tex = shadowContext.tex2DArray[0];
	SamplerState samp = shadowContext.samplers[0];
	uint algo = GPUSHADOWALGORITHM_MSM_HAUS;

	return EvalShadow_CascadedDepth( shadowContext, algo, tex, samp, positionWS, normalWS, shadowDataIndex, L );
}

float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
	return GetDirectionalShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L );
}
#endif



// This is an example of how to override the default dynamic resource dispatcher
// by hardcoding the resources used and calling the shadow sampling routines that take an explicit texture and sampler.
// It is the responsibility of the author to make sure that ShadowContext.hlsl binds the correct texture to the right slot,
// and that on the C# side the shadowContext bindDelegate binds the correct resource to the correct texture id.
//#define SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL
#ifdef  SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL
#define SHADOW_USE_SEPARATE_ALGOS 0
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L )
{

	Texture2DArray tex = shadowContext.tex2DArray[2];
	SamplerComparisonState compSamp = shadowContext.compSamplers[1];

#if SHADOW_USE_SEPARATE_ALGOS != 0
	// example for choosing different algos for point and spot lights
	ShadowData sd = shadowContext.shadowDatas[shadowDataIndex];
	uint shadowType;
	UnpackShadowType( sd.shadowType, shadowType );

	[branch]
	if( shadowType == GPUSHADOWTYPE_POINT )
		return EvalShadow_PointDepth( shadowContext, GPUSHADOWALGORITHM_PCF_1TAP, tex, compSamp, positionWS, normalWS, shadowDataIndex, L );
	else
		return EvalShadow_SpotDepth( shadowContext, GPUSHADOWALGORITHM_PCF_9TAP, tex, compSamp, positionWS, normalWS, shadowDataIndex, L );
#else
	// example for choosing the same algo 
	uint algo = GPUSHADOWALGORITHM_PCF_1TAP;
	return EvalShadow_PunctualDepth( shadowContext, algo, tex, compSamp, positionWS, normalWS, shadowDataIndex, L );
#endif
}
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
	return GetPunctualShadowAttenuation( shadowContext, positionWS, normalWS, shadowDataIndex, L );
}
#undef SHADOW_USE_SEPARATE_ALGOS
#endif
