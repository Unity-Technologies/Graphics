#ifndef ALIGNED_SYSTEM_CAPACITY
#error Make sure that VFXGlobalInclude is pasted before VFXInstancing.hlsl is included
#endif

struct VFXIndices
{
    uint instanceIndex;
    uint particleIndex;
    uint index;
};

uint GetIndexInInstance(uint threadId, uint instanceIndex, uint nbMax)
{
    return threadId - instanceIndex * nbMax;
}

uint GetIndexInAttributeBuffer(uint instanceIndex, uint indexInInstance, uint alignedSystemCapacity)
{
    return instanceIndex * alignedSystemCapacity + indexInInstance;
} //current "index"

uint GetInstanceIndexFromGroupID(uint3 groupId,uint nbThreadPerGroup, uint dispatchWidth, uint alignedSystemCapacity)
{
    return (groupId.x + dispatchWidth * groupId.y) * nbThreadPerGroup / alignedSystemCapacity;
}

void VFXSetComputeInstancingIndices(
                            uint nbParticlesPerInstance,
                            uint3 groupId,
                            uint nbThreadPerGroup,
                            uint dispatchWidth,
                            #if VFX_INSTANCING_INDIRECTION
                            StructuredBuffer<uint> indirectionBufferInstances,
                            #endif
                            uint alignedSystemCapacity,
                            inout VFXIndices vfxIndices
                            )
{
    vfxIndices.instanceIndex = GetInstanceIndexFromGroupID(groupId, nbThreadPerGroup, dispatchWidth, alignedSystemCapacity);
    vfxIndices.particleIndex = vfxIndices.index - vfxIndices.instanceIndex * nbParticlesPerInstance;
    #if VFX_INSTANCING_INDIRECTION
        vfxIndices.instanceIndex = indirectionBufferInstances[vfxIndices.instanceIndex];
    #endif
    vfxIndices.index = GetIndexInAttributeBuffer(vfxIndices.instanceIndex, vfxIndices.particleIndex,alignedSystemCapacity);
}


void VFXSetOutputInstancingIndices(
                            #if VFX_INSTANCING_VARIABLE_SIZE
                            uint nbInstancesInDispatch,
                            StructuredBuffer<uint> prefixSumInstances,
                            #else
                            uint nbParticlesPerInstance,
                            #endif
                            #if VFX_INSTANCING_INDIRECTION
                            StructuredBuffer<uint> indirectionBufferInstances,
                            #endif
                            uint alignedSystemCapacity,
                            inout VFXIndices vfxIndices
                            )
{
    #if VFX_INSTANCING_VARIABLE_SIZE
        vfxIndices.particleIndex = BinarySearchPrefixSum(vfxIndices.index, prefixSumInstances, nbInstancesInDispatch,vfxIndices.instanceIndex);
        #if VFX_INSTANCING_INDIRECTION
            vfxIndices.instanceIndex = indirectionBufferInstances[vfxIndices.instanceIndex];
        #endif
    #else
        vfxIndices.instanceIndex = vfxIndices.index / nbParticlesPerInstance;
        vfxIndices.particleIndex = vfxIndices.index - vfxIndices.instanceIndex * nbParticlesPerInstance;
        #if VFX_INSTANCING_INDIRECTION
            vfxIndices.instanceIndex = indirectionBufferInstances[vfxIndices.instanceIndex];
        #endif
    #endif
    vfxIndices.index = GetIndexInAttributeBuffer(vfxIndices.instanceIndex, vfxIndices.particleIndex,alignedSystemCapacity);
}
