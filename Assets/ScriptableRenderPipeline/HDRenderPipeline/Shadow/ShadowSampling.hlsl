// Various shadow sampling logic.
// Again two versions, one for dynamic resource indexing, one for static resource access.

//
//					1 tap PCF sampling
//
float SampleShadow_PCF_1tap( ShadowContext shadowContext, inout uint payloadOffset, float3 tcs, float bias, uint slice, uint texIdx, uint sampIdx )
{
	float depthBias = asfloat( shadowContext.payloads[payloadOffset].x );
	payloadOffset++;

	// add the depth bias
	tcs.z += depthBias;
	// sample the texture
	return SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, tcs, slice ).x;
}

float SampleShadow_PCF_1tap( ShadowContext shadowContext, inout uint payloadOffset, float3 tcs, float bias, uint slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
	float depthBias = asfloat( shadowContext.payloads[payloadOffset].x );
	payloadOffset++;

	// add the depth bias
	tcs.z += depthBias;
	// sample the texture
	return SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, tcs, slice );
}

//
//					9 tap adaptive PCF sampling
//
float SampleShadow_PCF_9tap_Adaptive( ShadowContext shadowContext, inout uint payloadOffset, float2 texelSizeRcp, float3 tcs, float bias, uint slice, uint texIdx, uint sampIdx )
{
	float2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
	float  depthBias  = params.x;
	float  filterSize = params.y;
	payloadOffset++;

	texelSizeRcp *= filterSize;

	// add the depth bias
	tcs.z += depthBias;

	// Terms0 are weights for the individual samples, the other terms are offsets in texel space
	float4 vShadow3x3PCFTerms0 = float4( 20.0f / 267.0f, 33.0f / 267.0f, 55.0f / 267.0f, 0.0f );
	float4 vShadow3x3PCFTerms1 = float4( texelSizeRcp.x,  texelSizeRcp.y, -texelSizeRcp.x, -texelSizeRcp.y );
	float4 vShadow3x3PCFTerms2 = float4( texelSizeRcp.x,  texelSizeRcp.y, 0.0f, 0.0f );
	float4 vShadow3x3PCFTerms3 = float4(-texelSizeRcp.x, -texelSizeRcp.y, 0.0f, 0.0f );

	float4 v20Taps;
	v20Taps.x = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, float3( tcs.xy + vShadow3x3PCFTerms1.xy, tcs.z ), slice ).x; //  1  1
	v20Taps.y = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, float3( tcs.xy + vShadow3x3PCFTerms1.zy, tcs.z ), slice ).x; // -1  1
	v20Taps.z = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, float3( tcs.xy + vShadow3x3PCFTerms1.xw, tcs.z ), slice ).x; //  1 -1
	v20Taps.w = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, float3( tcs.xy + vShadow3x3PCFTerms1.zw, tcs.z ), slice ).x; // -1 -1
	float flSum = dot( v20Taps.xyzw, float4( 0.25, 0.25, 0.25, 0.25 ) );
	// fully in light or shadow? -> bail
	if( ( flSum == 0.0 ) || ( flSum == 1.0 ) )
		return flSum;

	// we're in a transition area, do 5 more taps
	flSum *= vShadow3x3PCFTerms0.x * 4.0;

	float4 v33Taps;
	v33Taps.x = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, float3( tcs.xy + vShadow3x3PCFTerms2.xz, tcs.z ), slice ).x; //  1  0
	v33Taps.y = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, float3( tcs.xy + vShadow3x3PCFTerms3.xz, tcs.z ), slice ).x; // -1  0
	v33Taps.z = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, float3( tcs.xy + vShadow3x3PCFTerms3.zy, tcs.z ), slice ).x; //  0 -1
	v33Taps.w = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, float3( tcs.xy + vShadow3x3PCFTerms2.zy, tcs.z ), slice ).x; //  0  1
	flSum += dot( v33Taps.xyzw, vShadow3x3PCFTerms0.yyyy );

	flSum += SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, tcs, slice ).x * vShadow3x3PCFTerms0.z;

	return flSum;
}

