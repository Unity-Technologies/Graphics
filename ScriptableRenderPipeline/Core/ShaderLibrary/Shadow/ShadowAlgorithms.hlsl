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
float3 EvalShadow_GetTexcoords( ShadowData sd, float3 positionWS, out float3 posNDC, bool clampToRect )
{
	float4 posCS = mul(float4(positionWS, 1.0), sd.worldToShadow);
	posCS.z -= sd.bias * posCS.w;
	posNDC = posCS.xyz / posCS.w;
	// calc TCs
	float3 posTC = posNDC * 0.5 + 0.5;
	posTC.xy = clampToRect ? clamp( posTC.xy, sd.texelSizeRcp.zw*0.5, 1.0.xx - sd.texelSizeRcp.zw*0.5 ) : posTC.xy;
	posTC.xy = posTC.xy * sd.scaleOffset.xy + sd.scaleOffset.zw;
#if UNITY_REVERSED_Z
	posTC.z = 1.0 - posTC.z;
#endif
	return posTC;
}

float3 EvalShadow_GetTexcoords( ShadowData sd, float3 positionWS )
{
	float3 ndc;
	return EvalShadow_GetTexcoords( sd, positionWS, ndc, false );
}

uint2 EvalShadow_GetTexcoords( ShadowData sd, float3 positionWS, out float2 closestSampleNDC )
{
	float4 posCS = mul( float4( positionWS, 1.0 ), sd.worldToShadow );
	float2 posNDC = posCS.xy / posCS.w;
	// calc TCs
	float2 posTC = posNDC * 0.5 + 0.5;
	closestSampleNDC = (floor(posTC * sd.textureSize.zw) + 0.5) * sd.texelSizeRcp.zw * 2.0 - 1.0.xx;
	return uint2( (posTC * sd.scaleOffset.xy + sd.scaleOffset.zw) * sd.textureSize.xy );
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
	return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithm, texIdx, sampIdx );
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

#define kMaxShadowCascades 4
#define SHADOW_REPEAT_CASCADE( _x ) _x, _x, _x, _x

int EvalShadow_GetSplitSphereIndexForDirshadows( float3 positionWS, float4 dirShadowSplitSpheres[4], out float relDistance )
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
	relDistance = distances2[idx] / dirShadowSplitSphereSqRadii[idx];
	return idx <= 3 ? idx : -1;
}

int EvalShadow_GetSplitSphereIndexForDirshadows( float3 positionWS, float4 dirShadowSplitSpheres[4] )
{
	float relDist;
	return EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDist );
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

float EvalShadow_CascadedDepth_Blend( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int index, float3 L )
{
	// load the right shadow data for the current face
	float4 dirShadowSplitSpheres[4];
	uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );
	float relDistance;
	int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDistance );
	if( shadowSplitIndex < 0 )
		return 1.0;

	float4 scales = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;
	float4 borders = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;
	float border = borders[shadowSplitIndex];
	float alpha  = border <= 0.0 ? 0.0 : saturate( (relDistance - (1.0 - border)) / border );

	ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];
	// normal based bias
	float3 orig_pos = positionWS;
	uint orig_payloadOffset = payloadOffset;
	positionWS += EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L ) ), scales[shadowSplitIndex] * sd.texelSizeRcp.zw, sd.normalBias );
	// Be careful of this code, we need it here before the if statement otherwise the compiler screws up optimizing dirShadowSplitSpheres VGPRs away
	float3 splitSphere = dirShadowSplitSpheres[shadowSplitIndex].xyz;
	float3 cascadeDir  = normalize( -splitSphere + dirShadowSplitSpheres[min( shadowSplitIndex+1, kMaxShadowCascades-1 )].xyz );
	float3 wposDir     = normalize( -splitSphere + positionWS );
	float  cascDot     = dot( cascadeDir, wposDir );
		   alpha       = cascDot > 0.0 ? alpha : lerp( alpha, 0.0, saturate( -cascDot * 4.0 ) );

	// get shadowmap texcoords
	float3 posNDC;
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, true );

	// sample the texture
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );

	uint shadowType, shadowAlgorithm;
	UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );
	float shadow  = SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithm, texIdx, sampIdx );
	float shadow1 = 1.0;

	shadowSplitIndex++;
	if( shadowSplitIndex < kMaxShadowCascades )
	{
		shadow1 = shadow;

		if( alpha > 0.0 )
		{
			sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];
			positionWS = orig_pos + EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L ) ), scales[shadowSplitIndex] * sd.texelSizeRcp.zw, sd.normalBias );
			posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, false );
			// sample the texture
			UnpackShadowmapId( sd.id, slice );

			[branch]
			if( all( abs( posNDC.xy ) <= (1.0 - sd.texelSizeRcp.zw * 0.5) ) )
				shadow1 = SampleShadow_SelectAlgorithm( shadowContext, sd, orig_payloadOffset, posTC, sd.bias, slice, shadowAlgorithm, texIdx, sampIdx );
		}
	}
	shadow = lerp( shadow, shadow1, alpha );
	return shadow;
}

