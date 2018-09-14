// Various shadow sampling logic.
// Again two versions, one for dynamic resource indexing, one for static resource access.

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

// ------------------------------------------------------------------
//  PCF Filtering methods
// ------------------------------------------------------------------

//
//                  1 tap PCF sampling
//
real SampleShadow_PCF_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 coord, float slice, uint texIdx, uint sampIdx )
{
    real depthBias = asfloat( shadowContext.payloads[payloadOffset].x );
    payloadOffset++;

#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif
    // sample the texture
    return SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, coord, slice ).x;
}

real SampleShadow_PCF_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 coord, float slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
    real depthBias = asfloat( shadowContext.payloads[payloadOffset].x );
    payloadOffset++;

#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif
    // sample the texture
    return SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, coord, slice );
}

//
//                  3x3 tent PCF sampling (4 taps)
//
real SampleShadow_PCF_Tent_3x3( ShadowContext shadowContext, inout uint payloadOffset, real4 textureSize, real4 texelSizeRcp, real3 coord, real2 sampleBias, float slice, uint texIdx, uint sampIdx )
{
    real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
    real  depthBias  = params.x;
    payloadOffset++;

    real4 shadowMapTexture_TexelSize = real4( texelSizeRcp.xy, textureSize.xy );

#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif

    real shadow = 0.0;
    real fetchesWeights[4];
    real2 fetchesUV[4];

    SampleShadow_ComputeSamples_Tent_3x3(shadowMapTexture_TexelSize, coord.xy, fetchesWeights, fetchesUV);
    UNITY_LOOP
    for( int i = 0; i < 4; i++ )
    {
        shadow += fetchesWeights[i] * SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( fetchesUV[i].xy, coord.z + dot( fetchesUV[i].xy - coord.xy, sampleBias ) ), slice ).x;
    }
    return shadow;
}

real SampleShadow_PCF_Tent_3x3(ShadowContext shadowContext, inout uint payloadOffset, real4 textureSize, real4 texelSizeRcp, real3 coord, real2 sampleBias, float slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
    real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
    real  depthBias  = params.x;
    payloadOffset++;

    real4 shadowMapTexture_TexelSize = real4( texelSizeRcp.xy, textureSize.xy );

#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif

    real shadow = 0.0;
    real fetchesWeights[4];
    real2 fetchesUV[4];

    SampleShadow_ComputeSamples_Tent_3x3(shadowMapTexture_TexelSize, coord.xy, fetchesWeights, fetchesUV);
    for (int i = 0; i < 4; i++)
    {
        shadow += fetchesWeights[i] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[i].xy, coord.z + dot( fetchesUV[i].xy - coord.xy, sampleBias ) ), slice ).x;
    }
    return shadow;
}

//
//                  5x5 tent PCF sampling (9 taps)
//
real SampleShadow_PCF_Tent_5x5( ShadowContext shadowContext, inout uint payloadOffset, real4 textureSize, real4 texelSizeRcp, real3 coord, real2 sampleBias, float slice, uint texIdx, uint sampIdx )
{
    real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
    real  depthBias  = params.x;
    payloadOffset++;

    real4 shadowMapTexture_TexelSize = real4( texelSizeRcp.xy, textureSize.xy );

#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif

    real shadow = 0.0;
    real fetchesWeights[9];
    real2 fetchesUV[9];

    SampleShadow_ComputeSamples_Tent_5x5( shadowMapTexture_TexelSize, coord.xy, fetchesWeights, fetchesUV );
    UNITY_LOOP
    for( int i = 0; i < 9; i++ )
    {
        shadow += fetchesWeights[i] * SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( fetchesUV[i].xy, coord.z + dot( fetchesUV[i].xy - coord.xy, sampleBias ) ), slice ).x;
    }
    return shadow;
}

