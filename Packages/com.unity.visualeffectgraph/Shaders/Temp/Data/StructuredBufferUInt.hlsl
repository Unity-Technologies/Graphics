#ifndef __VFX_STRUCTURED_BUFFER_UINT
#define __VFX_STRUCTURED_BUFFER_UINT

#define __VFX_STRUCTURED_BUFFER_TYPE uint
#include "Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/StructuredBuffer.hlsl"

uint BinarySearchPrefixSum(uint value, VFXStructuredBuffer_uint prefixSum, out uint remainder)
{
    uint left = 0;
    uint right = prefixSum.size - 1;

    while (left < right)
    {
        uint center = (left + right) / 2;
        uint centerValue;
        prefixSum.LoadData(centerValue, center);
        if (value < centerValue)
        {
            right = center;
        }
        else
        {
            left = center + 1;
        }
    }
    uint index = left;

    uint prevValue = 0;
    [branch]
    if (index > 0)
    {
        prefixSum.LoadData(prevValue, index - 1);
    }
    remainder = value - prevValue;
    return index;
}

#endif //__VFX_STRUCTURED_BUFFER_UINT
