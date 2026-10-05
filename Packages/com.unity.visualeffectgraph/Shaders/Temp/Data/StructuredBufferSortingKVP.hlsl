#ifndef __VFX_STRUCTURED_BUFFER_SORTING_KVP
#define __VFX_STRUCTURED_BUFFER_SORTING_KVP

#define __VFX_STRUCTURED_BUFFER_TYPE SortingKVP
#include "Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/StructuredBuffer.hlsl"

void DoInterlockedAddOnIndex(VFXStructuredBuffer_SortingKVP buf, uint index, uint value, out uint original)
{
    original = 0;
    if (buf.writeAccess)
    {
        InterlockedAdd(buf.bufferRW[buf.offset + index].index, value, original);
    }
}

#endif //__VFX_STRUCTURED_BUFFER_SORTING_KVP
