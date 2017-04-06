// This file is empty by default.
// Project specific file to override the default shadow sampling routines.
// We need to define which dispatchers we're overriding, otherwise the compiler will pick default implementations which will lead to compile errors.
// Check Shadow.hlsl right below where this header is included for the individual defines.

// example of overriding directional lights
//#define SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL
#ifdef  SHADOW_DISPATCH_USE_CUSTOM_DIRECTIONAL
float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L )
{
    Texture2DArray tex = shadowContext.tex2DArray[0];
    SamplerComparisonState compSamp = shadowContext.compSamplers[0];

    return EvalShadow_CascadedDepth( shadowContext, tex, compSamp, positionWS, shadowDataIndex, L );

    //return EvalShadow_CascadedMomentum( shadowContext, positionWS, shadowDataIndex, L );
}

float GetDirectionalShadowAttenuation( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
    Texture2DArray tex = shadowContext.tex2DArray[0];
    SamplerComparisonState compSamp = shadowContext.compSamplers[0];

    return EvalShadow_CascadedDepth( shadowContext, tex, compSamp, positionWS, shadowDataIndex, L );

    //return EvalShadow_CascadedMomentum( shadowContext, positionWS, shadowDataIndex, L );
}
#endif



// This is an example of how to override the default dynamic resource dispatcher
// by hardcoding the resources used and calling the shadow sampling routines that take an explicit texture and sampler.
// It is the responsibility of the author to make sure that ShadowContext.hlsl binds the correct texture to the right slot,
// and that on the C# side the shadowContext bindDelegate binds the correct resource to the correct texture id.
//#define SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL
#ifdef  SHADOW_DISPATCH_USE_CUSTOM_PUNCTUAL
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L )
{
    Texture2DArray tex = shadowContext.tex2DArray[1];
    SamplerComparisonState compSamp = shadowContext.compSamplers[1];

    return EvalShadow_PunctualDepth( shadowContext, tex, compSamp, positionWS, shadowDataIndex, L );
}
float GetPunctualShadowAttenuation( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
    Texture2DArray tex = shadowContext.tex2DArray[1];
    SamplerComparisonState compSamp = shadowContext.compSamplers[1];

    return EvalShadow_PunctualDepth( shadowContext, tex, compSamp, positionWS, shadowDataIndex, L );
}
#endif
