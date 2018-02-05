// Various shadow algorithms
// There are two variants provided, one takes the texture and sampler explicitly so they can be statically passed in.
// The variant without resource parameters dynamically accesses the texture when sampling.

// function called by spot, point and directional eval routines to calculate shadow coordinates
real3 EvalShadow_GetTexcoords( ShadowData sd, real3 positionWS, out real3 posNDC, bool clampToRect, bool perspProj )
{
	real4 posCS = mul( real4( positionWS, 1.0 ), sd.worldToShadow );
	posNDC = perspProj ? (posCS.xyz / posCS.w) : posCS.xyz;
	// calc TCs
	real3 posTC = real3( posNDC.xy * 0.5 + 0.5, posNDC.z );
	posTC.xy = clampToRect ? clamp( posTC.xy, sd.texelSizeRcp.zw*0.5, 1.0.xx - sd.texelSizeRcp.zw*0.5 ) : posTC.xy;
	posTC.xy = posTC.xy * sd.scaleOffset.xy + sd.scaleOffset.zw;

	return posTC;
}

real3 EvalShadow_GetTexcoords( ShadowData sd, real3 positionWS, bool perspProj )
{
	real3 ndc;
	return EvalShadow_GetTexcoords( sd, positionWS, ndc, false, perspProj );
}

uint2 EvalShadow_GetTexcoords( ShadowData sd, real3 positionWS, out real2 closestSampleNDC, bool perspProj )
{
	real4 posCS = mul( real4( positionWS, 1.0 ), sd.worldToShadow );
	real2 posNDC = perspProj ? (posCS.xy / posCS.w) : posCS.xy;
	// calc TCs
	real2 posTC = posNDC * 0.5 + 0.5;
	closestSampleNDC = (floor(posTC * sd.textureSize.zw) + 0.5) * sd.texelSizeRcp.zw * 2.0 - 1.0.xx;
	return uint2( (posTC * sd.scaleOffset.xy + sd.scaleOffset.zw) * sd.textureSize.xy );
}

int EvalShadow_GetCubeFaceID( real3 sampleToLight )
{
	real3 lightToSample = -sampleToLight; // TODO: pass the correct (flipped) direction

#ifdef INTRINSIC_CUBEMAP_FACE_ID
	return (int)CubeMapFaceID(lightToSample);
#else
	// TODO: use CubeMapFaceID() defined in Common.hlsl for all pipelines on all platforms.
	real3 dir  = sampleToLight;
	real3 adir = abs(dir);

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
#endif
}


//
//	Biasing functions
//

// helper function to get the world texel size
real EvalShadow_WorldTexelSize( ShadowData sd, float L_dist, bool perspProj )
{
	return perspProj ? (sd.viewBias.w * L_dist) : sd.viewBias.w;
}

// used to scale down view biases to mitigate light leaking across shadowed corners
#if SHADOW_USE_VIEW_BIAS_SCALING != 0
real EvalShadow_ReceiverBiasWeightFlag( float flag )
{
	return (asint( flag ) & 2) ? 1.0 : 0.0;
}

bool EvalShadow_ReceiverBiasWeightUseNormalFlag( float flag )
{
	return (asint( flag ) & 4) ? true : false;
}

real3 EvalShadow_ReceiverBiasWeightPos( real3 positionWS, real3 normalWS, real3 L, real worldTexelSize, real tolerance, bool useNormal )
{
#if SHADOW_USE_ONLY_VIEW_BASED_BIASING != 0
	return positionWS + L * worldTexelSize * tolerance;
#else
	return positionWS + (useNormal ? normalWS : L) * worldTexelSize * tolerance;
#endif
}

real EvalShadow_ReceiverBiasWeight( ShadowContext shadowContext, uint shadowAlgorithm, ShadowData sd, uint texIdx, uint sampIdx, real3 positionWS, real3 normalWS, real3 L, real L_dist, real slice, bool perspProj )
{
	real weight = 1.0;

	[branch]
	if( shadowAlgorithm <= GPUSHADOWALGORITHM_PCF_TENT_7X7 )
	{
		real3 pos = EvalShadow_ReceiverBiasWeightPos( positionWS, normalWS, L, EvalShadow_WorldTexelSize( sd, L_dist, perspProj ), sd.edgeTolerance, EvalShadow_ReceiverBiasWeightUseNormalFlag( sd.nrmlBias.w ) );
		real3 tcs = EvalShadow_GetTexcoords( sd, pos, perspProj );
		weight = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, tcs, slice ).x;
	}
	
	return lerp( 1.0, weight, EvalShadow_ReceiverBiasWeightFlag( sd.nrmlBias.w ) );
}

real EvalShadow_ReceiverBiasWeight( ShadowData sd, Texture2DArray tex, SamplerComparisonState samp, real3 positionWS, real3 normalWS, real3 L, real L_dist, real slice, bool perspProj )
{
	real3 pos = EvalShadow_ReceiverBiasWeightPos( positionWS, normalWS, L, EvalShadow_WorldTexelSize( sd, L_dist, perspProj ), sd.edgeTolerance, EvalShadow_ReceiverBiasWeightUseNormalFlag( sd.nrmlBias.w ) );
	return lerp( 1.0, SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, samp, EvalShadow_GetTexcoords( sd, pos, perspProj ), slice ).x, EvalShadow_ReceiverBiasWeightFlag( sd.nrmlBias.w ) );
}

