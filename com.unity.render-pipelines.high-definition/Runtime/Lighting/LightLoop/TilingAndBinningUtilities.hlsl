#ifndef UNITY_TILINGANDBINNINGUTILITIES_INCLUDED
#define UNITY_TILINGANDBINNINGUTILITIES_INCLUDED

// REMOVE!!!
uint GetTileSize()
{
    return 16;
}

// The HLSL preprocessor does not support the '%' operator.
#define REMAINDER(a, n) ((a) - (n) * ((a) / (n)))

// Returns the location of the N-th set bit starting from the lowest order bit and working upward.
// Slow implementation - do not use for large bit sets.
// Could be optimized - see https://graphics.stanford.edu/~seander/bithacks.html
uint NthBitLow(uint value, uint n)
{
    uint b = -1;                                    // Consistent with the behavior of firstbitlow()
    uint c = countbits(value);

    if (n < c)                                      // Validate inputs
    {
        uint r = n + 1;                             // Compute the number of remaining bits

        do
        {
            uint f = firstbitlow(value >> (b + 1)); // Find the next set bit
            b += f + r;                             // Make a guess (assume all [b+f+1,b+f+r] bits are set)
            c = countbits(value << (32 - (b + 1))); // Count the number of bits actually set
            r = (n + 1) - c;                        // Compute the number of remaining bits
        } while (r > 0);
    }

    return b;
}

float4x4 Translation4x4(float3 d)
{
    float4x4 M = k_identity4x4;

    M._14_24_34 = d; // Last column

    return M;
}

// Scale followed by rotation (scaled axes).
float3x3 ScaledRotation3x3(float3 xAxis, float3 yAxis, float3 zAxis)
{
    float3x3 R = float3x3(xAxis, yAxis, zAxis);
    float3x3 C = transpose(R); // Row to column

    return C;
}

float3x3 Invert3x3(float3x3 R)
{
    float3x3 C   = transpose(R); // Row to column
    float    det = dot(C[0], cross(C[1], C[2]));
    float3x3 adj = float3x3(cross(C[1], C[2]),
                            cross(C[2], C[0]),
                            cross(C[0], C[1]));
    return rcp(det) * adj;
}

float4x4 Homogenize3x3(float3x3 R)
{
    float4x4 M = float4x4(float4(R[0], 0),
                          float4(R[1], 0),
                          float4(R[2], 0),
                          float4(0,0,0,1));
    return M;
}

float4x4 PerspectiveProjection4x4(float a, float g, float n, float f)
{
    float b = (f + n) * rcp(f - n);    // Z in [-1, 1]
    float c = -2 * f * n * rcp(f - n); // No Z-reversal

    return float4x4(g/a, 0, 0, 0,
                      0, g, 0, 0,
                      0, 0, b, c,
                      0, 0, 1, 0);
}

// The intervals must be defined s.t.
// the 'x' component holds the lower bound and
// the 'y' component holds the upper bound.
bool IntervalsOverlap(float2 i1, float2 i2)
{
    float l = max(i1.x, i2.x); // Lower bound of the intersection interval
    float u = min(i1.y, i2.y); // Upper bound of the intersection interval

    return l <= u;             // Is the interval non-empty?
}

uint ComputeEntityBoundsBufferIndex(uint entityIndex, uint eye)
{
    return IndexFromCoordinate(uint2(entityIndex, eye), _BoundedEntityCount);
}

uint ComputeZBinBufferIndex(uint bin, uint category, uint eye)
{
    return IndexFromCoordinate(uint3(bin, category, eye),
                               uint2(Z_BIN_COUNT, BOUNDEDENTITYCATEGORY_COUNT));
}

#ifndef NO_SHADERVARIABLESGLOBAL_HLSL

uint ComputeCoarseTileBufferIndex(uint2 tileCoord, uint category, uint eye)
{
    uint rowSize = (uint)_CoarseXyTileBufferDimensions.x;
    uint stride  = COARSE_XY_TILE_ENTITY_LIMIT / 2; // We use 'uint' buffer rather than a 'uint16_t[n]'

    return stride * IndexFromCoordinate(uint4(tileCoord, category, eye),
                                        uint3(rowSize, Z_BIN_COUNT, BOUNDEDENTITYCATEGORY_COUNT));
}

uint ComputeZBinFromLinearDepth(float w)
{
    float z = EncodeLogarithmicDepth(w, _ZBinBufferEncodingParams);
    z = saturate(z); // Clamp to the region between the near and the far planes

    return min((uint)(z * Z_BIN_COUNT), Z_BIN_COUNT - 1);
}

#endif // NO_SHADERVARIABLESGLOBAL_HLSL

#endif // UNITY_TILINGANDBINNINGUTILITIES_INCLUDED
