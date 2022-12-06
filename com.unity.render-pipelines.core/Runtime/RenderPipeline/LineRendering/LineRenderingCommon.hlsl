#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/RenderPipeline/LineRendering/LineRendering.Data.cs.hlsl"

#define COUNTER_BIN_RECORD          0 << 2
#define COUNTER_BIN_QUEUE_SIZE      1 << 2
#define COUNTER_BIN_QUEUE_INDEX     2 << 2
#define COUNTER_CLUSTER_QUEUE_SIZE  3 << 2
#define COUNTER_CLUSTER_QUEUE_INDEX 4 << 2
#define COUNTER_ACTIVE_SEGMENTS     5 << 2
#define COUNTER_SHADING_SAMPLES     6 << 2
#define COUNTER_GROUP_SEG_OFFSET    7 << 2

#define OFFSETS_VERTEX  0 << 2
#define OFFSETS_SEGMENT 1 << 2

// Maximum representable floating-point number
#define FLT_MAX  3.402823466e+38
#define DEPTH_CLIP_BIAS 1e-4

#define INTERP(coords, a, b) (coords.y * a) + (coords.x * b)

//TODO: make this a performance config for artists
#define SamplesPerSegment 2

#define INVALID_SHADING_SAMPLE 0xFFFFFFFF

struct PackedSegmentRecord
{
    uint4 A; // PositionSS0, DepthVS0, VertexID0
    uint4 B; // PositionSS1, DepthVS1, VertexID1

    static PackedSegmentRecord Pack(SegmentRecord record)
    {
        PackedSegmentRecord packedRecord;
        {
            packedRecord.A = uint4(asuint(float3(record.positionSS0, record.depthVS0)), record.vertexIndex0);
            packedRecord.B = uint4(asuint(float3(record.positionSS1, record.depthVS1)), record.vertexIndex1);
        }
        return packedRecord;
    }

    static SegmentRecord Unpack(PackedSegmentRecord packedRecord)
    {
        SegmentRecord record;
        {
            record.positionSS0  = asfloat(packedRecord.A.xy);
            record.positionSS1  = asfloat(packedRecord.B.xy);
            record.depthVS0     = asfloat(packedRecord.A.z);
            record.depthVS1     = asfloat(packedRecord.B.z);
            record.vertexIndex0 = packedRecord.A.w;
            record.vertexIndex1 = packedRecord.B.w;
        }
        return record;
    }
};

void StoreSegmentRecord(RWByteAddressBuffer buffer, SegmentRecord record, uint index)
{
    const PackedSegmentRecord packedSegment = PackedSegmentRecord::Pack(record);

    const uint offset = 32 * index;

    buffer.Store4(offset + (0 << 4), packedSegment.A);
    buffer.Store4(offset + (1 << 4), packedSegment.B);
}

SegmentRecord LoadSegmentRecord(ByteAddressBuffer buffer, uint index)
{
    const uint offset = 32 * index;

    PackedSegmentRecord packedRecord;
    {
        packedRecord.A = buffer.Load4(offset + (0 << 4));
        packedRecord.B = buffer.Load4(offset + (1 << 4));
    }
    return PackedSegmentRecord::Unpack(packedRecord);
}

struct PackedVertexRecord
{
    uint4 A; // PositionCS
    uint4 B; // PreviousPositionCS
    uint4 C; // Tangent, Normal
    uint4 D; // Texcoord0, Texcoord1

    static PackedVertexRecord Pack(VertexRecord record)
    {
        PackedVertexRecord packedRecord;
        {
            packedRecord.A = asuint(record.positionCS);
            packedRecord.B = asuint(record.previousPositionCS);
            packedRecord.C = asuint(float4(record.tangentWS, record.texCoord0));
        }
        return packedRecord;
    }