real EvalShadow_ReceiverBiasWeight( ShadowData sd, Texture2DArray tex, SamplerState samp, real3 positionWS, real3 normalWS, real3 L, real L_dist, real slice, bool perspProj )
{
	// only used by PCF filters
	return 1.0;
}
#else // SHADOW_USE_VIEW_BIAS_SCALING != 0
real EvalShadow_ReceiverBiasWeight( ShadowContext shadowContext, uint shadowAlgorithm, ShadowData sd, uint texIdx, uint sampIdx, real3 positionWS, real3 normalWS, real3 L, real L_dist, real slice, bool perspProj ) { return 1.0; }
real EvalShadow_ReceiverBiasWeight( ShadowData sd, Texture2DArray tex, SamplerComparisonState samp, real3 positionWS, real3 normalWS, real3 L, real L_dist, real slice, bool perspProj )                              { return 1.0; }
real EvalShadow_ReceiverBiasWeight (ShadowData sd, Texture2DArray tex, SamplerState samp, real3 positionWS, real3 normalWS, real3 L, real L_dist, real slice, bool perspProj )                                        { return 1.0; }
#endif // SHADOW_USE_VIEW_BIAS_SCALING != 0

// receiver bias either using the normal to weight normal and view biases, or just light view biasing
float3 EvalShadow_ReceiverBias( ShadowData sd, float3 positionWS, float3 normalWS, float3 L, float L_dist, float lightviewBiasWeight, bool perspProj )
{
#if SHADOW_USE_ONLY_VIEW_BASED_BIASING != 0 // only light vector based biasing
	float viewBiasScale = sd.viewBias.z;
	return positionWS + L * viewBiasScale * lightviewBiasWeight * EvalShadow_WorldTexelSize( sd, L_dist, perspProj );
#else // biasing based on the angle between the normal and the light vector
	float viewBiasMin   = sd.viewBias.x;
	float viewBiasMax   = sd.viewBias.y;
	float viewBiasScale = sd.viewBias.z;
	float nrmlBiasMin   = sd.nrmlBias.x;
	float nrmlBiasMax   = sd.nrmlBias.y;
	float nrmlBiasScale = sd.nrmlBias.z;

	float  NdotL       = saturate( dot( normalWS, L ) );
	float  sine        = sqrt( saturate( 1.0 - NdotL * NdotL ) );
	float  tangent     = abs( NdotL ) > 0.0 ? (sine / NdotL) : 0.0;
		   sine        = clamp( sine    * nrmlBiasScale, nrmlBiasMin, nrmlBiasMax );
		   tangent     = clamp( tangent * viewBiasScale * lightviewBiasWeight, viewBiasMin, viewBiasMax );
	float3 view_bias   = L        * tangent;
	float3 normal_bias = normalWS * sine;
	return positionWS + (normal_bias + view_bias) * EvalShadow_WorldTexelSize( sd, L_dist, perspProj );
#endif
}

// sample bias used by wide PCF filters to offset individual taps
#if SHADOW_USE_SAMPLE_BIASING != 0
real EvalShadow_SampleBiasFlag( float flag )
{
	return (asint( flag ) & 1) ? 1.0 : 0.0;
}


float2 EvalShadow_SampleBias_Persp( ShadowData sd, float3 positionWS, float3 normalWS, float3 tcs )
{
	float3 e1, e2;
	if( abs( normalWS.z ) > 0.65 )
	{
		e1 = float3( 1.0, 0.0, -normalWS.x / normalWS.z );
		e2 = float3( 0.0, 1.0, -normalWS.y / normalWS.z );
	}
	else if( abs( normalWS.y ) > 0.65 )
	{
		e1 = float3( 1.0, -normalWS.x / normalWS.y, 0.0 );
		e2 = float3( 0.0, -normalWS.z / normalWS.y, 1.0 );
	}
	else
	{
		e1 = float3( -normalWS.y / normalWS.x, 1.0, 0.0 );
		e2 = float3( -normalWS.z / normalWS.x, 0.0, 1.0 );
	}

	float4 p1 = mul( float4( positionWS + e1, 1.0 ), sd.worldToShadow );
	float4 p2 = mul( float4( positionWS + e2, 1.0 ), sd.worldToShadow );

	p1.xyz /= p1.w;
	p2.xyz /= p2.w;

	p1.xyz = float3( p1.xy * 0.5 + 0.5, p1.z );
	p2.xyz = float3( p2.xy * 0.5 + 0.5, p2.z );

	p1.xy = p1.xy * sd.scaleOffset.xy + sd.scaleOffset.zw;
	p2.xy = p2.xy * sd.scaleOffset.xy + sd.scaleOffset.zw;

	float3 nrm     = cross( p1.xyz - tcs, p2.xyz - tcs );
		   nrm.xy /= -nrm.z;

	return isfinite( nrm.xy ) ? (EvalShadow_SampleBiasFlag( sd.nrmlBias.w ) * nrm.xy) : 0.0.xx;
}

