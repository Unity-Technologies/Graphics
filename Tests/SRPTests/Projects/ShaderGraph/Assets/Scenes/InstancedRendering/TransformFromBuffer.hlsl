#ifndef TRANSFORM_FROM_BUFFER_HLSL
#define TRANSFORM_FROM_BUFFER_HLSL

StructuredBuffer<float4x4> _InstanceBuffer;

void TransformVertex_float(float3 inPos, uint instanceID, out float3 outPos)
{
    outPos = mul(_InstanceBuffer[instanceID], float4(inPos, 1)).xyz;
}

StructuredBuffer<float3> _VertexBuffer;
void TransformVertexFromVertexBuffer_float(uint vertexId, uint instanceID, out float3 outPos)
{
    outPos = mul(_InstanceBuffer[instanceID], float4(_VertexBuffer[vertexId], 1)).xyz;
}

StructuredBuffer<float3> _NormalBuffer;
void GetNormalFromNormalBuffer_float(uint vertexId, out float3 outNormal)
{
    outNormal = _NormalBuffer[vertexId];
}

#endif
