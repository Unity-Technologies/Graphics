#ifndef VOXELIZE_SCENE
#define VOXELIZE_SCENE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
float4x4 unity_ObjectToWorld;

#ifdef USE_INSTANCE_TRANSFORMS
CBUFFER_START(InstanceData)
    float4x4 _InstanceToWorld[1000]; // Max buffer size is 64KB, matrix is 64B
CBUFFER_END
#endif

RWTexture3D<float>  _Output : register(u4);
float               _OutputSize;
float3              _VolumeWorldOffset;
float3              _VolumeSize;
uint                _AxisIndex;
uint                _IndexBufferOffset;
uint                _IndexBufferTopology; // 0 is triangle topology and 1 is quad topology.
uint                _VertexBufferOffset;

#define INDEX_BUFFER_TOPOLOGY_TRIANGLES 0
#define INDEX_BUFFER_TOPOLOGY_QUADS 1

struct VertexInput
{
    float4 vertex : POSITION;
    uint instanceID : SV_InstanceID;
};

struct VertexToGeometry
{
    float4 vertex : SV_POSITION;
    float3 cellPos01 : TEXCOORD0;
};

struct GeometryToFragment
{
    float4 vertex : SV_POSITION;
    float3 cellPos01 : TEXCOORD0;
    nointerpolation float2 minMaxX : TEXCOORD1;
    nointerpolation float2 minMaxY : TEXCOORD2;
    nointerpolation float2 minMaxZ : TEXCOORD3;
};

sampler s_point_clamp_sampler;

TEXTURE2D(_TerrainHeightmapTexture);
TEXTURE2D(_TerrainHolesTexture);
float4              _TerrainSize;
float               _TerrainHeightmapResolution;

struct TerrainVertexToFragment
{
    float4 vertex : SV_POSITION;
    float3 cellPos01 : TEXCOORD0;
    float2 uv : TEXCOORD1;
};

TerrainVertexToFragment TerrainVert(uint vertexID : SV_VERTEXID, uint instanceID : SV_InstanceID)
{
    TerrainVertexToFragment o;

    uint quadID = vertexID / 4;
    uint2 quadPos = uint2(quadID % uint(_TerrainHeightmapResolution), quadID / uint(_TerrainHeightmapResolution));
    float4 vertex = GetQuadVertexPosition(vertexID % 4);
    uint2 heightmapLoadPosition = quadPos + vertex.xy;

    float2 scale = _TerrainSize.xz / _TerrainHeightmapResolution;
    vertex.xy = heightmapLoadPosition * scale;

    // flip quad to xz axis (default terrain orientation without rotation)
    vertex = float4(vertex.x, 0, vertex.y, 1);

    float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(uint3(heightmapLoadPosition, 0)));
    vertex.y += height * _TerrainSize.y * 2;

    o.uv = heightmapLoadPosition / _TerrainHeightmapResolution;

    float3 cellPos = mul(unity_ObjectToWorld, vertex).xyz;
    cellPos -= _VolumeWorldOffset;
    o.cellPos01 = (cellPos / _VolumeSize);

    float4 p = float4(cellPos, 1);

    switch (_AxisIndex)
    {
        case 0: // forward
            p.xyz = p.xyz;
            break;
        case 1: // right
            p.xyz = p.yzx;
            break;
        case 2: // up
            p.xyz = p.zxy;
            break;
    }

    o.vertex = float4(p.xyz / _VolumeSize, 1);
    o.vertex.xyz = o.vertex.xyz * 2 - 1; // map from [0, 1] to [-1 1].

    return o;
}

float4 TerrainFrag(TerrainVertexToFragment i) : SV_Target
{
    if (any(i.cellPos01 < -EPSILON) || any(i.cellPos01 >= 1 + EPSILON))
        return 0;

    // Offset the cellposition with the heightmap
    float hole = _TerrainHolesTexture.SampleLevel(s_point_clamp_sampler, i.uv, 0).r;
    clip(hole == 0.0f ? -1 : 1);

    uint3 pos = min(uint3(i.cellPos01 * _OutputSize), uint3(_OutputSize, _OutputSize, _OutputSize));
    _Output[pos] = 1;

    return float4(i.cellPos01.xyz, 1);
}

