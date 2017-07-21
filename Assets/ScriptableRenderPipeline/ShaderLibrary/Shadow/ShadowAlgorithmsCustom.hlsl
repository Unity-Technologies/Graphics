// This file is empty on purpose. Projects can put their custom shadow algorithms in here so they get automatically included by Shadow.hlsl.

float EvalShadow_CascadedMoment( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L )
{
	ShadowData sd = shadowContext.shadowDatas[shadowDataIndex];
	// normal based bias
	positionWS += EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L ) ), sd.texelSizeRcp.zw, sd.normalBias );

	// load the right shadow data for the current face
	float4 dirShadowSplitSpheres[4];
	uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, shadowDataIndex, dirShadowSplitSpheres );
	int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres );
	if (shadowSplitIndex < 0)
		return 1.0;

	// get the shadowmap data for the correct cascade
	sd = shadowContext.shadowDatas[shadowDataIndex + 1 + shadowSplitIndex];

	// get shadowmap texcoords
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );

	// sample the texture
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );

	uint shadowType, shadowAlgorithm;
	UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );

	switch( shadowAlgorithm )
	{
	case (GPUSHADOWALGORITHM_CUSTOM + 1): return 1.0;
	default: return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithm, texIdx, sampIdx );
	}
}

float EvalShadow_CascadedMoment( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int shadowDataIndex, float3 L, float2 unPositionSS )
{
	return EvalShadow_CascadedMoment( shadowContext, positionWS, normalWS, shadowDataIndex, L );
}