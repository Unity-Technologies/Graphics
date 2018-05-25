// Various shadow algorithms
// There are two variants provided, one takes the texture and sampler explicitly so they can be statically passed in.
// The variant without resource parameters dynamically accesses the texture when sampling.

float4 EvalShadow_WorldToShadow( ShadowData sd, real3 positionWS, bool perspProj )
{
    if( perspProj )
    {
        positionWS = positionWS - sd.pos;
        float3x3 view = { sd.rot0, sd.rot1, sd.rot2 };
        positionWS = mul( view, positionWS );
    }
    else
    {
        float3x4 view;
        view[0] = float4( sd.rot0, sd.pos.x );
        view[1] = float4( sd.rot1, sd.pos.y );
        view[2] = float4( sd.rot2, sd.pos.z );
        positionWS = mul( view, float4( positionWS, 1.0 ) ).xyz;
    }

    float4x4 proj;
    proj = 0.0;
    proj._m00 = sd.proj[0];
    proj._m11 = sd.proj[1];
    proj._m22 = sd.proj[2];
    proj._m23 = sd.proj[3];
    if( perspProj )
        proj._m32 = -1.0;
    else
        proj._m33 = 1.0;

    return mul( proj, float4( positionWS, 1.0 ) );
}
// function called by spot, point and directional eval routines to calculate shadow coordinates
real3 EvalShadow_GetTexcoords( ShadowData sd, real3 positionWS, out real3 posNDC, bool perspProj )
{
    real4 posCS = EvalShadow_WorldToShadow( sd, positionWS, perspProj );
    posNDC = perspProj ? (posCS.xyz / posCS.w) : posCS.xyz;
    // calc TCs
    real3 posTC = real3( posNDC.xy * 0.5 + 0.5, posNDC.z );
    posTC.xy = posTC.xy * sd.scaleOffset.xy + sd.scaleOffset.zw;

    return posTC;
}

real3 EvalShadow_GetTexcoords( ShadowData sd, real3 positionWS, bool perspProj )
{
    real3 ndc;
    return EvalShadow_GetTexcoords( sd, positionWS, ndc, perspProj );
}

real2 EvalShadow_GetTexcoords( ShadowData sd, real3 positionWS, out real2 closestSampleNDC, bool perspProj )
{
    real4 posCS = EvalShadow_WorldToShadow( sd, positionWS, perspProj );
    real2 posNDC = perspProj ? (posCS.xy / posCS.w) : posCS.xy;
    // calc TCs
    real2 posTC = posNDC * 0.5 + 0.5;
    closestSampleNDC = (floor(posTC * sd.textureSize.zw) + 0.5) * sd.texelSizeRcp.zw * 2.0 - 1.0.xx;
    return posTC * sd.scaleOffset.xy + sd.scaleOffset.zw;
}

uint2 EvalShadow_GetIntTexcoords( ShadowData sd, real3 positionWS, out real2 closestSampleNDC, bool perspProj )
{
    real2 texCoords = EvalShadow_GetTexcoords(sd, positionWS, closestSampleNDC, perspProj);
    return uint2(texCoords * sd.textureSize.xy);
}

//
//  Biasing functions
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

real EvalShadow_ReceiverBiasWeight( ShadowContext shadowContext, uint shadowAlgorithm, ShadowData sd, uint texIdx, uint sampIdx, real3 positionWS, real3 normalWS, real3 L, real L_dist, bool perspProj )
{
    real weight = 1.0;

    UNITY_BRANCH
    if( shadowAlgorithm <= GPUSHADOWALGORITHM_PCF_TENT_7X7 )
    {
        real3 pos = EvalShadow_ReceiverBiasWeightPos( positionWS, normalWS, L, EvalShadow_WorldTexelSize( sd, L_dist, perspProj ), sd.edgeTolerance, EvalShadow_ReceiverBiasWeightUseNormalFlag( sd.normalBias.w ) );
        real3 tcs = EvalShadow_GetTexcoords( sd, pos, perspProj );
        weight = SampleCompShadow_T2DA( shadowContext, texIdx, sampIdx, tcs, sd.slice ).x;
    }

    return lerp( 1.0, weight, EvalShadow_ReceiverBiasWeightFlag( sd.normalBias.w ) );
}

real EvalShadow_ReceiverBiasWeight( ShadowData sd, Texture2DArray tex, SamplerComparisonState samp, real3 positionWS, real3 normalWS, real3 L, real L_dist, bool perspProj )
{
    real3 pos = EvalShadow_ReceiverBiasWeightPos( positionWS, normalWS, L, EvalShadow_WorldTexelSize( sd, L_dist, perspProj ), sd.edgeTolerance, EvalShadow_ReceiverBiasWeightUseNormalFlag( sd.normalBias.w ) );
    return lerp( 1.0, SAMPLE_TEXTURE2D_ARRAY_SHADOW( tex, samp, EvalShadow_GetTexcoords( sd, pos, perspProj ), sd.slice ).x, EvalShadow_ReceiverBiasWeightFlag( sd.normalBias.w ) );
}