struct VertexToFragment
{
    float4 vertex : SV_POSITION;
    float3 cellPos01 : TEXCOORD0;
    nointerpolation float2 minMaxX : TEXCOORD1;
    nointerpolation float2 minMaxY : TEXCOORD2;
    nointerpolation float2 minMaxZ : TEXCOORD3;
};

ByteAddressBuffer _IndexBuffer;
ByteAddressBuffer _VertexBuffer;
uint _IndexBufferBitCount; // Expected to be 16 or 32.
uint _VertexBufferPositionOffset;
uint _VertexBufferStride;

uint GetByte(uint4 data, uint pos)
{
    const uint uintPos = pos / 4;
    const uint localPos = pos % 4;
    return (data[uintPos] >> localPos * 8) & 0xFF;
}

// Inputs are expected to be between 0 and 255 inclusive.
uint MergeBytes(uint byt0, uint byt1, uint byt2, uint byt3)
{
    return (byt3 << 24) | (byt2 << 16) | (byt1 << 8) | byt0;
}

float3 LoadVertexPosition(ByteAddressBuffer buffer, uint vertexStride, uint vertexPositionOffset, uint vertexIndex)
{
    // This procedure extracts the float3 position for a given vertex from a ByteAddressBuffer view into Unity's vertex buffer.
    const uint byteOffset = vertexIndex * vertexStride + vertexPositionOffset;
    const uint wordOffset = byteOffset % 4;
    const uint4 vertexBufferData = buffer.Load4(byteOffset - wordOffset);

    float3 output;
    for (uint i = 0; i < 3; ++i)
    {
        const uint localOffset = i * 4;
        uint data = MergeBytes(
            GetByte(vertexBufferData, wordOffset + localOffset + 0),
            GetByte(vertexBufferData, wordOffset + localOffset + 1),
            GetByte(vertexBufferData, wordOffset + localOffset + 2),
            GetByte(vertexBufferData, wordOffset + localOffset + 3));
        output[i] = asfloat(data);
    }
    return output;
}

uint LoadIndex(ByteAddressBuffer indexBuffer, uint position)
{
    uint output;
    if (_IndexBufferBitCount == 32)
    {
        output = indexBuffer.Load(position * 4);
    }
    else
    {
        const uint uintPosition = position / 2;
        output = indexBuffer.Load(uintPosition * 4);
        // 16 integers consist of two bytes XY. Suppose we have stored two 16bit integers AB and CD.
        // Because HLSL is little-endian this will be stored in memory as BADC.
        // When we from ByteAddressBuffer this comes out as CDAB because HLSL is little-endian.
        // Thus, we'll find the first integer in lower 16 bits and the second in the upper 16 bits.
        if (position % 2 == 0)
            output = output & 0xFFFF;
        else
            output = output >> 16;
    }
    return output;
}

// Given a triangle edge defined by 2 verts, calculate 2 new verts that define the same edge,
// pushed outwards enough to cover centroids of any pixels which may intersect the triangle.
void OffsetEdge(float2 v1, float2 v2, float2 pixelSize, bool flipNormal, out float2 v1Offset, out float2 v2Offset)
{
    // Find the normal of the edge
    float2 edge = v2 - v1;
    float2 normal = normalize(float2(-edge.y, edge.x)) * (flipNormal ? -1 : 1);

    // Find the amount to offset by. This is the semidiagonal of the pixel box in the same quadrant as the normal.
    float2 semidiagonal = pixelSize / sqrt(2.0);
    semidiagonal *= float2(normal.x > 0 ? 1 : -1, normal.y > 0 ? 1 : -1);

    // Offset the edge
    v1Offset = v1 + semidiagonal;
    v2Offset = v2 + semidiagonal;
}

// Given 2 lines defined by 2 points each that are assumed to not be parallel, find the intersection point.
float2 LineIntersectAssumingNotParallel(float2 p1, float2 p2, float2 p3, float2 p4)
{
    // Line p1p2 represented as a1x + b1y = c1
    float a1 = p2.y - p1.y;
    float b1 = p1.x - p2.x;
    float c1 = a1 * p1.x + b1 * p1.y;

    // Line p3p4 represented as a2x + b2y = c2
    float a2 = p4.y - p3.y;
    float b2 = p3.x - p4.x;
    float c2 = a2 * p3.x + b2 * p3.y;

    float determinant = a1 * b2 - a2 * b1;
    float x = (b2 * c1 - b1 * c2) / determinant;
    float y = (a1 * c2 - a2 * c1) / determinant;
    return float2(x, y);
}

