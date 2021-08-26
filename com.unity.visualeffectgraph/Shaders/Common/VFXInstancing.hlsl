#ifndef ALIGNED_SYSTEM_CAPACITY
#error Make sure that VFXGlobalInclude is pasted before VFXInstancing.hlsl is included
#endif

uint GetThreadId(uint3 groupId, uint3 groupThreadId, uint nbThreadPerGroup, uint dispatchWidth)
{
    return groupThreadId.x + groupId.x * nbThreadPerGroup + groupId.y * dispatchWidth * nbThreadPerGroup;
}

uint GetInstanceIndexFromVertexId(uint vertexId)
{
    return 0; //Not implemented
}

uint GetInstanceIndexFromThreadId(uint threadId, uint nbMax)
{
    //nbMax != ALIGNED_SYSTEM_CAPACITY for two reasons :
    // * nbMax can be equal to capacity, not aligned_capacity
    // * nbMax can be m_TotalSpawnCount in the case of immortal particles (without GPU events)
    return threadId / nbMax;
}
uint GetIndexInInstance(uint threadId, uint instanceIndex, uint nbMax)
{
    return threadId - instanceIndex * nbMax;
}

uint GetIndexInAttributeBuffer(uint instanceIndex, uint indexInInstance)
{
    return instanceIndex * ALIGNED_SYSTEM_CAPACITY + indexInInstance;
} //current "index"