    static VertexRecord Unpack(PackedVertexRecord packedRecord)
    {
        const float4 A = asfloat(packedRecord.A);
        const float4 B = asfloat(packedRecord.B);
        const float4 C = asfloat(packedRecord.C);
        const float4 D = asfloat(packedRecord.D);

        VertexRecord record;
        {
            record.positionCS         = A;
            record.previousPositionCS = B;
            record.tangentWS          = C.xyz;
            record.texCoord0          = C.w;;
            record.normalWS           = D.xyz;
            record.texCoord1          = D.w;
        }
        return record;
    }
};

void StoreVertexRecord(RWByteAddressBuffer buffer, VertexRecord record, uint index)
{
    const PackedVertexRecord packedVertex = PackedVertexRecord::Pack(record);

    const uint offset = 80 * index;

    buffer.Store4(offset + (0 << 4), packedVertex.A);
    buffer.Store4(offset + (1 << 4), packedVertex.B);
    buffer.Store4(offset + (2 << 4), packedVertex.C);
    buffer.Store4(offset + (3 << 4), packedVertex.D);
}

VertexRecord LoadVertexRecord(ByteAddressBuffer buffer, uint index)
{
    const uint offset = 80 * index;

    PackedVertexRecord packedRecord;
    {
        packedRecord.A = buffer.Load4(offset + (0 << 4));
        packedRecord.B = buffer.Load4(offset + (1 << 4));
        packedRecord.C = buffer.Load4(offset + (2 << 4));
        packedRecord.D = buffer.Load4(offset + (3 << 4));
    }
    return PackedVertexRecord::Unpack(packedRecord);
}

// Structures
// -----------------------------------------------------

struct AABB
{
    float2 min;
    float2 max;

    float2 Center()
    {
        return (min + max) * 0.5;
    }
};

#define INSIDE 0 // 0000
#define LEFT   1 // 0001
#define RIGHT  2 // 0010
#define BOTTOM 4 // 0100
#define TOP    8 // 1000

// TODO: Currently we perform the clipping in NDC, figure out how to do it in homogenous coordinates instead.
#define MIN_X -1
#define MAX_X +1
#define MIN_Y -1
#define MAX_Y +1

struct ClippingParams
{
    float minX;
    float maxX;
    float minY;
    float maxY;
};

ClippingParams DefaultClippingParams()
{
    ClippingParams params;
    {
        params.minX = -1;
        params.maxX = +1;
        params.minY = -1;
        params.maxY = +1;
    }
    return params;
}

uint ComputeOutCode(float x, float y, ClippingParams clipping)
{
    uint code = INSIDE;
    {
        if      (x < clipping.minX ) { code |= LEFT;   }
        else if (x > clipping.maxX ) { code |= RIGHT;  }
        if      (y < clipping.minY ) { code |= BOTTOM; }
        else if (y > clipping.maxY ) { code |= TOP;    }
    }
    return code;
}

// TODO: Investigate "Improvement in the Cohen-Sutherland Line Segment Clipping Algorithm" for something faster.
bool ClipSegmentCohenSutherland(inout float x0, inout float y0, inout float x1, inout float y1, ClippingParams clipping)
{
    uint outCode0 = ComputeOutCode(x0, y0, clipping);
    uint outCode1 = ComputeOutCode(x1, y1, clipping);

    bool accept = false;

    for(;;)
    {
        // Trivially accept, both points inside the viewport.
        if(!(outCode0 | outCode1))
        {
            accept = true;
            break;
        }
        // Trivially reject, both points outside the viewport.
        else if(outCode0 & outCode1)
        {
            break;
        }
        // Both tests failed, calculate the clipped segment.
        else
        {
            // One point is outside the window. Need to compute a new point clipped to the viewport edge.
            // Default initialize to keep the compiler warnings away.
            float x = -1, y = -1;

            // Choose the out code that is outside the viewport.
            uint outCodeOut = outCode1 > outCode0 ? outCode1 : outCode0;

            // Determine the clipped position based on the out code.
            if      (outCodeOut & TOP)    { x = x0 + (x1 - x0) * (clipping.maxY - y0) / (y1  - y0); y = clipping.maxY; }
            else if (outCodeOut & BOTTOM) { x = x0 + (x1 - x0) * (clipping.minY - y0) / (y1  - y0); y = clipping.minY; }
            else if (outCodeOut & RIGHT)  { y = y0 + (y1 - y0) * (clipping.maxX - x0) / (x1  - x0); x = clipping.maxX; }
            else if (outCodeOut & LEFT)   { y = y0 + (y1 - y0) * (clipping.minX - x0) / (x1  - x0); x = clipping.minX; }

            if (outCodeOut == outCode0)
            {
                x0 = x;
                y0 = y;
                outCode0 = ComputeOutCode(x0, y0, clipping);
            }
            else
            {
                x1 = x;
                y1 = y;
                outCode1 = ComputeOutCode(x1, y1, clipping);
            }
        }
    }

    return accept;
}


