
#define VFX_GPU_EVENT_ELEMENT_COUNT_OFFSET 0u
#define VFX_GPU_EVENT_TOTAL_COUNT_OFFSET 1u
#define VFX_GPU_EVENT_PREFIX_SUM_OFFSET 2u
#define VFX_GPU_EVENT_SOURCE_INDEX_OFFSET 3u


uint VFXGetEventListBufferIndex(uint offset, uint instanceIndex, uint instanceSize, uint index)
{
#if VFX_USE_INSTANCING
    return instancingBatchSize * offset + instanceIndex * instanceSize + index;
#else
    return offset + index;
#endif
}

uint VFXGetEventListBufferIndex(uint offset, uint instanceIndex)
{
    return VFXGetEventListBufferIndex(offset, instanceIndex, 1u, 0u);
}

uint VFXGetEventListBufferElementCountIndex(uint instanceIndex)
{
    return VFXGetEventListBufferIndex(VFX_GPU_EVENT_ELEMENT_COUNT_OFFSET, instanceIndex);
}

uint VFXGetEventListBufferTotalCountIndex(uint instanceIndex)
{
    return VFXGetEventListBufferIndex(VFX_GPU_EVENT_TOTAL_COUNT_OFFSET, instanceIndex);
}

uint VFXGetEventListBufferPrefixSumIndex(uint instanceActiveIndex)
{
    return VFXGetEventListBufferIndex(VFX_GPU_EVENT_PREFIX_SUM_OFFSET, instanceActiveIndex);
}

uint VFXGetEventListBufferSourceIndex(uint instanceIndex, uint instanceSize, uint index)
{
    return VFXGetEventListBufferIndex(VFX_GPU_EVENT_SOURCE_INDEX_OFFSET, instanceIndex, instanceSize, index);
}

void AppendEventTotalCount(RWStructuredBuffer<uint> outputBuffer, uint totalCount, uint instanceIndex)
{
    uint totalCountIndex = VFXGetEventListBufferTotalCountIndex(instanceIndex);
    InterlockedAdd(outputBuffer[totalCountIndex], totalCount);
}

void AppendEventBuffer(RWStructuredBuffer<uint> outputBuffer, uint sourceIndex, uint outputCapacity, uint instanceIndex)
{
    uint eventIndex;
    uint elementCountIndex = VFXGetEventListBufferElementCountIndex(instanceIndex);
    InterlockedAdd(outputBuffer[elementCountIndex], 1u, eventIndex);

    [branch]
    if (eventIndex < outputCapacity)
    {
        eventIndex = VFXGetEventListBufferSourceIndex(instanceIndex, outputCapacity, eventIndex);
        outputBuffer[eventIndex] = sourceIndex;
    }
}
