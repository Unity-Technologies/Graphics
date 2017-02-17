// This file is empty on purpose. Projects can put their custom shadow algorithms in here so they get automatically included by Shadow.hlsl.

float EvalShadow_CascadedMomentum( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L )
{
	return 1.0;
}

float EvalShadow_CascadedMomentum( ShadowContext shadowContext, float3 positionWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
	return 1.0;
}