float SampleShadow_PCF_9tap_Adaptive(ShadowContext shadowContext, inout uint payloadOffset, float2 texelSizeRcp, float3 tcs, float bias, uint slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
	float2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
	float  depthBias  = params.x;
	float  filterSize = params.y;
	payloadOffset++;

	texelSizeRcp *= filterSize;

	// add the depth bias
	tcs.z += depthBias;

	// Terms0 are weights for the individual samples, the other terms are offsets in texel space
	float4 vShadow3x3PCFTerms0 = float4(20.0f / 267.0f, 33.0f / 267.0f, 55.0f / 267.0f, 0.0f);
	float4 vShadow3x3PCFTerms1 = float4( texelSizeRcp.x,  texelSizeRcp.y, -texelSizeRcp.x, -texelSizeRcp.y);
	float4 vShadow3x3PCFTerms2 = float4( texelSizeRcp.x,  texelSizeRcp.y, 0.0f, 0.0f);
	float4 vShadow3x3PCFTerms3 = float4(-texelSizeRcp.x, -texelSizeRcp.y, 0.0f, 0.0f);

	float4 v20Taps;
	v20Taps.x = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( tcs.xy + vShadow3x3PCFTerms1.xy, tcs.z ), slice ).x; //  1  1
	v20Taps.y = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( tcs.xy + vShadow3x3PCFTerms1.zy, tcs.z ), slice ).x; // -1  1
	v20Taps.z = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( tcs.xy + vShadow3x3PCFTerms1.xw, tcs.z ), slice ).x; //  1 -1
	v20Taps.w = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( tcs.xy + vShadow3x3PCFTerms1.zw, tcs.z ), slice ).x; // -1 -1
	float flSum = dot( v20Taps.xyzw, float4( 0.25, 0.25, 0.25, 0.25 ) );
	// fully in light or shadow? -> bail
	if( ( flSum == 0.0 ) || ( flSum == 1.0 ) )
		return flSum;

	// we're in a transition area, do 5 more taps
	flSum *= vShadow3x3PCFTerms0.x * 4.0;

	float4 v33Taps;
	v33Taps.x = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( tcs.xy + vShadow3x3PCFTerms2.xz, tcs.z ), slice ).x; //  1  0
	v33Taps.y = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( tcs.xy + vShadow3x3PCFTerms3.xz, tcs.z ), slice ).x; // -1  0
	v33Taps.z = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( tcs.xy + vShadow3x3PCFTerms3.zy, tcs.z ), slice ).x; //  0 -1
	v33Taps.w = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, float3( tcs.xy + vShadow3x3PCFTerms2.zy, tcs.z ), slice ).x; //  0  1
	flSum += dot( v33Taps.xyzw, vShadow3x3PCFTerms0.yyyy );

	flSum += SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, tcs, slice ).x * vShadow3x3PCFTerms0.z;

	return flSum;
}

#include "ShadowMoments.hlsl"

//
//					1 tap VSM sampling
//
float SampleShadow_VSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, float3 tcs, uint slice, uint texIdx, uint sampIdx )
{
	float  depth		 = tcs.z;
	float2 params		 = asfloat( shadowContext.payloads[payloadOffset].xy );
	float  lightLeakBias = params.x;
	float  varianceBias  = params.y;
	payloadOffset++;

	float2 moments = SampleShadow_T2DA( shadowContext, texIdx, sampIdx, tcs.xy, slice ).xy;

	return ShadowMoments_ChebyshevsInequality( moments, depth, varianceBias, lightLeakBias );
}

float SampleShadow_VSM_1tap(ShadowContext shadowContext, inout uint payloadOffset, float3 tcs, uint slice, Texture2DArray tex, SamplerState samp )
{
	float  depth		 = tcs.z;
	float2 params		 = asfloat( shadowContext.payloads[payloadOffset].xy );
	float  lightLeakBias = params.x;
	float  varianceBias  = params.y;
	payloadOffset++;

	float2 moments = SAMPLE_TEXTURE2D_ARRAY_LOD( tex, samp, tcs.xy, slice, 0.0 ).xy;

	return ShadowMoments_ChebyshevsInequality( moments, depth, varianceBias, lightLeakBias );
}