real EvalShadow_ReceiverBiasWeight( ShadowData sd, Texture2DArray tex, SamplerState samp, real3 positionWS, real3 normalWS, real3 L, real L_dist, bool perspProj )
{
    // only used by PCF filters
    return 1.0;
}
#else // SHADOW_USE_VIEW_BIAS_SCALING != 0
real EvalShadow_ReceiverBiasWeight( ShadowContext shadowContext, uint shadowAlgorithm, ShadowData sd, uint texIdx, uint sampIdx, real3 positionWS, real3 normalWS, real3 L, real L_dist, bool perspProj ) { return 1.0; }
real EvalShadow_ReceiverBiasWeight( ShadowData sd, Texture2DArray tex, SamplerComparisonState samp, real3 positionWS, real3 normalWS, real3 L, real L_dist, bool perspProj )                              { return 1.0; }
real EvalShadow_ReceiverBiasWeight (ShadowData sd, Texture2DArray tex, SamplerState samp, real3 positionWS, real3 normalWS, real3 L, real L_dist, bool perspProj )                                        { return 1.0; }
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
    float normalBiasMin   = sd.normalBias.x;
    float normalBiasMax   = sd.normalBias.y;
    float normalBiasScale = sd.normalBias.z;

    float  NdotL       = dot( normalWS, L );
    float  sine        = sqrt( saturate( 1.0 - NdotL * NdotL ) );
    float  tangent     = abs( NdotL ) > 0.0 ? (sine / NdotL) : 0.0;
           sine        = clamp( sine    * normalBiasScale, normalBiasMin, normalBiasMax );
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

    float4 p1 = EvalShadow_WorldToShadow( sd, positionWS + e1, true );
    float4 p2 = EvalShadow_WorldToShadow( sd, positionWS + e2, true );

    p1.xyz /= p1.w;
    p2.xyz /= p2.w;

    p1.xyz = float3( p1.xy * 0.5 + 0.5, p1.z );
    p2.xyz = float3( p2.xy * 0.5 + 0.5, p2.z );

    p1.xy = p1.xy * sd.scaleOffset.xy + sd.scaleOffset.zw;
    p2.xy = p2.xy * sd.scaleOffset.xy + sd.scaleOffset.zw;

    float3 nrm     = cross( p1.xyz - tcs, p2.xyz - tcs );
           nrm.xy /= -nrm.z;

    return isfinite( nrm.xy ) ? (EvalShadow_SampleBiasFlag( sd.normalBias.w ) * nrm.xy) : 0.0.xx;
}

float2 EvalShadow_SampleBias_Ortho( ShadowData sd, float3 normalWS )
{
    float3x3 view = float3x3( sd.rot0, sd.rot1, sd.rot2 );
    float3 nrm = mul( view, normalWS );

    nrm.x /= sd.proj[0];
    nrm.y /= sd.proj[1];
    nrm.z /= sd.proj[2];

    nrm.x *= sd.scaleOffset.y;
    nrm.y *= sd.scaleOffset.x;
    nrm.z *= sd.scaleOffset.x * sd.scaleOffset.y;

    nrm.xy /= -nrm.z;

    return isfinite( nrm.xy ) ? (EvalShadow_SampleBiasFlag( sd.normalBias.w ) * nrm.xy) : 0.0.xx;
}
#else // SHADOW_USE_SAMPLE_BIASING != 0
float2 EvalShadow_SampleBias_Persp( ShadowData sd, float3 positionWS, float3 normalWS, float3 tcs ) { return 0.0.xx; }
float2 EvalShadow_SampleBias_Ortho( ShadowData sd, float3 normalWS )                                { return 0.0.xx; }
#endif // SHADOW_USE_SAMPLE_BIASING != 0


//
//  Point shadows
//
real EvalShadow_PointDepth( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int index, real3 L, real L_dist )
{
    ShadowData sd = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1];
    // get the algorithm
    uint shadowType, shadowAlgorithm;
    UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );
    // get the texture
    uint texIdx, sampIdx;
    UnpackShadowmapId( sd.id, texIdx, sampIdx );
    // bias the world position
    float recvBiasWeight = EvalShadow_ReceiverBiasWeight( shadowContext, shadowAlgorithm, sd, texIdx, sampIdx, positionWS, normalWS, L, L_dist, true );
    positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, L_dist, recvBiasWeight, true );
    // get shadowmap texcoords
    real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, true );
    // get the per sample bias
    real2 sampleBias = EvalShadow_SampleBias_Persp( sd, positionWS, normalWS, posTC );
    // sample the texture according to the given algorithm
    uint payloadOffset = GetPayloadOffset( sd );
    return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, shadowAlgorithm, texIdx, sampIdx );
}

