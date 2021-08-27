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

//Creates and sets :
// * the "index" to access the attribute buffer
// * the "indexInInstance"
// * the "instanceIndex" giving which instance is being drawn
//TODO :nbDrawnInstances is supposed to be a uniform in the future
#define VFXSetInstancingIndices(index) \
{ \
       uint nbDrawnInstances = 1;  \
       uint instanceIdInDrawCall; \
       uint indexInInstance = BinarySearchPrefixSum(index, instancePrefixSum, nbDrawnInstances,instanceIdInDrawCall);  \
       uint instanceIndex = instanceIndirectionBuffer[instanceIdInDrawCall];  \
       index = GetIndexInAttributeBuffer(instanceIndex, indexInInstance);  \
}

void /*VFX*/SetInstancingIndices(
#if VFX_INSTANCING_VARIABLE_SIZE
uint nbInstancesInDispatch,
StructuredBuffer<uint> instancePrefixSum,
#else
uint nbMax,
#endif
#if VFX_INSTANCING_INDIRECTION
StructuredBuffer<uint> instanceIndirectionBuffer,
#endif
inout uint index, inout uint particleIndex)
{
    uint instanceIndex;
#if VFX_INSTANCING_VARIABLE_SIZE
    particleIndex = BinarySearchPrefixSum(index, instancePrefixSum, nbInstancesInDispatch,instanceIndex);
    #if VFX_INSTANCING_INDIRECTION
        instanceIndex = instanceIndirectionBuffer[instanceIndex];
    #endif
#else
    instanceIndex = index / nbMax;
    particleIndex = index - instanceIndex * nbMax;
    #if VFX_INSTANCING_INDIRECTION
        instanceIndex = instanceIndirectionBuffer[instanceIndex];
    #endif
#endif
    index = GetIndexInAttributeBuffer(instanceIndex, particleIndex);
}
