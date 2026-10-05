#ifndef __VFX_EVENT_LIST_DATA
#define __VFX_EVENT_LIST_DATA

#include "Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/StructuredBufferUint.hlsl"
#include "Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/AttributeBuffer.hlsl"
#include "Packages/com.unity.visualeffectgraph/Shaders/VFXGPUEvent.hlsl"

struct CPUEventListData
{
    uint eventCount;
    uint spawnCount;
    uint prevEventCount;
    VFXStructuredBuffer_uint spawnCountPrefixSumBuffer;

    void Init(VFXStructuredBuffer_uint eventsPrefixSumBuffer, VFXStructuredBuffer_uint spawnCountPrefixSumBuffer, uint instanceIndex)
    {
        eventsPrefixSumBuffer.LoadData(eventCount, instanceIndex);
        prevEventCount = 0;
        [branch]
        if(instanceIndex > 0)
        {
            eventsPrefixSumBuffer.LoadData(prevEventCount, instanceIndex - 1);
            eventCount -= prevEventCount;
        }

        spawnCountPrefixSumBuffer.GetRange(this.spawnCountPrefixSumBuffer, prevEventCount, eventCount);
        this.spawnCountPrefixSumBuffer.LoadData(spawnCount, eventCount - 1);
    }

    uint GetAttributesIndex(uint spawnIndex)
    {
        uint remainder;
        return BinarySearchPrefixSum(spawnIndex, spawnCountPrefixSumBuffer, remainder);
    }
};

struct GPUEventListData
{
    uint eventCount;
    uint spawnCount;      // alias of eventCount, matches CPUEventListData's naming used by Init.hlsl
    uint baseEventCount;
    VFXStructuredBuffer_uint elementCountBuffer; // this instance's atomic counter (1 elem)
    VFXStructuredBuffer_uint totalCountBuffer;   // this instance's running total (1 elem)
    VFXStructuredBuffer_uint indexBuffer;        // this instance's source indices (capacity elems)

    void Init(VFXStructuredBuffer_uint elementCountBuffer, VFXStructuredBuffer_uint totalCountBuffer,
        VFXStructuredBuffer_uint prefixSumBuffer, VFXStructuredBuffer_uint indexBuffer, uint instanceActiveIndex)
    {
        this.elementCountBuffer = elementCountBuffer;
        this.totalCountBuffer = totalCountBuffer;
        this.indexBuffer = indexBuffer;

        prefixSumBuffer.LoadData(eventCount, instanceActiveIndex);
        [branch]
        if (instanceActiveIndex > 0)
        {
            uint prevEventCount;
            prefixSumBuffer.LoadData(prevEventCount, instanceActiveIndex - 1);
            eventCount -= prevEventCount;
        }
        spawnCount = eventCount;

        uint totalEventCount;
        totalCountBuffer.LoadData(totalEventCount, 0);
        baseEventCount = totalEventCount - eventCount;
    }

    uint GetAttributesIndex(uint eventIndex)
    {
        uint attributesIndex;
        indexBuffer.LoadData(attributesIndex, eventIndex);
        return attributesIndex;
    }

    void AppendEvents(uint count, uint index)
    {
        uint capacity = indexBuffer.size;
        for (uint i = 0; i < count; ++i)
        {
            uint eventIndex;
            DoInterlockedAdd(elementCountBuffer, 0, 1u, eventIndex);
            if (eventIndex < capacity)
                indexBuffer.StoreData(index, eventIndex);
        }
        uint original;
        DoInterlockedAdd(totalCountBuffer, 0, count, original);
    }
};

#endif //__VFX_EVENT_LIST_DATA
