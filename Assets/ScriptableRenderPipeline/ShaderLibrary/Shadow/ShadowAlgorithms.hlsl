// Various shadow algorithms
// There are two variants provided, one takes the texture and sampler explicitly so they can be statically passed in.
// The variant without resource parameters dynamically accesses the texture when sampling.

// Helper function to offset depth based on the surface normal and light direction.
// If the light hits the surface perpendicularly there will be no offset.
float3 EvalShadow_NormalBias( float3 normalWS, float NoL, float2 texelSize, float normalBias )
{
	return max( texelSize.x, texelSize.y ) * normalBias * (1.0 - NoL) * normalWS;
}

// function called by spot, point and directional eval routines to calculate shadow coordinates
float3 EvalShadow_GetTexcoords( ShadowData sd, float3 positionWS )
{
	float4 posCS = mul( float4( positionWS, 1.0 ), sd.worldToShadow );
	posCS.z -= sd.bias * posCS.w;
	float3 posNDC = posCS.xyz / posCS.w;
	// calc TCs
	float3 posTC = posNDC * 0.5 + 0.5;
	posTC.xy = posTC.xy * sd.scaleOffset.xy + sd.scaleOffset.zw;
#if UNITY_REVERSED_Z
	posTC.z = 1.0 - posTC.z;
#endif
	return posTC;
}

int EvalShadow_GetCubeFaceID( float3 dir )
{
	// TODO: Use faceID intrinsic on console
	float3 adir = abs(dir);

	// +Z -Z
	int faceIndex = dir.z > 0.0 ? CUBEMAPFACE_NEGATIVE_Z : CUBEMAPFACE_POSITIVE_Z;

	// +X -X
	if (adir.x > adir.y && adir.x > adir.z)
	{
		faceIndex = dir.x > 0.0 ? CUBEMAPFACE_NEGATIVE_X : CUBEMAPFACE_POSITIVE_X;
	}
	// +Y -Y
	else if (adir.y > adir.x && adir.y > adir.z)
	{
		faceIndex = dir.y > 0.0 ? CUBEMAPFACE_NEGATIVE_Y : CUBEMAPFACE_POSITIVE_Y;
	}
	return faceIndex;
}


//
//	Point shadows
//
float EvalShadow_PointDepth( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int index, float4 L )
{
	ShadowData sd = shadowContext.shadowDatas[index];
	float3 biased_posWS = positionWS + EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L.xyz ) ), sd.texelSizeRcp.zw, sd.normalBias );
	float3 lpos   = positionWS + L.xyz * L.w;
	positionWS    = biased_posWS;
	int faceIndex = EvalShadow_GetCubeFaceID( lpos - biased_posWS ) + 1;
	// load the right shadow data for the current face
	sd = shadowContext.shadowDatas[index + faceIndex];
	uint payloadOffset = GetPayloadOffset( sd );
	// normal based bias
	positionWS += EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L.xyz ) ), sd.texelSizeRcp.zw, sd.normalBias );
	// get shadowmap texcoords
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );
	// get the algorithm
	uint shadowType, shadowAlgorithm;
	UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );
	// sample the texture according to the given algorithm
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithm, texIdx, sampIdx );
}

#define EvalShadow_PointDepth_( _samplerType )																																			\
	float EvalShadow_PointDepth( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, float3 positionWS, float3 normalWS, int index, float4 L )	\
	{																																													\
		ShadowData sd = shadowContext.shadowDatas[index];                                                                                                                               \
		float3 biased_posWS = positionWS + EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L.xyz ) ), sd.texelSizeRcp.zw, sd.normalBias );                                    \
		float3 lpos   = positionWS + L.xyz * L.w;                                                                                                                                       \
		positionWS    = biased_posWS;                                                                                                                                                   \
		int faceIndex = EvalShadow_GetCubeFaceID( lpos - biased_posWS ) + 1;                                                                                                            \
		/* load the right shadow data for the current face */																															\
		sd = shadowContext.shadowDatas[index + faceIndex];																													            \
		uint payloadOffset = GetPayloadOffset( sd );																																	\
		/* normal based bias */																																							\
		positionWS += EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L.xyz ) ), sd.texelSizeRcp.zw, sd.normalBias );															\
		/* get shadowmap texcoords */																																					\
		float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );																														\
		/* sample the texture */																																						\
		float slice;																																									\
		UnpackShadowmapId( sd.id, slice );																																				\
		return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithm, tex, samp );														\
	}
	EvalShadow_PointDepth_( SamplerComparisonState )
	EvalShadow_PointDepth_( SamplerState )
