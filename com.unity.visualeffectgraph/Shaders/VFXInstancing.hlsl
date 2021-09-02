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
    return (1 + (groupId.x + dispatchWidth * groupId.y) * nbThreadPerGroup) / max(ALIGNED_SYSTEM_CAPACITY, nbThreadPerGroup);
}

void SetIndexAndInstanceIndex(inout VFXIndices vfxIndices)
{
    #if VFX_INSTANCING_INDIRECTION
    vfxIndices.instanceIndex = indirectionBufferInstances[vfxIndices.instanceIndex];
    #endif
    vfxIndices.index = GetIndexInAttributeBuffer(vfxIndices.instanceIndex, vfxIndices.particleIndex);
}

void VFXSetInstancingIndices(
                        #if !VFX_INSTANCING_VARIABLE_SIZE
                        uint nbParticlesPerInstance,
                        #endif
                        inout VFXIndices vfxIndices
    )
{
    #if VFX_INSTANCING_VARIABLE_SIZE
        uint nbInstancesInDispatch, stride;
        prefixSumInstances.GetDimensions(nbInstancesInDispatch, stride);
        vfxIndices.particleIndex = BinarySearchPrefixSum(vfxIndices.index, prefixSumInstances, nbInstancesInDispatch,vfxIndices.instanceIndex);
    #else
        vfxIndices.particleIndex = GetIndexInInstance(vfxIndices.index, vfxIndices.instanceIndex, nbParticlesPerInstance);
    #endif
    SetIndexAndInstanceIndex(vfxIndices);
}