#define EvalShadow_PointDepth_( _samplerType )                                                                                                                                                  \
    real EvalShadow_PointDepth( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, real3 positionWS, real3 normalWS, int index, real3 L, real L_dist )   \
    {                                                                                                                                                                                           \
        ShadowData sd = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1];                                                                                                             \
        /* bias the world position */                                                                                                                                                           \
        real recvBiasWeight = EvalShadow_ReceiverBiasWeight( sd, tex, samp, positionWS, normalWS, L, L_dist, true );                                                                            \
        positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, L_dist, recvBiasWeight, true );                                                                                      \
        /* get shadowmap texcoords */                                                                                                                                                           \
        real3  posTC = EvalShadow_GetTexcoords( sd, positionWS, true );                                                                                                                         \
        /* get the per sample bias */                                                                                                                                                           \
        real2  sampleBias = EvalShadow_SampleBias_Persp( sd, positionWS, normalWS, posTC );                                                                                                     \
        /* sample the texture */                                                                                                                                                                \
        uint payloadOffset = GetPayloadOffset( sd );                                                                                                                                            \
        return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, shadowAlgorithm, tex, samp );                                                                 \
    }
    EvalShadow_PointDepth_( SamplerComparisonState )
    EvalShadow_PointDepth_( SamplerState )
#undef EvalShadow_PointDepth_

//
//  Spot shadows
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
    UnpackShadowmapId( sd.id, texIdx, sampIdx );
    // bias the world position
    real recvBiasWeight = EvalShadow_ReceiverBiasWeight( shadowContext, shadowAlgorithm, sd, texIdx, sampIdx, positionWS, normalWS, L, L_dist, true );
    positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, L_dist, recvBiasWeight, true );
    // get shadowmap texcoords
    real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, true );
    // get the per sample bias
    real2 sampleBias = EvalShadow_SampleBias_Persp( sd, positionWS, normalWS, posTC );
    // sample the texture according to the given algorithm
    uint payloadOffset = GetPayloadOffset( sd );
    return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, shadowAlgorithm, texIdx, sampIdx );
}

#define EvalShadow_SpotDepth_( _samplerType )                                                                                                                                                   \
    real EvalShadow_SpotDepth( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, real3 positionWS, real3 normalWS, int index, real3 L, real L_dist )    \
    {                                                                                                                                                                                           \
        /* load the right shadow data for the current face */                                                                                                                                   \
        ShadowData sd = shadowContext.shadowDatas[index];                                                                                                                                       \
        /* bias the world position */                                                                                                                                                           \
        real recvBiasWeight = EvalShadow_ReceiverBiasWeight( sd, tex, samp, positionWS, normalWS, L, L_dist, true );                                                                            \
        positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, L_dist, recvBiasWeight, true );                                                                                      \
        /* get shadowmap texcoords */                                                                                                                                                           \
        real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, true );                                                                                                                          \
        /* get the per sample bias */                                                                                                                                                           \
        real2  sampleBias = EvalShadow_SampleBias_Persp( sd, positionWS, normalWS, posTC );                                                                                                     \
        /* sample the texture */                                                                                                                                                                \
        uint   payloadOffset = GetPayloadOffset( sd );                                                                                                                                          \
        return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, shadowAlgorithm, tex, samp );                                                                 \
    }
    EvalShadow_SpotDepth_( SamplerComparisonState )
    EvalShadow_SpotDepth_( SamplerState )
#undef EvalShadow_SpotDepth_

//
//  Punctual shadows for Point and Spot
//
real EvalShadow_PunctualDepth( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int index, real3 L, real L_dist )
{
    // get the algorithm
    ShadowData sd = shadowContext.shadowDatas[index];
    uint shadowType, shadowAlgorithm;
    UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );

    // load the right shadow data for the current face
    UNITY_BRANCH
    if( shadowType == GPUSHADOWTYPE_POINT )
    {
        sd.rot0           = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1].rot0;
        sd.rot1           = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1].rot1;
        sd.rot2           = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1].rot2;
        sd.shadowToWorld  = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1].shadowToWorld;
        sd.scaleOffset.zw = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1].scaleOffset.zw;
        sd.slice          = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1].slice;
    }

    uint texIdx, sampIdx;
    UnpackShadowmapId( sd.id, texIdx, sampIdx );
    // bias the world position
    float recvBiasWeight = EvalShadow_ReceiverBiasWeight( shadowContext, shadowAlgorithm, sd, texIdx, sampIdx, positionWS, normalWS, L, L_dist, true );
    positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, L_dist, recvBiasWeight, true );
    // get shadowmap texcoords
    real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, true );
    // get the per sample bias
    real2 sampleBias = EvalShadow_SampleBias_Persp( sd, positionWS, normalWS, posTC );
    // sample the texture according to the given algorithm
    uint payloadOffset = GetPayloadOffset( sd );
    return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, shadowAlgorithm, texIdx, sampIdx );
}