#undef EvalShadow_PointDepth_

//
//	Spot shadows
//
float EvalShadow_SpotDepth( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int index, float3 L )
{
	// load the right shadow data for the current face
	ShadowData sd = shadowContext.shadowDatas[index];
	uint payloadOffset = GetPayloadOffset( sd );
	// normal based bias
	positionWS += EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L ) ), sd.texelSizeRcp.zw, sd.normalBias );
	// get shadowmap texcoords
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );
	// get the algorithm
	uint shadowType, shadowAlgorithm;
	UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );
	// sample the texture according to the given algorithm
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithm, texIdx, sampIdx );
}

#define EvalShadow_SpotDepth_( _samplerType )																																		\
	float EvalShadow_SpotDepth( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, float3 positionWS, float3 normalWS, int index, float3 L )	\
	{																																												\
		/* load the right shadow data for the current face */																														\
		ShadowData sd = shadowContext.shadowDatas[index];																															\
		uint payloadOffset = GetPayloadOffset( sd );																																\
		/* normal based bias */																																						\
		positionWS += EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L ) ), sd.texelSizeRcp.zw, sd.normalBias );														    \
		/* get shadowmap texcoords */																																				\
		float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );																													\
		/* sample the texture */																																					\
		float slice;																																								\
		UnpackShadowmapId( sd.id, slice );																																			\
		return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithm, tex, samp );													\
	}
	EvalShadow_SpotDepth_( SamplerComparisonState )
	EvalShadow_SpotDepth_( SamplerState )
#undef EvalShadow_SpotDepth_

//
//	Punctual shadows for Point and Spot
//
float EvalShadow_PunctualDepth( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int index, float4 L )
{
	// load the right shadow data for the current face
	int faceIndex = 0;
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];
	uint shadowType, shadowAlgorithm;
	UnpackShadowType( sd.shadowType, shadowType );

	[branch]
	if( shadowType == GPUSHADOWTYPE_POINT )
	{
		float3 biased_posWS = positionWS + EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L.xyz ) ), sd.texelSizeRcp.zw, sd.normalBias );
		float3 lpos = positionWS + L.xyz * L.w;
		positionWS  = biased_posWS;
		faceIndex   = EvalShadow_GetCubeFaceID( lpos - biased_posWS ) + 1;
	}
	else
		positionWS += EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L.xyz ) ), sd.texelSizeRcp.zw, sd.normalBias );

	sd = shadowContext.shadowDatas[index + faceIndex];
	uint payloadOffset = GetPayloadOffset( sd );
	// get shadowmap texcoords
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );
	// sample the texture according to the given algorithm
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );
	return SampleShadow_SelectAlgorithm(shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithm, texIdx, sampIdx);
}