float2 EvalShadow_SampleBias_Ortho( ShadowData sd, float3 normalWS )
{
	float3 nrm = mul( (float3x3) sd.shadowToWorld, normalWS );

	nrm.x *= sd.scaleOffset.y;
	nrm.y *= sd.scaleOffset.x;
	nrm.z *= sd.scaleOffset.x * sd.scaleOffset.y;

	nrm.xy /= -nrm.z;

	return isfinite( nrm.xy ) ? (EvalShadow_SampleBiasFlag( sd.nrmlBias.w ) * nrm.xy) : 0.0.xx;
}
#else // SHADOW_USE_SAMPLE_BIASING != 0
float2 EvalShadow_SampleBias_Persp( ShadowData sd, float3 positionWS, float3 normalWS, float3 tcs ) { return 0.0.xx; }
float2 EvalShadow_SampleBias_Ortho( ShadowData sd, float3 normalWS )                                { return 0.0.xx; }
#endif // SHADOW_USE_SAMPLE_BIASING != 0


//
//	Point shadows
//
real EvalShadow_PointDepth( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int index, real3 L, real L_dist )
{
	ShadowData sd = shadowContext.shadowDatas[index + EvalShadow_GetCubeFaceID( L ) + 1];
	// get the algorithm
	uint shadowType, shadowAlgorithm;
	UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );
	// get the texture 
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	// bias the world position
	float recvBiasWeight = EvalShadow_ReceiverBiasWeight( shadowContext, shadowAlgorithm, sd, texIdx, sampIdx, positionWS, normalWS, L, L_dist, slice, true );
	positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, L_dist, recvBiasWeight, true );
	// get shadowmap texcoords
	real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, true );
	// get the per sample bias
	real2 sampleBias = EvalShadow_SampleBias_Persp( sd, positionWS, normalWS, posTC );
	// sample the texture according to the given algorithm
	uint payloadOffset = GetPayloadOffset( sd );
	return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, slice, shadowAlgorithm, texIdx, sampIdx );
}

#define EvalShadow_PointDepth_( _samplerType )																																			        \
	real EvalShadow_PointDepth( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, real3 positionWS, real3 normalWS, int index, real3 L, real L_dist )	\
	{																																													        \
		ShadowData sd = shadowContext.shadowDatas[index + EvalShadow_GetCubeFaceID( L ) + 1];                                                                                                   \
		float slice;																																									        \
		UnpackShadowmapId( sd.id, slice );																																				        \
		/* bias the world position */                                                                                                                                                           \
		real recvBiasWeight = EvalShadow_ReceiverBiasWeight( sd, tex, samp, positionWS, normalWS, L, L_dist, slice, true );                                                                     \
		positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, L_dist, recvBiasWeight, true );	                                                                                    \
		/* get shadowmap texcoords */																																					        \
		real3  posTC = EvalShadow_GetTexcoords( sd, positionWS, true );																														    \
		/* get the per sample bias */                                                                                                                                                           \
		real2  sampleBias = EvalShadow_SampleBias_Persp( sd, positionWS, normalWS, posTC );                                                                                                     \
		/* sample the texture */																																						        \
		uint payloadOffset = GetPayloadOffset( sd );																																	        \
		return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, slice, shadowAlgorithm, tex, samp );														    \
	}
	EvalShadow_PointDepth_( SamplerComparisonState )
	EvalShadow_PointDepth_( SamplerState )
#undef EvalShadow_PointDepth_

//
//	Spot shadows
//
real EvalShadow_SpotDepth( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int index, real3 L, real L_dist )
{
	// load the right shadow data for the current face
	ShadowData sd = shadowContext.shadowDatas[index];
	// get the algorithm
	uint shadowType, shadowAlgorithm;
	UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );
	// sample the texture according to the given algorithm
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	// bias the world position
	real recvBiasWeight = EvalShadow_ReceiverBiasWeight( shadowContext, shadowAlgorithm, sd, texIdx, sampIdx, positionWS, normalWS, L, L_dist, slice, true );
	positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, L_dist, recvBiasWeight, true );
	// get shadowmap texcoords
	real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, true );
	// get the per sample bias
	real2 sampleBias = EvalShadow_SampleBias_Persp( sd, positionWS, normalWS, posTC );
	// sample the texture according to the given algorithm
	uint payloadOffset = GetPayloadOffset( sd );
	return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, slice, shadowAlgorithm, texIdx, sampIdx );
}

#define EvalShadow_SpotDepth_( _samplerType )																																		            \
	real EvalShadow_SpotDepth( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, real3 positionWS, real3 normalWS, int index, real3 L, real L_dist )	\
	{																																												            \
		/* load the right shadow data for the current face */																														            \
		ShadowData sd = shadowContext.shadowDatas[index];																															            \
		float slice;																																								            \
		UnpackShadowmapId( sd.id, slice );																																			            \
		/* bias the world position */                                                                                                                                                           \
		real recvBiasWeight = EvalShadow_ReceiverBiasWeight( sd, tex, samp, positionWS, normalWS, L, L_dist, slice, true );                                                                     \
		positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, L_dist, recvBiasWeight, true );	                                                                                    \
		/* get shadowmap texcoords */																																				            \
		real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, true );																													        \
		/* get the per sample bias */                                                                                                                                                           \
		real2  sampleBias = EvalShadow_SampleBias_Persp( sd, positionWS, normalWS, posTC );                                                                                                     \
		/* sample the texture */																																					            \
		uint   payloadOffset = GetPayloadOffset( sd );																																            \
		return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, slice, shadowAlgorithm, tex, samp );													        \
	}
	EvalShadow_SpotDepth_( SamplerComparisonState )
	EvalShadow_SpotDepth_( SamplerState )
#undef EvalShadow_SpotDepth_

