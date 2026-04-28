#ifndef __VFX_EVENT_LIST_DATA
#define __VFX_EVENT_LIST_DATA

#include "Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/StructuredBufferUint.hlsl"
#include "Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/AttributeBuffer.hlsl"
#include "Packages/com.unity.visualeffectgraph/Shaders/VFXGPUEvent.hlsl"

struct CPUEventListData
{
    uint eventCount;
    uint baseEventCount;
    VFXStructuredBuffer_uint eventPrefixSumBuffer;

    void Init(uint eventCount, uint baseEventCount, VFXStructuredBuffer_uint eventPrefixSumBuffer)
    {
        this.eventCount = eventCount;
        this.baseEventCount = baseEventCount;
        this.eventPrefixSumBuffer = eventPrefixSumBuffer;
    }

    void Init(VFXStructuredBuffer_uint vfxBuffer)
    {
        // TODO: Event count is last item in buffer when there is no split, change when splits are supported
        vfxBuffer.LoadData(eventCount, vfxBuffer.size - 1);

        // TODO: Base event count currently stored in ContextData
        baseEventCount = _GraphValuesBuffer_buffer.Load4(0).z;

        //TODO: instance index * active instance count instead of 1
        eventPrefixSumBuffer = vfxBuffer.GetRange(1, vfxBuffer.size - 1);
    }

    uint GetAttributesIndex(uint eventIndex)
    {
        return BinarySearchPrefixSum(eventIndex, eventPrefixSumBuffer.buffer, eventPrefixSumBuffer.offset, eventPrefixSumBuffer.offset + eventPrefixSumBuffer.size) - eventPrefixSumBuffer.offset;
    }
};

struct GPUEventListData
{
    uint eventCount;
    uint baseEventCount;
    VFXStructuredBuffer_uint indexBuffer;

    void Init(uint eventCount, uint baseEventCount, VFXStructuredBuffer_uint indexBuffer)
    {
        this.eventCount = eventCount;
        this.baseEventCount = baseEventCount;
        this.indexBuffer = indexBuffer;
    }

    void Init(VFXStructuredBuffer_uint vfxBuffer)
    {
        vfxBuffer.LoadData(eventCount, 2);

        uint totalEventCount;
        vfxBuffer.LoadData(totalEventCount, 1);
        baseEventCount = totalEventCount - eventCount;

        indexBuffer = vfxBuffer.GetRange(3, vfxBuffer.size - 3);     // TODO: Needs changes for instancing
    }

    uint GetAttributesIndex(uint eventIndex)
    {
        uint attributesIndex;
        indexBuffer.LoadData(attributesIndex, eventIndex);
        return attributesIndex;
    }
    
    void AppendEvents(uint count, uint index)
    {
        uint capacity = indexBuffer.size + 4;
        count = min(count, capacity - eventCount);
        for (uint i = 0; i < count; ++i)
            AppendEventBuffer(indexBuffer.bufferRW, index, capacity, 0);
        AppendEventTotalCount(indexBuffer.bufferRW, count, 0);
    }
};

#endif //__VFX_EVENT_LIST_DATA