//
//					1 tap EVSM sampling
//
float SampleShadow_EVSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, float3 tcs, uint slice, uint texIdx, uint sampIdx, bool fourMoments )
{
	float  depth		 = tcs.z;
	float4 params		 = asfloat( shadowContext.payloads[payloadOffset] );
	float  lightLeakBias = params.x;
	float  varianceBias	 = params.y;
	float2 evsmExponents = params.zw;
	payloadOffset++;

	float2 warpedDepth = ShadowMoments_WarpDepth( depth, evsmExponents );

	float4 moments = SampleShadow_T2DA( shadowContext, texIdx, sampIdx, tcs.xy, slice );

	// Derivate of warping at depth
	float2 depthScale  = evsmExponents * warpedDepth;
	float2 minVariance = depthScale * depthScale * varianceBias;

	[branch]
	if( fourMoments )
	{
		float posContrib = ShadowMoments_ChebyshevsInequality( moments.xz, warpedDepth.x, minVariance.x, lightLeakBias );
		float negContrib = ShadowMoments_ChebyshevsInequality( moments.yw, warpedDepth.y, minVariance.y, lightLeakBias );
		return min( posContrib, negContrib );
	}
	else
	{
		return ShadowMoments_ChebyshevsInequality( moments.xy, warpedDepth.x, minVariance.x, lightLeakBias );
	}
}

float SampleShadow_EVSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, float3 tcs, uint slice, Texture2DArray tex, SamplerState samp, bool fourMoments )
{
	float  depth		 = tcs.z;
	float4 params		 = asfloat( shadowContext.payloads[payloadOffset] );
	float  lightLeakBias = params.x;
	float  varianceBias  = params.y;
	float2 evsmExponents = params.zw;
	payloadOffset++;

	float2 warpedDepth = ShadowMoments_WarpDepth( depth, evsmExponents );

	float4 moments = SAMPLE_TEXTURE2D_ARRAY_LOD( tex, samp, tcs.xy, slice, 0.0 );

	// Derivate of warping at depth
	float2 depthScale  = evsmExponents * warpedDepth;
	float2 minVariance = depthScale * depthScale * varianceBias;

	[branch]
	if( fourMoments )
	{
		float posContrib = ShadowMoments_ChebyshevsInequality( moments.xz, warpedDepth.x, minVariance.x, lightLeakBias );
		float negContrib = ShadowMoments_ChebyshevsInequality( moments.yw, warpedDepth.y, minVariance.y, lightLeakBias );
		return min( posContrib, negContrib );
	}
	else
	{
		return ShadowMoments_ChebyshevsInequality( moments.xy, warpedDepth.x, minVariance.x, lightLeakBias );
	}
}


//
//					1 tap MSM sampling
//
float SampleShadow_MSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, float3 tcs, uint slice, uint texIdx, uint sampIdx, bool useHamburger )
{
	float4 params        = asfloat( shadowContext.payloads[payloadOffset] );
	float  lightLeakBias = params.x;
	float  momentBias    = params.y;
	float  depthBias	 = params.z;
	float  bpp16		 = params.w;
	float  depth         = tcs.z + depthBias;
	payloadOffset++;

	float4 moments = SampleShadow_T2DA( shadowContext, texIdx, sampIdx, tcs.xy, slice );
	if( bpp16 != 0.0 )
		moments = ShadowMoments_Decode16MSM( moments );

	float3 z;
	float4 b;
	ShadowMoments_SolveMSM( moments, depth, momentBias, z, b );
	
	if( useHamburger )
		return ShadowMoments_SolveDelta3MSM( z, b.xy, lightLeakBias );
	else
		return (z[1] < 0.0 || z[2] > 1.0) ? ShadowMoments_SolveDelta4MSM( z, b, lightLeakBias ) : ShadowMoments_SolveDelta3MSM( z, b.xy, lightLeakBias );
}

float SampleShadow_MSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, float3 tcs, uint slice, Texture2DArray tex, SamplerState samp, bool useHamburger )
{
	float4 params        = asfloat( shadowContext.payloads[payloadOffset] );
	float  lightLeakBias = params.x;
	float  momentBias    = params.y;
	float  depthBias	 = params.z;
	float  bpp16		 = params.w;
	float  depth = tcs.z + depthBias;
	payloadOffset++;

	float4 moments = SAMPLE_TEXTURE2D_ARRAY_LOD( tex, samp, tcs.xy, slice, 0.0 );
	if( bpp16 != 0.0 )
		moments = ShadowMoments_Decode16MSM( moments );

	float3 z;
	float4 b;
	ShadowMoments_SolveMSM( moments, depth, momentBias, z, b );
	
	if( useHamburger )
		return ShadowMoments_SolveDelta3MSM( z, b.xy, lightLeakBias );
	else
		return (z[1] < 0.0 || z[2] > 1.0) ? ShadowMoments_SolveDelta4MSM( z, b, lightLeakBias ) : ShadowMoments_SolveDelta3MSM( z, b.xy, lightLeakBias );
}