//
//	Punctual shadows for Point and Spot
//
real EvalShadow_PunctualDepth( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int index, real3 L, real L_dist )
{
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];
	uint shadowType, shadowAlgorithm;
	UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );

	// load the right shadow data for the current face
	[branch]
	if( shadowType == GPUSHADOWTYPE_POINT )
	{
		sd.worldToShadow  = shadowContext.shadowDatas[index + EvalShadow_GetCubeFaceID( L ) + 1].worldToShadow;
		sd.shadowToWorld  = shadowContext.shadowDatas[index + EvalShadow_GetCubeFaceID( L ) + 1].shadowToWorld;
		sd.scaleOffset.zw = shadowContext.shadowDatas[index + EvalShadow_GetCubeFaceID( L ) + 1].scaleOffset.zw;
	}
	
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	// bias the world position
	float recvBiasWeight = EvalShadow_ReceiverBiasWeight( shadowContext, shadowAlgorithm, sd, texIdx, sampIdx, positionWS, normalWS, L, L_dist, slice, true );
	positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, L_dist, recvBiasWeight, true );
	// get shadowmap texcoords
	real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, true );
	// get the per sample bias
	real2 sampleBias = EvalShadow_SampleBias_Persp( sd, positionWS, normalWS, posTC );
	// sample the texture according to the given algorithm
	uint payloadOffset = GetPayloadOffset( sd );
	return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, slice, shadowAlgorithm, texIdx, sampIdx );
}

#define EvalShadow_PunctualDepth_( _samplerType )																																		            \
	real EvalShadow_PunctualDepth( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, real3 positionWS, real3 normalWS, int index, real3 L, real L_dist )    \
	{																																													            \
		int faceIndex = 0;																																								            \
		/* get the shadow type */																																						            \
		ShadowData sd = shadowContext.shadowDatas[index];                                                                                                                                           \
		uint shadowType;																																								            \
		UnpackShadowType( sd.shadowType, shadowType );																									                                            \
																																																	\
		/* load the right shadow data for the current face */																															            \
		[branch]																																										            \
		if( shadowType == GPUSHADOWTYPE_POINT )																																			            \
		{                                                                                                                                                                                           \
			sd.worldToShadow  = shadowContext.shadowDatas[index + EvalShadow_GetCubeFaceID( L ) + 1].worldToShadow;																					\
			sd.shadowToWorld  = shadowContext.shadowDatas[index + EvalShadow_GetCubeFaceID( L ) + 1].shadowToWorld;																					\
			sd.scaleOffset.zw = shadowContext.shadowDatas[index + EvalShadow_GetCubeFaceID( L ) + 1].scaleOffset.zw;                                                                                \
		}                                                                                                                                                                                           \
																																																	\
		float slice;																																									            \
		UnpackShadowmapId( sd.id, slice );																																				            \
		/* bias the world position */                                                                                                                                                               \
		real recvBiasWeight = EvalShadow_ReceiverBiasWeight( sd, tex, samp, positionWS, normalWS, L, L_dist, slice, true );                                                                         \
		positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, L_dist, recvBiasWeight, true );	                                                                                        \
		/* get shadowmap texcoords */																																					            \
		real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, true );																														        \
		/* get the per sample bias */                                                                                                                                                               \
		real2  sampleBias = EvalShadow_SampleBias_Persp( sd, positionWS, normalWS, posTC );                                                                                                         \
		/* sample the texture */																																						            \
		uint   payloadOffset = GetPayloadOffset( sd );																																	            \
		return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, slice, shadowAlgorithm, tex, samp );														        \
	}
	EvalShadow_PunctualDepth_( SamplerComparisonState )
	EvalShadow_PunctualDepth_( SamplerState )
#undef EvalShadow_PunctualDepth_

//
//	Directional shadows (cascaded shadow map)
//

#define kMaxShadowCascades 4
#define SHADOW_REPEAT_CASCADE( _x ) _x, _x, _x, _x

int EvalShadow_GetSplitSphereIndexForDirshadows( real3 positionWS, real4 dirShadowSplitSpheres[4], out real relDistance )
{
	real3 fromCenter0 = positionWS.xyz - dirShadowSplitSpheres[0].xyz;
	real3 fromCenter1 = positionWS.xyz - dirShadowSplitSpheres[1].xyz;
	real3 fromCenter2 = positionWS.xyz - dirShadowSplitSpheres[2].xyz;
	real3 fromCenter3 = positionWS.xyz - dirShadowSplitSpheres[3].xyz;
	real4 distances2 = real4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

	real4 dirShadowSplitSphereSqRadii;
	dirShadowSplitSphereSqRadii.x = dirShadowSplitSpheres[0].w;
	dirShadowSplitSphereSqRadii.y = dirShadowSplitSpheres[1].w;
	dirShadowSplitSphereSqRadii.z = dirShadowSplitSpheres[2].w;
	dirShadowSplitSphereSqRadii.w = dirShadowSplitSpheres[3].w;

	real4 weights = real4( distances2 < dirShadowSplitSphereSqRadii );
	weights.yzw = saturate( weights.yzw - weights.xyz );

	int idx = int( 4.0 - dot( weights, real4( 4.0, 3.0, 2.0, 1.0 ) ) );
	relDistance = distances2[idx] / dirShadowSplitSphereSqRadii[idx];
	return idx <= 3 ? idx : -1;
}