#define EvalShadow_CascadedDepth_( _samplerType ) 																																		                            \
	float EvalShadow_CascadedDepth_Blend( ShadowContext shadowContext, uint shadowAlgorithms[kMaxShadowCascades], Texture2DArray tex, _samplerType samp, float3 positionWS, float3 normalWS, int index, float3 L )  \
	{																																													                            \
		/* load the right shadow data for the current face */																															                            \
		float4 dirShadowSplitSpheres[kMaxShadowCascades];																																                            \
		uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );																				                            \
		float relDistance;                                                                                                                                                                                          \
		int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDistance );															                            \
		if( shadowSplitIndex < 0 )                                                                                                                                                                                  \
			return 1.0;                                                                                                                                                                                             \
																																																					\
		float4 scales = asfloat( shadowContext.payloads[payloadOffset] );                                                                                                                                           \
		payloadOffset++;                                                                                                                                                                                            \
		float4 borders = asfloat( shadowContext.payloads[payloadOffset] );                                                                                                                                          \
		payloadOffset++;                                                                                                                                                                                            \
		float border = borders[shadowSplitIndex];                                                                                                                                                                   \
		float alpha  = border <= 0.0 ? 0.0 : saturate( (relDistance - (1.0 - border)) / border );                                                                                                                   \
																																																					\
		ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];																										                            \
		/* normal based bias */																																							                            \
		float3 orig_pos = positionWS;                                                                                                                                                                               \
		uint orig_payloadOffset = payloadOffset;		                                                                                                                                                            \
		positionWS += EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L ) ), scales[shadowSplitIndex] * sd.texelSizeRcp.zw, sd.normalBias );									                            \
		/* Be careful of this code, we need it here before the if statement otherwise the compiler screws up optimizing dirShadowSplitSpheres VGPRs away */                                                         \
		float3 splitSphere = dirShadowSplitSpheres[shadowSplitIndex].xyz;                                                                                                                                           \
		float3 cascadeDir  = normalize( -splitSphere + dirShadowSplitSpheres[min( shadowSplitIndex+1, kMaxShadowCascades-1 )].xyz );                                                                                \
		float3 wposDir     = normalize( -splitSphere + positionWS );                                                                                                                                                \
		float  cascDot     = dot( cascadeDir, wposDir );                                                                                                                                                            \
			   alpha       = cascDot > 0.0 ? alpha : lerp( alpha, 0.0, saturate( -cascDot * 4.0 ) );                                                                                                                \
																																																					\
		/* get shadowmap texcoords */																																					                            \
		float3 posNDC;                                                                                                                                                                                              \
		float3 posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, true );																											                            \
																																																					\
		/* sample the texture */																																						                            \
		float slice;																																									                            \
		UnpackShadowmapId( sd.id, slice );																																				                            \
																																																					\
		float shadow  = SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithms[shadowSplitIndex], tex, samp );                                                     \
		float shadow1 = 1.0;                                                                                                                                                                                        \
																																																					\
		shadowSplitIndex++;                                                                                                                                                                                         \
		if( shadowSplitIndex < kMaxShadowCascades )                                                                                                                                                                 \
		{                                                                                                                                                                                                           \
			shadow1 = shadow;                                                                                                                                                                                       \
																																																					\
			if( alpha > 0.0 )                                                                                                                                                                                       \
			{                                                                                                                                                                                                       \
				sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];																										                                \
				positionWS = orig_pos + EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L ) ), scales[shadowSplitIndex] * sd.texelSizeRcp.zw, sd.normalBias );				                            \
				posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, false );																										                            \
				/* sample the texture */																																				                            \
				UnpackShadowmapId( sd.id, slice );																																		                            \
																																																					\
				[branch]                                                                                                                                                                                            \
				if( all( abs( posNDC.xy ) <= (1.0 - sd.texelSizeRcp.zw * 0.5) ) )                                                                                                                                   \
					shadow1 = SampleShadow_SelectAlgorithm( shadowContext, sd, orig_payloadOffset, posTC, sd.bias, slice, shadowAlgorithms[shadowSplitIndex], tex, samp );                                          \
			}                                                                                                                                                                                                       \
		}                                                                                                                                                                                                           \
		shadow = lerp( shadow, shadow1, alpha );                                                                                                                                                                    \
		return shadow;                                                                                                                                                                                              \
	}                                                                                                                                                                                                               \
																																																					\
	float EvalShadow_CascadedDepth_Blend( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, float3 positionWS, float3 normalWS, int index, float3 L )                       \
	{                                                                                                                                                                                                               \
		uint shadowAlgorithms[kMaxShadowCascades] = { SHADOW_REPEAT_CASCADE( shadowAlgorithm ) };                                                                                                                   \
		return EvalShadow_CascadedDepth_Blend( shadowContext, shadowAlgorithms, tex, samp, positionWS, normalWS, index, L );                                                                                        \
	}

	EvalShadow_CascadedDepth_( SamplerComparisonState )
	EvalShadow_CascadedDepth_( SamplerState )