#define EvalShadow_PunctualDepth_( _samplerType )                                                                                                                                                   \
    real EvalShadow_PunctualDepth( ShadowContext shadowContext, uint shadowAlgorithm, Texture2DArray tex, _samplerType samp, real3 positionWS, real3 normalWS, int index, real3 L, real L_dist )    \
    {                                                                                                                                                                                               \
        int faceIndex = 0;                                                                                                                                                                          \
        /* get the shadow type */                                                                                                                                                                   \
        ShadowData sd = shadowContext.shadowDatas[index];                                                                                                                                           \
        uint shadowType;                                                                                                                                                                            \
        UnpackShadowType( sd.shadowType, shadowType );                                                                                                                                              \
                                                                                                                                                                                                    \
        /* load the right shadow data for the current face */                                                                                                                                       \
        UNITY_BRANCH                                                                                                                                                                                \
        if( shadowType == GPUSHADOWTYPE_POINT )                                                                                                                                                     \
        {                                                                                                                                                                                           \
            sd.rot0           = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1].rot0;                                                                                                    \
            sd.rot1           = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1].rot1;                                                                                                    \
            sd.rot2           = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1].rot2;                                                                                                    \
            sd.shadowToWorld  = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1].shadowToWorld;                                                                                           \
            sd.scaleOffset.zw = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1].scaleOffset.zw;                                                                                          \
            sd.slice          = shadowContext.shadowDatas[index + CubeMapFaceID( -L ) + 1].slice;                                                                                                   \
        }                                                                                                                                                                                           \
                                                                                                                                                                                                    \
        /* bias the world position */                                                                                                                                                               \
        real recvBiasWeight = EvalShadow_ReceiverBiasWeight( sd, tex, samp, positionWS, normalWS, L, L_dist, true );                                                                                \
        positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, L_dist, recvBiasWeight, true );                                                                                          \
        /* get shadowmap texcoords */                                                                                                                                                               \
        real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, true );                                                                                                                              \
        /* get the per sample bias */                                                                                                                                                               \
        real2  sampleBias = EvalShadow_SampleBias_Persp( sd, positionWS, normalWS, posTC );                                                                                                         \
        /* sample the texture */                                                                                                                                                                    \
        uint   payloadOffset = GetPayloadOffset( sd );                                                                                                                                              \
        return SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, shadowAlgorithm, tex, samp );                                                                     \
    }
    EvalShadow_PunctualDepth_( SamplerComparisonState )
    EvalShadow_PunctualDepth_( SamplerState )
#undef EvalShadow_PunctualDepth_


//
//  Directional shadows (cascaded shadow map)
//

#define kMaxShadowCascades 4
#define SHADOW_REPEAT_CASCADE( _x ) _x, _x, _x, _x

void EvalShadow_LoadCascadeData( ShadowContext shadowContext, uint index, inout ShadowData sd )
{
    sd.shadowToWorld  = shadowContext.shadowDatas[index].shadowToWorld;
    sd.proj           = shadowContext.shadowDatas[index].proj;
    sd.pos            = shadowContext.shadowDatas[index].pos;
    sd.scaleOffset.zw = shadowContext.shadowDatas[index].scaleOffset.zw;
    sd.slice          = shadowContext.shadowDatas[index].slice;
    sd.viewBias.w     = shadowContext.shadowDatas[index].viewBias.w;
}

int EvalShadow_GetSplitIndex( ShadowContext shadowContext, int index, real3 positionWS, out uint payloadOffset, out real alpha, out int cascadeCount )
{
    payloadOffset = shadowContext.shadowDatas[index].payloadOffset;

    int   i = 0;
    real  relDistance = 0.0;
    real3 wposDir, splitSphere;

    // find the current cascade
    for( ; i < kMaxShadowCascades; i++, payloadOffset++ )
    {
        float4  sphere  = asfloat( shadowContext.payloads[payloadOffset] );
                wposDir = -sphere.xyz + positionWS;
        float   distSq  = dot( wposDir, wposDir );
        relDistance = distSq / sphere.w;
        if( relDistance > 0.0 && relDistance <= 1.0 )
        {
            splitSphere = sphere.xyz;
            wposDir    /= sqrt( distSq );
            break;
        }
    }
    int shadowSplitIndex = i < kMaxShadowCascades ? i : -1;

    payloadOffset    = shadowContext.shadowDatas[index].payloadOffset + kMaxShadowCascades;
    real3 cascadeDir = asfloat( shadowContext.payloads[payloadOffset].xyz );
    cascadeCount     = shadowContext.payloads[payloadOffset].w;
    payloadOffset++;
    real border      = asfloat( shadowContext.payloads[payloadOffset][shadowSplitIndex] );
    payloadOffset++;
          alpha      = border <= 0.0 ? 0.0 : saturate( (relDistance - (1.0 - border)) / border );
    real  cascDot    = dot( cascadeDir, wposDir );
          alpha      = lerp( alpha, 0.0, saturate( -cascDot * 4.0 ) );

    return shadowSplitIndex;
}