real SampleShadow_PCF_Tent_5x5(ShadowContext shadowContext, inout uint payloadOffset, real4 textureSize, real4 texelSizeRcp, real3 coord, real2 sampleBias, float slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
    real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
    real  depthBias  = params.x;
    payloadOffset++;

    real4 shadowMapTexture_TexelSize = real4( texelSizeRcp.xy, textureSize.xy );

#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif

    real shadow = 0.0;
    real fetchesWeights[9];
    real2 fetchesUV[9];

    SampleShadow_ComputeSamples_Tent_5x5( shadowMapTexture_TexelSize, coord.xy, fetchesWeights, fetchesUV );


#if SHADOW_OPTIMIZE_REGISTER_USAGE == 1 && SHADOW_USE_SAMPLE_BIASING == 0
    // the loops are only there to prevent the compiler form coalescing all 9 texture fetches which increases register usage
    int i;
    UNITY_LOOP
    for( i = 0; i < 1; i++ )
    {
        shadow += fetchesWeights[ 0] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 0].xy, coord.z + dot( fetchesUV[ 0].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[ 1] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 1].xy, coord.z + dot( fetchesUV[ 1].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[ 2] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 2].xy, coord.z + dot( fetchesUV[ 2].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[ 3] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 3].xy, coord.z + dot( fetchesUV[ 3].xy - coord.xy, sampleBias ) ), slice ).x;
    }

    UNITY_LOOP
    for( i = 0; i < 1; i++ )
    {
        shadow += fetchesWeights[ 4] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 4].xy, coord.z + dot( fetchesUV[ 4].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[ 5] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 5].xy, coord.z + dot( fetchesUV[ 5].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[ 6] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 6].xy, coord.z + dot( fetchesUV[ 6].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[ 7] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 7].xy, coord.z + dot( fetchesUV[ 7].xy - coord.xy, sampleBias ) ), slice ).x;
    }

    shadow += fetchesWeights[ 8] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 8].xy, coord.z + dot( fetchesUV[ 8].xy - coord.xy, sampleBias ) ), slice ).x;
#else
    for( int i = 0; i < 9; i++ )
    {
        shadow += fetchesWeights[i] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[i].xy, coord.z + dot( fetchesUV[i].xy - coord.xy, sampleBias ) ), slice ).x;
    }
#endif
    return shadow;
}

//
//                  7x7 tent PCF sampling (16 taps)
//
real SampleShadow_PCF_Tent_7x7( ShadowContext shadowContext, inout uint payloadOffset, real4 textureSize, real4 texelSizeRcp, real3 coord, real2 sampleBias, float slice, uint texIdx, uint sampIdx )
{
    real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
    real  depthBias  = params.x;
    payloadOffset++;

    real4 shadowMapTexture_TexelSize = real4( texelSizeRcp.xy, textureSize.xy );

#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif

    real shadow = 0.0;
    real fetchesWeights[16];
    real2 fetchesUV[16];

    SampleShadow_ComputeSamples_Tent_7x7( shadowMapTexture_TexelSize, coord.xy, fetchesWeights, fetchesUV );
    UNITY_LOOP
    for( int i = 0; i < 16; i++ )
    {
        shadow += fetchesWeights[i] * SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( fetchesUV[i].xy, coord.z + dot( fetchesUV[i].xy - coord.xy, sampleBias ) ), slice ).x;
    }

    return shadow;
}