#undef EvalShadow_CascadedDepth_


float EvalShadow_hash12( float2 pos )
{
	float3 p3  = frac( pos.xyx * float3( 443.8975, 397.2973, 491.1871 ) );
		   p3 += dot( p3, p3.yzx + 19.19 );
	return frac( (p3.x + p3.y) * p3.z );
}

float EvalShadow_CascadedDepth_Dither( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int index, float3 L )
{
	// load the right shadow data for the current face
	float4 dirShadowSplitSpheres[kMaxShadowCascades];
	uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );
	float relDistance;
	int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDistance );
	if( shadowSplitIndex < 0 )
		return 1.0;

	float4 scales = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;
	float4 borders = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;
	float border = borders[shadowSplitIndex];
	float alpha  = border <= 0.0 ? 0.0 : saturate( (relDistance - (1.0 - border)) / border );

	ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];
	// normal based bias
	float3 orig_pos = positionWS;
	positionWS += EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L ) ), scales[shadowSplitIndex] * sd.texelSizeRcp.zw, sd.normalBias );
	// get shadowmap texcoords
	float3 posNDC;
	float3 posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, true );

	int    nextSplit   = min( shadowSplitIndex+1, kMaxShadowCascades-1 );
	float3 splitSphere = dirShadowSplitSpheres[shadowSplitIndex].xyz;
	float3 cascadeDir  = normalize( -splitSphere + dirShadowSplitSpheres[min( 3, shadowSplitIndex + 1 )].xyz );
	float3 wposDir     = normalize( -splitSphere + positionWS );
	float  cascDot     = dot( cascadeDir, wposDir );
		   alpha       = cascDot > 0.0 ? alpha : lerp( alpha, 0.0, saturate( -cascDot * 4.0 ) );

	if( shadowSplitIndex < nextSplit && step( EvalShadow_hash12( posTC.xy ), alpha ) )
	{
		sd         = shadowContext.shadowDatas[index + 1 + nextSplit];
		positionWS = orig_pos + EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L ) ), scales[nextSplit] * sd.texelSizeRcp.zw, sd.normalBias );
		posTC      = EvalShadow_GetTexcoords( sd, positionWS );
	}
	// sample the texture
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );

	uint shadowType, shadowAlgorithm;
	UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );

	float shadow = SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithm, texIdx, sampIdx );
	return shadowSplitIndex < (kMaxShadowCascades-1) ? shadow : lerp( shadow, 1.0, alpha );
}