int EvalShadow_GetSplitSphereIndexForDirshadows( real3 positionWS, real4 dirShadowSplitSpheres[4] )
{
	real relDist;
	return EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDist );
}

uint EvalShadow_LoadSplitSpheres( ShadowContext shadowContext, int index, out real4 splitSpheres[4] )
{
	uint offset = GetPayloadOffset( shadowContext.shadowDatas[index] );

	splitSpheres[0] = asfloat( shadowContext.payloads[offset + 0] );
	splitSpheres[1] = asfloat( shadowContext.payloads[offset + 1] );
	splitSpheres[2] = asfloat( shadowContext.payloads[offset + 2] );
	splitSpheres[3] = asfloat( shadowContext.payloads[offset + 3] );
	return offset + 4;
}

real EvalShadow_CascadedDepth_Blend( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int index, real3 L )
{
	// load the right shadow data for the current face
	real4 dirShadowSplitSpheres[4];
	uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );
	real relDistance;
	int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDistance );
	if( shadowSplitIndex < 0 )
		return 1.0;

	real4 scales = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;
	real4 borders = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;
	real border = borders[shadowSplitIndex];
	real alpha  = border <= 0.0 ? 0.0 : saturate( (relDistance - (1.0 - border)) / border );

	ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];

	// sample the texture
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	uint shadowType, shadowAlgorithm;
	UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );

	// normal based bias
	real3 orig_pos = positionWS;
	uint orig_payloadOffset = payloadOffset;
	real recvBiasWeight = EvalShadow_ReceiverBiasWeight( shadowContext, shadowAlgorithm, sd, texIdx, sampIdx, positionWS, normalWS, L, 1.0, slice, false );
	positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, 1.0, recvBiasWeight, false );
	// Be careful of this code, we need it here before the if statement otherwise the compiler screws up optimizing dirShadowSplitSpheres VGPRs away
	real3 splitSphere = dirShadowSplitSpheres[shadowSplitIndex].xyz;
	real3 cascadeDir  = normalize( -splitSphere + dirShadowSplitSpheres[min( shadowSplitIndex+1, kMaxShadowCascades-1 )].xyz );
	real3 wposDir     = normalize( -splitSphere + positionWS );
	real  cascDot     = dot( cascadeDir, wposDir );
		  alpha       = cascDot > 0.0 ? alpha : lerp( alpha, 0.0, saturate( -cascDot * 4.0 ) );

	// get shadowmap texcoords
	real3 posNDC;
	real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, true, false );

	// evaluate the first cascade
	real2 sampleBias = EvalShadow_SampleBias_Ortho( sd, normalWS );
	real  shadow     = SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, slice, shadowAlgorithm, texIdx, sampIdx );
	real  shadow1    = 1.0;

	shadowSplitIndex++;
	if( shadowSplitIndex < kMaxShadowCascades )
	{
		shadow1 = shadow;

		if( alpha > 0.0 )
		{
			sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];
			positionWS = EvalShadow_ReceiverBias( sd, orig_pos, normalWS, L, 1.0, recvBiasWeight, false );
			posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, false, false );
			// sample the texture
			UnpackShadowmapId( sd.id, slice );
			sampleBias = EvalShadow_SampleBias_Ortho( sd, normalWS );

			[branch]
			if( all( abs( posNDC.xy ) <= (1.0 - sd.texelSizeRcp.zw * 0.5) ) )
				shadow1 = SampleShadow_SelectAlgorithm( shadowContext, sd, orig_payloadOffset, posTC, sampleBias, slice, shadowAlgorithm, texIdx, sampIdx );
		}
	}
	shadow = lerp( shadow, shadow1, alpha );
	return shadow;
}

