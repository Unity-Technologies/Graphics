#ifndef __PROBEVOLUME_HLSL__
#define __PROBEVOLUME_HLSL__

// APV specific code
struct APVConstants
{
    float3x4    WStoRS;
    float       normalBias; // amount of biasing along the normal
    int3        centerRS;   // index center location in refspace
    int3        centerIS;   // index center location in index space
    uint3       indexDim;   // resolution of the index
    uint3       poolDim;    // resolution of the brick pool
};

static const int kAPVConstantsSize = 12 + 1 + 3 + 3 + 3 + 3;


struct APVResources
{
    StructuredBuffer<int> index;
    Texture3D L0;
    Texture3D L1_R;
    Texture3D L1_G;
    Texture3D L1_B;
};

APVConstants LoadAPVConstants( StructuredBuffer<int> index )
{
    APVConstants apvc;
    apvc.WStoRS[0][0] = asfloat( index[ 0] );
    apvc.WStoRS[1][0] = asfloat( index[ 1] );
    apvc.WStoRS[2][0] = asfloat( index[ 2] );
    apvc.WStoRS[0][1] = asfloat( index[ 3] );
    apvc.WStoRS[1][1] = asfloat( index[ 4] );
    apvc.WStoRS[2][1] = asfloat( index[ 5] );
    apvc.WStoRS[0][2] = asfloat( index[ 6] );
    apvc.WStoRS[1][2] = asfloat( index[ 7] );
    apvc.WStoRS[2][2] = asfloat( index[ 8] );
    apvc.WStoRS[0][3] = asfloat( index[ 9] );
    apvc.WStoRS[1][3] = asfloat( index[10] );
    apvc.WStoRS[2][3] = asfloat( index[11] );
    apvc.normalBias   = asfloat( index[12] );
    apvc.centerRS.x   = index[13];
    apvc.centerRS.y   = index[14];
    apvc.centerRS.z   = index[15];
    apvc.centerIS.x   = index[16];
    apvc.centerIS.y   = index[17];
    apvc.centerIS.z   = index[18];
    apvc.indexDim.x   = index[19];
    apvc.indexDim.y   = index[20];
    apvc.indexDim.z   = index[21];
    apvc.poolDim.x    = index[22];
    apvc.poolDim.y    = index[23];
    apvc.poolDim.z    = index[24];
    return apvc;
}

float3 DecodeSH( float l0, float3 l1 )
{
    return (l1 - 0.5) * 4.0 * l0;
}

float3 EvaluateAmbientProbe(float3 normalWS)
{
    return float3( 1.00, 0.75, 0.3 );
}

float3 EvaluateAdaptiveProbeVolume(in float3 posWS, in float3 normalWS, in APVResources apvRes)
{
    APVConstants apvConst = LoadAPVConstants( apvRes.index );

    // transform into APV space
    float3 posRS  = mul( apvConst.WStoRS, float4( posWS + normalWS * apvConst.normalBias, 1.0 ) );
           posRS -= apvConst.centerRS;

    // check bounds
    if( any( abs( posRS ) > (apvConst.indexDim / 2) ) )
    {
        return EvaluateAmbientProbe( normalWS );
    }

    // convert to index
    int3 index = apvConst.centerIS + floor( posRS );
         index = index % apvConst.indexDim;
    // resolve the index
    int  flattened_index = index.z * (apvConst.indexDim.x * apvConst.indexDim.y)  + index.y * apvConst.indexDim.x + index.x;
    uint packed_pool_idx = apvRes.index[kAPVConstantsSize + flattened_index];

    // no valid brick loaded for this index, fallback to ambient probe
    if( packed_pool_idx == 0xffffffff )
    {
        return EvaluateAmbientProbe( normalWS );
    }

    // unpack pool idx
    // size is encoded in the upper 4 bits
    uint   subdiv              = (packed_pool_idx >> 28) & 15;
    float  cellSize            = pow( 3.0, subdiv );
    uint   flattened_pool_idx  = packed_pool_idx & ((1 << 28) - 1);
    uint3  pool_idx;
           pool_idx.z          = flattened_pool_idx / (apvConst.poolDim.x * apvConst.poolDim.y);
           flattened_pool_idx -= pool_idx.z * (apvConst.poolDim.x * apvConst.poolDim.y);
           pool_idx.y          = flattened_pool_idx / apvConst.poolDim.x;
           pool_idx.x          = flattened_pool_idx - (pool_idx.y * apvConst.poolDim.x);
    float3 pool_uvw            = ((float3) pool_idx + 0.5) / (float3) apvConst.poolDim;

    // calculate uv offset and scale
    float3 offset    = frac( posRS / (float) cellSize );    // [0;1] in brick space
           //offset    = clamp( offset, 0.25, 0.75 );         // [0.25;0.75] in brick space (is this actually necessary?)
           offset   *= 3.0 / (float3) apvConst.poolDim;     // convert brick footprint to texels footprint in pool texel space
           pool_uvw += offset;                              // add the final offset


    // sample the pool textures to get the SH coefficients
    float3 l0   = SAMPLE_TEXTURE3D_LOD(apvRes.L0  , s_linear_clamp_sampler, pool_uvw, 0).rgb;
    float3 l1_R = SAMPLE_TEXTURE3D_LOD(apvRes.L1_R, s_linear_clamp_sampler, pool_uvw, 0).rgb;
    float3 l1_G = SAMPLE_TEXTURE3D_LOD(apvRes.L1_G, s_linear_clamp_sampler, pool_uvw, 0).rgb;
    float3 l1_B = SAMPLE_TEXTURE3D_LOD(apvRes.L1_B, s_linear_clamp_sampler, pool_uvw, 0).rgb;

    // decode the L1 coefficients
    l1_R = DecodeSH(l0.r, l1_R);
    l1_G = DecodeSH(l0.g, l1_G);
    l1_B = DecodeSH(l0.b, l1_B);

    // evaluate the SH coefficients
    float3 final_color = SHEvalLinearL0L1( normalWS, float4( l1_R, l0.r ), float4( l1_G, l0.g ), float4( l1_B, l0.b ) );
    return final_color;
}

#endif // __PROBEVOLUME_HLSL__