real SampleShadow_PCF_Tent_7x7(ShadowContext shadowContext, inout uint payloadOffset, real4 textureSize, real4 texelSizeRcp, real3 coord, real2 sampleBias, float slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
    real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
    real  depthBias  = params.x;
    payloadOffset++;

    real4 shadowMapTexture_TexelSize = real4( texelSizeRcp.xy, textureSize.xy );

#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    coord.z += depthBias;
#endif

    real shadow = 0.0;
    real fetchesWeights[16];
    real2 fetchesUV[16];

    SampleShadow_ComputeSamples_Tent_7x7( shadowMapTexture_TexelSize, coord.xy, fetchesWeights, fetchesUV );

#if SHADOW_OPTIMIZE_REGISTER_USAGE == 1
    // the loops are only there to prevent the compiler form coalescing all 16 texture fetches which increases register usage
    int i;
    UNITY_LOOP
    for( i = 0; i < 1; i++ )
    {
        shadow += fetchesWeights[ 0] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 0].xy, coord.z + dot( fetchesUV[ 0].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[ 1] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 1].xy, coord.z + dot( fetchesUV[ 1].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[ 2] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 2].xy, coord.z + dot( fetchesUV[ 2].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[ 3] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 3].xy, coord.z + dot( fetchesUV[ 3].xy - coord.xy, sampleBias ) ), slice ).x;
    }
    UNITY_LOOP
    for( i = 0; i < 1; i++ )
    {
        shadow += fetchesWeights[ 4] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 4].xy, coord.z + dot( fetchesUV[ 4].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[ 5] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 5].xy, coord.z + dot( fetchesUV[ 5].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[ 6] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 6].xy, coord.z + dot( fetchesUV[ 6].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[ 7] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 7].xy, coord.z + dot( fetchesUV[ 7].xy - coord.xy, sampleBias ) ), slice ).x;
    }
    UNITY_LOOP
    for( i = 0; i < 1; i++ )
    {
        shadow += fetchesWeights[ 8] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 8].xy, coord.z + dot( fetchesUV[ 8].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[ 9] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[ 9].xy, coord.z + dot( fetchesUV[ 9].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[10] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[10].xy, coord.z + dot( fetchesUV[10].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[11] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[11].xy, coord.z + dot( fetchesUV[11].xy - coord.xy, sampleBias ) ), slice ).x;
    }
    UNITY_LOOP
    for( i = 0; i < 1; i++ )
    {
        shadow += fetchesWeights[12] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[12].xy, coord.z + dot( fetchesUV[12].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[13] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[13].xy, coord.z + dot( fetchesUV[13].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[14] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[14].xy, coord.z + dot( fetchesUV[14].xy - coord.xy, sampleBias ) ), slice ).x;
        shadow += fetchesWeights[15] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[15].xy, coord.z + dot( fetchesUV[15].xy - coord.xy, sampleBias ) ), slice ).x;
    }
#else
    for( int i = 0; i < 16; i++ )
    {
        shadow += fetchesWeights[i] * SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( fetchesUV[i].xy, coord.z + dot( fetchesUV[i].xy - coord.xy, sampleBias ) ), slice ).x;
    }
#endif
    return shadow;
}

//
//                  9 tap adaptive PCF sampling
//
real SampleShadow_PCF_9tap_Adaptive( ShadowContext shadowContext, inout uint payloadOffset, real4 texelSizeRcp, real3 tcs, real2 sampleBias, float slice, uint texIdx, uint sampIdx )
{
    real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
    real  depthBias  = params.x;
    real  filterSize = params.y;
    payloadOffset++;

    texelSizeRcp *= filterSize;

#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    tcs.z += depthBias;
#endif

    // Terms0 are weights for the individual samples, the other terms are offsets in texel space
    real4 vShadow3x3PCFTerms0 = real4( 20.0 / 267.0, 33.0 / 267.0, 55.0 / 267.0, 0.0 );
    real4 vShadow3x3PCFTerms1 = real4( texelSizeRcp.x,  texelSizeRcp.y, -texelSizeRcp.x, -texelSizeRcp.y );
    real4 vShadow3x3PCFTerms2 = real4( texelSizeRcp.x,  texelSizeRcp.y, 0.0, 0.0 );
    real4 vShadow3x3PCFTerms3 = real4(-texelSizeRcp.x, -texelSizeRcp.y, 0.0, 0.0 );

    real4 v20Taps;
    v20Taps.x = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms1.xy, tcs.z + dot( vShadow3x3PCFTerms1.xy, sampleBias ) ), slice ).x; //  1  1
    v20Taps.y = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms1.zy, tcs.z + dot( vShadow3x3PCFTerms1.zy, sampleBias ) ), slice ).x; // -1  1
    v20Taps.z = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms1.xw, tcs.z + dot( vShadow3x3PCFTerms1.xw, sampleBias ) ), slice ).x; //  1 -1
    v20Taps.w = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms1.zw, tcs.z + dot( vShadow3x3PCFTerms1.zw, sampleBias ) ), slice ).x; // -1 -1
    real flSum = dot( v20Taps.xyzw, real4( 0.25, 0.25, 0.25, 0.25 ) );
    // fully in light or shadow? -> bail
    if( ( flSum == 0.0 ) || ( flSum == 1.0 ) )
        return flSum;

    // we're in a transition area, do 5 more taps
    flSum *= vShadow3x3PCFTerms0.x * 4.0;

    real4 v33Taps;
    v33Taps.x = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms2.xz, tcs.z + dot( vShadow3x3PCFTerms2.xz, sampleBias ) ), slice ).x; //  1  0
    v33Taps.y = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms3.xz, tcs.z + dot( vShadow3x3PCFTerms3.xz, sampleBias ) ), slice ).x; // -1  0
    v33Taps.z = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms3.zy, tcs.z + dot( vShadow3x3PCFTerms3.zy, sampleBias ) ), slice ).x; //  0 -1
    v33Taps.w = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, real3( tcs.xy + vShadow3x3PCFTerms2.zy, tcs.z + dot( vShadow3x3PCFTerms2.zy, sampleBias ) ), slice ).x; //  0  1
    flSum += dot( v33Taps.xyzw, vShadow3x3PCFTerms0.yyyy );

    flSum += SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, tcs, slice ).x * vShadow3x3PCFTerms0.z;

    return flSum;
}