//-----------------------------------------------------------------------------------------------------
// helper function to dispatch a specific shadow algorithm
float SampleShadow_SelectAlgorithm( ShadowContext shadowContext, ShadowData shadowData, inout uint payloadOffset, float3 posTC, float depthBias, uint slice, uint algorithm, uint texIdx, uint sampIdx )
{
	[branch]
	switch( algorithm )
	{
	case GPUSHADOWALGORITHM_PCF_1TAP	: return SampleShadow_PCF_1tap( shadowContext, payloadOffset, posTC, depthBias, slice, texIdx, sampIdx );
	case GPUSHADOWALGORITHM_PCF_9TAP	: return SampleShadow_PCF_9tap_Adaptive( shadowContext, payloadOffset, shadowData.texelSizeRcp, posTC, depthBias, slice, texIdx, sampIdx );
	case GPUSHADOWALGORITHM_VSM			: return SampleShadow_VSM_1tap(  shadowContext, payloadOffset, posTC, slice, texIdx, sampIdx );
	case GPUSHADOWALGORITHM_EVSM_2		: return SampleShadow_EVSM_1tap( shadowContext, payloadOffset, posTC, slice, texIdx, sampIdx, false );
	case GPUSHADOWALGORITHM_EVSM_4		: return SampleShadow_EVSM_1tap( shadowContext, payloadOffset, posTC, slice, texIdx, sampIdx, true );
	case GPUSHADOWALGORITHM_MSM_HAM		: return SampleShadow_MSM_1tap(  shadowContext, payloadOffset, posTC, slice, texIdx, sampIdx, true );
	case GPUSHADOWALGORITHM_MSM_HAUS	: return SampleShadow_MSM_1tap(  shadowContext, payloadOffset, posTC, slice, texIdx, sampIdx, false );
	default: return 1.0;
	}
}

float SampleShadow_SelectAlgorithm( ShadowContext shadowContext, ShadowData shadowData, inout uint payloadOffset, float3 posTC, float depthBias, uint slice, uint algorithm, Texture2DArray tex, SamplerComparisonState compSamp )
{
	[branch]
	switch( algorithm )
	{
	case GPUSHADOWALGORITHM_PCF_1TAP	: return SampleShadow_PCF_1tap( shadowContext, payloadOffset, posTC, depthBias, slice, tex, compSamp );
	case GPUSHADOWALGORITHM_PCF_9TAP	: return SampleShadow_PCF_9tap_Adaptive( shadowContext, payloadOffset, shadowData.texelSizeRcp, posTC, depthBias, slice, tex, compSamp );
	default: return 1.0;
	}
}

float SampleShadow_SelectAlgorithm( ShadowContext shadowContext, ShadowData shadowData, inout uint payloadOffset, float3 posTC, float depthBias, uint slice, uint algorithm, Texture2DArray tex, SamplerState samp )
{
	[branch]
	switch( algorithm )
	{
	case GPUSHADOWALGORITHM_VSM			: return SampleShadow_VSM_1tap(  shadowContext, payloadOffset, posTC, slice, tex, samp );
	case GPUSHADOWALGORITHM_EVSM_2		: return SampleShadow_EVSM_1tap( shadowContext, payloadOffset, posTC, slice, tex, samp, false );
	case GPUSHADOWALGORITHM_EVSM_4		: return SampleShadow_EVSM_1tap( shadowContext, payloadOffset, posTC, slice, tex, samp, true );
	case GPUSHADOWALGORITHM_MSM_HAM		: return SampleShadow_MSM_1tap(  shadowContext, payloadOffset, posTC, slice, tex, samp, true );
	case GPUSHADOWALGORITHM_MSM_HAUS	: return SampleShadow_MSM_1tap(  shadowContext, payloadOffset, posTC, slice, tex, samp, false );
	default: return 1.0;
	}
}
