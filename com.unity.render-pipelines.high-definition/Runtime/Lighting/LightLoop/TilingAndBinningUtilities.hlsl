#ifndef UNITY_TILINGANDBINNINGUTILITIES_INCLUDED
#define UNITY_TILINGANDBINNINGUTILITIES_INCLUDED

// The HLSL preprocessor does not support the '%' operator.
#define REMAINDER(A, N)       ((A) - (N) * ((A) / (N)))
#define CLEAR_SIGN_BIT(X)     (asint(X) & INT_MAX)
#define DIV_ROUND_UP(N, D)    (((N) + (D) - 1) / (D)) // No division by 0 checks

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
    float    det = dot(C[0],cross(C[1], C[2]));
    float3x3 adj = float3x3(cross(C[1], C[2]),
                            cross(C[2], C[0]),
                            cross(C[0], C[1]));
    return rcp(det) * adj; // Adjugate / Determinant
}

float4x4 Homogenize3x3(float3x3 R)
{
    float4x4 M = float4x4(float4(R[0], 0),
                          float4(R[1], 0),
                          float4(R[2], 0),
                          float4(0,0,0,1));
    return M;
}

float4x4 OptimizeOrthographicMatrix(float4x4 M)
{
    // | x 0 0 x |
    // | 0 x 0 x |
    // | x x x x |
    // | 0 0 0 1 |

    M._12_13    = 0;
    M._21_23    = 0;
    M._41_42_43 = 0; M._44 = 1;

    return M;
}

float4x4 OptimizePerspectiveMatrix(float4x4 M)
{
    // | x 0 x 0 |
    // | 0 x x 0 |
    // | x x x x |
    // | 0 0 x 0 |

    M._12_14    = 0;
    M._21_24    = 0;
    M._41_42_44 = 0; // Unity sometimes sets M._43 = -1 :(

    return M;
}

// a: aspect ratio.
// p: distance to the projection plane.
// n: distance to the near plane.
// f: distance to the far plane.
float4x4 PerspectiveProjection4x4(float a, float p, float n, float f)
{
    float b = (f + n) * rcp(f - n);
    float c = -2 * f * n * rcp(f - n);

    return float4x4(p/a, 0, 0, 0,
                      0, p, 0, 0,  // No Y-flip
                      0, 0, b, c,  // Z in [-1, 1], no Z-reversal
                      0, 0, 1, 0); // No W-flip
}

// The intervals must be defined s.t.
// the 'x' component holds the lower bound and
// the 'y' component holds the upper bound.
bool IntervalsOverlap(float2 i1, float2 i2)
{
    float l = max(i1.x, i2.x); // Lower bound of the intersection interval
    float u = min(i1.y, i2.y); // Upper bound of the intersection interval

    return l <= u;             // Is the intersection non-empty?
}

// The intervals must be defined s.t.
// the 'x' component holds the lower bound and
// the 'y' component holds the upper bound.
bool IntervalsOverlap(uint2 i1, uint2 i2)
{
    uint l = max(i1.x, i2.x); // Lower bound of the intersection interval
    uint u = min(i1.y, i2.y); // Upper bound of the intersection interval

    return l <= u;            // Is the intersection non-empty?
}

uint ComputeEntityBoundsBufferIndex(uint globalEntityIndex, uint eye)
{
    return IndexFromCoordinate(uint2(globalEntityIndex, eye), _BoundedEntityCount);
}

uint ComputeZBinBufferIndex(uint zBin, uint category, uint eye)
{
    return IndexFromCoordinate(uint3(zBin, category, eye),
                               uint2(Z_BIN_COUNT, BOUNDEDENTITYCATEGORY_COUNT));
}

#ifndef NO_SHADERVARIABLESGLOBAL_HLSL

// Repackage to work around ridiculous constant buffer limitations of HLSL.
static uint s_BoundedEntityOffsetPerCategory[BOUNDEDENTITYCATEGORY_COUNT] = (uint[BOUNDEDENTITYCATEGORY_COUNT])_BoundedEntityOffsetPerCategory;
static uint s_BoundedEntityCountPerCategory[BOUNDEDENTITYCATEGORY_COUNT]  = (uint[BOUNDEDENTITYCATEGORY_COUNT])_BoundedEntityCountPerCategory;

