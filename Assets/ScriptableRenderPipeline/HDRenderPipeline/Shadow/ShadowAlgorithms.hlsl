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

//
//  Point shadows
//
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
    return SampleShadow_PCF_1tap( shadowContext, posTC, slice, texIdx, sampIdx );
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
    return SampleShadow_PCF_1tap( shadowContext, posTC, slice, tex, compSamp );
}


//
//  Spot shadows
//
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
    return SampleShadow_PCF_1tap( shadowContext, posTC, slice, texIdx, sampIdx );
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
    return SampleShadow_PCF_1tap( shadowContext, posTC, slice, tex, compSamp );
}

//
//  Punctual shadows for Point and Spot
//
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
    return SampleShadow_PCF_1tap( shadowContext, posTC, slice, texIdx, sampIdx );
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
    return SampleShadow_PCF_1tap( shadowContext, posTC, slice, tex, compSamp );
}

//
//  Directional shadows (cascaded shadow map)
//
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

    splitSpheres[0] = asfloat( shadowContext.payloads[offset + 0] );
    splitSpheres[1] = asfloat( shadowContext.payloads[offset + 1] );
    splitSpheres[2] = asfloat( shadowContext.payloads[offset + 2] );
    splitSpheres[3] = asfloat( shadowContext.payloads[offset + 3] );
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

    return SampleShadow_PCF_9tap_Adaptive( shadowContext, sd.texelSizeRcp, posTC, slice, texIdx, sampIdx );
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

    return SampleShadow_PCF_9tap_Adaptive( shadowContext, sd.texelSizeRcp, posTC, slice, tex, compSamp );
}