real SampleShadow_PCF_9tap_Adaptive(ShadowContext shadowContext, inout uint payloadOffset, real4 texelSizeRcp, real3 tcs, real2 sampleBias, float slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
    real2 params     = asfloat( shadowContext.payloads[payloadOffset].xy );
    real  depthBias  = params.x;
    real  filterSize = params.y;
    payloadOffset++;

    texelSizeRcp *= filterSize;

#if SHADOW_USE_DEPTH_BIAS == 1
    // add the depth bias
    tcs.z += depthBias;
#endif

    // Terms0 are weights for the individual samples, the other terms are offsets in texel space
    real4 vShadow3x3PCFTerms0 = real4(20.0 / 267.0, 33.0 / 267.0, 55.0 / 267.0, 0.0);
    real4 vShadow3x3PCFTerms1 = real4( texelSizeRcp.x,  texelSizeRcp.y, -texelSizeRcp.x, -texelSizeRcp.y);
    real4 vShadow3x3PCFTerms2 = real4( texelSizeRcp.x,  texelSizeRcp.y, 0.0, 0.0);
    real4 vShadow3x3PCFTerms3 = real4(-texelSizeRcp.x, -texelSizeRcp.y, 0.0, 0.0);

    real4 v20Taps;
    v20Taps.x = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms1.xy, tcs.z + dot( vShadow3x3PCFTerms1.xy, sampleBias ) ), slice ).x; //  1  1
    v20Taps.y = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms1.zy, tcs.z + dot( vShadow3x3PCFTerms1.zy, sampleBias ) ), slice ).x; // -1  1
    v20Taps.z = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms1.xw, tcs.z + dot( vShadow3x3PCFTerms1.xw, sampleBias ) ), slice ).x; //  1 -1
    v20Taps.w = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms1.zw, tcs.z + dot( vShadow3x3PCFTerms1.zw, sampleBias ) ), slice ).x; // -1 -1
    real flSum = dot( v20Taps.xyzw, real4( 0.25, 0.25, 0.25, 0.25 ) );
    // fully in light or shadow? -> bail
    if( ( flSum == 0.0 ) || ( flSum == 1.0 ) )
        return flSum;

    // we're in a transition area, do 5 more taps
    flSum *= vShadow3x3PCFTerms0.x * 4.0;

    real4 v33Taps;
    v33Taps.x = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms2.xz, tcs.z + dot( vShadow3x3PCFTerms2.xz, sampleBias ) ), slice ).x; //  1  0
    v33Taps.y = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms3.xz, tcs.z + dot( vShadow3x3PCFTerms3.xz, sampleBias ) ), slice ).x; // -1  0
    v33Taps.z = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms3.zy, tcs.z + dot( vShadow3x3PCFTerms3.zy, sampleBias ) ), slice ).x; //  0 -1
    v33Taps.w = SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, real3( tcs.xy + vShadow3x3PCFTerms2.zy, tcs.z + dot( vShadow3x3PCFTerms2.zy, sampleBias ) ), slice ).x; //  0  1
    flSum += dot( v33Taps.xyzw, vShadow3x3PCFTerms0.yyyy );

    flSum += SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, tcs, slice ).x * vShadow3x3PCFTerms0.z;

    return flSum;
}