#define EvalShadow_CascadedDepth_( _samplerType ) 																																		                            \
	float EvalShadow_CascadedDepth_Dither( ShadowContext shadowContext, uint shadowAlgorithms[kMaxShadowCascades], Texture2DArray tex, _samplerType samp, float3 positionWS, float3 normalWS, int index, float3 L ) \
	{																																													                            \
		/* load the right shadow data for the current face */																															                            \
		float4 dirShadowSplitSpheres[kMaxShadowCascades];																																                            \
		uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );																				                            \
		float relDistance;                                                                                                                                                                                          \
		int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDistance );															                            \
		if( shadowSplitIndex < 0 )                                                                                                                                                                                  \
			return 1.0;                                                                                                                                                                                             \
																																																                    \
		float4 scales = asfloat( shadowContext.payloads[payloadOffset] );                                                                                                                                           \
		payloadOffset++;                                                                                                                                                                                            \
		float4 borders = asfloat( shadowContext.payloads[payloadOffset] );                                                                                                                                          \
		payloadOffset++;                                                                                                                                                                                            \
		float border = borders[shadowSplitIndex];                                                                                                                                                                   \
		float alpha  = border <= 0.0 ? 0.0 : saturate( (relDistance - (1.0 - border)) / border );                                                                                                                   \
																																																                    \
		ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];																										                            \
		/* normal based bias */																																							                            \
		float3 orig_pos = positionWS;                                                                                                                                                                               \
		positionWS += EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L ) ), scales[shadowSplitIndex] * sd.texelSizeRcp.zw, sd.normalBias );									                            \
		/* get shadowmap texcoords */																																					                            \
		float3 posNDC;                                                                                                                                                                                              \
		float3 posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, true );																											                            \
																																																                    \
		int    nextSplit   = min( shadowSplitIndex+1, kMaxShadowCascades-1 );                                                                                                                                       \
		float3 splitSphere = dirShadowSplitSpheres[shadowSplitIndex].xyz;                                                                                                                                           \
		float3 cascadeDir  = normalize( -splitSphere + dirShadowSplitSpheres[nextSplit].xyz );                                                                                                                      \
		float3 wposDir     = normalize( -splitSphere + positionWS );                                                                                                                                                \
		float  cascDot     = dot( cascadeDir, wposDir );                                                                                                                                                            \
			   alpha       = cascDot > 0.0 ? alpha : lerp( alpha, 0.0, saturate( -cascDot * 4.0 ) );                                                                                                                \
																																																                    \
		if( shadowSplitIndex != nextSplit && step( EvalShadow_hash12( posTC.xy ), alpha ) )                                                                                                                         \
		{                                                                                                                                                                                                           \
			sd         = shadowContext.shadowDatas[index + 1 + nextSplit];                                                                                                                                          \
			positionWS = orig_pos + EvalShadow_NormalBias( normalWS, saturate( dot( normalWS, L ) ), scales[nextSplit] * sd.texelSizeRcp.zw, sd.normalBias );				                                        \
			posTC      = EvalShadow_GetTexcoords( sd, positionWS );                                                                                                                                                 \
		}                                                                                                                                                                                                           \
		/* sample the texture */																																						                            \
		float slice;																																									                            \
		UnpackShadowmapId( sd.id, slice );																																				                            \
		float shadow = SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sd.bias, slice, shadowAlgorithms[shadowSplitIndex], tex, samp );                                                      \
		return shadowSplitIndex < (kMaxShadowCascades-1) ? shadow : lerp( shadow, 1.0, alpha );                                                                                                                     \
	}                                                                                                                                                                                                               \
																																																                    \
	float EvalShadow_CascadedDepth_Dither( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, float3 positionWS, float3 normalWS, int index, float3 L )                      \
	{                                                                                                                                                                                                               \
		uint shadowAlgorithms[kMaxShadowCascades] = { SHADOW_REPEAT_CASCADE( shadowAlgorithm ) };                                                                                                                   \
		return EvalShadow_CascadedDepth_Dither( shadowContext, shadowAlgorithms, tex, samp, positionWS, normalWS, index, L );                                                                                       \
	}


	EvalShadow_CascadedDepth_( SamplerComparisonState )
	EvalShadow_CascadedDepth_( SamplerState )
#undef EvalShadow_CascadedDepth_


//------------------------------------------------------------------------------------------------------------------------------------

float3 EvalShadow_GetClosestSample_Point( ShadowContext shadowContext, float3 positionWS, int index, float3 L )
{
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];
	// load the right shadow data for the current face
	int faceIndex = EvalShadow_GetCubeFaceID( L ) + 1;
	sd = shadowContext.shadowDatas[index + faceIndex];

	float4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy );

	// load the texel
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	closestNDC.z = LoadShadow_T2DA( shadowContext, texIdx, texelIdx, slice );

	// reconstruct depth position
	float4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}


