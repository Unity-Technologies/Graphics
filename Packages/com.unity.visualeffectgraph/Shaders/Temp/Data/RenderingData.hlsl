#ifndef __VFX_RENDERING_DATA
#define __VFX_RENDERING_DATA

struct SortingKVP
{
    float key;
    uint index;
};

#include "Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/StructuredBufferUInt.hlsl"
#include "Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/StructuredBufferSortingKVP.hlsl"

struct VFXSortingData
{
    VFXStructuredBuffer_SortingKVP counter;
    VFXStructuredBuffer_SortingKVP keysBuffer;

    void Init(VFXStructuredBuffer_SortingKVP counter, VFXStructuredBuffer_SortingKVP keysBuffer)
    {
        this.counter = counter;
        this.keysBuffer = keysBuffer;
    }

    void RegisterSortingKVP(SortingKVP kvp)
    {
        uint indirectIndex;
        DoInterlockedAddOnIndex(counter, 0, 1, indirectIndex);
        //InterlockedAdd(sortingInputs[0].index, 1, indirectIndex); // Use the first uint of the buffer as a counter for the number of registered KVPs
        //indirectIndex += 1; // First element is used as counter, so we need to offset by 1
        keysBuffer.StoreData(kvp, indirectIndex);
    }
};


struct VFXDrawIndirectionData
{
    VFXStructuredBuffer_uint counter;
    VFXStructuredBuffer_uint buffer;
    void Init(VFXStructuredBuffer_uint counter, VFXStructuredBuffer_uint buffer)
    {
        this.counter = counter;
        this.buffer = buffer;
    }

    void AppendRenderingIndex(uint index)
    {
        uint indirectIndex;
        DoInterlockedAdd(counter, 0, 1, indirectIndex);
        buffer.StoreData(index, indirectIndex);
    }

    bool GetRenderingIndex(uint index, out uint indirectIndex)
    {
        uint maxCount;
        counter.LoadData(maxCount, 0);
        if (index >= maxCount)
            return false;

        buffer.LoadData(indirectIndex, index);
        return true;
    }
};

#endif //__VFX_RENDERING_DATA