#define EvalShadow_CascadedDepth_( _samplerType ) 																																		                            \
	real EvalShadow_CascadedDepth_Blend( ShadowContext shadowContext, uint shadowAlgorithms[kMaxShadowCascades], Texture2DArray tex, _samplerType samp, real3 positionWS, real3 normalWS, int index, real3 L )      \
	{																																													                            \
		/* load the right shadow data for the current face */																															                            \
		real4 dirShadowSplitSpheres[kMaxShadowCascades];																																                            \
		uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );																				                            \
		real relDistance;                                                                                                                                                                                           \
		int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDistance );															                            \
		if( shadowSplitIndex < 0 )                                                                                                                                                                                  \
			return 1.0;                                                                                                                                                                                             \
																																																					\
		real4 scales = asfloat( shadowContext.payloads[payloadOffset] );                                                                                                                                            \
		payloadOffset++;                                                                                                                                                                                            \
		real4 borders = asfloat( shadowContext.payloads[payloadOffset] );                                                                                                                                           \
		payloadOffset++;                                                                                                                                                                                            \
		real border = borders[shadowSplitIndex];                                                                                                                                                                    \
		real alpha  = border <= 0.0 ? 0.0 : saturate( (relDistance - (1.0 - border)) / border );                                                                                                                    \
																																																					\
		ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];																										                            \
																																																					\
		/* sample the texture */																																						                            \
		float slice;																																									                            \
		UnpackShadowmapId( sd.id, slice );																																				                            \
																																																					\
		/* normal based bias */																																							                            \
		real3 orig_pos = positionWS;                                                                                                                                                                                \
		uint orig_payloadOffset = payloadOffset;		                                                                                                                                                            \
		real recvBiasWeight = EvalShadow_ReceiverBiasWeight( sd, tex, samp, positionWS, normalWS, L, 1.0, slice, false );                                                                                           \
		positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, 1.0, recvBiasWeight, false );                                                                                                            \
		/* Be careful of this code, we need it here before the if statement otherwise the compiler screws up optimizing dirShadowSplitSpheres VGPRs away */                                                         \
		real3 splitSphere = dirShadowSplitSpheres[shadowSplitIndex].xyz;                                                                                                                                            \
		real3 cascadeDir  = normalize( -splitSphere + dirShadowSplitSpheres[min( shadowSplitIndex+1, kMaxShadowCascades-1 )].xyz );                                                                                 \
		real3 wposDir     = normalize( -splitSphere + positionWS );                                                                                                                                                 \
		real  cascDot     = dot( cascadeDir, wposDir );                                                                                                                                                             \
			  alpha       = cascDot > 0.0 ? alpha : lerp( alpha, 0.0, saturate( -cascDot * 4.0 ) );                                                                                                                 \
																																																					\
		/* get shadowmap texcoords */																																					                            \
		real3 posNDC;                                                                                                                                                                                               \
		real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, true, false );																											                    \
		/* evalute the first cascade */																																					                            \
		real2 sampleBias = EvalShadow_SampleBias_Ortho( sd, normalWS );                                                                                                                                             \
		real  shadow     = SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, slice, shadowAlgorithms[shadowSplitIndex], tex, samp );                                               \
		real  shadow1    = 1.0;                                                                                                                                                                                     \
																																																					\
		shadowSplitIndex++;                                                                                                                                                                                         \
		if( shadowSplitIndex < kMaxShadowCascades )                                                                                                                                                                 \
		{                                                                                                                                                                                                           \
			shadow1 = shadow;                                                                                                                                                                                       \
																																																					\
			if( alpha > 0.0 )                                                                                                                                                                                       \
			{                                                                                                                                                                                                       \
				sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];																										                                \
				positionWS = EvalShadow_ReceiverBias( sd, orig_pos, normalWS, L, 1.0, recvBiasWeight, false );				                                                                                        \
				posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, false, false );																										                    \
				/* sample the texture */																																				                            \
				UnpackShadowmapId( sd.id, slice );																																		                            \
				sampleBias = EvalShadow_SampleBias_Ortho( sd, normalWS );																																			\
																																																					\
				[branch]                                                                                                                                                                                            \
				if( all( abs( posNDC.xy ) <= (1.0 - sd.texelSizeRcp.zw * 0.5) ) )                                                                                                                                   \
					shadow1 = SampleShadow_SelectAlgorithm( shadowContext, sd, orig_payloadOffset, posTC, sampleBias, slice, shadowAlgorithms[shadowSplitIndex], tex, samp );                                       \
			}                                                                                                                                                                                                       \
		}                                                                                                                                                                                                           \
		shadow = lerp( shadow, shadow1, alpha );                                                                                                                                                                    \
		return shadow;                                                                                                                                                                                              \
	}                                                                                                                                                                                                               \
																																																					\
	real EvalShadow_CascadedDepth_Blend( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, real3 positionWS, real3 normalWS, int index, real3 L )                           \
	{                                                                                                                                                                                                               \
		uint shadowAlgorithms[kMaxShadowCascades] = { SHADOW_REPEAT_CASCADE( shadowAlgorithm ) };                                                                                                                   \
		return EvalShadow_CascadedDepth_Blend( shadowContext, shadowAlgorithms, tex, samp, positionWS, normalWS, index, L );                                                                                        \
	}

	EvalShadow_CascadedDepth_( SamplerComparisonState )
	EvalShadow_CascadedDepth_( SamplerState )
#undef EvalShadow_CascadedDepth_


real EvalShadow_hash12( real2 pos )
{
	real3 p3  = frac( pos.xyx * real3( 443.8975, 397.2973, 491.1871 ) );
		   p3 += dot( p3, p3.yzx + 19.19 );
	return frac( (p3.x + p3.y) * p3.z );
}

real EvalShadow_CascadedDepth_Dither( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int index, real3 L )
{
	// load the right shadow data for the current face
	real4 dirShadowSplitSpheres[kMaxShadowCascades];
	uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );
	real relDistance;
	int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDistance );
	if( shadowSplitIndex < 0 )
		return 1.0;

	real4 scales = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;
	real4 borders = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;
	real border = borders[shadowSplitIndex];
	real alpha  = border <= 0.0 ? 0.0 : saturate( (relDistance - (1.0 - border)) / border );

	ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];

	// get texture description
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	uint shadowType, shadowAlgorithm;
	UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );
	
	// normal based bias
	real3 orig_pos = positionWS;
	real  recvBiasWeight = EvalShadow_ReceiverBiasWeight( shadowContext, shadowAlgorithm, sd, texIdx, sampIdx, positionWS, normalWS, L, 1.0, slice, false );
	positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, 1.0, recvBiasWeight, false );
	// get shadowmap texcoords
	real3 posNDC;
	real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, true, false );

	int    nextSplit   = min( shadowSplitIndex+1, kMaxShadowCascades-1 );
	real3 splitSphere = dirShadowSplitSpheres[shadowSplitIndex].xyz;
	real3 cascadeDir  = normalize( -splitSphere + dirShadowSplitSpheres[min( 3, shadowSplitIndex + 1 )].xyz );
	real3 wposDir     = normalize( -splitSphere + positionWS );
	real  cascDot     = dot( cascadeDir, wposDir );
		   alpha       = cascDot > 0.0 ? alpha : lerp( alpha, 0.0, saturate( -cascDot * 4.0 ) );

	if( shadowSplitIndex < nextSplit && step( EvalShadow_hash12( posTC.xy ), alpha ) )
	{
		sd         = shadowContext.shadowDatas[index + 1 + nextSplit];
		positionWS = EvalShadow_ReceiverBias( sd, orig_pos, normalWS, L, 1.0, recvBiasWeight, false );
		posTC      = EvalShadow_GetTexcoords( sd, positionWS, false );
	}
	// sample the texture
	real2 sampleBias = EvalShadow_SampleBias_Ortho( sd, normalWS );
	real  shadow     = SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, slice, shadowAlgorithm, texIdx, sampIdx );
	return shadowSplitIndex < (kMaxShadowCascades-1) ? shadow : lerp( shadow, 1.0, alpha );
}