float3 RayPlaneIntersect(float3 rayOrigin, float3 rayDir, float3 planePosition, float3 planeNormal)
{
    // Want to find t such that HitPos = RayOrigin + t * RayDirection.
    // We know that dot(HitPos - PlanePosition, PlaneNormal) = 0.
    // Substituting in we get
    // dot(RayOrigin + t * RayDirection - PlanePosition, PlaneNormal) = 0,
    // and solving for t we get
    // t = dot(PlanePosition - RayOrigin, RayNormal) / dot(normal, RayOrigin).
    const float dotNumerator = dot(planePosition - rayOrigin, planeNormal);
    const float bottomDenominator = dot(rayDir, planeNormal);
    const float t = dotNumerator / bottomDenominator;
    return rayOrigin + t * rayDir;
}

uint GetIndexBufferTriangleOffset(uint vertexIndex, uint indexBufferTopology)
{
    if (indexBufferTopology == INDEX_BUFFER_TOPOLOGY_TRIANGLES)
    {
        const uint triIdx = vertexIndex / 3;
        return triIdx * 3;
    }
    else // Assuming quad topology.
    {
        const uint triIdx = vertexIndex / 3;
        const uint quadIdx = vertexIndex / 2;
        return quadIdx + (triIdx % 2);
    }
}

