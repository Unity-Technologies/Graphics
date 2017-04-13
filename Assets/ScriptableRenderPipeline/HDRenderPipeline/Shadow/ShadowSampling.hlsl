// Various shadow sampling logic.
// Again two versions, one for dynamic resource indexing, one for static resource access.

//
//                  1 tap PCF sampling
//
float SampleShadow_PCF_1tap( ShadowContext shadowContext, float3 tcs, uint slice, uint texIdx, uint sampIdx )
{
    // sample the texture
    return SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, tcs, slice ).x;
}

float SampleShadow_PCF_1tap( ShadowContext shadowContext, float3 tcs, uint slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
    // sample the texture
    return SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, compSamp, tcs, slice );
}

//
//                  9 tap adaptive PCF sampling
//
float SampleShadow_PCF_9tap_Adaptive( ShadowContext shadowContext, float2 texelSizeRcp, float3 tcs, uint slice, uint texIdx, uint sampIdx )
{
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

float SampleShadow_PCF_9tap_Adaptive(ShadowContext shadowContext, float2 texelSizeRcp, float3 tcs, uint slice, Texture2DArray tex, SamplerComparisonState compSamp )
{
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
