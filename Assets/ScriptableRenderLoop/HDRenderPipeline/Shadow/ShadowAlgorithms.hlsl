
// Various shadow algorithms
// There are two variants provided, one takes the texture and sampler explicitly so they can be statically passed in.
// The variant without resource parameters dynamically accesses the texture when sampling.

// function called by spot, point and directional eval routines to calculate shadow coordinates
float3 EvalShadow_GetTexcoords( ShadowData sd, float3 positionWS )
{
	float4 posCS = mul( float4( positionWS, 1.0 ), sd.worldToShadow );
	// apply a bias
	posCS.z -= sd.bias;
	float3 posNDC = posCS.xyz / posCS.w;
	// calc TCs
	float3 posTC = posNDC * 0.5 + 0.5;
	posTC.xy = posTC.xy * sd.scaleOffset.xy + sd.scaleOffset.zw;
#if UNITY_REVERSED_Z
	posTC.z = 1.0 - posTC.z;
#endif
	return posTC;
}

float EvalShadow_PointDepth( ShadowContext shadowContext, float3 positionWS, int index, float3 L )
{
	// load the right shadow data for the current face
	int faceIndex = 0;
	GetCubeFaceID( L, faceIndex );
	ShadowData sd = shadowContext.shadowDatas[index + faceIndex];
	// get shadowmap texcoords
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );
	// sample the texture
	uint texIdx, sampIdx;
	float slice;
	unpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	return SampleShadow_T2DA( shadowContext, texIdx, shadowContext.compSamplers[sampIdx], posTC, slice ).x;
}

float EvalShadow_PointDepth( ShadowContext shadowContext, Texture2DArray tex, SamplerComparisonState compSamp, float3 positionWS, int index, float3 L )
{
	// load the right shadow data for the current face
	int faceIndex = 0;
	GetCubeFaceID( L, faceIndex );
	ShadowData sd = shadowContext.shadowDatas[index + faceIndex];
	// get shadowmap texcoords
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );
	// sample the texture
	float slice;
	unpackShadowmapId( sd.id, slice );
	return SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, posTC, slice );
}


float EvalShadow_SpotDepth( ShadowContext shadowContext, float3 positionWS, int index, float3 L )
{
	// load the right shadow data for the current face
	ShadowData sd = shadowContext.shadowDatas[index];
	// get shadowmap texcoords
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );
	// sample the texture
	uint texIdx, sampIdx;
	float slice;
	unpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	return SampleShadow_T2DA( shadowContext, texIdx, shadowContext.compSamplers[sampIdx], posTC, slice ).x;
}

float EvalShadow_SpotDepth( ShadowContext shadowContext, Texture2DArray tex, SamplerComparisonState compSamp, float3 positionWS, int index, float3 L )
{
	// load the right shadow data for the current face
	ShadowData sd = shadowContext.shadowDatas[index];
	// get shadowmap texcoords
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );
	// sample the texture
	float slice;
	unpackShadowmapId( sd.id, slice );
	return SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, posTC, slice );
}

float EvalShadow_PunctualDepth( ShadowContext shadowContext, float3 positionWS, int index, float3 L )
{
	// load the right shadow data for the current face
	int faceIndex = 0;

	[branch]
	if( shadowContext.shadowDatas[index].shadowType == GPUSHADOWTYPE_POINT )
		GetCubeFaceID( L, faceIndex );

	ShadowData sd = shadowContext.shadowDatas[index + faceIndex];
	// get shadowmap texcoords
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );
	// sample the texture
	uint texIdx, sampIdx;
	float slice;
	unpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	return SampleShadow_T2DA( shadowContext, texIdx, shadowContext.compSamplers[sampIdx], posTC, slice ).x;
}

float EvalShadow_PunctualDepth( ShadowContext shadowContext, Texture2DArray tex, SamplerComparisonState compSamp, float3 positionWS, int index, float3 L )
{
	// load the right shadow data for the current face
	int faceIndex = 0;

	[branch]
	if( shadowContext.shadowDatas[index].shadowType == GPUSHADOWTYPE_POINT )
		GetCubeFaceID( L, faceIndex );

	ShadowData sd = shadowContext.shadowDatas[index + faceIndex];
	// get shadowmap texcoords
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );
	// sample the texture
	float slice;
	unpackShadowmapId( sd.id, slice );
	return SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, posTC, slice );
}