#define EvalShadow_PunctualDepth_( _samplerType )																																		\
	float EvalShadow_PunctualDepth( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, float3 positionWS, float3 normalWS, int index, float4 L )	\
	{																																													\
		/* load the right shadow data for the current face */																															\
		int faceIndex = 0;																																								\
		/* get the shadow type */																																						\
		ShadowData sd = shadowContext.shadowDatas[index];                                                                                                                               \
		uint shadowType;																																								\
		UnpackShadowType( sd.shadowType, shadowType );																									                                \
																																														\
		[branch]																																										\
		if( shadowType == GPUSHADOWTYPE_POINT )																																			\
		{																																												\
			float3 biased_posWS = positionWS + EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L.xyz ) ), sd.texelSizeRcp.zw, sd.normalBias );                                \
			float3 lpos = positionWS + L.xyz * L.w;                                                                                                                                     \
			positionWS  = biased_posWS;                                                                                                                                                 \
			faceIndex   = EvalShadow_GetCubeFaceID( lpos - biased_posWS ) + 1;																											\
		}																																												\
		else																																											\
			positionWS += EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L.xyz ) ), sd.texelSizeRcp.zw, sd.normalBias );														\
																																														\
		sd = shadowContext.shadowDatas[index + faceIndex];																													            \
		uint payloadOffset = GetPayloadOffset( sd );																																	\
		/* get shadowmap texcoords */																																					\
		float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );																														\
		/* sample the texture */																																						\
		float slice;																																									\
		UnpackShadowmapId( sd.id, slice );																																				\
		return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithm, tex, samp );														\
	}
	EvalShadow_PunctualDepth_( SamplerComparisonState )
	EvalShadow_PunctualDepth_( SamplerState )
#undef EvalShadow_PunctualDepth_

//
//	Directional shadows (cascaded shadow map)
//
int EvalShadow_GetSplitSphereIndexForDirshadows( float3 positionWS, float4 dirShadowSplitSpheres[4] )
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

	int idx = int( 4.0 - dot( weights, float4( 4.0, 3.0, 2.0, 1.0 ) ) );
	return idx <= 3 ? idx : -1;
}

uint EvalShadow_LoadSplitSpheres( ShadowContext shadowContext, int index, out float4 splitSpheres[4] )
{
	uint offset = GetPayloadOffset( shadowContext.shadowDatas[index] );

	splitSpheres[0] = asfloat( shadowContext.payloads[offset + 0] );
	splitSpheres[1] = asfloat( shadowContext.payloads[offset + 1] );
	splitSpheres[2] = asfloat( shadowContext.payloads[offset + 2] );
	splitSpheres[3] = asfloat( shadowContext.payloads[offset + 3] );
	return offset + 4;
}

float EvalShadow_CascadedDepth( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int index, float3 L )
{
	ShadowData sd = shadowContext.shadowDatas[index];
	// normal based bias
	positionWS += EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L ) ), sd.texelSizeRcp.zw, sd.normalBias );

	// load the right shadow data for the current face
	float4 dirShadowSplitSpheres[4];
	uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );
	int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres );
	if( shadowSplitIndex < 0 )
		return 1.0;

	sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];
	// get shadowmap texcoords
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );

	// sample the texture
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );

	uint shadowType, shadowAlgorithm;
	UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );

	return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithm, texIdx, sampIdx );
}

#define EvalShadow_CascadedDepth_( _samplerType ) 																																		\
	float EvalShadow_CascadedDepth( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, float3 positionWS, float3 normalWS, int index, float3 L ) \
	{																																													\
		ShadowData sd = shadowContext.shadowDatas[index];																										                        \
		/* normal based bias */																																							\
		positionWS += EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L ) ), sd.texelSizeRcp.zw, sd.normalBias );															    \
																																														\
		/* load the right shadow data for the current face */																															\
		float4 dirShadowSplitSpheres[4];																																				\
		uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );																				\
		int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres );																		\
		if( shadowSplitIndex < 0 )                                                                                                                                                      \
			return 1.0;                                                                                                                                                                 \
																																														\
		sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];																										            \
		/* get shadowmap texcoords */																																					\
		float3 posTC = EvalShadow_GetTexcoords( sd, positionWS );																														\
		/* sample the texture */																																						\
		float slice;																																									\
		UnpackShadowmapId( sd.id, slice );																																				\
																																														\
		return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithm, tex, samp );														\
	}
	EvalShadow_CascadedDepth_( SamplerComparisonState )
	EvalShadow_CascadedDepth_( SamplerState )
#undef EvalShadow_CascadedDepth_