#define EvalShadow_CascadedDepth_( _samplerType ) 																																		                            \
	real EvalShadow_CascadedDepth_Dither( ShadowContext shadowContext, uint shadowAlgorithms[kMaxShadowCascades], Texture2DArray tex, _samplerType samp, real3 positionWS, real3 normalWS, int index, real3 L )     \
	{																																													                            \
		/* load the right shadow data for the current face */																															                            \
		real4 dirShadowSplitSpheres[kMaxShadowCascades];																																                            \
		uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );																				                            \
		real relDistance;                                                                                                                                                                                           \
		int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDistance );															                            \
		if( shadowSplitIndex < 0 )                                                                                                                                                                                  \
			return 1.0;                                                                                                                                                                                             \
																																																					\
		real4 scales = asfloat( shadowContext.payloads[payloadOffset] );                                                                                                                                            \
		payloadOffset++;                                                                                                                                                                                            \
		real4 borders = asfloat( shadowContext.payloads[payloadOffset] );                                                                                                                                           \
		payloadOffset++;                                                                                                                                                                                            \
		real border = borders[shadowSplitIndex];                                                                                                                                                                    \
		real alpha  = border <= 0.0 ? 0.0 : saturate( (relDistance - (1.0 - border)) / border );                                                                                                                    \
																																																					\
		ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];																										                            \
																																																					\
		/* get texture description */                                                                                                                                                                               \
		float slice;																																									                            \
		UnpackShadowmapId( sd.id, slice );																																				                            \
																																																					\
		/* normal based bias */																																							                            \
		real3 orig_pos = positionWS;                                                                                                                                                                                \
		real  recvBiasWeight = EvalShadow_ReceiverBiasWeight( sd, tex, samp, positionWS, normalWS, L, 1.0, slice, false );                                                                                          \
		positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, 1.0, recvBiasWeight, false );                                                                                                            \
		/* get shadowmap texcoords */																																					                            \
		real3 posNDC;                                                                                                                                                                                               \
		real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, true, false );																											                    \
																																																					\
		int    nextSplit   = min( shadowSplitIndex+1, kMaxShadowCascades-1 );                                                                                                                                       \
		real3 splitSphere = dirShadowSplitSpheres[shadowSplitIndex].xyz;                                                                                                                                            \
		real3 cascadeDir  = normalize( -splitSphere + dirShadowSplitSpheres[nextSplit].xyz );                                                                                                                       \
		real3 wposDir     = normalize( -splitSphere + positionWS );                                                                                                                                                 \
		real  cascDot     = dot( cascadeDir, wposDir );                                                                                                                                                             \
			   alpha       = cascDot > 0.0 ? alpha : lerp( alpha, 0.0, saturate( -cascDot * 4.0 ) );                                                                                                                \
																																																					\
		if( shadowSplitIndex != nextSplit && step( EvalShadow_hash12( posTC.xy ), alpha ) )                                                                                                                         \
		{                                                                                                                                                                                                           \
			sd         = shadowContext.shadowDatas[index + 1 + nextSplit];                                                                                                                                          \
			positionWS = EvalShadow_ReceiverBias( sd, orig_pos, normalWS, L, 1.0, recvBiasWeight, false );                                                                                                          \
			posTC      = EvalShadow_GetTexcoords( sd, positionWS, false );                                                                                                                                          \
		}                                                                                                                                                                                                           \
		/* sample the texture */																																						                            \
		real2 sampleBias = EvalShadow_SampleBias_Ortho( sd, normalWS );                                                                                                                                             \
		real  shadow     = SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, slice, shadowAlgorithms[shadowSplitIndex], tex, samp );                                               \
		return shadowSplitIndex < (kMaxShadowCascades-1) ? shadow : lerp( shadow, 1.0, alpha );                                                                                                                     \
	}                                                                                                                                                                                                               \
																																																					\
	real EvalShadow_CascadedDepth_Dither( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, real3 positionWS, real3 normalWS, int index, real3 L )                          \
	{                                                                                                                                                                                                               \
		uint shadowAlgorithms[kMaxShadowCascades] = { SHADOW_REPEAT_CASCADE( shadowAlgorithm ) };                                                                                                                   \
		return EvalShadow_CascadedDepth_Dither( shadowContext, shadowAlgorithms, tex, samp, positionWS, normalWS, index, L );                                                                                       \
	}


	EvalShadow_CascadedDepth_( SamplerComparisonState )
	EvalShadow_CascadedDepth_( SamplerState )
