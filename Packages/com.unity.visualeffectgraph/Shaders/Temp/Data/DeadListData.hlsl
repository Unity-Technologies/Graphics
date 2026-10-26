#ifndef __VFX_DEAD_LIST_DATA
#define __VFX_DEAD_LIST_DATA

#include "Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/StructuredBufferUint.hlsl"

struct VFXDeadListData
{
    VFXStructuredBuffer_uint counter;
    VFXStructuredBuffer_uint counterCopy;
    VFXStructuredBuffer_uint buffer;

    void Init(VFXStructuredBuffer_uint counter, VFXStructuredBuffer_uint counterCopy, VFXStructuredBuffer_uint buffer)
    {
        this.counter = counter;
        this.counterCopy = counterCopy;
        this.buffer = buffer;
    }

    bool NewIndex(uint threadIndex, out uint particleIndex)
    {
        uint deadCount;
        counterCopy.LoadData(deadCount, 0);
        bool success = false;
        particleIndex = 0;

        if (threadIndex < deadCount)
        {
            uint deadIndex;
            DoInterlockedAdd(counter, 0, -1, deadIndex);
            success = buffer.LoadData(particleIndex, deadIndex - 1);
        }
        return success;
    }

    bool DeleteIndex(uint index)
    {
        uint deadIndex;
        DoInterlockedAdd(counter, 0, 1, deadIndex);

        return buffer.StoreData(index, deadIndex);
    }
};

#endif //__VFX_DEAD_LIST_DATA