real EvalShadow_CascadedDepth_Blend( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int index, real3 L )
{
    // load the right shadow data for the current face
    uint payloadOffset;
    real alpha;
    int  cascadeCount;
    int  shadowSplitIndex = EvalShadow_GetSplitIndex( shadowContext, index, positionWS, payloadOffset, alpha, cascadeCount );

    if( shadowSplitIndex < 0 )
        return 1.0;

    ShadowData sd = shadowContext.shadowDatas[index];
    EvalShadow_LoadCascadeData( shadowContext, index + 1 + shadowSplitIndex, sd );

    // sample the texture
    uint texIdx, sampIdx;
    UnpackShadowmapId( sd.id, texIdx, sampIdx );
    uint shadowType, shadowAlgorithm;
    UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );

    // normal based bias
    real3 orig_pos = positionWS;
    uint orig_payloadOffset = payloadOffset;
    real recvBiasWeight = EvalShadow_ReceiverBiasWeight( shadowContext, shadowAlgorithm, sd, texIdx, sampIdx, positionWS, normalWS, L, 1.0, false );
    positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, 1.0, recvBiasWeight, false );

    // get shadowmap texcoords
    real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, false );
    // evaluate the first cascade
    real2 sampleBias = EvalShadow_SampleBias_Ortho( sd, normalWS );
    real  shadow     = SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, shadowAlgorithm, texIdx, sampIdx );
    real  shadow1    = 1.0;

    shadowSplitIndex++;
    if( shadowSplitIndex < cascadeCount )
    {
        shadow1 = shadow;

        if( alpha > 0.0 )
        {
            EvalShadow_LoadCascadeData( shadowContext, index + 1 + shadowSplitIndex, sd );
            positionWS = EvalShadow_ReceiverBias( sd, orig_pos, normalWS, L, 1.0, recvBiasWeight, false );
            real3 posNDC;
            posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, false );
            // sample the texture
            sampleBias = EvalShadow_SampleBias_Ortho( sd, normalWS );

            UNITY_BRANCH
            if( all( abs( posNDC.xy ) <= (1.0 - sd.texelSizeRcp.zw * 0.5) ) )
                shadow1 = SampleShadow_SelectAlgorithm( shadowContext, sd, orig_payloadOffset, posTC, sampleBias, shadowAlgorithm, texIdx, sampIdx );
        }
    }
    shadow = lerp( shadow, shadow1, alpha );
    return shadow;
}


#define EvalShadow_CascadedDepth_( _samplerType )                                                                                                                                                                   \
    real EvalShadow_CascadedDepth_Blend( ShadowContext shadowContext, uint shadowAlgorithms[kMaxShadowCascades], Texture2DArray tex, _samplerType samp, real3 positionWS, real3 normalWS, int index, real3 L )      \
    {                                                                                                                                                                                                               \
        uint payloadOffset;                                                                                                                                                                                         \
        real alpha;                                                                                                                                                                                                 \
        int  cascadeCount;                                                                                                                                                                                          \
        int  shadowSplitIndex = EvalShadow_GetSplitIndex(shadowContext, index, positionWS, payloadOffset, alpha, cascadeCount );                                                                                    \
                                                                                                                                                                                                                    \
        if( shadowSplitIndex < 0 )                                                                                                                                                                                  \
            return 1.0;                                                                                                                                                                                             \
                                                                                                                                                                                                                    \
        ShadowData sd = shadowContext.shadowDatas[index];                                                                                                                                                           \
        EvalShadow_LoadCascadeData( shadowContext, index + 1 + shadowSplitIndex, sd );                                                                                                                              \
                                                                                                                                                                                                                    \
        /* normal based bias */                                                                                                                                                                                     \
        real3 orig_pos = positionWS;                                                                                                                                                                                \
        uint orig_payloadOffset = payloadOffset;                                                                                                                                                                    \
        real recvBiasWeight = EvalShadow_ReceiverBiasWeight( sd, tex, samp, positionWS, normalWS, L, 1.0, false );                                                                                                  \
        positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, 1.0, recvBiasWeight, false );                                                                                                            \
                                                                                                                                                                                                                    \
        /* get shadowmap texcoords */                                                                                                                                                                               \
        real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, false );                                                                                                                                             \
        /* evalute the first cascade */                                                                                                                                                                             \
        real2 sampleBias = EvalShadow_SampleBias_Ortho( sd, normalWS );                                                                                                                                             \
        real  shadow     = SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, shadowAlgorithms[shadowSplitIndex], tex, samp );                                                      \
        real  shadow1    = 1.0;                                                                                                                                                                                     \
                                                                                                                                                                                                                    \
        shadowSplitIndex++;                                                                                                                                                                                         \
        if( shadowSplitIndex < cascadeCount )                                                                                                                                                                       \
        {                                                                                                                                                                                                           \
            shadow1 = shadow;                                                                                                                                                                                       \
                                                                                                                                                                                                                    \
            if( alpha > 0.0 )                                                                                                                                                                                       \
            {                                                                                                                                                                                                       \
                EvalShadow_LoadCascadeData( shadowContext, index + 1 + shadowSplitIndex, sd );                                                                                                                      \
                positionWS = EvalShadow_ReceiverBias( sd, orig_pos, normalWS, L, 1.0, recvBiasWeight, false );                                                                                                      \
                real3 posNDC;                                                                                                                                                                                       \
                posTC = EvalShadow_GetTexcoords( sd, positionWS, posNDC, false );                                                                                                                                   \
                /* sample the texture */                                                                                                                                                                            \
                sampleBias = EvalShadow_SampleBias_Ortho( sd, normalWS );                                                                                                                                           \
                                                                                                                                                                                                                    \
                UNITY_BRANCH                                                                                                                                                                                        \
                if( all( abs( posNDC.xy ) <= (1.0 - sd.texelSizeRcp.zw * 0.5) ) )                                                                                                                                   \
                    shadow1 = SampleShadow_SelectAlgorithm( shadowContext, sd, orig_payloadOffset, posTC, sampleBias, shadowAlgorithms[shadowSplitIndex], tex, samp );                                              \
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
    uint payloadOffset;
    real alpha;
    int  cascadeCount;
    int  shadowSplitIndex = EvalShadow_GetSplitIndex( shadowContext, index, positionWS, payloadOffset, alpha, cascadeCount );

    if( shadowSplitIndex < 0 )
        return 1.0;

    ShadowData sd = shadowContext.shadowDatas[index];
    EvalShadow_LoadCascadeData( shadowContext, index + 1 + shadowSplitIndex, sd );

    // get texture description
    uint texIdx, sampIdx;
    UnpackShadowmapId( sd.id, texIdx, sampIdx );
    uint shadowType, shadowAlgorithm;
    UnpackShadowType( sd.shadowType, shadowType, shadowAlgorithm );

    // normal based bias
    real3 orig_pos = positionWS;
    real  recvBiasWeight = EvalShadow_ReceiverBiasWeight( shadowContext, shadowAlgorithm, sd, texIdx, sampIdx, positionWS, normalWS, L, 1.0, false );
    positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, 1.0, recvBiasWeight, false );
    // get shadowmap texcoords
    real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, false );

    int nextSplit = min( shadowSplitIndex+1, cascadeCount-1 );

    if( shadowSplitIndex < nextSplit && step( EvalShadow_hash12( posTC.xy ), alpha ) )
    {
        EvalShadow_LoadCascadeData( shadowContext, index + 1 + nextSplit, sd );
        positionWS = EvalShadow_ReceiverBias( sd, orig_pos, normalWS, L, 1.0, recvBiasWeight, false );
        posTC      = EvalShadow_GetTexcoords( sd, positionWS, false );
    }
    // sample the texture
    real2 sampleBias = EvalShadow_SampleBias_Ortho( sd, normalWS );
    real  shadow     = SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, shadowAlgorithm, texIdx, sampIdx );
    return shadowSplitIndex < (cascadeCount-1) ? shadow : lerp( shadow, 1.0, alpha );
}