#include "ShadowMoments.hlsl"

//
//                  1 tap VSM sampling
//
real SampleShadow_VSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, float slice, uint texIdx, uint sampIdx )
{
#if UNITY_REVERSED_Z
    real  depth      = 1.0 - tcs.z;
#else
    real  depth      = tcs.z;
#endif
    real2 params         = asfloat( shadowContext.payloads[payloadOffset].xy );
    real  lightLeakBias = params.x;
    real  varianceBias  = params.y;
    payloadOffset++;

    real2 moments = SampleShadow_T2DA( shadowContext, texIdx, sampIdx, tcs.xy, slice ).xy;

    return ShadowMoments_ChebyshevsInequality( moments, depth, varianceBias, lightLeakBias );
}

real SampleShadow_VSM_1tap(ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, float slice, Texture2DArray tex, SamplerState samp )
{
#if UNITY_REVERSED_Z
    real  depth      = 1.0 - tcs.z;
#else
    real  depth      = tcs.z;
#endif
    real2 params         = asfloat( shadowContext.payloads[payloadOffset].xy );
    real  lightLeakBias = params.x;
    real  varianceBias  = params.y;
    payloadOffset++;

    real2 moments = SAMPLE_TEXTURE2D_ARRAY_LOD( tex, samp, tcs.xy, slice, 0.0 ).xy;

    return ShadowMoments_ChebyshevsInequality( moments, depth, varianceBias, lightLeakBias );
}

//
//                  1 tap EVSM sampling
//
real SampleShadow_EVSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, float slice, uint texIdx, uint sampIdx, bool fourMoments )
{
#if UNITY_REVERSED_Z
    real  depth      = 1.0 - tcs.z;
#else
    real  depth      = tcs.z;
#endif
    real4 params         = asfloat( shadowContext.payloads[payloadOffset] );
    real  lightLeakBias = params.x;
    real  varianceBias   = params.y;
    real2 evsmExponents = params.zw;
    payloadOffset++;

    real2 warpedDepth = ShadowMoments_WarpDepth( depth, evsmExponents );

    real4 moments = SampleShadow_T2DA( shadowContext, texIdx, sampIdx, tcs.xy, slice );

    // Derivate of warping at depth
    real2 depthScale  = evsmExponents * warpedDepth;
    real2 minVariance = depthScale * depthScale * varianceBias;

    UNITY_BRANCH
    if( fourMoments )
    {
        real posContrib = ShadowMoments_ChebyshevsInequality( moments.xz, warpedDepth.x, minVariance.x, lightLeakBias );
        real negContrib = ShadowMoments_ChebyshevsInequality( moments.yw, warpedDepth.y, minVariance.y, lightLeakBias );
        return min( posContrib, negContrib );
    }
    else
    {
        return ShadowMoments_ChebyshevsInequality( moments.xy, warpedDepth.x, minVariance.x, lightLeakBias );
    }
}

