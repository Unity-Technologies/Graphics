#ifndef VBUFFER_COMMON_HLSL
#define VBUFFER_COMMON_HLSL
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/VBuffer/HDRenderPipeline.VertexBufferCompaction.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/VBuffer/HDRenderPipeline.VisibilityBuffer.cs.hlsl"

void UnpackVisibilityBuffer(uint packedData, out uint clusterID, out uint triangleID)
{
    triangleID = packedData & 127;
    // All the remaining 25 bits can be used for cluster (for a max of 33554431 (2^25 - 1) clusters)
    clusterID = (packedData >> 7) & 33554431;
}

// VBuffer
StructuredBuffer<CompactVertex> _CompactedVertexBuffer;
StructuredBuffer<uint> _CompactedIndexBuffer;
StructuredBuffer<InstanceVData> _InstanceVDataBuffer;
TEXTURE2D_X_UINT(_VBuffer0);
TEXTURE2D_X_UINT(_MaterialDepth);

#endif
