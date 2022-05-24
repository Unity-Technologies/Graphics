
// Number of instances in current drawcall/dispatch
#define instancingCurrentCount instancingConstants.x

// Number of instances that are active at the time of the drawcall/dispatch
// ContextData
// instancingCurrentCount <= instancingActiveCount
#define instancingActiveCount  instancingConstants.y

// Number of instances that this batch can hold
// instancingActiveCount <= instancingBatchSize
#define instancingBatchSize    instancingConstants.z

// Data shared for all contexts (init, update, output)
// Contains one entry for each ACTIVE instance
struct ContextData
{
    uint maxParticleCount;
    uint systemSeed;
};
StructuredBuffer<ContextData> instancingContextData;

#if VFX_INSTANCING_VARIABLE_SIZE
// Prefix sum with particle counts for each active instance
StructuredBuffer<uint> instancingPrefixSum;
#endif

#if VFX_INSTANCING_BATCH_INDIRECTION
// Indirection buffer, contains one entry for each active instance, holding the instance index in the batch
StructuredBuffer<uint> instancingIndirect;
#endif

#if VFX_INSTANCING_ACTIVE_INDIRECTION
// Indirection buffer, contains one entry for each instance in current drawcall/dispatch, holding the index in the active instances
StructuredBuffer<uint> instancingActiveIndirect;
#endif

#if VFX_INSTANCING_VARIABLE_SIZE
// Get instance index in current drawcall/dispatch, for variable size instances
uint VFXGetVariableSizeInstanceIndex(inout uint index)
{
    uint startIndex = 0u;
    uint endIndex = instancingCurrentCount;
    return BinarySearchPrefixSum(index, instancingPrefixSum, startIndex, endIndex, index);
}
#elif VFX_INSTANCING_FIXED_SIZE
// Get instance index in current drawcall/dispatch, for fixed size instances
uint VFXGetFixedSizeInstanceIndex(inout uint index)
{
    uint instanceIndex = index / VFX_INSTANCING_FIXED_SIZE;
    instanceIndex = min(instanceIndex, instancingCurrentCount - 1);
    index -= instanceIndex * VFX_INSTANCING_FIXED_SIZE;
    return instanceIndex;
}
#endif

// Get instance index in current drawcall/dispatch, from particle index
uint VFXGetInstanceCurrentIndex(inout uint index)
{
#if VFX_INSTANCING_VARIABLE_SIZE
    return VFXGetVariableSizeInstanceIndex(index);
#elif VFX_INSTANCING_FIXED_SIZE
    return VFXGetFixedSizeInstanceIndex(index);
#else
    return 0;
#endif
}

// Get instance index in the active instances, from instance index in current drawcall/dispatch
uint VFXGetInstanceActiveIndex(uint instanceCurrentIndex)
{
    uint instanceActiveIndex = instanceCurrentIndex;
#if VFX_INSTANCING_ACTIVE_INDIRECTION
    if (instancingCurrentCount < instancingActiveCount)
    {
        instanceActiveIndex = instancingActiveIndirect[instanceActiveIndex];
    }
#endif
    return instanceActiveIndex;
}

// Get instance index in the batch, from instance index in the active instances
uint VFXGetInstanceBatchIndex(uint instanceActiveIndex)
{
    uint instanceBatchIndex = instanceActiveIndex;
#if VFX_INSTANCING_BATCH_INDIRECTION
    if (instancingActiveCount < instancingBatchSize)
    {
        instanceBatchIndex = instancingIndirect[instanceBatchIndex];
    }
#endif
    return instanceBatchIndex;
}
