#ifndef PROCEDURAL_CUSTOM
#define PROCEDURAL_CUSTOM

#ifdef UNITY_DOTS_INSTANCING_UNIFORM_BUFFER

CBUFFER_START(_Positions)
    float4 Positions[1024];
CBUFFER_END

CBUFFER_START(_Normals)
    float4 Normals[1024];
CBUFFER_END

CBUFFER_START(_Tangents)
    float4 Tangents[1024];
CBUFFER_END

void ProceduralCustom_float(int VertexID, int BaseIndex, out float3 Position, out float3 Normal, out float3 Tangent)
{
    Position = Positions[VertexID + BaseIndex].xyz;
    Normal = Normals[VertexID + BaseIndex].xyz;
    Tangent = Tangents[VertexID + BaseIndex].xyz;
}

#else

StructuredBuffer<float4> _Positions;
StructuredBuffer<float4> _Normals;
StructuredBuffer<float4> _Tangents;

void ProceduralCustom_float(int VertexID, int BaseIndex, out float3 Position, out float3 Normal, out float3 Tangent)
{
    Position = _Positions[VertexID + BaseIndex].xyz;
    Normal = _Normals[VertexID + BaseIndex].xyz;
    Tangent = _Tangents[VertexID + BaseIndex].xyz;
}

#endif

#endif
