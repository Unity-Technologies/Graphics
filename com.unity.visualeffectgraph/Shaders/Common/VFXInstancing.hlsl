#ifndef ALIGNED_SYSTEM_CAPACITY
#error Make sure that VFXGlobalInclude is pasted before VFXInstancing.hlsl is included
#endif

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