real SampleShadow_EVSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, float slice, Texture2DArray tex, SamplerState samp, bool fourMoments )
{
#if UNITY_REVERSED_Z
    real  depth      = 1.0 - tcs.z;
#else
    real  depth      = tcs.z;
#endif
    real4 params         = asfloat( shadowContext.payloads[payloadOffset] );
    real  lightLeakBias = params.x;
    real  varianceBias  = params.y;
    real2 evsmExponents = params.zw;
    payloadOffset++;

    real2 warpedDepth = ShadowMoments_WarpDepth( depth, evsmExponents );

    real4 moments = SAMPLE_TEXTURE2D_ARRAY_LOD( tex, samp, tcs.xy, slice, 0.0 );

    // Derivate of warping at depth
    real2 depthScale  = evsmExponents * warpedDepth;
    real2 minVariance = depthScale * depthScale * varianceBias;

    UNITY_BRANCH
    if( fourMoments )
    {
        real posContrib = ShadowMoments_ChebyshevsInequality( moments.xz, warpedDepth.x, minVariance.x, lightLeakBias );
        real negContrib = ShadowMoments_ChebyshevsInequality( moments.yw, warpedDepth.y, minVariance.y, lightLeakBias );
        return min( posContrib, negContrib );
    }
    else
    {
        return ShadowMoments_ChebyshevsInequality( moments.xy, warpedDepth.x, minVariance.x, lightLeakBias );
    }
}


//
//                  1 tap MSM sampling
//
real SampleShadow_MSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, float slice, uint texIdx, uint sampIdx, bool useHamburger )
{
    real4 params        = asfloat( shadowContext.payloads[payloadOffset] );
    real  lightLeakBias = params.x;
    real  momentBias    = params.y;
    real  depthBias  = params.z;
    real  bpp16      = params.w;
#if UNITY_REVERSED_Z
    real  depth         = (1.0 - tcs.z) - depthBias;
#else
    real  depth         = tcs.z + depthBias;
#endif
    payloadOffset++;

    real4 moments = SampleShadow_T2DA( shadowContext, texIdx, sampIdx, tcs.xy, slice );
    if( bpp16 != 0.0 )
        moments = ShadowMoments_Decode16MSM( moments );

    real3 z;
    real4 b;
    ShadowMoments_SolveMSM( moments, depth, momentBias, z, b );

    if( useHamburger )
        return ShadowMoments_SolveDelta3MSM( z, b.xy, lightLeakBias );
    else
        return (z[1] < 0.0 || z[2] > 1.0) ? ShadowMoments_SolveDelta4MSM( z, b, lightLeakBias ) : ShadowMoments_SolveDelta3MSM( z, b.xy, lightLeakBias );
}

real SampleShadow_MSM_1tap( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, float slice, Texture2DArray tex, SamplerState samp, bool useHamburger )
{
    real4 params        = asfloat( shadowContext.payloads[payloadOffset] );
    real  lightLeakBias = params.x;
    real  momentBias    = params.y;
    real  depthBias  = params.z;
    real  bpp16      = params.w;
#if UNITY_REVERSED_Z
    real  depth         = (1.0 - tcs.z) - depthBias;
#else
    real  depth         = tcs.z + depthBias;
#endif
    payloadOffset++;

    real4 moments = SAMPLE_TEXTURE2D_ARRAY_LOD( tex, samp, tcs.xy, slice, 0.0 );
    if( bpp16 != 0.0 )
        moments = ShadowMoments_Decode16MSM( moments );

    real3 z;
    real4 b;
    ShadowMoments_SolveMSM( moments, depth, momentBias, z, b );

    if( useHamburger )
        return ShadowMoments_SolveDelta3MSM( z, b.xy, lightLeakBias );
    else
        return (z[1] < 0.0 || z[2] > 1.0) ? ShadowMoments_SolveDelta4MSM( z, b, lightLeakBias ) : ShadowMoments_SolveDelta3MSM( z, b.xy, lightLeakBias );
}

#include "PCSS.hlsl"

real SampleShadow_PCSS( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, real4 scaleOffset, real2 sampleBias, float slice, uint texIdx, uint sampIdx )
{
    real2 params           = asfloat(shadowContext.payloads[payloadOffset].xy);
    real shadowSoftnesss   = params.x;
    int sampleCount        = params.y;
    payloadOffset++;
    
    real2 sampleJitter = real2(sin(GenerateHashedRandomFloat(tcs.x)),
                               cos(GenerateHashedRandomFloat(tcs.y)));

    //1) Blocker Search
    real averageBlockerDepth = 0.0;
    real numBlockers         = 0.0;
    if (!BlockerSearch(averageBlockerDepth, numBlockers, shadowSoftnesss + 0.000001, tcs, sampleJitter, sampleBias, shadowContext, slice, texIdx, sampIdx, sampleCount))
        return 1.0;

    //2) Penumbra Estimation
    real filterSize = shadowSoftnesss * PenumbraSize(tcs.z, averageBlockerDepth);
    filterSize = max(filterSize, 0.000001);

    //3) Filter
    return PCSS(tcs, filterSize, scaleOffset, slice, sampleBias, sampleJitter, shadowContext, texIdx, sampIdx, sampleCount);
}