#define EvalShadow_CascadedDepth_( _samplerType )                                                                                                                                                                   \
    real EvalShadow_CascadedDepth_Dither( ShadowContext shadowContext, uint shadowAlgorithms[kMaxShadowCascades], Texture2DArray tex, _samplerType samp, real3 positionWS, real3 normalWS, int index, real3 L )     \
    {                                                                                                                                                                                                               \
        /* load the right shadow data for the current face */                                                                                                                                                       \
        uint payloadOffset;                                                                                                                                                                                         \
        real alpha;                                                                                                                                                                                                 \
        int  cascadeCount;                                                                                                                                                                                          \
        int  shadowSplitIndex = EvalShadow_GetSplitIndex( shadowContext, index, positionWS, payloadOffset, alpha, cascadeCount );                                                                                   \
                                                                                                                                                                                                                    \
        if( shadowSplitIndex < 0 )                                                                                                                                                                                  \
            return 1.0;                                                                                                                                                                                             \
                                                                                                                                                                                                                    \
        ShadowData sd = shadowContext.shadowDatas[index];                                                                                                                                                           \
        EvalShadow_LoadCascadeData( shadowContext, index + 1 + shadowSplitIndex, sd );                                                                                                                              \
                                                                                                                                                                                                                    \
        /* normal based bias */                                                                                                                                                                                     \
        real3 orig_pos = positionWS;                                                                                                                                                                                \
        real  recvBiasWeight = EvalShadow_ReceiverBiasWeight( sd, tex, samp, positionWS, normalWS, L, 1.0, false );                                                                                                 \
        positionWS = EvalShadow_ReceiverBias( sd, positionWS, normalWS, L, 1.0, recvBiasWeight, false );                                                                                                            \
        /* get shadowmap texcoords */                                                                                                                                                                               \
        real3 posTC = EvalShadow_GetTexcoords( sd, positionWS, false );                                                                                                                                             \
                                                                                                                                                                                                                    \
        int nextSplit = min( shadowSplitIndex+1, cascadeCount-1 );                                                                                                                                                  \
                                                                                                                                                                                                                    \
        if( shadowSplitIndex != nextSplit && step( EvalShadow_hash12( posTC.xy ), alpha ) )                                                                                                                         \
        {                                                                                                                                                                                                           \
            EvalShadow_LoadCascadeData( shadowContext, index + 1 + nextSplit, sd );                                                                                                                                 \
            positionWS = EvalShadow_ReceiverBias( sd, orig_pos, normalWS, L, 1.0, recvBiasWeight, false );                                                                                                          \
            posTC      = EvalShadow_GetTexcoords( sd, positionWS, false );                                                                                                                                          \
        }                                                                                                                                                                                                           \
        /* sample the texture */                                                                                                                                                                                    \
        real2 sampleBias = EvalShadow_SampleBias_Ortho( sd, normalWS );                                                                                                                                             \
        real  shadow     = SampleShadow_SelectAlgorithm( shadowContext, sd, payloadOffset, posTC, sampleBias, shadowAlgorithms[shadowSplitIndex], tex, samp );                                                      \
        return shadowSplitIndex < (cascadeCount-1) ? shadow : lerp( shadow, 1.0, alpha );                                                                                                                           \
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
    int faceIndex = CubeMapFaceID( -L ) + 1;
    sd = shadowContext.shadowDatas[index + faceIndex];

    real4 closestNDC = { 0,0,0,1 };
    uint2 texelIdx = EvalShadow_GetIntTexcoords( sd, positionWS, closestNDC.xy, true );

    // load the texel
    uint texIdx, sampIdx;
    UnpackShadowmapId( sd.id, texIdx, sampIdx );
    closestNDC.z = LoadShadow_T2DA( shadowContext, texIdx, texelIdx, sd.slice );

    // reconstruct depth position
    real4 closestWS = mul( closestNDC, sd.shadowToWorld );
    return closestWS.xyz / closestWS.w;
}