uint EvalShadow_GetSplitSphereIndexForDirshadows( float3 positionWS, float4 dirShadowSplitSpheres[4] )
{
	float3 fromCenter0 = positionWS.xyz - dirShadowSplitSpheres[0].xyz;
	float3 fromCenter1 = positionWS.xyz - dirShadowSplitSpheres[1].xyz;
	float3 fromCenter2 = positionWS.xyz - dirShadowSplitSpheres[2].xyz;
	float3 fromCenter3 = positionWS.xyz - dirShadowSplitSpheres[3].xyz;
	float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

	float4 dirShadowSplitSphereSqRadii;
	dirShadowSplitSphereSqRadii.x = dirShadowSplitSpheres[0].w;
	dirShadowSplitSphereSqRadii.y = dirShadowSplitSpheres[1].w;
	dirShadowSplitSphereSqRadii.z = dirShadowSplitSpheres[2].w;
	dirShadowSplitSphereSqRadii.w = dirShadowSplitSpheres[3].w;

	float4 weights = float4( distances2 < dirShadowSplitSphereSqRadii );
	weights.yzw = saturate( weights.yzw - weights.xyz );

	return uint( 4.0 - dot( weights, float4(4.0, 3.0, 2.0, 1.0 ) ) );
}

void EvalShadow_LoadSplitSpheres( ShadowContext shadowContext, int index, out float4 splitSpheres[4] )
{
	uint offset = GetPayloadOffset( shadowContext.shadowDatas[index] );

	splitSpheres[0].x = asfloat( shadowContext.payloads[offset + 0] );
	splitSpheres[0].y = asfloat( shadowContext.payloads[offset + 1] );
	splitSpheres[0].z = asfloat( shadowContext.payloads[offset + 2] );
	splitSpheres[0].w = asfloat( shadowContext.payloads[offset + 3] );

	splitSpheres[1].x = asfloat( shadowContext.payloads[offset + 4] );
	splitSpheres[1].y = asfloat( shadowContext.payloads[offset + 5] );
	splitSpheres[1].z = asfloat( shadowContext.payloads[offset + 6] );
	splitSpheres[1].w = asfloat( shadowContext.payloads[offset + 7] );
								 								    
	splitSpheres[2].x = asfloat( shadowContext.payloads[offset + 8] );
	splitSpheres[2].y = asfloat( shadowContext.payloads[offset + 9] );
	splitSpheres[2].z = asfloat( shadowContext.payloads[offset +10] );
	splitSpheres[2].w = asfloat( shadowContext.payloads[offset +11] );
								 								    
	splitSpheres[3].x = asfloat( shadowContext.payloads[offset +12] );
	splitSpheres[3].y = asfloat( shadowContext.payloads[offset +13] );
	splitSpheres[3].z = asfloat( shadowContext.payloads[offset +14] );
	splitSpheres[3].w = asfloat( shadowContext.payloads[offset +15] );
}

float EvalShadow_CascadedDepth( ShadowContext shadowContext, float3 positionWS, int index, float3 L )
{
	// load the right shadow data for the current face
	float4 dirShadowSplitSpheres[4];
	EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );
	uint shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres );
	ShadowData sd = shadowContext.shadowDatas[index + shadowSplitIndex];
	// get shadowmap texcoords
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );

	// sample the texture
	uint texIdx, sampIdx;
	float slice;
	unpackShadowmapId( sd.id, texIdx, sampIdx, slice );

	float4 vShadow3x3PCFTerms0;
	float4 vShadow3x3PCFTerms1;
	float4 vShadow3x3PCFTerms2;
	float4 vShadow3x3PCFTerms3;

	vShadow3x3PCFTerms0 = float4( 20.0f / 267.0f, 33.0f / 267.0f, 55.0f / 267.0f, 0.0f );
	vShadow3x3PCFTerms1 = float4( sd.texelSizeRcp.x,  sd.texelSizeRcp.y, -sd.texelSizeRcp.x, -sd.texelSizeRcp.y );
	vShadow3x3PCFTerms2 = float4( sd.texelSizeRcp.x,  sd.texelSizeRcp.y, 0.0f, 0.0f );
	vShadow3x3PCFTerms3 = float4(-sd.texelSizeRcp.x, -sd.texelSizeRcp.y, 0.0f, 0.0f );

	float4 v20Taps;
	v20Taps.x = SampleShadow_T2DA( shadowContext, texIdx, shadowContext.compSamplers[sampIdx], float3( posTC.xy + vShadow3x3PCFTerms1.xy, posTC.z ), slice ).x; //  1  1
	v20Taps.y = SampleShadow_T2DA( shadowContext, texIdx, shadowContext.compSamplers[sampIdx], float3( posTC.xy + vShadow3x3PCFTerms1.zy, posTC.z ), slice ).x; // -1  1
	v20Taps.z = SampleShadow_T2DA( shadowContext, texIdx, shadowContext.compSamplers[sampIdx], float3( posTC.xy + vShadow3x3PCFTerms1.xw, posTC.z ), slice ).x; //  1 -1
	v20Taps.w = SampleShadow_T2DA( shadowContext, texIdx, shadowContext.compSamplers[sampIdx], float3( posTC.xy + vShadow3x3PCFTerms1.zw, posTC.z ), slice ).x; // -1 -1
	float flSum = dot( v20Taps.xyzw, float4( 0.25, 0.25, 0.25, 0.25 ) );
	if( ( flSum == 0.0 ) || ( flSum == 1.0 ) )
		return flSum;
	flSum *= vShadow3x3PCFTerms0.x * 4.0;

	float4 v33Taps;
	v33Taps.x = SampleShadow_T2DA( shadowContext, texIdx, shadowContext.compSamplers[sampIdx], float3( posTC.xy + vShadow3x3PCFTerms2.xz, posTC.z ), slice ).x; //  1  0
	v33Taps.y = SampleShadow_T2DA( shadowContext, texIdx, shadowContext.compSamplers[sampIdx], float3( posTC.xy + vShadow3x3PCFTerms3.xz, posTC.z ), slice ).x; // -1  0
	v33Taps.z = SampleShadow_T2DA( shadowContext, texIdx, shadowContext.compSamplers[sampIdx], float3( posTC.xy + vShadow3x3PCFTerms3.zy, posTC.z ), slice ).x; //  0 -1
	v33Taps.w = SampleShadow_T2DA( shadowContext, texIdx, shadowContext.compSamplers[sampIdx], float3( posTC.xy + vShadow3x3PCFTerms2.zy, posTC.z ), slice ).x; //  0  1
	flSum += dot(v33Taps.xyzw, vShadow3x3PCFTerms0.yyyy);

	flSum += SampleShadow_T2DA( shadowContext, texIdx, shadowContext.compSamplers[sampIdx], posTC, slice ).x * vShadow3x3PCFTerms0.z;

	return flSum;
}

