#ifndef __VFX_STRUCTURED_BUFFER_UINT
#define __VFX_STRUCTURED_BUFFER_UINT

#define __VFX_STRUCTURED_BUFFER_TYPE uint
#include "Packages/com.unity.visualeffectgraph/Shaders/Temp/Data/StructuredBuffer.hlsl"

void DoInterlockedAdd(VFXStructuredBuffer_uint buf, uint index, uint value, out uint original)
{
    original = 0;
    if (buf.writeAccess)
    {
        InterlockedAdd(buf.bufferRW[buf.offset + index], value, original);
    }
}

void DoInterlockedAnd(VFXStructuredBuffer_uint buf, uint index, uint value, out uint original)
{
    original = 0;
    if (buf.writeAccess)
    {
        InterlockedAnd(buf.bufferRW[buf.offset + index], value, original);
    }
}

void DoInterlockedOr(VFXStructuredBuffer_uint buf, uint index, uint value, out uint original)
{
    original = 0;
    if (buf.writeAccess)
    {
        InterlockedOr(buf.bufferRW[buf.offset + index], value, original);
    }
}

void DoInterlockedXor(VFXStructuredBuffer_uint buf, uint index, uint value, out uint original)
{
    original = 0;
    if (buf.writeAccess)
    {
        InterlockedXor(buf.bufferRW[buf.offset + index], value, original);
    }
}

void DoInterlockedMin(VFXStructuredBuffer_uint buf, uint index, uint value, out uint original)
{
    original = 0;
    if (buf.writeAccess)
    {
        InterlockedMin(buf.bufferRW[buf.offset + index], value, original);
    }
}

void DoInterlockedMax(VFXStructuredBuffer_uint buf, uint index, uint value, out uint original)
{
    original = 0;
    if (buf.writeAccess)
    {
        InterlockedMax(buf.bufferRW[buf.offset + index], value, original);
    }
}

void DoInterlockedCompareStore(VFXStructuredBuffer_uint buf, uint index, uint compare, uint value)
{
    if (buf.writeAccess)
    {
        InterlockedCompareStore(buf.bufferRW[buf.offset + index], compare, value);
    }
}

void DoInterlockedCompareExchange(VFXStructuredBuffer_uint buf, uint index, uint compare, uint value, out uint original)
{
    original = 0;
    if (buf.writeAccess)
    {
        InterlockedCompareExchange(buf.bufferRW[buf.offset + index], compare, value, original);
    }
}

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