real3 EvalShadow_GetClosestSample_Point( ShadowContext shadowContext, Texture2DArray tex, real3 positionWS, int index, real3 L )
{
    // get the algorithm
    ShadowData sd = shadowContext.shadowDatas[index];
    // load the right shadow data for the current face
    int faceIndex = CubeMapFaceID( -L ) + 1;
    sd = shadowContext.shadowDatas[index + faceIndex];

    real4 closestNDC = { 0,0,0,1 };
    uint2 texelIdx = EvalShadow_GetIntTexcoords( sd, positionWS, closestNDC.xy, true );

    // load the texel
    closestNDC.z = LOAD_TEXTURE2D_ARRAY_LOD( tex, texelIdx, sd.slice, 0 ).x;

    // reconstruct depth position
    real4 closestWS = mul( closestNDC, sd.shadowToWorld );
    return closestWS.xyz / closestWS.w;
}

real3 EvalShadow_GetClosestSample_Spot( ShadowContext shadowContext, real3 positionWS, int index )
{
    // get the algorithm
    ShadowData sd = shadowContext.shadowDatas[index];

    real4 closestNDC = { 0,0,0,1 };
    uint2 texelIdx = EvalShadow_GetIntTexcoords( sd, positionWS, closestNDC.xy, true );

    // load the texel
    uint texIdx, sampIdx;
    UnpackShadowmapId( sd.id, texIdx, sampIdx );
    closestNDC.z = LoadShadow_T2DA( shadowContext, texIdx, texelIdx, sd.slice );

    // reconstruct depth position
    real4 closestWS = mul( closestNDC, sd.shadowToWorld );
    return closestWS.xyz / closestWS.w;
}


real3 EvalShadow_GetClosestSample_Spot( ShadowContext shadowContext, Texture2DArray tex, real3 positionWS, int index )
{
    // get the algorithm
    ShadowData sd = shadowContext.shadowDatas[index];

    real4 closestNDC = { 0,0,0,1 };
    uint2 texelIdx = EvalShadow_GetIntTexcoords( sd, positionWS, closestNDC.xy, true );

    // load the texel
    closestNDC.z = LOAD_TEXTURE2D_ARRAY_LOD( tex, texelIdx, sd.slice, 0 ).x;

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
    int faceIndex = shadowType == GPUSHADOWTYPE_POINT ? (CubeMapFaceID( -L ) + 1) : 0;
    sd = shadowContext.shadowDatas[index + faceIndex];

    real4 closestNDC = { 0,0,0,1 };
    uint2 texelIdx = EvalShadow_GetIntTexcoords( sd, positionWS, closestNDC.xy, true );

    // load the texel
    uint texIdx, sampIdx;
    UnpackShadowmapId( sd.id, texIdx, sampIdx );
    closestNDC.z = LoadShadow_T2DA( shadowContext, texIdx, texelIdx, sd.slice );

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
    int faceIndex = shadowType == GPUSHADOWTYPE_POINT ? (CubeMapFaceID( -L ) + 1) : 0;
    sd = shadowContext.shadowDatas[index + faceIndex];

    real4 closestNDC = { 0,0,0,1 };
    uint2 texelIdx = EvalShadow_GetIntTexcoords( sd, positionWS, closestNDC.xy, true );

    // load the texel
    closestNDC.z = LOAD_TEXTURE2D_ARRAY_LOD( tex, texelIdx, sd.slice, 0 ).x;

    // reconstruct depth position
    real4 closestWS = mul( closestNDC, sd.shadowToWorld );
    return closestWS.xyz / closestWS.w;
}