bool ClipSegmentCohenSutherland(inout float x0, inout float y0, inout float x1, inout float y1)
{
    return ClipSegmentCohenSutherland(x0, y0, x1, y1, DefaultClippingParams());
}

// Signed distance to a line segment.
// Ref: https://www.shadertoy.com/view/3tdSDj
float DistanceToSegmentAndTValueSq(float2 P, float2 A, float2 B, out float T)
{
    float2 BA = B - A;
    float2 PA = P - A;

    // Also output the 'barycentric' segment coordinate computed as a bi-product of the coverage.
    T = clamp( dot(PA, BA) / dot(BA, BA), 0.0, 1.0);

    const float2 V = PA - T * BA;
    return dot(V, V);
}

float DistanceToSegmentAndTValue(float2 P, float2 A, float2 B, out float T)
{
    return sqrt(DistanceToSegmentAndTValueSq(P, A, B, T));
}

void GetSegmentBoundingBox(SegmentRecord segment, out uint2 tilesB, out uint2 tilesE)
{
    // Determine the AABB of the segment.
    AABB aabb;
    aabb.min = min(segment.positionSS0, segment.positionSS1);
    aabb.max = max(segment.positionSS0, segment.positionSS1);

    // Transform AABB: NDC -> Tiled Raster Space.
    tilesB = ((aabb.min.xy * 0.5 + 0.5) * _SizeScreen.xy) / _SizeBin.x;
    tilesE = ((aabb.max.xy * 0.5 + 0.5) * _SizeScreen.xy) / _SizeBin.x;

    // Clamp AABB to tiled raster space.
    tilesB = clamp(tilesB, int2(0, 0), _DimBin - 1);
    tilesE = clamp(tilesE, int2(0, 0), _DimBin - 1);
}

bool SegmentsIntersectsBin(uint x, uint y, float2 p0, float2 p1, inout float z)
{
    float2 tileB = float2(x, y);
    float2 tileE = tileB + 1.0;

    // Construct an AABB of this tile.
    AABB aabbTile;
    aabbTile.min = float2(tileB * _SizeBin.yz - 1.0);
    aabbTile.max = float2(tileE * _SizeBin.yz - 1.0);

    // Get the tile's center.
    float2 center = aabbTile.Center();

    float d = DistanceToSegmentAndTValue(center.xy, p0.xy, p1.xy, z);

    // Compute the segment coverage provided by the segment distance.
    const uint pad = 2;
    float coverage = 1 - step((_SizeBin.x + pad) / min(_SizeScreen.x, _SizeScreen.y), d);

    return any(coverage);
}

float EncodeLineWidth(float alpha, float width)
{
    width /= 255.0;

    const uint a = PackFloatToUInt(alpha, 0,  16);
    const uint b = PackFloatToUInt(width,16,  16);
    return asfloat(a | b);
}

void DecodeLineWidth(float data, inout float alpha, inout float width)
{
    alpha = UnpackUIntToFloat(asuint(data), 0,  16);
    width = UnpackUIntToFloat(asuint(data), 16, 16);

    width *= 255.0;
    width  = clamp(width, 0.5f, 3.5f); // Soft clamp within tile size.
}
