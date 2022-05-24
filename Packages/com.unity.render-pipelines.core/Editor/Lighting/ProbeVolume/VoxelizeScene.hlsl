#ifndef VOXELIZE_SCENE
# define VOXELIZE_SCENE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
float4x4 unity_ObjectToWorld;

#ifdef PROCEDURAL_INSTANCING_ON
// We have to use procedural instancing because we don't have data outside transform matrix
// But because of that a lot of useful macros are not defined, so force defining them here
#undef PROCEDURAL_INSTANCING_ON
#define INSTANCING_ON
#define UNITY_DONT_INSTANCE_OBJECT_MATRICES
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

#ifdef INSTANCING_ON
CBUFFER_START(UnityInstancing_PerTree)
    float4x4 _TreeInstanceToWorld[1000]; // Max buffer size is 64KB, matrix is 64B
CBUFFER_END

#undef unity_ObjectToWorld
#define unity_ObjectToWorld _TreeInstanceToWorld[unity_InstanceID]
#endif

RWTexture3D<float>  _Output : register(u4);
float3              _OutputSize;
float3              _VolumeWorldOffset;
float3              _VolumeSize;
uint                _AxisSwizzle;
float4x4            _TreePrototypeTransform;

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

float3 VertexToCellPos(float4 vertex)
{
#ifdef INSTANCING_ON
    // For tree instance, first transform vertex with prefab matrix
    vertex = mul(_TreePrototypeTransform, vertex);
#endif
    return mul(unity_ObjectToWorld, vertex).xyz;
}

VertexToGeometry ConservativeVertex(VertexInput input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    VertexToGeometry o;

    float3 cellPos = VertexToCellPos(input.vertex);
    cellPos -= _VolumeWorldOffset;
    o.cellPos01 = (cellPos / _VolumeSize);

    float4 p = float4(cellPos, 1);

    switch (_AxisSwizzle)
    {
        default:
        case 0: // top
            p.xyz = p.zxy;
            break;
        case 1: // right
            p.xyz = p.yzx;
            break;
        case 2: // forward
            p.xyz = p.xyz;
            break;
    }
    o.vertex = float4(p.xyz / _VolumeSize, 1);

    // trasnform pos from 0 1 to -1 1
    o.vertex.xyz = o.vertex.xyz * 2 - 1;

    return o;
}

[maxvertexcount(3)]
void ConservativeGeom(triangle VertexToGeometry inputVertex[3], inout TriangleStream<GeometryToFragment> triangleStream)
{
    float3 minPos = min(min(inputVertex[0].cellPos01, inputVertex[1].cellPos01), inputVertex[2].cellPos01) - rcp(_OutputSize.x);
    float3 maxPos = max(max(inputVertex[0].cellPos01, inputVertex[1].cellPos01), inputVertex[2].cellPos01) + rcp(_OutputSize.x);

    for (int i = 0; i < 3; i++)
    {
        GeometryToFragment o;
        o.vertex = inputVertex[i].vertex;
        o.cellPos01 = inputVertex[i].cellPos01;
        o.minMaxX = float2(minPos.x, maxPos.x);
        o.minMaxY = float2(minPos.y, maxPos.y);
        o.minMaxZ = float2(minPos.z, maxPos.z);
        triangleStream.Append(o);
    }
}

float4 ConservativeFrag(GeometryToFragment i) : SV_Target
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

    return float4(i.cellPos01, 1);
}

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

    // flip quad to xz axis (default terrain orientation without rotation)
    vertex = float4(vertex.x, 0, vertex.y, 1);

    // Offset quad to create the plane terrain
    vertex.xz += (float2(quadPos) / float(_TerrainHeightmapResolution)) * _TerrainSize.xz;

    uint2 id = (quadPos / _TerrainSize.xz) * _TerrainHeightmapResolution;
    float height = UnpackHeightmap(_TerrainHeightmapTexture.Load(uint3(heightmapLoadPosition, 0)));
    vertex.y += height * _TerrainSize.y * 2;

    o.uv = heightmapLoadPosition / _TerrainHeightmapResolution;

    float3 cellPos = mul(unity_ObjectToWorld, vertex).xyz;
    cellPos -= _VolumeWorldOffset;
    o.cellPos01 = (cellPos / _VolumeSize);

    float4 p = float4(cellPos, 1);

    switch (_AxisSwizzle)
    {
        default:
        case 0: // top
            p.xyz = p.zxy;
            break;
        case 1: // right
            p.xyz = p.yzx;
            break;
        case 2: // forward
            p.xyz = p.xyz;
            break;
    }
    o.vertex = float4(p.xyz / _VolumeSize, 1);

    // trasnform pos between 0 1 to -1 1
    o.vertex.xyz = o.vertex.xyz * 2 - 1;

    return o;
}

float4 TerrainFrag(TerrainVertexToFragment i) : SV_Target
{
    if (any(i.cellPos01 < -EPSILON) || any(i.cellPos01 >= 1 + EPSILON))
        return 0;

    // Offset the cellposition with the heightmap
    float hole = _TerrainHolesTexture.SampleLevel(s_point_clamp_sampler, i.uv, 0).r;
    clip(hole == 0.0f ? -1 : 1);

    uint3 pos = min(uint3(i.cellPos01 * _OutputSize), _OutputSize);
    _Output[pos] = 1;

    return float4(i.cellPos01.xyz, 1);
}

struct VertexToFragment
{
    float4 vertex : SV_POSITION;
    float3 cellPos01 : TEXCOORD0;
};

VertexToFragment MeshVert(VertexInput input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    VertexToFragment o;

    float3 cellPos = VertexToCellPos(input.vertex);
    cellPos -= _VolumeWorldOffset;
    o.cellPos01 = (cellPos / _VolumeSize);

    float4 p = float4(cellPos, 1);

    switch (_AxisSwizzle)
    {
        default:
        case 0: // top
            p.xyz = p.zxy;
            break;
        case 1: // right
            p.xyz = p.yzx;
            break;
        case 2: // forward
            p.xyz = p.xyz;
            break;
    }
    o.vertex = float4(p.xyz / _VolumeSize, 1);

    // trasnform pos from 0 1 to -1 1
    o.vertex.xyz = o.vertex.xyz * 2 - 1;

    return o;
}

float4 MeshFrag(VertexToFragment i) : SV_Target
{
    if (any(i.cellPos01 < -EPSILON) || any(i.cellPos01 >= 1 + EPSILON))
        return 0;

    uint3 pos = min(uint3(i.cellPos01 * _OutputSize), _OutputSize);

    _Output[pos] = 1;

    return float4(i.cellPos01, 1);
}

#endif