real EvalShadow_SampleClosestDistance_Punctual( ShadowContext shadowContext, Texture2DArray tex, SamplerState sampl,
                                                real3 positionWS, int index, real3 L, real3 lightPositionWS )
{
    // get the algorithm
    ShadowData sd = shadowContext.shadowDatas[index];
    uint shadowType;
    UnpackShadowType( sd.shadowType, shadowType );
    // load the right shadow data for the current face
    int faceIndex = shadowType == GPUSHADOWTYPE_POINT ? (CubeMapFaceID( -L ) + 1) : 0;
    sd = shadowContext.shadowDatas[index + faceIndex];

    real4 closestNDC = { 0,0,0,1 };
    real2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy, true );

    // sample the shadow map
    closestNDC.z = SAMPLE_TEXTURE2D_ARRAY_LOD( tex, sampl, texelIdx, sd.slice, 0 ).x;

    // reconstruct depth position
    real4 closestWS = mul( closestNDC, sd.shadowToWorld );
    real3 occluderPosWS = closestWS.xyz / closestWS.w;

    return distance( occluderPosWS, lightPositionWS );
}

real3 EvalShadow_GetClosestSample_Cascade( ShadowContext shadowContext, real3 positionWS, real3 normalWS, int index, real4 L )
{
    // load the right shadow data for the current face
    uint payloadOffset;
    real alpha;
    int  cascadeCount;
    int  shadowSplitIndex = EvalShadow_GetSplitIndex( shadowContext, index, positionWS, payloadOffset, alpha, cascadeCount );

    if( shadowSplitIndex < 0 )
        return 0.0;

    ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];

    real4 closestNDC = { 0,0,0,1 };
    uint2 texelIdx = EvalShadow_GetIntTexcoords( sd, positionWS, closestNDC.xy, false );

    // load the texel
    uint texIdx, sampIdx;
    UnpackShadowmapId( sd.id, texIdx, sampIdx );
    closestNDC.z = LoadShadow_T2DA( shadowContext, texIdx, texelIdx, sd.slice );

    // reconstruct depth position
    real4 closestWS = mul( closestNDC, sd.shadowToWorld );
    return closestWS.xyz / closestWS.w;
}

real3 EvalShadow_GetClosestSample_Cascade( ShadowContext shadowContext, Texture2DArray tex, real3 positionWS, real3 normalWS, int index, real4 L )
{
    // load the right shadow data for the current face
    uint payloadOffset;
    real alpha;
    int  cascadeCount;
    int  shadowSplitIndex = EvalShadow_GetSplitIndex( shadowContext, index, positionWS, payloadOffset, alpha, cascadeCount );

    if( shadowSplitIndex < 0 )
        return 0.0;

    ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];

    real4 closestNDC = { 0,0,0,1 };
    uint2 texelIdx = EvalShadow_GetIntTexcoords( sd, positionWS, closestNDC.xy, false );

    // load the texel
    uint texIdx, sampIdx;
    UnpackShadowmapId( sd.id, texIdx, sampIdx );
    closestNDC.z = LOAD_TEXTURE2D_ARRAY_LOD( tex, texelIdx, sd.slice, 0 ).x;

    // reconstruct depth position
    real4 closestWS = mul( closestNDC, sd.shadowToWorld );
    return closestWS.xyz / closestWS.w;
}

real EvalShadow_SampleClosestDistance_Cascade( ShadowContext shadowContext, Texture2DArray tex, SamplerState sampl,
                                               real3 positionWS, real3 normalWS, int index, real4 L, out real3 nearPlanePositionWS )
{
    // load the right shadow data for the current face
    uint payloadOffset;
    real alpha;
    int  cascadeCount;
    int  shadowSplitIndex = EvalShadow_GetSplitIndex( shadowContext, index, positionWS, payloadOffset, alpha, cascadeCount );

    if( shadowSplitIndex < 0 )
        return 0.0;

    ShadowData sd = shadowContext.shadowDatas[index + 1 + shadowSplitIndex];

    real4 closestNDC = { 0,0,0,1 };
    real2 texelIdx = EvalShadow_GetTexcoords( sd, positionWS, closestNDC.xy, false );

    // sample the shadow map
    uint texIdx, sampIdx;
    UnpackShadowmapId( sd.id, texIdx, sampIdx );
    closestNDC.z = SAMPLE_TEXTURE2D_ARRAY_LOD( tex, sampl, texelIdx, sd.slice, 0 ).x;

    // reconstruct depth position
    real4 closestWS = mul( closestNDC, sd.shadowToWorld );
    real3 occluderPosWS = closestWS.xyz / closestWS.w;

    // TODO: avoid the matrix multiplication here.
    real4 nearPlanePos = mul( real4( 0,0,1,1 ), sd.shadowToWorld ); // Note the reversed Z
    nearPlanePositionWS = nearPlanePos.xyz / nearPlanePos.w;

    return distance( occluderPosWS, nearPlanePositionWS );
}
