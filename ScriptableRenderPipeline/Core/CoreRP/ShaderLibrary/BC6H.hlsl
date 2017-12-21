// Ref: https://github.com/knarkowicz/GPURealTimeBC6H/blob/master/bin/compress.hlsl
// Doc: https://msdn.microsoft.com/en-us/library/windows/desktop/hh308952(v=vs.85).aspx

// Measure compression error
real CalcMSLE(real3 a, real3 b)
{
    real3 err = log2(( b + 1.0) / (a + 1.0 ));
    err = err * err;
    return err.x + err.y + err.z;
}

// Quantification Helpers
real3 Quantize7(real3 x)
{
    return (f32tof16(x) * 128.0) / (0x7bff + 1.0);
}

real3 Quantize9(real3 x)
{
    return (f32tof16(x) * 512.0) / (0x7bff + 1.0);
}

real3 Quantize10(real3 x)
{
    return (f32tof16(x) * 1024.0) / (0x7bff + 1.0);
}

real3 Unquantize7(real3 x)
{
    return (x * 65536.0 + 0x8000) / 128.0;
}

real3 Unquantize9(real3 x)
{
    return (x * 65536.0 + 0x8000) / 512.0;
}

real3 Unquantize10(real3 x)
{
    return (x * 65536.0 + 0x8000) / 1024.0;
}

// BC6H Helpers
// Compute index of a texel projected against endpoints
uint ComputeIndex3( real texelPos, real endPoint0Pos, real endPoint1Pos )
{
    real r = ( texelPos - endPoint0Pos ) / ( endPoint1Pos - endPoint0Pos );
    return (uint) clamp( r * 6.98182f + 0.00909f + 0.5f, 0.0, 7.0 );
}

uint ComputeIndex4( real texelPos, real endPoint0Pos, real endPoint1Pos )
{
    real r = ( texelPos - endPoint0Pos ) / ( endPoint1Pos - endPoint0Pos );
    return (uint) clamp( r * 14.93333f + 0.03333f + 0.5f, 0.0, 15.0 );
}

void SignExtend( inout real3 v1, uint mask, uint signFlag )
{
    int3 v = (int3) v1;
    v.x = ( v.x & mask ) | ( v.x < 0 ? signFlag : 0 );
    v.y = ( v.y & mask ) | ( v.y < 0 ? signFlag : 0 );
    v.z = ( v.z & mask ) | ( v.z < 0 ? signFlag : 0 );
    v1 = v;
}

// 2nd step for unquantize
real3 FinishUnquantize( real3 endpoint0Unq, real3 endpoint1Unq, real weight )
{
    real3 comp = ( endpoint0Unq * ( 64.0 - weight ) + endpoint1Unq * weight + 32.0 ) * ( 31.0 / 4096.0 );
    return f16tof32( uint3( comp ) );
}

// BC6H Modes
void EncodeMode11( inout uint4 block, inout real blockMSLE, real3 texels[ 16 ] )
{
    // compute endpoints (min/max RGB bbox)
    real3 blockMin = texels[ 0 ];
    real3 blockMax = texels[ 0 ];
    uint i;
    for (i = 1; i < 16; ++i )
    {
        blockMin = min( blockMin, texels[ i ] );
        blockMax = max( blockMax, texels[ i ] );
    }

    // refine endpoints in log2 RGB space
    real3 refinedBlockMin = blockMax;
    real3 refinedBlockMax = blockMin;
    for (i = 0; i < 16; ++i )
    {
        refinedBlockMin = min( refinedBlockMin, texels[ i ] == blockMin ? refinedBlockMin : texels[ i ] );
        refinedBlockMax = max( refinedBlockMax, texels[ i ] == blockMax ? refinedBlockMax : texels[ i ] );
    }

    real3 logBlockMax          = log2( blockMax + 1.0 );
    real3 logBlockMin          = log2( blockMin + 1.0 );
    real3 logRefinedBlockMax   = log2( refinedBlockMax + 1.0 );
    real3 logRefinedBlockMin   = log2( refinedBlockMin + 1.0 );
    real3 logBlockMaxExt       = ( logBlockMax - logBlockMin ) * ( 1.0 / 32.0 );
    logBlockMin += min( logRefinedBlockMin - logBlockMin, logBlockMaxExt );
    logBlockMax -= min( logBlockMax - logRefinedBlockMax, logBlockMaxExt );
    blockMin = exp2( logBlockMin ) - 1.0;
    blockMax = exp2( logBlockMax ) - 1.0;

    real3 blockDir = blockMax - blockMin;
    blockDir = blockDir / ( blockDir.x + blockDir.y + blockDir.z );

    real3 endpoint0    = Quantize10( blockMin );
    real3 endpoint1    = Quantize10( blockMax );
    real endPoint0Pos  = f32tof16( dot( blockMin, blockDir ) );
    real endPoint1Pos  = f32tof16( dot( blockMax, blockDir ) );


    // check if endpoint swap is required
    real fixupTexelPos = f32tof16( dot( texels[ 0 ], blockDir ) );
    uint fixupIndex = ComputeIndex4( fixupTexelPos, endPoint0Pos, endPoint1Pos );
    if ( fixupIndex > 7 )
    {
        Swap( endPoint0Pos, endPoint1Pos );
        Swap( endpoint0, endpoint1 );
    }

    // compute indices
    uint indices[ 16 ] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
    for (i = 0; i < 16; ++i )
    {
        real texelPos = f32tof16( dot( texels[ i ], blockDir ) );
        indices[ i ] = ComputeIndex4( texelPos, endPoint0Pos, endPoint1Pos );
    }

    // compute compression error (MSLE)
    real3 endpoint0Unq = Unquantize10( endpoint0 );
    real3 endpoint1Unq = Unquantize10( endpoint1 );
    real msle = 0.0;
    for (i = 0; i < 16; ++i )
    {
        real weight = floor( ( indices[ i ] * 64.0 ) / 15.0 + 0.5);
        real3 texelUnc = FinishUnquantize( endpoint0Unq, endpoint1Unq, weight );

        msle += CalcMSLE( texels[ i ], texelUnc );
    }


    // encode block for mode 11
    blockMSLE = msle;
    block.x = 0x03;

    // endpoints
    block.x |= (uint) endpoint0.x << 5;
    block.x |= (uint) endpoint0.y << 15;
    block.x |= (uint) endpoint0.z << 25;
    block.y |= (uint) endpoint0.z >> 7;
    block.y |= (uint) endpoint1.x << 3;
    block.y |= (uint) endpoint1.y << 13;
    block.y |= (uint) endpoint1.z << 23;
    block.z |= (uint) endpoint1.z >> 9;

    // indices
    block.z |= indices[ 0 ] << 1;
    block.z |= indices[ 1 ] << 4;
    block.z |= indices[ 2 ] << 8;
    block.z |= indices[ 3 ] << 12;
    block.z |= indices[ 4 ] << 16;
    block.z |= indices[ 5 ] << 20;
    block.z |= indices[ 6 ] << 24;
    block.z |= indices[ 7 ] << 28;
    block.w |= indices[ 8 ] << 0;
    block.w |= indices[ 9 ] << 4;
    block.w |= indices[ 10 ] << 8;
    block.w |= indices[ 11 ] << 12;
    block.w |= indices[ 12 ] << 16;
    block.w |= indices[ 13 ] << 20;
    block.w |= indices[ 14 ] << 24;
    block.w |= indices[ 15 ] << 28;
}
