#ifndef VBUFFER_COMMON_HLSL
#define VBUFFER_COMMON_HLSL

#include "VertexBufferCompaction.cs.hlsl"

void UnpackVisibilityBuffer(uint packedData, out uint clusterID, out uint triangleID)
{
    triangleID = packedData & 127;
    // All the remaining 25 bits can be used for cluster (for a max of 33554431 (2^25 - 1) clusters)
    clusterID = (packedData >> 7) & 33554431;
}

uint PackVisBuffer(uint clusterID, uint triangleID)
{
    uint output = 0;
    // Cluster size is 128, hence we need 7 bits at most for triangle ID.
    output = triangleID & 127;
    // All the remaining 25 bits can be used for cluster (for a max of 33554431 (2^25 - 1) clusters)
    output |= (clusterID & 33554431) << 7;
    return output;
}


#endif