real SampleShadow_PCSS( ShadowContext shadowContext, inout uint payloadOffset, real3 tcs, real4 scaleOffset, real2 sampleBias, float slice, Texture2DArray tex, SamplerComparisonState compSamp, SamplerState samp )
{
    real2 params           = asfloat(shadowContext.payloads[payloadOffset].xy);
    real shadowSoftnesss   = params.x;
    int sampleCount        = params.y;
    payloadOffset++;

    real2 sampleJitter = real2(sin(GenerateHashedRandomFloat(tcs.x)),
                               cos(GenerateHashedRandomFloat(tcs.y)));

    //1) Blocker Search
    real averageBlockerDepth = 0.0;
    real numBlockers         = 0.0;
    if (!BlockerSearch(averageBlockerDepth, numBlockers, shadowSoftnesss + 0.000001, tcs, slice, sampleJitter, sampleBias, tex, samp, sampleCount)) 
        return 1.0;

    //2) Penumbra Estimation
    real filterSize = shadowSoftnesss * PenumbraSize(tcs.z, averageBlockerDepth);
    filterSize = max(filterSize, 0.000001);

    //3) Filter
    return PCSS(tcs, filterSize, scaleOffset, slice, sampleBias, sampleJitter, tex, compSamp, sampleCount);
}

//-----------------------------------------------------------------------------------------------------
// helper function to dispatch a specific shadow algorithm
real SampleShadow_SelectAlgorithm( ShadowContext shadowContext, ShadowData shadowData, inout uint payloadOffset, real3 posTC, real2 sampleBias, uint algorithm, uint texIdx, uint sampIdx )
{
    UNITY_BRANCH
    switch( algorithm )
    {
    case GPUSHADOWALGORITHM_PCF_1TAP        : return SampleShadow_PCF_1tap( shadowContext, payloadOffset, posTC, shadowData.slice, texIdx, sampIdx );
    case GPUSHADOWALGORITHM_PCF_9TAP        : return SampleShadow_PCF_9tap_Adaptive( shadowContext, payloadOffset, shadowData.texelSizeRcp, posTC, sampleBias, shadowData.slice, texIdx, sampIdx );
    case GPUSHADOWALGORITHM_PCF_TENT_3X3    : return SampleShadow_PCF_Tent_3x3( shadowContext, payloadOffset, shadowData.textureSize, shadowData.texelSizeRcp, posTC, sampleBias, shadowData.slice, texIdx, sampIdx );
    case GPUSHADOWALGORITHM_PCF_TENT_5X5    : return SampleShadow_PCF_Tent_5x5( shadowContext, payloadOffset, shadowData.textureSize, shadowData.texelSizeRcp, posTC, sampleBias, shadowData.slice, texIdx, sampIdx );
    case GPUSHADOWALGORITHM_PCF_TENT_7X7    : return SampleShadow_PCF_Tent_7x7( shadowContext, payloadOffset, shadowData.textureSize, shadowData.texelSizeRcp, posTC, sampleBias, shadowData.slice, texIdx, sampIdx );
    case GPUSHADOWALGORITHM_PCSS            : return SampleShadow_PCSS( shadowContext, payloadOffset, posTC, shadowData.scaleOffset, sampleBias, shadowData.slice, texIdx, sampIdx );
    case GPUSHADOWALGORITHM_VSM             : return SampleShadow_VSM_1tap(  shadowContext, payloadOffset, posTC, shadowData.slice, texIdx, sampIdx );
    case GPUSHADOWALGORITHM_EVSM_2          : return SampleShadow_EVSM_1tap( shadowContext, payloadOffset, posTC, shadowData.slice, texIdx, sampIdx, false );
    case GPUSHADOWALGORITHM_EVSM_4          : return SampleShadow_EVSM_1tap( shadowContext, payloadOffset, posTC, shadowData.slice, texIdx, sampIdx, true );
    case GPUSHADOWALGORITHM_MSM_HAM         : return SampleShadow_MSM_1tap(  shadowContext, payloadOffset, posTC, shadowData.slice, texIdx, sampIdx, true );
    case GPUSHADOWALGORITHM_MSM_HAUS        : return SampleShadow_MSM_1tap(  shadowContext, payloadOffset, posTC, shadowData.slice, texIdx, sampIdx, false );
    default: return 1.0;
    }
}