VertexToFragment MeshVert(uint instanceID : SV_InstanceID, uint vertexIndex : SV_VertexID)
{
    // This procedure dilates each triangle in order to emulate 2D conservative rasterization. This gives us
    // conservative rasterization on platforms that doesn't support conservative rasterization natively and
    // it ensures that we have a single code path for all platforms. The dilation logic is based on
    // https://developer.nvidia.com/gpugems/gpugems2/part-v-image-oriented-computing/chapter-42-conservative-rasterization

    const float voxelSize = rcp(_OutputSize);
    const uint indexBufferTriangleOffset = GetIndexBufferTriangleOffset(vertexIndex, _IndexBufferTopology);
    const uint triangleVertexId = vertexIndex % 3;

    float3 triangle3dCellPos01[3];
    for (uint i = 0; i < 3; ++i)
    {
        const uint vertexBufferIndex = _VertexBufferOffset + LoadIndex(_IndexBuffer, _IndexBufferOffset + indexBufferTriangleOffset + i);
        const float3 positionObjectSpace = LoadVertexPosition(_VertexBuffer, _VertexBufferStride, _VertexBufferPositionOffset, vertexBufferIndex);
        float4 positionWorldSpace = mul(unity_ObjectToWorld, float4(positionObjectSpace, 1.0f));
        #ifdef USE_INSTANCE_TRANSFORMS
        positionWorldSpace = mul(_InstanceToWorld[instanceID], positionWorldSpace);
        #endif
        float3 positionVolumeSpace = positionWorldSpace.xyz - _VolumeWorldOffset;
        triangle3dCellPos01[i] = positionVolumeSpace / _VolumeSize;
    }

    float2 triangle2dCellPos01[3];
    switch (_AxisIndex)
    {
    case 0: // forward
        triangle2dCellPos01[0] = triangle3dCellPos01[0].xy;
        triangle2dCellPos01[1] = triangle3dCellPos01[1].xy;
        triangle2dCellPos01[2] = triangle3dCellPos01[2].xy;
        break;
    case 1: // right
        triangle2dCellPos01[0] = triangle3dCellPos01[0].yz;
        triangle2dCellPos01[1] = triangle3dCellPos01[1].yz;
        triangle2dCellPos01[2] = triangle3dCellPos01[2].yz;
        break;
    case 2: // up
        triangle2dCellPos01[0] = triangle3dCellPos01[0].zx;
        triangle2dCellPos01[1] = triangle3dCellPos01[1].zx;
        triangle2dCellPos01[2] = triangle3dCellPos01[2].zx;
        break;
    }

    float2 dilated2dCellPos;
    // Find the dilated vertex position by intersecting two pushed-out triangle edges.
    // https://developer.nvidia.com/gpugems/gpugems2/part-v-image-oriented-computing/chapter-42-conservative-rasterization
    {
        // This normal check is required because may see backsides of triangles where winding order
        // in 2D is reversed. We want to voxelize these triangles too.
        const float2 edgeAB = triangle2dCellPos01[1] - triangle2dCellPos01[0];
        const float2 edgeAC = triangle2dCellPos01[2] - triangle2dCellPos01[0];
        const bool flipNormal = edgeAB.x * edgeAC.y - edgeAB.y * edgeAC.x > 0;

        const float2 pixelSize = float2(voxelSize, voxelSize);

        // Find their intersections. This is the new triangle.
        float2 line0Start, line0End, line1Start, line1End;
        OffsetEdge(triangle2dCellPos01[triangleVertexId], triangle2dCellPos01[(triangleVertexId + 1) % 3], pixelSize, flipNormal, line0Start, line0End);
        OffsetEdge(triangle2dCellPos01[(triangleVertexId + 2) % 3], triangle2dCellPos01[triangleVertexId], pixelSize, flipNormal, line1Start, line1End);
        dilated2dCellPos = LineIntersectAssumingNotParallel(line1Start, line1End, line0Start, line0End);
    }

    VertexToFragment output;

    // The dilated triangle may cause voxels that are arbitrarily far way from the triangle to be written. We avoid
    // this by rejecting all voxels that fall outside a conservatively defined box.
    {
        const float halfVoxelSize = voxelSize * 0.5f;
        const float3 minPos = min(min(triangle3dCellPos01[0], triangle3dCellPos01[1]), triangle3dCellPos01[2]) - halfVoxelSize;
        const float3 maxPos = max(max(triangle3dCellPos01[0], triangle3dCellPos01[1]), triangle3dCellPos01[2]) + halfVoxelSize;
        output.minMaxX = float2(minPos.x, maxPos.x);
        output.minMaxY = float2(minPos.y, maxPos.y);
        output.minMaxZ = float2(minPos.z, maxPos.z);
    }

    // The triangle has been dilated in 2D. We need to also find the equivalent 3D position on the plane of the
    // current triangle, so the hardware interpolator can do its thing.
    {
        float3 rayOrigin;
        float3 rayDir;
        switch (_AxisIndex)
        {
        case 0: // forward
            rayOrigin = float3(dilated2dCellPos.xy, 0);
            rayDir = float3(0, 0, 1);
            break;
        case 1: // right
            rayOrigin  = float3(0, dilated2dCellPos.xy);
            rayDir = float3(1, 0, 0);
            break;
        case 2: // up
            rayOrigin  = float3(dilated2dCellPos.y, 0, dilated2dCellPos.x);
            rayDir = float3(0, 1, 0);
            break;
        }
        const float3 triNormal = normalize(cross(triangle3dCellPos01[1] - triangle3dCellPos01[0], triangle3dCellPos01[2] - triangle3dCellPos01[0]));
        const float3 dilated3dCellPos = RayPlaneIntersect(rayOrigin, rayDir, triangle3dCellPos01[triangleVertexId], triNormal);
        output.cellPos01 = dilated3dCellPos;
    }

    {
        const float2 dilated2dClipPos = dilated2dCellPos * 2 - 1;
        output.vertex = float4(dilated2dClipPos, 0, 1);
    }

    return output;
}

float4 MeshFrag(VertexToFragment i) : SV_Target
{
    if (i.cellPos01.x < i.minMaxX.x || i.cellPos01.x > i.minMaxX.y)
        return 0;
    if (i.cellPos01.y < i.minMaxY.x || i.cellPos01.y > i.minMaxY.y)
        return 0;
    if (i.cellPos01.z < i.minMaxZ.x || i.cellPos01.z > i.minMaxZ.y)
        return 0;

    if (any(i.cellPos01 < -EPSILON) || any(i.cellPos01 >= 1 + EPSILON))
        return 0;

    uint3 pos = min(uint3(i.cellPos01 * _OutputSize), _OutputSize);

    _Output[pos] = 1;

    return float4(1, 1, 1, 1);
}

#endif