float3 EvalShadow_GetClosestSample_Point( ShadowContext shadowContext, Texture2DArray tex, float3 positionWS, int index, float3 L )
{
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];
	// load the right shadow data for the current face
	int faceIndex = EvalShadow_GetCubeFaceID( L ) + 1;
	sd = shadowContext.shadowDatas[index + faceIndex];

	float4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy );

	// load the texel
	float slice;
	UnpackShadowmapId(sd.id, slice);
	closestNDC.z = LOAD_TEXTURE2D_ARRAY_LOD( tex, texelIdx, slice, 0 ).x;

	// reconstruct depth position
	float4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}

float3 EvalShadow_GetClosestSample_Spot( ShadowContext shadowContext, float3 positionWS, int index )
{
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];

	float4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy );

	// load the texel
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	closestNDC.z = LoadShadow_T2DA( shadowContext, texIdx, texelIdx, slice );

	// reconstruct depth position
	float4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}


float3 EvalShadow_GetClosestSample_Spot( ShadowContext shadowContext, Texture2DArray tex, float3 positionWS, int index )
{
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];

	float4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy );

	// load the texel
	float slice;
	UnpackShadowmapId(sd.id, slice);
	closestNDC.z = LOAD_TEXTURE2D_ARRAY_LOD( tex, texelIdx, slice, 0 ).x;

	// reconstruct depth position
	float4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}

float3 EvalShadow_GetClosestSample_Punctual( ShadowContext shadowContext, float3 positionWS, int index, float3 L )
{
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];
	uint shadowType;
	UnpackShadowType( sd.shadowType, shadowType );
	// load the right shadow data for the current face
	int faceIndex = shadowType == GPUSHADOWTYPE_POINT ? (EvalShadow_GetCubeFaceID( L ) + 1) : 0;
	sd = shadowContext.shadowDatas[index + faceIndex];

	float4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy );

	// load the texel
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	closestNDC.z = LoadShadow_T2DA( shadowContext, texIdx, texelIdx, slice );

	// reconstruct depth position
	float4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}

float3 EvalShadow_GetClosestSample_Punctual( ShadowContext shadowContext, Texture2DArray tex, float3 positionWS, int index, float3 L )
{
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];
	uint shadowType;
	UnpackShadowType( sd.shadowType, shadowType );
	// load the right shadow data for the current face
	int faceIndex = shadowType == GPUSHADOWTYPE_POINT ? (EvalShadow_GetCubeFaceID( L ) + 1) : 0;
	sd = shadowContext.shadowDatas[index + faceIndex];

	float4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy );

	// load the texel
	float slice;
	UnpackShadowmapId(sd.id, slice);
	closestNDC.z = LOAD_TEXTURE2D_ARRAY_LOD( tex, texelIdx, slice, 0 ).x;

	// reconstruct depth position
	float4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}

float3 EvalShadow_GetClosestSample_Cascade( ShadowContext shadowContext, float3 positionWS, float3 normalWS, int index, float4 L )
{
	// load the right shadow data for the current face
	float4 dirShadowSplitSpheres[4];
	uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );
	float relDistance;
	int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDistance );
	if( shadowSplitIndex < 0 )
		return 1.0;

	float4 scales = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;
	float4 borders = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;

	ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];

	float4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy );

	// load the texel
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	closestNDC.z = LoadShadow_T2DA( shadowContext, texIdx, texelIdx, slice );

	// reconstruct depth position
	float4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}

float3 EvalShadow_GetClosestSample_Cascade( ShadowContext shadowContext, Texture2DArray tex, float3 positionWS, float3 normalWS, int index, float4 L )
{
	// load the right shadow data for the current face
	float4 dirShadowSplitSpheres[4];
	uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );
	float relDistance;
	int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDistance );
	if( shadowSplitIndex < 0 )
		return 1.0;

	float4 scales = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;
	float4 borders = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;

	ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];

	float4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy );

	// load the texel
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	closestNDC.z = LOAD_TEXTURE2D_ARRAY_LOD( tex, texelIdx, slice, 0 ).x;

	// reconstruct depth position
	float4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}