float EvalShadow_CascadedDepth( ShadowContext shadowContext, Texture2DArray tex, SamplerComparisonState compSamp, float3 positionWS, int index, float3 L )
{
	// load the right shadow data for the current face
	float4 dirShadowSplitSpheres[4];
	EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );
	uint shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres );
	ShadowData sd = shadowContext.shadowDatas[index + shadowSplitIndex];
	// get shadowmap texcoords
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );
	// sample the texture
	float slice;
	unpackShadowmapId(sd.id, slice);

	float4 vShadow3x3PCFTerms0;
	float4 vShadow3x3PCFTerms1;
	float4 vShadow3x3PCFTerms2;
	float4 vShadow3x3PCFTerms3;

	vShadow3x3PCFTerms0 = float4( 20.0f / 267.0f, 33.0f / 267.0f, 55.0f / 267.0f, 0.0f );
	vShadow3x3PCFTerms1 = float4( sd.texelSizeRcp.x,  sd.texelSizeRcp.y, -sd.texelSizeRcp.x, -sd.texelSizeRcp.y );
	vShadow3x3PCFTerms2 = float4( sd.texelSizeRcp.x,  sd.texelSizeRcp.y, 0.0f, 0.0f );
	vShadow3x3PCFTerms3 = float4(-sd.texelSizeRcp.x, -sd.texelSizeRcp.y, 0.0f, 0.0f );

	float4 v20Taps;
	v20Taps.x = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( posTC.xy + vShadow3x3PCFTerms1.xy, posTC.z ), slice ).x; //  1  1
	v20Taps.y = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( posTC.xy + vShadow3x3PCFTerms1.zy, posTC.z ), slice ).x; // -1  1
	v20Taps.z = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( posTC.xy + vShadow3x3PCFTerms1.xw, posTC.z ), slice ).x; //  1 -1
	v20Taps.w = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( posTC.xy + vShadow3x3PCFTerms1.zw, posTC.z ), slice ).x; // -1 -1
	float flSum = dot( v20Taps.xyzw, float4( 0.25, 0.25, 0.25, 0.25 ) );
	if( ( flSum == 0.0 ) || ( flSum == 1.0 ) )
		return flSum;
	flSum *= vShadow3x3PCFTerms0.x * 4.0;

	float4 v33Taps;
	v33Taps.x = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( posTC.xy + vShadow3x3PCFTerms2.xz, posTC.z ), slice ).x; //  1  0
	v33Taps.y = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( posTC.xy + vShadow3x3PCFTerms3.xz, posTC.z ), slice ).x; // -1  0
	v33Taps.z = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( posTC.xy + vShadow3x3PCFTerms3.zy, posTC.z ), slice ).x; //  0 -1
	v33Taps.w = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( posTC.xy + vShadow3x3PCFTerms2.zy, posTC.z ), slice ).x; //  0  1
	flSum += dot(v33Taps.xyzw, vShadow3x3PCFTerms0.yyyy);

	flSum += SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, posTC, slice ).x * vShadow3x3PCFTerms0.z;

	return flSum;
}