uint ComputeEntityBoundsBufferIndex(uint entityIndex, uint category, uint eye)
{
    uint offset = s_BoundedEntityOffsetPerCategory[category];
    return IndexFromCoordinate(uint2(offset + entityIndex, eye), _BoundedEntityCount);
}

// Cannot be used to index directly into the buffer.
// Use ComputeZBinBufferIndex for that purpose.
uint ComputeZBinIndex(float linearDepth)
{
    float z = EncodeLogarithmicDepth(linearDepth, _ZBinBufferEncodingParams);
    z = saturate(z); // Clamp to the region between the near and the far planes

    return min((uint)(z * Z_BIN_COUNT), Z_BIN_COUNT - 1);
}

#define COARSE_TILE_BUFFER_DIMS uint2(_CoarseTileBufferDimensions.xy)
#define   FINE_TILE_BUFFER_DIMS uint2(  _FineTileBufferDimensions.xy)

#if defined(COARSE_BINNING)
    #define TILE_BUFFER         _CoarseTileBuffer
    #define TILE_BUFFER_DIMS    COARSE_TILE_BUFFER_DIMS
    #define TILE_ENTRY_LIMIT    COARSE_TILE_ENTRY_LIMIT
    #define TILE_SIZE           COARSE_TILE_SIZE
#elif defined(FINE_BINNING)
    #define TILE_BUFFER         _FineTileBuffer
    #define TILE_BUFFER_DIMS    FINE_TILE_BUFFER_DIMS
    #define TILE_ENTRY_LIMIT    FINE_TILE_ENTRY_LIMIT
    #define TILE_SIZE           FINE_TILE_SIZE
#else // !(defined(COARSE_BINNING) || defined(FINE_BINNING))
    // These must be defined so the compiler does not complain.
    #define TILE_BUFFER_DIMS    uint2(0, 0)
    #define TILE_ENTRY_LIMIT    0
    #define TILE_SIZE           1
#endif

// Cannot be used to index directly into the buffer.
// Use ComputeTileBufferIndex for that purpose.
uint ComputeTileIndex(uint2 pixelCoord)
{
    uint2 tileCoord = pixelCoord / TILE_SIZE;
    return IndexFromCoordinate(uint4(tileCoord, 0, 0),
                               uint3(TILE_BUFFER_DIMS, BOUNDEDENTITYCATEGORY_COUNT));
}

// Internal. Do not call directly.
uint ComputeTileBufferHeaderIndex(uint tile, uint category, uint eye, uint2 tileBufferDims)
{
    uint eyeCatOffset = IndexFromCoordinate(uint4(0, 0, category, eye),
                                            uint3(tileBufferDims, BOUNDEDENTITYCATEGORY_COUNT));

    return eyeCatOffset + tile;
}

// Contains index ranges.
// tile: output of ComputeTileIndex.
uint ComputeTileBufferHeaderIndex(uint tile, uint category, uint eye)
{
    return ComputeTileBufferHeaderIndex(tile, category, eye, TILE_BUFFER_DIMS);
}

// Internal. Do not call directly.
uint ComputeTileBufferBodyIndex(uint tile, uint category, uint eye, uint2 tileBufferDims, uint tileEntryLimit)
{
    // TODO: may want to precompute this.
    uint headerOffset = tileBufferDims.x * tileBufferDims.y * BOUNDEDENTITYCATEGORY_COUNT * _XRViewCount;
    uint eyeCatOffset = IndexFromCoordinate(uint4(0, 0, category, eye),
                                            uint3(tileBufferDims, BOUNDEDENTITYCATEGORY_COUNT));
    uint stride = tileEntryLimit / 2; // 16-bit index list

    return headerOffset + stride * (eyeCatOffset + tile);
}

// Contains index lists.
uint ComputeTileBufferBodyIndex(uint tile, uint category, uint eye)
{
    return ComputeTileBufferBodyIndex(tile, category, eye, TILE_BUFFER_DIMS, TILE_ENTRY_LIMIT);
}

#endif // NO_SHADERVARIABLESGLOBAL_HLSL

#endif // UNITY_TILINGANDBINNINGUTILITIES_INCLUDED