#undef EvalShadow_CascadedDepth_


//------------------------------------------------------------------------------------------------------------------------------------

real3 EvalShadow_GetClosestSample_Point( ShadowContext shadowContext, real3 positionWS, int index, real3 L )
{
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];
	// load the right shadow data for the current face
	int faceIndex = EvalShadow_GetCubeFaceID( L ) + 1;
	sd = shadowContext.shadowDatas[index + faceIndex];

	real4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy, true );

	// load the texel
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	closestNDC.z = LoadShadow_T2DA( shadowContext, texIdx, texelIdx, slice );

	// reconstruct depth position
	real4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}


real3 EvalShadow_GetClosestSample_Point( ShadowContext shadowContext, Texture2DArray tex, real3 positionWS, int index, real3 L )
{
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];
	// load the right shadow data for the current face
	int faceIndex = EvalShadow_GetCubeFaceID( L ) + 1;
	sd = shadowContext.shadowDatas[index + faceIndex];

	real4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy, true );

	// load the texel
	float slice;
	UnpackShadowmapId(sd.id, slice);
	closestNDC.z = LOAD_TEXTURE2D_ARRAY_LOD( tex, texelIdx, slice, 0 ).x;

	// reconstruct depth position
	real4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}

real3 EvalShadow_GetClosestSample_Spot( ShadowContext shadowContext, real3 positionWS, int index )
{
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];

	real4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy, true );

	// load the texel
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	closestNDC.z = LoadShadow_T2DA( shadowContext, texIdx, texelIdx, slice );

	// reconstruct depth position
	real4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}


real3 EvalShadow_GetClosestSample_Spot( ShadowContext shadowContext, Texture2DArray tex, real3 positionWS, int index )
{
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];

	real4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy, true );

	// load the texel
	float slice;
	UnpackShadowmapId(sd.id, slice);
	closestNDC.z = LOAD_TEXTURE2D_ARRAY_LOD( tex, texelIdx, slice, 0 ).x;

	// reconstruct depth position
	real4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}

real3 EvalShadow_GetClosestSample_Punctual( ShadowContext shadowContext, real3 positionWS, int index, real3 L )
{
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];
	uint shadowType;
	UnpackShadowType( sd.shadowType, shadowType );
	// load the right shadow data for the current face
	int faceIndex = shadowType == GPUSHADOWTYPE_POINT ? (EvalShadow_GetCubeFaceID( L ) + 1) : 0;
	sd = shadowContext.shadowDatas[index + faceIndex];

	real4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy, true );

	// load the texel
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	closestNDC.z = LoadShadow_T2DA( shadowContext, texIdx, texelIdx, slice );

	// reconstruct depth position
	real4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}

real3 EvalShadow_GetClosestSample_Punctual( ShadowContext shadowContext, Texture2DArray tex, real3 positionWS, int index, real3 L )
{
	// get the algorithm
	ShadowData sd = shadowContext.shadowDatas[index];
	uint shadowType;
	UnpackShadowType( sd.shadowType, shadowType );
	// load the right shadow data for the current face
	int faceIndex = shadowType == GPUSHADOWTYPE_POINT ? (EvalShadow_GetCubeFaceID( L ) + 1) : 0;
	sd = shadowContext.shadowDatas[index + faceIndex];

	real4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy, true );

	// load the texel
	float slice;
	UnpackShadowmapId(sd.id, slice);
	closestNDC.z = LOAD_TEXTURE2D_ARRAY_LOD( tex, texelIdx, slice, 0 ).x;

	// reconstruct depth position
	real4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}

real3 EvalShadow_GetClosestSample_Cascade( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int index, real4 L )
{
	// load the right shadow data for the current face
	real4 dirShadowSplitSpheres[4];
	uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );
	real relDistance;
	int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDistance );
	if( shadowSplitIndex < 0 )
		return 1.0;

	real4 scales = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;
	real4 borders = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;

	ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];

	real4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy, false );

	// load the texel
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	closestNDC.z = LoadShadow_T2DA( shadowContext, texIdx, texelIdx, slice );

	// reconstruct depth position
	real4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}

real3 EvalShadow_GetClosestSample_Cascade( ShadowContext shadowContext, Texture2DArray tex, real3 positionWS, real3 normalWS, int index, real4 L )
{
	// load the right shadow data for the current face
	real4 dirShadowSplitSpheres[4];
	uint payloadOffset = EvalShadow_LoadSplitSpheres( shadowContext, index, dirShadowSplitSpheres );
	real relDistance;
	int shadowSplitIndex = EvalShadow_GetSplitSphereIndexForDirshadows( positionWS, dirShadowSplitSpheres, relDistance );
	if( shadowSplitIndex < 0 )
		return 1.0;

	real4 scales = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;
	real4 borders = asfloat( shadowContext.payloads[payloadOffset] );
	payloadOffset++;

	ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];

	real4 closestNDC = { 0,0,0,1 };
	uint2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy, false );

	// load the texel
	uint texIdx, sampIdx;
	float slice;
	UnpackShadowmapId( sd.id, texIdx, sampIdx, slice );
	closestNDC.z = LOAD_TEXTURE2D_ARRAY_LOD( tex, texelIdx, slice, 0 ).x;

	// reconstruct depth position
	real4 closestWS = mul( closestNDC, sd.shadowToWorld );
	return closestWS.xyz / closestWS.w;
}
