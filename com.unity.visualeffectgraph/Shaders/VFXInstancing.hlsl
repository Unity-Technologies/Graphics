#ifndef ALIGNED_SYSTEM_CAPACITY
#error Make sure that ALIGNED_SYSTEM_CAPACITY is defined before VFXInstancing.hlsl is included
#endif

#if VFX_INSTANCING_INDIRECTION
StructuredBuffer<uint> indirectionBufferInstances;
#endif
#if VFX_INSTANCING_VARIABLE_SIZE
StructuredBuffer<uint> prefixSumInstances;
#endif

struct VFXIndices
{
    uint instanceIndex;
    uint particleIndex;
    uint index;
};

uint GetIndexInInstance(uint rawIndex, uint instanceIndex, uint nbParticlesPerInstance)
{
    return rawIndex - instanceIndex * nbParticlesPerInstance;
}

uint GetIndexInAttributeBuffer(uint instanceIndex, uint indexInInstance)
{
    return instanceIndex * ALIGNED_SYSTEM_CAPACITY + indexInInstance;
} //current "index"

uint GetInstanceIndexFromGroupID(uint3 groupId,uint nbThreadPerGroup, uint dispatchWidth)
{
    return (groupId.x + dispatchWidth * groupId.y) * nbThreadPerGroup / ALIGNED_SYSTEM_CAPACITY;
}

void SetIndexAndInstanceIndex(inout VFXIndices vfxIndices)
{
    #if VFX_INSTANCING_INDIRECTION
    vfxIndices.instanceIndex = indirectionBufferInstances[vfxIndices.instanceIndex];
    #endif
    vfxIndices.index = GetIndexInAttributeBuffer(vfxIndices.instanceIndex, vfxIndices.particleIndex);
}

// The methods for extracting the indices are different in Compute and Output.
// In Compute shader, it relies on the SV_GroupId and DispatchWidth which are not relevant in Output
void VFXSetComputeInstancingIndices(
                            uint nbParticlesPerInstance,
                            uint3 groupId,
                            uint nbThreadPerGroup,
                            uint dispatchWidth,
                            inout VFXIndices vfxIndices)
{
    vfxIndices.instanceIndex = GetInstanceIndexFromGroupID(groupId, nbThreadPerGroup, dispatchWidth);
    vfxIndices.particleIndex = GetIndexInInstance(vfxIndices.index, vfxIndices.instanceIndex, nbParticlesPerInstance);
    SetIndexAndInstanceIndex(vfxIndices);
}
void VFXSetOutputInstancingIndices(
                            #if VFX_INSTANCING_VARIABLE_SIZE
                            uint nbInstancesInDispatch,
                            #else
                            uint nbParticlesPerInstance,
                            #endif
                            inout VFXIndices vfxIndices)
{
    #if VFX_INSTANCING_VARIABLE_SIZE
        vfxIndices.particleIndex = BinarySearchPrefixSum(vfxIndices.index, prefixSumInstances, nbInstancesInDispatch,vfxIndices.instanceIndex);
    #else
        vfxIndices.instanceIndex = vfxIndices.index / nbParticlesPerInstance;
        vfxIndices.particleIndex = GetIndexInInstance(vfxIndices.index, vfxIndices.instanceIndex, nbParticlesPerInstance);
    #endif
    SetIndexAndInstanceIndex(vfxIndices);
}
