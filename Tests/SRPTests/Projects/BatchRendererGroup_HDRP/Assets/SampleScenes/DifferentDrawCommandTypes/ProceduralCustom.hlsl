#ifndef PROCEDURAL_CUSTOM
#define PROCEDURAL_CUSTOM

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