real SampleShadow_SelectAlgorithm( ShadowContext shadowContext, ShadowData shadowData, inout uint payloadOffset, real3 posTC, real2 sampleBias, uint algorithm, Texture2DArray tex, SamplerComparisonState compSamp )
{
    UNITY_BRANCH
    switch( algorithm )
    {
    case GPUSHADOWALGORITHM_PCF_1TAP        : return SampleShadow_PCF_1tap( shadowContext, payloadOffset, posTC, shadowData.slice, tex, compSamp );
    case GPUSHADOWALGORITHM_PCF_9TAP        : return SampleShadow_PCF_9tap_Adaptive( shadowContext, payloadOffset, shadowData.texelSizeRcp, posTC, sampleBias, shadowData.slice, tex, compSamp );
    case GPUSHADOWALGORITHM_PCF_TENT_3X3    : return SampleShadow_PCF_Tent_3x3( shadowContext, payloadOffset, shadowData.textureSize, shadowData.texelSizeRcp, posTC, sampleBias, shadowData.slice, tex, compSamp );
    case GPUSHADOWALGORITHM_PCF_TENT_5X5    : return SampleShadow_PCF_Tent_5x5( shadowContext, payloadOffset, shadowData.textureSize, shadowData.texelSizeRcp, posTC, sampleBias, shadowData.slice, tex, compSamp );
    case GPUSHADOWALGORITHM_PCF_TENT_7X7    : return SampleShadow_PCF_Tent_7x7( shadowContext, payloadOffset, shadowData.textureSize, shadowData.texelSizeRcp, posTC, sampleBias, shadowData.slice, tex, compSamp );
    case GPUSHADOWALGORITHM_PCSS            : return SampleShadow_PCSS( shadowContext, payloadOffset, posTC, shadowData.scaleOffset, sampleBias, shadowData.slice, tex, compSamp, s_point_clamp_sampler );

    default: return 1.0;
    }
}

real SampleShadow_SelectAlgorithm( ShadowContext shadowContext, ShadowData shadowData, inout uint payloadOffset, real3 posTC, real2 sampleBias, uint algorithm, Texture2DArray tex, SamplerState samp )
{
    UNITY_BRANCH
    switch( algorithm )
    {
    case GPUSHADOWALGORITHM_VSM             : return SampleShadow_VSM_1tap(  shadowContext, payloadOffset, posTC, shadowData.slice, tex, samp );
    case GPUSHADOWALGORITHM_EVSM_2          : return SampleShadow_EVSM_1tap( shadowContext, payloadOffset, posTC, shadowData.slice, tex, samp, false );
    case GPUSHADOWALGORITHM_EVSM_4          : return SampleShadow_EVSM_1tap( shadowContext, payloadOffset, posTC, shadowData.slice, tex, samp, true );
    case GPUSHADOWALGORITHM_MSM_HAM         : return SampleShadow_MSM_1tap(  shadowContext, payloadOffset, posTC, shadowData.slice, tex, samp, true );
    case GPUSHADOWALGORITHM_MSM_HAUS        : return SampleShadow_MSM_1tap(  shadowContext, payloadOffset, posTC, shadowData.slice, tex, samp, false );
    default: return 1.0;
    }
}
